using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TransportUnits
{
    public class TransportIO : SimpleTransport
    {
        public LPosition CraneAdress { get; set; } // crane adress 
        public LPosition CraneLocation { get; set; } // aprox. location in warehouse
    }
}
