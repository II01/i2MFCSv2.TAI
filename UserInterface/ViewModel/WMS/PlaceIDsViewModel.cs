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

namespace UserInterface.ViewModel
{
    public sealed class PlaceIDsViewModel : ViewModelBase
    {
        public enum CommandType { None = 0, Edit, Block, Unblock};

        #region members
        private CommandType _selectedCommand;
        private ObservableCollection<PlaceIDViewModel> _PlaceIDList;
        private PlaceIDViewModel _selectedPlaceID;
        private List<PlaceIDViewModel> _selectedPlaceIDs;
        private PlaceIDViewModel _detailedPlaceID;
        private PlaceIDViewModel _managePlaceID;
        private bool _editEnabled;
        private bool _enabledCC;
        private string _blockUnblockLocations;
        private BasicWarehouse _warehouse;
        private DBServiceWMS _dbservicewms;
        private int _accessLevel;
        private string _accessUser;
        #endregion

        #region properites
        public RelayCommand Edit { get; private set; }
        public RelayCommand Block { get; private set; }
        public RelayCommand Unblock { get; private set; }
        public RelayCommand Confirm { get; private set; }
        public RelayCommand Cancel { get; private set; }
        public RelayCommand Refresh { get; private set; }

        public ObservableCollection<PlaceIDViewModel> PlaceIDList
        {
            get { return _PlaceIDList; }
            set
            {
                if (_PlaceIDList != value)
                {
                    _PlaceIDList = value;
                    RaisePropertyChanged("PlaceIDList");
                }
            }
        }

        public PlaceIDViewModel SelectedPlaceID
        {
            get
            {
                return _selectedPlaceID;
            }
            set
            {
                if (_selectedPlaceID != value)
                {
                    _selectedPlaceID = value;
                    RaisePropertyChanged("SelectedPlaceID");
                    try
                    {
                        if (_selectedPlaceID != null)
                        {
                            DetailedPlaceID = SelectedPlaceID;
                            BlockUnblockLocations = DetailedPlaceID.ID;
                            DetailedPlaceID.EditVisible = true;
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

        public List<PlaceIDViewModel> SelectedPlaceIDs
        {
            get
            {
                return _selectedPlaceIDs;
            }
            set
            {
                if (_selectedPlaceIDs != value)
                {
                    _selectedPlaceIDs = value;
                    RaisePropertyChanged("SelectedPlaceIDs");
                }
            }
        }

        public PlaceIDViewModel DetailedPlaceID
        {
            get { return _detailedPlaceID; }
            set
            {
                if (_detailedPlaceID != value)
                {
                    _detailedPlaceID = value;
                    RaisePropertyChanged("DetailedPlaceID");
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
        public string BlockUnblockLocations
        {
            get { return _blockUnblockLocations; }
            set
            {
                if (_blockUnblockLocations != value)
                {
                    _blockUnblockLocations = value;
                    RaisePropertyChanged("BlockUnblockLocartions");
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
        #endregion

        #region initialization
        public PlaceIDsViewModel()
        {
            DetailedPlaceID = null;
            SelectedPlaceID = null;
            _managePlaceID = new PlaceIDViewModel();
            _managePlaceID.Initialize(_warehouse);

            EditEnabled = false;
            EnabledCC = false;

            _selectedCommand = CommandType.None;

            Edit = new RelayCommand(() => ExecuteEdit(), CanExecuteEdit);
            Block = new RelayCommand(() => ExecuteBlock(), CanExecuteBlock);
            Unblock = new RelayCommand(() => ExecuteUnblock(), CanExecuteUnblock);
            Cancel = new RelayCommand(() => ExecuteCancel(), CanExecuteCancel);
            Confirm = new RelayCommand(async () => await ExecuteConfirm(), CanExecuteConfirm);
            Refresh = new RelayCommand(async () => await ExecuteRefresh());
        }

        public void Initialize(BasicWarehouse warehouse)
        {
            _warehouse = warehouse;
            _dbservicewms = new DBServiceWMS(_warehouse);
            try
            {
                PlaceIDList = new ObservableCollection<PlaceIDViewModel>();
                SelectedPlaceIDs = new List<PlaceIDViewModel>();
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
        private void ExecuteBlock()
        {
            try
            {
                _selectedCommand = CommandType.Block;
                DetailedPlaceID = new PlaceIDViewModel();
                DetailedPlaceID.Initialize(_warehouse);
                DetailedPlaceID.EditVisible = false;
                DetailedPlaceID.ValidationEnabled = true;
                DetailedPlaceID.FrequencyClass = 1;
                if (SelectedPlaceID != null)
                    DetailedPlaceID.ID = SelectedPlaceID.ID;
                EditEnabled = true;
                EnabledCC = true;
            }
            catch (Exception e)
            {
                _warehouse.AddEvent(Database.Event.EnumSeverity.Error, Database.Event.EnumType.Exception,
                                    string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
            }
        }

        private bool CanExecuteBlock()
        {
            try
            {
                return  !SimpleIoc.Default.GetInstance<ControlPanelViewModel>().Modes.StateWMS && 
                        !SimpleIoc.Default.GetInstance<ControlPanelViewModel>().Modes.StateRun && 
                        !EditEnabled && AccessLevel/10 >= 2;
            }
            catch (Exception e)
            {
                _warehouse.AddEvent(Database.Event.EnumSeverity.Error, Database.Event.EnumType.Exception,
                                    string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
                return false;
            }
        }
        private void ExecuteUnblock()
        {
            try
            {
                _selectedCommand = CommandType.Unblock;
                DetailedPlaceID = new PlaceIDViewModel();
                DetailedPlaceID.Initialize(_warehouse);
                DetailedPlaceID.EditVisible = false;
                DetailedPlaceID.ValidationEnabled = true;
                DetailedPlaceID.FrequencyClass = 1;
                if (SelectedPlaceID != null)
                    DetailedPlaceID.ID = SelectedPlaceID.ID;
                EditEnabled = true;
                EnabledCC = true;
            }
            catch (Exception e)
            {
                _warehouse.AddEvent(Database.Event.EnumSeverity.Error, Database.Event.EnumType.Exception,
                                    string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
            }
        }

        private bool CanExecuteUnblock()
        {
            try
            {
                return  !SimpleIoc.Default.GetInstance<ControlPanelViewModel>().Modes.StateWMS &&
                        !SimpleIoc.Default.GetInstance<ControlPanelViewModel>().Modes.StateRun &&
                        !EditEnabled && AccessLevel/10 >= 2;
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
                _managePlaceID.ID = SelectedPlaceID.ID;
                _managePlaceID.PositionTravel = SelectedPlaceID.PositionTravel;
                _managePlaceID.PositionHoist = SelectedPlaceID.PositionHoist;
                _managePlaceID.DimensionClass = SelectedPlaceID.DimensionClass;
                _managePlaceID.FrequencyClass = SelectedPlaceID.FrequencyClass;
                _managePlaceID.Status = SelectedPlaceID.Status;
                DetailedPlaceID = _managePlaceID;
                DetailedPlaceID.ValidationEnabled = true;
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
                return !EditEnabled && (SelectedPlaceID != null) && AccessLevel/10 >= 2;
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
                if (DetailedPlaceID != null)
                {
                    DetailedPlaceID.EditVisible = true;
                    DetailedPlaceID.ValidationEnabled = false;
                }
                DetailedPlaceID = SelectedPlaceID;
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
                DetailedPlaceID.EditVisible = true;
                EditEnabled = false;
                EnabledCC = false;
                try
                {
                    switch (_selectedCommand)
                    {
                        case CommandType.Edit:
                            _dbservicewms.UpdatePlaceID(DetailedPlaceID.PlaceID);
                            _warehouse.AddEvent(Event.EnumSeverity.Event, Event.EnumType.Material,
                                                String.Format("PlaceID changed: id: {0}", DetailedPlaceID.ID));
                            SelectedPlaceID.PositionTravel = DetailedPlaceID.PositionTravel;
                            SelectedPlaceID.PositionHoist = DetailedPlaceID.PositionHoist;
                            SelectedPlaceID.DimensionClass = DetailedPlaceID.DimensionClass;
                            SelectedPlaceID.FrequencyClass = DetailedPlaceID.FrequencyClass;
                            SelectedPlaceID.Status = DetailedPlaceID.Status;
                            _dbservicewms.AddLog(_accessUser, EnumLogWMS.Event, "UI", $"Edit PlaceID: {DetailedPlaceID.PlaceID.ToString()}");
                            break;
                        case CommandType.Block:
                        case CommandType.Unblock:
                            using (WMSToUIClient client = new WMSToUIClient())
                            {
                                EnumBlockedWMS reason = (DetailedPlaceID.ID.Length <= 4 && DetailedPlaceID.ID.StartsWith("W")) ? EnumBlockedWMS.Vehicle : EnumBlockedWMS.Rack;
                                await client.BlockLocationsAsync(DetailedPlaceID.ID, _selectedCommand == CommandType.Block, (int)reason);
                                await ExecuteRefresh();
                                string bl = _selectedCommand == CommandType.Block ? "BLOCK" : "UNBLOCK";
                                _dbservicewms.AddLog(_accessUser, EnumLogWMS.Event, "UI", $"Block locations: {DetailedPlaceID.ID}|{bl}|{(int)reason}|");
                            }
                            break;
                    }
                    if (DetailedPlaceID != null)
                        DetailedPlaceID.ValidationEnabled = false;
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
                return EditEnabled && DetailedPlaceID.AllPropertiesValid && AccessLevel/10 >= 2;
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
                string id = SelectedPlaceID?.ID; 
                PlaceIDList.Clear();
                foreach (var p in await _dbservicewms.GetPlaceIDs(0, int.MaxValue))
                    PlaceIDList.Add(new PlaceIDViewModel
                    {
                        ID = p.ID,
                        PositionTravel = p.PositionTravel,
                        PositionHoist = p.PositionHoist,
                        DimensionClass = p.DimensionClass,
                        FrequencyClass = p.FrequencyClass,
                        Status = (EnumBlockedWMS)p.Status
                    });
                foreach (var l in PlaceIDList)
                    l.Initialize(_warehouse);
                if ( id != null)
                    SelectedPlaceID = PlaceIDList.FirstOrDefault(p => p.ID == id);
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
                if (vm is PlaceIDsViewModel)
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
