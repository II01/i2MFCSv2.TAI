using Database;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.CommandWpf;
using GalaSoft.MvvmLight.Messaging;
using System;
using System.Collections.ObjectModel;
using System.Data.SqlTypes;
using System.Diagnostics;
using System.Linq;
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
        #endregion

        #region properites
        public RelayCommand Refresh { get; private set; }
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
                            Detailed = SelectedSKU;
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

            Refresh = new RelayCommand(() => ExecuteRefresh());
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
            Confirm = new RelayCommand(() => ExecuteConfirm(), CanExecuteConfirm);
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
                Messenger.Default.Register<MessageViewChanged>(this, vm => ExecuteViewActivated(vm.ViewModel));
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
                Detailed.EnableOrderEdit = true;
                Detailed.EnableSubOrderEdit = true;
                Detailed.EnableSKUEdit = true;
                Detailed.ValidationEnabled = true;
                Detailed.ReferenceOrderID = 0;
                Detailed.ReferenceSubOrderID = 0;
                Detailed.ERPID = null;
                Detailed.OrderID = 0;
                Detailed.Destination = "";
                Detailed.ReleaseTime = SqlDateTime.MaxValue.Value;
                Detailed.SubOrderID = 0;
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
                Detailed.EnableOrderEdit = true;
                Detailed.EnableSubOrderEdit = false;
                Detailed.EnableSKUEdit = false;
                Detailed.ReferenceOrderID = SelectedOrder.OrderID;
                Detailed.ReferenceSubOrderID = SelectedOrder.SubOrderID;
                Detailed.ID = SelectedOrder.ID;
                Detailed.ERPID = SelectedOrder.ERPID;
                Detailed.OrderID = SelectedOrder.OrderID;
                Detailed.Destination = SelectedOrder.Destination;
                Detailed.ReleaseTime = SelectedOrder.ReleaseTime;
                Detailed.SubOrderID = SelectedOrder.SubOrderID;
                Detailed.SubOrderName = SelectedOrder.SubOrderName;
                Detailed.SKUID = SelectedOrder.SKUID;
                Detailed.SKUBatch = SelectedOrder.SKUBatch;
                Detailed.SKUQty = SelectedOrder.SKUQty;
                Detailed.Status = SelectedOrder.Status;
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
                return SelectedOrder != null && !EditEnabled && AccessLevel/10 >= 2;
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
                return SelectedOrder != null && !EditEnabled && AccessLevel/10 >= 2;
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
                Detailed.EnableOrderEdit = false;
                Detailed.EnableSubOrderEdit = true;
                Detailed.EnableSKUEdit = true;
                Detailed.ValidationEnabled = true;
                Detailed.ReferenceOrderID = SelectedOrder.OrderID;
                Detailed.ReferenceSubOrderID = 0;
                Detailed.ERPID = SelectedOrder.ERPID;
                Detailed.OrderID = SelectedOrder.OrderID;
                Detailed.Destination = SelectedOrder.Destination;
                Detailed.ReleaseTime = SelectedOrder.ReleaseTime;
                Detailed.SubOrderID = 0;
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
                return SelectedOrder != null && !EditEnabled && AccessLevel/10 >= 2;
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
                Detailed.EnableOrderEdit = false;
                Detailed.EnableSubOrderEdit = true;
                Detailed.EnableSKUEdit = false;
                Detailed.ReferenceOrderID = SelectedSubOrder.OrderID;
                Detailed.ReferenceSubOrderID = SelectedSubOrder.SubOrderID;
                Detailed.ID = SelectedSubOrder.ID;
                Detailed.ERPID = SelectedSubOrder.ERPID;
                Detailed.OrderID = SelectedSubOrder.OrderID;
                Detailed.Destination = SelectedSubOrder.Destination;
                Detailed.ReleaseTime = SelectedSubOrder.ReleaseTime;
                Detailed.SubOrderID = SelectedSubOrder.SubOrderID;
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
                return SelectedSubOrder != null && !EditEnabled && AccessLevel/10 >= 2;
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
                return SelectedSubOrder != null && DataListSubOrder.Count > 1 && !EditEnabled && AccessLevel/10 >= 2;
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
                Detailed.EnableOrderEdit = false;
                Detailed.EnableSubOrderEdit = false;
                Detailed.EnableSKUEdit = true;
                Detailed.ReferenceOrderID = SelectedSubOrder.OrderID;
                Detailed.ReferenceSubOrderID = SelectedSubOrder.SubOrderID;
                Detailed.ERPID = SelectedSubOrder.ERPID;
                Detailed.OrderID = SelectedSubOrder.OrderID;
                Detailed.Destination = SelectedSubOrder.Destination;
                Detailed.ReleaseTime = SelectedSubOrder.ReleaseTime;
                Detailed.SubOrderID = SelectedSubOrder.SubOrderID;
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
                return SelectedSubOrder != null && !EditEnabled && AccessLevel/10 >= 2;
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
                Detailed.EnableOrderEdit = false;
                Detailed.EnableSubOrderEdit = false;
                Detailed.EnableSKUEdit = true;
                Detailed.ReferenceOrderID = SelectedSKU.OrderID;
                Detailed.ReferenceSubOrderID = SelectedSKU.SubOrderID;
                Detailed.ID = SelectedSKU.ID;
                Detailed.ERPID = SelectedSKU.ERPID;
                Detailed.OrderID = SelectedSKU.OrderID;
                Detailed.Destination = SelectedSKU.Destination;
                Detailed.ReleaseTime = SelectedSKU.ReleaseTime;
                Detailed.SubOrderID = SelectedSKU.SubOrderID;
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
                return SelectedSKU != null && !EditEnabled && AccessLevel/10 >= 2;
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
                return SelectedSKU != null && !EditEnabled && DataListSKU.Count > 1 && AccessLevel/10 >= 2;
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
        private void ExecuteConfirm()
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
                            _dbservicewms.AddOrder(Detailed.Order);
                            orderid = Detailed.OrderID;
                            ExecuteRefresh();
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
                            Detailed.Status = EnumWMSOrderStatus.Cancel;
                            _dbservicewms.UpdateOrders(SelectedOrder.ERPID, SelectedOrder.OrderID, Detailed.Order);
                            _dbservicewms.AddLog(_accessUser, EnumLogWMS.Event, "UI", $"Delete order: {Detailed.Order.ToString()}");
                            ExecuteRefresh();
                            break;
                        case CommandType.AddSubOrder:
                            if (Detailed.Order.SubOrderName == null)
                                Detailed.Order.SubOrderName = Detailed.Order.SubOrderID.ToString();
                            _dbservicewms.AddOrder(Detailed.Order);
                            orderid = Detailed.OrderID;
                            suborderid = Detailed.SubOrderID;
                            ExecuteRefresh();
                            SelectedSubOrder = DataListSubOrder.FirstOrDefault(p => p.OrderID == orderid && p.SubOrderID == suborderid);
                            _dbservicewms.AddLog(_accessUser, EnumLogWMS.Event, "UI", $"Add suborder: {Detailed.Order.ToString()}");
                            break;
                        case CommandType.EditSubOrder:
                            if (Detailed.Order.SubOrderName == null)
                                Detailed.Order.SubOrderName = Detailed.Order.SubOrderID.ToString();
                            _dbservicewms.UpdateSubOrders(SelectedSubOrder.OrderID, SelectedSubOrder.SubOrderID, Detailed.Order);
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
                            _dbservicewms.DeleteSubOrders(Detailed.OrderID, Detailed.SubOrderID);
                            _dbservicewms.AddLog(_accessUser, EnumLogWMS.Event, "UI", $"Delete suborder: {Detailed.Order.ToString()}");
                            ExecuteRefresh();
                            break;
                        case CommandType.AddSKU:
                            if (Detailed.Order.SubOrderName == null)
                                Detailed.Order.SubOrderName = Detailed.Order.SubOrderID.ToString();
                            _dbservicewms.AddSKU(Detailed.Order);
                            orderid = Detailed.OrderID;
                            suborderid = Detailed.SubOrderID;
                            skuid = Detailed.SKUID;
                            _dbservicewms.AddLog(_accessUser, EnumLogWMS.Event, "UI", $"Add SKU: {Detailed.Order.ToString()}");
                            ExecuteRefresh();
                            SelectedSubOrder = DataListSubOrder.FirstOrDefault(p => p.OrderID == orderid && p.SubOrderID == suborderid);
                            SelectedSKU = DataListSKU.FirstOrDefault(p => p.OrderID == orderid && p.SubOrderID == suborderid && p.SKUID == skuid);
                            break;
                        case CommandType.EditSKU:
                            _dbservicewms.UpdateSKU(Detailed.Order);
                            SelectedSKU.SKUID = Detailed.SKUID;
                            SelectedSKU.SKUBatch = Detailed.SKUBatch;
                            SelectedSKU.SKUQty = Detailed.SKUQty;
                            _dbservicewms.AddLog(_accessUser, EnumLogWMS.Event, "UI", $"Edit SKU: {Detailed.Order.ToString()}");
                            break;
                        case CommandType.DeleteSKU:
                            _dbservicewms.DeleteSKU(Detailed.Order);
                            _dbservicewms.AddLog(_accessUser, EnumLogWMS.Event, "UI", $"Delete SKU: {Detailed.Order.ToString()}");
                            ExecuteRefresh();
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
        private void ExecuteRefresh()
        {
            try
            {
                int? erpid= SelectedOrder == null ? -1 : SelectedOrder.ERPID;
                int orderid = SelectedOrder == null ? -1 : SelectedOrder.OrderID;
                int suborderid = SelectedSubOrder == null ? -1 : SelectedSubOrder.SubOrderID;
                int skuid = SelectedSKU == null ? -1 : SelectedSKU.ID;
                ExecuteRefreshOrder();
                if (orderid != -1)
                {
                    SelectedOrder = DataListOrder.FirstOrDefault(p => p.ERPID == erpid && p.OrderID == orderid);
                    if (SelectedOrder != null)
                    {
                        if (suborderid != -1)
                            SelectedSubOrder = DataListSubOrder.FirstOrDefault(p => p.SubOrderID == suborderid);
                        if(SelectedSubOrder == null)
                            SelectedSubOrder = DataListSubOrder.FirstOrDefault();
                        if (skuid != -1)
                            SelectedSKU = DataListSKU.FirstOrDefault(p => p.ID == skuid);
                        if(SelectedSKU == null)
                            SelectedSKU = DataListSKU.FirstOrDefault();
                    }
                }
            }
            catch (Exception e)
            {
                _warehouse.AddEvent(Database.Event.EnumSeverity.Error, Database.Event.EnumType.Exception,
                                    string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
            }
        }
        private void ExecuteRefreshOrder()
        {
            try
            {
                DataListOrder.Clear();
                foreach (var p in _dbservicewms.GetOrdersDistinct(10))
                    DataListOrder.Add(new OrderViewModel
                    {
                        ID = p.ID,
                        ERPID = p.ERP_ID,
                        OrderID = p.OrderID,
                        Destination = p.Destination,
                        ReleaseTime = p.ReleaseTime,
                        SubOrderID = p.SubOrderID,
                        SubOrderName = p.SubOrderName,
                        SKUID = p.SKU_ID,
                        SKUBatch = p.SKU_Batch,
                        SKUQty = p.SKU_Qty,
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
                            OrderID = p.OrderID,
                            Destination = p.Destination,
                            ReleaseTime = p.ReleaseTime,
                            SubOrderID = p.SubOrderID,
                            SubOrderName = p.SubOrderName,
                            SKUID = p.SKU_ID,
                            SKUBatch = p.SKU_Batch,
                            SKUQty = p.SKU_Qty,
                            Status = (EnumWMSOrderStatus)p.Status
                        });
                    foreach (var l in DataListOrder)
                        l.Initialize(_warehouse);
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
                            OrderID = p.OrderID,
                            Destination = p.Destination,
                            ReleaseTime = p.ReleaseTime,
                            SubOrderID = p.SubOrderID,
                            SubOrderName = p.SubOrderName,
                            SKUID = p.SKU_ID,
                            SKUBatch = p.SKU_Batch,
                            SKUQty = p.SKU_Qty,
                            Status = (EnumWMSOrderStatus)p.Status
                        });
                    foreach (var l in DataListOrder)
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
        public void ExecuteViewActivated(ViewModelBase vm)
        {
            try
            {
                if (vm is OrdersViewModel)
                {
                    ExecuteRefresh();
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
