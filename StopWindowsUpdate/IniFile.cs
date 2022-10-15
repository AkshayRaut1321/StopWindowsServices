using System;
using System.IO;
using System.Text;
using System.Xml;

namespace StopWindowsUpdate
{
    public class IniFile
    {
        private string fileName;
        public static string defaultServicesList = "wuauserv,BITS,WSService,DoSvc";

        /// <summary>
        /// Creates a new <see cref="IniFile"/> instance.
        /// </summary>
        /// <param name="fileName">Name of the INI file.</param>
        public IniFile(string fileName)
        {
            if (!File.Exists(fileName))
            {
                throw new FileNotFoundException(fileName + " does not exist", fileName);
            }
            this.fileName = fileName;
        }

        public static XmlDocument xDoc = new XmlDocument();
        public static XmlDocument read(string path)
        {

            StreamReader reader = new StreamReader(path);
            StringBuilder sb = new StringBuilder();
            string r = reader.ReadLine();
            while (r != null)
            {
                sb.Append(r);
                sb.Append(Environment.NewLine);
                r = reader.ReadLine();
            }
            reader.Close();
            XmlDocument doc = new XmlDocument();
            doc.InnerXml = sb.ToString();
            xDoc.LoadXml(sb.ToString());
            return doc;
        }

        private static string _serviceList = string.Empty;
        public static string SERVICES_LIST
        {
            get
            {
                var servicesList = xDoc["CONFIGURATION"]["SERVICES_LIST"].InnerText;
                if (servicesList == null || servicesList.Trim() == "")
                    return defaultServicesList;
                return servicesList;
            }
            set { _serviceList = value; }
        }

        private static int xWS_TIMEOUT = 60;
        public static int WS_TIMEOUT
        {
            get
            {
                return Convert.ToInt32(xDoc["CONFIGURATION"]["WS_TIMEOUT"].InnerText);
            }
            set { xWS_TIMEOUT = value; }
        }
    }
}
