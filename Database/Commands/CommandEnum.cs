using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Database
{
    public partial class Command
    {
        public enum EnumCommandStatus : int
        {
            NotActive = 0,
            Active = 1,
            Canceled = 2,
            Finished = 3
        }

        public enum EnumCommandReason: int
        {
            OK = 0,
            PLC = 1, 
            MFCS = 2,
            Operator = 3,
            LocationEmpty = 4,
            LocationFull = 5
        }
        public enum EnumCommandTask : int 
        {
            Move = 0,
            CreateMaterial = 1,
            DeleteMaterial = 2,
            InfoMaterial = 3,
            InfoSlot = 4,
            SegmentInfo = 10,
            SegmentOn = 11,
            SegmentOff = 12,
            SegmentReset = 13,
            SegmentHome = 14,
            CancelCommand = 100,
            InfoCommand = 101
        }
    }
}
