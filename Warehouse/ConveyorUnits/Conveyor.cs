using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using Telegrams;
using Database;
using Warehouse.Model;
using System.Collections;
using MFCS.Communication;
using System.Runtime.Serialization;
using System.ComponentModel;

namespace Warehouse.ConveyorUnits
{

    [Serializable]
    public class ConveyorException : Exception
    {
        public ConveyorException(string s) : base(s)
        { }
    }


    [DataContract]
    public class ConveyorInfo : ConveyorBasicInfo
    {
        [DataMember]
        [XmlIgnore]
        public string LastCommand { get; set; }

        [DataMember]
        [XmlIgnore]
        public Int32 Material { get; set; }

        [DataMember]
        [XmlIgnore]
        public BitArray MaterialError { get; set; }

        [DataMember]
        public List<State> StateList { get; set; }


        public void SetState(BitArray state)
        {
            try
            {
               StateList?.ForEach(prop => prop.Active = state[prop.Offset]);
            }
            catch (Exception ex)
            {
                throw new ConveyorBasicException(String.Format("ConveyorInfo.SetState failed. Reason:{0}", ex.Message));
            }
        }
    }

    // Place 
    public class Conveyor : ConveyorBasic
    {
        [XmlAttribute]
        [DefaultValue(false)]
        public bool AcumulationMark { get; set; }
 
        public ConveyorInfo ConveyorInfo { get; set; }

        [XmlIgnore]
        public TelegramTransportTO Command_Status { get; set; }

        [XmlIgnore]
        public TelegramTransportStatus PLC_Status { get; set; }

        [XmlIgnore]
        public SimpleCommand Command { get; set; }  


        private DateTime NextFree { get; set; }

        public Conveyor() : base()
        {
        }


        public override void OnReceiveTelegram(Telegram t)
        {
            if (t is TelegramTransportStatus)
                OnTelegramTransportStatus(t);
            if (t is TelegramTransportTO)
                OnTelegramTransportTO(t);
        }

        public virtual void OnTelegramTransportStatus(Telegram t)
        {
            try
            {
                if (t != null)
                    PLC_Status = t as TelegramTransportStatus;
                // call action
                if (ConveyorInfo != null && PLC_Status != null)
                {
                    ConveyorInfo.Name = Name;
                    ConveyorInfo.AlarmID = PLC_Status.FirstAlarmID;
                    ConveyorInfo.Fault = PLC_Status.Fault;
                    ConveyorInfo.SetAlarms(PLC_Status.Alarms, Warehouse);
                    ConveyorInfo.SetSensors(PLC_Status.State);
                    ConveyorInfo.SetState(PLC_Status.Status);
                    ConveyorInfo.Status = PLC_Status.Status;
                    ConveyorInfo.State = PLC_Status.State;
                    ConveyorInfo.Online = Online();
                    ConveyorInfo.Material = Place != null ? Place.Material : 0;
                    CallNotifyVM(ConveyorInfo);
                }
            }
            catch (Exception ex)
            {
                Warehouse.AddEvent(Event.EnumSeverity.Error, Event.EnumType.Exception, String.Format("Conveyor.OnTelegramTransportStatus {0}", ex.Message));
                Warehouse.SteeringCommands.Run = false;
            }
        }


        public virtual void OnTelegramTransportTO(Telegram t)
        {
            try
            {
                Command_Status = t as TelegramTransportTO;
                ProcesMaterialNotify(Command_Status);
                if (Command != null && ConveyorInfo != null)
                    ConveyorInfo.LastCommand = Command.ToSmallString();
                if (ConveyorInfo != null && PLC_Status != null)
                {
                    ConveyorInfo.Name = Name;
                    ConveyorInfo.Material = Place != null ? Place.Material : 0;
                    ConveyorInfo.MaterialError = (t as TelegramTransportTO).Palette.FaultCode;
                    CallNotifyVM(ConveyorInfo);
                }
            }
            catch (Exception ex)
            {
                string msg = "";
                if (t == null)
                    msg = "null";
                else
                    msg = (t as TelegramTransportTO).MFCS_ID.ToString();
                Warehouse.AddEvent(Event.EnumSeverity.Error, Event.EnumType.Exception, 
                                   String.Format("Conveyor.OnTelegramTransportTO place {0}, telID {1}", Name,  msg, ex.Message));
//                Warehouse.SteeringCommands.Run = false; // todo: jure bug, on site put back to function
            }
        }

        public override void FinishCommand(Int32 id, SimpleCommand cmd, SimpleCommand.EnumStatus s)
        {
            try
            {
                if (cmd == null)
                    Warehouse.AddEvent(Event.EnumSeverity.Event, Event.EnumType.Program, String.Format("Conveyor.FinishCommand ({0},{1},{2}) ", id, "null", s));

                SimpleConveyorCommand cOld = null; 
                if (cmd == null || cmd.ID != id)
                    if (id != 0)
                        cOld = Warehouse.DBService.FindSimpleConveyorCommandByID(id);

                if (cmd == null)
                    cmd = cOld;

                if (cmd != null)
                {
                    cmd.Status = s;
                    Warehouse.DBService.UpdateSimpleCommand(cmd);
                    if (cmd.Status == SimpleCommand.EnumStatus.Finished || cmd.Status == SimpleCommand.EnumStatus.Canceled)
                        OnSimpleCommandFinish(cmd);
                    Warehouse.AddEvent(Event.EnumSeverity.Event, Event.EnumType.Command, cmd.ToString());
                }
                if (cOld != null && cOld.Status < SimpleCommand.EnumStatus.Canceled)
                {
                    cOld.Status = SimpleCommand.EnumStatus.Canceled;
                    cOld.Reason = SimpleCommand.EnumReason.MFCS;
                    Warehouse.DBService.UpdateSimpleCommand(cOld);
                    OnSimpleCommandFinish(cOld);
                    Warehouse.AddEvent(Event.EnumSeverity.Event, Event.EnumType.Command, cOld.ToString());
                }
                if (cmd != null && cOld == null && cmd.ID != id && id != 0)
                    throw new ConveyorException("Can't find command by Id");

            }
            catch (Exception ex)
            {
                Warehouse.AddEvent(Event.EnumSeverity.Error, Event.EnumType.Exception, ex.Message);
                throw new ConveyorException(String.Format("{0} Conveyor.FinishCommand failed ({1},{2},{3})", Name, id, cmd != null ?  cmd.ToString() : "null" , s));
            }
        }


        public void ProcesMaterialNotify(TelegramTransportTO tel)
        {
            try
            {
                // update materialID
                if (tel.Confirmation != TelegramTransportTO.CONFIRMATION_PALETTETAKEN && tel.Confirmation != TelegramTransportTO.CONFIRMATION_PALETTEDELETED)
                {
                    // TAI specific: change hc to 2 if we can store to higher rack
                    if (tel.SenderTransport == 100 && Warehouse.DBService.IsRackSlotHC2Empty())
                        tel.Palette.Weight = (ushort)(20000 + tel.Palette.Weight % 10000);
                    CreateOrUpdateMaterialID(tel.Palette);
                }

                if (tel.Confirmation == TelegramTransportTO.CONFIRMATION_NOTIFY ||
                    tel.Confirmation == TelegramTransportTO.CONFIRMATION_DIMENSIONCHECKERROR)
                {
//                    Move(tel.Palette.Barcode, Warehouse.FindDeviceByPLC_ID(tel.Previous), this);
                    ConveyorBasic src = Warehouse.FindDeviceByPLC_ID(tel.Previous);
                    Move(tel.Palette.Barcode, src, this);
                }
                else if (tel.Confirmation == TelegramTransportTO.CONFIRMATION_INITIALNOTIFY)
                    InitialNotify(tel, tel.Palette.Barcode);
                else if (tel.Confirmation == TelegramTransportTO.CONFIRMATION_NEWPALETTE || tel.Confirmation == TelegramTransportTO.CONFIRMATION_PALETTECREATED)
                {
                    try
                    {
                        Place p = Warehouse.DBService.FindMaterial((int)tel.Palette.Barcode);
                        if (p != null)
                            Warehouse.AddEvent(Event.EnumSeverity.Error, Event.EnumType.Material,
                                               string.Format("Place {0}: pallet {1} exists in the system", p.Place1, tel.Palette.Barcode));
                        MaterialCreate(tel.Palette.Barcode);
                        FinishCommand(tel.MFCS_ID, Command, SimpleCommand.EnumStatus.Finished);
                    }
                    catch
                    {
                        Command.Reason = SimpleCommand.EnumReason.MFCS;
                        FinishCommand(tel.MFCS_ID, Command, SimpleCommand.EnumStatus.Canceled);
                        throw;
                    }
                    finally
                    {
                        Command = null;
                    }
                }
                else if (tel.Confirmation == TelegramTransportTO.CONFIRMATION_PALETTETAKEN || tel.Confirmation == TelegramTransportTO.CONFIRMATION_PALETTEDELETED)
                {
                    try
                    {
                        MaterialDelete(tel.Palette.Barcode);
                        FinishCommand(tel.MFCS_ID, Command, SimpleCommand.EnumStatus.Finished);
                    }
                    catch
                    {
                        Command.Reason = SimpleCommand.EnumReason.MFCS;
                        FinishCommand(tel.MFCS_ID, Command, SimpleCommand.EnumStatus.Canceled);
                        throw;
                    }
                    finally
                    {
                        Command = null;
                    }
                }
                else if (tel.Confirmation == TelegramTransportTO.CONFIRMATION_COMMANDFINISHED)
                {
                    try
                    {
                        Move(tel.Palette.Barcode, Warehouse.FindDeviceByPLC_ID(tel.Previous), this);
                        // for output we need additional confirmation PALETTETAKEN
                        if (tel.MFCS_ID != 0 && (!(this is IConveyorOutput)))
                            FinishCommand(tel.MFCS_ID, Command, SimpleCommand.EnumStatus.Finished);
                    }
                    finally
                    {
                        if (tel.MFCS_ID != 0 && (!(this is IConveyorOutput)))
                            Command = null;
                    }
                }
                else if (tel.Confirmation == TelegramTransportTO.CONFIRMATION_FAULT)
                {
                    try
                    {
                        Command.Reason = (Database.SimpleCommand.EnumReason)tel.Confirmation;
                        FinishCommand(tel.MFCS_ID, Command, SimpleCommand.EnumStatus.Canceled);
                    }
                    finally
                    {
                        Command = null;
                    }
                }
                else if (tel.Confirmation == TelegramTransportTO.CONFIRMATION_NONE)
                {
                    try
                    {
                        Command.Reason = (Database.SimpleCommand.EnumReason)tel.Fault;
                        FinishCommand(tel.MFCS_ID, Command, SimpleCommand.EnumStatus.Canceled);
                    }
                    finally
                    {
                        Command = null;
                    }
                }
            }
            catch (Exception ex)
            {
                Command.Reason = SimpleCommand.EnumReason.MFCS;
                FinishCommand(tel.MFCS_ID, Command, SimpleCommand.EnumStatus.Canceled);
                Warehouse.AddEvent(Event.EnumSeverity.Error, Event.EnumType.Exception, ex.Message);
                throw new ConveyorException(String.Format("{0} ProcesMaterialNotify failed", Name));
            }
        }

        
        public override bool Automatic()
        {
            try
            {
                if (PLC_Status == null)
                    return false;
                else
                    return PLC_Status.Status[TelegramTransportStatus.STATUS_AUTOMATIC];
            }
            catch (Exception ex)
            {
                Warehouse.AddEvent(Event.EnumSeverity.Error, Event.EnumType.Exception, ex.Message);
                throw new ConveyorException(String.Format("{0} Conveyor.Automatic failed", Name));
            }
        }
        public override bool Remote()
        {
            try
            {
                if (PLC_Status == null)
                    return false;
                else
                    return PLC_Status.Status[TelegramTransportStatus.STATUS_REMOTE];
            }
            catch (Exception ex)
            {
                Warehouse.AddEvent(Event.EnumSeverity.Error, Event.EnumType.Exception, ex.Message);
                throw new ConveyorException(String.Format("{0} Conveyor.Remote failed", Name));
            }
        }
        public override bool LongTermBlock()
        {
            try
            {
                if (PLC_Status == null)
                    return false;
                else
                    return PLC_Status.Status[TelegramTransportStatus.STATUS_LONGTERMFAULT];
            }
            catch (Exception ex)
            {
                Warehouse.AddEvent(Event.EnumSeverity.Error, Event.EnumType.Exception, ex.Message);
                throw new ConveyorException(String.Format("{0} Conveyor.LongTermBlock failed", Name));
            }
        }

        public override bool CheckIfAllNotified()
        {
            try
            {
                foreach (var c in Warehouse.ConveyorList)
                    if (c.Communicator == Communicator && !c.InitialNotified)
                        return false;
                return true;
            }
            catch (Exception ex)
            {
                Warehouse.AddEvent(Event.EnumSeverity.Error, Event.EnumType.Exception, ex.Message);
                throw new ConveyorException(String.Format("{0} Conveyor.CheckIfAllNotified failed", Name));
            }
        }


        public override void CreateAndSendTOTelegram(SimpleCommand cmd)
        {
            try
            {
                if (!(cmd is SimpleConveyorCommand))
                    throw new ConveyorException(String.Format("{0} is not SimpleCoveyorCommand", cmd.ToString()));

                SimpleConveyorCommand Cmd = cmd as SimpleConveyorCommand;

                MaterialID matID = Warehouse.DBService.FindMaterialID((int)cmd.Material, true);

                short task;
                switch (Cmd.Task)
                {
                    case SimpleCommand.EnumTask.Move:
                        task = TelegramTransportTO.ORDER_MOVE;
                        break;
                    case SimpleCommand.EnumTask.Delete:
                        task = TelegramTransportTO.ORDER_PALETTEDELETE;
                        break;
                    case SimpleCommand.EnumTask.Create:
                        task = TelegramTransportTO.ORDER_PALETTECREATE;
                        break;
                    case SimpleCommand.EnumTask.Cancel:
                        throw new NotImplementedException();
                    default:
                        throw new ConveyorException(String.Format("{0} has unknown Order", Cmd.ToString()));
                }

                if (Warehouse.Conveyor.ContainsKey(Cmd.Source))
                {
                    Communicator.AddSendTelegram(new TelegramTransportTO
                    {
                        MFCS_ID = Cmd.ID,
                        Sender = Communicator.MFCS_ID,
                        Receiver = Communicator.PLC_ID,
                        Order = task,
                        Palette = new Palette { Barcode = (UInt32)Cmd.Material, Type = (short)matID.Size, Weight = (ushort)matID.Weight },
                        SenderTransport = Warehouse.FindConveyorBasic(Cmd.Source).ConveyorAddress,
                        Source = Warehouse.FindConveyorBasic(Cmd.Source).ConveyorAddress,
                        Target = Warehouse.FindConveyorBasic(Cmd.Target).ConveyorAddress
                    });

                    cmd.Status = SimpleCommand.EnumStatus.Written;
                    Warehouse.DBService.UpdateSimpleCommand(cmd);
                    Warehouse.AddEvent(Event.EnumSeverity.Event, Event.EnumType.Command, cmd.ToString());

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
            }
            catch (Exception e)
            {
                Warehouse.AddEvent(Event.EnumSeverity.Error, Event.EnumType.Exception, e.Message);
                throw new ConveyorException(String.Format("{0} Conveyor.CreateAndSendTOTelegram failed ({1}).", Name, cmd != null ? cmd.ToString() : "null"));
            }
        }


        public override void DirectVMNotify()
        {
            if (ConveyorInfo != null && PLC_Status != null)
            {
                ConveyorInfo.Name = Name;
                ConveyorInfo.AlarmID = PLC_Status.FirstAlarmID;
                ConveyorInfo.Fault = PLC_Status.Fault;
                ConveyorInfo.SetAlarms(PLC_Status.Alarms, Warehouse);
                ConveyorInfo.SetSensors(PLC_Status.State);
                ConveyorInfo.SetState(PLC_Status.Status);
                ConveyorInfo.Status = PLC_Status.Status;
                ConveyorInfo.State = PLC_Status.State;
                ConveyorInfo.Online = Online();
                ConveyorInfo.Material = Place != null ? Place.Material : 0;
                CallNotifyVM(ConveyorInfo);
            }
        }

        public override void Initialize(BasicWarehouse w)
        {
            base.Initialize(w);
            try
            {
                base.Initialize(w);
                if (ConveyorInfo != null)
                    ConveyorInfo.Initialize();
                if (XmlRouteNode != null)
                {
                    Route = new RouteNode { Next = Warehouse.FindConveyorBasic(XmlRouteNode.Next), Cost = XmlRouteNode.Cost };
                    if (Route.Next is Crane)
                        Warehouse.ConnectCraneInConveyor(this, Route);
                }
            }
            catch (Exception ex)
            {
                Warehouse.AddEvent(Event.EnumSeverity.Error, Event.EnumType.Exception, ex.Message);
                throw new ConveyorException(String.Format("{0} Conveyor.Initialize failed", Name));
            }
        }
    }
}
