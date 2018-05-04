using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UserInterfaceGravityPanel.Model
{
    public class OrderCount
    {
        public int Status { get; set; }
        public int All { get; set; }
        public int Active { get; set; }
        public int Done { get; set; }
        public int Finished { get; set; }
    }
    public class LaneData
    {
        public int LaneID { get; set; }
        public int Count { get; set; }
        public int? FirstTUID { get; set; }
        public SKUData SKU { get; set; }
        public SubOrderData Suborder { get; set; }
    }

    public class SKUData
    {
        public string SKU { get; set; }
        public string SKUBatch { get; set; }
        public double SKUQty { get; set; }
    }
    public class SubOrderData
    {
        public int SubOrderID { get; set; }
        public int SubOrderERPID { get; set; }
        public string SubOrderName { get; set; }
    }
}
