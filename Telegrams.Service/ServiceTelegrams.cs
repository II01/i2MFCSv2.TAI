using SimpleLog;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceModel;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using Telegrams.Communication;
using Telegrams.WcfService;

namespace Telegrams.Service
{
    public partial class ServiceTelegrams : ServiceBase
    {
        protected ServiceHost _hostRcv = null;
        protected ServiceHost _hostSend = null;

        public ServiceTelegrams()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            try
            {
                System.IO.Directory.SetCurrentDirectory(System.AppDomain.CurrentDomain.BaseDirectory);


                _hostRcv = new ServiceHost(typeof(WCF_RcvTelProxy));
                _hostRcv.Open();

                _hostSend = new ServiceHost(typeof(WCF_SendTelProxy));
                _hostSend.Open();

                Log.AddLog(Log.Severity.EVENT, "Service telegrams", "Started");
            }
            catch( Exception ex)
            {
                Log.AddLog(Log.Severity.EXCEPTION, "Service telegrams", ex.Message);
            }
        }

        protected override void OnStop()
        {
            try
            {
                _hostRcv?.Close();
                _hostSend?.Close();
            }
            catch(Exception ex)
            {
                Log.AddLog(Log.Severity.EXCEPTION, "Service telegrams", ex.Message);
            }
        }
    }
}
