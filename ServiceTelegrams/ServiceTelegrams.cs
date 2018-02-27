using System.ServiceProcess;
using Warehouse.Model;

namespace ServiceTelegrams
{
    public partial class ServiceTelegrams : ServiceBase
    {

        private BasicWarehouse Warehouse { get; set; }

        public ServiceTelegrams()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            System.IO.Directory.SetCurrentDirectory(System.AppDomain.CurrentDomain.BaseDirectory);
            BasicWarehouse dummy = new BasicWarehouse();
            Warehouse = dummy.Deserialize(System.Configuration.ConfigurationManager.AppSettings["commconfig"]);

            Warehouse.Initialize();
            Warehouse.BuildRoutes(false);
            Warehouse.StartCommunication();
        }

        protected override void OnStop()
        {
            Warehouse?.Dispose();
        }
    }
}
