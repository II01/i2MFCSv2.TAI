using System;
using GalaSoft.MvvmLight;
using UserInterface.Services;
using DatabaseWMS;
using System.ComponentModel;
using Warehouse.Model;
using System.Diagnostics;
using UserInterface.DataServiceWMS;
using System.Data.SqlTypes;

namespace UserInterface.ViewModel
{
    public sealed class PlaceViewModel : ViewModelBase
    {
        #region members
        private Places _data;
        private DBServiceWMS _dbservicewms;
        private BasicWarehouse _warehouse;
        #endregion

        #region properties
        public Places Data
        {
            get { return _data; }
            set
            {
                if (_data != value)
                {
                    _data = value;
                    RaisePropertyChanged("Data");
                }
            }
        }
        public int TUID
        {
            get { return _data.TU_ID; }
            set
            {
                if (_data.TU_ID != value)
                {
                    _data.TU_ID = value;
                    RaisePropertyChanged("TUID");
                }
            }
        }

        public string PlaceID
        {
            get { return _data.PlaceID; }
            set
            {
                if (_data.PlaceID != value)
                {
                    _data.PlaceID = value;
                    RaisePropertyChanged("PlaceID");
                }
            }
        }
        public DateTime Time
        {
            get { return _data.Time; }
            set
            {
                if (_data.Time != value)
                {
                    _data.Time = value;
                    RaisePropertyChanged("Time");
                }
            }
        }
        #endregion

        #region initialization
        public PlaceViewModel()
        {
            _data = new Places();
            _dbservicewms = new DBServiceWMS(null);
        }
        public void Initialize(BasicWarehouse warehouse)
        {
            _warehouse = warehouse;
            try
            {
                _dbservicewms = new DBServiceWMS(warehouse);
            }
            catch (Exception e)
            {
                _warehouse.AddEvent(Database.Event.EnumSeverity.Error, Database.Event.EnumType.Exception, e.Message);
                throw new Exception(string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
            }
        }
        #endregion
    }
}

