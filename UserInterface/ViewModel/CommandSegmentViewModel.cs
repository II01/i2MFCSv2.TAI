using Database;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.CommandWpf;
using System;
using System.ComponentModel;
using System.Diagnostics;
using UserInterface.Services;
using Warehouse.ConveyorUnits;
using Warehouse.Model;

namespace UserInterface.ViewModel
{
    public sealed class CommandSegmentViewModel : CommandViewModel, IDataErrorInfo
    {

        #region properties
        public EnumCommandSegmentTask TaskSegment
        {
            get { return (EnumCommandSegmentTask)_command.Task; }
            set
            {
                if (_command.Task != (Command.EnumCommandTask)value)
                {
                    _command.Task = (Command.EnumCommandTask)value;
                    RaisePropertyChanged("TaskSegment");
                }
            }
        }
        public string Segment
        {
            get { return (_command as CommandSegment).Segment; }
            set
            {
                if ((_command as CommandSegment).Segment != value)
                {
                    (_command as CommandSegment).Segment = value;
                    RaisePropertyChanged("Segment");
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
        public CommandSegmentViewModel() : base()
        {
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
                    if(_warehouse.DBService != null)
                    {
                        string validationResult = String.Empty;
                        if (ValidationEnabled)
                        {
                            switch (propertyName)
                            {
                                case "Task":
                                    if (Task < EnumCommandTask.SegmentInfo || Task > EnumCommandTask.SegmentReset)
                                        validationResult = ResourceReader.GetString("ERR_TASK");
                                    break;
                                case "Segment":
                                    if (!_warehouse.SegmentList.Exists(p => p.Name == (_command as CommandSegment).Segment) && Segment != "*")
                                        validationResult = ResourceReader.GetString("ERR_SEGMENT");
                                    break;
                                case "Priority":
                                    if (Priority < 0 || Priority > 10)
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
