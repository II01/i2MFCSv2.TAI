namespace ServiceTelegrams
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
            this.serviceTelegramsProcessInstaller = new System.ServiceProcess.ServiceProcessInstaller();
            this.serviceTelegramsInstaller = new System.ServiceProcess.ServiceInstaller();
            // 
            // serviceTelegramsProcessInstaller
            // 
            this.serviceTelegramsProcessInstaller.Account = System.ServiceProcess.ServiceAccount.LocalSystem;
            this.serviceTelegramsProcessInstaller.Password = null;
            this.serviceTelegramsProcessInstaller.Username = null;
            // 
            // serviceTelegramsInstaller
            // 
            this.serviceTelegramsInstaller.Description = "i2MFCS Telegrams service to collect from PLC";
            this.serviceTelegramsInstaller.DisplayName = "i2MFCS Telegrams";
            this.serviceTelegramsInstaller.ServiceName = "i2MFCS Telegrams";
            this.serviceTelegramsInstaller.StartType = System.ServiceProcess.ServiceStartMode.Automatic;
            // 
            // ProjectInstaller
            // 
            this.Installers.AddRange(new System.Configuration.Install.Installer[] {
            this.serviceTelegramsProcessInstaller,
            this.serviceTelegramsInstaller});

        }

        #endregion

        private System.ServiceProcess.ServiceProcessInstaller serviceTelegramsProcessInstaller;
        private System.ServiceProcess.ServiceInstaller serviceTelegramsInstaller;
    }
}