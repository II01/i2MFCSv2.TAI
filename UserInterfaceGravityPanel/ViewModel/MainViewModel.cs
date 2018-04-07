using System;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.CommandWpf;
using GalaSoft.MvvmLight.Ioc;
using GalaSoft.MvvmLight.Messaging;
using System.Windows.Threading;
using System.Windows;
using System.Diagnostics;

namespace UserInterfaceGravityPanel.ViewModel
{
    public sealed class MainViewModel : ViewModelBase
    {
        #region members
        private string _currentTime;
        private int _ramp;
        private string _rampStr;
        private DispatcherTimer _timer;
        private OrderViewModel _orderInfo;
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
        #endregion

        #region initialization
        public void ExecuteOnLoaded()
        {
        }

        public void ExecuteOnClose()
        {
        }

        public MainViewModel()
        {
            try
            {
                _ramp = 2;
                RampStr = _ramp.ToString();

                OnLoaded = new RelayCommand(() => ExecuteOnLoaded());
                OnClose = new RelayCommand(() => ExecuteOnClose());

                _timer = new DispatcherTimer(DispatcherPriority.Render){Interval = TimeSpan.FromSeconds(1)};
                _timer.Tick += new EventHandler(OnTimer);
                _timer.Start();

                OrderInfo = new OrderViewModel { ERPID = "1", OrderID = "2", SubOrderID = "3", SubOrderName = "Štiri", PortionSubOrder = "5/6/7", PortionCommand = "8/9/10" };

            }
            catch(Exception e)
            {
                throw new Exception(string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
            }
        }

        private void OnTimer(object state, EventArgs e)
        {
            CurrentTime = string.Format("{0} {1}", DateTime.Now.ToShortDateString(), DateTime.Now.ToLongTimeString());
            if(DateTime.Now.Second % 5 == _ramp)
            {
                OrderInfo.OrderID = DateTime.Now.Second.ToString();
            }
        }
        #endregion
    }
}
