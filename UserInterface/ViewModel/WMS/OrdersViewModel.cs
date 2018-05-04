using Database;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.CommandWpf;
using GalaSoft.MvvmLight.Messaging;
using System;
using System.Collections.ObjectModel;
using System.Data.SqlTypes;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using UserInterface.DataServiceWMS;
using UserInterface.Messages;
using UserInterface.Services;
using Warehouse.Model;

namespace UserInterface.ViewModel
{
    public sealed class OrdersViewModel : ViewModelBase
    {
        public enum CommandType { None = 0, AddOrder, EditOrder, DeleteOrder, AddSubOrder, EditSubOrder, DeleteSubOrder, AddSKU, EditSKU, DeleteSKU};

        #region members
        private CommandType _selectedCommand;
        private ObservableCollection<OrderViewModel> _dataListOrder;
        private ObservableCollection<OrderViewModel> _dataListSubOrder;
        private ObservableCollection<OrderViewModel> _dataListSKU;
        private OrderViewModel _selectedOrder;
        private OrderViewModel _selectedSubOrder;
        private OrderViewModel _selectedSKU;
        private OrderViewModel _detailed;
        private bool _editEnabled;
        private bool _enabledCC;
        private BasicWarehouse _warehouse;
        private DBServiceWMS _dbservicewms;
        private int _accessLevel;
        private string _accessUser;
        int _suborderid;
        int _skuid;
        #endregion

        #region properites
        public RelayCommand Refresh { get; private set; }
        public RelayCommand RefreshSubOrders { get; private set; }        
        public RelayCommand AddOrder { get; private set; }
        public RelayCommand EditOrder { get; private set; }
        public RelayCommand DeleteOrder { get; private set; }
        public RelayCommand AddSubOrder { get; private set; }
        public RelayCommand EditSubOrder { get; private set; }
        public RelayCommand DeleteSubOrder { get; private set; }
        public RelayCommand AddSKU { get; private set; }
        public RelayCommand EditSKU { get; private set; }
        public RelayCommand DeleteSKU { get; private set; }
        public RelayCommand Confirm { get; private set; }
        public RelayCommand Cancel { get; private set; }

        public ObservableCollection<OrderViewModel> DataListOrder
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
        public ObservableCollection<OrderViewModel> DataListSubOrder
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
        public ObservableCollection<OrderViewModel> DataListSKU
        {
            get { return _dataListSKU; }
            set
            {
                if (_dataListSKU != value)
                {
                    _dataListSKU = value;
                    RaisePropertyChanged("DataListSKU");
                }
            }
        }

        public OrderViewModel SelectedOrder
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
                            Detailed = SelectedOrder;
                        ExecuteRefreshSubOrder();
                        if (_selectedOrder != null)
                            SelectedSubOrder = DataListSubOrder.FirstOrDefault();
                    }
                    catch (Exception e)
                    {
                        _warehouse.AddEvent(Database.Event.EnumSeverity.Error, Database.Event.EnumType.Exception, e.Message);
                        throw new Exception(string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
                    }
                }
            }
        }

        public OrderViewModel SelectedSubOrder
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
                    try
                    {
                        if (_selectedSubOrder != null)
                        {
                            Detailed = SelectedSubOrder;
                            Detailed.ERPIDRef = SelectedOrder.ERPIDRef;
                        }
                        ExecuteRefreshSKU();
                        if (_selectedSubOrder != null)
                            SelectedSKU = DataListSKU.FirstOrDefault();
                    }
                    catch (Exception e)
                    {
                        _warehouse.AddEvent(Database.Event.EnumSeverity.Error, Database.Event.EnumType.Exception, e.Message);
                        throw new Exception(string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
                    }
                }
            }
        }
        public OrderViewModel SelectedSKU
        {
            get
            {
                return _selectedSKU;
            }
            set
            {
                if (_selectedSKU != value)
                {
                    _selectedSKU = value;
                    RaisePropertyChanged("SelectedSKU");
                    try
                    {
                        if (_selectedSKU != null)
                        {
                            Detailed = SelectedSKU;
                            Detailed.ERPIDRef = SelectedOrder.ERPIDRef;
                        }
                    }
                    catch (Exception e)
                    {
                        _warehouse.AddEvent(Database.Event.EnumSeverity.Error, Database.Event.EnumType.Exception, e.Message);
                        throw new Exception(string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
                    }
                }
            }
        }


        public OrderViewModel Detailed
        {
            get { return _detailed; }
            set
            {
                if (_detailed != value)
                {
                    _detailed = value;
                    RaisePropertyChanged("Detailed");
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
        public OrdersViewModel()
        {
            Detailed = null;
            SelectedOrder = null;
            SelectedSubOrder = null;
            SelectedSKU = null;

            EditEnabled = false;
            EnabledCC = false;

            _selectedCommand = CommandType.None;

            Refresh = new RelayCommand(async () => await ExecuteRefresh());
            AddOrder = new RelayCommand(() => ExecuteAddOrder(), CanExecuteAddOrder);
            EditOrder = new RelayCommand(() => ExecuteEditOrder(), CanExecuteEditOrder);
            DeleteOrder = new RelayCommand(() => ExecuteDeleteOrder(), CanExecuteDeleteOrder);
            AddSubOrder = new RelayCommand(() => ExecuteAddSubOrder(), CanExecuteAddSubOrder);
            EditSubOrder = new RelayCommand(() => ExecuteEditSubOrder(), CanExecuteEditOrder);
            DeleteSubOrder = new RelayCommand(() => ExecuteDeleteSubOrder(), CanExecuteDeleteSubOrder);
            AddSKU = new RelayCommand(() => ExecuteAddSKU(), CanExecuteAddSKU);
            EditSKU = new RelayCommand(() => ExecuteEditSKU(), CanExecuteEditSKU);
            DeleteSKU = new RelayCommand(() => ExecuteDeleteSKU(), CanExecuteDeleteSKU);
            Cancel = new RelayCommand(() => ExecuteCancel(), CanExecuteCancel);
            Confirm = new RelayCommand(async () => await ExecuteConfirm(), CanExecuteConfirm);
        }

        public void Initialize(BasicWarehouse warehouse)
        {
            _warehouse = warehouse;
            _dbservicewms = new DBServiceWMS(_warehouse);
            try
            {
                DataListOrder = new ObservableCollection<OrderViewModel>();
                DataListSubOrder = new ObservableCollection<OrderViewModel>();
                DataListSKU = new ObservableCollection<OrderViewModel>();
                _accessUser = "";
                Messenger.Default.Register<MessageAccessLevel>(this, (mc) => { AccessLevel = mc.AccessLevel; _accessUser = mc.User; });
                Messenger.Default.Register<MessageViewChanged>(this, async vm => await ExecuteViewActivated(vm.ViewModel));
            }
            catch (Exception e)
            {
                _warehouse.AddEvent(Database.Event.EnumSeverity.Error, Database.Event.EnumType.Exception, e.Message);
                throw new Exception(string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
            }
        }
        #endregion

        #region commands
        private void ExecuteAddOrder()
        {
            try
            {
                EditEnabled = true;
                EnabledCC = true;                
                _selectedCommand = CommandType.AddOrder;
                Detailed = new OrderViewModel();
                Detailed.Initialize(_warehouse);
                Detailed.EnableOrderAdd = true;
                Detailed.EnableOrderEdit = true;
                Detailed.EnableSubOrderEdit = true;
                Detailed.EnableSKUEdit = true;
                Detailed.ValidationEnabled = true;
                Detailed.ReferenceOrderID = 0;
                Detailed.ReferenceSubOrderID = 0;
                Detailed.ERPID = null;
                Detailed.ERPIDRef = null;
                Detailed.OrderID = 0;
                Detailed.Destination = "";
                Detailed.ReleaseTime = SqlDateTime.MaxValue.Value;
                Detailed.SubOrderID = 0;
                Detailed.SubOrderERPID = 0;
                Detailed.SubOrderName = "";
                Detailed.SKUID = "";
                Detailed.SKUBatch = "";
                Detailed.SKUQty = 0;
                Detailed.Status = 0;
                Detailed.Initialize(_warehouse);
            }
            catch (Exception e)
            {
                _warehouse.AddEvent(Database.Event.EnumSeverity.Error, Database.Event.EnumType.Exception, 
                                    string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
            }
        }

        private bool CanExecuteAddOrder()
        {
            try
            {
                return !EditEnabled && AccessLevel/10 >= 2;
            }
            catch (Exception e)
            {
                _warehouse.AddEvent(Database.Event.EnumSeverity.Error, Database.Event.EnumType.Exception, 
                                    string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
                return false;
            }
        }
        private void ExecuteEditOrder()
        {
            try
            {
                EditEnabled = true;
                EnabledCC = true;
                _selectedCommand = CommandType.EditOrder;
                Detailed = new OrderViewModel();
                Detailed.Initialize(_warehouse);
                Detailed.ValidationEnabled = true;
                Detailed.EnableOrderAdd = false;
                Detailed.EnableOrderEdit = true;
                Detailed.EnableSubOrderEdit = false;
                Detailed.EnableSKUEdit = false;
                Detailed.ReferenceOrderID = SelectedSubOrder.OrderID;
                Detailed.ReferenceSubOrderID = SelectedSubOrder.SubOrderID;
                Detailed.ID = SelectedSubOrder.ID;
                Detailed.ERPID = SelectedSubOrder.ERPID;
                Detailed.ERPIDRef = SelectedOrder.ERPIDRef;
                Detailed.OrderID = SelectedSubOrder.OrderID;
                Detailed.Destination = SelectedSubOrder.Destination;
                Detailed.ReleaseTime = SelectedSubOrder.ReleaseTime;
                Detailed.SubOrderID = SelectedSubOrder.SubOrderID;
                Detailed.SubOrderERPID = SelectedSubOrder.SubOrderERPID;
                Detailed.SubOrderName = SelectedSubOrder.SubOrderName;
                Detailed.SKUID = SelectedSubOrder.SKUID;
                Detailed.SKUBatch = SelectedSubOrder.SKUBatch;
                Detailed.SKUQty = SelectedSubOrder.SKUQty;
                Detailed.Status = SelectedSubOrder.Status;
            }
            catch (Exception e)
            {
                _warehouse.AddEvent(Database.Event.EnumSeverity.Error, Database.Event.EnumType.Exception,
                                    string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
            }
        }
        private bool CanExecuteEditOrder()
        {
            try
            {
                return SelectedOrder != null && SelectedOrder.ERPID == null && 
                       SelectedOrder.Status == EnumWMSOrderStatus.Waiting && SelectedOrder.ReleaseTime == SqlDateTime.MaxValue.Value && 
                       !EditEnabled && AccessLevel/10 >= 2;
            }
            catch (Exception e)
            {
                _warehouse.AddEvent(Database.Event.EnumSeverity.Error, Database.Event.EnumType.Exception,
                                    string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
                return false;
            }
        }
        private void ExecuteDeleteOrder()
        {
            try
            {
                EditEnabled = true;
                EnabledCC = true;
                _selectedCommand = CommandType.DeleteOrder;
                Detailed = SelectedOrder;
            }
            catch (Exception e)
            {
                _warehouse.AddEvent(Database.Event.EnumSeverity.Error, Database.Event.EnumType.Exception,
                                    string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
            }
        }
        private bool CanExecuteDeleteOrder()
        {
            try
            {
                return SelectedOrder != null && SelectedOrder.ERPID == null &&
                       SelectedOrder.Status == EnumWMSOrderStatus.Waiting && SelectedOrder.ReleaseTime == SqlDateTime.MaxValue.Value && 
                       !EditEnabled && AccessLevel/10 >= 2;
            }
            catch (Exception e)
            {
                _warehouse.AddEvent(Database.Event.EnumSeverity.Error, Database.Event.EnumType.Exception,
                                    string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
                return false;
            }
        }

        private void ExecuteAddSubOrder()
        {
            try
            {
                EditEnabled = true;
                EnabledCC = true;
                _selectedCommand = CommandType.AddSubOrder;
                Detailed = new OrderViewModel();
                Detailed.Initialize(_warehouse);
                Detailed.EnableOrderAdd = false;
                Detailed.EnableOrderEdit = false;
                Detailed.EnableSubOrderEdit = true;
                Detailed.EnableSKUEdit = true;
                Detailed.ValidationEnabled = true;
                Detailed.ReferenceOrderID = SelectedOrder.OrderID;
                Detailed.ReferenceSubOrderID = 0;
                Detailed.ERPID = SelectedOrder.ERPID;
                Detailed.ERPIDRef = SelectedOrder.ERPIDRef;
                Detailed.OrderID = SelectedOrder.OrderID;
                Detailed.Destination = SelectedOrder.Destination;
                Detailed.ReleaseTime = SelectedOrder.ReleaseTime;
                Detailed.SubOrderERPID = 0;
                Detailed.SubOrderName = "";
                Detailed.SKUID = "";
                Detailed.SKUBatch = "";
                Detailed.SKUQty = 0;
                Detailed.Status = 0;
            }
            catch (Exception e)
            {
                _warehouse.AddEvent(Database.Event.EnumSeverity.Error, Database.Event.EnumType.Exception,
                                    string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
            }
        }

        private bool CanExecuteAddSubOrder()
        {
            try
            {
                return SelectedOrder != null && SelectedOrder.ERPID == null &&
                       SelectedOrder.Status == EnumWMSOrderStatus.Waiting && SelectedOrder.ReleaseTime == SqlDateTime.MaxValue.Value && 
                       !EditEnabled && AccessLevel/10 >= 2;
            }
            catch (Exception e)
            {
                _warehouse.AddEvent(Database.Event.EnumSeverity.Error, Database.Event.EnumType.Exception,
                                    string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
                return false;
            }
        }
        private void ExecuteEditSubOrder()
        {
            try
            {
                EditEnabled = true;
                EnabledCC = true;
                _selectedCommand = CommandType.EditSubOrder;
                Detailed = new OrderViewModel();
                Detailed.Initialize(_warehouse);
                Detailed.ValidationEnabled = true;
                Detailed.EnableOrderAdd = false;
                Detailed.EnableOrderEdit = false;
                Detailed.EnableSubOrderEdit = true;
                Detailed.EnableSKUEdit = false;
                Detailed.ReferenceOrderID = SelectedSubOrder.OrderID;
                Detailed.ReferenceSubOrderID = SelectedSubOrder.SubOrderID;
                Detailed.ID = SelectedSubOrder.ID;
                Detailed.ERPID = SelectedSubOrder.ERPID;
                Detailed.ERPIDRef = SelectedOrder.ERPIDRef;                
                Detailed.OrderID = SelectedSubOrder.OrderID;
                Detailed.Destination = SelectedSubOrder.Destination;
                Detailed.ReleaseTime = SelectedSubOrder.ReleaseTime;
                Detailed.SubOrderID = SelectedSubOrder.SubOrderID;
                Detailed.SubOrderERPID = SelectedSubOrder.SubOrderERPID;
                Detailed.SubOrderName = SelectedSubOrder.SubOrderName;
                Detailed.SKUID = SelectedSubOrder.SKUID;
                Detailed.SKUBatch = SelectedSubOrder.SKUBatch;
                Detailed.SKUQty = SelectedSubOrder.SKUQty;
                Detailed.Status = SelectedSubOrder.Status;
            }
            catch (Exception e)
            {
                _warehouse.AddEvent(Database.Event.EnumSeverity.Error, Database.Event.EnumType.Exception,
                                    string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
            }
        }
        private bool CanExecuteEditSubOrder()
        {
            try
            {
                return SelectedSubOrder != null && SelectedOrder.ERPID == null && 
                       SelectedOrder.Status == EnumWMSOrderStatus.Waiting && SelectedOrder.ReleaseTime == SqlDateTime.MaxValue.Value && 
                       !EditEnabled && AccessLevel/10 >= 2;
            }
            catch (Exception e)
            {
                _warehouse.AddEvent(Database.Event.EnumSeverity.Error, Database.Event.EnumType.Exception,
                                    string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
                return false;
            }
        }
        private void ExecuteDeleteSubOrder()
        {
            try
            {
                EditEnabled = true;
                EnabledCC = true;
                _selectedCommand = CommandType.DeleteSubOrder;
                Detailed = SelectedSubOrder;
            }
            catch (Exception e)
            {
                _warehouse.AddEvent(Database.Event.EnumSeverity.Error, Database.Event.EnumType.Exception,
                                    string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
            }
        }
        private bool CanExecuteDeleteSubOrder()
        {
            try
            {
                return SelectedSubOrder != null && SelectedOrder.ERPID == null &&
                       SelectedOrder.Status == EnumWMSOrderStatus.Waiting && SelectedOrder.ReleaseTime == SqlDateTime.MaxValue.Value && 
                       DataListSubOrder.Count > 1 && !EditEnabled && AccessLevel/10 >= 2;
            }
            catch (Exception e)
            {
                _warehouse.AddEvent(Database.Event.EnumSeverity.Error, Database.Event.EnumType.Exception,
                                    string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
                return false;
            }
        }

        private void ExecuteAddSKU()
        {
            try
            {
                EditEnabled = true;
                EnabledCC = true;
                _selectedCommand = CommandType.AddSKU;
                Detailed = new OrderViewModel();
                Detailed.Initialize(_warehouse);
                Detailed.ValidationEnabled = true;
                Detailed.EnableOrderAdd = false;
                Detailed.EnableOrderEdit = false;
                Detailed.EnableSubOrderEdit = false;
                Detailed.EnableSKUEdit = true;
                Detailed.ReferenceOrderID = SelectedSubOrder.OrderID;
                Detailed.ReferenceSubOrderID = SelectedSubOrder.SubOrderID;
                Detailed.ERPID = SelectedSubOrder.ERPID;
                Detailed.ERPIDRef = SelectedOrder.ERPIDRef;
                Detailed.OrderID = SelectedSubOrder.OrderID;
                Detailed.Destination = SelectedSubOrder.Destination;
                Detailed.ReleaseTime = SelectedSubOrder.ReleaseTime;
                Detailed.SubOrderID = SelectedSubOrder.SubOrderID;
                Detailed.SubOrderERPID = SelectedSubOrder.SubOrderERPID;
                Detailed.SubOrderName = SelectedSubOrder.SubOrderName;
                Detailed.SKUID = "";
                Detailed.SKUBatch = "";
                Detailed.SKUQty = 1;
                Detailed.Status = SelectedSKU.Status;
            }
            catch (Exception e)
            {
                _warehouse.AddEvent(Database.Event.EnumSeverity.Error, Database.Event.EnumType.Exception,
                                    string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
            }
        }

        private bool CanExecuteAddSKU()
        {
            try
            {
                return SelectedSubOrder != null && SelectedOrder.ERPID == null &&
                       SelectedOrder.Status == EnumWMSOrderStatus.Waiting && SelectedOrder.ReleaseTime == SqlDateTime.MaxValue.Value && 
                       !EditEnabled && AccessLevel/10 >= 2;
            }
            catch (Exception e)
            {
                _warehouse.AddEvent(Database.Event.EnumSeverity.Error, Database.Event.EnumType.Exception,
                                    string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
                return false;
            }
        }
        private void ExecuteEditSKU()
        {
            try
            {
                EditEnabled = true;
                EnabledCC = true;
                _selectedCommand = CommandType.EditSKU;
                Detailed = new OrderViewModel();
                Detailed.Initialize(_warehouse);
                Detailed.ValidationEnabled = true;
                Detailed.EnableOrderAdd = false;
                Detailed.EnableOrderEdit = false;
                Detailed.EnableSubOrderEdit = false;
                Detailed.EnableSKUEdit = true;
                Detailed.ReferenceOrderID = SelectedSKU.OrderID;
                Detailed.ReferenceSubOrderID = SelectedSKU.SubOrderID;
                Detailed.ID = SelectedSKU.ID;
                Detailed.ERPID = SelectedSKU.ERPID;
                Detailed.ERPIDRef = SelectedOrder.ERPIDRef;
                Detailed.OrderID = SelectedSKU.OrderID;
                Detailed.Destination = SelectedSKU.Destination;
                Detailed.ReleaseTime = SelectedSKU.ReleaseTime;
                Detailed.SubOrderID = SelectedSKU.SubOrderID;
                Detailed.SubOrderERPID = SelectedSKU.SubOrderERPID;
                Detailed.SubOrderName = SelectedSKU.SubOrderName;
                Detailed.SKUID = SelectedSKU.SKUID;
                Detailed.SKUBatch = SelectedSKU.SKUBatch;
                Detailed.SKUQty = SelectedSKU.SKUQty;
                Detailed.Status = SelectedSKU.Status;
            }
            catch (Exception e)
            {
                _warehouse.AddEvent(Database.Event.EnumSeverity.Error, Database.Event.EnumType.Exception,
                                    string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
            }
        }
        private bool CanExecuteEditSKU()
        {
            try
            {
                return SelectedSKU != null && SelectedOrder.ERPID == null &&
                       SelectedOrder.Status == EnumWMSOrderStatus.Waiting && SelectedOrder.ReleaseTime == SqlDateTime.MaxValue.Value && 
                       !EditEnabled && AccessLevel/10 >= 2;
            }
            catch (Exception e)
            {
                _warehouse.AddEvent(Database.Event.EnumSeverity.Error, Database.Event.EnumType.Exception,
                                    string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
                return false;
            }
        }
        private void ExecuteDeleteSKU()
        {
            try
            {
                EditEnabled = true;
                EnabledCC = true;
                _selectedCommand = CommandType.DeleteSKU;
                Detailed = SelectedSKU;
            }
            catch (Exception e)
            {
                _warehouse.AddEvent(Database.Event.EnumSeverity.Error, Database.Event.EnumType.Exception,
                                    string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
            }
        }
        private bool CanExecuteDeleteSKU()
        {
            try
            {
                return SelectedSKU != null && SelectedOrder.ERPID == null &&
                       SelectedOrder.Status == EnumWMSOrderStatus.Waiting && SelectedOrder.ReleaseTime == SqlDateTime.MaxValue.Value && 
                       !EditEnabled && DataListSKU.Count > 1 && AccessLevel/10 >= 2;
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
                EditEnabled = false;
                EnabledCC = false;
                if (Detailed != null)
                {
                    Detailed.ValidationEnabled = false;
                    Detailed.EnableOrderEdit = false;
                    Detailed.EnableSubOrderEdit = false;
                    Detailed.EnableSKUEdit = false;
                }
                Detailed = SelectedSKU;
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
                return EditEnabled;
            }
            catch (Exception e)
            {
                _warehouse.AddEvent(Database.Event.EnumSeverity.Error, Database.Event.EnumType.Exception, 
                                    string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
                return false;
            }
        }
        private async Task ExecuteConfirm()
        {
            int orderid, suborderid;
            string skuid;

            try
            {
                EditEnabled = false;
                EnabledCC = false;
                try
                {
                    switch (_selectedCommand)
                    {
                        case CommandType.AddOrder:
                            Detailed.Order.SubOrderERPID = Detailed.Order.SubOrderID;
                            _dbservicewms.AddOrder(Detailed.Order);
                            orderid = Detailed.OrderID;
                            await ExecuteRefresh();
                            SelectedOrder = DataListOrder.FirstOrDefault(p => p.OrderID == orderid);
                            _dbservicewms.AddLog(_accessUser, EnumLogWMS.Event, "UI", $"Add order: {Detailed.Order.ToString()}");
                            break;
                        case CommandType.EditOrder:
                            _dbservicewms.UpdateOrders(SelectedOrder.ERPID, SelectedOrder.OrderID, Detailed.Order);
                            var loe = from d in DataListOrder
                                      where d.OrderID == SelectedOrder.OrderID
                                      select d;
                            foreach (var l in loe)
                            {
                                l.OrderID = Detailed.OrderID;
                                l.Destination = Detailed.Destination;
                                l.ReleaseTime = Detailed.ReleaseTime;
                            }
                            _dbservicewms.AddLog(_accessUser, EnumLogWMS.Event, "UI", $"Edit order: {Detailed.Order.ToString()}");
                            break;
                        case CommandType.DeleteOrder:
                            _dbservicewms.DeleteOrders(SelectedOrder.ERPID, SelectedOrder.OrderID);
                            _dbservicewms.AddLog(_accessUser, EnumLogWMS.Event, "UI", $"Delete order: {Detailed.Order.ToString()}");
                            await ExecuteRefresh();
                            break;
                        case CommandType.AddSubOrder:
                            Detailed.Order.SubOrderERPID = Detailed.Order.SubOrderID;
                            _dbservicewms.AddOrder(Detailed.Order);
                            orderid = Detailed.OrderID;
                            suborderid = Detailed.SubOrderID;
                            await ExecuteRefresh();
                            SelectedSubOrder = DataListSubOrder.FirstOrDefault(p => p.OrderID == orderid && p.SubOrderID == suborderid);
                            _dbservicewms.AddLog(_accessUser, EnumLogWMS.Event, "UI", $"Add suborder: {Detailed.Order.ToString()}");
                            break;
                        case CommandType.EditSubOrder:
                            Detailed.Order.SubOrderERPID = Detailed.Order.SubOrderID;
                            _dbservicewms.UpdateSubOrders(Detailed.ERPID, SelectedSubOrder.OrderID, SelectedSubOrder.SubOrderID, Detailed.Order);
                            var lse = from d in DataListSubOrder
                                      where d.OrderID == SelectedSubOrder.OrderID && d.SubOrderID == SelectedSubOrder.SubOrderID
                                      select d;
                            foreach (var l in lse)
                            {
                                l.SubOrderID = Detailed.SubOrderID;
                                l.SubOrderName = Detailed.SubOrderName;
                            }
                            SelectedSubOrder.SubOrderID = Detailed.SubOrderID;
                            SelectedSubOrder.SubOrderName = Detailed.SubOrderName;
                            _dbservicewms.AddLog(_accessUser, EnumLogWMS.Event, "UI", $"Edit suborder: {Detailed.Order.ToString()}");
                            break;
                        case CommandType.DeleteSubOrder:
                            _dbservicewms.DeleteSubOrders(Detailed.ERPID, Detailed.OrderID, Detailed.SubOrderID);
                            _dbservicewms.AddLog(_accessUser, EnumLogWMS.Event, "UI", $"Delete suborder: {Detailed.Order.ToString()}");
                            await ExecuteRefresh();
                            break;
                        case CommandType.AddSKU:
                            if (Detailed.Order.SubOrderName == null)
                                Detailed.Order.SubOrderName = Detailed.Order.SubOrderID.ToString();
                            Detailed.Order.SubOrderERPID = Detailed.Order.SubOrderID;
                            _dbservicewms.AddSKU(Detailed.Order);
                            orderid = Detailed.OrderID;
                            suborderid = Detailed.SubOrderID;
                            skuid = Detailed.SKUID;
                            _dbservicewms.AddLog(_accessUser, EnumLogWMS.Event, "UI", $"Add SKU: {Detailed.Order.ToString()}");
                            await ExecuteRefresh();
                            SelectedSubOrder = DataListSubOrder.FirstOrDefault(p => p.OrderID == orderid && p.SubOrderID == suborderid);
                            SelectedSKU = DataListSKU.FirstOrDefault(p => p.OrderID == orderid && p.SubOrderID == suborderid && p.SKUID == skuid);
                            break;
                        case CommandType.EditSKU:
                            Detailed.Order.SubOrderERPID = Detailed.Order.SubOrderID;
                            _dbservicewms.UpdateSKU(Detailed.Order);
                            SelectedSKU.SKUID = Detailed.SKUID;
                            SelectedSKU.SKUBatch = Detailed.SKUBatch;
                            SelectedSKU.SKUQty = Detailed.SKUQty;
                            _dbservicewms.AddLog(_accessUser, EnumLogWMS.Event, "UI", $"Edit SKU: {Detailed.Order.ToString()}");
                            break;
                        case CommandType.DeleteSKU:
                            _dbservicewms.DeleteSKU(Detailed.Order);
                            _dbservicewms.AddLog(_accessUser, EnumLogWMS.Event, "UI", $"Delete SKU: {Detailed.Order.ToString()}");
                            await ExecuteRefresh();
                            break;
                        default:
                            break;
                    }
                    if (Detailed != null)
                    {
                        Detailed.EnableOrderEdit = false;
                        Detailed.EnableSubOrderEdit = false;
                        Detailed.EnableSKUEdit = false;
                        Detailed.ValidationEnabled = false;
                    }
                }
                catch (Exception e)
                {
                    _warehouse.AddEvent(Event.EnumSeverity.Error, Event.EnumType.Exception, e.Message);
                }
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
                return EditEnabled && Detailed.AllPropertiesValid && AccessLevel/10 >= 1;
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
                int? erpid= SelectedOrder == null ? -1 : SelectedOrder.ERPID;
                int orderid = SelectedOrder == null ? -1 : SelectedOrder.OrderID;
                _suborderid = SelectedSubOrder == null ? -1 : SelectedSubOrder.SubOrderID;
                _skuid = SelectedSKU == null ? -1 : SelectedSKU.ID;
                await ExecuteRefreshOrder();
                if (orderid != -1)
                    SelectedOrder = DataListOrder.FirstOrDefault(p => p.ERPID == erpid && p.OrderID == orderid);
            }
            catch (Exception e)
            {
                _warehouse.AddEvent(Database.Event.EnumSeverity.Error, Database.Event.EnumType.Exception,
                                    string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
            }
        }
        private async Task ExecuteRefreshOrder()
        {
            try
            {
                var orders = await _dbservicewms.GetOrdersDistinct(DateTime.Now.AddDays(-1), DateTime.MaxValue, (int)EnumWMSOrderStatus.ReadyToTake);
                DataListOrder.Clear();
                foreach (var p in orders)
                    DataListOrder.Add(new OrderViewModel
                    {
                        ID = 0,
                        ERPID = p.ERPID,
                        ERPIDRef = p.ERPIDStokbar,
                        OrderID = p.OrderID,
                        Destination = p.Destination,
                        ReleaseTime = p.ReleaseTime,
                        SubOrderID = p.SubOrderID,
                        SubOrderERPID = p.SubOrderERPID,
                        SubOrderName = p.SubOrderName,
                        SKUID = null,
                        SKUBatch = null,
                        SKUQty = 0,
                        Status = (EnumWMSOrderStatus)p.Status
                    });
                foreach (var l in DataListOrder)
                    l.Initialize(_warehouse);
            }
            catch (Exception e)
            {
                _warehouse.AddEvent(Database.Event.EnumSeverity.Error, Database.Event.EnumType.Exception, 
                                    string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
            }
        }
        private void ExecuteRefreshSubOrder()
        {
            try
            {
                DataListSubOrder.Clear();
                if( SelectedOrder != null )
                {
                    foreach (var p in _dbservicewms.GetSubOrdersDistinct(SelectedOrder.ERPID, SelectedOrder.OrderID))
                        DataListSubOrder.Add(new OrderViewModel
                        {
                            ID = p.ID,
                            ERPID = p.ERP_ID,
                            ERPIDRef = null,
                            OrderID = p.OrderID,
                            Destination = p.Destination,
                            ReleaseTime = p.ReleaseTime,
                            SubOrderID = p.SubOrderID,
                            SubOrderERPID = p.SubOrderERPID,
                            SubOrderName = p.SubOrderName,
                            SKUID = p.SKU_ID,
                            SKUBatch = p.SKU_Batch,
                            SKUQty = p.SKU_Qty,
                            Status = (EnumWMSOrderStatus)p.Status
                        });
                    foreach (var l in DataListOrder)
                        l.Initialize(_warehouse);
                    if (SelectedOrder != null)
                    {
                        if (_suborderid != -1)
                            SelectedSubOrder = DataListSubOrder.FirstOrDefault(p => p.SubOrderID == _suborderid);
                        if (SelectedSubOrder == null)
                            SelectedSubOrder = DataListSubOrder.FirstOrDefault();
                    }
                    ExecuteRefreshSKU();
                }
            }
            catch (Exception e)
            {
                _warehouse.AddEvent(Database.Event.EnumSeverity.Error, Database.Event.EnumType.Exception,
                                    string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
            }
        }
        private void ExecuteRefreshSKU()
        {
            try
            {
                DataListSKU.Clear();
                if (SelectedSubOrder != null)
                {
                    foreach (var p in _dbservicewms.GetSKUs(SelectedSubOrder.ERPID, SelectedSubOrder.OrderID, SelectedSubOrder.SubOrderID))
                        DataListSKU.Add(new OrderViewModel
                        {
                            ID = p.ID,
                            ERPID = p.ERP_ID,
                            ERPIDRef = null,
                            OrderID = p.OrderID,
                            Destination = p.Destination,
                            ReleaseTime = p.ReleaseTime,
                            SubOrderID = p.SubOrderID,
                            SubOrderERPID = p.SubOrderERPID,
                            SubOrderName = p.SubOrderName,
                            SKUID = p.SKU_ID,
                            SKUBatch = p.SKU_Batch,
                            SKUQty = p.SKU_Qty,
                            Status = (EnumWMSOrderStatus)p.Status
                        });
                    foreach (var l in DataListOrder)
                        l.Initialize(_warehouse);
                    if (_skuid != -1)
                        SelectedSKU = DataListSKU.FirstOrDefault(p => p.ID == _skuid);
                    if (SelectedSKU == null)
                        SelectedSKU = DataListSKU.FirstOrDefault();
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
                if (vm is OrdersViewModel)
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
