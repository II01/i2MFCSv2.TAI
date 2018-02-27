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
using System.Collections;

namespace UserInterface.ViewModel
{
    public sealed class SimpleCommandsViewModel : ViewModelBase
    {
        public enum CommandType {None = 0, Add, Restart, Delete };

        #region members
        private BasicWarehouse _warehouse;

        private bool _isEnabledField;
        private bool _isEnabledCC;
        private CommandType _selectedCommand;
        private SimpleCommandViewModel _selectedContent;
        private SimpleCommandViewModel _detailedContent;
        private SimpleCommandViewModel _manageContent;

        private ObservableCollection<SimpleCommandViewModel> _simpleCommandList;
        private int _accessLevel;
        private int _numberOfSelectedItems;
        #endregion

        #region properties
        public RelayCommand RefreshCmd { get; private set; }
        public RelayCommand ConveyorCmd { get; private set; }
        public RelayCommand CraneCmd { get; private set; }
        public RelayCommand SegmentCmd { get; private set; }
        public RelayCommand RestartCmd { get; private set; }
        public RelayCommand DeleteCmd { get; private set; }
        public RelayCommand Confirm { get; private set; }
        public RelayCommand Cancel { get; private set; }
        public RelayCommand<IList> SelectionChangedCommand { get; private set; }

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
        public bool IsEnabledField
        {
            get { return _isEnabledField; }
            set
            {
                if (_isEnabledField != value)
                {
                    _isEnabledField = value;
                    RaisePropertyChanged("IsEnabledField");
                }
            }
        }
        public bool IsEnabledCC
        {
            get { return _isEnabledCC; }
            set
            {
                if (_isEnabledCC != value)
                {
                    _isEnabledCC = value;
                    RaisePropertyChanged("IsEnabledCC");
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
                    try
                    {
                        DetailedContent = SelectedContent;
                    }
                    catch (Exception e)
                    {
                        _warehouse.AddEvent(Database.Event.EnumSeverity.Error, Database.Event.EnumType.Exception, e.Message);
                        throw new Exception(string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
                    }
                }
            }
        }

        public SimpleCommandViewModel DetailedContent
        {
            get { return _detailedContent; }
            set
            {
                if (_detailedContent != value)
                {
                    _detailedContent = value;
                    RaisePropertyChanged("DetailedContent");
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
        public int NumberOfSelectedItems
        {
            get
            {
                return _numberOfSelectedItems;
            }
            set
            {
                if (_numberOfSelectedItems != value)
                {
                    _numberOfSelectedItems = value;
                    RaisePropertyChanged("NumberOfSelectedItems");
                }
            }
        }
        #endregion

        #region initialization
        public SimpleCommandsViewModel()
        {
            IsEnabledField = false;
            IsEnabledCC = false;
            _selectedCommand = CommandType.None;

            SelectedContent = null;

            RefreshCmd = new RelayCommand(() => ExecuteRefreshCommand());
            ConveyorCmd = new RelayCommand(() => ExecuteConveyorCommand(), CanExecuteConveyorCommand);
            CraneCmd = new RelayCommand(() => ExecuteCraneCommand(), CanExecuteCraneCommand);
            SegmentCmd = new RelayCommand(() => ExecuteSegmentCommand(), CanExecuteSegmentCommand);
            RestartCmd = new RelayCommand(() => ExecuteRestart(), CanExecuteRestart);
            DeleteCmd = new RelayCommand(() => ExecuteDelete(), CanExecuteDelete);
            Confirm = new RelayCommand(() => ExecuteConfirm(), CanExecuteConfirm);
            Cancel = new RelayCommand(() => ExecuteCancel(), CanExecuteCancel);
            SelectionChangedCommand = new RelayCommand<IList>(
                items =>
                {
                    if (items == null)
                    {
                        NumberOfSelectedItems = 0;
                        return;
                    }

                    NumberOfSelectedItems = items.Count;
                });

            Messenger.Default.Register<MessageAccessLevel>(this, (mc) => { AccessLevel = mc.AccessLevel; });
            Messenger.Default.Register<MessageViewChanged>(this, vm => ExecuteViewActivated(vm.ViewModel));
        }

        public void Initialize(BasicWarehouse warehouse)
        {
            _warehouse = warehouse;
            try
            {
                SimpleCommandList = new ObservableCollection<SimpleCommandViewModel>();
                Messenger.Default.Register<MessageAccessLevel>(this, (mc) => { AccessLevel = mc.AccessLevel; });
            }
            catch (Exception e)
            {
                _warehouse.AddEvent(Database.Event.EnumSeverity.Error, Database.Event.EnumType.Exception, e.Message);
                throw new Exception(string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
            }
        }


        #endregion

        #region commands
        private void ExecuteRefreshCommand()
        {
            try
            {
                SimpleCommandViewModel sc = SelectedContent;
                SimpleCommandList.Clear();
                foreach (var s in _warehouse.DBService.GetSimpleCommands(null, SimpleCommand.EnumStatus.InPlc, DateTime.Now.AddMinutes(-30), DateTime.Now))
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
                if( sc != null)
                    SelectedContent = SimpleCommandList.FirstOrDefault(p => p.ID == sc.ID);
            }
            catch (Exception e)
            {
                _warehouse.AddEvent(Database.Event.EnumSeverity.Error, Database.Event.EnumType.Exception, 
                                    string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
            }
        }

        private void ExecuteConveyorCommand()
        {
            try
            {
                IsEnabledField = true;
                IsEnabledCC = true;
                _selectedCommand = CommandType.Add;
                _manageContent = new SimpleCommandConveyorViewModel { Command = new SimpleConveyorCommand() };
                _manageContent.Initialize(_warehouse);

                DetailedContent = _manageContent;
                DetailedContent.Task = EnumSimpleCommandTask.Move;
                DetailedContent.ValidationEnabled = true;
            }
            catch (Exception e)
            {
                _warehouse.AddEvent(Database.Event.EnumSeverity.Error, Database.Event.EnumType.Exception,
                                    string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
            }
        }

        private bool CanExecuteConveyorCommand()
        {
            try
            {
                return !IsEnabledField && AccessLevel == 2;
            }
            catch (Exception e)
            {
                _warehouse.AddEvent(Database.Event.EnumSeverity.Error, Database.Event.EnumType.Exception,
                                    string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
                return false;
            }
        }
        private void ExecuteCraneCommand()
        {
            try
            {
                IsEnabledField = true;
                IsEnabledCC = true;
                _selectedCommand = CommandType.Add;
                _manageContent = new SimpleCommandCraneViewModel { Command = new SimpleCraneCommand() };
                _manageContent.Initialize(_warehouse);

                DetailedContent = _manageContent;
                DetailedContent.Task = EnumSimpleCommandTask.Move;
                DetailedContent.ValidationEnabled = true;
            }
            catch (Exception e)
            {
                _warehouse.AddEvent(Database.Event.EnumSeverity.Error, Database.Event.EnumType.Exception,
                                    string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
            }
        }
        private bool CanExecuteCraneCommand()
        {
            try
            {
                return !IsEnabledField && AccessLevel == 2;
            }
            catch (Exception e)
            {
                _warehouse.AddEvent(Database.Event.EnumSeverity.Error, Database.Event.EnumType.Exception,
                                    string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
                return false;
            }
        }
        private void ExecuteSegmentCommand()
        {
            try
            {
                IsEnabledField = true;
                IsEnabledCC = true;
                _selectedCommand = CommandType.Add;
                _manageContent = new SimpleCommandSegmentViewModel { Command = new SimpleSegmentCommand() };
                _manageContent.Initialize(_warehouse);

                DetailedContent = _manageContent;
                DetailedContent.Task = EnumSimpleCommandTask.Info;
                DetailedContent.ValidationEnabled = true;
            }
            catch (Exception e)
            {
                _warehouse.AddEvent(Database.Event.EnumSeverity.Error, Database.Event.EnumType.Exception,
                                    string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
            }
        }
        private bool CanExecuteSegmentCommand()
        {
            try
            {
                return !IsEnabledField && AccessLevel == 2;
            }
            catch (Exception e)
            {
                _warehouse.AddEvent(Database.Event.EnumSeverity.Error, Database.Event.EnumType.Exception,
                                    string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
                return false;
            }
        }
        private void ExecuteRestart()
        {
            try
            {
                if (SelectedContent != null)
                {
                    IsEnabledField = false;
                    if(DetailedContent != null)
                        DetailedContent.ValidationEnabled = false;
                    IsEnabledCC = true;
                    _selectedCommand = CommandType.Restart;
                }
            }
            catch (Exception e)
            {
                _warehouse.AddEvent(Database.Event.EnumSeverity.Error, Database.Event.EnumType.Exception,
                                    string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
            }
        }
        private bool CanExecuteRestart()
        {
            try
            {
                return SelectedContent != null && AccessLevel == 2 &&
                       (SelectedContent.Status == EnumSimpleCommandStatus.Canceled ||
                        SelectedContent.Status == EnumSimpleCommandStatus.NotActive);
            }
            catch (Exception e)
            {
                _warehouse.AddEvent(Database.Event.EnumSeverity.Error, Database.Event.EnumType.Exception,
                                    string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
                return false;
            }
        }
        private void ExecuteDelete()
        {
            try
            {
                if (SelectedContent != null)
                {
                    IsEnabledField = false;
                    if (DetailedContent != null)
                        DetailedContent.ValidationEnabled = false;
                    IsEnabledCC = true;
                    _selectedCommand = CommandType.Delete;
                }
            }
            catch (Exception e)
            {
                _warehouse.AddEvent(Database.Event.EnumSeverity.Error, Database.Event.EnumType.Exception,
                                    string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
            }
        }
        private bool CanExecuteDelete()
        {
            try
            {
                return SelectedContent != null && AccessLevel == 2;
            }
            catch (Exception e)
            {
                _warehouse.AddEvent(Database.Event.EnumSeverity.Error, Database.Event.EnumType.Exception,
                                    string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
                return false;
            }
        }
        private void ExecuteCancel()
        {
            try
            {
                IsEnabledField = false;
                IsEnabledCC = false;
                DetailedContent.ValidationEnabled = false;
                DetailedContent = SelectedContent;
            }
            catch (Exception e)
            {
                _warehouse.AddEvent(Database.Event.EnumSeverity.Error, Database.Event.EnumType.Exception,
                                    string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
            }
        }
        private bool CanExecuteCancel()
        {
            try
            {
                return IsEnabledCC;
            }
            catch (Exception e)
            {
                _warehouse.AddEvent(Database.Event.EnumSeverity.Error, Database.Event.EnumType.Exception,
                                    string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
                return false;
            }
        }
        private void ExecuteConfirm()
        {
            SimpleCommand cmd;
            try
            {
                IsEnabledField = false;
                IsEnabledCC = false;
                switch (_selectedCommand)
                {
                    case CommandType.Add:
                        // execute cmd based on DetailedContent
                        DetailedContent.Time = DateTime.Now;
                        cmd = DetailedContent.Command;
                        if (cmd.Material == 0)
                            cmd.Material = null;
                        if(cmd.Material != null)
                            _warehouse.DBService.FindMaterialID((int)cmd.Material, true);
                        _warehouse.DBService.AddSimpleCommand(cmd);
                        SimpleCommandList.Add(DetailedContent);
                        break;
                    case CommandType.Restart:
                        cmd = _warehouse.DBService.FindSimpleCommandByID(DetailedContent.Command.ID);
                        if (cmd != null)
                        {
                            cmd.Status = SimpleCommand.EnumStatus.NotActive;
                            cmd.Time = DateTime.Now;
                            _warehouse.DBService.AddSimpleCommand(cmd);
                        }
                        break;
                    case CommandType.Delete:
                        cmd = _warehouse.DBService.FindSimpleCommandByID(DetailedContent.Command.ID);
                        if (cmd != null && cmd.Status ==  SimpleCommand.EnumStatus.NotActive)
                        {
                            cmd.Status = SimpleCommand.EnumStatus.Canceled;
                            _warehouse.DBService.UpdateSimpleCommand(cmd);
                        }
                        else if (cmd.Status == SimpleCommand.EnumStatus.InPlc || 
                                 cmd.Status == SimpleCommand.EnumStatus.Written )
                        {
                            if (cmd is SimpleCraneCommand)
                            {
                                if (cmd.Material == 0)
                                    cmd.Material = null;
                                _warehouse.DBService.AddSimpleCommand(
                                    new SimpleCraneCommand
                                    {
                                        Material = cmd.Material,
                                        Source = cmd.Source,
                                        Task = SimpleCommand.EnumTask.Cancel,
                                        Unit = (cmd as SimpleCraneCommand).Unit,
                                        CancelID = cmd.ID,
                                        Status = SimpleCommand.EnumStatus.NotActive,
                                        Time = DateTime.Now
                                    });
                            }
                            else if (cmd is SimpleConveyorCommand) // in case of hanging comman (inplc, written, you delete of command is forced)
                            {
                                DetailedContent.Status = EnumSimpleCommandStatus.Canceled;
                                cmd = _warehouse.DBService.FindSimpleCommandByID(DetailedContent.Command.ID);
                                cmd.Status = SimpleCommand.EnumStatus.Canceled;
                                _warehouse.DBService.UpdateSimpleCommand(cmd);
                            }
                        }
                        break;
                    default:
                        break;
                }
                DetailedContent.ValidationEnabled = false;
                DetailedContent = SelectedContent;
            }
            catch (Exception e)
            {
                _warehouse.AddEvent(Database.Event.EnumSeverity.Error, Database.Event.EnumType.Exception,
                                    string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
            }
        }
        private bool CanExecuteConfirm()
        {
            try
            {
                return IsEnabledCC && AccessLevel == 2 && 
                       (DetailedContent.AllPropertiesValid || _selectedCommand == CommandType.Delete);
            }
            catch (Exception e)
            {
                _warehouse.AddEvent(Database.Event.EnumSeverity.Error, Database.Event.EnumType.Exception,
                                    string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
                return false;
            }
        }

        #endregion
        public void ExecuteViewActivated(ViewModelBase vm)
        {
            try
            {
                if (vm is SimpleCommandsViewModel)
                {
                    ExecuteRefreshCommand();
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