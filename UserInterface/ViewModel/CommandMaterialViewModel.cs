using Database;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.CommandWpf;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using UserInterface.Services;
using Warehouse.Common;
using Warehouse.ConveyorUnits;
using Warehouse.Model;
using WCFClients;

namespace UserInterface.ViewModel
{
    public sealed class CommandMaterialViewModel : CommandViewModel, IDataErrorInfo
    {
        #region members
        private bool _isTargetEnabled;
        private string _materialStr;
        #endregion

        #region properties
        public RelayCommand GetBarcode { get; set; }
        public RelayCommand GetLocation { get; set; }
        public EnumCommandTUTask TaskTU
        {
            get { return (EnumCommandTUTask)_command.Task; }
            set
            {
                if (_command.Task != (Command.EnumCommandTask)value)
                {
                    _command.Task = (Command.EnumCommandTask)value;
                    RaisePropertyChanged("TaskTU");
                    IsTargetEnabled = TaskTU == EnumCommandTUTask.Move;
                    if (!IsTargetEnabled)
                        Target = Source;
                }
            }
        }
        public override Command Command
        {
            get { return _command; }
            set
            {
                if (_command != value)
                {
                    _command = value;
                    MaterialStr = ((_command as CommandMaterial).Material.HasValue && (_command as CommandMaterial).Material.Value > 0) ?
                                  string.Format("P{0:d9}", (_command as CommandMaterial).Material.Value) : "";
                    RaisePropertyChanged("Command");
                }
            }
        }

        public int? Material
        {
            get { return (_command as CommandMaterial).Material; }
            set
            {
                if ((_command as CommandMaterial).Material != value)
                {
                    (_command as CommandMaterial).Material = value;
                    MaterialStr = ((_command as CommandMaterial).Material.HasValue && (_command as CommandMaterial).Material.Value > 0) ?
                                  string.Format("P{0:d9}", (_command as CommandMaterial).Material.Value) : "";
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
                    if (_materialStr.Length > 0 && _materialStr[0] != 'P')
                        _materialStr = 'P' + _materialStr;
                    if (_materialStr.Length == 10 && Int32.TryParse(_materialStr.Substring(1), out int m))
                        (_command as CommandMaterial).Material = m;
                    else
                        (_command as CommandMaterial).Material = 0;
                    RaisePropertyChanged("MaterialStr");
                }
            }
        }

        public string Source
        {
            get { return (_command as CommandMaterial).Source; }
            set
            {
                if ((_command as CommandMaterial).Source != value)
                {
                    (_command as CommandMaterial).Source = value;
                    RaisePropertyChanged("Source");
                    if (!IsTargetEnabled)
                        Target = Source;
                    RaisePropertyChanged("Target");
                }
            }
        }
        public string Target
        {
            get { return (_command as CommandMaterial).Target; }
            set
            {
                if ((_command as CommandMaterial).Target != value)
                {
                    (_command as CommandMaterial).Target = value;
                    RaisePropertyChanged("Target");
                    RaisePropertyChanged("Source");
                }
            }
        }
        public bool IsTargetEnabled
        {
            get { return _isTargetEnabled; }
            set
            {
                if (_isTargetEnabled != value)
                {
                    _isTargetEnabled = value;
                    RaisePropertyChanged("IsTargetEnabled");
                }
            }
        }
        #endregion

        #region initialization
        public override void Initialize(BasicWarehouse warehouse)
        {
            try
            {
                base.Initialize(warehouse);
            }
            catch (Exception e)
            {
                _warehouse.AddEvent(Database.Event.EnumSeverity.Error, Database.Event.EnumType.Exception, e.Message);
                throw new Exception(string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
            }
        }
        public CommandMaterialViewModel() : base()
        {
            _isTargetEnabled = true;
            GetBarcode = new RelayCommand(ExecuteGetBarcode, CanExecuteGetBarcode);
            GetLocation = new RelayCommand(ExecuteGetLocation, CanExecuteGetLocation);
        }

        #endregion

        #region functions
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
                return _warehouse.DBService.FindPlaceID(Source) != null;
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
                if (Material.HasValue)
                {
                    Place p = _warehouse.DBService.FindMaterial(Material.Value);
                    if (p != null)
                        Source = p.Place1;
                    else
                        Source = "";
                }
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
                if (Material.HasValue)
                    return _warehouse.DBService.FindMaterial(Material.Value) != null;
                else
                    return false;
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
            get { return (_command as IDataErrorInfo).Error; }
        }

        public string this[string propertyName]
        {
            get
            {
                try
                {
                    LPosition p;
                    string frontLoc;

                    if (_warehouse.DBService != null)
                    {
                        string validationResult = String.Empty;
                        if (ValidationEnabled)
                        {
                            switch (propertyName)
                            {
                                case "TaskTU":
                                    if (TaskTU >= EnumCommandTUTask.InfoMaterial)
                                        validationResult = ResourceReader.GetString("ERR_TASK");
                                    break;
                                case "MaterialStr":
                                    if (TaskTU == EnumCommandTUTask.Move ||
                                        TaskTU == EnumCommandTUTask.CreateMaterial ||
                                        (TaskTU == EnumCommandTUTask.DeleteMaterial && Material != null))
                                    {
                                        if (!Material.HasValue || Material <= 0)
                                            validationResult = ResourceReader.GetString("ERR_MATERIALNOTVALID");
                                        else if (TaskTU != EnumCommandTUTask.CreateMaterial && _warehouse.DBService.FindMaterial(Material.Value) == null)
                                            validationResult = ResourceReader.GetString("ERR_MATERIALNOTEXISTS");
                                        else if (TaskTU == EnumCommandTUTask.CreateMaterial && _warehouse.DBService.FindMaterial(Material.Value) != null)
                                            validationResult = ResourceReader.GetString("ERR_MATERIALEXISTS");
                                    }
                                    break;
                                case "Source":
                                    if (_warehouse.DBService.FindPlaceID(Source) == null)
                                        validationResult = ResourceReader.GetString("ERR_LOCATION");
                                    else if (TaskTU == EnumCommandTUTask.Move && !(_warehouse.WCFClient as WCFUIClient).NotifyUIClient.RouteExists(Source, Target, false))
                                        validationResult = ResourceReader.GetString("ERR_ROUTE");
                                    else
                                    {
                                        p = LPosition.FromString(Source);
                                        frontLoc = Source;
                                        if (p.Shelve > 0 && p.Depth == 2)
                                        {
                                            LPosition pOther = new LPosition { Shelve = p.Shelve, Travel = p.Travel, Height = p.Height, Depth = 1 };
                                            frontLoc = pOther.ToString();
                                        }
                                        if (_warehouse.DBService.FindPlaceID(Source).Blocked || _warehouse.DBService.FindPlaceID(frontLoc).Blocked)
                                            validationResult = ResourceReader.GetString("ERR_BLOCKED");
                                    }
                                    break;
                                case "Target":
                                    if (_warehouse.DBService.FindPlaceID(Target) == null)
                                        validationResult = ResourceReader.GetString("ERR_LOCATION");
                                    else if (TaskTU == EnumCommandTUTask.Move && !(_warehouse.WCFClient as WCFUIClient).NotifyUIClient.RouteExists(Source, Target, false))
                                        validationResult = ResourceReader.GetString("ERR_ROUTE");
                                    else
                                    {
                                        p = LPosition.FromString(Target);
                                        frontLoc = Target;
                                        if (p.Shelve > 0 && p.Depth == 2)
                                            frontLoc = frontLoc.Substring(0, frontLoc.Length - 1) + '1';
                                        if (_warehouse.DBService.FindPlaceID(Target).Blocked || _warehouse.DBService.FindPlaceID(frontLoc).Blocked)
                                            validationResult = ResourceReader.GetString("ERR_BLOCKED");
                                    }
                                    break;
                                case "Priority":
                                    if (Priority < 0 || Priority > 100)
                                        validationResult = ResourceReader.GetString("ERR_PRIORITY");
                                    break;
                            }
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

    }
}
