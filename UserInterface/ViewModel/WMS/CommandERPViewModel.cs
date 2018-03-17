using System;
using GalaSoft.MvvmLight;
using UserInterface.Services;
using DatabaseWMS;
using System.ComponentModel;
using Warehouse.Model;
using System.Diagnostics;
using UserInterface.DataServiceWMS;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Xml;
using System.Text;

namespace UserInterface.ViewModel
{
    public sealed class CommandERPViewModel: ViewModelBase, IDataErrorInfo
    {
        #region members
        private CommandERPs _data;
        private bool _allPropertiesValid = false;
        private DBServiceWMS _dbservicewms;
        private BasicWarehouse _warehouse;
        private bool _validationEnabled;
        private ObservableCollection<CommandERPs> _detailList;
        string _commandAsXML;
        #endregion

        #region properties
        PropertyValidator Validator { get; set; }
        public CommandERPs Data
        {
            get { return _data; }
            set
            {
                if (_data != value)
                {
                    _data = value;
                    RaisePropertyChanged("Data");
                }
            }
        }
        public int ERPID
        {
            get { return _data.ID;  }
            set
            {
                if( _data.ID != value )
                {
                    _data.ID = value;
                    RaisePropertyChanged("ERPID");
                }
            }
        }

        public string Command
        {
            get { return _data.Command; }
            set
            {
                if( _data.Command != value)
                {
                    _data.Command = value;
                    RaisePropertyChanged("Command");
                    try
                    {
                        var doc = new XmlDocument();
                        doc.LoadXml(value);
                        var stringBuilder = new StringBuilder();
                        var xmlWriterSettings = new XmlWriterSettings { Indent = true, OmitXmlDeclaration = true };
                        doc.Save(XmlWriter.Create(stringBuilder, xmlWriterSettings));
                        CommandAsXML = stringBuilder.ToString();
                    }
                    catch
                    {
                        CommandAsXML = value;
                    }
                }
            }
        }

        public string CommandAsXML
        {
            get { return _commandAsXML; }
            set
            {
                if (_commandAsXML != value)
                {
                    _commandAsXML = value;
                    RaisePropertyChanged("CommandAsXML");
                }
            }
        }

        public EnumCommandERPStatus Status
        {
            get { return (EnumCommandERPStatus)_data.Status; }
            set
            {
                if (_data.Status != (int)value)
                {
                    _data.Status = (int)value;
                    RaisePropertyChanged("Status");
                }
            }
        }

        public bool ValidationEnabled
        {
            get { return _validationEnabled; }
            set
            {
                if (_validationEnabled != value)
                {
                    _validationEnabled = value;
                    RaisePropertyChanged("ValidationEnabled");
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
        public CommandERPViewModel()
        {
            _data = new CommandERPs();
            Validator = new PropertyValidator();
            ValidationEnabled = false;
        }
        public void Initialize(BasicWarehouse warehouse)
        {
            try
            {
                _warehouse = warehouse;
                _dbservicewms = new DBServiceWMS(warehouse);
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
            get { return (this as IDataErrorInfo).Error; }
        }

        public string this[string propertyName]
        {
            get
            {
                try
                {
                    string validationResult = String.Empty;
                    if( ValidationEnabled )
                    {
                        switch (propertyName)
                        {
                            case "ERPID":
                                break;
                            case "Command":
                                break;
                            case "Status":
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
