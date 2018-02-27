using System;

namespace Database
{
    public partial class SimpleCraneCommand : SimpleCommand
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
                case EnumTask.Delete: return String.Format("Command {0}: {1} {2} P{3:d9} from {4} {5} ({6})", 
                                                           ID, Unit, Task, Material, Source, Status, r);
                case EnumTask.Move: return String.Format("Command {0}: {1} {2} {3} {4} ({5})", 
                                                         ID, Unit, Task, Source, Status, r);
                case EnumTask.Drop:
                case EnumTask.Pick: return String.Format("Command {0}: {1} {2} P{3:d9} {4} {5} {6} ({7})", 
                                                         ID, Unit, Task, Material, Task == EnumTask.Drop ? "To" : "From", Source, Status, r);
            }
            return String.Format("Uknown task {0}", Task);
        }

        public override string ToSmallString()
        {
            if (Status < Database.SimpleCommand.EnumStatus.Canceled)
                return string.Format("{0}: {1}", Task.ToString()[0], Source);
            else
                return  "";
        }

    }

}
