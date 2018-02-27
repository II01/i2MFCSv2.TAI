namespace ServiceMFCS
{
    partial class ProjectInstaller
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Component Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.serviceMFCSProcessInstaller = new System.ServiceProcess.ServiceProcessInstaller();
            this.serviceMFCSInstaller = new System.ServiceProcess.ServiceInstaller();
            // 
            // serviceMFCSProcessInstaller
            // 
            this.serviceMFCSProcessInstaller.Account = System.ServiceProcess.ServiceAccount.LocalSystem;
            this.serviceMFCSProcessInstaller.Password = null;
            this.serviceMFCSProcessInstaller.Username = null;
            // 
            // serviceMFCSInstaller
            // 
            this.serviceMFCSInstaller.Description = "I2MFCS core service";
            this.serviceMFCSInstaller.DisplayName = "i2MFCS CORE";
            this.serviceMFCSInstaller.ServiceName = "i2MFCS CORE";
            // 
            // ProjectInstaller
            // 
            this.Installers.AddRange(new System.Configuration.Install.Installer[] {
            this.serviceMFCSProcessInstaller,
            this.serviceMFCSInstaller});

        }

        #endregion

        private System.ServiceProcess.ServiceProcessInstaller serviceMFCSProcessInstaller;
        private System.ServiceProcess.ServiceInstaller serviceMFCSInstaller;
    }
}