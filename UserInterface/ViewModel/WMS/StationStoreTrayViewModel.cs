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
    public sealed class StationStoreTrayViewModel: StationViewModel
    {
        #region members
        private string _tuidstr;
        private int _tuid;
        #endregion

        #region properties
        public string TUIDstr
        {
            get { return _tuid != 0 ? _tuid.ToString() : ""; }
            set
            {
                if (_tuidstr != value)
                {
                    _tuidstr = value;
                    TUID = Int32.TryParse(_tuidstr, out int res) ? res : 0;
                    RaisePropertyChanged("TUIDstr");
                }
            }
        }
        public int TUID
        {
            get { return _tuid; }
            set
            {
                if (_tuid != value)
                {
                    _tuid = value;
                    RaisePropertyChanged("TUID");
                }
            }
        }
        #endregion

        #region initialization
        public StationStoreTrayViewModel() : base()
        {
        }
        public override void Initialize(BasicWarehouse warehouse)
        {
            try
            {
                base.Initialize(warehouse);
                OperationName = "Store tray";
                TUID = DBServiceWMS.GetTUIDOnPlaceID(DBServiceWMS.GetParameter("Place.IOStation")).ID;
            }
            catch (Exception e)
            {
                Warehouse.AddEvent(Database.Event.EnumSeverity.Error, Database.Event.EnumType.Exception, e.Message);
                throw new Exception(string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
            }
        }
        #endregion

        #region commands
        #endregion

        #region validation
        public override string this[string propertyName]
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
                            case "TUIDstr":
                                if (TUID == 0)
                                    validationResult = ResourceReader.GetString("ERR_TUID");
                                else
                                {
                                    var place = DBServiceWMS.FindPlaceByTUID(TUID);
                                    if (place != null && place.PlaceID != DBServiceWMS.GetParameter("Place.IOStation") )
                                        validationResult = ResourceReader.GetString("ERR_TUIDEXISTS");
                                }
                                break;
                        }
                    }
                    Validator.AddOrUpdate(propertyName, validationResult == String.Empty);
                    AllPropertiesValid = Validator.IsValid();
                    return validationResult;
                }
                catch (Exception e)
                {
                    Warehouse.AddEvent(Database.Event.EnumSeverity.Error, Database.Event.EnumType.Exception,
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
