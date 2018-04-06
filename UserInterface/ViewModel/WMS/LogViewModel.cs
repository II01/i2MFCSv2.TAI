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
    public sealed class LogViewModel: ViewModelBase, IDataErrorInfo
    {
        #region members
        private Logs _data;
        private bool _allPropertiesValid = false;
        private DBServiceWMS _dbservicewms;
        private BasicWarehouse _warehouse;
        private bool _validationEnabled;
        #endregion

        #region properties
        PropertyValidator Validator { get; set; }
        public Logs Data
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
        public int ID
        {
            get { return _data.ID;  }
            set
            {
                if( _data.ID != value )
                {
                    _data.ID = value;
                    RaisePropertyChanged("ID");
                }
            }
        }

        public EnumLogWMS Severity
        {
            get { return (EnumLogWMS)_data.Severity; }
            set
            {
                if( _data.Severity != (int)value)
                {
                    _data.Severity = (int)value;
                    RaisePropertyChanged("Severity");
                }
            }
        }

        public string Message
        {
            get { return _data.Message; }
            set
            {
                if (_data.Message != value)
                {
                    _data.Message = value;
                    RaisePropertyChanged("Message");
                }
            }
        }

        public string Source
        {
            get { return _data.Source; }
            set
            {
                if (_data.Source != value)
                {
                    _data.Source = value;
                    RaisePropertyChanged("Source");
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
        public LogViewModel()
        {
            _data = new Logs();
            Validator = new PropertyValidator();
            ValidationEnabled = false;
        }
        public void Initialize(BasicWarehouse warehouse)
        {
            _warehouse = warehouse;
            _dbservicewms = new DBServiceWMS(warehouse);
            try
            {
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
            get { return (this as IDataErrorInfo).Error; }
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
