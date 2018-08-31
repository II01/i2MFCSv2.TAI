using Database;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using UserInterface.Services;
using Warehouse.Model;

namespace UserInterface.ViewModel
{
    public class SimpleCommandSegmentViewModel : SimpleCommandViewModel, IDataErrorInfo
    {
        #region properties
        public override string TaskDescription
        {
            get
            {
                string[] tt = TaskSegment.GetType().ToString().Split('.');
                string tstr = string.Format("{0}_{1}", tt[tt.Count() - 1], TaskSegment.ToString());
                return string.Format("{0}: {1}", ResourceReader.GetString(tstr), Segment);
            }
            set { }
        }
        public string Segment
        {
            get { return (_command as SimpleSegmentCommand).Segment; }
            set
            {
                if ((_command as SimpleSegmentCommand).Segment != value)
                {
                    (_command as SimpleSegmentCommand).Segment = value;
                    RaisePropertyChanged("Segment");
                }
            }
        }

        public EnumSimpleCommandSegmentTask TaskSegment
        {
            get { return (EnumSimpleCommandSegmentTask)_command.Task; }
            set
            {
                if (_command.Task != (SimpleCommand.EnumTask)value)
                {
                    _command.Task = (SimpleCommand.EnumTask)value;
                    RaisePropertyChanged("TaskSegment");
                }
            }
        }

        public List<String> Segments { get; set; }
        #endregion

        #region initialization
        public SimpleCommandSegmentViewModel() : base()
        {
        }

        public override void Initialize(BasicWarehouse warehouse)
        {
            try
            {
                base.Initialize(warehouse);
                Segments = _warehouse.SegmentList.ConvertAll(n => n.Name);
                Segments.Sort();
            }
            catch (Exception e)
            {
                _warehouse.AddEvent(Database.Event.EnumSeverity.Error, Database.Event.EnumType.Exception, e.Message);
                throw new Exception(string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
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
                            case "TaskSegment":
                                if (TaskSegment < EnumSimpleCommandSegmentTask.Reset)
                                    validationResult = ResourceReader.GetString("ERR_TASK");
                                break;
                            case "Segment":
                                if (!Segments.Any(p => p == Segment))
                                    validationResult = ResourceReader.GetString("ERR_SEGMENT");
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
