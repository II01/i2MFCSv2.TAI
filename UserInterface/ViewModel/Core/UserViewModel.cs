using System;
using GalaSoft.MvvmLight;
using UserInterface.Services;
using DatabaseWMS;
using System.ComponentModel;
using Warehouse.Model;
using System.Diagnostics;
using UserInterface.DataServiceWMS;
using Database;

namespace UserInterface.ViewModel
{
    public sealed class UserViewModel: ViewModelBase, IDataErrorInfo
    {
        #region members
        private string _userName;
        private string _password1;
        private string _password2;
        private EnumUserAccessLevel _accessLevelWMS;
        private EnumUserAccessLevel _accessLevelMFCS;
        private bool _allPropertiesValid = false;
        private bool _validationEnabled = false;
        private bool _editEnabledUser;
        private BasicWarehouse _warehouse;
        #endregion

        #region properties
        PropertyValidator Validator { get; set; }
        public string UserName
        {
            get { return _userName; }
            set
            {
                if (_userName != value)
                {
                    _userName = value;
                    RaisePropertyChanged("UserName");
                }
            }
        }
        public string Password1
        {
            get { return _password1; }
            set
            {
                if (_password1 != value)
                {
                    _password1 = value;
                    RaisePropertyChanged("Password1");
                    RaisePropertyChanged("Password2");
                }
            }
        }
        public string Password2
        {
            get { return _password2; }
            set
            {
                if (_password2 != value)
                {
                    _password2 = value;
                    RaisePropertyChanged("Password2");
                    RaisePropertyChanged("Password1");
                }
            }
        }
        public EnumUserAccessLevel AccessLevelWMS
        {
            get { return _accessLevelWMS;  }
            set
            {
                if(_accessLevelWMS != value )
                {
                    _accessLevelWMS = value;
                    RaisePropertyChanged("AccessLevelWMS");
                }
            }
        }
        public EnumUserAccessLevel AccessLevelMFCS
        {
            get { return _accessLevelMFCS; }
            set
            {
                if (_accessLevelMFCS != value)
                {
                    _accessLevelMFCS = value;
                    RaisePropertyChanged("AccessLevelMFCS");
                }
            }
        }
        public bool EditEnabledUser
        {
            get { return _editEnabledUser; }
            set
            {
                if (_editEnabledUser != value)
                {
                    _editEnabledUser = value;
                    RaisePropertyChanged("EditEnabledUser");
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
        public UserViewModel()
        {
            UserName = "";
            Validator = new PropertyValidator();
            ValidationEnabled = false;
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
            get { return (this as IDataErrorInfo).Error; }
        }

        public string this[string propertyName]
        {
            get
            {
                try
                {
                    string validationResult = String.Empty;
                    if (ValidationEnabled)
                    {
                        switch (propertyName)
                        {
                            case "UserName":
                                if (_editEnabledUser && (_warehouse.DBService.GetUser(_userName) != null || _userName.Length == 0))
                                    validationResult = ResourceReader.GetString("ERR_USER");
                                break;
                            case "Password1":
                            case "Password2":
                                if (Password1 != Password2)
                                    validationResult = ResourceReader.GetString("ERR_PASSWORD");
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
