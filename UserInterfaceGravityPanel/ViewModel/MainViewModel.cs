using System;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.CommandWpf;
using System.Windows.Threading;
using System.Windows;
using System.Diagnostics;
using System.Collections.ObjectModel;
using System.Windows.Media;
using System.Resources;
using System.Threading;
using UserInterfaceGravityPanel.DataServiceWMS;
using System.Threading.Tasks;

namespace UserInterfaceGravityPanel.ViewModel
{
    public sealed class MainViewModel : ViewModelBase
    {
        #region members
        private string _currentTime;
        private DBServiceWMS _dbservicewms;
        private int _ramp;
        private string _rampStr;
        private DispatcherTimer _timer;
        private OrderViewModel _orderInfo;
        private ObservableCollection<LaneViewModel> _lane;
        private string[] _orderStatus = new string[6];
        private ObservableCollection<SolidColorBrush> _subOrderColor;
        private Visibility _errorVisibility;
        private string _errorMessage;
        private string _active;
        private string _done;
        private string _all;
        private string _refreshfailed;
        private string _initfailed;
        private string _details;
        private int _refreshTime;
        #endregion

        #region properties
        public RelayCommand OnLoaded { get; private set; }
        public RelayCommand OnClose { get; private set; }

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
        public string RampStr
        {
            get
            {
                return this._rampStr;
            }
            set
            {
                if (_rampStr != value)
                {
                    _rampStr = value;
                    RaisePropertyChanged("RampStr");
                }
            }
        }
        public OrderViewModel OrderInfo
        {
            get
            {
                return this._orderInfo;
            }
            set
            {
                if (_orderInfo != value)
                {
                    _orderInfo = value;
                    RaisePropertyChanged("OrderInfo");
                }
            }
        }
        public ObservableCollection<LaneViewModel> Lane
        {
            get
            {
                return _lane;
            }
            set
            {
                if (_lane != value)
                {
                    _lane = value;
                    RaisePropertyChanged("Lane");
                }
            }
        }
        public string[] OrderStatus
        {
            get
            {
                return _orderStatus;
            }
            set
            {
                if (_orderStatus != value)
                {
                    _orderStatus = value;
                    RaisePropertyChanged("OrderStatus");
                }
            }
        }
        public ObservableCollection<SolidColorBrush> SubOrderColor
        {
            get
            {
                return _subOrderColor;
            }
            set
            {
                if (_subOrderColor != value)
                {
                    _subOrderColor = value;
                    RaisePropertyChanged("SubOrderColor");
                }
            }
        }
        public Visibility ErrorVisibility
        {
            get
            {
                return _errorVisibility;
            }
            set
            {
                if (_errorVisibility != value)
                {
                    _errorVisibility = value;
                    RaisePropertyChanged("ErrorVisibility");
                }
            }
        }
        public string ErrorMessage
        {
            get
            {
                return _errorMessage;
            }
            set
            {
                if (_errorMessage != value)
                {
                    _errorMessage = value;
                    RaisePropertyChanged("ErrorMessage");
                }
            }
        }
        #endregion

        #region initialization
        public void ExecuteOnLoaded()
        {
            Initialize();
        }

        public void ExecuteOnClose()
        {
        }

        public void Initialize()
        {
            try
            {
                // app config
                _ramp = 1;
                try
                {
                    _ramp = Math.Max(1, Math.Min(5, int.Parse(System.Configuration.ConfigurationManager.AppSettings["TruckRamp"])));
                    RampStr = _ramp.ToString();
                }
                catch { }
                _refreshTime = 3;
                try
                {
                    _refreshTime = Math.Max(1, Math.Min(10, int.Parse(System.Configuration.ConfigurationManager.AppSettings["RefreshTime"])));
                }
                catch { }

                // resources
                BrushConverter conv = new BrushConverter();
                ResourceManager rm = Properties.Resources.ResourceManager;
                ResourceSet rs = rm.GetResourceSet(Thread.CurrentThread.CurrentUICulture, true, true);
                // Customer colors
                SubOrderColor = new ObservableCollection<SolidColorBrush>();
                for (int i = 0; i < 8; i++)
                {
                    string colorstr = rs.GetString($"Color{i + 1}");
                    SubOrderColor.Add(conv.ConvertFromString(colorstr) as SolidColorBrush);
                }
                // Order statuses
                for (int i = 0; i < 6; i++)
                    OrderStatus[i] = rs.GetString($"OrderStatus{i}");
                // order status
                _active = rs.GetString("ShortActive");
                _done = rs.GetString("ShortDone");
                _all = rs.GetString("ShortAll");
                // errors
                _initfailed = rs.GetString("InitFailed");
                _refreshfailed = rs.GetString("RefreshFailed");
                _details = rs.GetString("Details");

                // db
                _dbservicewms = new DBServiceWMS();

                // view models
                OrderInfo = new OrderViewModel();
                Lane = new ObservableCollection<LaneViewModel>();
                for (int i = 0; i < 4; i++)
                    Lane.Add( new LaneViewModel {LaneID=_ramp*0+i+1, NumTU = 0});

                ErrorVisibility = Visibility.Hidden;
                ErrorMessage = "";

                // timer
                _timer = new DispatcherTimer(DispatcherPriority.Render) { Interval = TimeSpan.FromSeconds(1) };
                _timer.Tick += new EventHandler(OnTimer);
                _timer.Start();
            }
            catch (Exception ex)
            {
                ErrorVisibility = Visibility.Visible;
                ErrorMessage = $"Initialization failed.\nDetails:\n{ex.Message}";
            }
        }

        public MainViewModel()
        {
            OnLoaded = new RelayCommand(() => ExecuteOnLoaded());
            OnClose = new RelayCommand(() => ExecuteOnClose());
        }

        private async void OnTimer(object state, EventArgs e)
        {
            try
            {
                CurrentTime = string.Format("{0} {1}", DateTime.Now.ToShortDateString(), DateTime.Now.ToLongTimeString());
                if (DateTime.Now.Second % 5 == _ramp - 1) // each panel starts at different time (if clocks are in sync)
                {
                    await ExecuteRefresh();
                    ErrorVisibility = Visibility.Hidden;
                    ErrorMessage = "";
                }
            }
            catch (Exception ex)
            {
                ErrorVisibility = Visibility.Visible;
                ErrorMessage = $"{_refreshfailed}. {_details}:\n{ex.Message}";
            }
        }

        private async Task ExecuteRefresh()
        {
            try
            {
                var order = _dbservicewms.GetCurrentOrderForRamp(_ramp);
                var suborder = _dbservicewms.GetCurrentSubOrderForRamp(_ramp);
                var orderCount = await _dbservicewms.GetCurrentOrderActivity(order);
                var suborderCount = await _dbservicewms.GetCurrentSubOrderActivity(suborder);

                // orderviewmodel
                OrderInfo.ERPID = "";
                OrderInfo.Operation = "";
                OrderInfo.OrderID = "";
                OrderInfo.StatusOrder = "";
                OrderInfo.PortionSubOrder = "";
                OrderInfo.SubOrderID = "";
                OrderInfo.SubOrderName = "";
                OrderInfo.StatusSubOrder = "";
                OrderInfo.PortionCommand = "";
                OrderInfo.TruckPlate = "";
                OrderInfo.TruckType = "";
                OrderInfo.TruckNumber = "";

                if (order != null)
                {
                    if (order.ERP_ID != null)
                    {
                        OrderInfo.OrderID = order.SubOrderERPID.ToString();
                        string[] split = order.SubOrderName.Split('#');
                        if( split.Length == 5)
                        {
                            OrderInfo.Operation = split[1];
                            OrderInfo.TruckType = split[2];
                            OrderInfo.TruckPlate = split[3];
                            OrderInfo.TruckNumber = split[4];
                        }
                    }
                    else
                        OrderInfo.OrderID = order.OrderID.ToString();
                }
                OrderInfo.RightVisibility = order == null ? Visibility.Hidden : Visibility.Visible;

                if (orderCount != null)
                {
                    OrderInfo.StatusOrder = OrderStatus[(int)orderCount.Status];
                    OrderInfo.PortionSubOrder = $"{_active} {orderCount.Active}/{orderCount.All}   {_done} {orderCount.Done}/{orderCount.All}";
                    OrderInfo.SuborderTotal = orderCount.All.ToString();
                    OrderInfo.SuborderActive = orderCount.Active.ToString();
                    OrderInfo.SuborderDone = orderCount.Done.ToString();
                }
                OrderInfo.SubOrderID = "-";
                OrderInfo.PalletVisibility = Visibility.Hidden;
                if (suborder != null)
                {
                    string[] split = suborder.SubOrderName.Split('#');
                    string customer = "";
                    if (split.Length == 5)
                        customer = split[2];
                    OrderInfo.SubOrderID = suborder.SubOrderID.ToString();
                    OrderInfo.SubOrderName = $"{customer}";
                }
                if (suborderCount != null)
                {
                    OrderInfo.StatusSubOrder = OrderStatus[(int)suborderCount.Status];
                    OrderInfo.PortionCommand = $"{_active} {suborderCount.Active}/{suborderCount.All}   {_done} {suborderCount.Done}/{suborderCount.All}";
                    OrderInfo.CommandTotal = suborderCount.All.ToString();
                    OrderInfo.CommandDone = suborderCount.Done.ToString();
                    OrderInfo.CommandActive = suborderCount.Active.ToString();
                    OrderInfo.PalletVisibility = suborderCount.Active == 0 ? Visibility.Hidden : Visibility.Visible;
                }

                // laneviewmodel
                var lanes = _dbservicewms.GetLastPallets(_ramp);

                for (int i = 0; i < 4; i++)
                {
                    Lane[i].NumTU = 0;
                    Lane[i].FirstTU = null;
                }

                foreach (var l in lanes)
                {
                    if (l != null)
                    {
                        TUViewModel last = new TUViewModel();
                        if (l.FirstTUID != null)
                        {
                            last.TUID = $"{l.FirstTUID:d9}";
                            if (l.SKU != null)
                            {
                                last.SKUID = l.SKU.SKU;
                                last.SKUBatch1 = l.SKU.SKUBatch;
                                last.SKUQty = l.SKU.SKUQty;
                            }
                            if (l.Suborder != null)
                            {
                                last.SubOrderID = l.Suborder.SubOrderID;
                                last.SubOrderBrush = SubOrderColor[(last.SubOrderID - 1 + 8) % 8];
                                string[] s = l.Suborder.SubOrderName.Split('#');
                                last.SubOrderName = s.Length > 0 ? s[0].Trim() : l.Suborder.SubOrderName;
                            }
                        }
                        Lane[l.LaneID - 1].NumTU = l.Count;
                        Lane[l.LaneID - 1].FirstTU = last;
                    }
                }
            }
            catch (Exception  ex)
            {
                throw new Exception(string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, ex.Message));
            }
        }
        #endregion
    }
}
