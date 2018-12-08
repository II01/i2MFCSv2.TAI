using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UserInterface.DataServiceWMS
{
    public class BoxTUID
    {
        public string BoxID{ get; set; }
        public string SKUID { get; set; }
        public string Batch { get; set; }
        public int? TUID { get; set; }

        public BoxTUID()
        {
        }
    }
}
