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
using UserInterface.ProxyWMS_UI;

namespace UserInterface.ViewModel
{
    public sealed class PlaceTUIDsViewModel : ViewModelBase
    {
        public enum CommandType { None = 0, Edit, Book, Delete, Add, Block};

        #region members
        private CommandType _selectedCommand;
        private ObservableCollection<PlaceTUIDViewModel> _dataList;
        private PlaceTUIDViewModel _selected;
        private PlaceTUIDViewModel _detailed;
        private bool _editEnabled;
        private bool _enabledCC;
        private BasicWarehouse _warehouse;
        private DBServiceWMS _dbservicewms;
        private int _accessLevel;
        private string _accessUser;
        private bool _excludeWout = true;
        #endregion

        #region properites
        public RelayCommand Add { get; private set; }
        public RelayCommand Block { get; private set; }
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
                    if(_detailed != null && _detailed.DetailList !=null)
                        _detailed.PopulateList();
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
        public bool ExcludeWout
        {
            get
            {
                return _excludeWout;
            }
            set
            {
                if (_excludeWout != value)
                {
                    _excludeWout = value;
                    RaisePropertyChanged("ExcludeWout");
                }
            }
        }
        #endregion

        #region initialization
        public PlaceTUIDsViewModel()
        {
            Detailed = null;
            Selected = null;

            EditEnabled = false;
            EnabledCC = false;

            _selectedCommand = CommandType.None;

            Edit = new RelayCommand(() => ExecuteEdit(), CanExecuteEdit);
            Book = new RelayCommand(() => ExecuteBook(), CanExecuteBook);
            Delete = new RelayCommand(() => ExecuteDelete(), CanExecuteDelete);
            Add = new RelayCommand(() => ExecuteAdd(), CanExecuteAdd);
            Block = new RelayCommand(() => ExecuteBlock(), CanExecuteBlock);
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
                _accessUser = "";
                Messenger.Default.Register<MessageAccessLevel>(this, (mc) => { AccessLevel = mc.AccessLevel; _accessUser = mc.User; });
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
                Detailed = new PlaceTUIDViewModel();
                Detailed.Initialize(_warehouse);
                Detailed.AllowPlaceChange = false;
                Detailed.AllowTUIDChange = false;
                Detailed.AllowFieldChange = true;
                Detailed.AllowBlockedChange = false;
                Detailed.TUID = Selected.TUID;
                Detailed.PlaceID = Selected.PlaceID;
                Detailed.DimensionClass = Selected.DimensionClass;
                Detailed.Blocked = Selected.Blocked;
                Detailed.DetailList = new ObservableCollection<TUSKUIDViewModel>();
                foreach (var l in Selected.DetailList)
                    Detailed.DetailList.Add(l);
                Detailed.ValidationEnabled = true;
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
                return !EditEnabled && (Selected != null) && AccessLevel/10 >= 2;
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
                Detailed = new PlaceTUIDViewModel();
                Detailed.Initialize(_warehouse);
                Detailed.AllowTUIDChange = false;
                Detailed.AllowPlaceChange = true;
                Detailed.AllowFieldChange = false;
                Detailed.AllowBlockedChange = false;
                Detailed.TUID = Selected.TUID;
                Detailed.ValidationEnabled = true;
                Detailed.PlaceID = Selected.PlaceID;
                Detailed.DimensionClass = Selected.DimensionClass;
                Detailed.Blocked = Selected.Blocked;
                Detailed.DetailList = new ObservableCollection<TUSKUIDViewModel>();
                foreach (var l in Selected.DetailList)
                {
                    Detailed.DetailList.Add(l);
                    l.Initialize(_warehouse);
                }
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
                return !EditEnabled && (Selected != null) && AccessLevel/10 >= 2;
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
                EditEnabled = true;
                EnabledCC = true;
                Detailed = new PlaceTUIDViewModel();
                Detailed.Initialize(_warehouse);
                Detailed.ValidationEnabled = false;
                Detailed.AllowTUIDChange = false;
                Detailed.AllowPlaceChange = false;
                Detailed.AllowFieldChange = false;
                Detailed.AllowBlockedChange = false;
                Detailed.TUID = Selected.TUID;
                Detailed.PlaceID = Selected.PlaceID;
                Detailed.DimensionClass = Selected.DimensionClass;
                Detailed.Blocked = Selected.Blocked;
                Detailed.DetailList = new ObservableCollection<TUSKUIDViewModel>();
                foreach (var l in Selected.DetailList)
                {
                    Detailed.DetailList.Add(l);
                    l.Initialize(_warehouse);
                }
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
                return !EditEnabled && (Selected != null) && AccessLevel/10 >= 2;
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
                Detailed = new PlaceTUIDViewModel();
                Detailed.Initialize(_warehouse);
                Detailed.AllowTUIDChange = true;
                Detailed.AllowPlaceChange = true;
                Detailed.AllowFieldChange = true;
                Detailed.AllowBlockedChange = false;
                Detailed.ValidationEnabled = true;
                Detailed.TUID = 0;
                Detailed.PlaceID = "";
                Detailed.DimensionClass = 0;
                Detailed.Blocked = 0;
                Detailed.DetailList = new ObservableCollection<TUSKUIDViewModel>();
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
                return !EditEnabled && AccessLevel/10 >= 2;
            }
            catch (Exception e)
            {
                _warehouse.AddEvent(Database.Event.EnumSeverity.Error, Database.Event.EnumType.Exception,
                                    string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
                return false;
            }
        }
        private void ExecuteBlock()
        {
            try
            {
                _selectedCommand = CommandType.Block;
                EditEnabled = true;
                EnabledCC = true;
                Detailed = new PlaceTUIDViewModel();
                Detailed.Initialize(_warehouse);
                Detailed.AllowPlaceChange = false;
                Detailed.AllowTUIDChange = false;
                Detailed.AllowFieldChange = false;
                Detailed.AllowBlockedChange = true;
                Detailed.TUID = Selected.TUID;
                Detailed.PlaceID = Selected.PlaceID;
                Detailed.DimensionClass = Selected.DimensionClass;
                Detailed.Blocked = Selected.Blocked;
                Detailed.DetailList = new ObservableCollection<TUSKUIDViewModel>();
                foreach (var l in Selected.DetailList)
                    Detailed.DetailList.Add(l);
                Detailed.ValidationEnabled = true;
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
                return !EditEnabled && (Selected != null) && AccessLevel/10 >= 1;
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
                        case CommandType.Edit:
                            TU_ID tuid = new TU_ID { ID = Detailed.TUID, DimensionClass = Detailed.DimensionClass, Blocked = (int)Detailed.Blocked };
                            List<TUs> tulist = new List<TUs>();
                            foreach (var l in Detailed.DetailList)
                                tulist.Add(new TUs { TU_ID = Detailed.TUID, SKU_ID = l.SKUID, Qty = l.Qty, Batch = l.Batch, ProdDate = l.ProdDate, ExpDate = l.ExpDate  });
                            _dbservicewms.UpdateTUID(tuid);
                            _dbservicewms.UpdateTUs(tuid, tulist);
                            _dbservicewms.AddLog(_accessUser, EnumLogWMS.Event, "UI", $"Edit TUID: {tuid.ToString()}");
                            Selected.TUID = Detailed.TUID;
                            Selected.PlaceID = Detailed.PlaceID;
                            Selected.DimensionClass = Detailed.DimensionClass;
                            Selected.Blocked = Detailed.Blocked;
                            break;
                        case CommandType.Book:
                            using (WMSToUIClient client = new WMSToUIClient())
                            {
                                var pd = new ProxyWMS_UI.PlaceDiff[] { new ProxyWMS_UI.PlaceDiff { TUID = Detailed.TUID, PlaceWMS = Selected.PlaceID, PlaceMFCS = Detailed.PlaceID } };
                                client.UpdatePlace(pd, _accessUser);
                                _dbservicewms.UpdatePlaceMFCS(new Place {Material = Detailed.TUID, Place1 = Detailed.PlaceID });
                            }
                            _dbservicewms.AddLog(_accessUser, EnumLogWMS.Event, "UI", $"Rebook TUID: {Detailed.TUID} {Detailed.PlaceID}");
                            Selected.TUID = Detailed.TUID;
                            Selected.PlaceID = Detailed.PlaceID;
                            Selected.DimensionClass = Detailed.DimensionClass;
                            Selected.Blocked = Detailed.Blocked;
                            break;
                        case CommandType.Block:
                            using (WMSToUIClient client = new WMSToUIClient())
                            {
                                client.BlockTU(Detailed.TUID, Detailed.BlockedQC, (int)EnumBlockedWMS.Quality);
                                _dbservicewms.AddLog(_accessUser, EnumLogWMS.Event, "UI", $"Request block TUID: |{Detailed.TUID:d9}|{Detailed.BlockedQC}|{(int)EnumBlockedWMS.Quality}|");
                                Selected.TUID = Detailed.TUID;
                                Selected.PlaceID = Detailed.PlaceID;
                                Selected.DimensionClass = Detailed.DimensionClass;
                                Selected.Blocked = Detailed.Blocked;
                            }
                            break;
                        case CommandType.Delete:
                            _dbservicewms.DeleteTUs(Detailed.TUID);
                            using (WMSToUIClient client = new WMSToUIClient())
                            {
                                var pd = new ProxyWMS_UI.PlaceDiff[] { new ProxyWMS_UI.PlaceDiff { TUID = Detailed.TUID, PlaceWMS = Detailed.PlaceID, PlaceMFCS = null } };
                                client.UpdatePlace(pd, _accessUser);
                                _dbservicewms.UpdatePlaceMFCS(new Place { Material = Detailed.TUID, Place1 = Detailed.PlaceID});
                            }
                            _dbservicewms.DeletePlaceMFCS(new Place { Material = Detailed.TUID, Place1 = Detailed.PlaceID } );
                            _dbservicewms.AddLog(_accessUser, EnumLogWMS.Event, "UI", $"Delete Place: {Detailed.TUID}, {Detailed.PlaceID}");
                            Selected.TUID = Detailed.TUID;
                            Selected.PlaceID = Detailed.PlaceID;
                            Selected.DimensionClass = Detailed.DimensionClass;
                            Selected.Blocked = Detailed.Blocked;
                            break;
                        case CommandType.Add:
                            List<TUs> tul = new List<TUs>();
                            foreach (var l in Detailed.DetailList)
                                tul.Add(new TUs { TU_ID = Detailed.TUID, SKU_ID = l.SKUID, Qty = l.Qty, Batch = l.Batch, ProdDate = l.ProdDate, ExpDate = l.ExpDate });
                            using (WMSToUIClient client = new WMSToUIClient())
                            {
                                var pd = new ProxyWMS_UI.PlaceDiff[] { new ProxyWMS_UI.PlaceDiff { TUID = Detailed.TUID, PlaceWMS = null, PlaceMFCS = Detailed.PlaceID } };
                                client.UpdatePlace(pd, _accessUser);
                                _dbservicewms.UpdatePlaceMFCS(new Place { Material = Detailed.TUID, Place1 = Detailed.PlaceID });
                            }
                            _dbservicewms.AddTUs(tul);
                            _dbservicewms.AddLog(_accessUser, EnumLogWMS.Event, "UI", $"Add Place: {Detailed.TUID}, {Detailed.PlaceID}");
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
                int? tuid = Selected?.TUID; 
                DataList.Clear();
                foreach (var p in _dbservicewms.GetPlaceTUIDs(ExcludeWout))
                    DataList.Add(new PlaceTUIDViewModel
                    {
                        TUID = p.TUID,
                        PlaceID = p.PlaceID,
                        DimensionClass = p.DimensionClass,
                        Blocked = (EnumBlockedWMS)p.Blocked,
                        TimeStamp = p.TimeStamp
                    });
                foreach (var l in DataList)
                    l.Initialize(_warehouse);
                if (tuid != null)
                {
                    Selected = DataList.FirstOrDefault(p => p.TUID == tuid);
                    Detailed = Selected;
                }
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
                if (vm is PlaceTUIDsViewModel)
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
