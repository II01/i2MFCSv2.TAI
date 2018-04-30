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
        private string _SKUBatch1;
        private string _SKUBatch2;
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
        public string SKUBatch1
        {
            get { return _SKUBatch1; }
            set
            {
                if (_SKUBatch1 != value)
                {
                    _SKUBatch1 = value;
                    RaisePropertyChanged("SKUBatch1");
                }
            }
        }
        public string SKUBatch2
        {
            get { return _SKUBatch2; }
            set
            {
                if (_SKUBatch2 != value)
                {
                    _SKUBatch2 = value;
                    RaisePropertyChanged("SKUBatch2");
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
