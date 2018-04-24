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
    public class UsersViewModel: ViewModelBase
    {
        public enum CommandType { None = 0, Add, Edit, Delete };

        #region members
        BasicWarehouse _warehouse;
        private CommandType _selectedCommand;
        private string _password = "";
        private string _user = "";
        private bool _enabledReduceDB;
        private bool _enabledCC;
        private bool _editEnabled;
        private bool _enabledUserManagement;

        private ObservableCollection<UserViewModel> _dataList;
        private UserViewModel _selected;
        private UserViewModel _detailed;
        #endregion

        #region properties
        public RelayCommand Login { get; private set; }
        public RelayCommand Logout { get; private set; }
        public RelayCommand SwitchLanguage { get; private set; }
        public RelayCommand ReduceDB { get; private set; }
        public RelayCommand Add { get; private set; }
        public RelayCommand Edit { get; private set; }
        public RelayCommand Delete { get; private set; }
        public RelayCommand Confirm { get; private set; }
        public RelayCommand Cancel { get; private set; }
        public RelayCommand Refresh { get; private set; }

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
        public bool EnabledUserManagement
        {
            get
            {
                return _enabledUserManagement;
            }
            set
            {
                if (_enabledUserManagement != value)
                {
                    _enabledUserManagement = value;
                    RaisePropertyChanged("EnabledUserManagement");
                }
            }
        }

        public bool EditEnabled
        {
            get
            {
                return _editEnabled;
            }
            set
            {
                if (_editEnabled != value)
                {
                    _editEnabled = value;
                    RaisePropertyChanged("EditEnabled");
                }
            }
        }

        public bool EnabledCC
        {
            get
            {
                return _enabledCC;
            }
            set
            {
                if (_enabledCC != value)
                {
                    _enabledCC = value;
                    RaisePropertyChanged("EnabledCC");
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
        public UsersViewModel()
        {
            Login = new RelayCommand(() => ExecuteLogin(), CanExecuteLogin);
            Logout = new RelayCommand(() => ExecuteLogout(), CanExecuteLogout);
            SwitchLanguage = new RelayCommand(() => ExecuteSwitchLanguage());
            ReduceDB = new RelayCommand(async () => await ExecuteReduceDB());
            Add = new RelayCommand(() => ExecuteAdd(), CanExecuteAdd);
            Edit = new RelayCommand(() => ExecuteEdit(), CanExecuteEdit);
            Delete = new RelayCommand(() => ExecuteDelete(), CanExecuteDelete);
            Confirm = new RelayCommand(() => ExecuteConfirm(), CanExecuteConfirm);
            Cancel = new RelayCommand(() => ExecuteCancel(), CanExecuteCancel);
            Refresh = new RelayCommand(() => ExecuteRefresh());

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
                // machine
                try
                {
                    if(!valid)
                        using (PrincipalContext context = new PrincipalContext(ContextType.Machine))
                        {
                            valid = context.ValidateCredentials(User, Password);
                        }
                }
                catch { }
                // MFCS user
                if(false && !valid )
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
                EnabledReduceDB = App.AccessLevel == 22;
                EnabledUserManagement = App.AccessLevel == 22;
            }
            catch (Exception e)
            {
                _warehouse.AddEvent(Database.Event.EnumSeverity.Error, Database.Event.EnumType.Exception, 
                                    string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
            }
        }

        private bool CanExecuteLogin()
        {
            try
            {
                return !EditEnabled;
            }
            catch (Exception e)
            {
                _warehouse.AddEvent(Database.Event.EnumSeverity.Error, Database.Event.EnumType.Exception,
                                    string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
                return false;
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

        private bool CanExecuteLogout()
        {
            try
            {
                return !EditEnabled;
            }
            catch (Exception e)
            {
                _warehouse.AddEvent(Database.Event.EnumSeverity.Error, Database.Event.EnumType.Exception,
                                    string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
                return false;
            }
        }

        private void ExecuteAdd()
        {
            try
            {
                _selectedCommand = CommandType.Add;
                EditEnabled = true;
                EnabledCC = true;
                Detailed = new UserViewModel();
                Detailed.Initialize(_warehouse);
                Detailed.EditEnabledUser = true;
                Detailed.ValidationEnabled = true;
            }
            catch (Exception e)
            {
                _warehouse.AddEvent(Database.Event.EnumSeverity.Error, Database.Event.EnumType.Exception,
                                    string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
            }
        }

        private bool CanExecuteAdd()
        {
            try
            {
                return !EditEnabled;
            }
            catch (Exception e)
            {
                _warehouse.AddEvent(Database.Event.EnumSeverity.Error, Database.Event.EnumType.Exception,
                                    string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
                return false;
            }
        }

        private void ExecuteEdit()
        {
            try
            {
                _selectedCommand = CommandType.Edit;
                EditEnabled = true;
                EnabledCC = true;
                Detailed = new UserViewModel();
                Detailed.Initialize(_warehouse);
                Detailed.ValidationEnabled = true;
                Detailed.UserName = Selected.UserName;
                Detailed.AccessLevelWMS = Selected.AccessLevelWMS;
                Detailed.AccessLevelMFCS = Selected.AccessLevelMFCS;
            }
            catch (Exception e)
            {
                _warehouse.AddEvent(Database.Event.EnumSeverity.Error, Database.Event.EnumType.Exception,
                                    string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
            }
        }

        private bool CanExecuteEdit()
        {
            try
            {
                return !EditEnabled && Selected != null;
            }
            catch (Exception e)
            {
                _warehouse.AddEvent(Database.Event.EnumSeverity.Error, Database.Event.EnumType.Exception,
                                    string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
                return false;
            }
        }

        private void ExecuteDelete()
        {
            try
            {
                _selectedCommand = CommandType.Delete;
                EditEnabled = false;
                EnabledCC = true;
                Detailed = Selected;
            }
            catch (Exception e)
            {
                _warehouse.AddEvent(Database.Event.EnumSeverity.Error, Database.Event.EnumType.Exception,
                                    string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
            }
        }

        private bool CanExecuteDelete()
        {
            try
            {
                return !EditEnabled && Selected != null;
            }
            catch (Exception e)
            {
                _warehouse.AddEvent(Database.Event.EnumSeverity.Error, Database.Event.EnumType.Exception,
                                    string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
                return false;
            }
        }

        private void ExecuteCancel()
        {
            try
            {
                EditEnabled = false;
                EnabledCC = false;
                Detailed = Selected;
                if (Detailed != null)
                    Detailed.ValidationEnabled = false;
            }
            catch (Exception e)
            {
                _warehouse.AddEvent(Database.Event.EnumSeverity.Error, Database.Event.EnumType.Exception, e.Message);
            }
        }
        private bool CanExecuteCancel()
        {
            try
            {
                return EnabledCC;
            }
            catch (Exception e)
            {
                _warehouse.AddEvent(Database.Event.EnumSeverity.Error, Database.Event.EnumType.Exception,
                                    string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
                return false;
            }
        }

        private void ExecuteConfirm()
        {
            try
            {
                EditEnabled = false;
                EnabledCC = false;
                try
                {
                    switch (_selectedCommand)
                    {
                        case CommandType.Add:
                            _warehouse.DBService.AddUser(new User { User1 = Detailed.UserName, Password = "", AccessLevel = 10 * (int)Detailed.AccessLevelWMS + (int)Detailed.AccessLevelMFCS });
                            DataList.Add(new UserViewModel { UserName = Detailed.UserName, AccessLevelWMS = Detailed.AccessLevelWMS, AccessLevelMFCS = Detailed.AccessLevelMFCS});
                            Selected = DataList.FirstOrDefault(p => p.UserName == Detailed.UserName);
                            break;
                        case CommandType.Edit:
                            _warehouse.DBService.UpdateUser(new User { User1 = Detailed.UserName, AccessLevel = 10 * (int)Detailed.AccessLevelWMS + (int)Detailed.AccessLevelMFCS });
                            Selected.UserName = Detailed.UserName;
                            Selected.AccessLevelWMS = Detailed.AccessLevelWMS;
                            Selected.AccessLevelMFCS = Detailed.AccessLevelMFCS;
                            break;
                        case CommandType.Delete:
                            _warehouse.DBService.DeleteUser(new User { User1 = Detailed.UserName, AccessLevel = 10 * (int)Detailed.AccessLevelWMS + (int)Detailed.AccessLevelMFCS });
                            DataList.Remove(Detailed);
                            break;
                        default:
                            break;
                    }
                    if (Detailed != null)
                        Detailed.ValidationEnabled = false;
                }
                catch (Exception e)
                {
                    _warehouse.AddEvent(Event.EnumSeverity.Error, Event.EnumType.Exception, e.Message);
                }
            }
            catch (Exception e)
            {
                _warehouse.AddEvent(Database.Event.EnumSeverity.Error, Database.Event.EnumType.Exception,
                                    string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
            }
        }
        private bool CanExecuteConfirm()
        {
            try
            {
                return (EditEnabled && Detailed.AllPropertiesValid) || _selectedCommand == CommandType.Delete;
            }
            catch (Exception e)
            {
                _warehouse.AddEvent(Database.Event.EnumSeverity.Error, Database.Event.EnumType.Exception,
                                    string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
                return false;
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
                    Selected = DataList.FirstOrDefault(p => p.UserName == u.UserName);
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
                if (vm is UsersViewModel)
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
