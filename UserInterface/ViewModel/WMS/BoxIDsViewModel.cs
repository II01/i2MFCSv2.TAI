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
    public sealed class BoxIDsViewModel : ViewModelBase
    {
        public enum CommandType { None = 0, Add, Edit};

        #region members
        private CommandType _selectedCommand;
        private ObservableCollection<BoxIDViewModel> _boxIDList;
        private BoxIDViewModel _selectedBoxID;
        private List<BoxIDViewModel> _selectedBoxIDs;
        private BoxIDViewModel _detailedBoxID;
        private BoxIDViewModel _manageBoxID;
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

        public ObservableCollection<BoxIDViewModel> BoxIDList
        {
            get { return _boxIDList; }
            set
            {
                if (_boxIDList != value)
                {
                    _boxIDList = value;
                    RaisePropertyChanged("BoxIDList");
                }
            }
        }

        public BoxIDViewModel SelectedBoxID
        {
            get
            {
                return _selectedBoxID;
            }
            set
            {
                if (_selectedBoxID != value)
                {
                    _selectedBoxID = value;
                    RaisePropertyChanged("SelectedBoxID");
                    try
                    {
                        if (_selectedBoxID != null)
                        {
                            var l = new List<string>();
                            l.Add(SelectedBoxID.SKUID);
                            SelectedBoxID.SKUIDs = l;                            
                            DetailedBoxID = SelectedBoxID;
                            DetailedBoxID.EditVisible = true;
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

        public List<BoxIDViewModel> SelectedBoxIDs
        {
            get
            {
                return _selectedBoxIDs;
            }
            set
            {
                if (_selectedBoxIDs != value)
                {
                    _selectedBoxIDs = value;
                    RaisePropertyChanged("SelectedBoxIDs");
                }
            }
        }

        public BoxIDViewModel DetailedBoxID
        {
            get { return _detailedBoxID; }
            set
            {
                if (_detailedBoxID != value)
                {
                    _detailedBoxID = value;
                    RaisePropertyChanged("DetailedBoxID");
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
        public BoxIDsViewModel()
        {
            DetailedBoxID = null;
            SelectedBoxID = null;
            _manageBoxID = new BoxIDViewModel();
            _manageBoxID.Initialize(_warehouse);

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
                BoxIDList = new ObservableCollection<BoxIDViewModel>();
                SelectedBoxIDs = new List<BoxIDViewModel>();
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
                _manageBoxID.ID = "";
                _manageBoxID.SKUID = "";
                _manageBoxID.Batch = "";
                _manageBoxID.SKUIDs = _dbservicewms.GetSKUIDsSync().ConvertAll(p => p.ID);
                if (_manageBoxID.SKUIDs.Count > 0)
                    _manageBoxID.SKUID = _manageBoxID.SKUIDs[0];
                DetailedBoxID = _manageBoxID;
                DetailedBoxID.AddEnabled = true;
                DetailedBoxID.ValidationEnabled = true;
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
                _manageBoxID.ID = SelectedBoxID.ID;
                _manageBoxID.SKUID = SelectedBoxID.SKUID;
                _manageBoxID.Batch = SelectedBoxID.Batch;
                _manageBoxID.SKUIDs = _dbservicewms.GetSKUIDsSync().ConvertAll(p => p.ID);
                DetailedBoxID = _manageBoxID;
                DetailedBoxID.AddEnabled = false;
                DetailedBoxID.ValidationEnabled = true;
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
                return !EditEnabled && (SelectedBoxID != null) && AccessLevel/10 >= 2;
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
                if (DetailedBoxID != null)
                {
                    DetailedBoxID.EditVisible = true;
                    DetailedBoxID.ValidationEnabled = false;
                }
                DetailedBoxID = SelectedBoxID;
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
                DetailedBoxID.EditVisible = true;
                EditEnabled = false;
                EnabledCC = false;
                try
                {
                    switch (_selectedCommand)
                    {
                        case CommandType.Add:
                            _dbservicewms.AddBoxID(DetailedBoxID.BoxID);
                            _warehouse.AddEvent(Event.EnumSeverity.Event, Event.EnumType.Material,
                                                String.Format("BoxID added: id: {0}", DetailedBoxID.ID));
                            SelectedBoxID = DetailedBoxID;
                            await ExecuteRefresh();
                            _dbservicewms.AddLog(_accessUser, EnumLogWMS.Event, "UI", $"Add BoxID: {DetailedBoxID.BoxID.ToString()}");
                            break;
                        case CommandType.Edit:
                            _dbservicewms.UpdateBoxID(DetailedBoxID.BoxID);
                            _warehouse.AddEvent(Event.EnumSeverity.Event, Event.EnumType.Material,
                                                String.Format("BoxID changed: id: {0}", DetailedBoxID.ID));
                            SelectedBoxID.ID = DetailedBoxID.ID;
                            SelectedBoxID.SKUID = DetailedBoxID.SKUID;
                            SelectedBoxID.Batch = DetailedBoxID.Batch;
                            _dbservicewms.AddLog(_accessUser, EnumLogWMS.Event, "UI", $"Edit BoxID: {DetailedBoxID.BoxID.ToString()}");
                            break;
                    }
                    if (DetailedBoxID != null)
                        DetailedBoxID.ValidationEnabled = false;
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
                return EditEnabled && DetailedBoxID.AllPropertiesValid && AccessLevel/10 >= 2;
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
                string id = SelectedBoxID?.ID;
                var BoxIDs = await _dbservicewms.GetBoxIDs();
                BoxIDList.Clear();
                foreach (var p in BoxIDs)
                    BoxIDList.Add(new BoxIDViewModel
                    {
                        ID = p.ID,
                        SKUID = p.SKU_ID,
                        Batch = p.Batch
                    });
                foreach (var l in BoxIDList)
                    l.Initialize(_warehouse);
                if ( id != null)
                    SelectedBoxID = BoxIDList.FirstOrDefault(p => p.ID == id);
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
                if (vm is BoxIDsViewModel)
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
