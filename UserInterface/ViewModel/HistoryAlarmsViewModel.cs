using System;
using System.Linq;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using System.Collections.ObjectModel;
using UserInterface.Services;
using Warehouse.Model;
using System.Diagnostics;
using Warehouse.ConveyorUnits;
using System.Windows.Data;
using GalaSoft.MvvmLight.Messaging;
using UserInterface.Messages;
using Database;

namespace UserInterface.ViewModel
{
    public sealed class HistoryAlarmsViewModel : ViewModelBase
    {
        const int MAX_COUNT = 10000;

        #region members
        private BasicWarehouse _warehouse;
        private ObservableCollection<AlarmViewModel> _alarmList;
        private AlarmViewModel _selectedItem;
        private HistoryDateTimePickerViewModel _dateFrom;
        private HistoryDateTimePickerViewModel _dateTo;
        private string _unit;
        private string _id;
        private string _status;
        private int _records;
        #endregion

        #region properties
        public RelayCommand Refresh { get; set; }
        public ObservableCollection<AlarmViewModel> AlarmList
        {
            get { return _alarmList; }
            set
            {
                if (_alarmList != value)
                {
                    _alarmList = value;
                    RaisePropertyChanged("AlarmList");
                }
            }
        }

        public AlarmViewModel SelectedItem
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
        public string Unit
        {
            get { return _unit; }
            set
            {
                if (_unit != value)
                {
                    _unit = value;
                    RaisePropertyChanged("Unit");
                }
            }
        }
        public string ID
        {
            get { return _id; }
            set
            {
                if (_id != value)
                {
                    _id = value;
                    RaisePropertyChanged("ID");
                }
            }
        }
        public string Status
        {
            get { return _status; }
            set
            {
                if (_status != value)
                {
                    _status = value;
                    RaisePropertyChanged("Status");
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

        #region initialization
        public HistoryAlarmsViewModel()
        {
            AlarmList = new ObservableCollection<AlarmViewModel>();
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

                var al = AlarmList;
                al.Clear();
                foreach (var a in _warehouse.DBService.GetAlarms(DateFrom.TimeStamp, DateTo.TimeStamp, Unit, ID, Status))
                {
                    al.Add(new AlarmViewModel
                    {
                        Alarm = a,
                        Text = ResourceReader.GetString(string.Format("ALARM_{0}", a.AlarmID.PadLeft(5, '0')))
                    });
                }
                AlarmList = al;

                if (si != null)
                    SelectedItem = AlarmList.FirstOrDefault(p => p.ArrivedTime == si.ArrivedTime &&
                                                                 p.AlarmID == si.AlarmID &&
                                                                 p.Unit == si.Unit &&
                                                                 p.Status == si.Status);

                Records = AlarmList.Count;

            }
            catch (Exception e)
            {
                _warehouse.AddEvent(Event.EnumSeverity.Error, Event.EnumType.Exception,
                                    string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
            }
        }
        public void ExecuteViewActivated(ViewModelBase vm)
        {
            try
            {
                if (vm is HistoryEventsViewModel)
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

