using Database;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using Telegrams;
using UserInterface.Messages;
using UserInterface.Services;
using UserInterface.UserControls;
using Warehouse.ConveyorUnits;
using Warehouse.Model;
using WCFClients;

namespace UserInterface.ViewModel
{
    public class VisuSegmentViewModel : VisuDeviceBasicViewModel
    {
        #region members
        private DeviceStateEnum _state;
        ConveyorBasicInfo _info;
        private int _accessLevel;
        #endregion

        #region properties
        public RelayCommand<DeviceCommandEnum> Command { get; set; }
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
        #endregion

        #region initialization


        public void InitializeDetails()
        {
            try
            {
                DetailsAddOrUpdate("MODE", 0, "Mode", "", "");
            }
            catch (Exception e)
            {
                _warehouse.AddEvent(Database.Event.EnumSeverity.Error, Database.Event.EnumType.Exception, e.Message);
                throw new Exception(string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
            }
        }

        public override void Initialize(BasicWarehouse warehouse)
        {
            try
            {
                base.Initialize(warehouse);

                Model = _warehouse.SegmentList.FirstOrDefault(p => p.Name == DeviceName);
                if (Model != null)
                {
                    (Model as Segment).NotifyVM.Add(new Action<ConveyorBasicInfo>(OnDataChange));
                    Command = new RelayCommand<DeviceCommandEnum>((p) => ExecuteCommand(p, (Segment)Model));
                }
                InitializeDetails();
                Messenger.Default.Register<MessageLanguageChanged>(this, (mc) => ExecuteLanguageChanged(mc.Culture));
                Messenger.Default.Register<MessageAccessLevel>(this, (mc) => { AccessLevel = mc.AccessLevel; });
            }
            catch (Exception e)
            {
                _warehouse.AddEvent(Database.Event.EnumSeverity.Error, Database.Event.EnumType.Exception, e.Message);
                throw new Exception(string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
            }
        }
        public VisuSegmentViewModel()
        {
            try
            {
            }
            catch (Exception e)
            {
                throw new Exception(string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
            }
        }
        #endregion

        #region commands
        private void ExecuteCommand(DeviceCommandEnum dc, Segment s)
        {
            try
            {
                switch (dc)
                {
                    case DeviceCommandEnum.Refresh:
                        (_warehouse.WCFClient as WCFUIClient).NotifyUIClient.Info(s.Name);
                        break;
                    case DeviceCommandEnum.Reset:
                        (_warehouse.WCFClient as WCFUIClient).NotifyUIClient.Reset(s.Name);
                        break;
                    case DeviceCommandEnum.AutoOn:
                        (_warehouse.WCFClient as WCFUIClient).NotifyUIClient.AutoOn(s.Name);
                        break;
                    case DeviceCommandEnum.AutoOff:
                        (_warehouse.WCFClient as WCFUIClient).NotifyUIClient.AutoOff(s.Name);
                        break;
                    case DeviceCommandEnum.SetTime:
                        (_warehouse.WCFClient as WCFUIClient).NotifyUIClient.SetClock(s.Name);
                        break;
                    case DeviceCommandEnum.LongTermBlockOn:
                        (_warehouse.WCFClient as WCFUIClient).NotifyUIClient.LongTermBlockOn(s.Name);
                        var cl1 = _warehouse.ConveyorList.FindAll(p => p.Segment == (Model as Segment).Name);
                        foreach (var c in cl1)
                        {
                            PlaceID p = _warehouse.DBService.FindPlaceID(c.Name);
                            if (p != null)
                            {
                                p.Blocked = true;
                                _warehouse.DBService.UpdateLocation(p);
                            }
                        }
                        break;
                    case DeviceCommandEnum.LongTermBlockOff:
                        (_warehouse.WCFClient as WCFUIClient).NotifyUIClient.LongTermBlockOff(s.Name);
                        var cl2 = _warehouse.ConveyorList.FindAll(p => p.Segment == (Model as Segment).Name);
                        foreach (var c in cl2)
                        {
                            PlaceID p = _warehouse.DBService.FindPlaceID(c.Name);
                            if (p != null)
                            {
                                p.Blocked = false;
                                _warehouse.DBService.UpdateLocation(p);
                            }
                        }
                        break;
                    default:
                        break;
                }
            }
            catch (Exception e)
            {
                _warehouse.AddEvent(Database.Event.EnumSeverity.Error, Database.Event.EnumType.Exception, e.Message);
                throw new Exception(string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
            }
        }

        public void RefreshDetails()
        {
            try
            {
                if (_info == null)
                    return;

                if (!_info.Online)
                    State = DeviceStateEnum.Offline;
                else if (_info.Status[TelegramTransportStatus.STATUS_LONGTERMFAULT])
                    State = DeviceStateEnum.LongTermBlock;
                else if (!_info.Status[TelegramTransportStatus.STATUS_REMOTE])
                    State = DeviceStateEnum.Local;
                else if (_info.Status[TelegramTransportStatus.STATUS_FAULT])
                    State = DeviceStateEnum.Alarm;
                else if (_info.Status[TelegramTransportStatus.STATUS_REMOTE] && 
                         _info.Status[TelegramTransportStatus.STATUS_AUTOMATIC])
                    State = DeviceStateEnum.AutoRun;
                else
                    State = DeviceStateEnum.Remote;

                DetailsValueUpdate("MODE", _info.Status != null ? 
                    StatusToString(_info.Online, 
                                   _info.Status[TelegramTransportStatus.STATUS_REMOTE], 
                                   _info.Status[TelegramTransportStatus.STATUS_FAULT], 
                                   _info.Status[TelegramTransportStatus.STATUS_AUTOMATIC], 
                                   _info.Status[TelegramTransportStatus.STATUS_LONGTERMFAULT]) : "");
            }
            catch (Exception e)
            {
                _warehouse.AddEvent(Database.Event.EnumSeverity.Error, Database.Event.EnumType.Exception,
                                    string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
            }

        }
        public void OnDataChange(ConveyorBasicInfo info)
        {
            try
            {
                _info = info;

                RefreshDetails();
            }
            catch (Exception e)
            {
                _warehouse.AddEvent(Database.Event.EnumSeverity.Error, Database.Event.EnumType.Exception, 
                                    string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
            }
        }
        #endregion

        #region functions
        public void ExecuteLanguageChanged(CultureInfo ci)
        {
            InitializeDetails();
            RefreshDetails();
        }
        #endregion


    }
}
