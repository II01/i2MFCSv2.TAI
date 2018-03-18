using Database;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
    public sealed class VisuVehicleDoubleDeckViewModel: VisuDeviceBasicViewModel
    {
        #region members
        private ObservableCollection<VisuConveyorViewModel> _deck;
        private DeviceStateEnum _state;
        private string _location;
        private string _stateMachine;
        private ObservableCollection<string> _task;
        private List<List<DetailBasic>> _deckDetails;
        private int _position;
        private SegmentCrane _segment;
        private List<CraneInfo> _info;
        private int _accessLevel;
        #endregion

        #region properties
        public RelayCommand<DeviceCommandEnum> Command { get; set; }
        public string Strategy { get; set; }
        public ObservableCollection<VisuConveyorViewModel> Deck
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
        public string StateMachine
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
        public ObservableCollection<string> Task
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
                for(int d=0; d<2; d++)
                    if(Deck[d].Model != null)
                    {
                        DetailsAddOrUpdate(string.Format("DECK{0}", d), 0, (Deck[d].Model as Crane).Name, "", "");
                        DetailsAddOrUpdate(string.Format("TU{0}", d), 2, "TU", "", "");
                        DetailsAddOrUpdate(string.Format("CMD{0}", d), 2, "Tasks", "", "");
                        DetailsAddOrUpdate(string.Format("CMDE{0}", d), 4, "Execute", "", "");
                        DetailsAddOrUpdate(string.Format("CMDB{0}", d), 4, "Buffer", "", "");
                        DetailsAddOrUpdate(string.Format("SENS{0}", d), 2, "Sensors", "", "");
                        int i = 0;
                        if ((Deck[d].Model as Crane).CraneInfo != null && (Deck[d].Model as Crane).CraneInfo.SensorList != null)
                            (Deck[d].Model as Crane).CraneInfo.SensorList.ForEach(s => DetailsAddOrUpdate(string.Format("SENS{0}_{1}", d, i++), 4, s.Description, s.Reference, false));
                    }
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
                StrategyDoubleForkCrane strategy = (StrategyDoubleForkCrane)_warehouse.StrategyList.FirstOrDefault(p => p.Name == Strategy);
                if( strategy != null )
                {
                    Deck[0].Model = _warehouse.Crane.ContainsKey(strategy.Crane1Name) ? _warehouse.Crane[strategy.Crane1Name] : null;
                    if (Deck[0].Model != null)
                    {
                        Deck[0].DeviceName = (Deck[0].Model as Crane).Name;
                        (Deck[0].Model as Crane).NotifyVM.Add(new Action<ConveyorBasicInfo>((p => OnDataChange(p as CraneInfo, 0))));
                        _segment = (SegmentCrane)_warehouse.SegmentList.FirstOrDefault(p => p.Name == (Deck[0].Model as Crane).Segment);
                    }
                    Deck[1].Model = _warehouse.Crane.ContainsKey(strategy.Crane2Name) ? _warehouse.Crane[strategy.Crane2Name] : null;
                    if(Deck[1].Model != null)
                    {
                        Deck[1].DeviceName = (Deck[1].Model as Crane).Name;
                        (Deck[1].Model as Crane).NotifyVM.Add(new Action<ConveyorBasicInfo>((p => OnDataChange(p as CraneInfo, 1))));
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
        public VisuVehicleDoubleDeckViewModel()
        {
            Deck = new ObservableCollection<VisuConveyorViewModel>{new VisuConveyorViewModel{}, new VisuConveyorViewModel{}};
            Task = new ObservableCollection<string>{"", ""};
            _deckDetails = new List<List<DetailBasic>>
            {
                new List<DetailBasic>(),
                new List<DetailBasic>()
            };
            _info = new List<CraneInfo> { new CraneInfo(), new CraneInfo() };
        }
        #endregion

        #region functions

        public void RefreshDetails(int deck)
        {
            try
            {
                if (_info == null)
                    return;

                if (Deck[deck].Model != null)
                {
                    if (!_info[deck].Online || _info[deck].Status == null)
                        State = DeviceStateEnum.Offline;
                    else if (_info[deck].Status[TelegramCraneStatus.STATUS_LONGTERMBLOCK])
                        State = DeviceStateEnum.LongTermBlock;
                    else if(!_info[deck].Status[TelegramCraneStatus.STATUS_REMOTE])
                        State = DeviceStateEnum.Local;
                    else if (_info[deck].Status[TelegramCraneStatus.STATUS_FAULT])
                        State = DeviceStateEnum.Alarm;
                    else if (_info[deck].Status[TelegramCraneStatus.STATUS_REMOTE] && 
                             _info[deck].Status[TelegramCraneStatus.STATUS_AUTOMATIC])
                        State = DeviceStateEnum.AutoRun;
                    else 
                        State = DeviceStateEnum.Remote;

                    Position = _info[deck].FPosition != null ? _info[deck].FPosition.X : 0;
                    if (_info[deck].LPosition != null)
                        Location = _info[deck].LPosition.R == 0 ? String.Format("T{0:d3}", _info[deck].LPosition.X) :
                                                           String.Format("W:{0:d2}:{1:d3}:{2:d1}:{3:d1}", _info[deck].LPosition.R, _info[deck].LPosition.X, _info[deck].LPosition.Y, _info[deck].LPosition.Z);  // project specific
                    else
                        Location = "";
                    StateMachine = string.Format("{0}|{1}", _info[deck].StateMachine / 100, _info[deck].StateMachine % 100);

                    Task[deck] = _info[deck].LastCommand;

                    Deck[deck].TransportUnit = _info[deck].Palette != null ? (int)_info[deck].Palette.Barcode : 0;
                    if (_info[deck].SensorList != null)
                    {
                        Deck[deck].Sensor1Value = _info[deck].SensorList.Count() > 0 ? _info[deck].SensorList[0].Active : false;
                        Deck[deck].Sensor2Value = _info[deck].SensorList.Count() > 1 ? _info[deck].SensorList[1].Active : false;
                        Deck[deck].Sensor3Value = _info[deck].SensorList.Count() > 2 ? _info[deck].SensorList[2].Active : false;
                    }

                    DetailsValueUpdate("MODE", _info[deck].Status != null ? 
                        StatusToString(_info[deck].Online, 
                                       _info[deck].Status[TelegramCraneStatus.STATUS_REMOTE], 
                                       _info[deck].Status[TelegramCraneStatus.STATUS_FAULT], 
                                       _info[deck].Status[TelegramCraneStatus.STATUS_AUTOMATIC], 
                                       _info[deck].Status[TelegramCraneStatus.STATUS_LONGTERMBLOCK]) : "");
                    DetailsValueUpdate("SM", String.Format("{0}|{1}", _info[deck].StateMachine / 100, _info[deck].StateMachine % 100));
                    DetailsValueUpdate("POSL", Location);
                    DetailsValueUpdate("POSP", _info[deck].FPosition != null ? String.Format("X:{0} Y:{1} Z:{2}", _info[deck].FPosition.X * 10, _info[deck].FPosition.Y * 10, _info[deck].FPosition.Z * 10) : "");
                    if (Deck[deck].Model != null)
                    {
                        //                        DetailsValueUpdate(string.Format("TU{0}", deck), Deck[deck].TransportUnit != 0 ? string.Format("P{0:d9}", Deck[deck].TransportUnit) : "");
                        DetailsValueUpdate(string.Format("TU{0}", deck), Deck[deck].TransportUnit != 0 ? string.Format("{0:d9}", Deck[deck].TransportUnit) : "");
                        DetailsValueUpdate(string.Format("CMDE{0}", deck), _info[deck].LastCommand);
                        DetailsValueUpdate(string.Format("CMDB{0}", deck), _info[deck].LastBufferCommand);
                        int i = 0;
                        if (_info[deck] != null && _info[deck].SensorList != null)
                            _info[deck].SensorList.ForEach(s => DetailsValueUpdate(string.Format("SENS{0}_{1}", deck, i++), s.Active));
                    }
                    Deck[deck].DeviceDetails = DeviceDetails;
                }
            }
            catch (Exception e)
            {
                _warehouse.AddEvent(Database.Event.EnumSeverity.Error, Database.Event.EnumType.Exception,
                                    string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
            }
        }
        public void OnDataChange(CraneInfo info, int deck)
        {
            try
            {
                if(deck >= 0 && deck <= 1)
                {
                    _info[deck] = info;

                    RefreshDetails(deck);
                }
            }
            catch (Exception e)
            {
                _warehouse.AddEvent(Database.Event.EnumSeverity.Error, Database.Event.EnumType.Exception,
                                    string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
            }
        }

        private void ExecuteCommand(DeviceCommandEnum dc)
        {
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
                            Segment = ((Crane)Deck[0].Model).Segment,
                            Status = Database.Command.EnumCommandStatus.NotActive,
                            Time = DateTime.Now
                        });
                        break;
                    case DeviceCommandEnum.SetTime:
                        (_warehouse.WCFClient as WCFUIClient).NotifyUIClient.SetClock(_segment.Name);
                        break;
                    case DeviceCommandEnum.LongTermBlockOn:
                        (_warehouse.WCFClient as WCFUIClient).NotifyUIClient.LongTermBlockOn(_segment.Name);
                        for (int i =0; i< Deck.Count; i++)
                        {
                            PlaceID pid = _warehouse.DBService.FindPlaceID(((Crane)Deck[i].Model).Name);
                            pid.Blocked = true;
                            _warehouse.DBService.UpdateLocation(pid);
                        }
                        break;
                    case DeviceCommandEnum.LongTermBlockOff:
                        (_warehouse.WCFClient as WCFUIClient).NotifyUIClient.LongTermBlockOff(_segment.Name);
                        for (int i = 0; i < Deck.Count; i++)
                        {
                            PlaceID pid = _warehouse.DBService.FindPlaceID(((Crane)Deck[i].Model).Name);
                            pid.Blocked = false;
                            _warehouse.DBService.UpdateLocation(pid);
                        }
                        break;
                    default:
                        break;
                }
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
            RefreshDetails(0);
            RefreshDetails(1);
        }
        #endregion
    }
}
