using GalaSoft.MvvmLight;
using System.Windows.Media;

namespace UserInterfaceGravityPanel.ViewModel
{
    public sealed class TUViewModel: ViewModelBase
    {
        #region members
        private string _TUID;
        private int _subOrderID;
        private string _subOrderName;
        private SolidColorBrush _subOrderBrush;
        private string _SKUID;
        private string _SKUBatch;
        private double _SKUQty;
        #endregion

        #region properties
        public string TUID
        {
            get { return _TUID; }
            set
            {
                if (_TUID != value)
                {
                    _TUID = value;
                    RaisePropertyChanged("TUID");
                }
            }
        }
        public int SubOrderID
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
        public SolidColorBrush SubOrderBrush
        {
            get { return _subOrderBrush; }
            set
            {
                if (_subOrderBrush != value)
                {
                    _subOrderBrush = value;
                    RaisePropertyChanged("SubOrderBrush");
                }
            }
        }
        public string SKUID
        {
            get { return _SKUID; }
            set
            {
                if (_SKUID != value)
                {
                    _SKUID = value;
                    RaisePropertyChanged("SKUID");
                }
            }
        }
        public string SKUBatch
        {
            get { return _SKUBatch; }
            set
            {
                if (_SKUBatch != value)
                {
                    _SKUBatch = value;
                    RaisePropertyChanged("SKUBatch");
                }
            }
        }
        public double SKUQty
        {
            get { return _SKUQty; }
            set
            {
                if (_SKUQty != value)
                {
                    _SKUQty = value;
                    RaisePropertyChanged("SKUQty");
                }
            }
        }

        #endregion

        #region initialization
        public TUViewModel()
        {
        }
        #endregion
    }
}
