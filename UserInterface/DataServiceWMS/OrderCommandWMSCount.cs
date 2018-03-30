using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UserInterface.DataServiceWMS
{
    public class OrderCommandWMSCount
    {
        public int? ERPID { get; set; }
        public int OrderID { get; set; }
        public string Destination { get; set; }
        public DateTime ReleaseTime { get; set; }
        public int Status { get; set; }
        public int CountActive { get; set; }
        public int CountCanceled { get; set; }
        public int CountFinihsed { get; set; }
        public int CountAll { get; set; }

        public OrderCommandWMSCount()
        {
        }
    }
}
