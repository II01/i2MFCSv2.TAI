﻿using System;
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
using System.Threading.Tasks;

namespace UserInterface.ViewModel
{
    public sealed class HistorySimpleCommandsViewModel : ViewModelBase
    {
        #region members
        private BasicWarehouse _warehouse;

        private HistoryDateTimePickerViewModel _dateFrom;
        private HistoryDateTimePickerViewModel _dateTo;
        private int _records;

        private SimpleCommandViewModel _selectedContent;

        private ObservableCollection<SimpleCommandViewModel> _simpleCommandList;
        #endregion

        #region properties
        public RelayCommand RefreshCmd { get; private set; }

        public ObservableCollection<SimpleCommandViewModel> SimpleCommandList
        {
            get { return _simpleCommandList; }
            set
            {
                if (_simpleCommandList != value)
                {
                    _simpleCommandList = value;
                    RaisePropertyChanged("SimpleCommandList");
                }
            }
        }
        public SimpleCommandViewModel SelectedContent
        {
            get { return _selectedContent; }
            set
            {
                if (_selectedContent != value)
                {
                    _selectedContent = value;
                    RaisePropertyChanged("SelectedContent");
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
        public HistorySimpleCommandsViewModel()
        {
            SelectedContent = null;
            RefreshCmd = new RelayCommand(async () => await ExecuteRefreshCommand());
        }

        public void Initialize(BasicWarehouse warehouse)
        {
            _warehouse = warehouse;
            try
            {
                SimpleCommandList = new ObservableCollection<SimpleCommandViewModel>();
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
        private async Task ExecuteRefreshCommand()
        {
            try
            {
                SimpleCommandViewModel sc = SelectedContent;
                var scmds = await _warehouse.DBService.GetSimpleCommands(null, SimpleCommand.EnumStatus.Finished, DateFrom.TimeStamp, DateTo.TimeStamp);
                SimpleCommandList.Clear();
                foreach (var s in scmds)
                {
                    if (s is SimpleCraneCommand)
                        SimpleCommandList.Add((SimpleCommandViewModel)new SimpleCommandCraneViewModel { Command = s });
                    else if (s is SimpleSegmentCommand)
                        SimpleCommandList.Add((SimpleCommandViewModel)new SimpleCommandSegmentViewModel { Command = s });
                    else
                        SimpleCommandList.Add((SimpleCommandViewModel)new SimpleCommandConveyorViewModel { Command = s });
                }

                foreach (SimpleCommandViewModel scvm in SimpleCommandList)
                    scvm.Initialize(_warehouse);
                Records = SimpleCommandList.Count();
                if (sc != null)
                    SelectedContent = SimpleCommandList.FirstOrDefault(p => p.ID == sc.ID);
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
                if (vm is HistorySimpleCommandsViewModel)
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