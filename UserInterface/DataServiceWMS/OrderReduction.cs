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
        public int OrderID { get; set; }
        public int SubOrderID { get; set; }
        public string SubOrderName { get; set; }
        public string Destination { get; set; }
        public DateTime ReleaseTime { get; set; }
        public DateTime LastChange { get; set; }
        public int Status { get; set; }
        public int StatusMin { get; set; }
        public int StatusMax { get; set; }
        public int CountAll { get; set; }
        public int CountActive { get; set; }
        public int CountMoreThanActive { get; set; }

        public OrderReduction()
        {
        }
    }
}
