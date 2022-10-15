using System;
using System.IO;
using System.Windows.Forms;

namespace StopWindowsUpdate
{
    public class Logger
    {
        static object writeErrorLogLock = new object();
        public static void WriteLog(string errorMessage)
        {
            lock (writeErrorLogLock)
            {
                string filepath = Application.StartupPath + @"\ErrorLog" + DateTime.Now.ToString("yyyy-MM-dd");

                if (!Directory.Exists(filepath))
                {
                    Directory.CreateDirectory(filepath);
                }
                string t = DateTime.Today.Date.ToString();
                StreamWriter streamWriter = new StreamWriter(filepath + @"\" + DateTime.Now.ToString("yyyy-MM-dd") + ".txt", true);
                streamWriter.WriteLine(DateTime.Now.ToString() + "\t" + errorMessage);
                streamWriter.Close();
            }
        }
    }
}
