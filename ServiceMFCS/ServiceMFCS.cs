using System.ServiceProcess;
using Warehouse.Model;
using Warehouse.WMS;
using WcfService;

namespace ServiceMFCS
{
    public partial class ServiceMFCS : ServiceBase
    {
        private BasicWarehouse Warehouse { get; set; }

        public ServiceMFCS()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            // System.Diagnostics.Debugger.Launch();
            System.IO.Directory.SetCurrentDirectory(System.AppDomain.CurrentDomain.BaseDirectory);
            Warehouse = BasicWarehouse.Deserialize(System.Configuration.ConfigurationManager.AppSettings["xmlconfig"]);
            Warehouse.DBService = new Warehouse.DataService.DBService(Warehouse);
            Warehouse.WMS = new BasicWMS{WMSSimulation = bool.Parse(System.Configuration.ConfigurationManager.AppSettings["WMSSimulation"])};
            Warehouse.Initialize();
            Warehouse.BuildRoutes(false);
            Warehouse.WCFHost = new Warehouse.WCF.WCFHost();
            Warehouse.WCFHost.Start(Warehouse, typeof(MFCSService));
            Warehouse.WCFHost.Start(Warehouse, typeof(WMS));
            Warehouse.StartCommunication();
            Warehouse.AddEvent(Database.Event.EnumSeverity.Event, Database.Event.EnumType.Program, "Program started");
        }

        protected override void OnStop()
        {
            Warehouse?.Dispose();
        }
    }
}
