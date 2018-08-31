using System;
using GalaSoft.MvvmLight;
using UserInterface.Services;
using DatabaseWMS;
using System.ComponentModel;
using Warehouse.Model;
using System.Diagnostics;
using UserInterface.DataServiceWMS;
using System.Collections.Generic;

namespace UserInterface.ViewModel
{
    public sealed class PackageIDViewModel: ViewModelBase, IDataErrorInfo
    {
        #region members
        private Package_ID _packageid;
        private List<string> _SKUIDs;
        
        private bool _allPropertiesValid = false;
        private DBServiceWMS _dbservicewms;
        private BasicWarehouse _warehouse;
        private bool _validationEnabled;
        private bool _editVisible;
        private bool _addEnabled;
        #endregion

        #region properties
        PropertyValidator Validator { get; set; }
        public Package_ID PackageID
        {
            get { return _packageid; }
            set
            {
                if (_packageid != value)
                {
                    _packageid = value;
                    RaisePropertyChanged("PackageID");
                }
            }
        }
        public List<string> SKUIDs
        {
            get { return _SKUIDs; }
            set
            {
                if (_SKUIDs != value)
                {
                    _SKUIDs = value;
                    RaisePropertyChanged("SKUIDs");
                }
            }
        }
        public string ID
        {
            get { return _packageid.ID;  }
            set
            {
                if( _packageid.ID != value )
                {
                    _packageid.ID = value;
                    RaisePropertyChanged("ID");
                }
            }
        }

        public string SKUID
        {
            get { return _packageid.SKU_ID; }
            set
            {
                if(_packageid.SKU_ID != value)
                {
                    _packageid.SKU_ID = value;
                    RaisePropertyChanged("SKUID");
                }
            }
        }

        public string Batch
        {
            get { return _packageid.Batch; }
            set
            {
                if (_packageid.Batch != value)
                {
                    _packageid.Batch = value;
                    RaisePropertyChanged("Batch");
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
        public bool AddEnabled
        {
            get { return _addEnabled; }
            set
            {
                if (_addEnabled != value)
                {
                    _addEnabled = value;
                    RaisePropertyChanged("AddEnabled");
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
        public PackageIDViewModel()
        {
            _packageid = new Package_ID();
            _SKUIDs = new List<string>();
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
            get { return (PackageID as IDataErrorInfo).Error; }
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
                                if(AddEnabled && _dbservicewms.CountPackageIDs(ID) != 0 || ID.Length == 0)
                                    validationResult = ResourceReader.GetString("ERR_PACKAGEID_EXISTS");
                                break;
                            case "SKUID":
                                if (_dbservicewms.FindSKUID(SKUID) == null || SKUID == null || SKUID.Length == 0)
                                    validationResult = ResourceReader.GetString("ERR_SKUID_NONE");
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
