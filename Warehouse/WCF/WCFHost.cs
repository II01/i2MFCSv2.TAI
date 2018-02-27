using System;
using System.Collections.Generic;
using System.ServiceModel;
using Warehouse.Model;
using System.Runtime.Serialization;

namespace Warehouse.WCF
{

    [Serializable]
    public class WCFException : Exception
    {
        public WCFException() : base() { }
        public WCFException(string s) : base(s) { }
        public WCFException(SerializationInfo info, StreamingContext context) : base(info, context) { }

    }

    public class WarehouseServiceHost : ServiceHost 
    {
        public BasicWarehouse Warehouse { get; set; }
        public WarehouseServiceHost(Type type) : base(type)
        { }
    }

    public class WCFHost
    {
        private List<WarehouseServiceHost> WarehouseServiceHostList { get; set; }


        public WCFHost()
        {
            WarehouseServiceHostList = new List<WarehouseServiceHost>(); 
        }

        public void Start(BasicWarehouse w, Type type)
        {
            try
            {
                var sh = new WarehouseServiceHost(type);
                sh.Warehouse = w;
                WarehouseServiceHostList.Add(sh);
                sh.Open();
            }
            catch (Exception ex)
            {
                w.AddEvent(Database.Event.EnumSeverity.Error, Database.Event.EnumType.Exception, ex.Message);
                throw new WCFException(String.Format("WCFHost.Start failed. Type : {0}", type.ToString()));
            }
        }

        public void Stop()
        {
            foreach (var sh in WarehouseServiceHostList)
                try
                {
                    sh.Close();
                }
                catch (Exception)
                {
                }
        }
    }
}
