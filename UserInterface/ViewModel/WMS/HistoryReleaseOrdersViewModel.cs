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
using UserInterface.ProxyWMS_UI;
using UserInterface.Services;
using Warehouse.Model;
using WCFClients;

namespace UserInterface.ViewModel
{
    public sealed class HistoryReleaseOrdersViewModel : ViewModelBase
    {

        #region members
        private ObservableCollection<ReleaseOrderViewModel> _dataListOrder;
        private ObservableCollection<ReleaseOrderViewModel> _dataListSubOrder;
        private ObservableCollection<CommandWMSViewModel> _dataListCommand;
        private ReleaseOrderViewModel _selectedOrder;
        private ReleaseOrderViewModel _selectedSubOrder;
        private CommandWMSViewModel _selectedCommand;
        private ReleaseOrderViewModel _detailedOrder;
        private BasicWarehouse _warehouse;
        private DBServiceWMS _dbservicewms;
        private HistoryDateTimePickerViewModel _dateFrom;
        private HistoryDateTimePickerViewModel _dateTo;
        private int _records;
        private int _accessLevel;
        private string _accessUser;
        private int? _suborderid;
        #endregion

        #region properites
        public RelayCommand Refresh { get; private set; }
        public RelayCommand RefreshSubOrder { get; private set; }
        public RelayCommand RefreshCommand { get; private set; }

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
                        {
                            DetailedOrder = SelectedOrder;
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
        public HistoryDateTimePickerViewModel DateFrom
        {
            get { return _dateFrom; }
            set
            {
                if (_dateFrom != value)
                {
                    _dateFrom = value;
                    RaisePropertyChanged("DateFrom");
                }
            }
        }
        public HistoryDateTimePickerViewModel DateTo
        {
            get { return _dateTo; }
            set
            {
                if (_dateTo != value)
                {
                    _dateTo = value;
                    RaisePropertyChanged("DateTo");
                }
            }
        }
        public int Records
        {
            get { return _records; }
            set
            {
                if (_records != value)
                {
                    _records = value;
                    RaisePropertyChanged("Records");
                }
            }
        }
        #endregion

        #region initialization
        public HistoryReleaseOrdersViewModel()
        {
            DetailedOrder = null;
            SelectedOrder = null;

            Refresh = new RelayCommand(async () => await ExecuteRefresh());
            RefreshSubOrder = new RelayCommand(async () => await ExecuteRefreshSubOrder());
            RefreshCommand = new RelayCommand(async () => await ExecuteRefreshCommandWMS());
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
                DateFrom = new HistoryDateTimePickerViewModel { TimeStamp = DateTime.Now.AddDays(-1) };
                DateFrom.Initialize(_warehouse);
                DateTo = new HistoryDateTimePickerViewModel { TimeStamp = DateTime.Now.AddHours(+1) };
                DateTo.Initialize(_warehouse);
                Records = 0;
                _accessUser = "";
                Messenger.Default.Register<MessageAccessLevel>(this, (mc) => { AccessLevel = mc.AccessLevel; _accessUser = mc.User; });
                Messenger.Default.Register<MessageViewChanged>(this, (vm) => ExecuteViewActivated(vm.ViewModel));
            }
            catch (Exception e)
            {
                _warehouse.AddEvent(Database.Event.EnumSeverity.Error, Database.Event.EnumType.Exception, e.Message);
                throw new Exception(string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
            }
        }
        #endregion

        #region commands
        private async Task ExecuteRefresh()
        {
            try
            {
                await ExecuteRefreshOrder();
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
                int? erpid = SelectedOrder?.ERPID;
                int? orderid = SelectedOrder?.OrderID;
                _suborderid = SelectedSubOrder?.SubOrderID;

                var orders = await _dbservicewms.GetHistOrdersWithCount(DateFrom.TimeStamp, DateTo.TimeStamp, -1, null, null);
                DataListOrder.Clear();
                foreach (var p in orders)
                    DataListOrder.Add(new ReleaseOrderViewModel
                    {
                        ERPID = p.ERPID,
                        ERPIDref = p.ERPIDStokbar,
                        OrderID = p.OrderID,
                        Destination = p.Destination,
                        ReleaseTime = p.ReleaseTime,
                        LastChange = p.LastChange,
                        Portion = $"{p.CountActive}/{p.CountAll} - {p.CountMoveDone}/{p.CountAll} - {p.CountFinished}/{p.CountAll}",
                        Status = (EnumWMSOrderStatus)p.Status
                    });
                foreach (var l in DataListOrder)
                    l.Initialize(_warehouse);
                Records = DataListOrder.Count();
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
                DataListSubOrder.Clear();
                if(SelectedOrder != null)
                {
                    var suborders = await _dbservicewms.GetHistSubOrdersBySKUWithCount(SelectedOrder.ERPID, SelectedOrder.OrderID);
                    DataListSubOrder.Clear();
                    foreach (var p in suborders)
                        DataListSubOrder.Add(new ReleaseOrderViewModel
                        {
                            ID = p.WMSID,
                            ERPID = p.ERPID,
                            OrderID = p.OrderID,
                            SubOrderID = p.SubOrderID,
                            SubOrderERPID = p.SubOrderERPID,
                            SubOrderName = p.SubOrderName,
                            SKUID = p.SKUID,
                            SKUBatch = p.SKUBatch,
                            SKUQty = p.SKUQty,
                            Portion = $"{p.CountActive}/{p.CountAll} - {p.CountFinished}/{p.CountAll}",
                            Status = (EnumWMSOrderStatus)p.Status
                        });
                    foreach (var l in DataListOrder)
                        l.Initialize(_warehouse);
                    if (_suborderid != null)
                        SelectedSubOrder = DataListSubOrder.FirstOrDefault(p => p.SubOrderID == _suborderid);
                    if(SelectedSubOrder == null)
                        SelectedSubOrder = DataListSubOrder.FirstOrDefault();
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
                int? wmsid = SelectedCommand?.WMSID;
                DataListCommand.Clear();
                if( SelectedSubOrder != null )
                {
                    var cmds = await _dbservicewms.GetHistCommandsWMSForSubOrder(SelectedSubOrder.ID);
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
                            Time = cmd.Time,
                            Status = (EnumCommandWMSStatus)cmd.Status,
                        });
                    }
                    foreach (var l in DataListCommand)
                        l.Initialize(_warehouse);
                    if (wmsid != null)
                        SelectedCommand = DataListCommand.FirstOrDefault(p => p.WMSID == wmsid);
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
                if (vm is HistoryReleaseOrdersViewModel)
                {
                    // ExecuteRefresh();
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
