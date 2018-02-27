using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Telegrams;
using Database;


namespace TransportUnits
{

    public class CraneException : Exception
    {
        public CraneException(string s) : base(s)
        { }
    }

    // the class handles also statuses of CraneCommands but does not create new instances. 
    public class Crane : BasicTransport
    {
        // command
        public List<string> InTransportName { get; set; }
        public List<string> OutTransportName { get; set; }

        [XmlIgnore]
        public List<TransportIO> InTransport { get; set; }
        [XmlIgnore]
        public List<TransportIO> OutTransport { get; set; }
        public List<short> Shelve { get; set; }

        [XmlIgnore]
        public TelegramCraneTO Command_Status { get; set; }
        [XmlIgnore]
        public TelegramCraneStatus PLC_Status { get; set; }

        [XmlIgnore]
        public SimpleCraneCommand Command { get; set; }
        [XmlIgnore]
        public SimpleCraneCommand BufferCommand { get; set; }

        [XmlIgnore]
        public int CommandCancelFault { get; set; }
        [XmlIgnore]
        public string CommandCancelText { get; set; }
        [XmlIgnore]
        public string CommandCancelByText { get; set; }

        [XmlIgnore]
        public bool AutomaticOld { get; set; }

        [XmlIgnore]
        public SimpleCraneCommand FastCommand { get; set; }


        public Crane() : base()
        {
        }

        public override void Initialize()
        {
            Communicator.DispatchRcv.Add(new DispatchNode { DispatchTerm = (t) => t.Sender == PLC_ID && (t is TelegramCraneStatus), DispatchTo = OnTelegramCraneStatus });
            Communicator.DispatchRcv.Add(new DispatchNode { DispatchTerm = (t) => t.Sender == PLC_ID && (t is TelegramCraneTO), DispatchTo = OnTelegramCraneTO });
            Communicator.Startup.Add(new Action(Startup));
            InitializePlace();
        }


        public override bool Automatic()
        {
            if (PLC_Status == null)
                return false;
            else
                return PLC_Status.Status[TelegramCraneStatus.STATUS_AUTOMATIC];
        }

        public void AutomaticOn()
        {
            Communicator.AddSendTelegram(
                new Telegrams.TelegramCraneStatus
                {
                    MFCS_ID = 0,
                    Sender = PLC_ID,
                    Order = TelegramCraneStatus.ORDER_AUTOMATICON
                });
        }

        public void CancelCommand()
        {
            if (Command != null)
                Communicator.AddSendTelegram(
                    new TelegramCraneTO
                    {
                        MFCS_ID = 0,
                        Sender = PLC_ID,
                        Order = TelegramCraneTO.ORDER_CANCEL
                    });
        }

        public void AutomaticOff()
        {
            Communicator.AddSendTelegram(
                new Telegrams.TelegramCraneStatus
                {
                    MFCS_ID = 0,
                    Sender = PLC_ID,
                    Order = TelegramCraneStatus.ORDER_AUTOMATICOFF
                });
        }

        private void Startup()
        {
            Communicator.AddSendTelegram(
                new Telegrams.TelegramCraneStatus
                {
                    Sender = PLC_ID,
                    MFCS_ID = 0,
                    Order = TelegramCraneStatus.ORDER_SYSTEMQUERY
                });
        }

        protected TransportIO FindInTransport(string name)
        {
            try
            {
                return InTransport.Find(prop => prop.Name == name) as TransportIO;
            }
            catch
            {
                throw new CraneException(String.Format("{0} does not find input transport {1}", Name, name));
            }
        }

        protected TransportIO FindOutTransport(string name)
        {
            try
            {
                return OutTransport.Find(prop => prop.Name == name) as TransportIO;
            }
            catch
            {
                throw new CraneException(String.Format("{0} does not find output transport {1}", Name, name));
            }
        }

        protected TransportIO FindInOutTransport(string name)
        {
            try
            {
                if (InTransport.Exists(prop => prop.Name == name))
                    return InTransport.Find(prop => prop.Name == name) as TransportIO;
                if (OutTransport.Exists(prop => prop.Name == name))
                    return OutTransport.Find(prop => prop.Name == name) as TransportIO;
                throw new Exception();
            }
            catch
            {
                throw new CraneException(String.Format("{0} does not find input/output transport {1}", Name, name));
            }
        }

        private void AddCommandToPLC(SimpleCraneCommand cmd)
        {
            LPosition pos = LPosition.FromString(cmd.Source);
            if (!pos.IsWarehouse())
            {
                if (cmd.Task == EnumSimpleCraneCommandTask.Pick)
                    pos = FindInTransport(cmd.Source).CraneAdress;
                else if (cmd.Task == EnumSimpleCraneCommandTask.Drop)
                    pos = FindOutTransport(cmd.Source).CraneAdress;
                else if (cmd.Task == EnumSimpleCraneCommandTask.Goto)
                    pos = FindInOutTransport(cmd.Source).CraneAdress;
            }
            Communicator.AddSendTelegram(
                new Telegrams.TelegramCraneTO
                {
                    Sender = PLC_ID,
                    MFCS_ID = cmd.ID, 
                    Order = (short) cmd.Task,
                    Position = new Telegrams.Position { R = (short) pos.Shelve, X = (short) pos.Travel, Y = (short) pos.Height, Z = (short) pos.Depth },
                    Palette = new Telegrams.Palette { Barcode = Convert.ToUInt32(cmd.Material) }
                });
            using (var dc = new MFCSEntities())
            {
                dc.SimpleCraneCommands.Attach(cmd);
                cmd.Status = EnumSimpleCraneCommandStatus.Written;
                dc.SaveChanges();
            }
        }

        public void SaveCommands()
        {
            using (var dc = new MFCSEntities())
            {
                if (Command != null)
                {
                    dc.SimpleCraneCommands.Attach(Command);
                    dc.Entry<SimpleCraneCommand>(Command).State = System.Data.Entity.EntityState.Modified;
                }
                if (BufferCommand != null)
                {
                    dc.SimpleCraneCommands.Attach(BufferCommand);
                    dc.Entry<SimpleCraneCommand>(Command).State = System.Data.Entity.EntityState.Modified;
                }
                dc.SaveChanges();
            }
        }

        public void WriteCommandTOPlc()
        {
            if (PLC_Status != null && PLC_Status.Status[Telegrams.TelegramCraneStatus.STATUS_AUTOMATIC])
            {
                if (Command != null && Command.Status == EnumSimpleCraneCommandStatus.NotActive && PLC_Status.NumCommands == 0)
                {
                    AddCommandToPLC(Command);
                }
                if (BufferCommand != null && Command != null && BufferCommand.Status == EnumSimpleCraneCommandStatus.NotActive)
                {
                    AddCommandToPLC(BufferCommand);
                }
            }
        }

        public void OnTelegramCraneStatus(Telegram t)
        {
            PLC_Status = t as TelegramCraneStatus;
            // do some coding here
        }


        public void OnTelegramCraneTO(Telegram t)
        {
            TelegramCraneTO cmd = t as TelegramCraneTO;

            if (Command == null)
                throw new CraneException(String.Format("{0} command=null at new notification MFCS_ID={1}",Name,cmd.MFCS_ID));


            if (Command.ID != cmd.MFCS_ID)
                throw new CraneException(String.Format("{0} CommandID({1} != cmd.MFCS_ID{2} ", Name, Command.ID, cmd.MFCS_ID));

            Command_Status = cmd;

            if (Command.Status <= EnumSimpleCraneCommandStatus.InPlc)
                Command.Status = EnumSimpleCraneCommandStatus.InPlc;

            if (Command_Status.Confirmation == TelegramCraneTO.CONFIRMATION_OK)
            {
                if (Command.Status <= EnumSimpleCraneCommandStatus.PLCFinished)
                    Command.Status = EnumSimpleCraneCommandStatus.PLCFinished;
            }
            if (Command_Status.Confirmation == TelegramCraneTO.CONFIRMATION_CANCELBYMFCS ||
                Command_Status.Confirmation == TelegramCraneTO.CONFIRMATION_CANCELBYWAREHOUSE)
            {
                CommandCancelFault = Command_Status.Fault;
                CommandCancelText = Command_Status.Fault.ToString();
                CommandCancelByText = Command_Status.Confirmation.ToString();
                Command.Status = EnumSimpleCraneCommandStatus.PLCCanceled;
                if (BufferCommand != null)
                    BufferCommand.Status = EnumSimpleCraneCommandStatus.PLCCanceled;
            }
        }
    }
}
