using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;
using Telegrams;
using Database;
using Warehouse.Common;
using Warehouse.Model;
using MFCS.Communication;
using System.Runtime.Serialization;

namespace Warehouse.ConveyorUnits
{

    [Serializable]
    public class CraneException : Exception
    {
        public CraneException(string s) : base(s)
        {
            
        }
    }


    [DataContract]
    [KnownType(typeof(SimpleCraneCommand))]
    [KnownType(typeof(Position))]
    public class CraneInfo : ConveyorBasicInfo
    {
        [XmlIgnore]
        [DataMember]
        public Int16 NumCommands { get; set; }
        [DataMember]
        [XmlIgnore]
        public Int32 Command_ID { get; set; }
        [DataMember]
        [XmlIgnore]
        public Int32 BufferCommand_ID { get; set; }
        [DataMember]
        [XmlIgnore]
        public Position LPosition { get; set; }
        [DataMember]
        [XmlIgnore]
        public Position FPosition { get; set; }
        [DataMember]
        [XmlIgnore]
        public Int16 StateMachine { get; set; }
        [DataMember]
        [XmlIgnore]
        public Palette Palette { get; set; }
        [DataMember]
        [XmlIgnore]
        public string LastCommand { get; set; }
        [DataMember]
        [XmlIgnore]
        public string LastBufferCommand { get; set; }

    }


    // the class handles also statuses of CraneCommands but does not create new instances. 
    public class Crane : ConveyorBasic
    {

        public LPosition CraneAddress { get; set; }
        // command
        public List<short> Shelve { get; set; }
        public RouteDef OutRouteDef { get; set; }
        public CraneInfo CraneInfo { get; set; }
        public string HomePosition { get; set; }

        public bool FinalDevice { get; set; }

        [XmlIgnore]
        public LPosition LPHomePosition { get; set; }

        [XmlIgnore]
        public List<IConveyorIO> InConveyor { get; set; }
        [XmlIgnore]
        public List<IConveyorIO> OutConveyor { get; set; }

        [XmlIgnore]
        public TelegramCraneTO Command_Status { get; set; }
        [XmlIgnore]
        public TelegramCraneStatus PLC_Status { get; set; }

        [XmlIgnore]
        public SimpleCraneCommand Command { get; set; }
        [XmlIgnore]
        public SimpleCraneCommand BufferCommand { get; set; }

        [XmlIgnore]
        public SimpleCraneCommand FastCommand { get; set; }

        [XmlIgnore]
        public Action OnStrategy { get; set; }

        [XmlIgnore]
        public Random Random { get; private set; }

        public Crane()
        {
            Random = new Random();
        }

        public override void OnReceiveTelegram(Telegram t)
        {
            if (t is TelegramCraneStatus)
                OnTelegramCraneStatus(t);
            if (t is TelegramCraneTO)
                OnTelegramCraneTO(t);
        }

        private void AssignCommandsAfterFinish()
        {
            if (Command != null && Command.Status == SimpleCommand.EnumStatus.Canceled)
            {
                Command = null;
                BufferCommand = null;
            }
            if (Command != null && Command.Status == SimpleCommand.EnumStatus.Finished)
            {
                Command = BufferCommand;
                BufferCommand = null;
            }
            if (FastCommand != null && (FastCommand.Status >= SimpleCommand.EnumStatus.Canceled))
                FastCommand = null;
        }


        public override void DirectVMNotify()
        {
            if (CraneInfo != null && PLC_Status != null)
            {
                CraneInfo.Name = Name;
                CraneInfo.NumCommands = PLC_Status.NumCommands;
                CraneInfo.Command_ID = PLC_Status.Command_ID;
                CraneInfo.BufferCommand_ID = PLC_Status.BufferCommand_ID;
                CraneInfo.LPosition = PLC_Status.LPosition;
                CraneInfo.FPosition = PLC_Status.FPosition;
                CraneInfo.StateMachine = PLC_Status.StateMachine;
                CraneInfo.Palette = PLC_Status.Palette;
                CraneInfo.AlarmID = PLC_Status.AlarmID;
                CraneInfo.Fault = PLC_Status.Fault;
                CraneInfo.SetAlarms(PLC_Status.CurrentAlarms, Warehouse);
                CraneInfo.SetSensors(PLC_Status.Status);
                CraneInfo.Status = PLC_Status.Status;
                CraneInfo.LastCommand = Command != null ? Command.ToSmallString() : "";
                CraneInfo.LastBufferCommand = BufferCommand != null? BufferCommand.ToSmallString() : "";
                CraneInfo.Palette = Place != null ? new Palette { Barcode = (uint)Place.Material } : null;
                CraneInfo.Online = Online();
                CallNotifyVM(CraneInfo);
            }
        }

        public void OnTelegramCraneStatus(Telegram t)
        {
            try
            {
                if (t == null)
                    return;
                if (t != null)
                {
                    PLC_Status = t as TelegramCraneStatus;
                    FLocation = new FLocation
                    {
                        X = PLC_Status.FPosition.X * 10,
                        Y = PLC_Status.FPosition.Y * 10,
                        Z = 0
                    };
                }
                DirectVMNotify();
            }
            catch (Exception ex)
            {
                Warehouse.AddEvent(Event.EnumSeverity.Error, Event.EnumType.Exception, ex.Message);
                Warehouse.SteeringCommands.Run = false;
            }
        }

        public void OnTelegramCraneTO(Telegram t)
        {
            try
            {
                TelegramCraneTO tel = t as TelegramCraneTO;
                Command_Status = tel;

                // check if material exists
                if (tel.Order != TelegramCraneTO.ORDER_DELETEPALETTE)
                {
                    CreateOrUpdateMaterialID(tel.Palette);
                }

                // check if this is move command
                if (tel.Order < 50)
                {
                    if (tel.Confirmation == TelegramCraneTO.CONFIRMATION_INITIALNOTIFY)
                        InitialNotify(t, tel.Palette.Barcode);
                    else if (Command != null && Command.ID == tel.MFCS_ID)
                        WorkCommand(tel, Command, false);
                    else if (BufferCommand != null && BufferCommand.ID == tel.MFCS_ID)
                    {
                        // without confirmation of Command
                        Command = BufferCommand;
                        BufferCommand = null;
                        Warehouse.AddEvent(Event.EnumSeverity.Event, Event.EnumType.Program, String.Format("{0} goes from buffer directly in execution", Command.ToString()));
                        WorkCommand(tel, Command, false);
                    }
                    else
                    {
                        Command = Warehouse.DBService.FindSimpleCraneCommandByID(tel.MFCS_ID);
                        if (Command == null)
                            throw new CraneException(String.Format("Crane.OnTelegramCraneTO {0}, tel.MFCS ({1}) does not match any move active command.", Name, tel.MFCS_ID));
                        Warehouse.AddEvent(Event.EnumSeverity.Event, Event.EnumType.Program, String.Format("{0} goes from database directly in execution", Command.ToString()));
                        WorkCommand(tel, Command, false);
                    }
                }
                else
                {
                    // fast command
                    if (Command_Status.MFCS_ID != 0 && (FastCommand == null || FastCommand.ID != Command_Status.MFCS_ID))
                        FastCommand = Warehouse.DBService.FindSimpleCraneCommandByID(tel.MFCS_ID);

                    if ((FastCommand != null) && (FastCommand.ID == tel.MFCS_ID))
                        WorkCommand(tel, FastCommand, true);
                    else
                    {
                        Warehouse.SteeringCommands.Run = false;
                        throw new CraneException(String.Format("Crane.OnTelegramCraneTO {0}, tel.MFCS ({1}) does not match any fast active command.", Name, tel.MFCS_ID));
                    }
                }
                OnStrategy?.Invoke();
                if (Command != null && CraneInfo != null)
                    CraneInfo.LastCommand = Command.ToSmallString();
                if (BufferCommand != null && CraneInfo != null)
                    CraneInfo.LastBufferCommand = BufferCommand.ToString();
                DirectVMNotify();
            }
            catch (Exception e)
            {
                Warehouse.SteeringCommands.Run = false;
                Warehouse.AddEvent(Event.EnumSeverity.Error, Event.EnumType.Exception, e.Message);
                Warehouse.AddEvent(Event.EnumSeverity.Error, Event.EnumType.Exception, String.Format("{0} OnTelegramCraneTO failed.", Name));
            }
        }

        public override void InitialNotify(Telegram t, uint material)
        {
            try
            {
                TelegramCraneTO tel = t as TelegramCraneTO;
                base.InitialNotify(t, material);
                if (tel.Buffer_ID != 0)
                {
                    BufferCommand = Warehouse.DBService.FindSimpleCraneCommandByID(tel.Buffer_ID);
                    if (BufferCommand == null)
                        throw new CraneException(String.Format("Unknown BufferCommand ({0})", tel.Buffer_ID));
                    BufferCommand.Status = SimpleCommand.EnumStatus.InPlc;
                    Warehouse.DBService.UpdateSimpleCommand(BufferCommand);
                    Warehouse.AddEvent(Event.EnumSeverity.Event, Event.EnumType.Command, String.Format("{0} buffer command ({1}) in PLC", Name, BufferCommand != null ? BufferCommand.ToString() : "null"));
                }
                else if (BufferCommand != null)
                {
                    BufferCommand.Reason = SimpleCommand.EnumReason.MFCS;
                    BufferCommand.Status = SimpleCommand.EnumStatus.Canceled;
                    Warehouse.DBService.UpdateSimpleCommand(BufferCommand);
                    OnSimpleCommandFinish(BufferCommand);
                    BufferCommand = null;
                    Warehouse.AddEvent(Event.EnumSeverity.Event, Event.EnumType.Command, String.Format("{0} buffer command ({1}) not in PLC.", Name, BufferCommand != null ? BufferCommand.ToString() : "null"));
                }
                if (tel.MFCS_ID != 0)
                {
                    Command = Warehouse.DBService.FindSimpleCraneCommandByID(tel.MFCS_ID);
                    if (Command == null)
                        throw new CraneException(String.Format("Unknown Command ({0})", tel.Buffer_ID));
                    Command.Status = SimpleCommand.EnumStatus.InPlc;
                    Warehouse.DBService.UpdateSimpleCommand(Command);
                    Warehouse.AddEvent(Event.EnumSeverity.Event, Event.EnumType.Command, String.Format("{0} Command ({1}) in PLC", Name, Command != null ? Command.ToString() : "null"));
                }
                else if (Command != null)
                {
                    Command.Reason = SimpleCommand.EnumReason.MFCS;
                    Command.Status = SimpleCommand.EnumStatus.Canceled;
                    Warehouse.DBService.UpdateSimpleCommand(Command);
                    OnSimpleCommandFinish(Command);
                    Warehouse.AddEvent(Event.EnumSeverity.Event, Event.EnumType.Command, String.Format("{0} Command ({1}) not in PLC", Name, Command != null ? Command.ToString() : "null"));
                    Command = null;
                }
            }
            catch (Exception ex)
            {
                Warehouse.AddEvent(Event.EnumSeverity.Error, Event.EnumType.Exception, ex.Message);
                throw new CraneException(String.Format("{0} Crane.InitialNotify failed ({1},{2})", Name, t is TelegramCraneTO ? (t as TelegramCraneTO).MFCS_ID : 0, material));
            }
        }


        // proces incoming TO telegram connected to command
        // proces incoming TO telegram connected to command
        private void WorkCommand(TelegramCraneTO tel, SimpleCraneCommand cmd, bool fastCommand)
        {
            try
            {
                if (tel.Confirmation == TelegramCraneTO.CONFIRMATION_OK)
                {
                    try
                    {
                        if (fastCommand && cmd.Task < SimpleCommand.EnumTask.Delete && fastCommand)
                            throw new CraneException(String.Format("Crane.WorkCommand {0} FastCommand{1} has wrong task {2}", Name, cmd.ID, cmd.Task));
                        if (cmd.Task == SimpleCommand.EnumTask.Pick)
                            Pick(tel.Palette.Barcode, cmd.Source);
                        else if (cmd.Task == SimpleCommand.EnumTask.Drop)
                            Drop(tel.Palette.Barcode, cmd.Source);
                        else if (cmd.Task == SimpleCommand.EnumTask.Create)
                            MaterialCreate(tel.Palette.Barcode);
                        else if (cmd.Task == SimpleCommand.EnumTask.Delete)
                            MaterialDelete(tel.Palette.Barcode);

                        FinishCommand(tel.MFCS_ID, cmd, SimpleCommand.EnumStatus.Finished);
                    }
                    catch
                    {
                        cmd.Reason = SimpleCommand.EnumReason.MFCS;
                        FinishCommand(tel.MFCS_ID, cmd, SimpleCommand.EnumStatus.Canceled);
                        throw;
                    }
                    finally
                    {
                        AssignCommandsAfterFinish();
                    }
                }
                else if (tel.Confirmation == TelegramCraneTO.CONFIRMATION_DIMENSIONCHECKERROR)
                {
                    cmd.Reason = SimpleCommand.EnumReason.DimensionCheck;
                    FinishCommand(tel.MFCS_ID, cmd, SimpleCommand.EnumStatus.Canceled);
                    Command Command = Warehouse.DBService.FindCommandByID(cmd.Command_ID.Value);
                    if (Command != null)
                    {
                        Command.Status = Database.Command.EnumCommandStatus.Canceled;
                        Warehouse.DBService.UpdateCommand(Command);
                        Warehouse.OnCommandFinish?.Invoke(Command);
                    }
                    else
                        throw new ConveyorBasicException(String.Format("{0} has no corresponding Command", cmd != null ? cmd.ToString() : "null"));
                }
                else if (tel.Confirmation == TelegramCraneTO.CONFIRMATION_CANCELBYWAREHOUSE ||
                         tel.Confirmation == TelegramCraneTO.CONFIRMATION_FAULT ||
                         tel.Confirmation == TelegramCraneTO.CONFIRMATION_CANCELBYMFCS)
                {
                    bool finish = false;
                    if (tel.Confirmation == TelegramCraneTO.CONFIRMATION_CANCELBYWAREHOUSE)
                        cmd.Reason = SimpleCommand.EnumReason.Warehouse;
                    else if (tel.Confirmation == TelegramCraneTO.CONFIRMATION_FAULT)
                    {
                        cmd.Reason = (Database.SimpleCommand.EnumReason)tel.Fault;
                        if (tel.Fault == TelegramCraneTO.FAULT_CANCEL_NOCMD)
                        {
                            FinishCommand(tel.Buffer_ID, null, SimpleCommand.EnumStatus.Canceled);
                            finish = true;
                        }
                    }
                    else if (tel.Confirmation == TelegramCraneTO.CONFIRMATION_CANCELBYMFCS)
                        cmd.Reason = SimpleCommand.EnumReason.MFCS;
                    if (finish)
                        FinishCommand(tel.MFCS_ID, cmd, SimpleCommand.EnumStatus.Finished);
                    else
                        FinishCommand(tel.MFCS_ID, cmd, SimpleCommand.EnumStatus.Canceled);
                    if (BufferCommand != null)
                    {
                        BufferCommand.Reason = SimpleCommand.EnumReason.MFCS;
                        FinishCommand(tel.MFCS_ID, BufferCommand, SimpleCommand.EnumStatus.Canceled);
                    }
                    AssignCommandsAfterFinish();
                    Warehouse.AddEvent(Event.EnumSeverity.Error, Event.EnumType.Command,
                                        String.Format("{0} Confirmation({1}), Fault({2})",
                                        cmd.ToString(), tel.Confirmation, tel.Fault));
                    Warehouse.SteeringCommands.Run &= tel.Confirmation == TelegramCraneTO.CONFIRMATION_CANCELBYMFCS ||
                                                      tel.Confirmation == TelegramCraneTO.CONFIRMATION_CANCELBYWAREHOUSE ||
                                                      (tel.Confirmation == TelegramCraneTO.CONFIRMATION_FAULT &&
                                                       (tel.Fault == TelegramCraneTO.FAULT_CANCEL_NOCMD || tel.Fault > TelegramCraneTO.FAULT_REPEATORDER));
                }
                else if (cmd.Status <= SimpleCommand.EnumStatus.InPlc)
                {
                    cmd.Status = SimpleCommand.EnumStatus.InPlc;
                    Warehouse.DBService.UpdateSimpleCommand(Command);
                }
            }
            catch (Exception ex)
            {
                Warehouse.AddEvent(Event.EnumSeverity.Error, Event.EnumType.Exception, ex.Message);
                throw new CraneException(String.Format("{0} Crane.WorkCommand failed", Name));
            }
        }

        public override void FinishCommand(Int32 id, SimpleCommand cmd, SimpleCommand.EnumStatus s)
        {
            try
            {
                if (cmd == null)
                    Warehouse.AddEvent(Event.EnumSeverity.Event, Event.EnumType.Program, String.Format("Crane.FinishCommand ({0},{1},{2}) ", id, "null", s));

                SimpleCraneCommand cOld = null;
                if (cmd == null || cmd.ID != id)
                    if (id != 0)
                        cOld = Warehouse.DBService.FindSimpleCraneCommandByID(id);

                if (cmd == null)
                    cmd = cOld;

                if (cmd == null && id != 0)
                    throw new CraneException("Can't find command by Id");
                if (cmd != null && cOld == null && cmd.ID != id && id != 0)
                    throw new CraneException("Can't find command by Id");
                if (cmd != null && cOld != null && cmd.ID != id && cOld.ID != id)
                    throw new CraneException("Can't find command by Id");


                cmd.Status = s;
                Warehouse.DBService.UpdateSimpleCommand(cmd);
                Warehouse.AddEvent(Event.EnumSeverity.Event, Event.EnumType.Command, cmd.ToString());

                if (cmd.Status == SimpleCommand.EnumStatus.Finished || cmd.Status == SimpleCommand.EnumStatus.Canceled)
                    OnSimpleCommandFinish(cmd);

                if (cOld != null && cOld.Status < SimpleCommand.EnumStatus.Canceled)
                {
                    cOld.Reason = SimpleCommand.EnumReason.MFCS;
                    cOld.Status = SimpleCommand.EnumStatus.Canceled;
                    Warehouse.DBService.UpdateSimpleCommand(cOld);
                    OnSimpleCommandFinish(cOld);
                    Warehouse.AddEvent(Event.EnumSeverity.Event, Event.EnumType.Command, cOld.ToString());
                }
            }
            catch (Exception ex)
            {
                Warehouse.AddEvent(Event.EnumSeverity.Error, Event.EnumType.Exception, ex.Message);
                throw new CraneException(String.Format("{0} ConveyorBasic.FinishCommand failed ({1},{2},{3})", Name, id, cmd != null ? cmd.ToString() : "null", s));
            }
        }



        // make all neceseary changes after pick command
        public void Pick(UInt32 material, string source)
        {
            try
            {
                LPosition pos = LPosition.FromString(source);
                if (pos.IsWarehouse())
                {
                    Warehouse.DBService.MaterialMove((int)material, source, Name);
                    Warehouse.OnMaterialMove?.Invoke(new Place { Material = (int)material, Place1 = Name }, EnumMovementTask.Move);
                    Place = Warehouse.DBService.FindPlace(Name);
                }
                else
                {
                    IConveyorIO cIO = FindInConveyor(source);
                    Move(material, cIO as ConveyorBasic, this);
                }
            }
            catch (Exception ex)
            {
                Warehouse.AddEvent(Event.EnumSeverity.Error, Event.EnumType.Exception, ex.Message);
                throw new CraneException(String.Format("{0} Crane.Pick failed ({1},{2})", Name, material, source));
            }
        }

        // make all nceseary changes after drop command
        public void Drop(UInt32 material, string target)
        {
            try
            {
                LPosition pos = LPosition.FromString(target);
                if (pos.IsWarehouse())
                {
                    Warehouse.DBService.MaterialMove((int)material, Name, target);
                    Warehouse.OnMaterialMove?.Invoke(new Place { Material = (int)material, Place1 = target }, EnumMovementTask.Move);
                    Place = null;
                }
                else
                {
                    ConveyorBasic cIO = FindOutConveyor(target) as ConveyorBasic;
                    Move(material, this, cIO); // this will be done automatically
                }
            }
            catch (Exception ex)
            {
                Warehouse.AddEvent(Event.EnumSeverity.Error, Event.EnumType.Exception, ex.Message);
                throw new CraneException(String.Format("{0} Crane.Drop failed ({1},{2})", Name, material, target));
            }
        }


        public override void CreateAndSendTOTelegram(SimpleCommand cmd)
        {
            try
            {
                if (!(cmd is SimpleCraneCommand))
                    throw new CraneException(String.Format("{0} is not SimpleCraneCommand.", cmd.ToString()));

                SimpleCraneCommand Cmd = cmd as SimpleCraneCommand;

                MaterialID matID = null;
                if (Cmd.Material.HasValue)
                    matID = Warehouse.DBService.FindMaterialID((int)Cmd.Material, true);

                LPosition pos = LPosition.FromString(Cmd.Source);
                if (!pos.IsWarehouse())
                {
                    if (cmd.Task == SimpleCommand.EnumTask.Pick)
                        pos = FindInConveyor(cmd.Source).CraneAddress;
                    else if (cmd.Task == SimpleCommand.EnumTask.Drop)
                        pos = FindOutConveyor(cmd.Source).CraneAddress;
                    else if (cmd.Task == SimpleCommand.EnumTask.Move)
                        pos = FindInOutConveyor(cmd.Source).CraneAddress;
                    else if (cmd.Task == SimpleCommand.EnumTask.Cancel)
                    {
                        pos = CraneAddress;
                        if (!cmd.CancelID.HasValue)
                            throw new CraneException(String.Format("{0} Parameter null", cmd != null ? cmd.ToString() : "null"));
                    }
                    else if (cmd.Task >= SimpleCommand.EnumTask.Delete)
                        pos = CraneAddress;
                }
                if (matID == null && cmd.Task != SimpleCommand.EnumTask.Move && cmd.Task != SimpleCommand.EnumTask.Cancel)
                    throw new CraneException(String.Format("Command validity fault ({0})", cmd.ToString()));

                Communicator.AddSendTelegram(
                    new Telegrams.TelegramCraneTO
                    {
                        Sender = Communicator.MFCS_ID,
                        Receiver = Communicator.PLC_ID,
                        MFCS_ID = cmd.ID,
                        Order = (short)cmd.Task,
                        Buffer_ID = (cmd.Task != SimpleCommand.EnumTask.Cancel) ? (matID != null ? matID.ID : 0) : cmd.CancelID.Value,
                        Position = new Telegrams.Position { R = (short)pos.Shelve, X = (short)pos.Travel, Y = (short)pos.Height, Z = (short)pos.Depth },
                        Palette = new Telegrams.Palette { Barcode = Convert.ToUInt32(matID != null ? matID.ID : 0), Type = (Int16)(matID != null ? matID.Size : 0), Weight = (UInt16)(matID != null ? matID.Weight : 0) },
                        ID = PLC_ID
                    });
                cmd.Status = SimpleCommand.EnumStatus.Written;
                Warehouse.DBService.UpdateSimpleCommand(cmd);
                Warehouse.AddEvent(Event.EnumSeverity.Event, Event.EnumType.Command, cmd.ToString());

                // check for blocked locations
                LPosition p = LPosition.FromString(cmd.Source);
                string frontLoc;

                if (p.Shelve > 0 && p.Depth == 2)
                {
                    LPosition pOther = new LPosition { Shelve = p.Shelve, Travel = p.Travel, Height = p.Height, Depth = 1 };
                    frontLoc = pOther.ToString();
                }
                else
                    frontLoc = cmd.Source;
                if (Warehouse.DBService.FindPlaceID(cmd.Source) != null && 
                    (Warehouse.DBService.FindPlaceID(cmd.Source).Blocked || Warehouse.DBService.FindPlaceID(frontLoc).Blocked))
                {
                    Warehouse.Segment[Segment].AlarmRequest(0);
                    Warehouse.AddEvent(Event.EnumSeverity.Error, Event.EnumType.Command, string.Format("Location blocked. Command: {0}", cmd.ToString() ));
                }

                if (cmd.Command_ID.HasValue)
                {
                    Command command = Warehouse.DBService.FindCommandByID(cmd.Command_ID.Value);
                    if (command == null)
                        throw new ConveyorException($"Command {command.ToString()} null.");
                    if (command.Status < Database.Command.EnumCommandStatus.Active)
                    {
                        command.Status = Database.Command.EnumCommandStatus.Active;
                        Warehouse.DBService.UpdateCommand(command);
                        Warehouse.OnCommandFinish?.Invoke(command);
                    }
                }

            }
            catch (Exception ex)
            {
                Warehouse.AddEvent(Event.EnumSeverity.Error, Event.EnumType.Exception, ex.Message);
                throw new CraneException(String.Format("{0} Crane.CreateAndSendTOTelegram failed. ({1})", Name, cmd != null ? cmd.ToString() : "null"));
            }
        }

        public void WriteCommandToPLC(SimpleCraneCommand cmd, bool fastCommand = false)
        {
            try
            {
                //                if (Automatic())
                if (Remote() && cmd != null)
                {
                    if ((Automatic() || fastCommand) && cmd != null && cmd.Status <= SimpleCommand.EnumStatus.NotActive)  // dodano: automatic
                    {
                        if (cmd.Status == SimpleCommand.EnumStatus.NotInDB)
                        {
                            cmd.Status = SimpleCommand.EnumStatus.NotActive;
                            Warehouse.DBService.AddSimpleCommand(cmd);
                        }
                        CreateAndSendTOTelegram(cmd);

                        if (fastCommand)
                            FastCommand = cmd;
                        else if (Command == null)
                            Command = cmd;
                        else if (BufferCommand == null)
                            BufferCommand = cmd;
                    }
                }
            }
            catch (Exception ex)
            {
                Warehouse.AddEvent(Event.EnumSeverity.Error, Event.EnumType.Exception, ex.Message);
                throw new CraneException(String.Format("{0} WriteCommandToPLC failed", Name));
            }
        }


        // write all pending commands to PLC
        public void WriteAllCommandsTOPlc()
        {
            try
            {
//                if (Automatic())
                if (Remote())
                {
                    WriteCommandToPLC(FastCommand, true);
                    WriteCommandToPLC(Command);
                    WriteCommandToPLC(BufferCommand);
                }
            }
            catch (Exception ex)
            {
                Warehouse.AddEvent(Event.EnumSeverity.Error, Event.EnumType.Exception, ex.Message);
                throw new CraneException(String.Format("{0} WriteAllCommandsToPlc failed", Name));
            }
        }


        public SimpleCraneCommand FindBestOutput(CommandMaterial cmd)
        {
            try
            {
                var routes = from route in OutRouteDef.FinalRouteCost
                             where route.Items.Last().Final.Compatible(cmd.Target) && route.Items[0].Next.Place == null && route.Items[0].Final is Conveyor
                             group route by new { Route = route.Items[0] } into Group
                             select new
                             {
                                 Node1 = Group.Key.Route,
                                 RouteCost = Group.Min((x) => x.Cost)
                             };

                if (routes.Count() > 0)
                {
                    var route = routes.ElementAt(Random.Next(routes.Count())); // -1
                    return new SimpleCraneCommand
                    {
                        Command_ID = cmd.ID,
                        Material = (int)cmd.Material,
                        Source = route.Node1.Final.Name,
                        Unit = Name,
                        Task = SimpleCommand.EnumTask.Drop,
                        Status = SimpleCommand.EnumStatus.NotInDB,
                        Time = DateTime.Now
                    };
                }
                return null;
            }
            catch (Exception ex)
            {
                Warehouse.AddEvent(Event.EnumSeverity.Error, Event.EnumType.Exception, ex.Message);
                throw new CraneException(String.Format("{0} Crane.FindBestOutput failed {1}", Name, cmd != null ? cmd.ToString() : "null"));
            }
        }


        public class BestInput
        {
            public Int32 ID { get; set; }
            public string Place { get; set; }

            public int? Material { get; set; }
        }

        public SimpleCraneCommand FindBestInput(bool automatic, IConveyorIO forcedInput )
        {
            try
            {
                CommandMaterial cmd = null;
                List<BestInput> res = new List<BestInput>();

                foreach (ConveyorJunction c in InConveyor)
                    if (c.Place != null)
                    {
                        cmd = Warehouse.DBService.FindFirstCommand(c.Place.Material, automatic);
                        if (cmd != null)
                        {
                            bool check = (c.ActiveRoute != null) && (c.ActiveRoute.Items[0].Final is Crane) && (c.ActiveRoute.Items[0].Final.Name == Name) && (c.ActiveMaterial == cmd.Material);
                            check = check && !Warehouse.DBService.CheckIfSimpleCraneCommandToPlaceMaterialExist(c.Place, automatic);
                            if (check)
                            {
                                Warehouse.AddEvent(Event.EnumSeverity.Event, Event.EnumType.Program, $"{Name}.FindBestInput({automatic},{forcedInput?.ToString()}");
                                foreach (var route in OutRouteDef.FinalRouteCost)
                                    if (route.Items.Last().Final.Compatible(cmd.Target))  // && route.Items[0].Final.Place == null)
                                        if ((Warehouse.FreePlaces(route.Items[0].Final) > Warehouse.DBService.CountSimpleCraneCommandForTarget(route.Items[0].Final.Name, true)) ||
                                            route.Items[0].Final is Crane)
                                        {
                                            int a = Warehouse.FreePlaces(route.Items[0].Final);
                                            int b = Warehouse.DBService.CountSimpleCraneCommandForTarget(route.Items[0].Final.Name, true);
                                            Warehouse.AddEvent(Event.EnumSeverity.Event, Event.EnumType.Program, String.Format("FindBestInput On {0} Free places {1} Incoming commands {2}", route.Items[0].Final.Name, a, b));
                                            res.Add(new BestInput { ID = cmd.ID, Place = c.Name, Material = cmd.Material });
                                            if ((forcedInput != null && forcedInput == c))
                                            {
                                                res.Clear();
                                                res.Add(new BestInput { ID = cmd.ID, Place = c.Name, Material = cmd.Material });
                                                break;
                                            }
                                        }

                                /*                            var routes = from route in OutRouteDef.FinalRouteCost
                                                                         where route.Items.Last().Final.Compatible(cmd.Target) && route.Items[0].Next.Place == null &&
                                                                         (route.Items[0].Final is Crane || route.Items[0].Final.Place == null)
                                                                         group route by new { Route = route.Items[0] } into Group
                                                                         select new
                                                                         {
                                                                             Node1 = Group.Key.Route,
                                                                             RouteCost = Group.Min((x) => x.Cost)
                                                                         };
                                */
                            }
                        }
                    }

                if (res.Count() > 0)
                {
                    var c = res.ElementAt(Random.Next(res.Count())); // -1
                    return new SimpleCraneCommand
                    {
                        Command_ID = c.ID,
                        Material = c.Material,
                        Source = c.Place,
                        Unit = Name,
                        Task = SimpleCommand.EnumTask.Pick,
                        Status = SimpleCommand.EnumStatus.NotInDB,
                        Time = DateTime.Now
                    };
                }
                return null;
            }
            catch (Exception ex)
            {
                Warehouse.AddEvent(Event.EnumSeverity.Error, Event.EnumType.Exception, ex.Message);
                throw new CraneException(String.Format("{0} Crane.FindBestInput failed ({1})", Name, automatic));
            }
        }


        public SimpleCraneCommand FindBestWarehouse(bool automatic, List<string> bannedPlaces, SimpleCraneCommand otherDeck)
        {
            try
            {
                CommandMaterial cmd = Warehouse.DBService.FindFirstCommandStillInWarehouse(Shelve, automatic, bannedPlaces);
                bool found = false;
                if (cmd != null)
                {
                    foreach (var route in OutRouteDef.FinalRouteCost)
                        if (route.Items.Last().Final.Compatible(cmd.Target))  // && route.Items[0].Final.Place == null)
                        {
                            int cnt = Warehouse.CountSimpleCommandToAcumulation(route.Items[0].Final, new List<ConveyorBasic>(), otherDeck);
                            if (Warehouse.FreePlaces(route.Items[0].Final) > cnt ||
                                route.Items[0].Final is Crane)
                            {
                                int a = Warehouse.FreePlaces(route.Items[0].Final);
                                int b = Warehouse.DBService.CountSimpleCraneCommandForTarget(route.Items[0].Final.Name, true);
                                Warehouse.AddEvent(Event.EnumSeverity.Event, Event.EnumType.Program, String.Format("FindBestWarehouse On {0} Free places {1} Incoming commands {2}", route.Items[0].Final.Name, a, b));
                                found = true;
                                break;
                            }
                        }

                    if (found)
                        return new SimpleCraneCommand
                        {
                            Command_ID = cmd.ID,
                            Material = (int)cmd.Material,
                            Source = cmd.Source,
                            Unit = Name,
                            Task = SimpleCommand.EnumTask.Pick,
                            Status = SimpleCommand.EnumStatus.NotInDB,
                            Time = DateTime.Now
                        };
                }
                return null;
            }
            catch (Exception ex)
            {
                Warehouse.AddEvent(Event.EnumSeverity.Error, Event.EnumType.Exception, ex.Message);
                throw new CraneException(String.Format("{0} Crane.FindBestWarehouse failed ({1})", Name, automatic));
            }
        }




        public override bool Automatic()
        {
            if (PLC_Status == null)
                return false;
            else
                return PLC_Status.Status[TelegramCraneStatus.STATUS_AUTOMATIC];
        }

        public override bool Remote()
        {
            if (PLC_Status == null)
                return false;
            else
                return PLC_Status.Status[TelegramCraneStatus.STATUS_REMOTE];
        }
        public override bool LongTermBlock()
        {
            if (PLC_Status == null)
                return false;
            else
                return PLC_Status.Status[TelegramCraneStatus.STATUS_LONGTERMBLOCK];
        }
        public override bool CheckIfAllNotified()
        {
            foreach (var c in Warehouse.CraneList)
                if (c.Communicator == Communicator && !c.InitialNotified)
                    return false;
            return true;
        }

        protected IConveyorIO FindInConveyor(string name)
        {
            try
            {
                return InConveyor.Find(prop => prop.Name == name) as IConveyorIO;
            }
            catch(Exception ex)
            {
                Warehouse.AddEvent(Event.EnumSeverity.Error, Event.EnumType.Exception, ex.Message);
                throw new CraneException(String.Format("{0} Crane.FindInConveyor failed ({1})", Name, name));
            }
        }

        protected IConveyorIO FindOutConveyor(string name)
        {
            try
            {
                return OutConveyor.Find(prop => prop.Name == name) as IConveyorIO;
            }
            catch (Exception ex)
            {
                Warehouse.AddEvent(Event.EnumSeverity.Error, Event.EnumType.Exception, ex.Message);
                throw new CraneException(String.Format("{0} Crane.FindOutConveyor failed {1}", Name, name));
            }
        }

        protected IConveyorIO FindInOutConveyor(string name)
        {
            try
            {
                if (InConveyor != null && InConveyor.Exists(prop => prop.Name == name))
                    return InConveyor.Find(prop => prop.Name == name) as IConveyorIO;
                if (OutConveyor != null && OutConveyor.Exists(prop => prop.Name == name))
                    return OutConveyor.Find(prop => prop.Name == name) as IConveyorIO;
                throw new Exception();
            }
            catch (Exception ex)
            {
                Warehouse.AddEvent(Event.EnumSeverity.Error, Event.EnumType.Exception, ex.Message);
                throw new CraneException(String.Format("{0} Crane.FindInOutConveyor failed ({1})", Name, name));
            }
        }


        // initialize
        public override void Initialize(BasicWarehouse w)
        {
            try
            {
                base.Initialize(w);
                if (CraneInfo != null)
                {
                    CraneInfo.Name = Name;                    
                    CraneInfo.Initialize();
                }

                if(HomePosition != null)
                    LPHomePosition = LPosition.FromString(HomePosition);

                if (OutRouteDef != null)
                {
                    if (OutRouteDef == null || OutRouteDef.XmlRoute == null)
                        throw new BasicWarehouseException(String.Format("{0} has no XmlRoute defined", Name));
                    OutConveyor = new List<IConveyorIO>();
                    OutRouteDef.Node = new List<RouteNode>();
                    OutRouteDef.FinalRouteCost = new List<Route>();
                    foreach (XmlRouteNode node in OutRouteDef.XmlRoute)
                    {
                        OutRouteDef.Node.Add(new RouteNode { Next = Warehouse.FindConveyorBasic(node.Next), Cost = node.Cost });
                        OutConveyor.Add(Warehouse.CheckForConveyorIO(node.Next));
                    }
                }
            }
            catch (Exception ex)
            {
                Warehouse.AddEvent(Event.EnumSeverity.Error, Event.EnumType.Exception, ex.Message);
                throw new CraneException(String.Format("{0} Crane.Initialize failed", Name));
            }
        }
    

    }

}
