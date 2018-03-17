using System;
using GalaSoft.MvvmLight;
using UserInterface.Services;
using DatabaseWMS;
using System.ComponentModel;
using Warehouse.Model;
using System.Diagnostics;
using UserInterface.DataServiceWMS;
using System.Data.SqlTypes;

namespace UserInterface.ViewModel
{
    public sealed class OrderViewModel: ViewModelBase, IDataErrorInfo
    {
        #region members
        private Orders _data;
        private bool _allPropertiesValid = false;
        private DBServiceWMS _dbservicewms;
        private BasicWarehouse _warehouse;
        private bool _validationEnabled;
        private bool _enableOrderAdd;
        private bool _enableOrderEdit;
        private bool _enableSubOrderEdit;
        private bool _enableSKUEdit;
        #endregion

        #region properties
        public int ReferenceOrderID { get; set; }
        public int ReferenceSubOrderID { get; set; }
        PropertyValidator Validator { get; set; }
        public Orders Order
        {
            get { return _data; }
            set
            {
                if (_data != value)
                {
                    _data = value;
                    RaisePropertyChanged("Order");
                }
            }
        }
        public int ID
        {
            get { return _data.ID;  }
            set
            {
                if( _data.ID != value )
                {
                    _data.ID = value;
                    RaisePropertyChanged("ID");
                }
            }
        }
        public int? ERPID
        {
            get { return _data.ERP_ID; }
            set
            {
                if (_data.ERP_ID != value)
                {
                    _data.ERP_ID = value;
                    RaisePropertyChanged("ERPID");
                }
            }
        }

        public int OrderID
        {
            get { return _data.OrderID; }
            set
            {
                if (_data.OrderID != value)
                {
                    _data.OrderID = value;
                    RaisePropertyChanged("OrderID");
                }
            }
        }
        public string Destination
        {
            get { return _data.Destination; }
            set
            {
                if (_data.Destination != value)
                {
                    _data.Destination = value;
                    RaisePropertyChanged("Destination");
                }
            }
        }
        public DateTime ReleaseTime
        {
            get { return _data.ReleaseTime; }
            set
            {
                if (_data.ReleaseTime != value)
                {
                    _data.ReleaseTime = value;
                    RaisePropertyChanged("ReleaseTime");
                }
            }
        }
        public int SubOrderID
        {
            get { return _data.SubOrderID; }
            set
            {
                if(_data.SubOrderID != value)
                {
                    _data.SubOrderID = value;
                    RaisePropertyChanged("SubOrderID");
                }
            }
        }

        public string SubOrderName
        {
            get { return _data.SubOrderName; }
            set
            {
                if (_data.SubOrderName != value)
                {
                    _data.SubOrderName = value;
                    RaisePropertyChanged("SubOrderName");
                }
            }
        }

        public string SKUID
        {
            get { return _data.SKU_ID; }
            set
            {
                if (_data.SKU_ID != value)
                {
                    _data.SKU_ID = value;
                    RaisePropertyChanged("SKUID");
                }
            }
        }

        public string SKUBatch
        {
            get { return _data.SKU_Batch; }
            set
            {
                if (_data.SKU_Batch != value)
                {
                    _data.SKU_Batch = value;
                    RaisePropertyChanged("SKUBatch");
                }
            }
        }
        public double SKUQty
        {
            get { return _data.SKU_Qty; }
            set
            {
                if (_data.SKU_Qty != value)
                {
                    _data.SKU_Qty = value;
                    RaisePropertyChanged("SKUQty");
                }
            }
        }
        public EnumWMSOrderStatus Status
        {
            get { return (EnumWMSOrderStatus)_data.Status; }
            set
            {
                if (_data.Status != (int)value)
                {
                    _data.Status = (int)value;
                    RaisePropertyChanged("Status");
                }
            }
        }

        public bool EnableOrderAdd
        {
            get { return _enableOrderAdd; }
            set
            {
                if (_enableOrderAdd != value)
                {
                    _enableOrderAdd = value;
                    RaisePropertyChanged("EnableOrderAdd");
                }
            }
        }
        public bool EnableOrderEdit
        {
            get { return _enableOrderEdit; }
            set
            {
                if (_enableOrderEdit != value)
                {
                    _enableOrderEdit = value;
                    RaisePropertyChanged("EnableOrderEdit");
                }
            }
        }

        public bool EnableSubOrderEdit
        {
            get { return _enableSubOrderEdit; }
            set
            {
                if (_enableSubOrderEdit != value)
                {
                    _enableSubOrderEdit = value;
                    RaisePropertyChanged("EnableSubOrderEdit");
                }
            }
        }
        public bool EnableSKUEdit
        {
            get { return _enableSKUEdit; }
            set
            {
                if (_enableSKUEdit != value)
                {
                    _enableSKUEdit = value;
                    RaisePropertyChanged("EnableSKUEdit");
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
        public OrderViewModel()
        {
            _data = new Orders();
            _enableOrderAdd = false;
            _enableOrderEdit = false;
            _enableSubOrderEdit = false;
            _enableSKUEdit = false;
            ReferenceOrderID = 0;
            ReferenceSubOrderID = 0;
            OrderID = -1;
            SubOrderID = -1;
            SKUQty = -1;
            Validator = new PropertyValidator();
            ValidationEnabled = false;
        }
        public void Initialize(BasicWarehouse warehouse)
        {
            _warehouse = warehouse;
            _dbservicewms = new DBServiceWMS(warehouse);
            try
            {
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
            get { return (Order as IDataErrorInfo).Error; }
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
                            case "OrderID":
                                if (OrderID <= 0 || (_dbservicewms.ExistsOrderID(ERPID, OrderID) && OrderID != ReferenceOrderID  ))
                                    validationResult = ResourceReader.GetString("ERR_ORDERID");
                                break;
                            case "Destination":
                                if (_dbservicewms.FindPlaceID(Destination) == null)
                                    validationResult = ResourceReader.GetString("ERR_DESTINATION");
                                break;
                            case "ReleaseTime":
                                if (ReleaseTime < SqlDateTime.MinValue.Value || ReleaseTime > SqlDateTime.MaxValue.Value)
                                    validationResult = ResourceReader.GetString("ERR_RANGE");
                                break;
                            case "SubOrderID":
                                if (SubOrderID <= 0 || (_dbservicewms.ExistsSubOrderID(OrderID, SubOrderID) && SubOrderID != ReferenceSubOrderID))
                                    validationResult = ResourceReader.GetString("ERR_SUBORDERID");
                                break;
                            case "SubOrderName":
                                break;
                            case "SKUID":
                                if (_dbservicewms.FindSKUID(SKUID) == null)
                                    validationResult = ResourceReader.GetString("ERR_SKU");
                                break;
                            case "SKUBatch":
                                break;
                            case "SKUQty":
                                if( SKUQty <= 0)
                                    validationResult = ResourceReader.GetString("ERR_RANGE");
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
