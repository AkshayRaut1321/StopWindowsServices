using System.ServiceProcess;
using System.IO;
using System.Windows.Forms;
using System;

namespace StopWindowsServices
{
    static class Program
    {
        public static string CurrentApplicationPath = Application.StartupPath;
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        ///  
        [STAThread]
        static void Main(string[] args)
        {
            if (args != null)
            {
                if (args.Length > 0)
                {
                    if (args[0].ToLower() == "configuration")
                    {
                        Application.Run(new frmConfiguration());
                        return;
                    }
                    else
                    {
                        Application.Exit();
                        return;
                    }
                }
            }
            if (File.Exists(Application.StartupPath + "\\config.ini"))
            {
                IniFile.read(Application.StartupPath + "\\config.ini");
                ServiceBase[] ServicesToRun;
                StopWindowsServices service = new StopWindowsServices();
                ServicesToRun = new ServiceBase[]
                {
                    service
                };

                #region to test this service without installing

                #if (!DEBUG)
                    ServiceBase.Run(ServicesToRun);
                #else
                    service.RunServiceFirstTime();
                #endif

                #endregion
            }
            else
            {
                Application.Run(new frmConfiguration());
            }
        }
    }
}
