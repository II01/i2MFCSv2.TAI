using System;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.CommandWpf;
using UserInterface.Services;
using DatabaseWMS;
using System.ComponentModel;
using Warehouse.Model;
using System.Diagnostics;
using UserInterface.DataServiceWMS;
using System.Data.SqlTypes;

namespace UserInterface.ViewModel
{
    public abstract class StationViewModel: ViewModelBase, IDataErrorInfo
    {
        #region members
        public DBServiceWMS DBServiceWMS { get; set; }
        public BasicWarehouse Warehouse { get; set; }
        private string _operationName;
        private bool _setFocus;
        private bool _validationEnabled;
        private bool _allPropertiesValid;
        #endregion

        #region properties
        public PropertyValidator Validator { get; set; }
        public string OperationName
        {
            get { return _operationName; }
            set
            {
                if (_operationName != value)
                {
                    _operationName = value;
                    RaisePropertyChanged("OperationName");
                }
            }
        }
        public bool SetFocus
        {
            get { return _setFocus; }
            set
            {
                if (_setFocus != value)
                {
                    _setFocus = value;
                    RaisePropertyChanged("SetFocus");
                }
            }
        }
        public bool ValidationEnabled
        {
            get { return ValidationEnabled1; }
            set
            {
                if (ValidationEnabled1 != value)
                {
                    ValidationEnabled1 = value;
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
        public StationViewModel()
        {
            Validator = new PropertyValidator();
            ValidationEnabled = false;
        }
        public virtual void Initialize(BasicWarehouse warehouse)
        {
            Warehouse = warehouse;
            DBServiceWMS = new DBServiceWMS(warehouse);
            try
            {
            }
            catch (Exception e)
            {
                Warehouse.AddEvent(Database.Event.EnumSeverity.Error, Database.Event.EnumType.Exception, e.Message);
                throw new Exception(string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
            }
        }
        #endregion

        #region commands

        #endregion

        #region validation
        public string Error
        {
            get { return (this as IDataErrorInfo).Error; }
        }

        public bool ValidationEnabled1 { get => _validationEnabled; set => _validationEnabled = value; }

        public virtual string this[string propertyName]
        {
            get
            {
                try
                {
                    string validationResult = String.Empty;
                    if( ValidationEnabled )
                    {
                    }
                    Validator.AddOrUpdate(propertyName, validationResult == String.Empty);
                    AllPropertiesValid = Validator.IsValid();
                    return validationResult;
                }
                catch (Exception e)
                {
                    Warehouse.AddEvent(Database.Event.EnumSeverity.Error, Database.Event.EnumType.Exception,
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
