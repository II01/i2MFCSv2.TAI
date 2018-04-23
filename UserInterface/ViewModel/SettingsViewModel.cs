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
using System.DirectoryServices.AccountManagement;
using System.Collections.ObjectModel;
using System.Linq;

namespace UserInterface.ViewModel
{
    public class SettingsViewModel: ViewModelBase
    {
        #region members
        BasicWarehouse _warehouse;
        private string _password = "";
        private string _user = "";
        private bool _enabledReduceDB;
        private ObservableCollection<UserViewModel> _dataList;
        private UserViewModel _selected;
        private UserViewModel _detailed;
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

        public ObservableCollection<UserViewModel> DataList
        {
            get
            {
                return _dataList;
            }
            set
            {
                if (_dataList != value)
                {
                    _dataList = value;
                    RaisePropertyChanged("DataList");
                }
            }
        }

        public UserViewModel Selected
        {
            get
            {
                return _selected;
            }
            set
            {
                if (_selected != value)
                {
                    _selected = value;
                    RaisePropertyChanged("Selected");
                    Detailed = Selected;
                }
            }
        }
        public UserViewModel Detailed
        {
            get
            {
                return _detailed;
            }
            set
            {
                if (_detailed != value)
                {
                    _detailed = value;
                    RaisePropertyChanged("Detailed");
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
            Messenger.Default.Register<MessageViewChanged>(this, vm => ExecuteViewActivated(vm.ViewModel));
        }
        public void Initialize(BasicWarehouse warehouse)
        {
            _warehouse = warehouse;
            try
            {
                SendAccessLevelAndUser(App.AccessLevel, "");
                DataList = new ObservableCollection<UserViewModel>();
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
                bool valid = false;
                // domain
                try
                {
                    using (PrincipalContext context = new PrincipalContext(ContextType.Domain))
                    {
                        valid = context.ValidateCredentials(User, Password);
                    }
                }
                catch { }
                try
                {
                    if(!valid)
                        using (PrincipalContext context = new PrincipalContext(ContextType.Machine))
                        {
                            valid = context.ValidateCredentials(User, Password);
                        }
                }
                catch { }
                if(!valid )
                {
                    User usr = _warehouse.DBService.GetUserPassword(User);
                    if (usr != null && usr.Password == Password)
                        valid = true;    
                }
                if(valid)
                {
                    User usr = _warehouse.DBService.GetUserPassword(User);
                    if (usr != null)
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
                }
                else
                {
                    App.AccessLevel = 0;
                    SendAccessLevelAndUser(App.AccessLevel, "");
                }

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
        private void ExecuteRefresh()
        {
            try
            {
                UserViewModel u = Selected;
                DataList.Clear();
                foreach (var p in _warehouse.DBService.GetUsers())
                    DataList.Add(
                        new UserViewModel
                        {
                            UserName = p.User1.ToUpper(),
                            AccessLevelWMS = (EnumUserAccessLevel)(p.AccessLevel/10),
                            AccessLevelMFCS = (EnumUserAccessLevel)(p.AccessLevel%10)
                        });
                foreach (var l in DataList)
                    l.Initialize(_warehouse);
                if (u != null)
                    Selected = DataList.FirstOrDefault(p => p.User == u.User);
            }
            catch (Exception e)
            {
                _warehouse.AddEvent(Database.Event.EnumSeverity.Error, Database.Event.EnumType.Exception,
                                    string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
            }
        }

        public void ExecuteViewActivated(ViewModelBase vm)
        {
            try
            {
                if (vm is SettingsViewModel)
                {
                    ExecuteRefresh();
                }
            }
            catch (Exception e)
            {
                _warehouse.AddEvent(Event.EnumSeverity.Error, Event.EnumType.Exception,
                                    string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
            }
        }
        #endregion
    }
}
