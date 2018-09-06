using System;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.CommandWpf;
using UserInterface.Services;
using DatabaseWMS;
using System.ComponentModel;
using Warehouse.Model;
using System.Diagnostics;
using UserInterface.DataServiceWMS;
using System.Data.SqlTypes;

namespace UserInterface.ViewModel
{
    public sealed class ReleaseOrderViewModel: ViewModelBase, IDataErrorInfo
    {
        #region members
        private Orders _data;
        private DateTime _dataLastChange;
        private string _portion;
        private bool _allPropertiesValid = false;
        private DBServiceWMS _dbservicewms;
        private BasicWarehouse _warehouse;
        private bool _validationEnabled;
        private int? _erpidref;
        #endregion

        #region properties
        public RelayCommand<string> SetDestination { get; private set; }
        public RelayCommand<string> SetReleaseTime { get; private set; }


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
        public int? ERPIDref
        {
            get { return _erpidref; }
            set
            {
                if (_erpidref != value)
                {
                    _erpidref = value;
                    RaisePropertyChanged("ERPIDref");
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
        public int SubOrderID
        {
            get { return _data.SubOrderID; }
            set
            {
                if (_data.SubOrderID != value)
                {
                    _data.SubOrderID = value;
                    RaisePropertyChanged("SubOrderID");
                }
            }
        }

        public int SubOrderERPID
        {
            get { return _data.SubOrderERPID; }
            set
            {
                if (_data.SubOrderERPID != value)
                {
                    _data.SubOrderERPID = value;
                    RaisePropertyChanged("SubOrderERPID");
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
        public DateTime LastChange
        {
            get { return _dataLastChange; }
            set
            {
                if (_dataLastChange != value)
                {
                    _dataLastChange = value;
                    RaisePropertyChanged("LastChange");
                }
            }
        }
        public string Portion
        {
            get { return _portion; }
            set
            {
                if (_portion != value)
                {
                    _portion = value;
                    RaisePropertyChanged("Portion");
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
        public ReleaseOrderViewModel()
        {
            _data = new Orders();
            _dataLastChange = DateTime.Now;
            Validator = new PropertyValidator();
            ValidationEnabled = false;
        }
        public void Initialize(BasicWarehouse warehouse)
        {
            _warehouse = warehouse;
            _dbservicewms = new DBServiceWMS(warehouse);
            try
            {
                SetDestination = new RelayCommand<string>(dest => ExecuteSetDestination(dest));
                SetReleaseTime = new RelayCommand<string>(rt => ExecuteSetReleaseTime(rt));
            }
            catch (Exception e)
            {
                _warehouse.AddEvent(Database.Event.EnumSeverity.Error, Database.Event.EnumType.Exception, e.Message);
                throw new Exception(string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
            }
        }
        #endregion

        #region commands
        private void ExecuteSetDestination(string dest)
        {
            Destination = dest;
        }
        private void ExecuteSetReleaseTime(string hours)
        {
            if(double.TryParse(hours, System.Globalization.NumberStyles.AllowDecimalPoint, System.Globalization.CultureInfo.InvariantCulture, out double h))
            {
                ReleaseTime = DateTime.Now.AddHours(h);
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
                            case "Destination":
                                if (_dbservicewms.FindPlaceID(Destination) == null)
                                    validationResult = ResourceReader.GetString("ERR_DESTINATION");
                                break;
                            case "ReleaseTime":
                                if (ReleaseTime < SqlDateTime.MinValue.Value || ReleaseTime >= SqlDateTime.MaxValue.Value)
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
