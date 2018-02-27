using System;
using GalaSoft.MvvmLight;
using System.ComponentModel;
using UserInterface.Services;
using Database;

namespace UserInterface.ViewModel
{
    public class AlarmViewModel : ViewModelBase
    {
        #region members
        private Alarm _alarm;
        private string _text;
        #endregion

        #region properties
        public Alarm Alarm
        {
            get { return _alarm; }
            set
            {
                if (_alarm != value)
                {
                    _alarm = value;
                    RaisePropertyChanged("Alarm");
                }
            }
        }
        public string AlarmID
        {
            get { return _alarm.AlarmID.PadLeft(5, '0'); }
            set
            {
                if (_alarm.AlarmID != value)
                {
                    _alarm.AlarmID = value;
                    RaisePropertyChanged("AlarmID");
                    Text = ResourceReader.GetString(_alarm.AlarmID);
                }
            }
        }

        public string Unit
        {
            get { return _alarm.Unit; }
            set
            {
                if( _alarm.Unit != value )
                {
                    _alarm.Unit = value;
                    RaisePropertyChanged("Unit");
                }
            }
        }
        public EnumAlarmStatus Status
        {
            get { return (EnumAlarmStatus)_alarm.Status; }
            set
            {
                if (_alarm.Status != (Alarm.EnumAlarmStatus)value)
                {
                    _alarm.Status = (Alarm.EnumAlarmStatus)value;
                    RaisePropertyChanged("Status");
                }
            }
        }

        public EnumAlarmSeverity Severity
        {
            get { return (EnumAlarmSeverity)_alarm.Severity; }
            set
            {
                if (_alarm.Severity != (Alarm.EnumAlarmSeverity)value)
                {
                    _alarm.Severity = (Alarm.EnumAlarmSeverity)value;
                    RaisePropertyChanged("Severity");
                }
            }
        }

        public string Text
        {
            get { return _text; }
            set
            {
                if (_text != value)
                {
                    _text = value;
                    RaisePropertyChanged("Text");
                }
            }
        }

        public DateTime ArrivedTime
        {
            get { return _alarm.ArrivedTime; }
            set
            {
                if (_alarm.ArrivedTime != value)
                {
                    _alarm.ArrivedTime = value;
                    RaisePropertyChanged("ArrivedTime");
                }
            }
        }

        public DateTime? AckTime
        {
            get { return _alarm.AckTime; }
            set
            {
                if(_alarm.AckTime != value )
                {
                    _alarm.AckTime = value;
                    RaisePropertyChanged("AckTime");
                }
            }
        }

        public DateTime? RemovedTime
        {
            get { return _alarm.RemovedTime; }
            set
            {
                if (_alarm.RemovedTime != value)
                {
                    _alarm.RemovedTime = value;
                    RaisePropertyChanged("RemovedTime");
                }
            }
        }
        #endregion

        #region initilization
        public AlarmViewModel()
        {
            _alarm = new Alarm();
        }
        #endregion

    }
}
