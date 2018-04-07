using System;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.CommandWpf;
using GalaSoft.MvvmLight.Ioc;
using GalaSoft.MvvmLight.Messaging;
using System.Windows.Threading;
using System.Windows;
using System.Diagnostics;
using System.Collections.ObjectModel;
using System.Windows.Media;
using System.Resources;
using System.Threading;
using UserInterfaceGravityPanel.DataServiceWMS;

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
        private SolidColorBrush[] _subOrderColor = new SolidColorBrush[8];
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
        public SolidColorBrush[] SubOrderColor
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
        #endregion

        #region initialization
        public void ExecuteOnLoaded()
        {
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

                // resources
                BrushConverter conv = new BrushConverter();
                ResourceManager rm = Properties.Resources.ResourceManager;
                ResourceSet rs = rm.GetResourceSet(Thread.CurrentThread.CurrentUICulture, true, true);
                // Customer colors
                for (int i = 0; i < 8; i++)
                {
                    string colorstr = rs.GetString($"Color{i + 1}");
                    SubOrderColor[i] = conv.ConvertFromString(colorstr) as SolidColorBrush;
                }
                // Order statuses
                for (int i = 0; i < 6; i++)
                    OrderStatus[i] = rs.GetString($"OrderStatus{i}");

                // timer
                _timer = new DispatcherTimer(DispatcherPriority.Render) { Interval = TimeSpan.FromSeconds(1) };
                _timer.Tick += new EventHandler(OnTimer);
                _timer.Start();

                OnLoaded = new RelayCommand(() => ExecuteOnLoaded());
                OnClose = new RelayCommand(() => ExecuteOnClose());

                // db
                _dbservicewms = new DBServiceWMS();

                // view models
                OrderInfo = new OrderViewModel();
                Lane = new ObservableCollection<LaneViewModel>();
                for (int i = 0; i < 4; i++)
                    Lane.Add( new LaneViewModel {LaneID=_ramp*10+i+1, NumTU = 0});
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Initialization not successful read! {ex.Message}");
                throw new Exception(string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, ex.Message));
            }
        }

        public MainViewModel()
        {
            Initialize();
        }

        private void OnTimer(object state, EventArgs e)
        {
            CurrentTime = string.Format("{0} {1}", DateTime.Now.ToShortDateString(), DateTime.Now.ToLongTimeString());
            if(DateTime.Now.Second % 5 == _ramp-1) // each panel starts at different time (if clocks are in sync)
                ExecuteRefresh();
        }

        private void ExecuteRefresh()
        {
            var order = _dbservicewms.GetCurrentOrderForRamp(_ramp);
            var suborder = _dbservicewms.GetCurrentSubOrderForRamp(_ramp);
            var orderCount = _dbservicewms.GetCurrentOrderActivity(order);
            var suborderCount = _dbservicewms.GetCurrentSubOrderActivity(suborder);

            // orderviewmodel
            OrderInfo.ERPID = "";
            OrderInfo.OrderID = "";
            OrderInfo.StatusOrder = "";
            OrderInfo.PortionSubOrder = "";
            OrderInfo.SubOrderID = "";
            OrderInfo.SubOrderName = "";
            OrderInfo.StatusSubOrder = "";
            OrderInfo.PortionCommand = "";
            if (order != null)
            {
                OrderInfo.ERPID = order.ERP_ID == null ? "" : order.ERP_ID.ToString();
                OrderInfo.OrderID = order.OrderID.ToString();
            }
            if(orderCount != null)
            {
                OrderInfo.StatusOrder = OrderStatus[(int)orderCount.Status];
                OrderInfo.PortionSubOrder = $"{orderCount.Active} / {orderCount.Done} / {orderCount.All}";
            }
            if (suborder != null)
            {
                OrderInfo.SubOrderID = suborder.SubOrderID.ToString();
                OrderInfo.SubOrderName = suborder.SubOrderName;
            }
            if (suborderCount != null)
            {
                OrderInfo.StatusSubOrder = OrderStatus[(int)suborderCount.Status];
                OrderInfo.PortionCommand = $"{suborderCount.Active} / {suborderCount.Done} / {suborderCount.All}";
            }

            // laneviewmodel
            var lanes = _dbservicewms.GetLastPallets(_ramp);

            for(int i = 0; i<4; i++)
            {
                Lane[i].NumTU = 0;
                Lane[i].LastTU = null;
            }

            foreach (var l in lanes)
            {
                if(l != null)
                {
                    TUViewModel last = new TUViewModel();
                    if (l.LastTUID != null)
                    {
                        last.TUID = $"{l.LastTUID:d9}";
                        if (l.SKU != null)
                        {
                            last.SKUID = l.SKU.SKU;
                            last.SKUBatch = l.SKU.SKUBatch;
                            last.SKUQty = l.SKU.SKUQty;
                        }
                        if (l.Suborder != null)
                        {
                            last.SubOrderID = l.Suborder.SubOrderID;
                            last.SubOrderBrush = SubOrderColor[l.Suborder.SubOrderID % 8];
                            last.SubOrderName = l.Suborder.SubOrderName;
                        }
                    }
                    Lane[l.LaneID-1].NumTU = l.Count;
                    Lane[l.LaneID-1].LastTU = last;
                }
            }
        }
        #endregion
    }
}
