using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.ServiceProcess;
using System.Timers;

namespace StopWindowsUpdate
{
    public partial class Service1 : ServiceBase
    {
        private Timer _startupTimer;
        static object acquireStop = new object();
        public Service1()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
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
                Logger.WriteLog("Service1 : OnStart :" + ex.ToString());
                _startupTimer.Stop();
                _startupTimer.Start();
            }
        }

        internal void RunService()
        {
            try
            {
                lock (acquireStop)
                {
                    var serviceList = IniFile.SERVICES_LIST?.ToLower().Split(',')?.ToList();
                    CheckAndStopWindowsUpdate(serviceList);
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog("Service1 : RunService : " + ex.ToString());
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
                CheckAndStopWindowsUpdate(serviceList);
            }
            catch (Exception ex)
            {
                Logger.WriteLog("Service1 : _startupTimer_Elapsed : " + ex.ToString());
            }
            finally
            {
                _startupTimer.Start();
            }
        }

        private void CheckAndStopWindowsUpdate(List<string> listServiceNames)
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
                            ServiceHelper.ChangeStartMode(service, ServiceStartMode.Disabled);
                        }
                    }
                }
                catch (ExternalException ex)
                {
                    Logger.WriteLog("Service1 : CheckAndStopWindowsUpdate :" + ex.ToString());
                }
                catch (Exception ex)
                {
                    Logger.WriteLog("Service1 : CheckAndStopWindowsUpdate :" + ex.ToString());
                }
            }
        }
    }
}
