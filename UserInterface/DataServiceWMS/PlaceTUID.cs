using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UserInterface.DataServiceWMS
{
    public class PlaceTUID
    {
        public int TUID { get; set; }
        public string PlaceID { get; set; }
        public int DimensionClass { get; set; }
        public int Blocked { get; set; }

        public PlaceTUID()
        {
        }
    }
}
