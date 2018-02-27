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
using Database;
using WCFClients;
using GalaSoft.MvvmLight.Messaging;
using System.Globalization;
using UserInterface.Messages;

namespace UserInterface.ViewModel
{
    public sealed class AlarmsViewModel : ViewModelBase
    {
        #region members
        private BasicWarehouse _warehouse;
        private ObservableCollection<AlarmViewModel> _alarmList;
        private AlarmViewModel _selectedAlarm;
        private object _lockAlarmList;

        #endregion

        #region properties
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
        public AlarmViewModel SelectedAlarm
        {
            get { return _selectedAlarm; }
            set
            {
                if (_selectedAlarm != value)
                {
                    _selectedAlarm = value;
                    RaisePropertyChanged("SelectedAlarm");
                }
            }
        }
        public RelayCommand AckAlarmCommand { get; set; }
        #endregion

        #region intialization
        public void Initialize(BasicWarehouse warehouse)
        {
            _warehouse = warehouse;
            try
            {
                AlarmList = new ObservableCollection<AlarmViewModel>();
                _lockAlarmList = new object();
                BindingOperations.EnableCollectionSynchronization(AlarmList, _lockAlarmList);

                _warehouse.SegmentList.ForEach(c => c.NotifyVM.Add(new Action<ConveyorBasicInfo>(p => OnDataChange(p, c.Name))));
                _warehouse.ConveyorList.ForEach(c => c.NotifyVM.Add(new Action<ConveyorBasicInfo>(p => OnDataChange(p, c.Name))));
                _warehouse.CraneList.ForEach(c => c.NotifyVM.Add(new Action<ConveyorBasicInfo>(p => OnDataChange(p, c.Name))));

                Messenger.Default.Register<MessageLanguageChanged>(this, (mc) => ExecuteLanguageChanged(mc.Culture));
            }
            catch (Exception e)
            {
                _warehouse.AddEvent(Database.Event.EnumSeverity.Error, Database.Event.EnumType.Exception, e.Message);
                throw new Exception(string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
            }
        }

        public AlarmsViewModel()
        {
            try
            {
                AckAlarmCommand = new RelayCommand(() => ExecuteAckAlarm());
            }
            catch (Exception e)
            {
                throw new Exception(string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
            }
        }
        #endregion

        #region functions
        private void OnDataChange(ConveyorBasicInfo info, string name)
        {
            try
            {
                if (info != null)
                {
                    if(info.ActiveAlarms != null)
                    {
                        var alarmToDel = from a in AlarmList
                                         where a.Unit == name && !info.ActiveAlarms.Contains(Convert.ToInt32(a.AlarmID))
                                         select a;
                        alarmToDel.ToList().ForEach(p => AlarmList.Remove(p));
                        var alarmToAdd = from a in info.ActiveAlarms
                                         where  AlarmList.FirstOrDefault(p => p.Unit == name && p.AlarmID == a.ToString()) == null
                                         select new AlarmViewModel
                                         {
                                             AlarmID = a.ToString("00000"),
                                             Unit = name,
                                             Severity = EnumAlarmSeverity.Error,
                                             Status = EnumAlarmStatus.Active,
                                             Text = ResourceReader.GetString(string.Format("ALARM_{0}", a.ToString("00000"))),
                                             ArrivedTime = DateTime.Now,
                                             AckTime = null                                             
                                         };
                        alarmToAdd.ToList().ForEach(p => AlarmList.Add(p));
                    }
                }
            }
            catch (Exception e)
            {
                _warehouse.AddEvent(Database.Event.EnumSeverity.Error, Database.Event.EnumType.Exception, e.Message);
                throw new Exception(string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
            }
        }


        private void ExecuteAckAlarm()
        {
            try
            {
                _warehouse.SegmentList.ForEach((s) => (_warehouse.WCFClient as WCFUIClient).NotifyUIClient.Reset(s.Name));
            }
            catch (Exception e)
            {
                _warehouse.AddEvent(Database.Event.EnumSeverity.Error, Database.Event.EnumType.Exception, 
                                    string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
            }
        }

        public void ExecuteLanguageChanged(CultureInfo ci)
        {
            foreach (AlarmViewModel a in AlarmList)
                a.Text = ResourceReader.GetString(string.Format("ALARM_{0}", a.AlarmID));
        }
        #endregion
    }
}

