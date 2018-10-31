using System;

namespace Database
{
    public partial class SimpleConveyorCommand : SimpleCommand
    {
        public override string ToString()
        {
            int r = 0;
            if (Reason.HasValue)
                r = (int)Reason.Value;

            switch (Task)
            {
                case EnumTask.Cancel:
                case EnumTask.Create:
                case EnumTask.Delete:
                    return String.Format("Command {0}: {1} P{2:d5} on {3} {4} ({5})", 
                                         ID, Task, Material, Source, Status, r);
                case EnumTask.Move:
                    return String.Format("Command {0}: {1} P{2:d5} from {3} to {4} {5} ({6})", 
                                         ID, Task, Material, Source, Target, Status, r);
                default:
                    return "Task unknown...";
            }
        }

        public override string ToSmallString()
        {
            return String.Format("{0}", Target);
        }

    }

}
