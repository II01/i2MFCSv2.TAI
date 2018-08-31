using System;
using GalaSoft.MvvmLight;
using UserInterface.Services;
using Database;
using System.ComponentModel;
using Warehouse.Model;
using System.Diagnostics;

namespace UserInterface.ViewModel
{
    public sealed class MaterialViewModel: ViewModelBase, IDataErrorInfo
    {
        #region members
        private BasicWarehouse _warehouse;

        private MaterialID _material;
        private string _idString;
        private string _location;

        private bool _enabledMaterial;
        private bool _enabledLocation;
        private bool _enabledProperty;
        private bool _allPropertiesValid;
        #endregion

        #region properties
        public PropertyValidator Validator { get; set; }
        public bool EnabledMaterial
        {
            get { return _enabledMaterial; }
            set
            {
                if (_enabledMaterial != value)
                {
                    _enabledMaterial = value;
                    RaisePropertyChanged("EnabledMaterial");
                }
            }
        }

        public bool EnabledLocation
        {
            get { return _enabledLocation; }
            set
            {
                if (_enabledLocation != value)
                {
                    _enabledLocation = value;
                    RaisePropertyChanged("EnabledLocation");
                }
            }
        }
        public bool EnabledProperty
        {
            get { return _enabledProperty; }
            set
            {
                if (_enabledProperty != value)
                {
                    _enabledProperty = value;
                    RaisePropertyChanged("EnabledProperty");
                }
            }
        }
        public string Location
        {
            get { return _location; }
            set
            {
                if (_location != value)
                {
                    _location = value;
                    RaisePropertyChanged("Location");
                }
            }
        }

        public int ID
        {
            get { return _material.ID;  }
            set
            {
                if(_material.ID != value)
                {
                    _material.ID = value;
//                    IDString = _material.ID > 0 ? string.Format("P{0:d9}", _material.ID) : "";
                    IDString = _material.ID > 0 ? string.Format("{0:d9}", _material.ID) : "";
                    RaisePropertyChanged("ID");
                }
            }
        }

        public string IDString
        {
            get { return _idString; }
            set
            {
//                if (_idString != value && value.Length <= 10)
                if (_idString != value && value.Length <= 9)
                {
                    _idString = value;
//                    if (_idString.Length > 0 && _idString[0] != 'P')
//                        _idString = 'P' + _idString;
//                    if (_idString.Length == 10 && Int32.TryParse(_idString.Substring(1), out int m))
                    if (Int32.TryParse(_idString, out int m))
                        _material.ID = m;
                    else
                        _material.ID = 0;
                    RaisePropertyChanged("IDString");
                }
            }
        }

        public int Size
        {
            get { return _material.Size; }
            set
            {
                if( _material.Size != value)
                {
                    _material.Size= value;
                    RaisePropertyChanged("Size");
                }
            }
        }

        public int Weight
        {
            get { return _material.Weight; }
            set
            {
                if (_material.Weight!= value)
                {
                    _material.Weight = value;
                    RaisePropertyChanged("Weight");
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
        public MaterialViewModel()
        {
            _material = new MaterialID();
            Validator = new PropertyValidator();
        }
        public void Initialize(BasicWarehouse warehouse)
        {
            _warehouse = warehouse;
            try
            {
            }
            catch (Exception e)
            {
                _warehouse.AddEvent(Database.Event.EnumSeverity.Error, Database.Event.EnumType.Exception, e.Message);
                throw new UIException(string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
            }
        }
        #endregion

        #region validation
        public string Error
        {
            get { return (_material as IDataErrorInfo).Error; }
        }

        public string this[string propertyName]
        {
            get
            {
                try
                {
                    string validationResult = String.Empty;

                    switch (propertyName)
                    {
                        case "IDString":
                            if (EnabledMaterial && ID <= 0)
                                validationResult = ResourceReader.GetString("ERR_MATERIALNOTVALID");
                            else if (EnabledMaterial && _warehouse.DBService.FindMaterial(ID) != null)
                                validationResult = ResourceReader.GetString("ERR_MATERIALEXSITS");
                            break;
                        case "Location":
                            if (EnabledLocation && _warehouse.DBService.FindPlaceID(Location) == null)
                                validationResult = ResourceReader.GetString("ERR_NOLOCATION");
                            else if (EnabledLocation && _warehouse.DBService.FindPlace(Location) != null)
                                validationResult = ResourceReader.GetString("ERR_LOCATIONFULL");
                            break;
                        case "Size":
                            if (EnabledProperty && !(Size >= 0 && Size <= 2))
                                validationResult = ResourceReader.GetString("ERR_SIZE");
                            break;
                        case "Weight":
                            if (EnabledProperty && !(Weight >= 10000 && Weight < 30000))
                                validationResult = ResourceReader.GetString("ERR_WEIGHT");
                            break;
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