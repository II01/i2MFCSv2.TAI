using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegrams;

namespace TransportUnits
{

    public class LPositionException : Exception
    {
        public LPositionException(string s) : base(s)
        { }
    }

    public class LPosition
    {
        private string Name { get; set; }
        public int Shelve { get; set; }
        public int Travel { get; set; }
        public int Height { get; set; }
        public int Depth { get; set; }

        public override string ToString()
        {
            if (Shelve > 0)
                return String.Format("R:{0}:{1}:{2}:{3}", Shelve, Travel, Height, Depth);
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
                if (s[0] == 'R')
                    return new LPosition { Name = s, Shelve = Convert.ToInt32(s[0]), Travel = Convert.ToInt32(s[1]), Height = Convert.ToInt32(s[2]), Depth = Convert.ToInt32(s[3]) };
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
        public static LPosition Transport(int t)
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
