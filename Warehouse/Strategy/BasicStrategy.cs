using System.Xml.Serialization;
using Warehouse.Model;

namespace Warehouse.Strategy
{
    public abstract class BasicStrategy
    {
        [XmlAttribute]
        public string Name { get; set; }

        [XmlIgnore]
        public BasicWarehouse Warehouse { get; set; }

        public abstract void Strategy();
        public abstract void Initialize(BasicWarehouse w);

        public abstract void Refresh();
    }
}
