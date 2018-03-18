using Database;
using GalaSoft.MvvmLight;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UserInterface.DataServiceWMS;
using UserInterface.Services;

namespace UserInterface.ViewModel
{
    public class PlaceDiffViewModel : ViewModelBase
    {
        #region members
        private PlaceDiff _placeDiff;
        #endregion

        #region properties
        public int TUID
        {
            get { return _placeDiff.TUID; }
            set
            {
                if (_placeDiff.TUID != value)
                {
                    _placeDiff.TUID = value;
                    RaisePropertyChanged("TUID");
                }
            }
        }

        public string PlaceWMS
        {
            get { return _placeDiff.PlaceWMS; }
            set
            {
                if (_placeDiff.PlaceWMS != value)
                {
                    _placeDiff.PlaceWMS = value;
                    RaisePropertyChanged("PlaceWMS");
                }
            }
        }
        public string PlaceMFCS
        {
            get { return _placeDiff.PlaceMFCS; }
            set
            {
                if (_placeDiff.PlaceMFCS != value)
                {
                    _placeDiff.PlaceMFCS = value;
                    RaisePropertyChanged("PlaceMFCS");
                }
            }
        }

        #endregion

        #region initialization
        public PlaceDiffViewModel()
        {
            _placeDiff = new PlaceDiff();
        }
        #endregion
    }
}
