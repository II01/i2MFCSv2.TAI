using Database;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.CommandWpf;
using GalaSoft.MvvmLight.Messaging;
using System;
using System.Diagnostics;
using UserInterface.Messages;
using UserInterface.Services;
using UserInterface.UserControls;
using Warehouse.Model;
using WCFClients;

namespace UserInterface.ViewModel
{
    public class ControlPanelModeViewModel : ViewModelBase
    {
        #region members
        private bool _stateWMS;
        private bool _stateAuto;
        private bool _stateRun;
        private string _commandSource;
        private string _commandType;
        private bool _commandTypeVisibility;
        private BasicWarehouse _warehouse;
        private int _accessLevel;
        #endregion

        #region properties
        public string UCName { get; set; }
        public RelayCommand<ModeCommandEnum> Command { get; set; }

        public bool StateWMS
        {
            get { return _stateWMS; }
            set
            {
                if (_stateWMS != value)
                {
                    _stateWMS = value;
                    RaisePropertyChanged("StateWMS");
                }
                if (StateWMS)
                    CommandSource = ResourceReader.GetString("WMS");
                else
                    CommandSource = ResourceReader.GetString("MFCS");
            }
        }
        public bool StateAuto
        {
            get { return _stateAuto; }
            set
            {
                if (_stateAuto != value)
                {
                    _stateAuto = value;
                    RaisePropertyChanged("StateAuto");
                }
                if (StateAuto)
                    CommandType = ResourceReader.GetString("Commands");
                else
                    CommandType = ResourceReader.GetString("Simple") + "\n" + ResourceReader.GetString("Commands");
            }
        }

        public string CommandSource
        {
            get { return _commandSource; }
            set
            {
                if (_commandSource != value)
                {
                    _commandSource = value;
                    RaisePropertyChanged("CommandSource");
                }
            }
        }

        public string CommandType
        {
            get { return _commandType; }
            set
            {
                if (_commandType != value)
                {
                    _commandType = value;
                    RaisePropertyChanged("CommandType");
                }
            }
        }

        public bool StateRun
        {
            get { return _stateRun; }
            set
            {
                if (_stateRun != value)
                {
                    _stateRun = value;
                    RaisePropertyChanged("StateRun");
                }
            }
        }

        public bool CommandTypeVisibility
        {
            get { return _commandTypeVisibility; }
            set
            {
                if (_commandTypeVisibility != value)
                {
                    _commandTypeVisibility = value;
                    RaisePropertyChanged("CommandTypeVisibility");
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

        public void Initialize(BasicWarehouse warehouse)
        {
            _warehouse = warehouse;
            _warehouse.SteeringCommands.SteeringNotify.Add(new Action<bool, bool, bool>(OnStateChange));
            Messenger.Default.Register<MessageViewChanged>(this, vm => ExecuteViewActivated(vm.ViewModel));

            StateWMS = false;
            StateAuto = true;
            StateRun = false;

            Messenger.Default.Register<MessageAccessLevel>(this, (mc) => { AccessLevel = mc.AccessLevel; });
            Messenger.Default.Register<MessageLanguageChanged>(this, (mc) => CommandSource = ResourceReader.GetString(StateWMS ? "WMS" : "MFCS"));
        }
        public ControlPanelModeViewModel()
        {
            Command = new RelayCommand<ModeCommandEnum>(p => ExecuteCommand(p));
        }
        #endregion

        #region functions

        private void OnStateChange(bool wms, bool auto, bool run)
        {
            try
            {
                StateWMS = wms;
                StateAuto = auto;
                StateRun = run;
            }
            catch (Exception e)
            {
                _warehouse.AddEvent(Event.EnumSeverity.Error, Event.EnumType.Exception,
                                    string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
            }
        }
        private void ExecuteCommand(ModeCommandEnum mc)
        {
            try
            {
                switch (mc)
                {
                    case ModeCommandEnum.ToggleWMS:
                        if (StateWMS)
                            (_warehouse.WCFClient as WCFUIClient).NotifyUIClient.SetMode(!StateWMS, StateAuto, StateRun);
                        else
                            (_warehouse.WCFClient as WCFUIClient).NotifyUIClient.SetMode(true, true, false);
                        break;
                    case ModeCommandEnum.ModeWMS:
                        (_warehouse.WCFClient as WCFUIClient).NotifyUIClient.SetMode(true, true, StateWMS && StateRun);
                        break;
                    case ModeCommandEnum.ModeMFCS:
                        (_warehouse.WCFClient as WCFUIClient).NotifyUIClient.SetMode(false, StateAuto, !StateWMS && StateRun);
                        break;
                    case ModeCommandEnum.ToggleAuto:
                        if(!StateWMS)
                            (_warehouse.WCFClient as WCFUIClient).NotifyUIClient.SetMode(StateWMS, !StateAuto, StateRun);
                        break;
                    case ModeCommandEnum.SetAuto:
                        (_warehouse.WCFClient as WCFUIClient).NotifyUIClient.SetMode(StateWMS, true, StateRun);
                        break;
                    case ModeCommandEnum.SetNotAuto:
                        if (!StateWMS)
                            (_warehouse.WCFClient as WCFUIClient).NotifyUIClient.SetMode(StateWMS, false, StateRun);
                        break;
                    case ModeCommandEnum.Start:
                        (_warehouse.WCFClient as WCFUIClient).NotifyUIClient.SetMode(StateWMS, StateAuto, true);
                        break;
                    case ModeCommandEnum.Stop:
                        (_warehouse.WCFClient as WCFUIClient).NotifyUIClient.SetMode(StateWMS, StateAuto, false);
                        break;
                }
            }
            catch (Exception e)
            {
                _warehouse.AddEvent(Event.EnumSeverity.Error, Event.EnumType.Exception,
                                    string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
            }
        }
        public void ExecuteViewActivated(ViewModelBase vm)
        {
            try
            {
                CommandTypeVisibility = (vm is SimpleCommandViewModel);
            }
            catch (Exception e)
            {
                _warehouse.AddEvent(Event.EnumSeverity.Error, Event.EnumType.Exception,
                                    string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
            }
        }
        #endregion
    }
}