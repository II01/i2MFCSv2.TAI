using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UserInterface.DataServiceWMS
{
    public class TUSKUID
    {
        public string SKUID { get; set; }
        public double Qty { get; set; }
        public string Batch { get; set; }
        public DateTime ProdDate { get; set; }
        public DateTime ExpDate { get; set; }
        public string Description { get; set; }

        public TUSKUID()
        {
        }
    }
}
