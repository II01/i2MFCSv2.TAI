using System;
using Warehouse.Model;
using Warehouse.WMS;
using WcfService;

namespace WcfHostMFCS
{
    class Program
    {
        static void Main(string[] args)
        {
            using (BasicWarehouse Warehouse = BasicWarehouse.Deserialize(System.Configuration.ConfigurationManager.AppSettings["xmlconfig"]))
            {
                Warehouse.DBService = new Warehouse.DataService.DBService(Warehouse);
                // Warehouse.WMS = new BasicWMS { WMSSimulation = bool.Parse(System.Configuration.ConfigurationManager.AppSettings["WMSSimulation"]) };
                Warehouse.Initialize();
                Warehouse.BuildRoutes(false);
                // service host is initialized in this module
                Warehouse.WCFHost = new Warehouse.WCF.WCFHost();
                Warehouse.WCFHost.Start(Warehouse, typeof(MFCSService));
                Warehouse.WCFHost.Start(Warehouse, typeof(WMS));
                Warehouse.StartCommunication();

                Warehouse.AddEvent(Database.Event.EnumSeverity.Event, Database.Event.EnumType.Program, "Program started");

                Console.WriteLine("WcfHostMFCS\n-----------");
                Console.WriteLine("Press SPACE and ENTER to stop ...");
                char ch='k';
                do
                {
                    try
                    {
                        ch = Console.ReadKey().KeyChar;
                        Warehouse.AddEvent(Database.Event.EnumSeverity.Event, Database.Event.EnumType.Program, "Key was pressed in MFCS server");
                    }
                    catch { }
                }
                while (ch != ' ');
                Console.ReadLine();
                Console.WriteLine("... stopped.");

            } // dispose call here
        }
    }
}
