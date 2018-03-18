using Database;
using GalaSoft.MvvmLight.CommandWpf;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using UserInterface.Services;
using Warehouse.Model;

namespace UserInterface.ViewModel
{
    public class SimpleCommandCraneViewModel : SimpleCommandViewModel, IDataErrorInfo
    {
        #region members
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
                    MaterialStr = (_command.Material.HasValue && _command.Material.Value > 0) ? string.Format("{0:d9}", _command.Material.Value) : "";
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
                    MaterialStr = (_command.Material.HasValue && _command.Material.Value > 0) ? string.Format("{0:d9}", _command.Material.Value) : "";
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
//                    if (_materialStr.Length == 10 && Int32.TryParse(_materialStr.Substring(1), out int m))
                    if (Int32.TryParse(_materialStr, out int m))
                        _command.Material = m;
                    else
                        _command.Material = 0;
                    RaisePropertyChanged("MaterialStr");
                }
            }
        }
        public string Unit
        {
            get { return (_command as SimpleCraneCommand).Unit; }
            set
            {
                try
                {
                    if ((_command as SimpleCraneCommand).Unit != value)
                    {
                        (_command as SimpleCraneCommand).Unit = value;
                        RaisePropertyChanged("Unit");
                    }
                }
                catch (Exception e)
                {
                    _warehouse.AddEvent(Database.Event.EnumSeverity.Error, Database.Event.EnumType.Exception, 
                                        string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
                }
            }
        }
        public EnumSimpleCommandCraneTask TaskCrane
        {
            get { return (EnumSimpleCommandCraneTask)_command.Task; }
            set
            {
                if (_command.Task != (SimpleCommand.EnumTask)value)
                {
                    _command.Task = (SimpleCommand.EnumTask)value;
                    RaisePropertyChanged("TaskCrane");
                }
            }
        }

        public override string TaskDescription
        {
            get
            {
                if ((_command as SimpleCraneCommand).Unit != null)
                {
                    string[] tt = TaskCrane.GetType().ToString().Split('.');
                    string tstr = string.Format("{0}_{1}", tt[tt.Count() - 1], TaskCrane.ToString());
                    return string.Format("{0} {1}: {2} {3}: {4} {5}:{6}",
                            ResourceReader.GetString(tstr),
                            ResourceReader.GetString("TU"),
//                            _command.Material.HasValue ? string.Format("P{0:d9}", _command.Material.Value) : "",
                            _command.Material.HasValue ? string.Format("{0:d9}", _command.Material.Value) : "",
                            ResourceReader.GetString("with"),
                            (_command as SimpleCraneCommand).Unit,
                            ResourceReader.GetString("Location"),
                            _command.Source);
                }
                return "";
            }
            set { }
        }

        public List<String> Devices { get; set; }
        #endregion

        #region initialization
        public SimpleCommandCraneViewModel() : base()
        {
            GetBarcode = new RelayCommand(ExecuteGetBarcode, CanExecuteGetBarcode);
            GetLocation = new RelayCommand(ExecuteGetLocation, CanExecuteGetLocation);
        }

        public override void Initialize(BasicWarehouse warehouse)
        {
            try
            {
                base.Initialize(warehouse);
                Devices = _warehouse.CraneList.ConvertAll(n => n.Name);
                Devices.Sort();
            }
            catch (Exception e)
            {
                _warehouse.AddEvent(Database.Event.EnumSeverity.Error, Database.Event.EnumType.Exception, e.Message);
                throw new Exception(string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
            }
        }

        #endregion

        #region commands
        public void ExecuteGetBarcode()
        {
            try
            {
                if (TaskCrane == EnumSimpleCommandCraneTask.Drop)
                {
                    Place p = _warehouse.DBService.FindPlace((_command as SimpleCraneCommand).Unit);
                    if (p != null)
                        Material = p.Material;
                    else
                        Material = 0;
                }
                else
                {
                    Place p = _warehouse.DBService.FindPlace(Source);
                    if (p != null)
                        Material = p.Material;
                    else
                        Material = 0;

                }
            }
            catch (Exception e)
            {
                _warehouse.AddEvent(Database.Event.EnumSeverity.Error, Database.Event.EnumType.Exception,
                                    string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
            }
        }
        public bool CanExecuteGetBarcode()
        {
            try
            {
                return Validator.IsValid("Source") || TaskCrane == EnumSimpleCommandCraneTask.Drop;
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
                return Validator.IsValid("MaterialStr") && TaskCrane == EnumSimpleCommandCraneTask.Pick;
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
                    if(ValidationEnabled)
                    {
                        switch (propertyName)
                        {
                            case "TaskCrane":
                                if (TaskCrane != EnumSimpleCommandCraneTask.Move &&
                                    TaskCrane != EnumSimpleCommandCraneTask.Pick &&
                                    TaskCrane != EnumSimpleCommandCraneTask.Drop &&
                                    TaskCrane != EnumSimpleCommandCraneTask.Create &&
                                    TaskCrane != EnumSimpleCommandCraneTask.Delete)
                                    validationResult = ResourceReader.GetString("ERR_TASK");
                                if (TaskCrane == EnumSimpleCommandCraneTask.Move)
                                    Material = null;
                                if (TaskCrane == EnumSimpleCommandCraneTask.Create ||
                                    TaskCrane == EnumSimpleCommandCraneTask.Delete)
                                    Source = Unit;
                                RaisePropertyChanged("Source");
                                RaisePropertyChanged("MaterialStr");
                                break;
                            case "MaterialStr":
                                if (TaskCrane == EnumSimpleCommandCraneTask.Create)
                                {
                                    if (Material.HasValue && _warehouse.DBService.FindMaterial(Material.Value) != null)
                                        validationResult = ResourceReader.GetString("ERR_MATERIALEXISTS");
                                }
                                else if (TaskCrane != EnumSimpleCommandCraneTask.Move)
                                {
                                    if (!Material.HasValue || Material <= 0)
                                        validationResult = ResourceReader.GetString("ERR_MATERIALNOTVALID");
                                    else if (Material.HasValue && _warehouse.DBService.FindMaterial(Material.Value) == null)
                                        validationResult = ResourceReader.GetString("ERR_MATERIALUNKNOWN");
                                }
                                break;
                            case "Unit":
                                if (!Devices.Any(p => p == Unit))
                                    validationResult = ResourceReader.GetString("ERR_UNIT");
                                if (TaskCrane == EnumSimpleCommandCraneTask.Create || TaskCrane == EnumSimpleCommandCraneTask.Delete)
                                    Source = Unit;
                                RaisePropertyChanged("Source");
                                break;
                            case "Source":
                                if (Source != null)
                                {
                                    if (TaskCrane == EnumSimpleCommandCraneTask.Create ||
                                        TaskCrane == EnumSimpleCommandCraneTask.Delete)
                                    {
                                        if (Source != Unit)
                                            validationResult = ResourceReader.GetString("ERR_LOCATION");
                                    }
                                    else
                                    {
                                        int s = (Source.Length > 4 && Source[0] == 'W' && int.TryParse(Source.Substring(2, 2), out s)) ? s : -1;
                                        if (Devices.Any(p => p == Unit))
                                        {
                                            bool inconv = _warehouse.Crane[Unit].InConveyor != null && 
                                                          (_warehouse.Crane[Unit].InConveyor.Any(p => p.Name == Source) &&
                                                           (TaskCrane == EnumSimpleCommandCraneTask.Move ||
                                                            TaskCrane == EnumSimpleCommandCraneTask.Pick));
                                            bool outconv = _warehouse.Crane[Unit].OutConveyor != null &&
                                                           (_warehouse.Crane[Unit].OutConveyor.Any(p => p.Name == Source) &&
                                                            (TaskCrane == EnumSimpleCommandCraneTask.Move ||
                                                             TaskCrane == EnumSimpleCommandCraneTask.Drop));
                                            bool wh = _warehouse.Crane[Unit].Shelve != null && 
                                                      _warehouse.Crane[Unit].Shelve.Any(p => p == s);
                                            if (_warehouse.DBService.FindPlaceID(Source) == null)
                                                validationResult = ResourceReader.GetString("ERR_LOCATIONEXISTS");
                                            else if (!inconv && !outconv && !wh)
                                                validationResult = ResourceReader.GetString("ERR_LOCATION");
                                        }
                                    }
                                }
                                else
                                    validationResult = ResourceReader.GetString("ERR_LOCATION");
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
