using System;
using Warehouse.Model;

namespace WCFHostTelegrams
{
    class Program
    {
        static void Main(string[] args)
        {
            BasicWarehouse dummy = new BasicWarehouse();


            using (BasicWarehouse Warehouse = dummy.Deserialize(System.Configuration.ConfigurationManager.AppSettings["commconfig"]))
            {

                Warehouse.Initialize();
                // service host is initialized in this module
                // Warehouse.WCFHost = new Warehouse.WCF.WCFHost();
                // Warehouse.WCFHost.Start(Warehouse, typeof(MFCService));
                Warehouse.StartCommunication();

                Warehouse.AddEvent(Database.Event.EnumSeverity.Event, Database.Event.EnumType.Program, "Program started");


                Console.WriteLine("Press ENTER key to stop...");
                Console.ReadLine();

            } // disposed call here 
        }
    }
}
