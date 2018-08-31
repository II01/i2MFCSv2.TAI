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
using System.Threading.Tasks;

namespace UserInterface.ViewModel
{
    public sealed class PlaceDiffsViewModel : ViewModelBase
    {
        public enum CommandType { None = 0, UpdateWMS, UpdateMFCS};

        #region members
        private CommandType _selectedCommand;
        private ObservableCollection<PlaceDiffViewModel> _dataList;
        private PlaceDiffViewModel _selected;
        private bool _editEnabled;
        private bool _enabledCC;
        private BasicWarehouse _warehouse;
        private DBServiceWMS _dbservicewms;
        private int _accessLevel;
        private string _accessUser;
        #endregion

        #region properites
        public RelayCommand UpdateMFCS { get; private set; }
        public RelayCommand UpdateWMS { get; private set; }
        public RelayCommand Confirm { get; private set; }
        public RelayCommand Cancel { get; private set; }
        public RelayCommand Refresh { get; private set; }

        public ObservableCollection<PlaceDiffViewModel> DataList
        {
            get { return _dataList; }
            set
            {
                if (_dataList != value)
                {
                    _dataList = value;
                    RaisePropertyChanged("DataList");
                }
            }
        }

        public PlaceDiffViewModel Selected
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
        public PlaceDiffsViewModel()
        {
            Selected = null;

            EditEnabled = false;
            EnabledCC = false;

            _selectedCommand = CommandType.None;

            UpdateMFCS = new RelayCommand(() => ExecuteUpdateMFCS(), CanExecuteUpdateMFCS);
            UpdateWMS = new RelayCommand(() => ExecuteUpdateWMS(), CanExecuteUpdateWMS);
            Cancel = new RelayCommand(() => ExecuteCancel(), CanExecuteCancel);
            Confirm = new RelayCommand(() => ExecuteConfirm(), CanExecuteConfirm);
            Refresh = new RelayCommand(async () => await ExecuteRefresh());
        }

        public void Initialize(BasicWarehouse warehouse)
        {
            _warehouse = warehouse;
            _dbservicewms = new DBServiceWMS(_warehouse);
            try
            {
                DataList = new ObservableCollection<PlaceDiffViewModel>();
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

        private void ExecuteUpdateMFCS()
        {
            try
            {
                EditEnabled = true;
                EnabledCC = true;
                _selectedCommand = CommandType.UpdateMFCS;
            }
            catch (Exception e)
            {
                _warehouse.AddEvent(Database.Event.EnumSeverity.Error, Database.Event.EnumType.Exception,
                                    string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
            }
        }

        private bool CanExecuteUpdateMFCS()
        {
            try
            {
                return !EditEnabled && AccessLevel%10 >= 2;
            }
            catch (Exception e)
            {
                _warehouse.AddEvent(Database.Event.EnumSeverity.Error, Database.Event.EnumType.Exception,
                                    string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
                return false;
            }

        }
        private void ExecuteUpdateWMS()
        {
            try
            {
                EditEnabled = true;
                EnabledCC = true;
                _selectedCommand = CommandType.UpdateWMS;
            }
            catch (Exception e)
            {
                _warehouse.AddEvent(Database.Event.EnumSeverity.Error, Database.Event.EnumType.Exception,
                                    string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
            }
        }

        private bool CanExecuteUpdateWMS()
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
        private void ExecuteCancel()
        {
            try
            {
                EditEnabled = false;
                EnabledCC = false;
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
                List<DataServiceWMS.PlaceDiff> pd = new List<DataServiceWMS.PlaceDiff>();
                List<ProxyWMS_UI.PlaceDiff> pdproxy = new List<ProxyWMS_UI.PlaceDiff>();
                try
                {
                    switch (_selectedCommand)
                    {
                        case CommandType.UpdateMFCS:
                            foreach (var d in DataList)
                                pd.Add(new DataServiceWMS.PlaceDiff {
                                    TUID = d.TUID,
                                    PlaceMFCS = d.PlaceMFCS,
                                    PlaceWMS = d.PlaceWMS,
                                    DimensionMFCS = d.DimensionMFCS,
                                    DimensionWMS = d.DimensionWMS,
                                    TimeMFCS = d.TimeMFCS,
                                    TimeWMS = d.TimeWMS });
                            _dbservicewms.UpdatePlacesMFCS(pd, _accessUser);
                            break;
                        case CommandType.UpdateWMS:
                            foreach (var d in DataList) 
                                pdproxy.Add(new ProxyWMS_UI.PlaceDiff {
                                    TUID = d.TUID,
                                    PlaceMFCS = d.PlaceMFCS,
                                    PlaceWMS = d.PlaceWMS,
                                    DimensionMFCS = d.DimensionMFCS != null? d.DimensionMFCS.Value: 0,
                                    DimensionWMS = d.DimensionWMS != null? d.DimensionWMS.Value: 0,
                                    TimeMFCS = d.TimeMFCS,
                                    TimeWMS = d.TimeWMS });
                            using (WMSToUIClient client = new WMSToUIClient())
                            {
                                client.UpdatePlace(pdproxy.ToArray(), _accessUser);
                            }
                            break;
                        default:
                            break;
                    }
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
                return (EditEnabled && AccessLevel%10 >= 2 && AccessLevel/10 >= 2);
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
                int? tuid = Selected?.TUID;
                DataList = new ObservableCollection<PlaceDiffViewModel>();
                var diffs = await _dbservicewms.PlaceWMSandMFCSDiff();
                foreach (var p in diffs)
                    DataList.Add(new PlaceDiffViewModel
                    {
                        TUID = p.TUID,
                        PlaceMFCS = p.PlaceMFCS,
                        PlaceWMS = p.PlaceWMS,
                        DimensionMFCS = p.DimensionMFCS,
                        DimensionWMS = p.DimensionWMS,
                        TimeMFCS = p.TimeMFCS,
                        TimeWMS = p.TimeWMS
                    });
                if ( tuid != null)
                    Selected = DataList.FirstOrDefault(p => p.TUID == tuid);
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
                if (vm is PlaceDiffsViewModel)
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
