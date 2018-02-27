using System;
using System.Linq;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Ioc;
using GalaSoft.MvvmLight.Messaging;
using System.Collections.ObjectModel;
using UserInterface.Messages;
using System.Windows.Threading;
using System.Windows;
using Warehouse.Model;
using Database;
using System.Diagnostics;
using WCFClients;
using System.Threading;
using SimpleLog;

namespace UserInterface.ViewModel
{
    public class ViewModelBaseExtended : ViewModelBase
    {
        #region members
        public Visibility _visible = Visibility.Hidden;
        #endregion

        #region properties
        public ViewModelBase View { get; set; }
        public virtual Visibility Visible
        {
            get { return _visible; }
            set
            {
                if (_visible != value)
                {
                    _visible = value;
                    RaisePropertyChanged("Visible");
                }
            }
        }
        #endregion
    }
    public sealed class MainViewModel : ViewModelBase
    {
        #region members
        private ObservableCollection<ViewModelBaseExtended> _viewModel;
        private ViewModelBase _controlPanelViewModel;
        private int _accessLevel;
        private bool _enabledAccessL1;
        private bool _enabledAccessL2;
        private string _currentTime;
        private string _user;
        private DispatcherTimer _timer;
        #endregion

        #region properties
        public BasicWarehouse Warehouse { get; set; }
        public RelayCommand OnLoaded { get; private set; }
        public RelayCommand OnClose { get; private set; }
        public RelayCommand<object> TreeviewSelectedItemChanged { get; private set; }

        public int XAMLViewModelsToLoad { get; set;}

        public ObservableCollection<ViewModelBaseExtended> ViewModel
        {
            get { return _viewModel; }
            set
            {
                if (_viewModel != value)
                {
                    _viewModel = value;
                    RaisePropertyChanged("ViewModel");
                }
            }
        }

        public ViewModelBase ControlPanelViewModel
        {
            get
            {
                return _controlPanelViewModel;
            }
            set
            {
                if (_controlPanelViewModel != value)
                {
                    _controlPanelViewModel = value;
                    RaisePropertyChanged("ControlPanelViewModel");
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
                    try
                    {
                        if (_accessLevel == 1 || _accessLevel == 2)
                            EnabledAccessL1 = true;
                        else
                            EnabledAccessL1 = false;
                        if (_accessLevel == 2)
                            EnabledAccessL2 = true;
                        else
                            EnabledAccessL2 = false;
                    }
                    catch (Exception e)
                    {
                        throw new Exception(string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
                    }
                }
            }
        }
        public bool EnabledAccessL1
        {
            get
            {
                return _enabledAccessL1;
            }
            set
            {
                if (_enabledAccessL1 != value)
                {
                    _enabledAccessL1 = value;
                    RaisePropertyChanged("EnabledAccessL1");
                }
            }
        }
        public bool EnabledAccessL2
        {
            get
            {
                return _enabledAccessL2;
            }
            set
            {
                if (_enabledAccessL2 != value)
                {
                    _enabledAccessL2 = value;
                    RaisePropertyChanged("EnabledAccessL2");
                }
            }
        }

        public string CurrentTime
        {
            get
            {
                return this._currentTime;
            }
            set
            {
                if (_currentTime != value)
                {
                    _currentTime = value;
                    RaisePropertyChanged("CurrentTime");
                }
            }
        }
        public string User
        {
            get
            {
                return this._user;
            }
            set
            {
                if (_user != value)
                {
                    _user = value;
                    RaisePropertyChanged("User");
                }
            }
        }
        #endregion

        #region initialization
        public void Initialize()
        {
            try
            {
                // initialize warehouse
                Warehouse = (BasicWarehouse)BasicWarehouse.Deserialize(System.Configuration.ConfigurationManager.AppSettings["xmlconfig"]);
                Warehouse.DBService = new Warehouse.DataService.DBService(Warehouse);

                // create view models - events first
                ViewModel.Add(new ViewModelBaseExtended { View = SimpleIoc.Default.GetInstance<EventsViewModel>(), Visible = Visibility.Hidden });    // must be the first
                ((EventsViewModel)ViewModel[0].View).Initialize(Warehouse);

                Warehouse.OnNewEvent.Add(((EventsViewModel)(ViewModel[0].View)).AddEvent);
                Warehouse.Initialize();
                Warehouse.BuildRoutes(true);

                //                Warehouse.WCFHost = new Warehouse.WCF.WCFHost();
                //                Warehouse.WCFHost.Start(Warehouse, typeof(TelegramService));

                XAMLViewModelsToLoad = 2;    // visualization, control panel
                Messenger.Default.Register<MessageLoadingCompleted>(this, m => StartCommunication());

                ViewModel.Add(new ViewModelBaseExtended { View = SimpleIoc.Default.GetInstance<SettingsViewModel>(), Visible = Visibility.Hidden });
                ViewModel.Add(new ViewModelBaseExtended { View = SimpleIoc.Default.GetInstance<LocationsViewModel>(), Visible = Visibility.Hidden });
                ViewModel.Add(new ViewModelBaseExtended { View = SimpleIoc.Default.GetInstance<MaterialsViewModel>(), Visible = Visibility.Hidden });
                ViewModel.Add(new ViewModelBaseExtended { View = SimpleIoc.Default.GetInstance<SimpleCommandsViewModel>(), Visible = Visibility.Hidden });
                ViewModel.Add(new ViewModelBaseExtended { View = SimpleIoc.Default.GetInstance<CommandsViewModel>(), Visible = Visibility.Hidden });
                ViewModel.Add(new ViewModelBaseExtended { View = SimpleIoc.Default.GetInstance<AlarmsViewModel>(), Visible = Visibility.Hidden });
                ViewModel.Add(new ViewModelBaseExtended { View = SimpleIoc.Default.GetInstance<VisualizationViewModel>(), Visible = Visibility.Visible });
                ViewModel.Add(new ViewModelBaseExtended { View = SimpleIoc.Default.GetInstance<HistoryEventsViewModel>(), Visible = Visibility.Hidden });
                ViewModel.Add(new ViewModelBaseExtended { View = SimpleIoc.Default.GetInstance<HistoryAlarmsViewModel>(), Visible = Visibility.Hidden });
                ViewModel.Add(new ViewModelBaseExtended { View = SimpleIoc.Default.GetInstance<HistoryMovementsViewModel>(), Visible = Visibility.Hidden });
                ViewModel.Add(new ViewModelBaseExtended { View = SimpleIoc.Default.GetInstance<HistoryCommandsViewModel>(), Visible = Visibility.Hidden });
                ViewModel.Add(new ViewModelBaseExtended { View = SimpleIoc.Default.GetInstance<HistorySimpleCommandsViewModel>(), Visible = Visibility.Hidden });

                // intialize view models
                SimpleIoc.Default.GetInstance<SettingsViewModel>().Initialize(Warehouse);
                SimpleIoc.Default.GetInstance<LocationsViewModel>().Initialize(Warehouse);
                SimpleIoc.Default.GetInstance<MaterialsViewModel>().Initialize(Warehouse);
                SimpleIoc.Default.GetInstance<SimpleCommandsViewModel>().Initialize(Warehouse);
                SimpleIoc.Default.GetInstance<CommandsViewModel>().Initialize(Warehouse);
                SimpleIoc.Default.GetInstance<AlarmsViewModel>().Initialize(Warehouse);
                SimpleIoc.Default.GetInstance<VisualizationViewModel>().Initialize(Warehouse);
                SimpleIoc.Default.GetInstance<HistoryEventsViewModel>().Initialize(Warehouse);
                SimpleIoc.Default.GetInstance<HistoryAlarmsViewModel>().Initialize(Warehouse);
                SimpleIoc.Default.GetInstance<HistoryMovementsViewModel>().Initialize(Warehouse);
                SimpleIoc.Default.GetInstance<HistoryCommandsViewModel>().Initialize(Warehouse);
                SimpleIoc.Default.GetInstance<HistorySimpleCommandsViewModel>().Initialize(Warehouse);

                ControlPanelViewModel = SimpleIoc.Default.GetInstance<ControlPanelViewModel>();
                ((ControlPanelViewModel)ControlPanelViewModel).Initialize(Warehouse);

                Messenger.Default.Register<MessageAccessLevel>(this, (mc) => { this.AccessLevel = mc.AccessLevel; this.User = mc.User.ToUpper(); });

                SimpleIoc.Default.GetInstance<SettingsViewModel>().SetLanguage();
            }
            catch (Exception e)
            {
                Log.AddLog(Log.Severity.EXCEPTION, "MainViewModel", e.Message);
            }
        }       
        public void ExecuteOnLoaded()
        {
            Initialize();
        }

        public void ExecuteOnClose()
        {
            Warehouse.Dispose();
        }

        public MainViewModel()
        {
            try
            {
                ViewModel = new ObservableCollection<ViewModelBaseExtended>();

                OnLoaded = new RelayCommand(() => ExecuteOnLoaded());
                OnClose = new RelayCommand(() => ExecuteOnClose());
                TreeviewSelectedItemChanged = new RelayCommand<object>((param1) => ExecuteTreeviewSelectedItemChanged(param1));

                _timer = new DispatcherTimer(DispatcherPriority.Render){Interval = TimeSpan.FromSeconds(1)};
                _timer.Tick += (sender, args) => { CurrentTime = string.Format("{0} {1}", DateTime.Now.ToShortDateString(), DateTime.Now.ToLongTimeString()); };
                _timer.Start();
            }
            catch(Exception e)
            {
                throw new Exception(string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
            }
        }
        #endregion

        #region functions
        public void StartCommunication()
        {
            try
            {
                XAMLViewModelsToLoad--;
                if (XAMLViewModelsToLoad == 0)
                {
                    SimpleIoc.Default.GetInstance<SettingsViewModel>().SendAccessLevelAndUser(2, "admin");
                    Warehouse.WCFClient = new WCFUIClient();
                    Warehouse.WCFClient.Initialize(Warehouse);
                    Warehouse.StartCommunication();
                    Warehouse.AddEvent(Event.EnumSeverity.Event, Event.EnumType.Program, "Communication started.");
                }
            }
            catch (Exception e)
            {
                Warehouse.AddEvent(Event.EnumSeverity.Error, Event.EnumType.Exception,
                                   string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
            }
        }

        private void ExecuteTreeviewSelectedItemChanged(object o)
        {
            try
            {
                foreach (var v in ViewModel)
                    v.Visible = Visibility.Hidden;
                ViewModelBaseExtended vmb = ViewModel.FirstOrDefault(p => p.View == o);
                if (vmb != null)
                {
                    vmb.Visible = Visibility.Visible;
                    Messenger.Default.Send<MessageViewChanged>(new MessageViewChanged() { ViewModel = vmb.View });
                }
            }
            catch (Exception e)
            {
                Warehouse.AddEvent(Event.EnumSeverity.Error, Event.EnumType.Exception,
                                   string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
            }
        }
        #endregion

    }
}
