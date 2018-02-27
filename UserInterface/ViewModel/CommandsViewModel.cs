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
using System.Threading.Tasks;
using Warehouse.DataService;
using System.Collections.Generic;
using GalaSoft.MvvmLight.Messaging;
using UserInterface.Messages;
using System.Collections;

namespace UserInterface.ViewModel
{
    public sealed class CommandsViewModel : ViewModelBase, IDataErrorInfo
    {
        public enum CommandType { None = 0, AddMaterial, AddSegment, Restart, Delete };

        #region members

        private BasicWarehouse _warehouse;

        private bool _isEnabledField;
        private bool _isEnabledCC;
        private bool _isInDeleteMode;

        private string _generateLoc;
        private int _generateQuantity;
        private int _palletStart;

        private CommandType _selectedCommand;
        private ObservableCollection<CommandViewModel> _commandList;
        private ObservableCollection<SimpleCommandViewModel> _simpleCommandList;
        private CommandViewModel _selectedContent;
        private CommandViewModel _detailedContent;
        private CommandViewModel _manageContent;
        private bool _allPropertiesValid;
        private int _accessLevel;
        private int _numberOfSelectedItems;
        #endregion

        #region properites
        public RelayCommand GenerateCmdOUT { get; private set; }
        public RelayCommand GeneratePallets { get; private set; }
        public RelayCommand GenerateCmdIN { get; private set; }
        public RelayCommand RefreshCmd { get; private set; }
        public RelayCommand AddCmdMat { get; private set; }
        public RelayCommand AddCmdSeg { get; private set; }
        public RelayCommand RestartCmd { get; private set; }
        public RelayCommand DeleteCmd { get; private set; }
        public RelayCommand Confirm { get; private set; }
        public RelayCommand Cancel { get; private set; }
        public RelayCommand<IList> SelectionChangedCommand { get; private set; }

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
        public bool IsInDeleteMode
        {
            get { return _isInDeleteMode; }
            set
            {
                if (_isInDeleteMode != value)
                {
                    _isInDeleteMode = value;
                    RaisePropertyChanged("IsInDeleteMode");
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
                        DetailedContent = SelectedContent;
                        SimpleCommandList.Clear();
                        if ( DetailedContent != null)
                        {
                            List<SimpleCommand> l = _warehouse.DBService.GetSimpleCommands(DetailedContent.Command.ID, SimpleCommand.EnumStatus.Finished, null, null);
                            foreach (var cmd in l)
                            {
                                if (cmd is SimpleCraneCommand)
                                    SimpleCommandList.Add((SimpleCommandViewModel)new SimpleCommandCraneViewModel { Command = cmd });
                                else if (cmd is SimpleSegmentCommand)
                                    SimpleCommandList.Add((SimpleCommandViewModel)new SimpleCommandSegmentViewModel { Command = cmd });
                                else
                                    SimpleCommandList.Add((SimpleCommandViewModel)new SimpleCommandConveyorViewModel { Command = cmd });
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

        public CommandViewModel DetailedContent
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

        public string GenerateLoc
        {
            get { return _generateLoc; }
            set
            {
                if (value != _generateLoc)
                {
                    _generateLoc = value;
                    RaisePropertyChanged("GenerateLoc");
                }
            }
        }

        public int GenerateQuantity
        {
            get { return _generateQuantity; }
            set
            {
                if (value != _generateQuantity)
                {
                    _generateQuantity = value;
                    RaisePropertyChanged("GenerateQuantity");
                }
            }
        }
        public int PalletStart
        {
            get { return _palletStart; }
            set
            {
                if (value != _palletStart)
                {
                    _palletStart = value;
                    RaisePropertyChanged("PalletStart");
                }
            }
        }
        public bool AllPropertiesValid
        {
            get { return _allPropertiesValid; }
            set
            {
                if (value != _allPropertiesValid)
                {
                    _allPropertiesValid = value;
                    RaisePropertyChanged("AllPropertiesValid");
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

        public EnumCommandReason Reason
        {
            get { return (_detailedContent == null || !_detailedContent.Command.Reason.HasValue)? 0: (EnumCommandReason)_detailedContent.Command.Reason; }
            set
            {
                if (_detailedContent != null && _detailedContent.Command.Reason != (Command.EnumCommandReason)value)
                {
                    _detailedContent.Command.Reason = (Command.EnumCommandReason)value;
                    RaisePropertyChanged("Reason");
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
        public CommandsViewModel()
        {
            Validator = new PropertyValidator();
            SimpleCommandList = new ObservableCollection<SimpleCommandViewModel>();

            IsEnabledCC = false;
            IsEnabledField = false;
            IsInDeleteMode = false;

            _selectedCommand = CommandType.None;
            SelectedContent = null;

            GenerateCmdOUT = new RelayCommand(() => ExecuteGenerateCmdOUT(), CanExecuteGenerateCmdOUT);
            GenerateCmdIN = new RelayCommand(() => ExecuteGenerateCmdIN(), CanExecuteGenerateCmdIN);
            GeneratePallets = new RelayCommand(() => ExecuteGeneratePallets(), CanExecuteGeneratePallets);
            RefreshCmd = new RelayCommand(() => ExecuteRefresh(), CanExecuteRefresh);
            AddCmdMat = new RelayCommand(() => ExecuteAddMat(), CanExecuteAddMat);
            AddCmdSeg = new RelayCommand(() => ExecuteAddSeg(), CanExecuteAddSeg);
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
        }
        public void Initialize(BasicWarehouse warehouse)
        {
            _warehouse = warehouse;
            try
            {
                CommandList = new ObservableCollection<CommandViewModel>();
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
        public async void ExecuteGenerateCmdOUT()
        {
            try
            {
                await Task.Run(() => _warehouse.TestToOut(GenerateLoc, GenerateQuantity));
            }
            catch (Exception e)
            {
                _warehouse.AddEvent(Database.Event.EnumSeverity.Error, Database.Event.EnumType.Exception, 
                                    string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
            }
        }

        public bool CanExecuteGenerateCmdOUT()
        {
            try
            {
                return AllPropertiesValid && AccessLevel == 2;
            }
            catch (Exception e)
            {
                _warehouse.AddEvent(Database.Event.EnumSeverity.Error, Database.Event.EnumType.Exception,
                                    string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
                return false;
            }
        }
        public async void ExecuteGenerateCmdIN()
        {
            try
            {
                await Task.Run(() => _warehouse.TestToIn(PalletStart, GenerateLoc, GenerateQuantity));
            }
            catch (Exception e)
            {
                _warehouse.AddEvent(Database.Event.EnumSeverity.Error, Database.Event.EnumType.Exception,
                                    string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
            }
        }

        public bool CanExecuteGenerateCmdIN()
        {
            try
            {
                return AllPropertiesValid && AccessLevel == 2;
            }
            catch (Exception e)
            {
                _warehouse.AddEvent(Database.Event.EnumSeverity.Error, Database.Event.EnumType.Exception,
                                    string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
                return false;
            }
        }
        public async void ExecuteGeneratePallets()
        {
            try
            {
                await Task.Run(() => _warehouse.TestFillRack(GenerateLoc, GenerateQuantity));
            }
            catch (Exception e)
            {
                _warehouse.AddEvent(Database.Event.EnumSeverity.Error, Database.Event.EnumType.Exception,
                                    string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
            }
        }

        public bool CanExecuteGeneratePallets()
        {
            try
            {
                return AllPropertiesValid && AccessLevel == 2;
            }
            catch (Exception e)
            {
                _warehouse.AddEvent(Database.Event.EnumSeverity.Error, Database.Event.EnumType.Exception,
                                    string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
                return false;
            }
        }
        public void ExecuteRefresh()
        {
            try
            {
                CommandViewModel cmd = SelectedContent;


                var res = new ObservableCollection<CommandViewModel>();
                CommandList.Clear();
                foreach (var c in _warehouse.DBService.GetCommands(Command.EnumCommandStatus.Active, DateTime.Now.AddMinutes(-30), DateTime.Now))
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
                if( cmd != null )
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
                return !IsEnabledCC;
            }
            catch (Exception e)
            {
                _warehouse.AddEvent(Database.Event.EnumSeverity.Error, Database.Event.EnumType.Exception,
                                    string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
                return false;
            }
        }
        public void ExecuteAddMat()
        {
            try
            {
                IsEnabledCC = true;
                IsEnabledField = true;
                _selectedCommand = CommandType.AddMaterial;
                _manageContent = new CommandMaterialViewModel { Command = new CommandMaterial() };
                _manageContent.Initialize(_warehouse);
                DetailedContent = _manageContent;
                DetailedContent.ValidationEnabled = true;
            }
            catch (Exception e)
            {
                _warehouse.AddEvent(Database.Event.EnumSeverity.Error, Database.Event.EnumType.Exception,
                                    string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
            }
        }

        public bool CanExecuteAddMat()
        {
            try
            {
                return !IsEnabledCC && AccessLevel >= 1;
            }
            catch (Exception e)
            {
                _warehouse.AddEvent(Database.Event.EnumSeverity.Error, Database.Event.EnumType.Exception,
                                    string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
                return false;
            }
        }

        public void ExecuteAddSeg()
        {
            try
            {
                IsEnabledCC = true;
                IsEnabledField = true;
                _selectedCommand = CommandType.AddSegment;
                _manageContent = new CommandSegmentViewModel { Command = new CommandSegment() };
                _manageContent.Initialize(_warehouse);
                _manageContent.Command.Task = Command.EnumCommandTask.SegmentInfo;
                DetailedContent = _manageContent;
                DetailedContent.ValidationEnabled = true;
            }
            catch (Exception e)
            {
                _warehouse.AddEvent(Database.Event.EnumSeverity.Error, Database.Event.EnumType.Exception,
                                    string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
            }
        }

        public bool CanExecuteAddSeg()
        {
            try
            {
                return !IsEnabledCC && AccessLevel >= 1;
            }
            catch (Exception e)
            {
                _warehouse.AddEvent(Database.Event.EnumSeverity.Error, Database.Event.EnumType.Exception,
                                    string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
                return false;
            }
        }

        public void ExecuteRestart()
        {
            try
            {
                if (SelectedContent != null)
                {
                    IsEnabledCC = true;
                    IsEnabledField = false;
                    if (DetailedContent != null)
                        DetailedContent.ValidationEnabled = false;
                    _selectedCommand = CommandType.Restart;
                    DetailedContent = SelectedContent;
                }
            }
            catch (Exception e)
            {
                _warehouse.AddEvent(Database.Event.EnumSeverity.Error, Database.Event.EnumType.Exception,
                                    string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
            }
        }
        public bool CanExecuteRestart()
        {
            try
            {
                return !IsEnabledCC && (SelectedContent != null) && (SelectedContent.Command.Status != Command.EnumCommandStatus.Active) && AccessLevel >= 1;
            }
            catch (Exception e)
            {
                _warehouse.AddEvent(Database.Event.EnumSeverity.Error, Database.Event.EnumType.Exception,
                                    string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
                return false;
            }
        }
        public void ExecuteDelete()
        {
            try
            {
                if (SelectedContent != null)
                {
                    IsEnabledCC = true;
                    IsEnabledField = false;
                    IsInDeleteMode = true;
                    if (DetailedContent != null)
                        DetailedContent.ValidationEnabled = false;
                    _selectedCommand = CommandType.Delete;
                    Reason = EnumCommandReason.OK;
                    DetailedContent = SelectedContent;
                }
            }
            catch (Exception e)
            {
                _warehouse.AddEvent(Database.Event.EnumSeverity.Error, Database.Event.EnumType.Exception,
                                    string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
            }
        }

        public bool CanExecuteDelete()
        {
            try
            {
                return !IsEnabledCC && (SelectedContent != null) && AccessLevel >= 1;
            }
            catch (Exception e)
            {
                _warehouse.AddEvent(Database.Event.EnumSeverity.Error, Database.Event.EnumType.Exception,
                                    string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
                return false;
            }
        }

        public void ExecuteCancel()
        {
            try
            {
                IsEnabledField = false;
                IsInDeleteMode = false;
                if (DetailedContent != null)
                    DetailedContent.ValidationEnabled = false;
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
        public bool CanExecuteCancel()
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
        public void ExecuteConfirm()
        {
            IsEnabledField = false;
            IsEnabledCC = false;
            IsInDeleteMode = false;
            try
            {
                switch (_selectedCommand)
                {
                    case CommandType.AddMaterial:
                        DetailedContent.Time = DateTime.Now;
                        CommandMaterial cmd = DetailedContent.Command as CommandMaterial;
                        if (cmd.Material != null)
                            _warehouse.DBService.FindMaterialID(cmd.Material.Value, true);
                        _warehouse.DBService.AddCommand(cmd);
                        CommandList.Add(DetailedContent);
                        break;
                    case CommandType.AddSegment:
                        DetailedContent.Time = DateTime.Now;
                        _warehouse.DBService.AddCommand(DetailedContent.Command as CommandSegment);
                        CommandList.Add(DetailedContent);
                        break;
                    case CommandType.Restart:
                        DetailedContent.Time = DateTime.Now;
                        DetailedContent.Status = EnumCommandStatus.Waiting;
                        _warehouse.DBService.UpdateCommand(DetailedContent.Command);
                        break;
                    case CommandType.Delete:
                        _warehouse.DBService.UpdateCommand(DetailedContent.Command); 
                        _warehouse.DBService.AddCommand(new CommandCommand
                        {
                            Task = Command.EnumCommandTask.CancelCommand,
                            CommandID = DetailedContent.ID,
                            Priority = 0,
                            Status = Command.EnumCommandStatus.NotActive,
//                            Reason = DetailedContent.Command.Reason,
                            Time = DateTime.Now
                        });
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

        public bool CanExecuteConfirm()
        {
            try
            {
                return IsEnabledCC && (DetailedContent != null) && AllPropertiesValid &&
                       (DetailedContent.AllPropertiesValid || _selectedCommand == CommandType.Delete ) &&
                        AccessLevel >= 1;
            }
            catch (Exception e)
            {
                _warehouse.AddEvent(Database.Event.EnumSeverity.Error, Database.Event.EnumType.Exception,
                                    string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
                return false;
            }
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
                    if (_warehouse.DBService != null)
                    {
                        string validationResult = String.Empty;

                        switch (propertyName)
                        {
                            case "Reason":
                                if( IsInDeleteMode && _detailedContent.Command.Reason == Command.EnumCommandReason.OK)
                                    validationResult = ResourceReader.GetString("ERR_REASON");
                                break;
                            case "GenerateLoc":
                                if (GenerateLoc != null && (GenerateLoc.StartsWith("W:5") || !GenerateLoc.StartsWith("W")))
                                    validationResult = ResourceReader.GetString("ERR_RACKNOTOK");
                                break;
                        }
                        Validator.AddOrUpdate(propertyName, validationResult == String.Empty);
                        AllPropertiesValid = Validator.IsValid();
                        return validationResult;
                    }
                    Validator.AddOrUpdate(propertyName, false);
                    AllPropertiesValid = Validator.IsValid();
                    return ResourceReader.GetString("ERR_NULL");

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
        public void ExecuteViewActivated(ViewModelBase vm)
        {
            try
            {
                if (vm is CommandsViewModel)
                {
                    ExecuteRefresh();
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