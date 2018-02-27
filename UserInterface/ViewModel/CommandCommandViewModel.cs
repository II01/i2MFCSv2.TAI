using Database;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.CommandWpf;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using UserInterface.Services;
using Warehouse.ConveyorUnits;
using Warehouse.Model;

namespace UserInterface.ViewModel
{
    public sealed class CommandCommandViewModel : CommandViewModel, IDataErrorInfo
    {
        #region properties
        public EnumCommandCommandTask TaskCommand
        {
            get { return (EnumCommandCommandTask)_command.Task; }
            set
            {
                if (_command.Task != (Command.EnumCommandTask)value)
                {
                    _command.Task = (Command.EnumCommandTask)value;
                    RaisePropertyChanged("TaskCommand");
                }
            }
        }

        public int? CommandID
        {
            get { return (_command as CommandCommand).CommandID; }
            set
            {
                if ((_command as CommandCommand).CommandID != value)
                {
                    (_command as CommandCommand).CommandID = value;
                    RaisePropertyChanged("CommandID");
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
        public CommandCommandViewModel() : base()
        {
        }

        #endregion

        #region functions
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
