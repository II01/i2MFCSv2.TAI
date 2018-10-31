using Database;
using GalaSoft.MvvmLight.CommandWpf;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using UserInterface.Services;
using Warehouse.ConveyorUnits;
using Warehouse.Model;

namespace UserInterface.ViewModel
{
    public class SimpleCommandConveyorSourceAndTargets
    {
        public string Source { get; set; }
        public List<string> Targets { get; set; }

        public SimpleCommandConveyorSourceAndTargets()
        {
        }
    }

    public class SimpleCommandConveyorViewModel : SimpleCommandViewModel, IDataErrorInfo
    {
        #region members
        List<SimpleCommandConveyorSourceAndTargets> _conveyorCommands;
        List<string> _sources;
        List<string> _targets;
        List<string> _srcMove;
        List<string> _srcManage;
        private string _materialStr;
        #endregion

        #region properties
        public override SimpleCommand Command
        {
            get { return _command; }
            set
            {
                if (_command != value)
                {
                    _command = value;
//                    MaterialStr = (_command.Material.HasValue && _command.Material.Value > 0) ? string.Format("P{0:d9}", _command.Material.Value) : "";
                    MaterialStr = (_command.Material.HasValue && _command.Material.Value > 0) ? string.Format("{0:d5}", _command.Material.Value) : "";
                    RaisePropertyChanged("Command");
                }
            }
        }

        public override int? Material
        {
            get { return _command.Material; }
            set
            {
                if (_command.Material != value)
                {
                    _command.Material = value;
//                    MaterialStr = (_command.Material.HasValue && _command.Material.Value > 0) ? string.Format("P{0:d9}", _command.Material.Value) : "";
                    MaterialStr = (_command.Material.HasValue && _command.Material.Value > 0) ? string.Format("{0:d5}", _command.Material.Value) : "";
                    RaisePropertyChanged("Material");
                }
            }
        }
        public string MaterialStr
        {
            get { return _materialStr; }
            set
            {
                if (_materialStr != value && value.Length <= 10)
                {
                    _materialStr = value;
//                    if (_materialStr.Length > 0 && _materialStr[0] != 'P')
//                        _materialStr = 'P' + _materialStr;
                    if (Int32.TryParse(_materialStr, out int m))
                        _command.Material = m;
                    else
                        _command.Material = 0;
                    RaisePropertyChanged("MaterialStr");
                }
            }
        }

        public override string Source
        {
            get { return (_command as SimpleConveyorCommand).Source; }
            set
            {
                if ((_command as SimpleConveyorCommand).Source != value)
                {
                    (_command as SimpleConveyorCommand).Source = value;
                    RaisePropertyChanged("Source");
                    if((_command as SimpleConveyorCommand).Source != null && TaskConveyor == EnumSimpleCommandConveyorTask.Move)
                        Targets = _conveyorCommands.FirstOrDefault(p => p.Source == value)?.Targets;
                }
            }
        }
        public string Target
        {
            get { return (_command as SimpleConveyorCommand).Target; }
            set
            {
                if ((_command as SimpleConveyorCommand).Target != value)
                {
                    (_command as SimpleConveyorCommand).Target = value;
                    RaisePropertyChanged("Target");
                }
            }
        }
        public override string TaskDescription
        {
            get
            {
                string[] tt = TaskConveyor.GetType().ToString().Split('.');
                string tstr = string.Format("{0}_{1}", tt[tt.Count() - 1], TaskConveyor.ToString());

                return string.Format("{0} {1}: {2} {3}: {4} {5}: {6}",
                                     ResourceReader.GetString(tstr),
                                     ResourceReader.GetString("TU"),
//                                     _command.Material != null ? string.Format("P{0:d9}", _command.Material) : "",
                                     _command.Material != null ? string.Format("{0:d5}", _command.Material) : "",
                                     ResourceReader.GetString("From"),
                                     _command.Source ?? "",
                                     ResourceReader.GetString("To"),
                                     ((_command as SimpleConveyorCommand) != null && (_command as SimpleConveyorCommand).Target != null) ? 
                                     (_command as SimpleConveyorCommand).Target : "");
            }
            set { }
        }

        public List<String> Sources
        {
            get { return _sources; }
            set
            {
                if(_sources != value)
                {
                    _sources = value;
                    RaisePropertyChanged("Sources");
                }
            }
        }
        public List<String> Targets
        {
            get { return _targets; }
            set
            {
                if (_targets != value)
                {
                    _targets = value;
                    RaisePropertyChanged("Targets");
                }
            }
        }
        public EnumSimpleCommandConveyorTask TaskConveyor
        {
            get { return (EnumSimpleCommandConveyorTask)_command.Task; }
            set
            {
                if (_command.Task != (SimpleCommand.EnumTask)value)
                {
                    _command.Task = (SimpleCommand.EnumTask)value;
                    RaisePropertyChanged("TaskConveyor");
                    try
                    {
                        if (value == EnumSimpleCommandConveyorTask.Move)
                        {
                            IsMoveTask = true;
                            Sources = _srcMove;
                            Targets = null;
                        }
                        else
                        {
                            IsMoveTask = false;
                            Sources = _srcManage;
                            Targets = _srcManage;
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
        #endregion

        #region initialization
        public SimpleCommandConveyorViewModel() : base()
        {
            _conveyorCommands = new List<SimpleCommandConveyorSourceAndTargets>();
            GetBarcode = new RelayCommand(ExecuteGetBarcode, CanExecuteGetBarcode);
            GetLocation = new RelayCommand(ExecuteGetLocation, CanExecuteGetLocation);
        }

        public override void Initialize(BasicWarehouse warehouse)
        {
            try
            {
                base.Initialize(warehouse);
                
                PossibleConveyorCommands();

                var src = from s in _conveyorCommands
                          select s.Source;

                _srcMove = src.ToList();
                _srcManage = _warehouse.ConveyorList.ConvertAll(p => p.Name);

                if(TaskConveyor == EnumSimpleCommandConveyorTask.Move)
                {
                    _sources = _srcMove;
                    _targets = null;
                    _isMoveTask = true;
                }
                else
                {
                    _sources = _srcManage;
                    _targets = _srcManage;
                    _isMoveTask = false;
                }
            }
            catch (Exception e)
            {
                _warehouse.AddEvent(Database.Event.EnumSeverity.Error, Database.Event.EnumType.Exception, e.Message);
                throw new Exception(string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
            }
        }
        public void PossibleConveyorCommands()
        {
            var dev = from d in _warehouse.ConveyorList
                      where d is ConveyorJunction && (d as ConveyorJunction).RouteDef.FinalRouteCost != null
                      select d;
            List<Conveyor> devices = dev.ToList();
            foreach (var d in devices)
            {
                List<string> targets = new List<string>();
                foreach (var p in (d as ConveyorJunction).RouteDef.FinalRouteCost)
                {
                    var tt = from i in p.Items
                             where i.First.Name == d.Name && (i.Final is Conveyor) && !targets.Contains(i.Final.Name)
                             select i.Final.Name;
                    targets.AddRange(tt.ToList());
                }
                if (targets.Count >= 1) 
                {
                    targets.Sort();
                    _conveyorCommands.Add(new SimpleCommandConveyorSourceAndTargets { Source = d.Name, Targets = targets });
                }
            }
        }

        #endregion

        #region commands
        public void ExecuteGetBarcode()
        {
            try
            {
                Place p = _warehouse.DBService.FindPlace(Source);
                if (p != null)
                    Material = p.Material;
                else
                    Material = 0;
            }
            catch(Exception e)
            {
                _warehouse.AddEvent(Database.Event.EnumSeverity.Error, Database.Event.EnumType.Exception,
                                    string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
            }
        }
        public bool CanExecuteGetBarcode()
        {
            try
            {
                return Validator.IsValid("Source");
            }
            catch (Exception e)
            {
                _warehouse.AddEvent(Database.Event.EnumSeverity.Error, Database.Event.EnumType.Exception,
                                    string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
                return false;
            }
        }
        public void ExecuteGetLocation()
        {
            try
            {
                Place p = _warehouse.DBService.FindMaterial(Material.Value);
                if (p != null)
                    Source = p.Place1;
                else
                    Source = "";
            }
            catch (Exception e)
            {
                _warehouse.AddEvent(Database.Event.EnumSeverity.Error, Database.Event.EnumType.Exception,
                                    string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
            }
        }
        public bool CanExecuteGetLocation()
        {
            try
            {
                return Validator.IsValid("MaterialStr");
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
            get { return ResourceReader.GetString("ERR_FIELD"); }
        }

        public string this[string propertyName]
        {
            get
            {
                try
                {
                    string validationResult = String.Empty;
                    if (ValidationEnabled)
                    {
                        switch (propertyName)
                        {
                            case "TaskConveyor":
                                if (TaskConveyor != EnumSimpleCommandConveyorTask.Move &&
                                    TaskConveyor != EnumSimpleCommandConveyorTask.Create &&
                                    TaskConveyor != EnumSimpleCommandConveyorTask.Delete)
                                    validationResult = ResourceReader.GetString("ERR_TASK");
                                RaisePropertyChanged("Source");
                                break;
                            case "MaterialStr":
                                if (TaskConveyor == EnumSimpleCommandConveyorTask.Create)
                                {
                                    if (Material.HasValue && _warehouse.DBService.FindMaterial(Material.Value) != null)
                                        validationResult = ResourceReader.GetString("ERR_MATERIALEXISTS");
                                }
                                else
                                {
                                    if (!Material.HasValue || Material <= 0)
                                        validationResult = ResourceReader.GetString("ERR_MATERIALNOTVALID");
                                    else if (_warehouse.DBService.FindMaterial(Material.Value) == null)
                                        validationResult = ResourceReader.GetString("ERR_MATERIALUNKNOWN");
                                }
                                break;
                            case "Source":
                                if (Sources != null && !Sources.Any(p => p == Source))
                                    validationResult = ResourceReader.GetString("ERR_LOCATION");
                                if (TaskConveyor == EnumSimpleCommandConveyorTask.Create ||
                                    TaskConveyor == EnumSimpleCommandConveyorTask.Delete)
                                {
                                    Target = Source;
                                }
                                break;
                            case "Target":
                                if (Targets != null && !Targets.Any(p => p == Target))
                                    validationResult = ResourceReader.GetString("ERR_LOCATION");
                                if (TaskConveyor == EnumSimpleCommandConveyorTask.Create ||
                                    TaskConveyor == EnumSimpleCommandConveyorTask.Delete)
                                {
                                    Source = Target;
                                }
                                break;
                        }
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
