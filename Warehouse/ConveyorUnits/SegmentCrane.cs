using System;
using System.Collections.Generic;
using System.Linq;
using Telegrams;
using Database;
using Warehouse.Model;
using MFCS.Communication;

namespace Warehouse.ConveyorUnits
{



    public class SegmentCrane : Segment
    {
        private List<Crane> SegmentCranes { get; set; } 
        public CraneInfo SegmentInfo { get; set; }

        public override void OnlineTrigger()
        {
            if (Communicator.Online() && !_oldOnline)
            {
                Startup();
                DirectVMNotify();
                SegmentCranes.ForEach(prop => prop.DirectVMNotify());
            }
            if (!Communicator.Online() && _oldOnline)
            {
                DirectVMNotify();
                SegmentCranes.ForEach(prop => prop.DirectVMNotify());
            }
            _oldOnline = Communicator.Online();
        }

        public void CancelAllCranes(int id)
        {
            foreach (Crane c in SegmentCranes)
            {
                Communicator.AddSendTelegram(new TelegramCraneTO
                {
                    MFCS_ID = id++,
                    ID = (short)PLC_ID,
                    Sender = Communicator.MFCS_ID,
                    Receiver = Communicator.PLC_ID,
                    Order = TelegramCraneTO.ORDER_CANCEL,
                });
                Warehouse.AddEvent(Database.Event.EnumSeverity.Event, Database.Event.EnumType.Program, $"Blind command cancel for {c.Name}");
            }
        }

        public override void Startup()
        {
            Info(0);
            CancelAllCranes(0);
        }

        public override void Refresh()
        {
            base.Refresh();
            if (!UINotified)
            {
                SegmentCranes.ForEach(prop => prop.DirectVMNotify());
                DirectVMNotify();
                UINotified = true;    
            }
        }

        public override void AutomaticOn(Int32 id)
        {
            Communicator.AddSendTelegram(new TelegramCraneStatus
            {
                MFCS_ID = id,
                ID = (short)PLC_ID,
                Sender = Communicator.MFCS_ID,
                Receiver = Communicator.PLC_ID,
                Order = TelegramTransportStatus.ORDER_AUTOON
            });
            Warehouse.AddEvent(Database.Event.EnumSeverity.Event, Database.Event.EnumType.Program, String.Format("Segment {0} AutomaticOn called.", Name));
        }

        public override void AutomaticOff(Int32 id)
        {
            Communicator.AddSendTelegram(new TelegramCraneStatus
            {
                MFCS_ID = id,
                ID = (short)PLC_ID,
                Sender = Communicator.MFCS_ID,
                Receiver = Communicator.PLC_ID,
                Order = TelegramTransportStatus.ORDER_AUTOOFF
            });
            Warehouse.AddEvent(Database.Event.EnumSeverity.Event, Database.Event.EnumType.Program, String.Format("Segment {0} AutomaticOff called.", Name));
        }

        public override void Info(Int32 id)
        {
            Communicator.AddSendTelegram(new TelegramCraneStatus
            {
                MFCS_ID = id,
                ID = (short)PLC_ID,
                Sender = Communicator.MFCS_ID,
                Receiver = Communicator.PLC_ID,
                Order = TelegramTransportStatus.ORDER_INFO
            });
            Warehouse.AddEvent(Database.Event.EnumSeverity.Event, Database.Event.EnumType.Program, String.Format("Segment {0} refresh called.", Name));
        }

        public override void AlarmRequest(Int32 id)
        {
            Communicator.AddSendTelegram(new TelegramCraneStatus
            {
                MFCS_ID = id,
                ID = (short)PLC_ID,
                Sender = Communicator.MFCS_ID,
                Receiver = Communicator.PLC_ID,
                Order = TelegramTransportStatus.ORDER_ALARM
            });
            Warehouse.AddEvent(Database.Event.EnumSeverity.Event, Database.Event.EnumType.Program, String.Format("Segment {0} alarm request called.", Name));
        }
        public override void Reset(Int32 id)
        {
            Communicator.AddSendTelegram(new TelegramCraneStatus
            {
                MFCS_ID = id,
                ID = (short)PLC_ID,
                Sender = Communicator.MFCS_ID,
                Receiver = Communicator.PLC_ID,
                Order = TelegramTransportStatus.ORDER_RESET
            });
            SegmentInfo.ActiveAlarms.ForEach(aa => Warehouse.DBService.UpdateAlarm(Name, aa.ToString(), Alarm.EnumAlarmStatus.Ack) );
            foreach (var c in SegmentCranes)
                c.CraneInfo.ActiveAlarms.ForEach(aa => Warehouse.DBService.UpdateAlarm(c.Name, aa.ToString(), Alarm.EnumAlarmStatus.Ack) );
            Warehouse.AddEvent(Database.Event.EnumSeverity.Event, Database.Event.EnumType.Program, String.Format("Segment {0} reset called.", Name));
        }
        public override void LongTermBlockOn(Int32 id)
        {
            Communicator.AddSendTelegram(new TelegramCraneStatus
            {
                MFCS_ID = id,
                ID = (short)PLC_ID,
                Sender = Communicator.MFCS_ID,
                Receiver = Communicator.PLC_ID,
                Order = TelegramTransportStatus.ORDER_LONGTERMBLOCKON
            });
            SegmentInfo.ActiveAlarms.ForEach(aa => Warehouse.DBService.UpdateAlarm(Name, aa.ToString(), Alarm.EnumAlarmStatus.Ack));
            foreach (var c in SegmentCranes)
                c.CraneInfo.ActiveAlarms.ForEach(aa => Warehouse.DBService.UpdateAlarm(c.Name, aa.ToString(), Alarm.EnumAlarmStatus.Ack));
            Warehouse.AddEvent(Database.Event.EnumSeverity.Event, Database.Event.EnumType.Program, String.Format("Segment {0} LongTermBlockOn called.", Name));
        }
        public override void LongTermBlockOff(Int32 id)
        {
            Communicator.AddSendTelegram(new TelegramCraneStatus
            {
                MFCS_ID = id,
                ID = (short)PLC_ID,
                Sender = Communicator.MFCS_ID,
                Receiver = Communicator.PLC_ID,
                Order = TelegramTransportStatus.ORDER_LONGTERMBLOCKOFF
            });
            SegmentInfo.ActiveAlarms.ForEach(aa => Warehouse.DBService.UpdateAlarm(Name, aa.ToString(), Alarm.EnumAlarmStatus.Ack));
            foreach (var c in SegmentCranes)
                c.CraneInfo.ActiveAlarms.ForEach(aa => Warehouse.DBService.UpdateAlarm(c.Name, aa.ToString(), Alarm.EnumAlarmStatus.Ack));
            Warehouse.AddEvent(Database.Event.EnumSeverity.Event, Database.Event.EnumType.Program, String.Format("Segment {0} LongTermBlockOff called.", Name));
        }

        public override void SetClock(Int32 id)
        {
            Communicator.AddSendTelegram(
                new TelegramCraneStatus
                {
                    MFCS_ID = id,
                    ID = (short)PLC_ID,
                    Sender = Communicator.MFCS_ID,
                    Receiver = Communicator.PLC_ID,
                    Order = TelegramCraneStatus.ORDER_SETDATETIME,
                    LPosition = new Position { R = (short)DateTime.Now.Year, X = (short)DateTime.Now.Month, Y = (short)DateTime.Now.Day },
                    FPosition = new Position { R = (short)DateTime.Now.Hour, X = (short)DateTime.Now.Minute, Y = (short)DateTime.Now.Second }
                });
            Warehouse.AddEvent(Database.Event.EnumSeverity.Event, Database.Event.EnumType.Program, String.Format("Segment {0} SetClock called.", Name));
        }

        public override void DirectVMNotify()
        {
            if (SegmentInfo != null && PLC_Status != null)
            {
                TelegramCraneStatus tel = PLC_Status as TelegramCraneStatus;
                SegmentInfo.Name = Name;
                SegmentInfo.LPosition = tel.LPosition;
                SegmentInfo.FPosition = tel.FPosition;
                SegmentInfo.StateMachine = tel.StateMachine;
                SegmentInfo.AlarmID = tel.AlarmID;
                SegmentInfo.Fault = tel.Fault;
                SegmentInfo.SetAlarms(tel.CurrentAlarms, Warehouse);
                SegmentInfo.SetSensors(tel.Status);
                SegmentInfo.Status = tel.Status;
                SegmentInfo.Online = Communicator.Online();
                CallNotifyVM(SegmentInfo);
            }
        }

        public override void OnRecTelegram(Telegram t)
        {
            if (t is TelegramCraneStatus)
                OnTelegramCraneStatus(t);
        }

        public void OnTelegramCraneStatus(Telegram t)
        {
            try
            {
                SegmentCranes.ForEach(prop => prop.OnTelegramCraneStatus(t));
                if (t != null)
                    PLC_Status = t;
                DirectVMNotify();
            }
            catch (Exception ex)
            {
                Warehouse.AddEvent(Event.EnumSeverity.Error, Event.EnumType.Exception, ex.Message);
                throw new SegmentException(String.Format("{0} SegmentCrane.OnTelegramCraneStatus failed.", Name));
            }
        }

        public override void Initialize(BasicWarehouse w)
        {
            try
            {
                base.Initialize(w);
                SegmentCranes = (from c in Warehouse.CraneList
                                 where c.Segment == Name
                                 select c).ToList();
            }
            catch (Exception ex)
            {
                Warehouse.AddEvent(Event.EnumSeverity.Error, Event.EnumType.Exception, ex.Message);
                throw new SegmentException(String.Format("{0} SegmentCrane.Initialize failed ", Name));
            }
        }



    }
}
