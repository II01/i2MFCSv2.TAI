using Database;
using DatabaseWMS;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.CommandWpf;
using GalaSoft.MvvmLight.Messaging;
using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.Data.SqlTypes;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Threading;
using UserInterface.DataServiceWMS;
using UserInterface.Messages;
using UserInterface.ProxyWMS_UI;
using UserInterface.Services;
using Warehouse.Model;
using WCFClients;

namespace UserInterface.ViewModel
{
    public sealed class StationsViewModel : ViewModelBase
    {
        public enum CommandType { None = 0, ChangeHeightClass, ActivateOrder, CancelOrder, StoreTray, BringTray, RemoveTray, DropBox, BringBox, PickBox };

        #region members
        private CommandType _selectedCmd;
        private BasicWarehouse _warehouse;
        private DBServiceWMS _dbservicewms;
        private StationViewModel _operation;
        private ReleaseOrderViewModel _activeOrder;
        private ObservableCollection<ReleaseOrderViewModel> _dataListOrder;
        private ObservableCollection<ReleaseOrderViewModel> _dataListSubOrder;
        private ObservableCollection<CommandWMSViewModel> _dataListCommand;
        private ObservableCollection<TUSKUIDViewModel> _dataListBoxes;
        private ReleaseOrderViewModel _selectedOrder;
        private ReleaseOrderViewModel _selectedSubOrder;
        private ReleaseOrderViewModel _detailedOrder;
        private CommandWMSViewModel _selectedCommand;
        private DatabaseWMS.TU_ID _activeTUID;
        private bool _editEnabled;
        private bool _enabledCC;
        private int _accessLevel;
        private string _accessUser;
        private int? _suborderid;
        private int? _commandid;
        private string _bcrcommand = "";
        private bool _donotrefreshsuborder;
        private DispatcherTimer _timer;
        private ViewModelBase _activevm;
        private bool _visibleOperation;
        private bool _visibleBringTray;
        private bool _visibleRemoveTray;
        private bool _visibleBringBox;
        private bool _visiblePickBox;
        #endregion

        #region properites
        public RelayCommand RefreshSubOrder { get; private set; }
        public RelayCommand RefreshCommand { get; private set; }
        public RelayCommand CmdRefresh { get; private set; }
        public RelayCommand CmdStart { get; private set; }
        public RelayCommand CmdFinish { get; private set; }
        public RelayCommand CmdStop { get; private set; }
        public RelayCommand CmdChangeHC { get; private set; }
        public RelayCommand CmdStoreTray { get; private set; }
        public RelayCommand CmdBringTray { get; private set; }
        public RelayCommand CmdRemoveTray { get; private set; }
        public RelayCommand CmdDropBox { get; private set; }
        public RelayCommand CmdBringBox { get; private set; }
        public RelayCommand CmdPickBox { get; private set; }
        public RelayCommand Cancel { get; private set; }
        public RelayCommand Confirm { get; private set; }
        public RelayCommand<MessageKeyPressed> KeyDown { get; private set; }

        public StationViewModel Operation
        {
            get { return _operation; }
            set
            {
                if (_operation != value)
                {
                    _operation = value;
                    RaisePropertyChanged("Operation");
                }
            }
        }
        public ReleaseOrderViewModel ActiveOrder
        {
            get { return _activeOrder; }
            set
            {
                if (_activeOrder != value)
                {
                    _activeOrder = value;
                    RaisePropertyChanged("ActiveOrder");
                }
            }
        }
        public ObservableCollection<ReleaseOrderViewModel> DataListOrder
        {
            get { return _dataListOrder; }
            set
            {
                if (_dataListOrder != value)
                {
                    _dataListOrder = value;
                    RaisePropertyChanged("DataListOrder");
                }
            }
        }
        public ObservableCollection<ReleaseOrderViewModel> DataListSubOrder
        {
            get { return _dataListSubOrder; }
            set
            {
                if (_dataListSubOrder != value)
                {
                    _dataListSubOrder = value;
                    RaisePropertyChanged("DataListSubOrder");
                }
            }
        }
        public ObservableCollection<CommandWMSViewModel> DataListCommand
        {
            get { return _dataListCommand; }
            set
            {
                if (_dataListCommand != value)
                {
                    _dataListCommand = value;
                    RaisePropertyChanged("DataListCommand");
                }
            }
        }
        public ObservableCollection<TUSKUIDViewModel> DataListBoxes
        {
            get { return _dataListBoxes; }
            set
            {
                if (_dataListBoxes != value)
                {
                    _dataListBoxes = value;
                    RaisePropertyChanged("DataListBoxes");
                }
            }
        }

        public ReleaseOrderViewModel SelectedOrder
        {
            get
            {
                return _selectedOrder;
            }
            set
            {
                if (_selectedOrder != value)
                {
                    _selectedOrder = value;
                    RaisePropertyChanged("SelectedOrder");
                    try
                    {
                        if (_selectedOrder != null)
                            DetailedOrder = SelectedOrder;
                    }
                    catch (Exception e)
                    {
                        _warehouse.AddEvent(Database.Event.EnumSeverity.Error, Database.Event.EnumType.Exception, e.Message);
                        throw new Exception(string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
                    }
                }
            }
        }
        public ReleaseOrderViewModel SelectedSubOrder
        {
            get
            {
                return _selectedSubOrder;
            }
            set
            {
                if (_selectedSubOrder != value)
                {
                    _selectedSubOrder = value;
                    RaisePropertyChanged("SelectedSubOrder");
                }
            }
        }

        public CommandWMSViewModel SelectedCommand
        {
            get
            {
                return _selectedCommand;
            }
            set
            {
                if (_selectedCommand != value)
                {
                    _selectedCommand = value;
                    RaisePropertyChanged("SelectedCommand");
                }
            }
        }
        public ReleaseOrderViewModel DetailedOrder
        {
            get { return _detailedOrder; }
            set
            {
                if (_detailedOrder != value)
                {
                    _detailedOrder = value;
                    RaisePropertyChanged("DetailedOrder");
                }
            }
        }

        public DatabaseWMS.TU_ID ActiveTUID
        {
            get { return _activeTUID; }
            set
            {
                if (_activeTUID != value)
                {
                    _activeTUID = value;
                    RaisePropertyChanged("ActiveTUID");
                    RaisePropertyChanged("ActiveTUIDID");
                    RaisePropertyChanged("ActiveTUIDHC");
                }
            }
        }
        public int? ActiveTUIDID
        {
            get { return _activeTUID?.ID; }
            set
            {
                if (_activeTUID.ID != value)
                {
                    _activeTUID.ID = value??0;
                    RaisePropertyChanged("ActiveTUIDID");
                }
            }
        }
        public int? ActiveTUIDHC
        {
            get { return _activeTUID?.DimensionClass; }
            set
            {
                if (_activeTUID.DimensionClass != value)
                {
                    _activeTUID.DimensionClass = value??0;
                    RaisePropertyChanged("ActiveTUIDHC");
                }
            }
        }

        public bool EditEnabled
        {
            get { return _editEnabled; }
            set
            {
                if (_editEnabled != value)
                {
                    _editEnabled = value;
                    RaisePropertyChanged("EditEnabled");
                }
            }
        }
        public bool EnabledCC
        {
            get { return _enabledCC; }
            set
            {
                if (_enabledCC != value)
                {
                    _enabledCC = value;
                    RaisePropertyChanged("EnabledCC");
                }
            }
        }
        public bool VisibleOperation
        {
            get { return _visibleOperation; }
            set
            {
                if (_visibleOperation != value)
                {
                    _visibleOperation = value;
                    RaisePropertyChanged("VisibleOperation");
                }
            }
        }

        public bool VisibleBringTray
        {
            get { return _visibleBringTray; }
            set
            {
                if (_visibleBringTray != value)
                {
                    _visibleBringTray = value;
                    RaisePropertyChanged("VisibleBringTray");
                }
            }
        }
        public bool VisibleRemoveTray
        {
            get { return _visibleRemoveTray; }
            set
            {
                if (_visibleRemoveTray != value)
                {
                    _visibleRemoveTray = value;
                    RaisePropertyChanged("VisibleRemoveTray");
                }
            }
        }
        public bool VisibleBringBox
        {
            get { return _visibleBringBox; }
            set
            {
                if (_visibleBringBox != value)
                {
                    _visibleBringBox = value;
                    RaisePropertyChanged("VisibleBringBox");
                }
            }
        }

        public bool VisiblePickBox
        {
            get { return _visiblePickBox; }
            set
            {
                if (_visiblePickBox != value)
                {
                    _visiblePickBox = value;
                    RaisePropertyChanged("VisiblePickBox");
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
        public StationsViewModel()
        {
            DetailedOrder = null;
            SelectedOrder = null;
            SelectedCommand = null;

            EditEnabled = false;
            EnabledCC = false;

            _selectedCmd = CommandType.None;
            _activeTUID = new DatabaseWMS.TU_ID();

            RefreshSubOrder = new RelayCommand(async () => await ExecuteRefreshSubOrder());
            RefreshCommand = new RelayCommand(async () => await ExecuteRefreshCommandWMS());
            CmdRefresh = new RelayCommand(async() => await ExecuteRefresh());
            CmdStart = new RelayCommand(() => ExecuteStart(), CanExecuteStart);
            CmdFinish = new RelayCommand(async () => await ExecuteFinish(), CanExecuteFinish);
            CmdStop = new RelayCommand(() => ExecuteStop(), CanExecuteStop);
            CmdChangeHC = new RelayCommand(() => ExecuteChangeHC(), CanExecuteChangeHC);
            CmdStoreTray = new RelayCommand(() => ExecuteStoreTray(), CanExecuteStoreTray);
            CmdBringTray = new RelayCommand(() => ExecuteBringRemoveTray(CommandType.BringTray), CanExecuteBringRemoveTray);
            CmdRemoveTray = new RelayCommand(() => ExecuteBringRemoveTray(CommandType.RemoveTray), CanExecuteBringRemoveTray);
            CmdDropBox = new RelayCommand(() => ExecuteDropBox(), CanExecuteDropBox);
            CmdBringBox = new RelayCommand(() => ExecuteBringPickBox(CommandType.BringBox), CanExecuteBringPickBox);
            CmdPickBox = new RelayCommand(() => ExecuteBringPickBox(CommandType.PickBox), CanExecuteBringPickBox);
            Cancel = new RelayCommand(() => ExecuteCancel(), CanExecuteCancel);
            Confirm = new RelayCommand(() => ExecuteConfirm(), CanExecuteConfirm);
            KeyDown = new RelayCommand<MessageKeyPressed>(async (k) => await ExecuteKeyPressed(k));
        }

        public void Initialize(BasicWarehouse warehouse)
        {
            _warehouse = warehouse;
            _dbservicewms = new DBServiceWMS(_warehouse);
            VisibleOperation = false;
            VisibleBringTray = false;
            VisibleRemoveTray = true;
            VisibleBringBox = false;
            VisiblePickBox = true;
            try
            {
                DataListOrder = new ObservableCollection<ReleaseOrderViewModel>();
                DataListSubOrder = new ObservableCollection<ReleaseOrderViewModel>();
                DataListCommand = new ObservableCollection<CommandWMSViewModel>();
                DataListBoxes = new ObservableCollection<TUSKUIDViewModel>();

                _accessUser = "";
                Messenger.Default.Register<MessageAccessLevel>(this, (mc) => { AccessLevel = mc.AccessLevel; _accessUser = mc.User; });
                Messenger.Default.Register<MessageViewChanged>(this, async (vm) => await ExecuteViewActivated(vm.ViewModel));
                Messenger.Default.Register<MessageKeyPressed>(this, async (k) => await ExecuteKeyPressed(k));

                _timer = new DispatcherTimer();
                _timer.Interval = TimeSpan.FromMilliseconds(1000);
                _timer.Tick += (sender, args) => { ExecuteRefresh(); };
                _timer.Start();
            }
            catch (Exception e)
            {
                _warehouse.AddEvent(Database.Event.EnumSeverity.Error, Database.Event.EnumType.Exception, e.Message);
                throw new Exception(string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
            }
        }
        #endregion

        #region commands

        private void ExecuteStart()
        {
            try
            {
                _dbservicewms.SetOrderToActive(SelectedOrder.ERPID, SelectedOrder.OrderID);
            }
            catch (Exception e)
            {
                _warehouse.AddEvent(Database.Event.EnumSeverity.Error, Database.Event.EnumType.Exception,
                                    string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
            }
        }

        private bool CanExecuteStart()
        {
            try
            {
                return Operation == null && DataListOrder != null && DataListOrder.FirstOrDefault(p => p.Status == EnumWMSOrderStatus.Inactive) != null;
            }
            catch (Exception e)
            {
                _warehouse.AddEvent(Database.Event.EnumSeverity.Error, Database.Event.EnumType.Exception,
                                    string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
                return false;
            }
        }
        private async Task ExecuteFinish()
        {
            try
            {
                try
                {
                    using (WMSToUIClient client = new WMSToUIClient())
                    {
                        var cmd = DataListCommand.FirstOrDefault(p => (p.Operation == EnumOrderOperation.ConfirmStore || p.Operation == EnumOrderOperation.ConfirmFinish) && p.Status == EnumCommandWMSStatus.Active);
                        await client.CommandStatusChangedAsync(cmd.WMSID, (int)EnumCommandWMSStatus.Finished);
                    }
                }
                catch (Exception e)
                {
                    _warehouse.AddEvent(Database.Event.EnumSeverity.Error, Database.Event.EnumType.Exception,
                                       string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
                }
            }
            catch (Exception e)
            {
                _warehouse.AddEvent(Database.Event.EnumSeverity.Error, Database.Event.EnumType.Exception,
                                    string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
            }
        }

        private bool CanExecuteFinish()
        {
            try
            {
                return DataListCommand != null && 
                       DataListCommand.Any(p => (p.Operation == EnumOrderOperation.ConfirmStore || p.Operation == EnumOrderOperation.ConfirmFinish) && p.Status == EnumCommandWMSStatus.Active);
            }
            catch (Exception e)
            {
                _warehouse.AddEvent(Database.Event.EnumSeverity.Error, Database.Event.EnumType.Exception,
                                    string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
                return false;
            }
        }
        private void ExecuteStop()
        {
            try
            {
                _selectedCmd = CommandType.CancelOrder;
                Operation = null;
                EnabledCC = true;
            }
            catch (Exception e)
            {
                _warehouse.AddEvent(Database.Event.EnumSeverity.Error, Database.Event.EnumType.Exception,
                                    string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
            }
        }

        private bool CanExecuteStop()
        {
            try
            {
                return Operation == null && SelectedOrder != null;
            }
            catch (Exception e)
            {
                _warehouse.AddEvent(Database.Event.EnumSeverity.Error, Database.Event.EnumType.Exception,
                                    string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
                return false;
            }
        }

        private void ExecuteChangeHC()
        {
            try
            {
                _selectedCmd = CommandType.ChangeHeightClass;
                string io = _dbservicewms.GetParameter("Place.IOStation");
                Operation = null;
                EnabledCC = false;
                _warehouse.DBService.CreateOrUpdateMaterialID(_activeTUID.ID, _activeTUID.DimensionClass % 2 + 1, null);
                _warehouse.DBService.AddCommand(new CommandMaterial
                {
                    Task = Database.Command.EnumCommandTask.CreateMaterial,
                    Material = _activeTUID.ID,
                    Source = io,
                    Target = io,
                    Info = "modify",
                    Status = Database.Command.EnumCommandStatus.NotActive,
                    Time = DateTime.Now
                });
                _selectedCmd = CommandType.None;

            }
            catch (Exception e)
            {
                _warehouse.AddEvent(Database.Event.EnumSeverity.Error, Database.Event.EnumType.Exception,
                                    string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
            }
        }

        private bool CanExecuteChangeHC()
        {
            try
            {
                return Operation == null && _activeTUID != null && ActiveOrder == null;
            }
            catch (Exception e)
            {
                _warehouse.AddEvent(Database.Event.EnumSeverity.Error, Database.Event.EnumType.Exception,
                                    string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
                return false;
            }
        }
        private void ExecuteStoreTray()
        {
            try
            {
                _selectedCmd = CommandType.StoreTray;
                EditEnabled = true;
                EnabledCC = true;
                VisibleOperation = true;
                Operation = new StationStoreTrayViewModel();
                Operation.Initialize(_warehouse);
                Operation.ValidationEnabled = true;
                Operation.SetFocus = true;
            }
            catch (Exception e)
            {
                _warehouse.AddEvent(Database.Event.EnumSeverity.Error, Database.Event.EnumType.Exception, 
                                    string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
            }
        }

        private bool CanExecuteStoreTray()
        {
            try
            {
                return Operation == null && _dbservicewms != null && _dbservicewms.CanStoreTray(_dbservicewms.GetParameter("Place.IOStation"));
            }
            catch (Exception e)
            {
                _warehouse.AddEvent(Database.Event.EnumSeverity.Error, Database.Event.EnumType.Exception, 
                                    string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
                return false;
            }
        }
        private void ExecuteBringRemoveTray(CommandType cmd)
        {
            try
            {
                _selectedCmd = cmd;
                EditEnabled = true;
                EnabledCC = true;
                VisibleOperation = true;
                Operation = new StationRemoveTrayViewModel();
                Operation.Initialize(_warehouse);
                Operation.ValidationEnabled = true;
                Operation.SetFocus = true;
            }
            catch (Exception e)
            {
                _warehouse.AddEvent(Database.Event.EnumSeverity.Error, Database.Event.EnumType.Exception,
                                    string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
            }
        }

        private bool CanExecuteBringRemoveTray()
        {
            try
            {
                return Operation == null;
            }
            catch (Exception e)
            {
                _warehouse.AddEvent(Database.Event.EnumSeverity.Error, Database.Event.EnumType.Exception,
                                    string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
                return false;
            }
        }
        private void ExecuteDropBox()
        {
            try
            {
                _selectedCmd = CommandType.DropBox;
                EditEnabled = true;
                EnabledCC = true;
                VisibleOperation = true;
                Operation = new StationDropBoxViewModel();
                Operation.Initialize(_warehouse);
                Operation.ValidationEnabled = true;
                Operation.SetFocus = true;
            }
            catch (Exception e)
            {
                _warehouse.AddEvent(Database.Event.EnumSeverity.Error, Database.Event.EnumType.Exception,
                                    string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
            }
        }

        private bool CanExecuteDropBox()
        {
            try
            {
                return Operation == null;
            }
            catch (Exception e)
            {
                _warehouse.AddEvent(Database.Event.EnumSeverity.Error, Database.Event.EnumType.Exception,
                                    string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
                return false;
            }
        }
        private void ExecuteBringPickBox(CommandType cmd)
        {
            try
            {
                _selectedCmd = cmd;
                EditEnabled = true;
                EnabledCC = true;
                VisibleOperation = true;
                Operation = new StationPickBoxViewModel();
                Operation.Initialize(_warehouse);
                Operation.ValidationEnabled = true;
                Operation.SetFocus = true;
            }
            catch (Exception e)
            {
                _warehouse.AddEvent(Database.Event.EnumSeverity.Error, Database.Event.EnumType.Exception,
                                    string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
            }
        }

        private bool CanExecuteBringPickBox()
        {
            try
            {
                return Operation == null;
            }
            catch (Exception e)
            {
                _warehouse.AddEvent(Database.Event.EnumSeverity.Error, Database.Event.EnumType.Exception,
                                    string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
                return false;
            }
        }

        private async Task ExecuteKeyPressed(MessageKeyPressed mkp)
        {
            try
            {
                if(_activevm is StationsViewModel)
                {
                    _bcrcommand = _bcrcommand + mkp.KeyPressed.ToUpper(); ;
                    if (!_bcrcommand.StartsWith("."))
                        _bcrcommand = "";
                    else if (_bcrcommand.StartsWith(".."))
                        _bcrcommand = ".";

                    switch (_bcrcommand)
                    {
                        case ".AC.":
                            if (CanExecuteStart())
                                ExecuteStart();
                            break;
                        case ".TS.":
                            if (CanExecuteStoreTray())
                                ExecuteStoreTray();
                            break;
                        case ".TB.":
                            if (CanExecuteBringRemoveTray())
                                ExecuteBringRemoveTray(CommandType.BringTray);
                            break;
                        case ".TR.":
                            if (CanExecuteBringRemoveTray())
                                ExecuteBringRemoveTray(CommandType.RemoveTray);
                            break;
                        case ".BD.":
                            if (CanExecuteDropBox())
                                ExecuteDropBox();
                            break;
                        case ".BB.":
                            if (CanExecuteBringPickBox())
                                ExecuteBringPickBox(CommandType.BringBox);
                            break;
                        case ".BP.":
                            if (CanExecuteBringPickBox())
                                ExecuteBringPickBox(CommandType.PickBox);
                            break;
                        case ".CN.":
                            if (CanExecuteCancel())
                                ExecuteCancel();
                            break;
                        case ".OK.":
                            if (Operation == null)
                            {
                                if (CanExecuteFinish())
                                    await ExecuteFinish();
                            }
                            else
                            {
                                if (Operation is StationDropBoxViewModel)
                                {
                                    var o = Operation as StationDropBoxViewModel;
                                    if (!o.AllPropertiesValid && o.CanExecuteSuggestTU())
                                        await o.ExecuteSuggestTU();
                                    else if (CanExecuteConfirm())
                                        ExecuteConfirm();
                                }
                                else if (CanExecuteConfirm())
                                    ExecuteConfirm();
                            }
                            break;

                    }

                    if (_bcrcommand.Length >= 4)
                        _bcrcommand = "";
                }
            }
            catch (Exception e)
            {
                _warehouse.AddEvent(Database.Event.EnumSeverity.Error, Database.Event.EnumType.Exception,
                                    string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
            }
        }

        private void ExecuteCancel()
        {
            try
            {
                if(Operation is StationActionViewModel)
                {
                    var cmd = DataListCommand.FirstOrDefault(p => p.Status == EnumCommandWMSStatus.Active);
                    if (cmd != null)
                        using (WMSToUIClient client = new WMSToUIClient())
                        {
                            client.CancelCommand(new DTOCommand
                            {
                                ID = cmd.WMSID
                            });
                        }
                }

                Operation = null;
                EnabledCC = false;
                VisibleOperation = false;
            }
            catch (Exception e)
            {
                _warehouse.AddEvent(Database.Event.EnumSeverity.Error, Database.Event.EnumType.Exception, e.Message);
            }
        }
        private bool CanExecuteCancel()
        {
            try
            {
                return EnabledCC; 
            }
            catch (Exception e)
            {
                _warehouse.AddEvent(Database.Event.EnumSeverity.Error, Database.Event.EnumType.Exception, 
                                    string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
                return false;
            }
        }
        private void ExecuteConfirm()
        {
            try
            {
                EditEnabled = false;
                EnabledCC = false;
                VisibleOperation = false;
                try
                {
                    switch (_selectedCmd)
                    {
                        case CommandType.StoreTray:
                            using (WMSToUIClient client = new WMSToUIClient())
                            {
                                client.StoreTUID((Operation as StationStoreTrayViewModel).TUID);
//                              _dbservicewms.CreateOrder_StoreTUID((Operation as StationStoreTrayViewModel).TUID);
                            }
                            break;
                        case CommandType.BringTray:
                            _dbservicewms.CreateOrder_BringTUID((Operation as StationRemoveTrayViewModel).TUID);
                            break;
                        case CommandType.RemoveTray:
                            _dbservicewms.CreateOrder_RemoveTUID((Operation as StationRemoveTrayViewModel).TUID);
                            break;
                        case CommandType.DropBox:
                            _dbservicewms.CreateOrder_DropBox((Operation as StationDropBoxViewModel).TUID, (Operation as StationDropBoxViewModel).BoxList);
                            break;
                        case CommandType.BringBox:
                            _dbservicewms.CreateOrder_BringBox((Operation as StationPickBoxViewModel).BoxList);
                            break;
                        case CommandType.PickBox:
                            _dbservicewms.CreateOrder_PickBox((Operation as StationPickBoxViewModel).BoxList);
                            break;
                        case CommandType.CancelOrder:
                            using (WMSToUIClient client = new WMSToUIClient())
                            {
                                client.CancelOrder(new DTOOrder
                                {
                                    ERP_ID = SelectedOrder.ERPID,
                                    OrderID = SelectedOrder.OrderID,
                                    SubOrderID = 0      // cancel all suborders
                                });
                            }
                            break;
                    }
                    _selectedCmd = CommandType.None;
                }
                catch (Exception e)
                {
                    _warehouse.AddEvent(Event.EnumSeverity.Error, Event.EnumType.Exception, e.Message);
                }
                Operation = null;
            }
            catch (Exception e)
            {
                _warehouse.AddEvent(Database.Event.EnumSeverity.Error, Database.Event.EnumType.Exception, 
                                    string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
            }
        }
        private bool CanExecuteConfirm()
        {
            try
            {
                return (Operation != null && Operation.AllPropertiesValid) || _selectedCmd == CommandType.CancelOrder;
            }
            catch (Exception e)
            {
                _warehouse.AddEvent(Database.Event.EnumSeverity.Error, Database.Event.EnumType.Exception, 
                                    string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
                return false;
            }
        }
        private async Task ExecuteRefresh()
        {
            try
            {
                if (!(_activevm is StationsViewModel))
                    return;

                VisibleRemoveTray = _dbservicewms.CanStoreTray(_dbservicewms.GetParameter("Place.IOStation"));
                VisibleBringTray = !VisibleRemoveTray;
                VisibleBringBox = !VisibleRemoveTray;
                VisiblePickBox = VisibleRemoveTray;

                CommandManager.InvalidateRequerySuggested();        // forces buttons to refresh
                await ExecuteRefreshOrders();
                await ExecuteRefreshCommandWMS();
                await ExecuteRefreshBoxes();
            }
            catch (Exception e)
            {
                _warehouse.AddEvent(Database.Event.EnumSeverity.Error, Database.Event.EnumType.Exception,
                                    string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
            }
        }
        private async Task ExecuteRefreshOrders()
        {
            try
            {
                int? erpid = SelectedOrder?.ERPID;
                int? orderid = SelectedOrder?.OrderID;
                int? ao_erpid = ActiveOrder?.ERPID;
                int? ao_orderid = ActiveOrder?.OrderID;
                _suborderid = SelectedSubOrder?.ID;
                _commandid = SelectedCommand?.WMSID;

                var ao = _dbservicewms.GetOrders((int)EnumWMSOrderStatus.Active, (int)EnumWMSOrderStatus.Active).FirstOrDefault();
                if (ao != null)
                    ActiveOrder = new ReleaseOrderViewModel
                    {
                        ERPID = ao.ERP_ID,
                        OrderID = ao.OrderID,
                        TUID = ao.TU_ID,
                        BoxID = ao.Box_ID,
                        SKUID = ao.SKU_ID,
                        SKUBatch = ao.SKU_Batch,
                        Destination = ao.Destination,
                        ReleaseTime = ao.ReleaseTime,
                        Status = (EnumWMSOrderStatus)ao.Status
                    };
                else
                    ActiveOrder = null;
//                if(ao_erpid != ActiveOrder?.ERPID || ao_orderid != ActiveOrder?.OrderID)
//                    await ExecuteRefreshCommandWMS();

                var orders = await _dbservicewms.GetOrdersDistinct(DateTime.Now.AddSeconds(1), DateTime.Now, (int)EnumWMSOrderStatus.OnTargetPart);

                _donotrefreshsuborder = true;
                DataListOrder.Clear();
                foreach (var p in orders)
                    DataListOrder.Add(new ReleaseOrderViewModel
                    {
                        ERPID = p.ERPID,
                        OrderID = p.OrderID,
                        TUID = p.TUID,
                        BoxID = p.BoxID,
                        SKUID = p.SKUID,
                        SKUBatch = p.SKUBatch,
                        Destination = p.Destination,
                        ReleaseTime = p.ReleaseTime,
                        LastChange = p.LastChange,
                        Status = (EnumWMSOrderStatus)p.Status
                    });
                foreach (var l in DataListOrder)
                    l.Initialize(_warehouse);
                _donotrefreshsuborder = false;
                if (orderid != null)
                    SelectedOrder = DataListOrder.FirstOrDefault(p => p.ERPID == erpid && p.OrderID == orderid);
                else
                    SelectedOrder = DataListOrder.FirstOrDefault();
                if (SelectedOrder == null)
                    await ExecuteRefreshSubOrder();
            }
            catch (Exception e)
            {
                _warehouse.AddEvent(Database.Event.EnumSeverity.Error, Database.Event.EnumType.Exception, 
                                    string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
            }
        }

        private async Task ExecuteRefreshSubOrder()
        {
            try
            {
                if (_donotrefreshsuborder)
                    return;
                if ( _suborderid == null)
                    _suborderid = SelectedSubOrder?.ID;

                if (DataListOrder.Count == 0)
                    DataListSubOrder.Clear();
                if (SelectedOrder != null)
                {
                    var suborders = await _dbservicewms.GetSubOrders(SelectedOrder.ERPID, SelectedOrder.OrderID);
                    DataListSubOrder.Clear();
                    foreach (var p in suborders)
                        DataListSubOrder.Add(new ReleaseOrderViewModel
                        {
                            ID = p.ID,
                            ERPID = p.ERP_ID,
                            OrderID = p.OrderID,
                            SubOrderID = p.SubOrderID,
                            SubOrderERPID = p.SubOrderERPID,
                            SubOrderName = p.SubOrderName,
                            TUID = p.TU_ID,
                            BoxID = p.Box_ID,
                            SKUID = p.SKU_ID,
                            SKUBatch = p.SKU_Batch,
                            SKUQty = p.SKU_Qty,
                            Portion = "",
                            Operation = (EnumOrderOperation)p.Operation,
                            Status = (EnumWMSOrderStatus)p.Status
                        });
                    foreach (var l in DataListOrder)
                        l.Initialize(_warehouse);
//                    if (_suborderid != null)
//                        SelectedSubOrder = DataListSubOrder.FirstOrDefault(p => p.ID == _suborderid);
//                    if(SelectedSubOrder == null)
//                        SelectedSubOrder = DataListSubOrder.FirstOrDefault();
                    _suborderid = null;
                }

            }
            catch (Exception e)
            {
                _warehouse.AddEvent(Database.Event.EnumSeverity.Error, Database.Event.EnumType.Exception,
                                    string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
            }
        }
        private async Task ExecuteRefreshCommandWMS()
        {
            try
            {
                if (_commandid == null)
                    _commandid = SelectedCommand?.WMSID;

                var cmds = ActiveOrder != null ? await _dbservicewms.GetCommandsWMSForOrder(ActiveOrder.ERPID, ActiveOrder.OrderID) : null;
                DataListCommand.Clear();
                if ( cmds != null )
                {
                    foreach (var cmd in cmds)
                    {
                        DataListCommand.Add(new CommandWMSViewModel
                        {
                            WMSID = cmd.ID,
                            OrderERPID = cmd.OrderERPID,
                            OrderID = cmd.Order_ID,
                            OrderOrderID = cmd.OrderOrderID,
                            OrderSubOrderID = cmd.OrderSubOrderID,
                            OrderSubOrderERPID = cmd.OrderSubOrderERPID,
                            OrderSubOrderName = cmd.OrderSubOrderName,
                            OrderSKUID = cmd.OrderSKUID,
                            OrderSKUBatch = cmd.OrderSKUBatch,
                            Source = cmd.Source,
                            Target = cmd.Target,
                            TUID = cmd.TU_ID,
                            BoxID = cmd.Box_ID,
                            Operation = (EnumOrderOperation)cmd.Operation,
                            Time = cmd.Time,
                            Status = (EnumCommandWMSStatus)cmd.Status,
                        });
                    }
                    foreach (var l in DataListCommand)
                        l.Initialize(_warehouse);

                    var cmdsDrop = cmds
                                        .Where(p => p.Status == (int)EnumCommandWMSStatus.Active && p.Operation == (int)EnumOrderOperation.DropBox)
                                        .Select(p => p)
                                        .ToList();
                    var cmdsPick = cmds
                                        .Where(p => p.Status == (int)EnumCommandWMSStatus.Active && p.Operation == (int)EnumOrderOperation.PickBox)
                                        .Select(p => p)
                                        .ToList();
                    if (cmdsDrop.Count > 0 && Operation == null)
                    {
                        Operation = new StationActionViewModel();
                        (Operation as StationActionViewModel).Initialize(_warehouse, cmdsDrop);
                        (Operation as StationActionViewModel).Command = StationActionViewModel.CommandType.DropBox;
                        VisibleOperation = true;
                        EnabledCC = true;
                        EditEnabled = true;
                    }
                    if (cmdsPick.Count > 0 && Operation == null)
                    {
                        Operation = new StationActionViewModel();
                        (Operation as StationActionViewModel).Initialize(_warehouse, cmdsPick);
                        (Operation as StationActionViewModel).Command = StationActionViewModel.CommandType.PickBox;
                        VisibleOperation = true;
                        EnabledCC = true;
                        EditEnabled = true;
                    }
                    else if (cmdsDrop.Count == 0 && cmdsPick.Count == 0 && Operation is StationActionViewModel)
                    {
                        Operation = null;
                        VisibleOperation = false;
                        EnabledCC = false;
                        EditEnabled = false;
                    }
                }
            }
            catch (Exception e)
            {
                _warehouse.AddEvent(Database.Event.EnumSeverity.Error, Database.Event.EnumType.Exception,
                                    string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
            }
        }

        private async Task ExecuteRefreshBoxes()
        {
            try
            {
                ActiveTUID = _dbservicewms.GetTUIDOnPlaceID(_dbservicewms.GetParameter("Place.IOStation"));

                var tus = _activeTUID != null ? await _dbservicewms.GetTUSKUIDsAsync(_activeTUID.ID) : null;

                DataListBoxes.Clear();
                if (tus != null)
                {
                    foreach (var tu in tus)
                    {
                        DataListBoxes.Add(new TUSKUIDViewModel
                        {
                            BoxID = tu.BoxID,
                            SKUID = tu.SKUID,
                            Batch = tu.Batch,
                            Qty = tu.Qty,
                            ProdDate = tu.ProdDate,
                            ExpDate = tu.ExpDate
                        });
                    }
                    foreach (var l in DataListBoxes)
                        l.Initialize(_warehouse);
                }
            }
            catch (Exception e)
            {
                _warehouse.AddEvent(Database.Event.EnumSeverity.Error, Database.Event.EnumType.Exception,
                                    string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
            }
        }
        #endregion
        public async Task ExecuteViewActivated(ViewModelBase vm)
        {
            try
            {
                _activevm = vm;
                if (vm is StationsViewModel)
                {
                    await ExecuteRefresh();
                }
            }
            catch (Exception e)
            {
                _warehouse.AddEvent(Event.EnumSeverity.Error, Event.EnumType.Exception,
                                    string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
            }
        }
    }
}
