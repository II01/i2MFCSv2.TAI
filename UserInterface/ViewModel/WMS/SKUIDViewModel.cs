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
    public sealed class SKUIDViewModel: ViewModelBase, IDataErrorInfo
    {
        #region members
        private SKU_ID _skuid;
        private bool _allPropertiesValid = false;
        private DBServiceWMS _dbservicewms;
        private BasicWarehouse _warehouse;
        private bool _allowChangeIndex;
        private bool _validationEnabled;
        #endregion

        #region properties
        PropertyValidator Validator { get; set; }
        public SKU_ID SKUID
        {
            get { return _skuid; }
            set
            {
                if (_skuid != value)
                {
                    _skuid = value;
                    RaisePropertyChanged("SKUID");
                }
            }
        }
        public string ID
        {
            get { return _skuid.ID;  }
            set
            {
                if( _skuid.ID != value )
                {
                    _skuid.ID = value;
                    RaisePropertyChanged("ID");
                }
            }
        }

        public string Description
        {
            get { return _skuid.Description; }
            set
            {
                if( _skuid.Description != value)
                {
                    _skuid.Description = value;
                    RaisePropertyChanged("Description");
                }
            }
        }

        public double DefaultQty
        {
            get { return _skuid.DefaultQty; }
            set
            {
                if (_skuid.DefaultQty != value)
                {
                    _skuid.DefaultQty = value;
                    RaisePropertyChanged("DefaultQty");
                }
            }
        }

        public string Unit
        {
            get { return _skuid.Unit; }
            set
            {
                if (_skuid.Unit != value)
                {
                    _skuid.Unit = value;
                    RaisePropertyChanged("Unit");
                }
            }
        }
        public double Weight
        {
            get { return _skuid.Weight; }
            set
            {
                if (_skuid.Weight != value)
                {
                    _skuid.Weight = value;
                    RaisePropertyChanged("Weight");
                }
            }
        }
        public int FrequencyClass
        {
            get { return _skuid.FrequencyClass; }
            set
            {
                if (_skuid.FrequencyClass != value)
                {
                    _skuid.FrequencyClass = value;
                    RaisePropertyChanged("FrequencyClass");
                }
            }
        }

        public int Length
        {
            get { return _skuid.Length; }
            set
            {
                if (_skuid.Length != value)
                {
                    _skuid.Length = value;
                    RaisePropertyChanged("Length");
                }
            }
        }

        public int Width
        {
            get { return _skuid.Width; }
            set
            {
                if (_skuid.Width != value)
                {
                    _skuid.Width = value;
                    RaisePropertyChanged("Width");
                }
            }
        }
        public int Height
        {
            get { return _skuid.Height; }
            set
            {
                if (_skuid.Height != value)
                {
                    _skuid.Height = value;
                    RaisePropertyChanged("Height");
                }
            }
        }
        public bool AllowChangeIndex
        {
            get { return _allowChangeIndex; }
            set
            {
                if (_allowChangeIndex != value)
                {
                    _allowChangeIndex = value;
                    RaisePropertyChanged("AllowChangeIndex");
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
        public SKUIDViewModel()
        {
            _skuid = new SKU_ID();
            Validator = new PropertyValidator();
            ValidationEnabled = false;
            AllowChangeIndex = false;
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
            get { return (SKUID as IDataErrorInfo).Error; }
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
                                if(ID == null || ID.Trim().Length == 0)
                                    validationResult = ResourceReader.GetString("ERR_INDEX_FORMAT");
                                else if (AllowChangeIndex && _dbservicewms.FindSKUID(ID) != null)
                                    validationResult = ResourceReader.GetString("ERR_INDEX_EXISTS");
                                break;
                            case "DefaultQty":
                                if (DefaultQty <= 0)
                                    validationResult = ResourceReader.GetString("ERR_RANGE") + " > 0";
                                break;
                            case "FrequencyClass":
                                if (FrequencyClass < 0)
                                    validationResult = ResourceReader.GetString("ERR_RANGE") + " >= 0";
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
