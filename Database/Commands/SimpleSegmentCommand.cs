using System;

namespace Database
{
    public partial class SimpleSegmentCommand : SimpleCommand
    {
        public enum SegmentTask {Info = 0, Reset, AutoOn, AutoOff, SetClock };
        public override string ToString()
        {
            return String.Format("Command {0}:{1} {2}", ID, Segment, Task);
        }

        public override string ToSmallString()
        {
            return String.Format("{0} :{1} {2}", ID, Segment, Task);
        }

    }
}
