using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Telegrams;
using Database;
using Warehouse;

namespace TransportUnits
{
    public abstract class BasicTransport
    {
        [XmlIgnore]
        public PlaceID PlaceID;
        [XmlIgnore]
        public Place Place;
        [XmlIgnore]
        public BasicWarehouse Warehouse;


        public string Name { get; set; }
        public Int16 PLC_ID { get; set; }

        [XmlIgnore]
        public Communication Communicator { get; set; }

        public string CommunicatorName { get; set; }

        protected void InitializePlace()
        {
            using (var dc = new MFCSEntities())
            {
                PlaceID = dc.PlaceIDs.Find(Name);
                Place = dc.Places.Find(Name);
                if (PlaceID == null)
                    throw new SimpleTransportException(String.Format("For {0} is PlaceID not defined.", Name));
            }
        }

        public abstract void Initialize();
        public abstract bool Automatic();
        public virtual bool Online()
        {
            return Communicator.Online();
        }
    }
}
