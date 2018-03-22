using System;
using GalaSoft.MvvmLight;
using UserInterface.Services;
using DatabaseWMS;
using System.ComponentModel;
using Warehouse.Model;
using System.Diagnostics;
using UserInterface.DataServiceWMS;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using GalaSoft.MvvmLight.Messaging;
using UserInterface.Messages;

namespace UserInterface.ViewModel
{
    public sealed class PlaceTUIDViewModel: ViewModelBase, IDataErrorInfo
    {
        #region members
        private PlaceTUID _data;
        private bool _allPropertiesValid = false;
        private DBServiceWMS _dbservicewms;
        private BasicWarehouse _warehouse;
        private bool _validationEnabled;
        private bool _allowTUIDChange;
        private bool _allowPlaceChange;
        private bool _allowFieldChange;
        private ObservableCollection<TUSKUIDViewModel> _detailList;
        private bool _subTableValidation;
        #endregion

        #region properties
        PropertyValidator Validator { get; set; }
        public PlaceTUID PlaceTUID
        {
            get { return _data; }
            set
            {
                if (_data != value)
                {
                    _data = value;
                    RaisePropertyChanged("PlaceTUID");
                }
            }
        }
        public int TUID
        {
            get { return _data.TUID;  }
            set
            {
                if( _data.TUID != value )
                {
                    _data.TUID = value;
                    RaisePropertyChanged("TUID");
                }
            }
        }

        public string PlaceID
        {
            get { return _data.PlaceID; }
            set
            {
                if( _data.PlaceID!= value)
                {
                    _data.PlaceID = value;
                    RaisePropertyChanged("PlaceID");
                }
            }
        }

        public int DimensionClass
        {
            get { return _data.DimensionClass; }
            set
            {
                if (_data.DimensionClass != value)
                {
                    _data.DimensionClass = value;
                    RaisePropertyChanged("DimensionClass");
                }
            }
        }

        public int Blocked
        {
            get { return _data.Blocked; }
            set
            {
                if (_data.Blocked != value)
                {
                    _data.Blocked = value;
                    RaisePropertyChanged("Blocked");
                }
            }
        }

        public ObservableCollection<TUSKUIDViewModel> DetailList
        {
            get { return _detailList; }
            set
            {
                if (_detailList != value)
                {
                    _detailList = value;
                    RaisePropertyChanged("DetailList");
                }
            }
        }

        public bool SubTableValidation
        {
            get { return _subTableValidation; }
            set
            {
                if (_subTableValidation != value)
                {
                    _subTableValidation = value;
                    AllPropertiesValid = Validator.IsValid() &&_subTableValidation;
                    RaisePropertyChanged("SubTableValidation");
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
        public bool AllowTUIDChange
        {
            get { return _allowTUIDChange; }
            set
            {
                if (_allowTUIDChange != value)
                {
                    _allowTUIDChange = value;
                    RaisePropertyChanged("AllowTUIDChange");
                }
            }
        }
        public bool AllowPlaceChange
        {
            get { return _allowPlaceChange; }
            set
            {
                if (_allowPlaceChange != value)
                {
                    _allowPlaceChange = value;
                    RaisePropertyChanged("AllowPlaceChange");
                }
            }
        }

        public bool AllowFieldChange
        {
            get { return _allowFieldChange; }
            set
            {
                if (_allowFieldChange != value)
                {
                    _allowFieldChange = value;
                    RaisePropertyChanged("AllowFieldChange");
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
        public PlaceTUIDViewModel()
        {
            _data = new PlaceTUID();
            Validator = new PropertyValidator();
            ValidationEnabled = false;
            _allowTUIDChange = false;
            _allowPlaceChange = false;
            _allowFieldChange = false;
            _subTableValidation = false;
            Messenger.Default.Register<MessageValidationInfo>(this, msg => { SubTableValidation = msg.AllPropertiesValid; });
        }
        public void Initialize(BasicWarehouse warehouse)
        {
            _warehouse = warehouse;
            try
            {
                _dbservicewms = new DBServiceWMS(warehouse);
                DetailList = new ObservableCollection<TUSKUIDViewModel>();
            }
            catch (Exception e)
            {
                _warehouse.AddEvent(Database.Event.EnumSeverity.Error, Database.Event.EnumType.Exception, e.Message);
                throw new Exception(string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
            }
        }
        public void PopulateList()
        {
            try
            {
                DetailList.Clear();
                foreach (var p in _dbservicewms.GetTUSKUIDs(TUID))
                    DetailList.Add(new TUSKUIDViewModel
                    {
                        SKUID = p.SKUID,
                        Qty = p.Qty,
                        Batch = p.Batch,
                        ProdDate = p.ProdDate,
                        ExpDate = p.ExpDate,
                        Description = p.Description
                    });
                foreach (var l in DetailList)
                    l.Initialize(_warehouse);
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
            get { return (PlaceTUID as IDataErrorInfo).Error; }
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
                            case "TUID":
                                if (TUID == 0 || _dbservicewms.FindPlaceByTUID(TUID) != null)
                                    validationResult = ResourceReader.GetString("ERR_TUID");
                                break;
                            case "PlaceID":
                                if (_dbservicewms.FindPlaceID(PlaceID) == null)
                                    validationResult = ResourceReader.GetString("ERR_PLACE");
                                break;
                        }
                    }
                    Validator.AddOrUpdate(propertyName, validationResult == String.Empty);
                    AllPropertiesValid = Validator.IsValid() && SubTableValidation;
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
