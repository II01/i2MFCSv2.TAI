using System;
using System.Linq;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.CommandWpf;
using System.Collections.ObjectModel;
using UserInterface.Services;
using Database;
using Warehouse.Model;
using System.Diagnostics;
using GalaSoft.MvvmLight.Messaging;
using UserInterface.Messages;
using WCFClients;
using UserInterface.DataServiceWMS;
using System.Collections.Generic;
using UserInterface.ProxyWMS_UI;
using System.Threading.Tasks;
using GalaSoft.MvvmLight.Ioc;
using System.Collections;

namespace UserInterface.ViewModel
{
    public sealed class PackageIDsViewModel : ViewModelBase
    {
        public enum CommandType { None = 0, Add, Edit};

        #region members
        private CommandType _selectedCommand;
        private ObservableCollection<PackageIDViewModel> _PackageIDList;
        private PackageIDViewModel _selectedPackageID;
        private List<PackageIDViewModel> _selectedPackageIDs;
        private PackageIDViewModel _detailedPackageID;
        private PackageIDViewModel _managePackageID;
        private bool _editEnabled;
        private bool _enabledCC;
        private BasicWarehouse _warehouse;
        private DBServiceWMS _dbservicewms;
        private int _accessLevel;
        private string _accessUser;
        private int _numberOfSelectedItems;
        #endregion

        #region properites
        public RelayCommand Add { get; private set; }
        public RelayCommand Edit { get; private set; }
        public RelayCommand Confirm { get; private set; }
        public RelayCommand Cancel { get; private set; }
        public RelayCommand Refresh { get; private set; }
        public RelayCommand<IList> SelectionChangedCommand { get; private set; }

        public ObservableCollection<PackageIDViewModel> PackageIDList
        {
            get { return _PackageIDList; }
            set
            {
                if (_PackageIDList != value)
                {
                    _PackageIDList = value;
                    RaisePropertyChanged("PackageIDList");
                }
            }
        }

        public PackageIDViewModel SelectedPackageID
        {
            get
            {
                return _selectedPackageID;
            }
            set
            {
                if (_selectedPackageID != value)
                {
                    _selectedPackageID = value;
                    RaisePropertyChanged("SelectedPackageID");
                    try
                    {
                        if (_selectedPackageID != null)
                        {
                            var l = new List<string>();
                            l.Add(SelectedPackageID.SKUID);
                            SelectedPackageID.SKUIDs = l;                            
                            DetailedPackageID = SelectedPackageID;
                            DetailedPackageID.EditVisible = true;
                        }
                    }
                    catch (Exception e)
                    {
                        _warehouse.AddEvent(Database.Event.EnumSeverity.Error, Database.Event.EnumType.Exception, e.Message);
                        throw new Exception(string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
                    }
                }
            }
        }

        public List<PackageIDViewModel> SelectedPackageIDs
        {
            get
            {
                return _selectedPackageIDs;
            }
            set
            {
                if (_selectedPackageIDs != value)
                {
                    _selectedPackageIDs = value;
                    RaisePropertyChanged("SelectedPackageIDs");
                }
            }
        }

        public PackageIDViewModel DetailedPackageID
        {
            get { return _detailedPackageID; }
            set
            {
                if (_detailedPackageID != value)
                {
                    _detailedPackageID = value;
                    RaisePropertyChanged("DetailedPackageID");
                }
            }
        }

        public bool EditEnabled
        {
            get { return _editEnabled; }
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
            get { return _enabledCC; }
            set
            {
                if (_enabledCC != value)
                {
                    _enabledCC = value;
                    RaisePropertyChanged("EnabledCC");
                }
            }
        }
        public int AccessLevel
        {
            get
            {
                return _accessLevel;
            }
            set
            {
                if (_accessLevel != value)
                {
                    _accessLevel = value;
                    RaisePropertyChanged("AccessLevel");
                }
            }
        }

        public int NumberOfSelectedItems
        {
            get
            {
                return _numberOfSelectedItems;
            }
            set
            {
                if (_numberOfSelectedItems != value)
                {
                    _numberOfSelectedItems = value;
                    RaisePropertyChanged("NumberOfSelectedItems");
                }
            }
        }

        #endregion

        #region initialization
        public PackageIDsViewModel()
        {
            DetailedPackageID = null;
            SelectedPackageID = null;
            _managePackageID = new PackageIDViewModel();
            _managePackageID.Initialize(_warehouse);

            EditEnabled = false;
            EnabledCC = false;

            _selectedCommand = CommandType.None;

            Add = new RelayCommand(() => ExecuteAdd(), CanExecuteAdd);
            Edit = new RelayCommand(() => ExecuteEdit(), CanExecuteEdit);
            Cancel = new RelayCommand(() => ExecuteCancel(), CanExecuteCancel);
            Confirm = new RelayCommand(async () => await ExecuteConfirm(), CanExecuteConfirm);
            Refresh = new RelayCommand(async () => await ExecuteRefresh());
            SelectionChangedCommand = new RelayCommand<IList>(items => NumberOfSelectedItems = items == null ? 0 : items.Count);
        }

        public void Initialize(BasicWarehouse warehouse)
        {
            _warehouse = warehouse;
            _dbservicewms = new DBServiceWMS(_warehouse);
            try
            {
                PackageIDList = new ObservableCollection<PackageIDViewModel>();
                SelectedPackageIDs = new List<PackageIDViewModel>();
                _accessUser = "";
                Messenger.Default.Register<MessageAccessLevel>(this, (mc) => { AccessLevel = mc.AccessLevel; _accessUser = mc.User; });
                Messenger.Default.Register<MessageViewChanged>(this, async (vm) => await ExecuteViewActivated(vm.ViewModel));
            }
            catch (Exception e)
            {
                _warehouse.AddEvent(Database.Event.EnumSeverity.Error, Database.Event.EnumType.Exception, e.Message);
                throw new Exception(string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
            }
        }
        #endregion

        #region commands
        private void ExecuteAdd()
        {
            try
            {
                EditEnabled = true;
                EnabledCC = true;
                _selectedCommand = CommandType.Add;
                _managePackageID.ID = "";
                _managePackageID.SKUID = "";
                _managePackageID.Batch = "";
                _managePackageID.SKUIDs = _dbservicewms.GetSKUIDsSync().ConvertAll(p => p.ID);
                if (_managePackageID.SKUIDs.Count > 0)
                    _managePackageID.SKUID = _managePackageID.SKUIDs[0];
                DetailedPackageID = _managePackageID;
                DetailedPackageID.AddEnabled = true;
                DetailedPackageID.ValidationEnabled = true;
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
                return !EditEnabled && AccessLevel / 10 >= 2;
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
                EditEnabled = true;
                EnabledCC = true;                
                _selectedCommand = CommandType.Edit;
                _managePackageID.ID = SelectedPackageID.ID;
                _managePackageID.SKUID = SelectedPackageID.SKUID;
                _managePackageID.Batch = SelectedPackageID.Batch;
                _managePackageID.SKUIDs = _dbservicewms.GetSKUIDsSync().ConvertAll(p => p.ID);
                DetailedPackageID = _managePackageID;
                DetailedPackageID.AddEnabled = false;
                DetailedPackageID.ValidationEnabled = true;
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
                return !EditEnabled && (SelectedPackageID != null) && AccessLevel/10 >= 2;
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
                if (DetailedPackageID != null)
                {
                    DetailedPackageID.EditVisible = true;
                    DetailedPackageID.ValidationEnabled = false;
                }
                DetailedPackageID = SelectedPackageID;
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
                return EditEnabled;
            }
            catch (Exception e)
            {
                _warehouse.AddEvent(Database.Event.EnumSeverity.Error, Database.Event.EnumType.Exception, 
                                    string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
                return false;
            }
        }
        private async Task ExecuteConfirm()
        {
            try
            {
                DetailedPackageID.EditVisible = true;
                EditEnabled = false;
                EnabledCC = false;
                try
                {
                    switch (_selectedCommand)
                    {
                        case CommandType.Add:
                            _dbservicewms.AddPackageID(DetailedPackageID.PackageID);
                            _warehouse.AddEvent(Event.EnumSeverity.Event, Event.EnumType.Material,
                                                String.Format("PackageID added: id: {0}", DetailedPackageID.ID));
                            SelectedPackageID = DetailedPackageID;
                            await ExecuteRefresh();
                            _dbservicewms.AddLog(_accessUser, EnumLogWMS.Event, "UI", $"Add PackageID: {DetailedPackageID.PackageID.ToString()}");
                            break;
                        case CommandType.Edit:
                            _dbservicewms.UpdatePackageID(DetailedPackageID.PackageID);
                            _warehouse.AddEvent(Event.EnumSeverity.Event, Event.EnumType.Material,
                                                String.Format("PackageID changed: id: {0}", DetailedPackageID.ID));
                            SelectedPackageID.ID = DetailedPackageID.ID;
                            SelectedPackageID.SKUID = DetailedPackageID.SKUID;
                            SelectedPackageID.Batch = DetailedPackageID.Batch;
                            _dbservicewms.AddLog(_accessUser, EnumLogWMS.Event, "UI", $"Edit PackageID: {DetailedPackageID.PackageID.ToString()}");
                            break;
                    }
                    if (DetailedPackageID != null)
                        DetailedPackageID.ValidationEnabled = false;
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
                return EditEnabled && DetailedPackageID.AllPropertiesValid && AccessLevel/10 >= 2;
            }
            catch (Exception e)
            {
                _warehouse.AddEvent(Database.Event.EnumSeverity.Error, Database.Event.EnumType.Exception, 
                                    string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
                return false;
            }
        }
        private async Task ExecuteRefresh()
        {
            try
            {
                string id = SelectedPackageID?.ID;
                var packageids = await _dbservicewms.GetPackageIDs();
                PackageIDList.Clear();
                foreach (var p in packageids)
                    PackageIDList.Add(new PackageIDViewModel
                    {
                        ID = p.ID,
                        SKUID = p.SKU_ID,
                        Batch = p.Batch
                    });
                foreach (var l in PackageIDList)
                    l.Initialize(_warehouse);
                if ( id != null)
                    SelectedPackageID = PackageIDList.FirstOrDefault(p => p.ID == id);
            }
            catch (Exception e)
            {
                _warehouse.AddEvent(Database.Event.EnumSeverity.Error, Database.Event.EnumType.Exception, 
                                    string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
            }
        }
        #endregion
        public async Task ExecuteViewActivated(ViewModelBase vm)
        {
            try
            {
                if (vm is PackageIDsViewModel)
                {
                    await ExecuteRefresh();
                }
            }
            catch (Exception e)
            {
                _warehouse.AddEvent(Event.EnumSeverity.Error, Event.EnumType.Exception,
                                    string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
            }
        }
    }
}
