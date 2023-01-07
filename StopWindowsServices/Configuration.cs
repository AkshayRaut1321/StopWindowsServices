using System;
using System.Text;
using System.Windows.Forms;
using System.Xml;
using System.IO;

namespace StopWindowsServices
{
    public partial class frmConfiguration : Form
    {
        public static XmlDocument xDoc = new XmlDocument();

        public frmConfiguration()
        {
            InitializeComponent();
            ReadConfiguration();
        }

        private void frmConfiguration_Load(object sender, EventArgs e)
        {
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            try
            {
                if (CheckValidation())
                {
                    WriteConfiguration();
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog("btnSave_Click() :" + ex.Message, ex);
            }
        }

        private void ReadConfiguration()
        {
            try
            {
                if (File.Exists(Application.StartupPath + "\\config.ini"))
                {
                    xDoc = IniFile.read(Application.StartupPath + "\\config.ini");
                    //txtWSURL.Text = IniFile.ServiceList;
                    txtServicesList.Text = IniFile.SERVICES_LIST.ToString();
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog("ReadConfiguration() :" + ex.Message, ex);
            }
        }
        private void WriteConfiguration()
        {
            try
            {
                string strPlantType = string.Empty;
                XmlDocument doc = new XmlDocument();
                StringBuilder sb = new StringBuilder();
                sb.Append("<CONFIGURATION>");
                sb.Append("<SERVICES_LIST>" + txtServicesList.Text + "</SERVICES_LIST>");
                sb.Append("</CONFIGURATION>");
                if (sb == null)
                    Logger.WriteLog("SB is empty");
                doc.InnerXml = sb.ToString();
                doc.Save((Application.StartupPath + "/config.ini"));
                MessageBox.Show("configuration saved successfully.", "Gateway configuration", MessageBoxButtons.OK, MessageBoxIcon.Information);
                this.Close();
            }
            catch (Exception ex)
            {
                Logger.WriteLog("WriteConfiguration() :" + ex.Message, ex);
            }
        }
        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.Close();
        }
        public bool CheckValidation()
        {
            if (txtServicesList.Text.Trim().Length == 0)
            {
                MessageBox.Show("Please enter names of services you want to stop", "Configuration Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtServicesList.Focus();
                return false;
            }
            return true;
        }
    }
}
