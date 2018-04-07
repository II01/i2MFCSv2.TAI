using GalaSoft.MvvmLight;
using System.Windows.Media;

namespace UserInterfaceGravityPanel.ViewModel
{
    public sealed class LaneViewModel: ViewModelBase
    {
        #region members
        private int _laneID;
        private int _numTU;
        private TUViewModel _lastTU;
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
        public TUViewModel LastTU
        {
            get { return _lastTU; }
            set
            {
                if (_lastTU != value)
                {
                    _lastTU = value;
                    RaisePropertyChanged("LastTU");
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
