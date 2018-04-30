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
using System.Collections;
using System.Threading.Tasks;

namespace UserInterface.ViewModel
{
    public sealed class SKUIDsViewModel : ViewModelBase
    {
        public enum CommandType { None = 0, Add, Edit};

        #region members
        private CommandType _selectedCommand;
        private ObservableCollection<SKUIDViewModel> _SKUIDList;
        private SKUIDViewModel _selectedSKUID;
        private SKUIDViewModel _detailedSKUID;
        private SKUIDViewModel _manageSKUID;
        private bool _editEnabled;
        private bool _enabledCC;
        private BasicWarehouse _warehouse;
        private DBServiceWMS _dbservicewms;
        private int _accessLevel;
        private string _accessUser;
        private ObservableCollection<TUViewModel> _TUList;
        private int _numberOfSelectedItems;
        #endregion

        #region properites
        public RelayCommand Add { get; private set; }
        public RelayCommand Edit { get; private set; }
        public RelayCommand Confirm { get; private set; }
        public RelayCommand Cancel { get; private set; }
        public RelayCommand Refresh { get; private set; }
        public RelayCommand RefreshTUs { get; private set; }
        public RelayCommand<IList> SelectionChangedCommand { get; private set; }

        public ObservableCollection<SKUIDViewModel> SKUIDList
        {
            get { return _SKUIDList; }
            set
            {
                if (_SKUIDList != value)
                {
                    _SKUIDList = value;
                    RaisePropertyChanged("SKUIDList");
                }
            }
        }

        public ObservableCollection<TUViewModel> TUList
        {
            get { return _TUList; }
            set
            {
                if (_TUList != value)
                {
                    _TUList = value;
                    RaisePropertyChanged("TUList");
                }
            }
        }
        public SKUIDViewModel SelectedSKUID
        {
            get
            {
                return _selectedSKUID;
            }
            set
            {
                if (_selectedSKUID != value)
                {
                    _selectedSKUID = value;
                    RaisePropertyChanged("SelectedSKUID");
                }
            }
        }

        public SKUIDViewModel DetailedSKUID
        {
            get { return _detailedSKUID; }
            set
            {
                if (_detailedSKUID != value)
                {
                    _detailedSKUID = value;
                    RaisePropertyChanged("DetailedSKUID");
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
        public SKUIDsViewModel()
        {
            DetailedSKUID = new SKUIDViewModel();
            TUList = new ObservableCollection<TUViewModel>();
            SelectedSKUID = null;

            EditEnabled = false;
            EnabledCC = false;

            _selectedCommand = CommandType.None;

            Add = new RelayCommand(() => ExecuteAdd(), CanExecuteAdd);
            Edit = new RelayCommand(() => ExecuteEdit(), CanExecuteEdit);
            Cancel = new RelayCommand(() => ExecuteCancel(), CanExecuteCancel);
            Confirm = new RelayCommand(() => ExecuteConfirm(), CanExecuteConfirm);
            Refresh = new RelayCommand(async () => await ExecuteRefresh());
            RefreshTUs = new RelayCommand(async () => await ExecuteRefreshTUs());
            SelectionChangedCommand = new RelayCommand<IList>(items => NumberOfSelectedItems = items == null ? 0 : items.Count);
        }

        public void Initialize(BasicWarehouse warehouse)
        {
            _warehouse = warehouse;
            _dbservicewms = new DBServiceWMS(_warehouse);
            try
            {
                SKUIDList = new ObservableCollection<SKUIDViewModel>();
                DetailedSKUID.Initialize(_warehouse);
                _accessUser = "";
                Messenger.Default.Register<MessageAccessLevel>(this, (mc) => { AccessLevel = mc.AccessLevel; _accessUser = mc.User; });
                Messenger.Default.Register<MessageViewChanged>(this, async vm => await ExecuteViewActivated(vm.ViewModel));
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
                _manageSKUID = new SKUIDViewModel{ID = "", Description = "", DefaultQty = 0,  Unit = "", Weight = 0, FrequencyClass = 0};
                _manageSKUID.Initialize(_warehouse);
                _manageSKUID.AllowChangeIndex = true;
                _manageSKUID.ValidationEnabled = true;
                DetailedSKUID = _manageSKUID;
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
        private void ExecuteEdit()
        {
            try
            {
                EditEnabled = true;
                EnabledCC = true;                
                _selectedCommand = CommandType.Edit;
                _manageSKUID = new SKUIDViewModel {
                    ID = SelectedSKUID.ID,
                    Description = SelectedSKUID.Description,
                    DefaultQty = SelectedSKUID.DefaultQty,
                    Unit = SelectedSKUID.Unit,
                    Weight = SelectedSKUID.Weight,
                    FrequencyClass = SelectedSKUID.FrequencyClass
                };
                _manageSKUID.Initialize(_warehouse);
                _manageSKUID.ValidationEnabled = true;
                DetailedSKUID = _manageSKUID;
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
                return !EditEnabled && (SelectedSKUID != null) && AccessLevel/10 >= 2;
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
                if (DetailedSKUID != null)
                {
                    DetailedSKUID.ValidationEnabled = false;
                }
                DetailedSKUID = SelectedSKUID;
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
                        case CommandType.Add:
                            _dbservicewms.AddSKUID(DetailedSKUID.SKUID);
                            SKUIDList.Add(DetailedSKUID);
                            SelectedSKUID = SKUIDList.FirstOrDefault(p => p.ID == DetailedSKUID.ID);
                            _dbservicewms.AddLog(_accessUser, EnumLogWMS.Event, "UI", $"Add SKUID: {DetailedSKUID.SKUID.ToString()}");
                            break;
                        case CommandType.Edit:
                            _dbservicewms.UpdateSKUID(DetailedSKUID.SKUID);
                            _dbservicewms.AddLog(_accessUser, EnumLogWMS.Event, "UI", $"Edit SKUID: {DetailedSKUID.SKUID.ToString()}");
                            SelectedSKUID.ID = DetailedSKUID.ID;
                            SelectedSKUID.Description = DetailedSKUID.Description;
                            SelectedSKUID.DefaultQty = DetailedSKUID.DefaultQty;
                            SelectedSKUID.Unit = DetailedSKUID.Unit;
                            SelectedSKUID.Weight = DetailedSKUID.Weight;
                            SelectedSKUID.FrequencyClass = DetailedSKUID.FrequencyClass;
                            break;
                        default:
                            break;
                    }
                    if (DetailedSKUID != null)
                        DetailedSKUID.ValidationEnabled = false;
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
                return EditEnabled && DetailedSKUID.AllPropertiesValid && AccessLevel >= 1;
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
                SKUIDViewModel sl = SelectedSKUID; 
                SKUIDList.Clear();
                var skuids = await _dbservicewms.GetSKUIDs();
                foreach (var p in skuids)
                    SKUIDList.Add(new SKUIDViewModel { ID = p.ID, Description = p.Description, DefaultQty = p.DefaultQty, Unit = p.Unit, Weight = p.Weight, FrequencyClass = p.FrequencyClass });
                foreach (var l in SKUIDList)
                    l.Initialize(_warehouse);
                SelectedSKUID = SKUIDList.FirstOrDefault(p => p.ID == sl.ID);
            }
            catch (Exception e)
            {
                _warehouse.AddEvent(Database.Event.EnumSeverity.Error, Database.Event.EnumType.Exception, 
                                    string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
            }
        }
        private async Task ExecuteRefreshTUs()
        {
            try
            {
                TUList.Clear();
                if (SelectedSKUID != null)
                {
                    var sdl = await _dbservicewms.GetAvailableTUs(SelectedSKUID.ID);
                    foreach (var s in sdl)
                        TUList.Add(new TUViewModel { TUID = s.TU_ID, SKUID = s.SKU_ID, Batch = s.Batch, Qty = s.Qty, ProdDate = s.ProdDate, ExpDate = s.ExpDate });
                }
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
                if (vm is SKUIDsViewModel)
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
