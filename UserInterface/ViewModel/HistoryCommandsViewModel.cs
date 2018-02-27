using System;
using System.Linq;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.CommandWpf;
using System.Collections.ObjectModel;
using UserInterface.Services;
using Database;
using Warehouse.Model;
using System.Diagnostics;
using System.ComponentModel;
using GalaSoft.MvvmLight.Messaging;
using UserInterface.Messages;

namespace UserInterface.ViewModel
{
    public sealed class HistoryCommandsViewModel : ViewModelBase
    {
        #region members
        private BasicWarehouse _warehouse;

        private HistoryDateTimePickerViewModel _dateFrom;
        private HistoryDateTimePickerViewModel _dateTo;
        private int _records;

        private ObservableCollection<CommandViewModel> _commandList;
        private ObservableCollection<SimpleCommandViewModel> _simpleCommandList;
        private CommandViewModel _selectedContent;
        #endregion

        #region properites
        public RelayCommand RefreshCmd { get; private set; }

        public PropertyValidator Validator { get; set; }
        public ObservableCollection<CommandViewModel> CommandList
        {
            get { return _commandList; }
            set
            {
                if (_commandList != value)
                {
                    _commandList = value;
                    RaisePropertyChanged("CommandList");
                }
            }
        }
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
        public CommandViewModel SelectedContent
        {
            get { return _selectedContent; }
            set
            {
                if (_selectedContent != value)
                {
                    _selectedContent = value;

                    RaisePropertyChanged("SelectedContent");
                    try
                    {
                        if (SelectedContent != null)
                        {
                            SimpleCommandList.Clear();
                            foreach (var sc in _warehouse.DBService.GetSimpleCommands(SelectedContent.Command.ID, SimpleCommand.EnumStatus.Finished, null, null))
                            {
                                if (sc is SimpleCraneCommand)
                                    SimpleCommandList.Add((SimpleCommandViewModel)new SimpleCommandCraneViewModel { Command = sc });
                                else if (sc is SimpleSegmentCommand)
                                    SimpleCommandList.Add((SimpleCommandViewModel)new SimpleCommandSegmentViewModel { Command = sc });
                                else
                                    SimpleCommandList.Add((SimpleCommandViewModel)new SimpleCommandConveyorViewModel { Command = sc });
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        _warehouse.AddEvent(Database.Event.EnumSeverity.Error, Database.Event.EnumType.Exception, 
                                            string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
                    }
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
        public HistoryCommandsViewModel()
        {
            Validator = new PropertyValidator();

            SelectedContent = null;

            RefreshCmd = new RelayCommand(() => ExecuteRefresh(), CanExecuteRefresh);
        }
        public void Initialize(BasicWarehouse warehouse)
        {
            _warehouse = warehouse;
            try
            {
                CommandList = new ObservableCollection<CommandViewModel>();
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
        public void ExecuteRefresh()
        {
            try
            {
                CommandViewModel cmd = SelectedContent;

                CommandList.Clear();
                foreach (var c in _warehouse.DBService.GetCommands(Command.EnumCommandStatus.Finished, DateFrom.TimeStamp, DateTo.TimeStamp))
                {
                    if (c is CommandMaterial)
                        CommandList.Add((CommandViewModel)new CommandMaterialViewModel { Command = c });
                    else if (c is CommandSegment)
                        CommandList.Add((CommandViewModel)new CommandSegmentViewModel { Command = c });
                    else
                        CommandList.Add((CommandViewModel)new CommandCommandViewModel { Command = c });
                }
                foreach (var c in CommandList)
                    c.Initialize(_warehouse);
                Records = CommandList.Count();
                if (cmd != null)
                    SelectedContent = CommandList.FirstOrDefault(p => p.ID == cmd.ID);
            }
            catch (Exception e)
            {
                _warehouse.AddEvent(Database.Event.EnumSeverity.Error, Database.Event.EnumType.Exception,
                                    string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
            }
        }
        public bool CanExecuteRefresh()
        {
            try
            {
                return true;
            }
            catch (Exception e)
            {
                _warehouse.AddEvent(Database.Event.EnumSeverity.Error, Database.Event.EnumType.Exception,
                                    string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
                return false;
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