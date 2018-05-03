using GalaSoft.MvvmLight;
using System.Windows;

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
        private string _statusOrder;
        private string _statusSubOrder;
        private string _operation;
        private string _truckNumber;
        private string _truckType;
        private string _truckPlate;
        private string _commandTotal;
        private string _commandActive;
        private string _commandDone;
        private string _suborderTotal;
        private string _suborderActive;
        private string _suborderDone;
        private Visibility _palletVisibility;
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
                    RaisePropertyChanged("SubOrderName");
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

        public string StatusOrder
        {
            get { return _statusOrder; }
            set
            {
                if (_statusOrder != value)
                {
                    _statusOrder = value;
                    RaisePropertyChanged("StatusOrder");
                }
            }
        }

        public string StatusSubOrder
        {
            get { return _statusSubOrder; }
            set
            {
                if (_statusSubOrder != value)
                {
                    _statusSubOrder = value;
                    RaisePropertyChanged("StatusSubOrder");
                }
            }
        }
        public string Operation
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
        public string TruckNumber
        {
            get { return _truckNumber; }
            set
            {
                if (_truckNumber != value)
                {
                    _truckNumber = value;
                    RaisePropertyChanged("TruckNumber");
                }
            }
        }
        public string TruckType
        {
            get { return _truckType; }
            set
            {
                if (_truckType != value)
                {
                    _truckType = value;
                    RaisePropertyChanged("TruckType");
                }
            }
        }
        public string TruckPlate
        {
            get { return _truckPlate; }
            set
            {
                if (_truckPlate != value)
                {
                    _truckPlate = value;
                    RaisePropertyChanged("TruckPlate");
                }
            }
        }
        public string SuborderTotal
        {
            get { return _suborderTotal; }
            set
            {
                if (_suborderTotal != value)
                {
                    _suborderTotal = value;
                    RaisePropertyChanged("SuborderTotal");
                }
            }
        }
        public string SuborderActive
        {
            get { return _suborderActive; }
            set
            {
                if (_suborderActive != value)
                {
                    _suborderActive = value;
                    RaisePropertyChanged("SuborderActive");
                }
            }
        }
        public string SuborderDone
        {
            get { return _suborderDone; }
            set
            {
                if (_suborderDone != value)
                {
                    _suborderDone = value;
                    RaisePropertyChanged("SuborderDone");
                }
            }
        }
        public string CommandTotal
        {
            get { return _commandTotal; }
            set
            {
                if (_commandTotal != value)
                {
                    _commandTotal = value;
                    RaisePropertyChanged("CommandTotal");
                }
            }
        }
        public string CommandActive
        {
            get { return _commandActive; }
            set
            {
                if (_commandActive != value)
                {
                    _commandActive = value;
                    RaisePropertyChanged("CommandActive");
                }
            }
        }
        public string CommandDone
        {
            get { return _commandDone; }
            set
            {
                if (_commandDone != value)
                {
                    _commandDone = value;
                    RaisePropertyChanged("CommandDone");
                }
            }
        }
        public Visibility PalletVisibility
        {
            get { return _palletVisibility; }
            set
            {
                if (_palletVisibility != value)
                {
                    _palletVisibility = value;
                    RaisePropertyChanged("PalletVisibility");
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
