﻿using System.ComponentModel;
using System.Configuration.Install;

namespace StopWindowsServices
{
    [RunInstaller(true)]
    public partial class ProjectInstaller : Installer
    {
        public ProjectInstaller()
        {
            InitializeComponent();
        }

        private void serviceProcessInstaller1_AfterInstall(object sender, InstallEventArgs e)
        {
        }

        private void serviceInstaller1_AfterInstall(object sender, InstallEventArgs e)
        {
        }

        private void serviceProcessInstaller1_BeforeInstall(object sender, InstallEventArgs e)
        {
        }
    }
}
