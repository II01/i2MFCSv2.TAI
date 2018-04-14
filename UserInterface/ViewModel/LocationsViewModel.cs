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

namespace UserInterface.ViewModel
{
    public sealed class LocationsViewModel : ViewModelBase
    {

        #region members
        private ObservableCollection<LocationViewModel> _locationList;
        private LocationViewModel _selectedLocation;
        private LocationViewModel _detailedLocation;
        private bool _editEnabled;
        private bool _enabledCC;
        private BasicWarehouse _warehouse;
        private int _accessLevel;
        #endregion

        #region properites
        public RelayCommand Edit { get; private set; }
        public RelayCommand Confirm { get; private set; }
        public RelayCommand Cancel { get; private set; }
        public RelayCommand Refresh { get; private set; }

        public ObservableCollection<LocationViewModel> LocationList
        {
            get { return _locationList; }
            set
            {
                if (_locationList != value)
                {
                    _locationList = value;
                    RaisePropertyChanged("LocationList");
                }
            }
        }

        public LocationViewModel SelectedLocation
        {
            get
            {
                return _selectedLocation;
            }
            set
            {
                if (_selectedLocation != value)
                {
                    _selectedLocation = value;
                    RaisePropertyChanged("SelectedLocation");
                    try
                    {
                        if (_selectedLocation != null)
                        {
                            DetailedLocation.ID = SelectedLocation.ID;
                            DetailedLocation.Size = SelectedLocation.Size;
                            DetailedLocation.Blocked = SelectedLocation.Blocked;
                            DetailedLocation.Reserved = SelectedLocation.Reserved;
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

        public LocationViewModel DetailedLocation
        {
            get { return _detailedLocation; }
            set
            {
                if (_detailedLocation != value)
                {
                    _detailedLocation = value;
                    RaisePropertyChanged("DetailedLocation");
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
        public LocationsViewModel()
        {
            DetailedLocation = new LocationViewModel();

            EditEnabled = false;
            EnabledCC = false;

            Edit = new RelayCommand(() => ExecuteEdit(), CanExecuteEdit);
            Cancel = new RelayCommand(() => ExecuteCancel(), CanExecuteCancel);
            Confirm = new RelayCommand(() => ExecuteConfirm(), CanExecuteConfirm);
            Refresh = new RelayCommand(() => ExecuteRefresh());
        }

        public void Initialize(BasicWarehouse warehouse)
        {
            _warehouse = warehouse;
            try
            {
                LocationList = new ObservableCollection<LocationViewModel>();
/*              foreach (var p in _warehouse.DBService.GetPlaceIDs())
                    LocationList.Add(new LocationViewModel { ID = p.ID, Size = p.Size, Blocked = p.Blocked, Reserved = p.Reserved });
                foreach (var l in LocationList)
                    l.Initialize(_warehouse);
*/
                DetailedLocation.Initialize(_warehouse);
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
                return !EditEnabled && (SelectedLocation != null) && AccessLevel%10 >= 1;
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
                DetailedLocation.ID = SelectedLocation.ID;
                DetailedLocation.Size = SelectedLocation.Size;
                DetailedLocation.Blocked = SelectedLocation.Blocked;
                DetailedLocation.Reserved = SelectedLocation.Reserved;
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
            bool rebuildRoute;

            try
            {
                EditEnabled = false;
                EnabledCC = false;
                rebuildRoute = _warehouse.DBService.FindPlaceID(DetailedLocation.ID).Blocked != DetailedLocation.Blocked && DetailedLocation.ID[0] != 'W';

                if (_warehouse.DBService.FindPlaceID(DetailedLocation.ID).Blocked != DetailedLocation.Blocked ||
                    _warehouse.DBService.FindPlaceID(DetailedLocation.ID).Reserved != DetailedLocation.Reserved ||
                    _warehouse.DBService.FindPlaceID(DetailedLocation.ID).Size != DetailedLocation.Size)
                {
                    _warehouse.AddEvent(Event.EnumSeverity.Event, Event.EnumType.Material,
                                         String.Format(":-)"));
                    (_warehouse.WCFClient as WCFUIClient).NotifyUIClient.PlaceIDChanged(DetailedLocation.Place);
                    _warehouse.AddEvent(Event.EnumSeverity.Event, Event.EnumType.Material,
                                         String.Format(":-)"));
                }

                _warehouse.DBService.UpdateLocation(DetailedLocation.Place);
                if (rebuildRoute)
                    (_warehouse.WCFClient as WCFUIClient).NotifyUIClient.RebuildRoutes(false);
                SelectedLocation.Size = DetailedLocation.Size;
                SelectedLocation.Blocked = DetailedLocation.Blocked;
                SelectedLocation.Reserved = DetailedLocation.Reserved;
                _warehouse.AddEvent(Event.EnumSeverity.Event, Event.EnumType.Material, 
                                    String.Format("Location changed: id: {0}, size: {1}, blocked: {2}, reserved: {3}", 
                                                  DetailedLocation.ID, 
                                                  DetailedLocation.Size,
                                                  DetailedLocation.Blocked,
                                                  DetailedLocation.Reserved));
                if(SelectedLocation.ID != DetailedLocation.ID)
                {
                    try
                    {
                        LocationList.Clear();
                        foreach (var p in _warehouse.DBService.GetPlaceIDs())
                            LocationList.Add(new LocationViewModel { ID = p.ID, Size = p.Size, Blocked = p.Blocked, Reserved = p.Reserved });
                        foreach (var l in LocationList)
                            l.Initialize(_warehouse);
                    }
                    catch (Exception e)
                    {
                        _warehouse.AddEvent(Event.EnumSeverity.Error, Event.EnumType.Exception, e.Message);
                    }
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
                return EditEnabled && DetailedLocation.AllPropertiesValid && AccessLevel%10 >= 1;
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
                LocationViewModel sl = SelectedLocation; 
                LocationList.Clear();
                foreach (var p in _warehouse.DBService.GetPlaceIDs())
                    LocationList.Add(new LocationViewModel { ID = p.ID, Size = p.Size, Blocked = p.Blocked, Reserved = p.Reserved });
                foreach (var l in LocationList)
                    l.Initialize(_warehouse);
                if ( sl != null)
                    SelectedLocation = LocationList.FirstOrDefault(p => p.ID == sl.ID);
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
                if (vm is LocationsViewModel)
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