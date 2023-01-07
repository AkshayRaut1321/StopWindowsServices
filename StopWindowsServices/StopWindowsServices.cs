using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.ServiceProcess;
using System.Timers;
using System.Windows.Forms;
using Timer = System.Timers.Timer;

namespace StopWindowsServices
{
    public partial class StopWindowsServices : ServiceBase
    {
        private Timer _startupTimer;
        static object acquireStop = new object();
        public StopWindowsServices()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            try
            {
                try
                {
                    RunService();
                    _startupTimer = new Timer(IniFile.WS_TIMEOUT);
                    _startupTimer.Elapsed += _startupTimer_Elapsed;
                    _startupTimer.Start();
                }
                catch (Exception ex)
                {
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

        internal void RunService()
        {
            try
            {
                lock (acquireStop)
                {
                    var serviceList = IniFile.SERVICES_LIST?.ToLower().Split(',')?.ToList();
                    CheckAndStopWindowsServices(serviceList);
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog("StopWindowsServices : RunService : " + ex.Message, ex);
            }
        }

        protected override void OnStop()
        {
            _startupTimer.Stop();
        }

        void _startupTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            try
            {
                _startupTimer.Stop();
                var serviceList = IniFile.SERVICES_LIST?.ToLower().Split(',')?.ToList();
                CheckAndStopWindowsServices(serviceList);
            }
            catch (Exception ex)
            {
                Logger.WriteLog("StopWindowsServices : _startupTimer_Elapsed : " + ex.Message, ex);
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
                    if (listServiceNames.Contains(service.DisplayName.ToLower()))
                    {
                        if (service.Status != ServiceControllerStatus.Stopped)
                        {
                            service.Stop();
                            service.WaitForStatus(ServiceControllerStatus.Stopped);
                            //ServiceHelper.ChangeStartMode(service, ServiceStartMode.Disabled);
                        }
                    }
                }
                catch (ExternalException ex)
                {
                    Logger.WriteLog("StopWindowsServices : CheckAndStopWindowsServices :" + ex.Message, ex);
                }
                catch (Exception ex)
                {
                    Logger.WriteLog("StopWindowsServices : CheckAndStopWindowsServices :" + ex.Message, ex);
                }
            }
        }
    }
}
