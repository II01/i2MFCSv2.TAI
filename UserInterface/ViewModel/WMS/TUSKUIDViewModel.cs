using System;
using GalaSoft.MvvmLight;
using UserInterface.Services;
using DatabaseWMS;
using System.ComponentModel;
using Warehouse.Model;
using System.Diagnostics;
using UserInterface.DataServiceWMS;
using System.Data.SqlTypes;
using GalaSoft.MvvmLight.Messaging;
using UserInterface.Messages;

namespace UserInterface.ViewModel
{
    public sealed class TUSKUIDViewModel: ViewModelBase, IDataErrorInfo
    {
        #region members
        private TUSKUID _data;
        private bool _allPropertiesValid = false;
        private DBServiceWMS _dbservicewms;
        private BasicWarehouse _warehouse;
        private bool _allowChangeIndex;
        private bool _validationEnabled;
        #endregion

        #region properties
        PropertyValidator Validator { get; set; }
        public TUSKUID Data
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
        public string SKUID
        {
            get { return _data.SKUID;  }
            set
            {
                if( _data.SKUID != value )
                {
                    _data.SKUID = value;
                    RaisePropertyChanged("SKUID");
                }
            }
        }

        public double Qty
        {
            get { return _data.Qty; }
            set
            {
                if (_data.Qty != value)
                {
                    _data.Qty = value;
                    RaisePropertyChanged("Qty");
                }
            }
        }

        public string Batch
        {
            get { return _data.Batch; }
            set
            {
                if (_data.Batch != value)
                {
                    _data.Batch = value;
                    RaisePropertyChanged("Batch");
                }
            }
        }
        public DateTime ProdDate
        {
            get { return _data.ProdDate; }
            set
            {
                if (_data.ProdDate != value)
                {
                    _data.ProdDate = value;
                    RaisePropertyChanged("ProdDate");
                }
            }
        }
        public DateTime ExpDate
        {
            get { return _data.ExpDate; }
            set
            {
                if (_data.ExpDate != value)
                {
                    _data.ExpDate = value;
                    RaisePropertyChanged("ExpDate");
                }
            }
        }
        public string Description
        {
            get { return _data.Description; }
            set
            {
                if( _data.Description != value)
                {
                    _data.Description = value;
                    RaisePropertyChanged("Description");
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
        public TUSKUIDViewModel()
        {
            _data = new TUSKUID();
            _dbservicewms = new DBServiceWMS(null);
            Validator = new PropertyValidator();
            ValidationEnabled = true;
            AllowChangeIndex = false;
            ProdDate = DateTime.Now;
            ExpDate = DateTime.Now;
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

        #region validation
        public string Error
        {
            get { return (Data as IDataErrorInfo).Error; }
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
                            case "SKUID":
                                if (_dbservicewms.FindSKUID(SKUID) == null)
                                    validationResult = ResourceReader.GetString("ERR_SKUID");
                                break;
                            case "Qty":
                                if (Qty <= 0 )
                                    validationResult = ResourceReader.GetString("ERR_RANGE");
                                break;
                            case "Batch":
                                if (Batch == null)
                                    validationResult = ResourceReader.GetString("ERR_BATCH");
                                break;
                            case "ProdDate":
                                if (ProdDate < SqlDateTime.MinValue.Value || ProdDate > SqlDateTime.MaxValue.Value)
                                    validationResult = ResourceReader.GetString("ERR_DATE");
                                break;
                            case "ExpDate":
                                if (ExpDate < SqlDateTime.MinValue.Value || ExpDate > SqlDateTime.MaxValue.Value)
                                    validationResult = ResourceReader.GetString("ERR_DATE");
                                break;
                        }
                    }
                    Validator.AddOrUpdate(propertyName, validationResult == String.Empty);
                    AllPropertiesValid = Validator.IsValid();
                    Messenger.Default.Send<MessageValidationInfo>(new MessageValidationInfo() { AllPropertiesValid = AllPropertiesValid });
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
