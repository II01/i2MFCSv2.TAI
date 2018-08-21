using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Warehouse.Common;
using Warehouse.Model;

namespace Warehouse.ConveyorUnits
{
    public class ConveyorJunctionAndIOAndOutput : ConveyorJunction, IConveyorIO, IConveyorOutput
    {
        [XmlIgnore]
        public List<Route> FinalRouteCost { get; set; }

        public LPosition CraneAddress { get; set; }
    }
}
