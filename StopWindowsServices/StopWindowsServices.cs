using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.ServiceProcess;
using System.Threading.Tasks;
using System.Timers;
using Timer = System.Timers.Timer;
using System.Diagnostics;

namespace StopWindowsServices
{
    public partial class StopWindowsServices : ServiceBase
    {
        private Timer _startupTimer;
        static object acquireStop = new object();
        private const int MaxRetryAttempts = 5;
        private const int RetryDelayMs = 2000; // 2 seconds for retry backoff

        public StopWindowsServices()
        {
            InitializeComponent();
            // Set up the event log source if not already done
            if (!EventLog.SourceExists("StopWindowsServices"))
            {
                EventLog.CreateEventSource("StopWindowsServices", "Application");
            }
        }

        protected override void OnStart(string[] args)
        {
            try
            {
                try
                {
                    // Log service start event
                    EventLog.WriteEntry("StopWindowsServices", "Service is starting.", EventLogEntryType.Information);

                    Task.Run(() => RunServiceFirstTime());
                    _startupTimer = new Timer(IniFile.WS_TIMEOUT);
                    _startupTimer.Elapsed += _startupTimer_Elapsed;
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

        internal void RunServiceFirstTime()
        {
            lock (acquireStop)
            {
                RunService();
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
            RunService();
        }

        void RunService()
        {
            try
            {
                var serviceList = IniFile.SERVICES_LIST?.ToLower().Split(',')?.ToList();
                if (serviceList != null)
                {
                    // Running CheckAndStopWindowsServices asynchronously
                    Task.Run(() => CheckAndStopWindowsServices(serviceList));
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

        private void CheckAndStopWindowsServices(List<string> listServiceNames)
        {
            var listServices = ServiceController.GetServices();
            foreach (var service in listServices)
            {
                try
                {
                    // Retry logic for external services not yet started
                    int retries = MaxRetryAttempts;
                    while (retries-- > 0)
                    {
                        try
                        {
                            if (listServiceNames.Contains(service.DisplayName.ToLower()))
                            {
                                service.Refresh(); // Ensure the status is fresh
                                if (service.Status == ServiceControllerStatus.Running)
                                {
                                    service.Stop();
                                    service.WaitForStatus(ServiceControllerStatus.Stopped);
                                    //ServiceHelper.ChangeStartMode(service, ServiceStartMode.Disabled);
                                }
                            }
                            break; // Exit the retry loop if successful
                        }
                        catch (Exception ex)
                        {
                            EventLog.WriteEntry("StopWindowsServices", $"Error stopping service {service.DisplayName}: Attempt {MaxRetryAttempts - retries} failed: {ex.Message}", EventLogEntryType.Error);

                            Logger.WriteLog($"StopWindowsServices : CheckAndStopWindowsServices : Attempt {MaxRetryAttempts - retries} failed: {ex.Message}", ex);
                            if (retries == 0)
                            {
                                // Log final failure after retry attempts are exhausted
                                Logger.WriteLog($"StopWindowsServices : CheckAndStopWindowsServices : Service {service.DisplayName} failed after {MaxRetryAttempts} attempts.", ex);
                            }
                            else
                            {
                                // Delay before retrying
                                System.Threading.Thread.Sleep(RetryDelayMs);
                            }
                        }
                    }
                }
                catch (ExternalException ex)
                {
                    Logger.WriteLog("StopWindowsServices : CheckAndStopWindowsServices :" + ex.Message, ex);

                    EventLog.WriteEntry("StopWindowsServices", $"ExternalException in CheckAndStopWindowsServices for service {service.DisplayName}: {ex.Message}", EventLogEntryType.Error);
                }
                catch (Exception ex)
                {
                    Logger.WriteLog("StopWindowsServices : CheckAndStopWindowsServices :" + ex.Message, ex);

                    EventLog.WriteEntry("StopWindowsServices", $"Error in CheckAndStopWindowsServices for service {service.DisplayName}: {ex.Message}", EventLogEntryType.Error);
                }
            }
        }
    }
}
