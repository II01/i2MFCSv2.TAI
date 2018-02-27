using Database;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.CommandWpf;
using System;
using System.Diagnostics;
using UserInterface.Services;
using Warehouse.Model;

namespace UserInterface.ViewModel
{
    public abstract class SimpleCommandViewModel : ViewModelBase
    {
        #region members
        protected BasicWarehouse _warehouse;
        protected SimpleCommand _command;
        protected bool _allPropertiesValid;
        protected bool _isMoveTask;
        #endregion

        #region properties
        public RelayCommand GetBarcode { get; set; }
        public RelayCommand GetLocation { get; set; }
        public bool ValidationEnabled { get; set; }
        public PropertyValidator Validator { get; set; }
        public virtual SimpleCommand Command
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
        public int? Command_ID
        {
            get { return _command.Command_ID; }
            set
            {
                if (_command.Command_ID != value)
                {
                    _command.Command_ID = value;
                    RaisePropertyChanged("Command_ID");
                }
            }
        }

        public virtual int? Material
        {
            get { return _command.Material; }
            set
            {
                if (_command.Material != value)
                {
                    _command.Material = value;
                    RaisePropertyChanged("Material");
                }
            }
        }
        public virtual string Source
        {
            get { return _command.Source; }
            set
            {
                if (_command.Source != value)
                {
                    _command.Source = value;
                    RaisePropertyChanged("Source");
                }
            }
        }
        public EnumSimpleCommandStatus Status
        {
            get { return (EnumSimpleCommandStatus)_command.Status; }
            set
            {
                if (_command.Status != (SimpleCommand.EnumStatus)value)
                {
                    _command.Status = (SimpleCommand.EnumStatus)value;
                    RaisePropertyChanged("Status");
                }
            }
        }
        public virtual EnumSimpleCommandTask Task
        {
            get { return (EnumSimpleCommandTask)_command.Task; }
            set
            {
                if (_command.Task != (SimpleCommand.EnumTask)value)
                {
                    _command.Task = (SimpleCommand.EnumTask)value;
                    RaisePropertyChanged("Task");
                }
            }
        }
        public DateTime Time
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

        public virtual string TaskDescription
        {
            get { return ""; }
            set { }
        }

        public virtual bool IsMoveTask
        {
            get { return _isMoveTask; }
            set
            {
                if(_isMoveTask != value)
                {
                    _isMoveTask = value;
                    RaisePropertyChanged("IsMoveTask");
                }
            }
        }

        #endregion

        #region initialization
        public SimpleCommandViewModel()
        {
        }
        public virtual void Initialize(BasicWarehouse warehouse)
        {
            try
            {
                _warehouse = warehouse;
                Validator = new PropertyValidator();
                IsMoveTask = false;
                AllPropertiesValid = false;
                ValidationEnabled = false;
            }
            catch (Exception e)
            {
                _warehouse.AddEvent(Database.Event.EnumSeverity.Error, Database.Event.EnumType.Exception, e.Message);
                throw new Exception(string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
            }
        }
        #endregion
    }
}
