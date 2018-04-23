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
        private User _user;
        private EnumUserAccessLevel _accessLevelWMS;
        private EnumUserAccessLevel _accessLevelMFCS;
        private bool _allPropertiesValid = false;
        private bool _validationEnabled = false;
        private BasicWarehouse _warehouse;
        #endregion

        #region properties
        PropertyValidator Validator { get; set; }
        public User User
        {
            get { return _user; }
            set
            {
                if (_user != value)
                {
                    _user = value;
                    RaisePropertyChanged("User");
                }
            }
        }
        public string UserName
        {
            get { return _user.User1; }
            set
            {
                if (_user.User1 != value)
                {
                    _user.User1 = value;
                    RaisePropertyChanged("UserName");
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
            _user = new User();
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
            get { return (User as IDataErrorInfo).Error; }
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
                                if (_warehouse.DBService.GetUser(_user.User1) != null)
                                    validationResult = ResourceReader.GetString("ERR_USER_EXISTS");
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
