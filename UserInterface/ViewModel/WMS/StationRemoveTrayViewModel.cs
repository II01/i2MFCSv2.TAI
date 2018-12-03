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
using GalaSoft.MvvmLight.Messaging;
using UserInterface.Messages;
using System.Windows.Input;

namespace UserInterface.ViewModel
{
    public sealed class StationRemoveTrayViewModel : StationViewModel
    {
        #region members
        private string _tuidstr;
        private int _tuid;
        private string _placeid;
        #endregion

        #region properties
        public bool RemoveTray { get; set; }
        public string TUIDstr
        {
            get { return _tuidstr; }
            set
            {
                if (_tuidstr != value)
                {
                    _tuidstr = value;
                    int idx = _tuidstr.IndexOf('.');
                    if (idx == -1)
                        idx = _tuidstr.Length;
                    TUID = Int32.TryParse(_tuidstr.Substring(0, idx), out int res) ? res : 0;
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
        public string PlaceID
        {
            get { return _placeid; }
            set
            {
                if (_placeid != value)
                {
                    _placeid = value;
                    RaisePropertyChanged("PlaceID");
                }
            }
        }
        #endregion

        #region initialization
        public StationRemoveTrayViewModel() : base()
        {
        }
        public override void Initialize(BasicWarehouse warehouse)
        {
            try
            {
                base.Initialize(warehouse);
                if (RemoveTray)
                    OperationName = "Remove tray";
                else
                    OperationName = "Bring tray";
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
                                PlaceID = "";
                                if (TUID == 0)
                                    validationResult = ResourceReader.GetString("ERR_TUID");
                                else 
                                {
                                    var place = DBServiceWMS.FindPlaceByTUID(TUID);
                                    if (place == null || place.PlaceID == "W:out")
                                        validationResult = ResourceReader.GetString("ERR_TUID");
                                    else if (RemoveTray && !DBServiceWMS.IsTUIDEmpty(place.TU_ID))
                                        validationResult = ResourceReader.GetString("ERR_TUIDFULL");
                                    PlaceID = place?.PlaceID;
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
