using System;
using System.Collections.Generic;
using System.Linq;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.CommandWpf;
using Warehouse.Model;
using Warehouse.ConveyorUnits;
using UserInterface.UserControls;
using System.Diagnostics;
using Database;
using WCFClients;
using GalaSoft.MvvmLight.Messaging;
using UserInterface.Messages;
using Telegrams;

namespace UserInterface.ViewModel
{
    public class SegmentStatus
    {
        public bool None { get; set; }
        public bool Offline {get; set;}
        public bool Alarm { get; set; }
        public bool LongTermBlock { get; set; }
        public bool AutoRun { get; set; }
        public bool Remote { get; set; }
        public bool Local { get; set; }        

        public SegmentStatus()
        {
            None = true;
            Offline = false;
            Alarm = false;
            LongTermBlock = false;
            AutoRun = false;
            Remote = false;
            Local = false;
        }
    }

    public class ControlPanelSegmentsViewModel: ViewModelBase
    {
        #region members
        private BasicWarehouse _warehouse;
        private string _UCName;
        private string _deviceNames;
        private DeviceStateEnum _state;
        private List<Segment> _segmentList;
        private Dictionary<string, SegmentStatus> _segmentStatusDict;
        private int _accessLevel;
        #endregion

        #region properties

        public string UCName
        {
            get { return _UCName; }
            set
            {
                if (_UCName != value)
                {
                    _UCName = value;
                    RaisePropertyChanged("UCName");
                }
            }
        }
        public string DeviceNames
        {
            get { return _deviceNames; }
            set
            {
                if (_deviceNames != value)
                {
                    _deviceNames = value;
                    RaisePropertyChanged("DeviceNames");
                }
            }
        }
        public DeviceStateEnum State
        {
            get { return _state; }
            set
            {
                if (_state != value)
                {
                    _state = value;
                    RaisePropertyChanged("State");
                }
            }
        }
        public int AccessLevel
        {
            get
            {
                return _accessLevel;
            }
            set
            {
                if (_accessLevel != value)
                {
                    _accessLevel = value;
                    RaisePropertyChanged("AccessLevel");
                }
            }
        }
        public RelayCommand<DeviceCommandEnum> Command { get; set; }
        #endregion

        #region intialization
        public void Initialize(BasicWarehouse warehouse)
        {
            _warehouse = warehouse;
            try
            {
                List<string> devices = DeviceNames.Split('|').ToList();

                if(_segmentList.Count == 0)
                    devices.ForEach(p =>
                    {
                        _warehouse.Segment[p].NotifyVM.Add(new Action<ConveyorBasicInfo>(pp => OnDataChange(pp, p)));
                        _segmentList.Add(_warehouse.Segment[p]);
                        _segmentStatusDict.Add(p, new SegmentStatus());
                    });

                Messenger.Default.Register<MessageAccessLevel>(this, (mc) => { AccessLevel = mc.AccessLevel; });
            }
            catch (Exception e)
            {
                _warehouse.AddEvent(Database.Event.EnumSeverity.Error, Database.Event.EnumType.Exception, e.Message);
                throw new Exception(string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
            }
        }
        public ControlPanelSegmentsViewModel()
        {
            try
            {
                _segmentList = new List<Segment>();
                _segmentStatusDict = new Dictionary<string, SegmentStatus>();
                Command = new RelayCommand<DeviceCommandEnum>((p) => ExecuteCommand(p));
            }
            catch (Exception e)
            {
                throw new Exception(string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
            }
        }
        #endregion

        #region commands
        private void ExecuteCommand(DeviceCommandEnum dc)
        {
            try
            {
                switch (dc)
                {
                    case DeviceCommandEnum.Refresh:
                        _segmentList.ForEach((s) => (_warehouse.WCFClient as WCFUIClient).NotifyUIClient.Info(s.Name));
                        break;
                    case DeviceCommandEnum.Reset:
                        _segmentList.ForEach((s) => (_warehouse.WCFClient as WCFUIClient).NotifyUIClient.Reset(s.Name));
                        break;
                    case DeviceCommandEnum.AutoOn:
                        _segmentList.ForEach((s) => (_warehouse.WCFClient as WCFUIClient).NotifyUIClient.AutoOn(s.Name));
                        break;
                    case DeviceCommandEnum.AutoOff:
                        _segmentList.ForEach((s) => (_warehouse.WCFClient as WCFUIClient).NotifyUIClient.AutoOff(s.Name));
                        break;
                    default:
                        break;
                }
            }
            catch (Exception e)
            {
                _warehouse.AddEvent(Event.EnumSeverity.Error, Event.EnumType.Exception,
                                    string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
            }

        }

        public void OnDataChange(ConveyorBasicInfo info, string name)
        {
            bool none = false;
            bool noComm = false;
            bool notLongTermBlock = true;
            bool alarm = false;
            bool autoRun = true;
            bool remote = true;
            bool local;

            try
            {
                var ss = _segmentStatusDict[name];
                ss.None = false;


                if (info is ConveyorInfo)
                {
                    ss.Offline = !(info as ConveyorInfo).Online;
                    if ((info as ConveyorInfo).Status != null)
                    {
                        ss.LongTermBlock = (info as ConveyorInfo).Status[TelegramTransportStatus.STATUS_LONGTERMFAULT];
                        ss.Alarm = (info as ConveyorInfo).Status[TelegramTransportStatus.STATUS_FAULT];
                        ss.AutoRun = (info as ConveyorInfo).Status[TelegramTransportStatus.STATUS_AUTOMATIC];
                        ss.Remote = (info as ConveyorInfo).Status[TelegramTransportStatus.STATUS_REMOTE];
                    }
                }
                else if (info is CraneInfo)
                {
                    ss.Offline = !(info as CraneInfo).Online;
                    if ((info as CraneInfo).Status != null)
                    {
                        ss.LongTermBlock = (info as CraneInfo).Status[TelegramCraneStatus.STATUS_LONGTERMBLOCK];
                        ss.Alarm = (info as CraneInfo).Status[TelegramCraneStatus.STATUS_FAULT];
                        ss.AutoRun = (info as CraneInfo).Status[TelegramCraneStatus.STATUS_AUTOMATIC];
                        ss.Remote = (info as CraneInfo).Status[TelegramCraneStatus.STATUS_REMOTE];
                    }
                }

                foreach (KeyValuePair<string, SegmentStatus> kvp in _segmentStatusDict)
                {
                    none |= kvp.Value.None;
                    noComm |= kvp.Value.Offline;
                    notLongTermBlock &= !kvp.Value.LongTermBlock;
                    alarm |= kvp.Value.Alarm;
                    autoRun &= kvp.Value.AutoRun;
                    remote &= kvp.Value.Remote;
                };
                local = !(noComm || notLongTermBlock || alarm || remote || autoRun || none);

                if (noComm)
                    State = DeviceStateEnum.Offline;
                else if (!notLongTermBlock)
                    State = DeviceStateEnum.LongTermBlock;
                else if (!remote)
                    State = DeviceStateEnum.Local;
                else if (alarm)
                    State = DeviceStateEnum.Alarm;
                else if (autoRun && remote)
                    State = DeviceStateEnum.AutoRun;
                else
                    State = DeviceStateEnum.Remote;
            }
            catch (Exception e)
            {
                _warehouse.AddEvent(Event.EnumSeverity.Error, Event.EnumType.Exception, 
                                    string.Format("{0}.{1}: {2} ({3})", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message, name));
            }
        }
        #endregion
    }
}
