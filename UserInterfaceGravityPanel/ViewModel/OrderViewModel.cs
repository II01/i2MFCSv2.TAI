using GalaSoft.MvvmLight;

namespace UserInterfaceGravityPanel.ViewModel
{
    public sealed class OrderViewModel: ViewModelBase
    {
        #region members
        private string _ERPID;
        private string _orderID;
        private string _subOrderID;
        private string _subOrderName;
        private string _portionSubOrder;
        private string _portionCommand;
        #endregion

        #region properties
        public string ERPID
        {
            get { return _ERPID; }
            set
            {
                if (_ERPID != value)
                {
                    _ERPID = value;
                    RaisePropertyChanged("ERPID");
                }
            }
        }
        public string OrderID
        {
            get { return _orderID;  }
            set
            {
                if(_orderID != value )
                {
                    _orderID = value;
                    RaisePropertyChanged("OrderID");
                }
            }
        }
        public string SubOrderID
        {
            get { return _subOrderID; }
            set
            {
                if (_subOrderID != value)
                {
                    _subOrderID = value;
                    RaisePropertyChanged("SubOrderID");
                }
            }
        }
        public string SubOrderName
        {
            get { return _subOrderName; }
            set
            {
                if (_subOrderName != value)
                {
                    _subOrderName = value;
                    RaisePropertyChanged("SubOrderIName");
                }
            }
        }
        public string PortionSubOrder
        {
            get { return _portionSubOrder; }
            set
            {
                if (_portionSubOrder != value)
                {
                    _portionSubOrder = value;
                    RaisePropertyChanged("PortionSubOrder");
                }
            }
        }

        public string PortionCommand
        {
            get { return _portionCommand; }
            set
            {
                if (_portionCommand != value)
                {
                    _portionCommand = value;
                    RaisePropertyChanged("PortionCommand");
                }
            }
        }

        #endregion

        #region initialization
        public OrderViewModel()
        {
        }
        #endregion
    }
}
