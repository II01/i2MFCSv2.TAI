using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegrams;

namespace Warehouse.SimpleCommands
{

    public class SimpleConveyorCommand : SimpleCommand
    {
        private string _target;

        public string Target
        {
            get { return _target; }
            set
            {
                if (_target != value)
                {
                    _target = value;
                    RaisePropertyChanged("Target");
                }
            }
        }

        public SimpleConveyorCommand() : base()
        {
        }

        public SimpleConveyorCommand(SimpleConveyorCommand scc) : base()
        {
            this.ID = scc.ID;
            this.Command_ID = scc.Command_ID;
            this.Status = scc.Status;
            this.Time = scc.Time;
            this.Task = scc.Task;
            this.Material = scc.Material;
            this.Source = scc.Source;
            this.Target = scc.Target;
        }

        // source, target are assigned at this point yet
        public SimpleConveyorCommand(TelegramTransportTO tel) : base()
        {
            this.ID = tel.MFCS_ID;
            this.Material = tel.Palette.Barcode;
            switch (tel.Order)
            {
                case TelegramTransportTO.ORDER_MOVE: Task = EnumTask.Move; break;
                case TelegramTransportTO.ORDER_PALETTECREATE: Task = EnumTask.Create; break;
                case TelegramTransportTO.ORDER_PALETTEDELETE: Task = EnumTask.Delete; break;
                default: break;
            }
            //this.Source = tel.Source
            //this.Target = tel.Target;
            this.Time = DateTime.Now;
        }

        public override string ToString()
        {
            return String.Format("Command {0}:{1} {2} from {3} to {4}", ID, Task, Material, Source, Target);
        }

    }
}
