using GalaSoft.MvvmLight;
using System.Windows.Media;

namespace UserInterfaceGravityPanel.ViewModel
{
    public sealed class LaneViewModel: ViewModelBase
    {
        #region members
        private int _laneID;
        private int _numTU;
        private TUViewModel _firstTU;
        #endregion

        #region properties
        public int LaneID
        {
            get { return _laneID; }
            set
            {
                if (_laneID != value)
                {
                    _laneID = value;
                    RaisePropertyChanged("LaneID");
                }
            }
        }
        public int NumTU
        {
            get { return _numTU;  }
            set
            {
                if(_numTU != value )
                {
                    _numTU = value;
                    RaisePropertyChanged("NumTU");
                }
            }
        }
        public TUViewModel FirstTU
        {
            get { return _firstTU; }
            set
            {
                if (_firstTU != value)
                {
                    _firstTU = value;
                    RaisePropertyChanged("FirstTU");
                }
            }
        }
        #endregion

        #region initialization
        public LaneViewModel()
        {
        }
        #endregion
    }
}
