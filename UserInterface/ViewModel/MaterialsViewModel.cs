using System;
using System.Collections.Generic;
using System.Linq;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.CommandWpf;
using System.Collections.ObjectModel;
using UserInterface.Services;
using Warehouse.Model;
using System.Diagnostics;
using WCFClients;
using Database;
using GalaSoft.MvvmLight.Messaging;
using UserInterface.Messages;
using System.Collections;

namespace UserInterface.ViewModel
{
    public sealed class MaterialsViewModel: ViewModelBase
    {
        enum CommandType { None, Delete, Create, Move };

        #region members
        private BasicWarehouse _warehouse;
        List<String> _devices;
        private ObservableCollection<MaterialViewModel> _placeList;
        private MaterialViewModel _selectedPlace;
        private MaterialViewModel _detailedPlace;
        private bool _editEnabled;
        private bool _enabledCC;
        CommandType _cmdtype;
        private int _accessLevel;
        private int _numberOfSelectedItems;
        #endregion

        #region properties
        public RelayCommand Refresh { get; private set; }
        public RelayCommand Delete { get; private set; }
        public RelayCommand Create { get; private set; }
        public RelayCommand Move { get; private set; }
        public RelayCommand Cancel { get; private set; }
        public RelayCommand Confirm { get; private set; }
        public RelayCommand<IList> SelectionChangedCommand { get; private set; }

        public ObservableCollection<MaterialViewModel> PlaceList
        {
            get { return _placeList; }
            set
            {
                if (_placeList != value)
                {
                    _placeList = value;
                    RaisePropertyChanged("PlaceList");
                }
            }
        }

        public MaterialViewModel SelectedPlace
        {
            get { return _selectedPlace; }
            set
            {
                if( _selectedPlace != value)
                {
                    _selectedPlace = value;
                    RaisePropertyChanged("SelectedPlace");
                    try
                    {
                        if (_selectedPlace != null)
                        {
                            DetailedPlace.ID = _selectedPlace.ID;
                            DetailedPlace.Location = _selectedPlace.Location;
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
        public MaterialViewModel DetailedPlace
        {
            get { return _detailedPlace; }
            set
            {
                if (_detailedPlace != value)
                {
                    _detailedPlace = value;
                    RaisePropertyChanged("DetailedPlace");
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

        #region intialization    
        public MaterialsViewModel()
        {
            _editEnabled = false;
            _enabledCC = false;
            _cmdtype = CommandType.None;
            DetailedPlace = new MaterialViewModel();
        }
        public void Initialize(BasicWarehouse warehouse)
        {
            _warehouse = warehouse;
            try
            {
                _devices = _warehouse.ConveyorList.ConvertAll(n => n.Name);
                _devices.AddRange(_warehouse.CraneList.ConvertAll(n => n.Name));
                _devices.Sort();

                Refresh = new RelayCommand(() => ExecuteRefresh(), CanExecuteRefresh);
                Delete = new RelayCommand(() => ExecuteDelete(), CanExecuteDelete);
                Create = new RelayCommand(() => ExecuteCreate(), CanExecuteCreate);
                Move = new RelayCommand(() => ExecuteMove(), CanExecuteMove);
                Cancel = new RelayCommand(() => ExecuteCancel(), CanExecuteCancel);
                Confirm = new RelayCommand(() => ExecuteConfirm(), CanExecuteConfirm);
                SelectionChangedCommand = new RelayCommand<IList>(
                    items =>
                    {
                        if (items == null)
                        {
                            NumberOfSelectedItems = 0;
                            return;
                        }

                        NumberOfSelectedItems = items.Count;
                    });

                PlaceList = new ObservableCollection<MaterialViewModel>();
/*                foreach (var p in _warehouse.DBService.GetPlaces())
                    PlaceList.Add(new MaterialViewModel { Location = p.Place1, ID = p.Material });
                foreach (var mvm in PlaceList)
                    mvm.Initialize(_warehouse);
*/
                DetailedPlace.Initialize(_warehouse);
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
        private void ExecuteRefresh()
        {
            try
            {
                MaterialViewModel c = SelectedPlace;
                PlaceList.Clear();
                foreach (var p in _warehouse.DBService.GetPlaces())
                    PlaceList.Add(new MaterialViewModel { Location = p.Place1, ID = p.Material });
                foreach (var mvm in PlaceList)
                    mvm.Initialize(_warehouse);
//                RaisePropertyChanged("PlaceList");
                if( c != null)
                    SelectedPlace = PlaceList.FirstOrDefault(p => p.ID == c.ID);
            }
            catch (Exception e)
            {
                _warehouse.AddEvent(Database.Event.EnumSeverity.Error, Database.Event.EnumType.Exception, 
                                    string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
            }
        }

        private bool CanExecuteRefresh()
        {
            try
            {
                return !EnabledCC;
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
                _cmdtype = CommandType.Delete;
                DetailedPlace.EnabledLocation = false;
                DetailedPlace.EnabledMaterial = false;
                EditEnabled = true;
                EnabledCC = true;
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
                if (SelectedPlace != null)
                    return !EnabledCC && (SelectedPlace.ID > 0 || _devices.Any(p => p == SelectedPlace.Location)) && AccessLevel >= 1;
                return false;
            }
            catch (Exception e)
            {
                _warehouse.AddEvent(Database.Event.EnumSeverity.Error, Database.Event.EnumType.Exception,
                                    string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
                return false;
            }
        }
        private void ExecuteCreate()
        {
            try
            {
                _cmdtype = CommandType.Create;
                DetailedPlace.EnabledLocation = true;
                DetailedPlace.EnabledMaterial = true;
                EditEnabled = true;
                EnabledCC = true;
                DetailedPlace.ID = 0;
                DetailedPlace.Location = "";
            }
            catch (Exception e)
            {
                _warehouse.AddEvent(Database.Event.EnumSeverity.Error, Database.Event.EnumType.Exception,
                                    string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
            }
        }
        private bool CanExecuteCreate()
        {
            try
            {
                return !EnabledCC && AccessLevel >= 1;
            }
            catch (Exception e)
            {
                _warehouse.AddEvent(Database.Event.EnumSeverity.Error, Database.Event.EnumType.Exception,
                                    string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
                return false;
            }
        }
        private void ExecuteMove()
        {
            try
            {
                _cmdtype = CommandType.Move;
                DetailedPlace.EnabledLocation = true;
                DetailedPlace.EnabledMaterial = false;
                EditEnabled = true;
                EnabledCC = true;
            }
            catch (Exception e)
            {
                _warehouse.AddEvent(Database.Event.EnumSeverity.Error, Database.Event.EnumType.Exception,
                                    string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
            }
        }
        private bool CanExecuteMove()
        {
            try
            {
                return !EnabledCC && (SelectedPlace != null) && AccessLevel >= 1;
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
                _cmdtype = CommandType.None;
                DetailedPlace.EnabledLocation = false;
                DetailedPlace.EnabledMaterial = false;
                DetailedPlace.ID = 1;                   // dirty trick: force valid value to remove red square
                DetailedPlace.Location = "T001";        // dirty trick: force valid value to remove red square
                if (SelectedPlace != null)
                {
                    DetailedPlace.Location = SelectedPlace.Location;
                    DetailedPlace.ID = SelectedPlace.ID;
                }
                else
                {
                    DetailedPlace.Location = "";
                    DetailedPlace.ID = 0;
                }
                EditEnabled = false;
                EnabledCC = false;
            }
            catch (Exception e)
            {
                _warehouse.AddEvent(Database.Event.EnumSeverity.Error, Database.Event.EnumType.Exception,
                                    string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
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
                switch (_cmdtype)
                {
                    case CommandType.Delete:
                        _warehouse.DBService.AddCommand(new CommandMaterial {
                            Task = Command.EnumCommandTask.DeleteMaterial,
                            Material = DetailedPlace.ID,
                            Source = DetailedPlace.Location,
                            Target = DetailedPlace.Location,
                            Status = Command.EnumCommandStatus.NotActive,
                            Time = DateTime.Now
                        });
                        break;
                    case CommandType.Create:
                        _warehouse.DBService.FindMaterialID(DetailedPlace.ID, true);
                        _warehouse.DBService.AddCommand(new CommandMaterial
                        {
                            Task = Command.EnumCommandTask.CreateMaterial,
                            Material = DetailedPlace.ID,
                            Source = DetailedPlace.Location,
                            Target = DetailedPlace.Location,
                            Status = Command.EnumCommandStatus.NotActive,
                            Time = DateTime.Now
                        });
                        break;
                    case CommandType.Move:
                        _warehouse.DBService.AddCommand(new CommandMaterial
                        {
                            Task = Command.EnumCommandTask.DeleteMaterial,
                            Material = DetailedPlace.ID,
                            Source = _warehouse.DBService.FindMaterial(DetailedPlace.ID).Place1,
                            Target = _warehouse.DBService.FindMaterial(DetailedPlace.ID).Place1,
                            Status = Command.EnumCommandStatus.NotActive,
                            Time = DateTime.Now
                        });
                        _warehouse.DBService.AddCommand(new CommandMaterial
                        {
                            Task = Command.EnumCommandTask.CreateMaterial,
                            Material = DetailedPlace.ID,
                            Source = DetailedPlace.Location,
                            Target = DetailedPlace.Location,
                            Status = Command.EnumCommandStatus.NotActive,
                            Time = DateTime.Now
                        });
                        break;
                }
                _cmdtype = CommandType.None;
                DetailedPlace.EnabledLocation = false;
                DetailedPlace.EnabledMaterial = false;
                EditEnabled = false;
                EnabledCC = false;
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
                return EnabledCC && DetailedPlace.AllPropertiesValid && AccessLevel >= 1;
            }
            catch (Exception e)
            {
                _warehouse.AddEvent(Database.Event.EnumSeverity.Error, Database.Event.EnumType.Exception,
                                    string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
                return false;
            }
        }
        #endregion
        public void ExecuteViewActivated(ViewModelBase vm)
        {
            try
            {
                if (vm is MaterialsViewModel)
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
