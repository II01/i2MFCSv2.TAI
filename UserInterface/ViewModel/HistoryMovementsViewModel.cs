using System;
using System.Collections.Generic;
using System.Linq;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.CommandWpf;
using System.Collections.ObjectModel;
using UserInterface.Services;
using Warehouse.Model;
using System.Diagnostics;
using Database;
using GalaSoft.MvvmLight.Messaging;
using UserInterface.Messages;

namespace UserInterface.ViewModel
{
    public sealed class HistoryMovementsViewModel: ViewModelBase
    {
        #region members
        private BasicWarehouse _warehouse;
        private ObservableCollection<MovementViewModel> _movementList;
        private MovementViewModel _selectedMovement;
        private HistoryDateTimePickerViewModel _dateFrom;
        private HistoryDateTimePickerViewModel _dateTo;
        private string _location;
        private string _transportUnit;
        private int _records;
        #endregion

        #region properties
        public RelayCommand Refresh { get; private set; }
        public ObservableCollection<MovementViewModel> MovementList
        {
            get { return _movementList; }
            set
            {
                if (_movementList != value)
                {
                    _movementList = value;
                    RaisePropertyChanged("MovementList");
                }
            }
        }
        public MovementViewModel SelectedMovement
        {
            get { return _selectedMovement; }
            set
            {
                if (_selectedMovement != value)
                {
                    _selectedMovement = value;
                    RaisePropertyChanged("SelectedMovement");
                }
            }
        }
        public HistoryDateTimePickerViewModel DateFrom
        {
            get { return _dateFrom; }
            set
            {
                if (_dateFrom != value)
                {
                    _dateFrom = value;
                    RaisePropertyChanged("DateFrom");
                }
            }
        }
        public HistoryDateTimePickerViewModel DateTo
        {
            get { return _dateTo; }
            set
            {
                if (_dateTo != value)
                {
                    _dateTo = value;
                    RaisePropertyChanged("DateTo");
                }
            }
        }
        public string Location
        {
            get { return _location; }
            set
            {
                if (_location != value)
                {
                    _location = value;
                    RaisePropertyChanged("Location");
                }
            }
        }
        public string TransportUnit
        {
            get { return _transportUnit; }
            set
            {
                if (_transportUnit != value)
                {
                    _transportUnit = value;
                    RaisePropertyChanged("TransportUnit");
                }
            }
        }
        public int Records
        {
            get { return _records; }
            set
            {
                if (_records != value)
                {
                    _records = value;
                    RaisePropertyChanged("Records");
                }
            }
        }
        #endregion

        #region intialization    
        public HistoryMovementsViewModel()
        {
            Refresh = new RelayCommand(() => ExecuteRefresh());
        }
        public void Initialize(BasicWarehouse warehouse)
        {
            _warehouse = warehouse;
            try
            {
                MovementList = new ObservableCollection<MovementViewModel>();
                DateFrom = new HistoryDateTimePickerViewModel { TimeStamp = DateTime.Now.AddDays(-1) };
                DateFrom.Initialize(_warehouse);
                DateTo = new HistoryDateTimePickerViewModel { TimeStamp = DateTime.Now.AddHours(+1) };
                DateTo.Initialize(_warehouse);
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
                MovementViewModel sm = SelectedMovement;
                MovementList.Clear();
                foreach(var m in _warehouse.DBService.GetMovements(DateFrom.TimeStamp, DateTo.TimeStamp, Location, TransportUnit))
                    MovementList.Add( new MovementViewModel { Movement = m});
                if( sm != null)
                    SelectedMovement = MovementList.FirstOrDefault(p => p.ID == sm.ID);
                Records = MovementList.Count;
            }
            catch (Exception e)
            {
                _warehouse.AddEvent(Database.Event.EnumSeverity.Error, Database.Event.EnumType.Exception, 
                                    string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
            }
        }

        public void ExecuteViewActivated(ViewModelBase vm)
        {
            try
            {
                if (vm is HistoryMovementsViewModel)
                {
                    HistoryDateTimePickerViewModel d;
                    d = new HistoryDateTimePickerViewModel { TimeStamp = DateTime.Now.AddDays(-1) };
                    d.Initialize(_warehouse);
                    DateFrom = d;

                    d = new HistoryDateTimePickerViewModel { TimeStamp = DateTime.Now.AddHours(+1) };
                    d.Initialize(_warehouse);
                    DateTo = d;
                }
            }
            catch (Exception e)
            {
                _warehouse.AddEvent(Event.EnumSeverity.Error, Event.EnumType.Exception,
                                    string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
            }
        }
        #endregion
    }
}
