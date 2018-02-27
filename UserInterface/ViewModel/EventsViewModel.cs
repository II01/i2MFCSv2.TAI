using System;
using System.Linq;
using GalaSoft.MvvmLight;
using System.Collections.ObjectModel;
using Database;
using System.Windows.Data;
using Warehouse.Model;
using System.Diagnostics;
using UserInterface.Services;

namespace UserInterface.ViewModel
{
    public sealed class EventsViewModel: ViewModelBase
    {
        const int MAX_COUNT = 10000;

        #region members
        private BasicWarehouse _warehouse;
        private ObservableCollection<EventViewModel> _eventList;
        private EventViewModel _selectedEvent;
        private object _lockEventList;
        #endregion

        #region properties
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

        public EventViewModel SelectedEvent
        {
            get { return _selectedEvent; }
            set
            {
                if (_selectedEvent != value)
                {
                    _selectedEvent = value;
                    RaisePropertyChanged("SelectedEvent");
                }
            }
        }
        #endregion

        #region initialization
        public EventsViewModel()
        {
            EventList = new ObservableCollection<EventViewModel>();
            _lockEventList = new object();
            BindingOperations.EnableCollectionSynchronization(EventList, _lockEventList);
        }
        public void Initialize(BasicWarehouse warehouse)
        {
            try
            {
                _warehouse = warehouse;
            }
            catch (Exception e)
            {
                _warehouse.AddEvent(Database.Event.EnumSeverity.Error, Database.Event.EnumType.Exception, e.Message);
                throw new Exception(string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
            }
        }

        #endregion

        #region functions
        public void AddEvent(DateTime time, Event.EnumSeverity severity, Event.EnumType type, string description)
        {
            try
            {
                EventList.Add(new EventViewModel
                {
                    Time = time,
                    Severity = (EnumEventSeverity)severity,
                    Type = (EnumEventType)type,
                    Description = description
                });
                if (EventList.Count > MAX_COUNT)
                    while (EventList.Count > MAX_COUNT - 100)
                        EventList.Remove(EventList.First());
            }
            catch (Exception e)
            {
                // _warehouse.AddEvent(Database.Event.EnumSeverity.Error, Database.Event.EnumType.Exception, e.Message);
                throw new Exception(string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
            }
        }
        #endregion
    }
}
