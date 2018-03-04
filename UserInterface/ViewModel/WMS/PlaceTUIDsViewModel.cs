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
using DatabaseWMS;

namespace UserInterface.ViewModel
{
    public sealed class PlaceTUIDsViewModel : ViewModelBase
    {
        public enum CommandType { None = 0, Edit, Book, Delete, Add};

        #region members
        private CommandType _selectedCommand;
        private ObservableCollection<PlaceTUIDViewModel> _dataList;
        private PlaceTUIDViewModel _selected;
        private PlaceTUIDViewModel _detailed;
        private PlaceTUIDViewModel _managed;
        private bool _editEnabled;
        private bool _enabledCC;
        private BasicWarehouse _warehouse;
        private DBServiceWMS _dbservicewms;
        private int _accessLevel;
        #endregion

        #region properites
        public RelayCommand Add { get; private set; }
        public RelayCommand Delete { get; private set; }
        public RelayCommand Book { get; private set; }
        public RelayCommand Edit { get; private set; }
        public RelayCommand Confirm { get; private set; }
        public RelayCommand Cancel { get; private set; }
        public RelayCommand Refresh { get; private set; }

        public ObservableCollection<PlaceTUIDViewModel> DataList
        {
            get { return _dataList; }
            set
            {
                if (_dataList != value)
                {
                    _dataList = value;
                    RaisePropertyChanged("Records");
                }
            }
        }

        public PlaceTUIDViewModel Selected
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
                    try
                    {
                        if (_selected != null)
                            Detailed = Selected;
                    }
                    catch (Exception e)
                    {
                        _warehouse.AddEvent(Database.Event.EnumSeverity.Error, Database.Event.EnumType.Exception, e.Message);
                        throw new Exception(string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
                    }
                }
            }
        }

        public PlaceTUIDViewModel Detailed
        {
            get { return _detailed; }
            set
            {
                if (_detailed != value)
                {
                    _detailed = value;
                    RaisePropertyChanged("Detailed");
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
        public PlaceTUIDsViewModel()
        {
            Detailed = null;
            Selected = null;
            _managed = new PlaceTUIDViewModel();
            _managed.Initialize(_warehouse);

            EditEnabled = false;
            EnabledCC = false;

            _selectedCommand = CommandType.None;

            Edit = new RelayCommand(() => ExecuteEdit(), CanExecuteEdit);
            Book = new RelayCommand(() => ExecuteBook(), CanExecuteBook);
            Delete = new RelayCommand(() => ExecuteDelete(), CanExecuteDelete);
            Add = new RelayCommand(() => ExecuteAdd(), CanExecuteAdd);
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
                DataList = new ObservableCollection<PlaceTUIDViewModel>();
                foreach (var p in _dbservicewms.GetPlaceTUIDs())
                    DataList.Add(new PlaceTUIDViewModel
                    {
                        TUID = p.TUID,
                        PlaceID = p.PlaceID,
                        DimensionClass = p.DimensionClass,
                        Blocked = p.Blocked
                    });
                foreach (var l in DataList)
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
                _selectedCommand = CommandType.Edit;
                EditEnabled = true;
                EnabledCC = true;                
                _managed.TUID = Selected.TUID;
                _managed.PlaceID = Selected.PlaceID;
                _managed.DimensionClass = Selected.DimensionClass;
                _managed.Blocked = Selected.Blocked;
                _managed.DetailList = new ObservableCollection<TUSKUIDViewModel>();
                foreach (var l in Selected.DetailList)
                    _managed.DetailList.Add(l);
                _managed.AllowPlaceChange = false;
                _managed.AllowTUIDChange = false;
                _managed.AllowFieldChange = false;
                _managed.ValidationEnabled = true;
                Detailed = _managed;
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
                return !EditEnabled && (Selected != null) && AccessLevel >= 1;
            }
            catch (Exception e)
            {
                _warehouse.AddEvent(Database.Event.EnumSeverity.Error, Database.Event.EnumType.Exception, 
                                    string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
                return false;
            }
        }
        private void ExecuteBook()
        {
            try
            {
                _selectedCommand = CommandType.Book;
                EditEnabled = true;
                EnabledCC = true;
                _managed.TUID = Selected.TUID;
                _managed.PlaceID = Selected.PlaceID;
                _managed.DimensionClass = Selected.DimensionClass;
                _managed.Blocked = Selected.Blocked;
                _managed.DetailList = new ObservableCollection<TUSKUIDViewModel>();
                foreach (var l in Selected.DetailList)
                {
                    _managed.DetailList.Add(l);
                    l.Initialize(_warehouse);
                }
                _managed.ValidationEnabled = true;
                _managed.AllowTUIDChange = false;
                _managed.AllowPlaceChange = true;
                _managed.AllowFieldChange = true;
                Detailed = _managed;
            }
            catch (Exception e)
            {
                _warehouse.AddEvent(Database.Event.EnumSeverity.Error, Database.Event.EnumType.Exception,
                                    string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
            }
        }

        private bool CanExecuteBook()
        {
            try
            {
                return !EditEnabled && (Selected != null) && AccessLevel >= 1;
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
                _managed.TUID = Selected.TUID;
                _managed.PlaceID = Selected.PlaceID;
                _managed.DimensionClass = Selected.DimensionClass;
                _managed.Blocked = Selected.Blocked;
                _managed.DetailList = new ObservableCollection<TUSKUIDViewModel>();
                foreach (var l in Selected.DetailList)
                {
                    _managed.DetailList.Add(l);
                    l.Initialize(_warehouse);
                }
                _managed.ValidationEnabled = true;
                _managed.AllowTUIDChange = false;
                _managed.AllowPlaceChange = false;
                _managed.AllowFieldChange = false;
                Detailed = _managed;
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
                return !EditEnabled && (Selected != null) && AccessLevel >= 1;
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
                _managed.TUID = 0;
                _managed.PlaceID = "";
                _managed.DimensionClass = 0;
                _managed.Blocked = 0;
                _managed.DetailList = new ObservableCollection<TUSKUIDViewModel>();
                _managed.AllowTUIDChange = true;
                _managed.AllowPlaceChange = true;
                _managed.AllowFieldChange = true;
                _managed.ValidationEnabled = true;
                Detailed = _managed;
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
                return !EditEnabled && AccessLevel >= 1;
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
                if (Detailed != null)
                {
                    Detailed.ValidationEnabled = false;
                }
                Detailed = Selected;
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
                        case CommandType.Edit:
                            TU_ID tuid = new TU_ID { ID = Detailed.TUID, DimensionClass = Detailed.DimensionClass, Blocked = Detailed.Blocked };
                            List<TUs> tulist = new List<TUs>();
                            foreach (var l in Detailed.DetailList)
                                tulist.Add(new TUs { TU_ID = Detailed.TUID, SKU_ID = l.SKUID, Qty = l.Qty, Batch = l.Batch, ProdDate = l.ProdDate, ExpDate = l.ExpDate  });
                            _dbservicewms.UpdateTUID(tuid);
                            _dbservicewms.UpdateTUs(tuid, tulist);
                            _warehouse.AddEvent(Event.EnumSeverity.Event, Event.EnumType.Material,
                                                String.Format("TUID changed: tuid: {0}", Detailed.TUID));
                            Selected.TUID = Detailed.TUID;
                            Selected.PlaceID = Detailed.PlaceID;
                            Selected.DimensionClass = Detailed.DimensionClass;
                            Selected.Blocked = Detailed.Blocked;
                            break;
                        case CommandType.Book:
                            _dbservicewms.UpdatePlace(new Places { TU_ID = Detailed.TUID, PlaceID = Detailed.PlaceID });
                            _warehouse.AddEvent(Event.EnumSeverity.Event, Event.EnumType.Material,
                                                String.Format("Place changed: tuid: {0}", Detailed.TUID));
                            Selected.TUID = Detailed.TUID;
                            Selected.PlaceID = Detailed.PlaceID;
                            Selected.DimensionClass = Detailed.DimensionClass;
                            Selected.Blocked = Detailed.Blocked;
                            break;
                        case CommandType.Delete:
                            _dbservicewms.DeleteTUs(Detailed.TUID);
                            _dbservicewms.DeletePlace(new Places { TU_ID = Detailed.TUID, PlaceID = Detailed.PlaceID });
                            _warehouse.AddEvent(Event.EnumSeverity.Event, Event.EnumType.Material,
                                                String.Format("Place deleted: tuid: {0}", Detailed.TUID));
                            Selected.TUID = Detailed.TUID;
                            Selected.PlaceID = Detailed.PlaceID;
                            Selected.DimensionClass = Detailed.DimensionClass;
                            Selected.Blocked = Detailed.Blocked;
                            break;
                        case CommandType.Add:
                            List<TUs> tul = new List<TUs>();
                            foreach (var l in Detailed.DetailList)
                                tul.Add(new TUs { TU_ID = Detailed.TUID, SKU_ID = l.SKUID, Qty = l.Qty, Batch = l.Batch, ProdDate = l.ProdDate, ExpDate = l.ExpDate });
                            _dbservicewms.AddTUs(tul);
                            _dbservicewms.AddPlace(new Places { TU_ID = Detailed.TUID, PlaceID = Detailed.PlaceID });
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
                return (EditEnabled && Detailed.AllPropertiesValid && AccessLevel >= 1) || _selectedCommand == CommandType.Delete;
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
                PlaceTUIDViewModel sl = Selected; 
                DataList.Clear();
                foreach (var p in _dbservicewms.GetPlaceTUIDs())
                    DataList.Add(new PlaceTUIDViewModel
                    {
                        TUID = p.TUID,
                        PlaceID = p.PlaceID,
                        DimensionClass = p.DimensionClass,
                        Blocked = p.Blocked
                    });
                foreach (var l in DataList)
                    l.Initialize(_warehouse);
                if ( sl != null)
                    Selected = DataList.FirstOrDefault(p => p.TUID == sl.TUID);
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
