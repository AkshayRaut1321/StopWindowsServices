using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.ServiceProcess;
using System.Threading.Tasks;
using System.Timers;
using Timer = System.Timers.Timer;
using System.Diagnostics;
using System.Threading;

namespace StopWindowsServices
{
    public partial class StopWindowsServices : ServiceBase
    {
        private Timer _startupTimer;
        static object acquireStop = new object();
        private const int MaxRetryAttempts = 5;
        private const int RetryDelayMs = 2000; // 2 seconds for retry backoff
        private CancellationTokenSource _cts;

        public StopWindowsServices()
        {
            InitializeComponent();
            // Set up the event log source if not already done
            if (!EventLog.SourceExists("StopWindowsServices"))
            {
                EventLog.CreateEventSource("StopWindowsServices", "Application");
            }
            _startupTimer = new Timer(IniFile.WS_TIMEOUT);
            _startupTimer.Elapsed += _startupTimer_Elapsed;
        }

        protected override void OnStart(string[] args)
        {
            try
            {
                try
                {
                    _cts = new CancellationTokenSource();

                    // Log service start event
                    EventLog.WriteEntry("StopWindowsServices", "Service is starting.", EventLogEntryType.Information);

                    System.Threading.Thread.Sleep(10000);

                    Task.Run(() => RunServiceFirstTime(_cts.Token));
                    _startupTimer.Start();

                    // Log timer start event
                    EventLog.WriteEntry("StopWindowsServices", "Timer started to check services.", EventLogEntryType.Information);
                }
                catch (Exception ex)
                {
                    // Log error
                    EventLog.WriteEntry("StopWindowsServices", $"Error in OnStart: {ex.Message}", EventLogEntryType.Error);

                    Logger.WriteLog("StopWindowsServices : OnStart :" + ex.Message, ex);
                    if (_startupTimer == null)
                        Logger.WriteLog("Start up timer is null");

                    _startupTimer.Stop();
                    _startupTimer.Start();
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog("StopWindowsServices : OnStart :" + ex.Message, ex);
            }
        }

        internal void RunServiceFirstTime(CancellationToken token)
        {
            lock (acquireStop)
            {
                RunService(token);
            }
        }

        protected override void OnStop()
        {
            _startupTimer.Stop();

            // Log service stop event
            EventLog.WriteEntry("StopWindowsServices", "Service is stopping.", EventLogEntryType.Information);
        }

        void _startupTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            _startupTimer.Stop();
            RunService(_cts.Token);
        }

        void RunService(CancellationToken token)
        {
            try
            {
                var serviceList = IniFile.SERVICES_LIST?.ToLower().Split(',')?.ToList();
                if (serviceList != null)
                {
                    // Running CheckAndStopWindowsServices asynchronously

                    Task.Run(() => CheckAndStopWindowsServices(serviceList, token));
                    //CheckAndStopWindowsServices(serviceList);
                }
                else
                {
                    Logger.WriteLog("No services found in configuration.", null);

                    EventLog.WriteEntry("StopWindowsServices", "No services found in configuration for timer check.", EventLogEntryType.Warning);
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog("StopWindowsServices : _startupTimer_Elapsed : " + ex.Message, ex);

                EventLog.WriteEntry("StopWindowsServices", $"Error in _startupTimer_Elapsed: {ex.Message}", EventLogEntryType.Error);
            }
            finally
            {
                _startupTimer.Start();
            }
        }

        private void CheckAndStopWindowsServices(List<string> listServiceNames, CancellationToken token)
        {
            if (token.IsCancellationRequested)
            {
                Logger.WriteLog("Service cancellation requested. Exiting service loop.");
                return;
            }

            foreach (string serviceName in listServiceNames)
            {
                // Retry logic for external services not yet started
                try
                {
                    var service = ServiceController.GetServices().FirstOrDefault(s => s.DisplayName.ToLower() == serviceName);
                    if (service != null)
                    {
                        try
                        {
                            service.Refresh(); // Ensure the status is fresh
                            if (service.Status == ServiceControllerStatus.Running || service.Status == ServiceControllerStatus.StartPending)
                            {
                                service.Stop();
                                service.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(15));
                            }
                            else if (service.Status != ServiceControllerStatus.Stopped)
                            {
                                Logger.WriteLog($"Service {service.Status}");
                            }
                        }
                        catch (ExternalException ex)
                        {
                            Logger.WriteLog("StopWindowsServices : CheckAndStopWindowsServices:" + ex.Message, ex);

                            EventLog.WriteEntry("StopWindowsServices", $"CheckAndStopWindowsServices ExternalException for service {service.DisplayName}: {ex.Message}", EventLogEntryType.Error);
                        }
                        catch (Exception ex)
                        {
                            EventLog.WriteEntry("StopWindowsServices", $"CheckAndStopWindowsServices Error stopping service {service.DisplayName}: {ex.Message}", EventLogEntryType.Error);

                            Logger.WriteLog($"StopWindowsServices : CheckAndStopWindowsServices: {ex.Message}", ex);
                        }
                    }
                    else
                    {
                        Logger.WriteLog($"StopWindowsServices : CheckAndStopWindowsServices: service not found {service.DisplayName}");
                    }
                }
                catch (ExternalException ex)
                {
                    Logger.WriteLog("StopWindowsServices : CheckAndStopWindowsServices :" + ex.Message, ex);

                    EventLog.WriteEntry("StopWindowsServices", $"CheckAndStopWindowsServices ExternalException: {ex.Message}", EventLogEntryType.Error);
                }
                catch (Exception ex)
                {
                    EventLog.WriteEntry("StopWindowsServices", $"CheckAndStopWindowsServices Error: {ex.Message}", EventLogEntryType.Error);

                    Logger.WriteLog($"StopWindowsServices : CheckAndStopWindowsServices error: {ex.Message}", ex);
                }
            }
        }
    }
}
