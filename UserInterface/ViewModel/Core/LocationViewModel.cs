using System;
using GalaSoft.MvvmLight;
using UserInterface.Services;
using Database;
using System.ComponentModel;
using Warehouse.Model;
using System.Diagnostics;

namespace UserInterface.ViewModel
{
    public sealed class LocationViewModel: ViewModelBase, IDataErrorInfo
    {
        #region members
        private PlaceID _place;
        private bool _allPropertiesValid = false;
        private BasicWarehouse _warehouse;
        #endregion

        #region properties
        PropertyValidator Validator { get; set; }
        public PlaceID Place
        {
            get { return _place; }
            set
            {
                if (_place != value)
                {
                    _place = value;
                    RaisePropertyChanged("Place");
                }
            }
        }

        public string ID
        {
            get { return _place.ID;  }
            set
            {
                if( _place.ID != value )
                {
                    _place.ID = value;
                    RaisePropertyChanged("ID");
                }
            }
        }

        public bool Blocked
        {
            get { return _place.Blocked; }
            set
            {
                if( _place.Blocked != value)
                {
                    _place.Blocked = value;
                    RaisePropertyChanged("Blocked");
                }
            }
        }

        public bool Reserved
        {
            get { return _place.Reserved; }
            set
            {
                if (_place.Reserved != value)
                {
                    _place.Reserved = value;
                    RaisePropertyChanged("Reserved");
                }
            }
        }
        public int Size
        {
            get { return _place.Size; }
            set
            {
                if (_place.Size != value)
                {
                    _place.Size = value;
                    RaisePropertyChanged("Size");
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
        public LocationViewModel()
        {
            _place = new PlaceID();
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
                throw new Exception(string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
            }
        }
        #endregion

        #region validation
        public string Error
        {
            get { return (Place as IDataErrorInfo).Error; }
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
                        case "Size":
                            if (ID != null && Size != 1 && Size != 999)
                                validationResult = ResourceReader.GetString("ERR_RANGE") + " 1, 999";
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
