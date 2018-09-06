using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UserInterface.DataServiceWMS
{
    public class OrderReduction
    {
        public int? ERPID { get; set; }
        public int? ERPIDStokbar { get; set; }
        public int OrderID { get; set; }
        public int SubOrderID { get; set; }
        public int SubOrderERPID { get; set; }
        public int WMSID { get; set; }
        public string SubOrderName { get; set; }
        public string Destination { get; set; }
        public string SKUID { get; set; }
        public string SKUBatch { get; set; }
        public double SKUQty { get; set; }
        public int TUID { get; set; }
        public string BoxID { get; set; }


        public DateTime ReleaseTime { get; set; }
        public DateTime LastChange { get; set; }
        public int Status { get; set; }
        public int CountAll { get; set; }
        public int CountActive { get; set; }
        public int CountMoveDone { get; set; }
        public int CountFinished { get; set; }

        public OrderReduction()
        {
        }
    }
}
