using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UserInterface.DataServiceWMS
{
    public class CommandWMSOrder
    {
        public int ID { get; set; }
        public int Order_ID { get; set; }
        public int TU_ID { get; set; }
        public string Source { get; set; }
        public string Target{ get; set; }
        public int Status { get; set; }
        public int? OrderERPID { get; set; }
        public int OrderOrderID { get; set; }
        public int OrderSubOrderID { get; set; }
        public string OrderSubOrderName { get; set; }
        public string OrderSKUID { get; set; }
        public string OrderSKUBatch { get; set; }

        public CommandWMSOrder()
        {
        }
    }
}
