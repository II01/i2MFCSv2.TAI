using Database;
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
        public enum CommandType { None = 0, ActivateOrder, CancelOrder, StoreTray, RemoveTray, DropBox, PickBox };

        #region members
        private CommandType _selectedCmd;
        private BasicWarehouse _warehouse;
        private DBServiceWMS _dbservicewms;
        private StationViewModel _operation;
        private ObservableCollection<ReleaseOrderViewModel> _dataListOrder;
        private ObservableCollection<ReleaseOrderViewModel> _dataListSubOrder;
        private ObservableCollection<CommandWMSViewModel> _dataListCommand;
        private ReleaseOrderViewModel _selectedOrder;
        private ReleaseOrderViewModel _selectedSubOrder;
        private ReleaseOrderViewModel _detailedOrder;
        private CommandWMSViewModel _selectedCommand;
        private bool _editEnabled;
        private bool _enabledCC;
        private int _accessLevel;
        private string _accessUser;
        private int? _suborderid;
        private int? _commandid;
        #endregion

        #region properites
        public RelayCommand RefreshSubOrder { get; private set; }
        public RelayCommand RefreshCommand { get; private set; }
        public RelayCommand CmdRefresh { get; private set; }
        public RelayCommand CmdStart { get; private set; }
        public RelayCommand CmdStop { get; private set; }
        public RelayCommand CmdStoreTray { get; private set; }
        public RelayCommand CmdRemoveTray { get; private set; }
        public RelayCommand CmdDropBox { get; private set; }
        public RelayCommand CmdPickBox { get; private set; }
        public RelayCommand Cancel { get; private set; }
        public RelayCommand Confirm { get; private set; }

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

            RefreshSubOrder = new RelayCommand(async () => await ExecuteRefreshSubOrder());
            RefreshCommand = new RelayCommand(async () => await ExecuteRefreshCommandWMS());
            CmdRefresh = new RelayCommand(async() => await ExecuteRefresh());
            CmdStart = new RelayCommand(() => ExecuteStart(), CanExecuteStart);
            CmdStop = new RelayCommand(() => ExecuteStop(), CanExecuteStop);
            CmdStoreTray = new RelayCommand(() => ExecuteStoreTray(), CanExecuteStoreTray);
            CmdRemoveTray = new RelayCommand(() => ExecuteRemoveTray(), CanExecuteRemoveTray);
            CmdDropBox = new RelayCommand(() => ExecuteDropBox(), CanExecuteDropBox);
            CmdPickBox = new RelayCommand(() => ExecutePickBox(), CanExecutePickBox);
            Cancel = new RelayCommand(() => ExecuteCancel(), CanExecuteCancel);
            Confirm = new RelayCommand(() => ExecuteConfirm(), CanExecuteConfirm);

        }

        public void Initialize(BasicWarehouse warehouse)
        {
            _warehouse = warehouse;
            _dbservicewms = new DBServiceWMS(_warehouse);
            try
            {
                DataListOrder = new ObservableCollection<ReleaseOrderViewModel>();
                DataListSubOrder = new ObservableCollection<ReleaseOrderViewModel>();
                DataListCommand = new ObservableCollection<CommandWMSViewModel>();
                _accessUser = "";
                Messenger.Default.Register<MessageAccessLevel>(this, (mc) => { AccessLevel = mc.AccessLevel; _accessUser = mc.User; });
                Messenger.Default.Register<MessageViewChanged>(this, async (vm) => await ExecuteViewActivated(vm.ViewModel));
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
                return Operation == null;
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
                return Operation == null;
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
                return Operation == null;
            }
            catch (Exception e)
            {
                _warehouse.AddEvent(Database.Event.EnumSeverity.Error, Database.Event.EnumType.Exception, 
                                    string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
                return false;
            }
        }
        private void ExecuteRemoveTray()
        {
            try
            {
                _selectedCmd = CommandType.RemoveTray;
                EditEnabled = true;
                EnabledCC = true;
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

        private bool CanExecuteRemoveTray()
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
        private void ExecutePickBox()
        {
            try
            {
                _selectedCmd = CommandType.PickBox;
                EditEnabled = true;
                EnabledCC = true;
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

        private bool CanExecutePickBox()
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

        private void ExecuteCancel()
        {
            try
            {
                Operation = null;
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
                return Operation != null;
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
                try
                {
                    switch (_selectedCmd)
                    {
                        case CommandType.StoreTray:
                            _dbservicewms.CreateOrder_StoreTUID((Operation as StationStoreTrayViewModel).TUID);
                            break;
                        case CommandType.RemoveTray:
                            _dbservicewms.CreateOrder_RemoveTUID((Operation as StationRemoveTrayViewModel).TUID);
                            break;
                        case CommandType.DropBox:
                            _dbservicewms.CreateOrder_DropBox((Operation as StationDropBoxViewModel).TUID, (Operation as StationDropBoxViewModel).BoxList);
                            break;
                        case CommandType.PickBox:
                            _dbservicewms.CreateOrder_PickBox((Operation as StationPickBoxViewModel).BoxList);
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
                return Operation != null && Operation.AllPropertiesValid;
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
                await ExecuteRefreshOrders();
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
                _suborderid = SelectedSubOrder?.ID;
                _commandid = SelectedCommand?.WMSID;

                var orders = await _dbservicewms.GetOrdersDistinct(DateTime.Now.AddYears(-1), DateTime.Now, (int)EnumWMSOrderStatus.OnTargetPart);

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
                if (orderid != null)
                    SelectedOrder = DataListOrder.FirstOrDefault(p => p.ERPID == erpid && p.OrderID == orderid);
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
                if( _suborderid == null)
                    _suborderid = SelectedSubOrder?.ID;

                DataListSubOrder.Clear();
                if(SelectedOrder != null)
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
                    if (_suborderid != null)
                        SelectedSubOrder = DataListSubOrder.FirstOrDefault(p => p.ID == _suborderid);
                    if(SelectedSubOrder == null)
                        SelectedSubOrder = DataListSubOrder.FirstOrDefault();
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

                DataListCommand.Clear();
                if( SelectedSubOrder != null )
                {
                    var cmds = await _dbservicewms.GetCommandsWMSForOrder(SelectedOrder.ERPID, SelectedOrder.OrderID);
                    DataListCommand.Clear();
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
                    if (_commandid != null)
                        SelectedCommand = DataListCommand.FirstOrDefault(p => p.WMSID == _commandid.Value);
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
