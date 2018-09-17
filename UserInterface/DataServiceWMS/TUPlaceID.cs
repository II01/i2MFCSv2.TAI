using DatabaseWMS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UserInterface.Services;

namespace UserInterface.DataServiceWMS
{
    public class TUPlaceID
    {
        public int TUID { get; set; }
        public string BoxID { get; set; }
        public string SKUID { get; set; }
        public double Qty { get; set; }
        public string Batch { get; set; }
        public DateTime ProdDate { get; set; }
        public DateTime ExpDate { get; set; }
        public string Location { get; set; }
        public EnumBlockedWMS Status { get; set; }

        public TUPlaceID()
        {
        }
    }
}
