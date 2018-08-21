using System;
using System.Linq;
using System.Xml.Serialization;
using Telegrams;

namespace Warehouse.Common
{

    [Serializable]
    public class LPositionException : Exception
    {
        public LPositionException(string s) : base(s)
        { }
    }

    public class LPosition
    {
        private string Name { get; set; }
        [XmlAttribute]
        public int Shelve { get; set; }
        [XmlAttribute]
        public int Travel { get; set; }
        [XmlAttribute]
        public int Height { get; set; }
        [XmlAttribute]
        public int Depth { get; set; }


        public LPosition()
        { }

        public LPosition( Position p)
        {
            Shelve = p.R;
            Travel = p.X;
            Height = p.Y;
            Depth = p.Z;
        }

        public bool IsEqual(LPosition p)
        {
            if (IsWarehouse() ^ p.IsWarehouse())
                return false;
            if (IsWarehouse())
                return Name == p.Name;
            return ((Shelve == p.Shelve) && (Travel == p.Travel) && (Height == p.Height) && (Depth == p.Depth));             
        }

        public override string ToString()
        {
            if (Shelve > 0)
                return String.Format("W:{0:d2}:{1:d2}:{2:d2}:{3:d1}", Shelve, Travel, Height, Depth);  // project specific
            else
                return Name;
        }

        public static LPosition FromString(string s)
        {
            if (s == null)
                throw new LPositionException(String.Format("LPosition.FromString s is null"));
            try
            {
                string[] split = s.Split(':');
                if (split[0] == "W")
                {
                    if (split.Count() != 5)
                        throw new LPositionException(String.Format("Wrong LPosition format {0}", s));

                    return new LPosition { Name = s, Shelve = Convert.ToInt32(split[1]), Travel = Convert.ToInt32(split[2]), Height = Convert.ToInt32(split[3]), Depth = Convert.ToInt32(split[4]) };
                }
                else
                    return new LPosition { Name = s };
            }
            catch
            {
                throw new LPositionException(String.Format("LPosition.FromString with parameter {0} failed.", s));
            }
        }

        public bool IsWarehouse()
        {
            return Shelve > 0;
        }
        public static LPosition Conveyor(int t)
        {
            return new LPosition { Shelve = 0, Travel = t };
        }

    }

    public class Material
    {
        public bool Occupied { get; set; }
        public UInt32 Barcode { get; set; }
        public LPosition Target { get; set; }
        public LPosition Drain { get; set; }
        public TelegramTransportTO Command_Status { get; set; }
        public DateTime ArriveTime { get; set; }
    }
}
