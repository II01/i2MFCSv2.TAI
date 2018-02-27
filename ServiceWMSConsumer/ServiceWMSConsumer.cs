using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceModel;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using WcfService;

namespace ServiceWMSConsumer
{
    public partial class ServiceWMSConsumer : ServiceBase
    {
        ServiceHost sh;
        public ServiceWMSConsumer()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            sh = new ServiceHost(typeof(WMSConsumer));
            sh.Open();
        }

        protected override void OnStop()
        {
        }
    }
}
