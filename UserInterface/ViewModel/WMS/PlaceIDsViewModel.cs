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

namespace UserInterface.ViewModel
{
    public sealed class PlaceIDsViewModel : ViewModelBase
    {
        public enum CommandType { None = 0, Edit};

        #region members
        private CommandType _selectedCommand;
        private ObservableCollection<PlaceIDViewModel> _PlaceIDList;
        private PlaceIDViewModel _selectedPlaceID;
        private PlaceIDViewModel _detailedPlaceID;
        private PlaceIDViewModel _managePlaceID;
        private bool _editEnabled;
        private bool _enabledCC;
        private BasicWarehouse _warehouse;
        private DBServiceWMS _dbservicewms;
        private int _accessLevel;
        #endregion

        #region properites
        public RelayCommand Edit { get; private set; }
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
                            DetailedPlaceID = SelectedPlaceID;
                    }
                    catch (Exception e)
                    {
                        _warehouse.AddEvent(Database.Event.EnumSeverity.Error, Database.Event.EnumType.Exception, e.Message);
                        throw new Exception(string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
                    }
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
            Cancel = new RelayCommand(() => ExecuteCancel(), CanExecuteCancel);
            Confirm = new RelayCommand(() => ExecuteConfirm(), CanExecuteConfirm);
            Refresh = new RelayCommand(() => ExecuteRefresh());
        }

        public void Initialize(BasicWarehouse warehouse)
        {
            _warehouse = warehouse;
            _dbservicewms = new DBServiceWMS(_warehouse);
            try
            {
                PlaceIDList = new ObservableCollection<PlaceIDViewModel>();
                foreach (var p in _dbservicewms.GetPlaceIDs())
                    PlaceIDList.Add(new PlaceIDViewModel
                    {
                        ID = p.ID,
                        PositionTravel = p.PositionTravel,
                        PositionHoist = p.PositionHoist,
                        DimensionClass = p.DimensionClass,
                        FrequencyClass = p.FrequencyClass,
                        Status = p.Status
                    });
                foreach (var l in PlaceIDList)
                    l.Initialize(_warehouse);
                Messenger.Default.Register<MessageAccessLevel>(this, (mc) => { AccessLevel = mc.AccessLevel; });
                Messenger.Default.Register<MessageViewChanged>(this, vm => ExecuteViewActivated(vm.ViewModel));
            }
            catch (Exception e)
            {
                _warehouse.AddEvent(Database.Event.EnumSeverity.Error, Database.Event.EnumType.Exception, e.Message);
                throw new Exception(string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
            }
        }
        #endregion

        #region commands
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
                return !EditEnabled && (SelectedPlaceID != null) && AccessLevel >= 1;
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
                        case CommandType.Edit:
                            _dbservicewms.UpdatePlaceID(DetailedPlaceID.PlaceID);
                            _warehouse.AddEvent(Event.EnumSeverity.Event, Event.EnumType.Material,
                                                String.Format("PlaceID changed: id: {0}", DetailedPlaceID.ID));
                            SelectedPlaceID.PositionTravel = DetailedPlaceID.PositionTravel;
                            SelectedPlaceID.PositionHoist = DetailedPlaceID.PositionHoist;
                            SelectedPlaceID.DimensionClass = DetailedPlaceID.DimensionClass;
                            SelectedPlaceID.FrequencyClass = DetailedPlaceID.FrequencyClass;
                            SelectedPlaceID.Status = DetailedPlaceID.Status;
                            /*                            PlaceIDList.Clear();
                                                        foreach (var p in _dbservicewms.GetPlaceIDs())
                                                            PlaceIDList.Add(new PlaceIDViewModel {
                                                                ID = p.ID,
                                                                PositionTravel = p.PositionTravel,
                                                                PositionHoist = p.PositionHoist,
                                                                DimensionClass = p.DimensionClass,
                                                                FrequencyClass = p.FrequencyClass,
                                                                Status = p.Status});
                                                        foreach (var l in PlaceIDList)
                                                            l.Initialize(_warehouse);*/
                            break;
                        default:
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
                return EditEnabled && DetailedPlaceID.AllPropertiesValid && AccessLevel >= 1;
            }
            catch (Exception e)
            {
                _warehouse.AddEvent(Database.Event.EnumSeverity.Error, Database.Event.EnumType.Exception, 
                                    string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
                return false;
            }
        }
        private void ExecuteRefresh()
        {
            try
            {
                PlaceIDViewModel sl = SelectedPlaceID; 
                PlaceIDList.Clear();
                foreach (var p in _dbservicewms.GetPlaceIDs())
                    PlaceIDList.Add(new PlaceIDViewModel
                    {
                        ID = p.ID,
                        PositionTravel = p.PositionTravel,
                        PositionHoist = p.PositionHoist,
                        DimensionClass = p.DimensionClass,
                        FrequencyClass = p.FrequencyClass,
                        Status = p.Status
                    });
                foreach (var l in PlaceIDList)
                    l.Initialize(_warehouse);
                if ( sl != null)
                    SelectedPlaceID = PlaceIDList.FirstOrDefault(p => p.ID == sl.ID);
            }
            catch (Exception e)
            {
                _warehouse.AddEvent(Database.Event.EnumSeverity.Error, Database.Event.EnumType.Exception, 
                                    string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
            }
        }
        #endregion
        public void ExecuteViewActivated(ViewModelBase vm)
        {
            try
            {
                if (vm is PlaceIDsViewModel)
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
    }
}
