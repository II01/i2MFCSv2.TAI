using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Messaging;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using UserInterface.Messages;
using UserInterface.Services;
using Warehouse.Model;

namespace UserInterface.ViewModel
{
    public class HistoryDateTimePickerViewModel : ViewModelBase, IDataErrorInfo
    {
        #region members
        private BasicWarehouse _warehouse;
        private bool _allPropertiesValid;
        private DateTime _timeStamp;
        private DateTime _timeStampDate;
        private string _timeStampTime;
        #endregion

        #region properties


        public DateTime TimeStamp
        {
            get { return _timeStamp; }
            set
            {
                if (_timeStamp != value)
                {
                    _timeStamp = value;
                    RaisePropertyChanged("TimeStamp");
                }
            }
        }
        public DateTime TimeStampDate
        {
            get { return _timeStampDate; }
            set
            {
                if (_timeStampDate != value)
                {
                    _timeStampDate = value;
                    UpdateTimeStamp();
                    RaisePropertyChanged("TimeStampDate");
                }
            }
        }
        public string TimeStampTime
        {
            get { return _timeStampTime; }
            set
            {
                if (_timeStampTime != value)
                {
                    _timeStampTime = value;
                    UpdateTimeStamp();
                    RaisePropertyChanged("TimeStampTime");
                }
            }
        }
        public PropertyValidator Validator { get; set; }
        public bool AllPropertiesValid
        {
            get { return _allPropertiesValid; }
            set
            {
                if (_allPropertiesValid != value)
                {
                    _allPropertiesValid = value;
                    RaisePropertyChanged("AllPropertiesValid");
                }
            }
        }
        #endregion

        #region initialization
        public HistoryDateTimePickerViewModel()
        {
        }

        public void Initialize(BasicWarehouse warehouse)
        {
            _warehouse = warehouse;
            try
            {
                Validator = new PropertyValidator();

                DateTime dt = TimeStamp;

                TimeStampDate = dt.Date;
                TimeStampTime = string.Format("{0}:{1:d2}", dt.Hour, dt.Minute);

                Messenger.Default.Register<MessageLanguageChanged>(this, (mc) => ExecuteLanguageChanged(mc.Culture));
            }
            catch (Exception e)
            {
                _warehouse.AddEvent(Database.Event.EnumSeverity.Error, Database.Event.EnumType.Exception, e.Message);
                throw new Exception(string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
            }

        }
        #endregion  

        #region functions
        public TimeSpan? StringToTimeSpan(string str)
        {
            try
            {
                if(str != null)
                {
                    string[] l = str.Split(':');

                    if (l.Length == 2 && l[1].Length == 2 &&
                        Int32.TryParse(l[0], out int h) && h >= 0 && h <= 23 &&
                        Int32.TryParse(l[1], out int m) && m >= 0 && m <= 59)
                        return new TimeSpan(h, m, 0);
                }
            }
            catch (Exception e)
            {
                _warehouse.AddEvent(Database.Event.EnumSeverity.Error, Database.Event.EnumType.Exception, 
                                    string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
            }
            return null;
        }
        private void UpdateTimeStamp()
        {
            try
            {
                TimeStamp = TimeStampDate;
                TimeSpan? ts = StringToTimeSpan(TimeStampTime);
                if(ts != null)
                    TimeStamp = TimeStamp.Add(ts.Value);
            }
            catch(Exception e)
            {
                _warehouse.AddEvent(Database.Event.EnumSeverity.Error, Database.Event.EnumType.Exception,
                                    string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
            }
        }

        public void ExecuteLanguageChanged(CultureInfo ci)
        {
            DateTime d = TimeStampDate;
            TimeStampDate = TimeStampDate.AddDays(1);
            TimeStampDate = d;
        }

        #endregion

        #region validation
        public string Error
        {
            get { return null; }
        }

        public string this[string propertyName]
        {
            get
            {
                try
                {
                    string validationResult = String.Empty;

                    switch (propertyName)
                    {
                        case "TimeStampTime":
                            if (StringToTimeSpan(TimeStampTime) == null)
                                validationResult = ResourceReader.GetString("ERR_TIME"); ;
                            break;
                    }
                    Validator.AddOrUpdate(propertyName, validationResult == String.Empty);
                    AllPropertiesValid = Validator.IsValid();
                    return validationResult;
                }
                catch (Exception e)
                {
                    _warehouse.AddEvent(Database.Event.EnumSeverity.Error, Database.Event.EnumType.Exception,
                                        string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
                    Validator.AddOrUpdate(propertyName, false);
                    AllPropertiesValid = Validator.IsValid();
                    return ResourceReader.GetString("ERR_EXCEPTION");
                }
            }
        }
        #endregion
    }
}
