using System;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.CommandWpf;
using GalaSoft.MvvmLight.Messaging;
using UserInterface.Services;
using UserInterface.Messages;
using Infralution.Localization.Wpf;
using System.Globalization;
using Warehouse.Model;
using System.Diagnostics;
using System.Threading.Tasks;
using Database;
using UserInterface.DataServiceWMS;

namespace UserInterface.ViewModel
{
    public class SettingsViewModel: ViewModelBase
    {
        #region members
        BasicWarehouse _warehouse;
        private string _password = "";
        private string _user = "";
        private bool _enabledReduceDB;
        #endregion

        #region properties
        public RelayCommand Login { get; private set; }
        public RelayCommand Logout { get; private set; }
        public RelayCommand SwitchLanguage { get; private set; }
        public RelayCommand ReduceDB { get; private set; }

        public string User
        {
            get
            {
                return _user;
            }
            set
            {
                if (_user != value)
                {
                    _user = value;
                    RaisePropertyChanged("User");
                }
            }
        }

        public string Password
        {
            get
            {
                return _password;
            }
            set
            {
                if(_password != value )
                {
                    _password = value;
                    RaisePropertyChanged("Password");
                }
            }
        }
        public bool EnabledReduceDB
        {
            get
            {
                return _enabledReduceDB;
            }
            set
            {
                if (_enabledReduceDB != value)
                {
                    _enabledReduceDB = value;
                    RaisePropertyChanged("EnabledReduceDB");
                }
            }
        }
        #endregion

        #region initialization
        public SettingsViewModel()
        {
            Login = new RelayCommand(() => ExecuteLogin());
            Logout = new RelayCommand(() => ExecuteLogout());
            SwitchLanguage = new RelayCommand(() => ExecuteSwitchLanguage());
            ReduceDB = new RelayCommand(async () => await ExecuteReduceDB());
        }
        public void Initialize(BasicWarehouse warehouse)
        {
            _warehouse = warehouse;
            try
            {
                SendAccessLevelAndUser(App.AccessLevel, "");
            }
            catch (Exception e)
            {
                _warehouse.AddEvent(Database.Event.EnumSeverity.Error, Database.Event.EnumType.Exception, e.Message);
                throw new Exception(string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
            }
        }

        #endregion

        #region commands
        private void ExecuteLogin()
        {
            try
            {
                User usr = _warehouse.DBService.GetUserPassword(User);

                if (usr != null && usr.Password == Password)
                {
                    App.AccessLevel = usr.AccessLevel;
                    SendAccessLevelAndUser(App.AccessLevel, usr.User1);
                }
                else
                {
                    App.AccessLevel = 0;
                    SendAccessLevelAndUser(App.AccessLevel, "");
                }
                Password = "";

                EnabledReduceDB = App.AccessLevel == 2;
            }
            catch (Exception e)
            {
                _warehouse.AddEvent(Database.Event.EnumSeverity.Error, Database.Event.EnumType.Exception, 
                                    string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
            }
        }

        private void ExecuteLogout()
        {
            try
            {
                if (App.AccessLevel != 0)
                {
                    App.AccessLevel = 0;
                    SendAccessLevelAndUser(App.AccessLevel, "");
                }
            }
            catch (Exception e)
            {
                _warehouse.AddEvent(Database.Event.EnumSeverity.Error, Database.Event.EnumType.Exception, 
                                    string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
            }
        }

        public void SendAccessLevelAndUser(int al, string user)
        {
            try
            {
                Messenger.Default.Send<MessageAccessLevel>(new MessageAccessLevel() { AccessLevel = al, User = user });
            }
            catch (Exception e)
            {
                _warehouse.AddEvent(Database.Event.EnumSeverity.Error, Database.Event.EnumType.Exception, e.Message);
                throw new Exception(string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
            }
        }

        private void ExecuteSwitchLanguage()
        {
            try
            {
                App.Language = (App.Language + 1) % App.LanguageTag.Count;
                SetLanguage();
            }
            catch (Exception e)
            {
                _warehouse.AddEvent(Database.Event.EnumSeverity.Error, Database.Event.EnumType.Exception, 
                                    string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
            }
        }

        public void SetLanguage()
        {
            try
            {
                CultureManager.UICulture = new CultureInfo(App.LanguageTag[App.Language]);
                ResourceReader.ChangeResourceSet(CultureManager.UICulture);
                Messenger.Default.Send<MessageLanguageChanged>(new MessageLanguageChanged() { Culture = CultureManager.UICulture });
            }
            catch (Exception e)
            {
                _warehouse.AddEvent(Database.Event.EnumSeverity.Error, Database.Event.EnumType.Exception, e.Message);
                throw new Exception(string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
            }
        }
        private async Task ExecuteReduceDB()
        {
            try
            {
                DBServiceWMS ds = new DBServiceWMS(_warehouse);

                double dbSizeGBMax = double.Parse(System.Configuration.ConfigurationManager.AppSettings["DataBaseSizeGBMax"]);
                double dbSizeGBReduced = double.Parse(System.Configuration.ConfigurationManager.AppSettings["DataBaseSizeGBReduced"]);

                EnabledReduceDB = false;
                double sizeDB = _warehouse.DBService.GetDBSizeInGB();
                double reducePerc = 0.0;
                if (true || sizeDB > dbSizeGBMax)
                {
                    reducePerc = 1 - dbSizeGBReduced / Math.Max(dbSizeGBReduced, sizeDB);
                    if (reducePerc < 0.01)
                        reducePerc = 0.0;

                    await _warehouse.DBService.DBCleaning(reducePerc);
                }
                EnabledReduceDB = App.AccessLevel == 2;

            }
            catch (Exception e)
            {
                _warehouse.AddEvent(Database.Event.EnumSeverity.Error, Database.Event.EnumType.Exception,
                                    string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
            }
        }
        #endregion
    }
}
