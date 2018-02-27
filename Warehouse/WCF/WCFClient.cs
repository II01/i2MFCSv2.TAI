using System;
using Warehouse.Model;

namespace Warehouse.WCF
{

    public class WCFBasicClient : IDisposable
    {
        public BasicWarehouse Warehouse { get; set; }

        public virtual void Dispose()
        {
        }

        public virtual void Initialize(BasicWarehouse w)
        {
            Warehouse = w;
        }
    }




}
