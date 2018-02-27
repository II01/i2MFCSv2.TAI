using Database;
using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using Warehouse.Model;
using MFCS.Communication;
using System.Windows.Data;
using System.Runtime.Serialization;
using Telegrams;

namespace Warehouse.ConveyorUnits
{

    [Serializable]
    public class SegmentException : Exception
    {
        public SegmentException(string s) : base(s)
        { }
    }


    [DataContract]
    public class SegmentInfo : ConveyorBasicInfo
    {
    }

    public abstract class Segment
    {
        [XmlIgnore]
        public BasicWarehouse Warehouse;
        [XmlAttribute]
        public string Name { get; set; }
        [XmlAttribute]
        public string CommunicatorName { get; set; }
        [XmlAttribute]
        public short PLC_ID { get; set; }
        [XmlIgnore]
        public BasicCommunicator Communicator { get; set; }
        protected bool _oldOnline;

        [XmlIgnore]
        public List<Action<ConveyorBasicInfo>> NotifyVM { get; set; }
        [XmlIgnore]
        public bool UINotified { get; set; }
        protected Telegram PLC_Status;
        private object _lock;


    

        public Segment()
        {
            NotifyVM = new List<Action<ConveyorBasicInfo>>();
            _lock = new object();
            BindingOperations.EnableCollectionSynchronization(NotifyVM, _lock);
            UINotified = false;
        }

        public abstract void OnRecTelegram(Telegram t);

        public virtual void OnlineTrigger()
        {
            if (Communicator.Online() && !_oldOnline)
            {
                // Startup();
                DirectVMNotify();
            }
            if (!Communicator.Online() && _oldOnline)
            {
                DirectVMNotify();
            }
            _oldOnline = Communicator.Online();
        }

        public virtual void Refresh()
        {
            try
            {
                OnlineTrigger();
//                if (Communicator.Online())
//                  ForceNotifyVM();
                Strategy();
            }
            catch (Exception ex)
            {
                Warehouse.AddEvent(Event.EnumSeverity.Error, Event.EnumType.Exception, ex.Message);
                throw new SegmentException(String.Format("{0} Segment.Refresh failed.", Name));
            }
        }

        public virtual void OnSimpleCommandFinish(SimpleSegmentCommand sc)
        {
            try
            {
                if (sc.Command_ID.HasValue)
                {
                    if (Warehouse.DBService.AllSimpleCommandWithCommandIDFinished(sc.Command_ID.Value))
                    {
                        Command cmd = Warehouse.DBService.FindCommandByID(sc.Command_ID.Value);
                        cmd.Status = Command.EnumCommandStatus.Finished;
                        Warehouse.DBService.UpdateCommand(cmd);
                        Warehouse.OnCommandFinish?.Invoke(cmd);
                    }
                }
            }
            catch (Exception ex)
            {
                Warehouse.AddEvent(Event.EnumSeverity.Error, Event.EnumType.Exception, ex.Message);
                throw new SegmentException(String.Format("{0} Segment.OnSimpleCommandFinish failed. {1}", Name, sc.ToString()));
            }
        }

        public virtual void Strategy()
        {
            try
            {
                var sc = Warehouse.DBService.FindSimpleSegmentCommand(Name);
                if (sc != null)
                {
                    switch (sc.Task)
                    {
                        case SimpleSegmentCommand.EnumTask.AutoOff:
                            AutomaticOff(sc.ID);
                            break;
                        case SimpleSegmentCommand.EnumTask.AutoOn:
                            AutomaticOn(sc.ID);
                            break;
                        case SimpleSegmentCommand.EnumTask.Reset:
                            Reset(sc.ID);
                            break;
                        case SimpleSegmentCommand.EnumTask.Info:
                            Info(sc.ID);
                            break;
                    }
                    sc.Status = SimpleCommand.EnumStatus.Finished;
                    Warehouse.DBService.UpdateSimpleCommand(sc);
                    OnSimpleCommandFinish(sc);
                }
            }
            catch (Exception ex)
            {
                Warehouse.AddEvent(Event.EnumSeverity.Error, Event.EnumType.Exception, ex.Message);
                throw new SegmentException(String.Format("{0} Segment.Strategy failed.", Name));
            }
        }

        public abstract void AutomaticOn(Int32 id);
        public abstract void AutomaticOff(Int32 id);

        public abstract void Info(Int32 id);
        public abstract void AlarmRequest(Int32 id);

        public abstract void Reset(Int32 id);

        public abstract void LongTermBlockOn(Int32 id);
        public abstract void LongTermBlockOff(Int32 id);

        public abstract void Startup();

        public abstract void SetClock(Int32 id);


        public void CallNotifyVM(ConveyorBasicInfo cbi)
        {
            List<Action<ConveyorBasicInfo>> notActive = new List<Action<ConveyorBasicInfo>>();

            for (int i = 0; i < NotifyVM.Count; i++)
                try
                {
                    NotifyVM[i].Invoke(cbi); // , null, null);
                }
                catch (Exception ex)
                {
                    notActive.Add(NotifyVM[i]);
                }
            notActive.ForEach(p => NotifyVM.Remove(p));
        }

        public virtual void Initialize(BasicWarehouse w)
        {
            try
            {
                Warehouse = w;
                Communicator = Warehouse.Communicator[CommunicatorName];
                Communicator.OnRefresh += Refresh;

            }
            catch (Exception ex)
            {
                Warehouse.AddEvent(Event.EnumSeverity.Error, Event.EnumType.Exception, ex.Message);
                throw new SegmentException(String.Format("{0} Segment.Initialize failed ", Name));
            }
        }

        public abstract void DirectVMNotify();

    }





}
