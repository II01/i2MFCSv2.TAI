using System;
using GalaSoft.MvvmLight;
using UserInterface.Services;
using DatabaseWMS;
using System.ComponentModel;
using Warehouse.Model;
using System.Diagnostics;
using UserInterface.DataServiceWMS;

namespace UserInterface.ViewModel
{
    public sealed class PlaceIDViewModel: ViewModelBase, IDataErrorInfo
    {
        #region members
        private PlaceIDs _placeid;
        private bool _allPropertiesValid = false;
        private DBServiceWMS _dbservicewms;
        private BasicWarehouse _warehouse;
        private bool _validationEnabled;
        private bool _editVisible;
        #endregion

        #region properties
        PropertyValidator Validator { get; set; }
        public PlaceIDs PlaceID
        {
            get { return _placeid; }
            set
            {
                if (_placeid != value)
                {
                    _placeid = value;
                    RaisePropertyChanged("PlaceID");
                }
            }
        }
        public string ID
        {
            get { return _placeid.ID;  }
            set
            {
                if( _placeid.ID != value )
                {
                    _placeid.ID = value;
                    RaisePropertyChanged("ID");
                }
            }
        }

        public double PositionTravel
        {
            get { return _placeid.PositionTravel; }
            set
            {
                if( _placeid.PositionTravel != value)
                {
                    _placeid.PositionTravel= value;
                    RaisePropertyChanged("PositionTravel");
                }
            }
        }

        public double PositionHoist
        {
            get { return _placeid.PositionHoist; }
            set
            {
                if (_placeid.PositionHoist != value)
                {
                    _placeid.PositionHoist = value;
                    RaisePropertyChanged("PositionHoist");
                }
            }
        }

        public int DimensionClass
        {
            get { return _placeid.DimensionClass; }
            set
            {
                if (_placeid.DimensionClass != value)
                {
                    _placeid.DimensionClass = value;
                    RaisePropertyChanged("DimensionClass");
                }
            }
        }

        public int FrequencyClass
        {
            get { return _placeid.FrequencyClass; }
            set
            {
                if (_placeid.FrequencyClass != value)
                {
                    _placeid.FrequencyClass = value;
                    RaisePropertyChanged("FrequencyClass");
                }
            }
        }
        public int Status
        {
            get { return _placeid.Status; }
            set
            {
                if (_placeid.Status!= value)
                {
                    _placeid.Status = value;
                    RaisePropertyChanged("Status");
                }
            }
        }

        public bool EditVisible
        {
            get { return _editVisible; }
            set
            {
                if (_editVisible != value)
                {
                    _editVisible = value;
                    RaisePropertyChanged("EditVisible");
                }
            }
        }

        public bool ValidationEnabled
        {
            get { return _validationEnabled; }
            set
            {
                if (_validationEnabled != value)
                {
                    _validationEnabled = value;
                    RaisePropertyChanged("ValidationEnabled");
                }
            }
        }

        public bool AllPropertiesValid
        {
            get { return _allPropertiesValid; }
            set
            {
                if (_allPropertiesValid != value)
                {
                    _allPropertiesValid = value;
                    RaisePropertyChanged("AllPropertiesValid");
                }
            }
        }
        #endregion

        #region initialization
        public PlaceIDViewModel()
        {
            _placeid = new PlaceIDs();
            Validator = new PropertyValidator();
            ValidationEnabled = false;
        }
        public void Initialize(BasicWarehouse warehouse)
        {
            _warehouse = warehouse;
            _dbservicewms = new DBServiceWMS(warehouse);
            try
            {
                EditVisible = true;
            }
            catch (Exception e)
            {
                _warehouse.AddEvent(Database.Event.EnumSeverity.Error, Database.Event.EnumType.Exception, e.Message);
                throw new Exception(string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
            }
        }
        #endregion

        #region validation
        public string Error
        {
            get { return (PlaceID as IDataErrorInfo).Error; }
        }

        public string this[string propertyName]
        {
            get
            {
                try
                {
                    string validationResult = String.Empty;
                    if( ValidationEnabled )
                    {
                        switch (propertyName)
                        {
                            case "ID":
                                if(_dbservicewms.CountPlaceIDs(ID) == 0)
                                    validationResult = ResourceReader.GetString("ERR_LOC_RANGE");
                                break;
                            case "FrequencyClass":
                                if (FrequencyClass < 0 || (FrequencyClass == 0 && ID.StartsWith("W")))
                                    validationResult = ResourceReader.GetString("ERR_RANGE");
                                break;
                        }
                    }
                    Validator.AddOrUpdate(propertyName, validationResult == String.Empty);
                    AllPropertiesValid = Validator.IsValid();
                    return validationResult;
                }
                catch (Exception e)
                {
                    _warehouse.AddEvent(Database.Event.EnumSeverity.Error, Database.Event.EnumType.Exception,
                                        string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
                    Validator.AddOrUpdate(propertyName, false);
                    AllPropertiesValid = Validator.IsValid();
                    return ResourceReader.GetString("ERR_EXCEPTION");
                }
            }
        }
        #endregion
    }
}
