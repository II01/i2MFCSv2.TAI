using Database;
using GalaSoft.MvvmLight;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UserInterface.Services;

namespace UserInterface.ViewModel
{
    public class EventViewModel : ViewModelBase
    {
        #region members
        private Event _event;
        #endregion

        #region properties
        public Event Event
        {
            get { return _event; }
            set
            {
                if (_event != value)
                {
                    _event = value;
                    RaisePropertyChanged("Event");
                }
            }
        }
        public DateTime Time
        {
            get { return _event.Time; }
            set
            {
                if(value != _event.Time)
                {
                    _event.Time = value;
                    RaisePropertyChanged("Time");
                }
            }
        }
        public EnumEventSeverity Severity
        {
            get { return (EnumEventSeverity)_event.Severity; }
            set
            {
                if (_event.Severity != (Event.EnumSeverity)value)
                {
                    _event.Severity = (Event.EnumSeverity)value;
                    RaisePropertyChanged("Severity");
                }
            }
        }
        public EnumEventType Type
        {
            get { return (EnumEventType)_event.Type; }
            set
            {
                if (_event.Type != (Event.EnumType)value)
                {
                    _event.Type = (Event.EnumType)value;
                    RaisePropertyChanged("Type");
                }
            }
        }
        public string Description
        {
            get { return _event.Text; }
            set
            {
                if (_event.Text != value)
                {
                    _event.Text = value;
                    RaisePropertyChanged("Description");
                }
            }
        }
        #endregion

        #region initialization
        public EventViewModel()
        {
            _event = new Event();
        }
        #endregion
    }
}
