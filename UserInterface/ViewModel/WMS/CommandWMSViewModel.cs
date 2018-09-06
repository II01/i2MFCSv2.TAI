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
    public sealed class CommandWMSViewModel: ViewModelBase, IDataErrorInfo
    {
        #region members
        private CommandWMSOrder _data;
        private bool _allPropertiesValid = false;
        private DBServiceWMS _dbservicewms;
        private BasicWarehouse _warehouse;
        private bool _validationEnabled;
        #endregion

        #region properties
        PropertyValidator Validator { get; set; }
        public CommandWMSOrder Data
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
        public int WMSID
        {
            get { return _data.ID;  }
            set
            {
                if( _data.ID != value )
                {
                    _data.ID = value;
                    RaisePropertyChanged("WMSID");
                }
            }
        }

        public int OrderID
        {
            get { return _data.Order_ID; }
            set
            {
                if( _data.Order_ID != value)
                {
                    _data.Order_ID = value;
                    RaisePropertyChanged("OrderID");
                }
            }
        }

        public int TUID
        {
            get { return _data.TU_ID; }
            set
            {
                if (_data.TU_ID != value)
                {
                    _data.TU_ID = value;
                    RaisePropertyChanged("TUID");
                }
            }
        }
        public string BoxID
        {
            get { return _data.Box_ID; }
            set
            {
                if (_data.Box_ID != value)
                {
                    _data.Box_ID = value;
                    RaisePropertyChanged("BoxID");
                }
            }
        }
        public string Source
        {
            get { return _data.Source; }
            set
            {
                if (_data.Source != value)
                {
                    _data.Source = value;
                    RaisePropertyChanged("Source");
                }
            }
        }
        public string Target
        {
            get { return _data.Target; }
            set
            {
                if (_data.Target != value)
                {
                    _data.Target = value;
                    RaisePropertyChanged("Target");
                }
            }
        }

        public EnumOrderOperation Operation
        {
            get { return (EnumOrderOperation)_data.Operation; }
            set
            {
                if (_data.Operation != (int)value)
                {
                    _data.Operation = (int)value;
                    RaisePropertyChanged("Operation");
                }
            }
        }

        public EnumCommandWMSStatus Status
        {
            get { return (EnumCommandWMSStatus)_data.Status; }
            set
            {
                if (_data.Status != (int)value)
                {
                    _data.Status = (int)value;
                    RaisePropertyChanged("Status");
                }
            }
        }

        public DateTime Time
        {
            get { return _data.Time; }
            set
            {
                if (_data.Time != value)
                {
                    _data.Time = value;
                    RaisePropertyChanged("Time");
                }
            }
        }
        public int? OrderERPID
        {
            get { return _data.OrderERPID; }
            set
            {
                if (_data.OrderERPID != value)
                {
                    _data.OrderERPID = value;
                    RaisePropertyChanged("OrderERPID");
                }
            }
        }

        public int OrderOrderID
        {
            get { return _data.OrderOrderID; }
            set
            {
                if (_data.OrderOrderID != value)
                {
                    _data.OrderOrderID = value;
                    RaisePropertyChanged("OrderOrderID");
                }
            }
        }
        public int OrderSubOrderID
        {
            get { return _data.OrderSubOrderID; }
            set
            {
                if (_data.OrderSubOrderID != value)
                {
                    _data.OrderSubOrderID = value;
                    RaisePropertyChanged("OrderSubOrderID");
                }
            }
        }
        public int OrderSubOrderERPID
        {
            get { return _data.OrderSubOrderERPID; }
            set
            {
                if (_data.OrderSubOrderERPID != value)
                {
                    _data.OrderSubOrderERPID = value;
                    RaisePropertyChanged("OrderSubOrderERPID");
                }
            }
        }
        public string OrderSubOrderName
        {
            get { return _data.OrderSubOrderName; }
            set
            {
                if (_data.OrderSubOrderName != value)
                {
                    _data.OrderSubOrderName = value;
                    RaisePropertyChanged("OrderSubOrderName");
                }
            }
        }
        public string OrderSKUID
        {
            get { return _data.OrderSKUID; }
            set
            {
                if (_data.OrderSKUID != value)
                {
                    _data.OrderSKUID = value;
                    RaisePropertyChanged("OrderSKUID");
                }
            }
        }
        public string OrderSKUBatch
        {
            get { return _data.OrderSKUBatch; }
            set
            {
                if (_data.OrderSKUBatch != value)
                {
                    _data.OrderSKUBatch = value;
                    RaisePropertyChanged("OrderSKUBatch");
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
        public CommandWMSViewModel()
        {
            _data = new CommandWMSOrder();
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
                            case "WMSID":
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
