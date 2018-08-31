﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UserInterface.DataServiceWMS
{
    public class PlaceDiff
    {
        public int TUID { get; set; }
        public string PlaceWMS { get; set; }
        public string PlaceMFCS { get; set; }

        public int? DimensionWMS { get; set; }
        public int? DimensionMFCS { get; set; }

        public DateTime? TimeWMS { get; set; }
        public DateTime? TimeMFCS { get; set; }

        public PlaceDiff()
        {
        }
    }
}
