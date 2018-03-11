using System;
using System.Linq;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.CommandWpf;
using System.Collections.ObjectModel;
using UserInterface.Services;
using Database;
using Warehouse.Model;
using System.Diagnostics;
using GalaSoft.MvvmLight.Messaging;
using UserInterface.Messages;
using WCFClients;
using UserInterface.DataServiceWMS;
using System.Data.SqlTypes;

namespace UserInterface.ViewModel
{
    public sealed class OrdersViewModel : ViewModelBase
    {
        public enum CommandType { None = 0, AddOrder, EditOrder, DeleteOrder, AddSubOrder, EditSubOrder, DeleteSubOrder, AddSKU, EditSKU, DeleteSKU};

        #region members
        private CommandType _selectedCommand;
        private ObservableCollection<OrderViewModel> _dataList;
        private OrderViewModel _selected;
        private OrderViewModel _detailed;
        private OrderViewModel _managed;
        private bool _editEnabled;
        private bool _enabledCC;
        private BasicWarehouse _warehouse;
        private DBServiceWMS _dbservicewms;
        private int _accessLevel;
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

        public ObservableCollection<OrderViewModel> DataList
        {
            get { return _dataList; }
            set
            {
                if (_dataList != value)
                {
                    _dataList = value;
                    RaisePropertyChanged("DataList");
                }
            }
        }

        public OrderViewModel Selected
        {
            get
            {
                return _selected;
            }
            set
            {
                if (_selected != value)
                {
                    _selected = value;
                    RaisePropertyChanged("Selected");
                    try
                    {
                        if (_selected != null)
                            Detailed = Selected;
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
            Selected = null;
            _managed = new OrderViewModel();
            _managed.Initialize(_warehouse);

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
                DataList = new ObservableCollection<OrderViewModel>();
                foreach (var p in _dbservicewms.GetOrders(10))
                    DataList.Add(new OrderViewModel
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
                        Status = p.Status
                    });
                foreach (var l in DataList)
                    l.Initialize(_warehouse);
                Messenger.Default.Register<MessageAccessLevel>(this, (mc) => { AccessLevel = mc.AccessLevel; });
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
                _managed.EnableOrderEdit = true;
                _managed.EnableSubOrderEdit = true;
                _managed.EnableSKUEdit = true;
                _managed.ValidationEnabled = true;
                _managed.ERPID = 0;
                _managed.OrderID = 0;
                _managed.Destination = "";
                _managed.ReleaseTime = SqlDateTime.MaxValue.Value;
                _managed.SubOrderID = 0;
                _managed.SubOrderName = "";
                _managed.SKUID = "";
                _managed.SKUBatch = "";
                _managed.SKUQty = 0;
                _managed.Status = 0;
                _managed.ReferenceOrderID = _managed.OrderID;
                _managed.ReferenceSubOrderID = _managed.SubOrderID;
                Detailed = _managed;
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
                return !EditEnabled && AccessLevel >= 1;
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
                _managed.ValidationEnabled = true;
                _managed.EnableOrderEdit = true;
                _managed.EnableSubOrderEdit = false;
                _managed.EnableSKUEdit = false;
                _managed.ID = Selected.ID;
                _managed.ERPID = Selected.ERPID;
                _managed.OrderID = Selected.OrderID;
                _managed.Destination = Selected.Destination;
                _managed.ReleaseTime = Selected.ReleaseTime;
                _managed.SubOrderID = Selected.SubOrderID;
                _managed.SubOrderName = Selected.SubOrderName;
                _managed.SKUID = Selected.SKUID;
                _managed.SKUBatch = Selected.SKUBatch;
                _managed.SKUQty = Selected.SKUQty;
                _managed.Status = Selected.Status;
                _managed.ReferenceOrderID = _managed.OrderID;
                _managed.ReferenceSubOrderID = _managed.SubOrderID;
                Detailed = _managed;
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
                return Selected != null && !EditEnabled && AccessLevel >= 1;
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
                Detailed = Selected;
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
                return Selected != null && !EditEnabled && AccessLevel >= 1;
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
                _managed.ValidationEnabled = true;
                _managed.EnableOrderEdit = false;
                _managed.EnableSubOrderEdit = true;
                _managed.EnableSKUEdit = true;
                _managed.ERPID = Selected.ERPID;
                _managed.OrderID = Selected.OrderID;
                _managed.Destination = Selected.Destination;
                _managed.ReleaseTime = Selected.ReleaseTime;
                _managed.SubOrderID = 0;
                _managed.SubOrderName = "";
                _managed.SKUID = "";
                _managed.SKUBatch = "";
                _managed.SKUQty = 0;
                _managed.Status = 0;
                _managed.ReferenceOrderID = _managed.OrderID;
                _managed.ReferenceSubOrderID = _managed.SubOrderID;
                Detailed = _managed;
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
                return Selected != null && !EditEnabled && AccessLevel >= 1;
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
                _managed.ValidationEnabled = true;
                _managed.EnableOrderEdit = false;
                _managed.EnableSubOrderEdit = true;
                _managed.EnableSKUEdit = false;
                _managed.ID = Selected.ID;
                _managed.ERPID = Selected.ERPID;
                _managed.OrderID = Selected.OrderID;
                _managed.Destination = Selected.Destination;
                _managed.ReleaseTime = Selected.ReleaseTime;
                _managed.SubOrderID = Selected.SubOrderID;
                _managed.SubOrderName = Selected.SubOrderName;
                _managed.SKUID = Selected.SKUID;
                _managed.SKUBatch = Selected.SKUBatch;
                _managed.SKUQty = Selected.SKUQty;
                _managed.Status = Selected.Status;
                _managed.ReferenceOrderID = _managed.OrderID;
                _managed.ReferenceSubOrderID = _managed.SubOrderID;
                Detailed = _managed;
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
                return Selected != null && !EditEnabled && AccessLevel >= 1;
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
                Detailed = Selected;
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
                return Selected != null && !EditEnabled && AccessLevel >= 1;
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
                _managed.ValidationEnabled = true;
                _managed.EnableOrderEdit = false;
                _managed.EnableSubOrderEdit = false;
                _managed.EnableSKUEdit = true;
                _managed.ERPID = Selected.ERPID;
                _managed.OrderID = Selected.OrderID;
                _managed.Destination = Selected.Destination;
                _managed.ReleaseTime = Selected.ReleaseTime;
                _managed.SubOrderID = Selected.SubOrderID;
                _managed.SubOrderName = Selected.SubOrderName;
                _managed.SKUID = "";
                _managed.SKUBatch = "";
                _managed.SKUQty = 1;
                _managed.Status = Selected.Status;
                _managed.ReferenceOrderID = _managed.OrderID;
                _managed.ReferenceSubOrderID = _managed.SubOrderID;
                Detailed = _managed;
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
                return Selected != null && !EditEnabled && AccessLevel >= 1;
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
                _managed.ValidationEnabled = true;
                _managed.EnableOrderEdit = false;
                _managed.EnableSubOrderEdit = false;
                _managed.EnableSKUEdit = true;
                _managed.ID = Selected.ID;
                _managed.ERPID = Selected.ERPID;
                _managed.OrderID = Selected.OrderID;
                _managed.Destination = Selected.Destination;
                _managed.ReleaseTime = Selected.ReleaseTime;
                _managed.SubOrderID = Selected.SubOrderID;
                _managed.SubOrderName = Selected.SubOrderName;
                _managed.SKUID = Selected.SKUID;
                _managed.SKUBatch = Selected.SKUBatch;
                _managed.SKUQty = Selected.SKUQty;
                _managed.Status = Selected.Status;
                _managed.ReferenceOrderID = _managed.OrderID;
                _managed.ReferenceSubOrderID = _managed.SubOrderID;
                Detailed = _managed;
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
                return Selected != null && !EditEnabled && AccessLevel >= 1;
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
                Detailed = Selected;
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
                return Selected != null && !EditEnabled && AccessLevel >= 1;
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
                Detailed = Selected;
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
                            Selected = DataList.FirstOrDefault(p => p.OrderID == orderid);
                            break;
                        case CommandType.EditOrder:
                            _dbservicewms.UpdateOrders(Selected.OrderID, Detailed.Order);
                            var loe = from d in DataList
                                      where d.OrderID == Selected.OrderID
                                      select d;
                            foreach (var l in loe)
                            {
                                l.OrderID = Detailed.OrderID;
                                l.Destination = Detailed.Destination;
                                l.ReleaseTime = Detailed.ReleaseTime;
                            }
                            break;
                        case CommandType.DeleteOrder:
                            _dbservicewms.DeleteOrders(Detailed.OrderID);
                            ExecuteRefresh();
                            break;
                        case CommandType.AddSubOrder:
                            _dbservicewms.AddOrder(Detailed.Order);
                            orderid = Detailed.OrderID;
                            suborderid = Detailed.SubOrderID;
                            ExecuteRefresh();
                            Selected = DataList.FirstOrDefault(p => p.OrderID == orderid && p.SubOrderID == suborderid);
                            break;
                        case CommandType.EditSubOrder:
                            _dbservicewms.UpdateSubOrders(Selected.OrderID, Selected.SubOrderID, Detailed.Order);
                            var lse = from d in DataList
                                      where d.OrderID == Selected.OrderID && d.SubOrderID == Selected.SubOrderID
                                      select d;
                            foreach (var l in lse)
                            {
                                l.SubOrderID = Detailed.SubOrderID;
                                l.SubOrderName = Detailed.SubOrderName;
                            }
                            break;
                        case CommandType.DeleteSubOrder:
                            _dbservicewms.DeleteSubOrders(Detailed.OrderID, Detailed.SubOrderID);
                            ExecuteRefresh();
                            break;
                        case CommandType.AddSKU:
                            _dbservicewms.AddSKU(Detailed.Order);
                            orderid = Detailed.OrderID;
                            suborderid = Detailed.SubOrderID;
                            skuid = Detailed.SKUID;
                            ExecuteRefresh();
                            Selected = DataList.FirstOrDefault(p => p.OrderID == orderid && p.SubOrderID == suborderid && p.SKUID == skuid);
                            break;
                        case CommandType.EditSKU:
                            _dbservicewms.UpdateSKU(Detailed.Order);
                            Selected.SKUID = Detailed.SKUID;
                            Selected.SKUBatch = Detailed.SKUBatch;
                            Selected.SKUQty = Detailed.SKUQty;
                            break;
                        case CommandType.DeleteSKU:
                            _dbservicewms.DeleteSKU(Detailed.Order);
                            DataList.Remove(Detailed);
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
                return EditEnabled && Detailed.AllPropertiesValid && AccessLevel >= 1;
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
                OrderViewModel sl = Selected; 
                DataList.Clear();
                foreach (var p in _dbservicewms.GetOrders(10))
                    DataList.Add(new OrderViewModel
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
                        Status = p.Status
                    });
                foreach (var l in DataList)
                    l.Initialize(_warehouse);
                if ( sl != null)
                    Selected = DataList.FirstOrDefault(p => p.ID == sl.ID);
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
                if (vm is PlaceIDsViewModel)
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
