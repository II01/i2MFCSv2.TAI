using System;
using GalaSoft.MvvmLight;
using Database;
using Warehouse.Model;
using System.Diagnostics;
using GalaSoft.MvvmLight.CommandWpf;
using UserInterface.Services;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using UserInterface.Messages;
using GalaSoft.MvvmLight.Messaging;

namespace UserInterface.ViewModel
{
    public sealed class HistoryEventsViewModel: ViewModelBase
    {
        const int MAX_COUNT = 10000;

        #region members
        private bool _refreshEnabled;
        private BasicWarehouse _warehouse;
        private ObservableCollection<EventViewModel> _eventList;
        private EventViewModel _selectedItem;
        private HistoryDateTimePickerViewModel _dateFrom;
        private HistoryDateTimePickerViewModel _dateTo;
        private string _severity;
        private string _type;
        private int _records;
        #endregion

        #region properties
        public RelayCommand Refresh { get; set; }
        public ObservableCollection<EventViewModel> EventList
        {
            get { return _eventList; }
            set
            {
                if (_eventList != value)
                {
                    _eventList = value;
                    RaisePropertyChanged("EventList");
                }
            }
        }

        public EventViewModel SelectedItem
        {
            get { return _selectedItem; }
            set
            {
                if (_selectedItem != value)
                {
                    _selectedItem = value;
                    RaisePropertyChanged("SelectedItem");
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
        public string Severity
        {
            get { return _severity; }
            set
            {
                if (_severity != value)
                {
                    _severity = value;
                    RaisePropertyChanged("Severity");
                }
            }
        }
        public string Type
        {
            get { return _type; }
            set
            {
                if (_type != value)
                {
                    _type = value;
                    RaisePropertyChanged("Type");
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
        public bool RefreshEnabled
        {
            get { return _refreshEnabled; }
            set
            {
                if (_refreshEnabled != value)
                {
                    _refreshEnabled = value;
                    RaisePropertyChanged("RefreshEnabled");
                }
            }
        }
        #endregion

        #region initialization
        public HistoryEventsViewModel()
        {
            EventList = new ObservableCollection<EventViewModel>();
            Refresh = new RelayCommand(ExecuteRefresh);
        }
        public void Initialize(BasicWarehouse warehouse)
        {
            try
            {
                _warehouse = warehouse;
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

        public void ExecuteRefresh()
        {
            try
            {
                var si = SelectedItem;

                var el = EventList;
                el.Clear();
                foreach (var e in _warehouse.DBService.GetEvents(DateFrom.TimeStamp, DateTo.TimeStamp, Severity, Type))
                    el.Add( new EventViewModel { Event = e });
                EventList = el;
                
                if(si != null)
                    SelectedItem = EventList.FirstOrDefault(p => p.Time == si.Time && p.Severity == si.Severity && p.Type == si.Type && p.Description == si.Description);

                Records = EventList.Count;

            }
            catch (Exception e)
            {
                _warehouse.AddEvent(Event.EnumSeverity.Error, Database.Event.EnumType.Exception, 
                                    string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
            }
        }
        public void ExecuteViewActivated(ViewModelBase vm)
        {
            try
            {
                if(vm is HistoryEventsViewModel)
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
                _warehouse.AddEvent(Event.EnumSeverity.Error, Database.Event.EnumType.Exception,
                                    string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
            }
        }
        #endregion
    }
}
