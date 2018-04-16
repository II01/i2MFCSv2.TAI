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

namespace UserInterface.ViewModel
{
    public sealed class HistoryLogsViewModel : ViewModelBase
    {
        #region members
        private ObservableCollection<LogViewModel> _dataList;
        private LogViewModel _selected;
        private BasicWarehouse _warehouse;
        private DBServiceWMS _dbservicewms;
        private int _accessLevel;
        private HistoryDateTimePickerViewModel _dateFrom;
        private HistoryDateTimePickerViewModel _dateTo;
        private int _records;
        #endregion

        #region properites
        public RelayCommand Refresh { get; private set; }

        public ObservableCollection<LogViewModel> DataList
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
        public LogViewModel Selected
        {
            get { return _selected; }
            set
            {
                if (_selected != value)
                {
                    _selected = value;
                    RaisePropertyChanged("Selected");
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
        public HistoryLogsViewModel()
        {
            Refresh = new RelayCommand(() => ExecuteRefresh());
        }

        public void Initialize(BasicWarehouse warehouse)
        {
            _warehouse = warehouse;
            _dbservicewms = new DBServiceWMS(_warehouse);
            try
            {
                DataList = new ObservableCollection<LogViewModel>();
                DateFrom = new HistoryDateTimePickerViewModel { TimeStamp = DateTime.Now.AddDays(-1) };
                DateFrom.Initialize(_warehouse);
                DateTo = new HistoryDateTimePickerViewModel { TimeStamp = DateTime.Now.AddHours(+1) };
                DateTo.Initialize(_warehouse);
                Records = 0;
                Messenger.Default.Register<MessageAccessLevel>(this, (mc) => { AccessLevel = mc.AccessLevel; });
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
                int? id = Selected?.ID;
                DataList.Clear();
                foreach (var p in _dbservicewms.GetLogs(DateFrom.TimeStamp, DateTo.TimeStamp))
                    DataList.Add(new LogViewModel
                    {
                        ID = p.ID,
                        Severity = (EnumLogWMS)p.Severity,
                        Source = p.Source,
                        Message = p.Message,
                        Time = p.Time
                    });
                foreach (var l in DataList)
                    l.Initialize(_warehouse);
                Records = DataList.Count();
                if ( id != null)
                    Selected = DataList.FirstOrDefault(p => p.ID == id);
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
                if (vm is HistoryLogsViewModel)
                {
                    ; // ExecuteRefresh();
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
