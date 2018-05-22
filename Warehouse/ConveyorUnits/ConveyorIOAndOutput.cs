using System.Collections.Generic;
using System.Xml.Serialization;
using Warehouse.Common;
using Warehouse.Model;

namespace Warehouse.ConveyorUnits
{

    // define Conveyor with communication to Crane
    public class ConveyorIOAndOutput : Conveyor, IConveyorIO, IConveyorOutput
    {
        [XmlIgnore]
        public List<Route> FinalRouteCost { get; set; }

        public LPosition CraneAddress { get; set; } // crane adress 
    }
}
