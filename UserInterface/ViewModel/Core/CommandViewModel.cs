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
    public class CommandViewModel : ViewModelBase
    {
        #region members
        protected BasicWarehouse _warehouse;

        protected Command _command;
        protected bool _allPropertiesValid;
        #endregion

        #region properties
        public PropertyValidator Validator { get; set; }

        public bool ValidationEnabled { get; set; }
        public virtual Command Command
        {
            get { return _command; }
            set
            {
                if (_command != value)
                {
                    _command = value;
                    RaisePropertyChanged("Command");
                }
            }
        }

        public int ID
        {
            get { return _command.ID; }
            set
            {
                if (_command.ID != value)
                {
                    _command.ID = value;
                    RaisePropertyChanged("ID");
                }
            }
        }
        public int WMS_ID
        {
            get { return _command.WMS_ID; }
            set
            {
                if (_command.WMS_ID != value)
                {
                    _command.WMS_ID = value;
                    RaisePropertyChanged("WMS_ID");
                }
            }
        }
        public EnumCommandTask Task
        {
            get { return (EnumCommandTask)_command.Task; }
            set
            {
                if (_command.Task != (Command.EnumCommandTask)value)
                {
                    _command.Task = (Command.EnumCommandTask)value;
                    RaisePropertyChanged("Task");
                }
            }
        }

        public string TaskStr
        {
            get { return ((EnumCommandTask)_command.Task).ToString(); }
        }

        public string Info
        {
            get { return _command.Info; }
            set
            {
                if (_command.Info != value)
                {
                    _command.Info = value;
                    RaisePropertyChanged("Info");
                }
            }
        }
        public EnumCommandStatus Status
        {
            get { return (EnumCommandStatus)_command.Status; }
            set
            {
                if (_command.Status != (Command.EnumCommandStatus)value)
                {
                    _command.Status = (Command.EnumCommandStatus)value;
                    RaisePropertyChanged("Status");
                }
            }
        }
        public EnumCommandReason Reason
        {
            get { return !_command.Reason.HasValue? EnumCommandReason.OK: (EnumCommandReason)_command.Reason; }
            set
            {
                if (_command.Reason != (Command.EnumCommandReason)value)
                {
                    _command.Reason = (Command.EnumCommandReason)value;
                    RaisePropertyChanged("Reason");
                }
            }
        }
        public int Priority
        {
            get { return _command.Priority; }
            set
            {
                if (_command.Priority != value)
                {
                    _command.Priority = value;
                    RaisePropertyChanged("Priority");
                }
            }
        }
        public DateTime? Time
        {
            get { return _command.Time; }
            set
            {
                if (_command.Time != value)
                {
                    _command.Time = value;
                    RaisePropertyChanged("Time");
                }
            }
        }
        public bool AllPropertiesValid
        {
            get { return _allPropertiesValid; }
            set
            {
                if (_allPropertiesValid != value)
                {
                    _allPropertiesValid = value;
                    RaisePropertyChanged("AllPropertiesValid");
                }
            }
        }
        #endregion

        #region initialization
        public virtual void Initialize(BasicWarehouse warehouse)
        {
            _warehouse = warehouse;
            try
            {
                ValidationEnabled = false;
            }
            catch (Exception e)
            {
                _warehouse.AddEvent(Database.Event.EnumSeverity.Error, Database.Event.EnumType.Exception, e.Message);
                throw new Exception(string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
            }
        }
        public CommandViewModel()
        {
            _allPropertiesValid = false;
            Validator = new PropertyValidator();
        }

        #endregion

    }
}
