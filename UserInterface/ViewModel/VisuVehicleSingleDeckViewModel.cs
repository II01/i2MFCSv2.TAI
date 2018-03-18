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
using Warehouse.Strategy;
using WCFClients;

namespace UserInterface.ViewModel
{
    public sealed class VisuVehicleSingleDeckViewModel: VisuDeviceBasicViewModel
    {
        #region members
        private SegmentCrane _segment;
        private VisuConveyorViewModel _deck;
        private DeviceStateEnum _state;
        private string _location;
        private int _stateMachine;
        private string _task;
        private int _position;
        private CraneInfo _info;
        private int _accessLevel;
        #endregion

        #region properties
        public RelayCommand<DeviceCommandEnum> Command { get; set; }
        public string Strategy { get; set; }
        public VisuConveyorViewModel Deck
        {
            get { return _deck; }
            set
            {
                if (_deck != value)
                {
                    _deck = value;
                    RaisePropertyChanged("Deck");
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
        public string Location
        {
            get { return _location; }
            set
            {
                if (_location != value)
                {
                    _location = value;
                    RaisePropertyChanged("Location");
                }
            }
        }
        public int StateMachine
        {
            get { return _stateMachine; }
            set
            {
                if (_stateMachine != value)
                {
                    _stateMachine = value;
                    RaisePropertyChanged("StateMachine");
                }
            }
        }
        public string Task
        {
            get { return _task; }
            set
            {
                if (_task != value)
                {
                    _task = value;
                    RaisePropertyChanged("Task");
                }
            }
        }
        public int Position
        {
            get { return _position; }
            set
            {
                if (_position != value)
                {
                    _position = value;
                    RaisePropertyChanged("Position");
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
                DetailsAddOrUpdate("SM", 0, "State_machine", "", "");
                DetailsAddOrUpdate("POS", 0, "Positions", "", "");
                DetailsAddOrUpdate("POSL", 2, "Logical", "", "");
                DetailsAddOrUpdate("POSP", 2, "Physical", "", "");
                if (Deck.Model != null)
                {
                    DetailsAddOrUpdate("DECK", 0, (Deck.Model as Crane).Name, "", "");
                    DetailsAddOrUpdate("TU", 2, "TU", "", "");
                    DetailsAddOrUpdate("CMD", 2, "Tasks", "", "");
                    DetailsAddOrUpdate("CMDE", 4, "Execute", "", "");
                    DetailsAddOrUpdate("CMDB", 4, "Buffer", "", "");
                    DetailsAddOrUpdate("SENS", 2, "Sensors", "", "");
                    int i = 0;
                    if ((Deck.Model as Crane).CraneInfo != null && (Deck.Model as Crane).CraneInfo.SensorList != null)
                        (Deck.Model as Crane).CraneInfo.SensorList.ForEach(s => DetailsAddOrUpdate(string.Format("SENS_{0}", i++), 4, s.Description, s.Reference, false));
                }
            }
            catch(Exception e)
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

                StrategyCrane strategy = (StrategyCrane)_warehouse.StrategyList.FirstOrDefault(p => p.Name == Strategy);
                if (strategy != null)
                {
                    Deck.Model = _warehouse.Crane.ContainsKey(strategy.CraneName) ? _warehouse.Crane[strategy.CraneName] : null;
                    if(Deck.Model != null)
                    {
                        Deck.DeviceName = (Deck.Model as Crane).Name;
                        (Deck.Model as Crane).NotifyVM.Add(new Action<ConveyorBasicInfo>((p => OnDataChange(p as CraneInfo))));
                        _segment = (SegmentCrane)_warehouse.SegmentList.FirstOrDefault(p => p.Name == (Deck.Model as Crane).Segment);
                    }
                    InitializeDetails();
                    Command = new RelayCommand<DeviceCommandEnum>((p) => ExecuteCommand(p));
                    Messenger.Default.Register<MessageLanguageChanged>(this, (mc) => ExecuteLanguageChanged(mc.Culture));
                    Messenger.Default.Register<MessageAccessLevel>(this, (mc) => { AccessLevel = mc.AccessLevel; });
                }
            }
            catch (Exception e)
            {
                _warehouse.AddEvent(Database.Event.EnumSeverity.Error, Database.Event.EnumType.Exception, e.Message);
                throw new Exception(string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
            }
        }
        public VisuVehicleSingleDeckViewModel()
        {
            Deck = new VisuConveyorViewModel { };
        }
        #endregion

        #region functions

        public void RefreshDetails()
        {
            try
            {
                if (_info == null)
                    return;
                if (_info.Status == null)
                    throw new UIException("c.CraneInfo.Status is null");
                if (_info.FPosition == null)
                    throw new UIException("c.CraneInfo.FPosition is null");
                if (_info.LPosition == null)
                    throw new UIException("c.CraneInfo.LPosition is null");
                if (Deck.Model == null)
                    throw new UIException("Deck.Model is null");

                if (!_info.Online)
                    State = DeviceStateEnum.Offline;
                else if (_info.Status[TelegramCraneStatus.STATUS_LONGTERMBLOCK])
                    State = DeviceStateEnum.LongTermBlock;
                else if (!_info.Status[TelegramCraneStatus.STATUS_REMOTE])
                    State = DeviceStateEnum.Local;
                else if (_info.Status[TelegramCraneStatus.STATUS_FAULT])
                    State = DeviceStateEnum.Alarm;
                else if (_info.Status[TelegramCraneStatus.STATUS_REMOTE] && 
                         _info.Status[TelegramCraneStatus.STATUS_AUTOMATIC])
                    State = DeviceStateEnum.AutoRun;
                else
                    State = DeviceStateEnum.Remote;

                Position = _info.FPosition.X;
                Location = _info.LPosition.R == 0 ? string.Format("T{0:d3}", _info.LPosition.X) :
                                                    string.Format("W:{0:d2}:{1:d3}:{2:d1}:{3:d1}", _info.LPosition.R,   // project specific
                                                                                                   _info.LPosition.X,
                                                                                                   _info.LPosition.Y,
                                                                                                   _info.LPosition.Z);
                StateMachine = _info.StateMachine;
                Task = _info.LastCommand;

                Deck.TransportUnit = _info.Palette != null ? (int)_info.Palette.Barcode : 0;
                if (_info.SensorList != null)
                {
                    Deck.Sensor1Value = _info.SensorList.Count() > 0 ? _info.SensorList[0].Active : false;
                    Deck.Sensor2Value = _info.SensorList.Count() > 1 ? _info.SensorList[1].Active : false;
                    Deck.Sensor3Value = _info.SensorList.Count() > 2 ? _info.SensorList[2].Active : false;
                }

                DetailsValueUpdate("MODE", _info.Status != null ? 
                    StatusToString(_info.Online, 
                                   _info.Status[TelegramCraneStatus.STATUS_REMOTE], 
                                   _info.Status[TelegramCraneStatus.STATUS_FAULT], 
                                   _info.Status[TelegramCraneStatus.STATUS_AUTOMATIC], 
                                   _info.Status[TelegramCraneStatus.STATUS_LONGTERMBLOCK]) : "");
                DetailsValueUpdate("SM", _info.StateMachine.ToString());
                DetailsValueUpdate("POSL", Location);
                DetailsValueUpdate("POSP", _info.FPosition != null ? String.Format("X:{0} Y:{1} Z:{2}", _info.FPosition.X * 10, _info.FPosition.Y * 10, _info.FPosition.Z * 10) : "");
//                DetailsValueUpdate("TU", _info.Palette != null && _info.Palette.Barcode != 0 ? string.Format("P{0:d9}", _info.Palette.Barcode) : "");
                DetailsValueUpdate("TU", _info.Palette != null && _info.Palette.Barcode != 0 ? string.Format("{0:d9}", _info.Palette.Barcode) : "");
                DetailsValueUpdate("CMDE", _info.LastCommand);  //CraneCommandToString(info.LastCommand));
                DetailsValueUpdate("CMDB", _info.LastBufferCommand); // CraneCommandToString(info.LastBufferCommand));
                int i = 0;
                if (_info.SensorList != null)
                    _info.SensorList.ForEach(s => DetailsValueUpdate(string.Format("SENS_{0}", i++), s.Active));

                Deck.DeviceDetails = DeviceDetails;
            }
            catch (Exception e)
            {
                _warehouse.AddEvent(Database.Event.EnumSeverity.Error, Database.Event.EnumType.Exception, string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
            }
        }
        public void OnDataChange(CraneInfo info)
        {
            try
            {
                _info = info;

                RefreshDetails();
            }
            catch (Exception e)
            {
                _warehouse.AddEvent(Database.Event.EnumSeverity.Error, Database.Event.EnumType.Exception, string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
            }
        }

        private void ExecuteCommand(DeviceCommandEnum dc)
        {
            PlaceID pid;

            try
            {
                switch (dc)
                {
                    case DeviceCommandEnum.Refresh:
                        (_warehouse.WCFClient as WCFUIClient).NotifyUIClient.Info(_segment.Name);
                        break;
                    case DeviceCommandEnum.Reset:
                        (_warehouse.WCFClient as WCFUIClient).NotifyUIClient.Reset(_segment.Name);
                        break;
                    case DeviceCommandEnum.AutoOn:
                        (_warehouse.WCFClient as WCFUIClient).NotifyUIClient.AutoOn(_segment.Name);
                        break;
                    case DeviceCommandEnum.AutoOff:
                        (_warehouse.WCFClient as WCFUIClient).NotifyUIClient.AutoOff(_segment.Name);
                        break;
                    case DeviceCommandEnum.Home:
                        _warehouse.DBService.AddCommand(new CommandSegment
                        {
                            Task = Database.Command.EnumCommandTask.SegmentHome,
                            Segment = ((Crane)Deck.Model).Segment,
                            Status = Database.Command.EnumCommandStatus.NotActive,
                            Time = DateTime.Now                            
                        });
                        break;
                    case DeviceCommandEnum.SetTime:
                        (_warehouse.WCFClient as WCFUIClient).NotifyUIClient.SetClock(_segment.Name);
                        break;
                    case DeviceCommandEnum.LongTermBlockOn:
                        (_warehouse.WCFClient as WCFUIClient).NotifyUIClient.LongTermBlockOn(_segment.Name);
                        pid = _warehouse.DBService.FindPlaceID(((Crane)Deck.Model).Name);
                        pid.Blocked = true;
                        _warehouse.DBService.UpdateLocation(pid);
                        break;
                    case DeviceCommandEnum.LongTermBlockOff:
                        (_warehouse.WCFClient as WCFUIClient).NotifyUIClient.LongTermBlockOff(_segment.Name);
                        pid = _warehouse.DBService.FindPlaceID(((Crane)Deck.Model).Name);
                        pid.Blocked = false;
                        _warehouse.DBService.UpdateLocation(pid);
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
