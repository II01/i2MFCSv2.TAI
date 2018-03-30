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
using UserInterface.ProxyWMS_UI;
using UserInterface.Services;
using Warehouse.Model;

namespace UserInterface.ViewModel
{
    public sealed class ReleaseOrdersViewModel : ViewModelBase
    {
        public enum CommandType { None = 0, ReleaseOrder, DeleteOrder, ReleaseRamp };

        #region members
        private CommandType _selectedCmd;
        private ObservableCollection<ReleaseOrderViewModel> _dataListOrder;
        private ObservableCollection<CommandWMSViewModel> _dataListCommand;
        private ObservableCollection<PlaceIDViewModel> _dataListPlace;
        private ObservableCollection<PlaceViewModel> _dataListTU;
        private ReleaseOrderViewModel _selectedOrder;
        private ReleaseOrderViewModel _detailedOrder;
        private CommandWMSViewModel _selectedCommand;
        private PlaceIDViewModel _selectedPlace;
        private bool _editEnabled;
        private bool _enabledCC;
        private bool _visibleOrder;
        private bool _visibleRamp;
        private BasicWarehouse _warehouse;
        private DBServiceWMS _dbservicewms;
        private int _accessLevel;
        #endregion

        #region properites
        public RelayCommand Refresh { get; private set; }
        public RelayCommand CmdReleaseOrder { get; private set; }
        public RelayCommand CmdDeleteOrder { get; private set; }
        public RelayCommand CmdReleaseTruckRamp { get; private set; }
        public RelayCommand Cancel { get; private set; }
        public RelayCommand Confirm { get; private set; }
        public RelayCommand<string> SetDestination { get; private set; }
        public RelayCommand<string> SetReleaseTime { get; private set; }

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

        public ObservableCollection<PlaceIDViewModel> DataListPlace
        {
            get { return _dataListPlace; }
            set
            {
                if (_dataListPlace != value)
                {
                    _dataListPlace = value;
                    RaisePropertyChanged("DataListPlace");
                }
            }
        }

        public ObservableCollection<PlaceViewModel> DataListTU
        {
            get { return _dataListTU; }
            set
            {
                if (_dataListTU != value)
                {
                    _dataListTU = value;
                    RaisePropertyChanged("DataListTU");
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
                        ExecuteRefreshCommandWMS();
                    }
                    catch (Exception e)
                    {
                        _warehouse.AddEvent(Database.Event.EnumSeverity.Error, Database.Event.EnumType.Exception, e.Message);
                        throw new Exception(string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
                    }
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
        public PlaceIDViewModel SelectedPlace
        {
            get
            {
                return _selectedPlace;
            }
            set
            {
                if (_selectedPlace != value)
                {
                    _selectedPlace = value;
                    ExecuteRefreshTU();
                    RaisePropertyChanged("SelectedPlace");
                    RaisePropertyChanged("SelectedPlaceID");
                }
            }
        }
        public string SelectedPlaceID
        {
            get
            {
                return _selectedPlace?.ID;
            }
            set
            {
                if (_selectedPlace.ID != value)
                {
                    _selectedPlace.ID = value;
                    ExecuteRefreshTU();
                    RaisePropertyChanged("SelectedPlaceID");
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
        public bool VisibleOrder
        {
            get { return _visibleOrder; }
            set
            {
                if (_visibleOrder != value)
                {
                    _visibleOrder = value;
                    RaisePropertyChanged("VisibleOrder");
                }
            }
        }
        public bool VisibleRamp
        {
            get { return _visibleRamp; }
            set
            {
                if (_visibleRamp != value)
                {
                    _visibleRamp = value;
                    RaisePropertyChanged("VisibleRamp");
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
        public ReleaseOrdersViewModel()
        {
            DetailedOrder = null;
            SelectedOrder = null;
            SelectedCommand = null;
            SelectedPlace = null;

            EditEnabled = false;
            EnabledCC = false;
            VisibleOrder = true;
            VisibleRamp = false;

            _selectedCmd = CommandType.None;

            Refresh = new RelayCommand(() => ExecuteRefresh());
            CmdReleaseOrder = new RelayCommand(() => ExecuteReleaseOrder(), CanExecuteReleaseOrder);
            CmdDeleteOrder = new RelayCommand(() => ExecuteDeleteOrder(), CanExecuteDeleteOrder);
            CmdReleaseTruckRamp = new RelayCommand(() => ExecuteReleaseTruckRamp(), CanExecuteReleaseTruckRamp);
            Cancel = new RelayCommand(() => ExecuteCancel(), CanExecuteCancel);
            Confirm = new RelayCommand(() => ExecuteConfirm(), CanExecuteConfirm);
            SetDestination = new RelayCommand<string>(dest => ExecuteSetDestination(dest));
            SetReleaseTime = new RelayCommand<string>(rt => ExecuteSetReleaseTime(rt));
        }

        public void Initialize(BasicWarehouse warehouse)
        {
            _warehouse = warehouse;
            _dbservicewms = new DBServiceWMS(_warehouse);
            try
            {
                DataListOrder = new ObservableCollection<ReleaseOrderViewModel>();
                DataListCommand = new ObservableCollection<CommandWMSViewModel>();
                DataListPlace = new ObservableCollection<PlaceIDViewModel>();
                DataListTU = new ObservableCollection<PlaceViewModel>();
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
        private void ExecuteReleaseOrder()
        {
            try
            {
                EditEnabled = true;
                EnabledCC = true;                
                _selectedCmd = CommandType.ReleaseOrder;
                DetailedOrder = new ReleaseOrderViewModel();
                DetailedOrder.Initialize(_warehouse);
                DetailedOrder.ValidationEnabled = true;
                DetailedOrder.ID = SelectedOrder.ID;
                DetailedOrder.ERPID = SelectedOrder.ERPID;
                DetailedOrder.OrderID = SelectedOrder.OrderID;
                DetailedOrder.Destination = SelectedOrder.Destination;
                DetailedOrder.ReleaseTime = SelectedOrder.ReleaseTime;
                DetailedOrder.PortionActive = SelectedOrder.PortionActive;
                DetailedOrder.PortionDone = SelectedOrder.PortionDone;
                DetailedOrder.Status = SelectedOrder.Status;
            }
            catch (Exception e)
            {
                _warehouse.AddEvent(Database.Event.EnumSeverity.Error, Database.Event.EnumType.Exception, 
                                    string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
            }
        }

        private bool CanExecuteReleaseOrder()
        {
            try
            {
                return SelectedOrder != null && SelectedOrder.Status == EnumWMSOrderStatus.Waiting && !EditEnabled && AccessLevel >= 1;
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
                EditEnabled = false;
                EnabledCC = true;
                _selectedCmd = CommandType.DeleteOrder;
                DetailedOrder = new ReleaseOrderViewModel();
                DetailedOrder.Initialize(_warehouse);
                DetailedOrder.ValidationEnabled = true;
                DetailedOrder.ID = SelectedOrder.ID;
                DetailedOrder.ERPID = SelectedOrder.ERPID;
                DetailedOrder.OrderID = SelectedOrder.OrderID;
                DetailedOrder.Destination = SelectedOrder.Destination;
                DetailedOrder.ReleaseTime = SelectedOrder.ReleaseTime;
                DetailedOrder.PortionActive = SelectedOrder.PortionActive;
                DetailedOrder.PortionDone = SelectedOrder.PortionDone;
                DetailedOrder.Status = EnumWMSOrderStatus.Cancel;
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
                return SelectedOrder != null && SelectedOrder.Status < EnumWMSOrderStatus.Cancel && !EditEnabled && AccessLevel >= 1;
            }
            catch (Exception e)
            {
                _warehouse.AddEvent(Database.Event.EnumSeverity.Error, Database.Event.EnumType.Exception,
                                    string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
                return false;
            }
        }

        private void ExecuteReleaseTruckRamp()
        {
            EditEnabled = true;
            EnabledCC = true;
            VisibleOrder = false;
            VisibleRamp = true;
            _selectedCmd = CommandType.ReleaseRamp;
        }

        private bool CanExecuteReleaseTruckRamp()
        {
            try
            {
                return SelectedPlace != null && !EditEnabled && AccessLevel >= 1;
            }
            catch (Exception e)
            {
                _warehouse.AddEvent(Database.Event.EnumSeverity.Error, Database.Event.EnumType.Exception,
                                    string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
                return false;
            }
        }

        private void ExecuteSetDestination(string dest)
        {
            DetailedOrder.Destination = dest;
        }
        private void ExecuteSetReleaseTime(string hours)
        {
            if (double.TryParse(hours, System.Globalization.NumberStyles.AllowDecimalPoint, System.Globalization.CultureInfo.InvariantCulture, out double h))
            {
                DetailedOrder.ReleaseTime = DateTime.Now.AddHours(h);
            }
        }
        private void ExecuteCancel()
        {
            try
            {
                EditEnabled = false;
                EnabledCC = false;
                if (DetailedOrder != null)
                {
                    DetailedOrder.ValidationEnabled = false;
                }
                VisibleOrder = true;
                VisibleRamp = false;
                _selectedCmd = CommandType.None;
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
                return _selectedCmd != CommandType.None;
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
                        case CommandType.ReleaseOrder:
                            _dbservicewms.UpdateOrders(DetailedOrder.ERPID, DetailedOrder.OrderID, DetailedOrder.Order);
                            SelectedOrder.Destination = DetailedOrder.Destination;
                            SelectedOrder.ReleaseTime = DetailedOrder.ReleaseTime;
                            break;
                        case CommandType.DeleteOrder:
                            SelectedOrder.Status = DetailedOrder.Status;
                            using (WMSToUIClient client = new WMSToUIClient())
                            {
                                client.CancelOrder(new DTOOrder
                                {
                                    ERP_ID = DetailedOrder.ERPID,
                                    OrderID = DetailedOrder.OrderID,
                                    ReleaseTime = DetailedOrder.ReleaseTime,
                                    Destination = DetailedOrder.Destination
                                });
                            }
                            break;
                        case CommandType.ReleaseRamp:
                            _dbservicewms.UpdateOrders(SelectedPlace.ID, EnumWMSOrderStatus.Finished);
                            break;
                    }
                    VisibleOrder = true;
                    VisibleRamp = false;
                    _selectedCmd = CommandType.None;
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
                return _selectedCmd == CommandType.DeleteOrder || EditEnabled && DetailedOrder.AllPropertiesValid && AccessLevel >= 1;
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
                ExecuteRefreshOrder();
                ExecuteRefreshPlace();
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
                int? erpid = SelectedOrder?.ERPID;
                int? orderid = SelectedOrder?.OrderID;

                DataListOrder.Clear();
                foreach (var p in _dbservicewms.GetReleaseOrders((int)EnumWMSOrderStatus.Finished))
                    DataListOrder.Add(new ReleaseOrderViewModel
                    {
                        ERPID = p.ERPID,
                        OrderID = p.OrderID,
                        Destination = p.Destination,
                        ReleaseTime = p.ReleaseTime,
                        PortionActive = p.CountAll == 0 ? "0/0 (0%)" : $"{p.CountActive}/{p.CountAll} ({p.CountActive/p.CountAll*100.0}%)",
                        PortionDone = p.CountAll == 0 ? "0/0 (0%)" : $"{p.CountCanceled + p.CountFinihsed}/{p.CountAll} ({(p.CountCanceled+p.CountFinihsed)/p.CountAll*100.0}%)",
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
        private void ExecuteRefreshCommandWMS()
        {
            try
            {
                int? wmsid = SelectedCommand?.WMSID;
                DataListCommand.Clear();
                if( SelectedOrder != null )
                {
                    foreach (var cmd in _dbservicewms.GetCommandsWMSForOrder(SelectedOrder.ERPID, SelectedOrder.OrderID, (int)EnumWMSOrderStatus.Finished))
                    {
                        DataListCommand.Add(new CommandWMSViewModel
                        {
                            WMSID = cmd.ID,
                            OrderERPID = cmd.OrderERPID,
                            OrderID = cmd.Order_ID,
                            OrderOrderID = cmd.OrderOrderID,
                            OrderSubOrderID = cmd.OrderSubOrderID,
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
        private void ExecuteRefreshPlace()
        {
            try
            {
                string placeid = SelectedPlace?.ID;

                DataListPlace.Clear();
                foreach (var pl in _dbservicewms.GetPlaceIDs(-1, -1))
                {
                    DataListPlace.Add(new PlaceIDViewModel
                    {
                        ID = pl.ID
                    });
                }
                foreach (var l in DataListPlace)
                    l.Initialize(_warehouse);
                if (placeid != null)
                    SelectedPlace = DataListPlace.First(p => p.ID == placeid);
            }
            catch (Exception e)
            {
                _warehouse.AddEvent(Database.Event.EnumSeverity.Error, Database.Event.EnumType.Exception,
                                    string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
            }
        }

        private void ExecuteRefreshTU()
        {
            try
            {
                DataListTU.Clear();
                foreach (var tu in _dbservicewms.GetPlaces(SelectedPlace?.ID))
                {
                    DataListTU.Add(new PlaceViewModel
                    {
                        TUID = tu.TU_ID,
                        PlaceID = tu.PlaceID,                        
                        Time = tu.Time
                    });
                }
                DataListTU.OrderBy(p => p.Time);
                foreach (var l in DataListTU)
                    l.Initialize(_warehouse);
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
                if (vm is ReleaseOrdersViewModel)
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
