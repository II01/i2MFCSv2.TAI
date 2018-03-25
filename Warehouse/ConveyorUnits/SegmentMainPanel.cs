using MFCS.Communication;
using Database;
using System;
using System.Collections.Generic;
using System.Linq;
using Telegrams;
using Warehouse.Model;

namespace Warehouse.ConveyorUnits
{

    public class SegmentBasicException : Exception
    {
        public SegmentBasicException(string s) : base(s) { }
    }

    public class SegmentMainPanel : Segment
    {
        private List<Conveyor> SegmentConveyors { get; set; }
        public ConveyorInfo SegmentInfo { get; set; }

        public override void OnlineTrigger()
        {
            if (Communicator.Online() && !_oldOnline)
            {
                Startup();
                DirectVMNotify();
                SegmentConveyors.ForEach(prop => prop.DirectVMNotify());
            }
            if (!Communicator.Online() && _oldOnline)
            {
                DirectVMNotify();
                SegmentConveyors.ForEach(prop => prop.DirectVMNotify());
            }
            _oldOnline = Communicator.Online();
        }

        public override void Refresh()
        {
            base.Refresh();
            if (!UINotified)
            {
                SegmentConveyors.ForEach(prop => prop.DirectVMNotify());
                Warehouse.SteeringCommands.DirectVMNotify();
                DirectVMNotify();
                UINotified = true;
            }
        }

        public override void AutomaticOff(Int32 id)
        {
            Communicator.AddSendTelegram(new TelegramTransportStatus
            {
                MFCS_ID = id,
                SegmentID = (short)PLC_ID,
                Sender = Communicator.MFCS_ID,
                Receiver = Communicator.PLC_ID,
                Order = TelegramTransportStatus.ORDER_AUTOOFF
            });
            Warehouse.AddEvent(Database.Event.EnumSeverity.Event, Database.Event.EnumType.Program, String.Format("Segment {0} AutomaticOff called.", Name));
        }

        public override void AutomaticOn(Int32 id)
        {
            Communicator.AddSendTelegram(new TelegramTransportStatus
            {
                MFCS_ID = id,
                SegmentID = (short)PLC_ID,
                Sender = Communicator.MFCS_ID,
                Receiver = Communicator.PLC_ID,
                Order = TelegramTransportStatus.ORDER_AUTOON
            });
            Warehouse.AddEvent(Database.Event.EnumSeverity.Event, Database.Event.EnumType.Program, String.Format("Segment {0} AutomaticOn called.", Name));
        }

        public override void LongTermBlockOn(Int32 id)
        {
            Communicator.AddSendTelegram(new TelegramTransportStatus
            {
                MFCS_ID = id,
                SegmentID = (short)PLC_ID,
                Sender = Communicator.MFCS_ID,
                Receiver = Communicator.PLC_ID,
                Order = TelegramTransportStatus.ORDER_LONGTERMBLOCKON
            });
            Warehouse.AddEvent(Database.Event.EnumSeverity.Event, Database.Event.EnumType.Program, String.Format("Segment {0} LongTermBlockOn called.", Name));
        }

        public override void LongTermBlockOff(Int32 id)
        {
            Communicator.AddSendTelegram(new TelegramTransportStatus
            {
                MFCS_ID = id,
                SegmentID = (short)PLC_ID,
                Sender = Communicator.MFCS_ID,
                Receiver = Communicator.PLC_ID,
                Order = TelegramTransportStatus.ORDER_LONGTERMBLOCKOFF
            });
            Warehouse.AddEvent(Database.Event.EnumSeverity.Event, Database.Event.EnumType.Program, String.Format("Segment {0} LongTermBlockOff called.", Name));
        }

        public override void Info(Int32 id)
        {
            Communicator.AddSendTelegram(new TelegramTransportStatus
            {
                MFCS_ID = id,
                SegmentID = (short)PLC_ID,
                Sender = Communicator.MFCS_ID,
                Receiver = Communicator.PLC_ID,
                Order = TelegramTransportStatus.ORDER_INFO
            });
            Warehouse.AddEvent(Database.Event.EnumSeverity.Event, Database.Event.EnumType.Program, String.Format("Segment {0} refresh called.", Name));
        }

        public override void AlarmRequest(Int32 id)
        {
            Communicator.AddSendTelegram(new TelegramTransportStatus
            {
                MFCS_ID = id,
                SegmentID = (short)PLC_ID,
                Sender = Communicator.MFCS_ID,
                Receiver = Communicator.PLC_ID,
                Order = TelegramTransportStatus.ORDER_ALARM
            });
            Warehouse.AddEvent(Database.Event.EnumSeverity.Event, Database.Event.EnumType.Program, String.Format("Segment {0} refresh called.", Name));
        }

        public override void Startup()
        {
            // SetClock(0);
            Info(0);
        }

        public override void Reset(Int32 id)
        {
            Communicator.AddSendTelegram(new TelegramTransportStatus
            {
                MFCS_ID = id,
                SegmentID = (short)PLC_ID,
                Sender = Communicator.MFCS_ID,
                Receiver = Communicator.PLC_ID,
                Order = TelegramTransportStatus.ORDER_RESET
            });
            SegmentInfo.ActiveAlarms.ForEach( aa => Warehouse.DBService.UpdateAlarm(Name, aa.ToString(), Alarm.EnumAlarmStatus.Ack) );
            foreach (var c in SegmentConveyors)
                c.ConveyorInfo.ActiveAlarms.ForEach(aa => Warehouse.DBService.UpdateAlarm(c.Name, aa.ToString(), Alarm.EnumAlarmStatus.Ack) );

            Warehouse.AddEvent(Database.Event.EnumSeverity.Event, Database.Event.EnumType.Program, String.Format("Segment {0} reset called.", Name));
        }

        public override void SetClock(Int32 id)
        {
            Communicator.AddSendTelegram(new TelegramTransportSetTime
            {
                MFCS_ID = id,
                Sender = Communicator.MFCS_ID,
                Receiver = Communicator.PLC_ID,
                PLCSetTime = new PLCSetTime { Year = (short)DateTime.Now.Year, Day = (short)DateTime.Now.Day, Hour = (short)DateTime.Now.Hour, Month = (short)DateTime.Now.Month, Minute = (short)DateTime.Now.Minute, Seconds = (short)DateTime.Now.Second },
                Order = TelegramTransportSetTime.ORDER_SETDATETIME
            });
            Warehouse.AddEvent(Database.Event.EnumSeverity.Event, Database.Event.EnumType.Program, String.Format("Segment {0} SetTime called.", Name));
        }

        public override void DirectVMNotify()
        {
            System.Collections.BitArray ta;

            if (SegmentInfo != null && PLC_Status != null)
            {
                TelegramTransportStatus tel = PLC_Status as TelegramTransportStatus;

                SegmentInfo.Name = Name;
                SegmentInfo.AlarmID = tel.FirstAlarmID;
                SegmentInfo.Fault = tel.Fault;
                ta = tel.Alarms.Clone() as System.Collections.BitArray;
                if (SegmentConveyors.Count() == 0) // only main cabinet
                    Warehouse.StrategyList.ForEach(bs => ta[98] = (bs is Strategy.StrategyGeneral)? (bs as Strategy.StrategyGeneral).DatabaseToLarge: ta[98]);
                SegmentInfo.SetAlarms(ta, Warehouse);
                SegmentInfo.SetSensors(tel.State);
                SegmentInfo.SetState(tel.Status);
                SegmentInfo.Status = tel.Status;
                SegmentInfo.State = tel.State;
                SegmentInfo.Online = Communicator.Online();
                CallNotifyVM(SegmentInfo);                
            }
        }

        public override void OnRecTelegram(Telegram t)
        {
            if (t is TelegramTransportStatus)
                OnTelegramTransportStatus(t);
        }


        public virtual void OnTelegramTransportStatus(Telegram t)
        {
            // notify all segment conveyors
            try
            {
                SegmentConveyors.ForEach(prop => prop.OnTelegramTransportStatus(t));
                if (t != null)
                    PLC_Status = t;
                DirectVMNotify();
            }
            catch (Exception ex)
            {
                Warehouse.AddEvent(Event.EnumSeverity.Error, Event.EnumType.Exception, ex.Message);
                throw new SegmentException(String.Format("{0} SegmentMainPanel.OnTelegramTransportStatus failed ", Name));
            }
        }


        public override void Initialize(BasicWarehouse w)
        {
            try
            {
                base.Initialize(w);
                SegmentConveyors = (from c in Warehouse.ConveyorList
                                    where c.Segment == Name
                                    select c).ToList();
/*                Communicator.DispatchRcv.Add(
                    new DispatchNode
                    {
                        DispatchTerm = (t) => (t is TelegramTransportStatus &&
                                                (t as TelegramTransportStatus).SegmentID == PLC_ID),
                        DispatchTo = OnTelegramTransportStatus
                    });
*/
            }
            catch (Exception ex)
            {
                Warehouse.AddEvent(Event.EnumSeverity.Error, Event.EnumType.Exception, ex.Message);
                throw new SegmentException(String.Format("{0} SegmentMainPanel.Initialize failed ", Name));
            }
        }

    }
}

