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
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using UserInterface.ProxyWMS_UI;
using System.Windows.Threading;
using GalaSoft.MvvmLight.Messaging;
using UserInterface.Messages;

namespace UserInterface.ViewModel
{
    public sealed class StationDropBoxViewModel: StationViewModel
    {
        #region members
        private string _boxes;
        private int _tuid;
        private string _tuidstr;
        private string _placeid;
        private List<string> _boxList;
        private DispatcherTimer _timer;
        List<string> _boxesAnnounced;
        #endregion

        #region properties

        public RelayCommand SuggestTU { get; private set; }

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
        public string Boxes
        {
            get { return _boxes; }
            set
            {
                if (_boxes != value)
                {
                    _boxes = value;
                    Regex regex = new Regex(@"[0-9A-Z]+[-0-9A-Z\s]*");
                    if (!regex.IsMatch(_boxes))
                        _boxes = "";
                    RaisePropertyChanged("Boxes");
                }
            }
        }
        public List<string> BoxList
        {
            get { return _boxList; }
            set
            {
                if (_boxList != value)
                {
                    _boxList = value;
                    RaisePropertyChanged("BoxList");
                }
            }
        }
        #endregion

        #region initialization
        public StationDropBoxViewModel() : base()
        {
            _boxesAnnounced = new List<string>();
            SuggestTU = new RelayCommand(async () => await ExecuteSuggestTU(), CanExecuteSuggestTU);
        }
        public override void Initialize(BasicWarehouse warehouse)
        {
            try
            {
                base.Initialize(warehouse);
                OperationName = "Drop box";
                _boxList = new List<string>();
                _timer = new DispatcherTimer();
                _timer.Interval = TimeSpan.FromMilliseconds(1000);
                _timer.Tick += (sender, args) => {RaisePropertyChanged("Boxes");};
                _timer.Start();
            }
            catch (Exception e)
            {
                Warehouse.AddEvent(Database.Event.EnumSeverity.Error, Database.Event.EnumType.Exception, e.Message);
                throw new Exception(string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
            }
        }
        #endregion

        #region commands
        public async Task ExecuteSuggestTU()
        {
            try
            {
                using (WMSToUIClient client = new WMSToUIClient())
                {
                    TUID = await client.SuggestTUIDAsync(_boxList.ToArray());
                    TUIDstr = TUID.ToString();
                    var place = DBServiceWMS.GetPlaceWithTUID(TUID);
                    PlaceID = place != null ? place.PlaceID : "-";
                }
            }
            catch (Exception e)
            {
                Warehouse.AddEvent(Database.Event.EnumSeverity.Error, Database.Event.EnumType.Exception,
                                   string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
            }
        }

        public bool CanExecuteSuggestTU()
        {
            try
            {
                return _boxList.Count > 0;
            }
            catch (Exception e)
            {
                Warehouse.AddEvent(Database.Event.EnumSeverity.Error, Database.Event.EnumType.Exception,
                                   string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
                return false;
            }
        }
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
                            case "TUID":
                            case "TUIDstr":
                                if (TUID == 0)
                                    validationResult = ResourceReader.GetString("ERR_TUID");
                                else
                                {
                                    var p = DBServiceWMS.FindPlaceByTUID(TUID);
                                    if (p == null)
                                        validationResult = ResourceReader.GetString("ERR_TUID");
                                    else if (p.PlaceID.StartsWith("W:out"))
                                        validationResult = ResourceReader.GetString("ERR_TUID");
                                    var place = DBServiceWMS.GetPlaceWithTUID(TUID);
                                    PlaceID = place != null ? place.PlaceID : "-";
                                }
                                break;
                            case "Boxes":
                                if( Boxes != null)
                                {
                                    string[] boxArray = Regex.Split(Boxes, @"[,|;\s\n]+");
                                    _boxList.Clear();
                                    foreach (var b in boxArray)
                                    {
                                        if (b != "")
                                        {
                                            if (DBServiceWMS.FindBoxByBoxID(b) == null)
                                            {
                                                if( b.Length == 14 && b[4] == '-' && b[9] == '-' && 
                                                    !_boxesAnnounced.Exists(p => p == b))  // announce box to WMS and ERP
                                                    using (WMSToUIClient client = new WMSToUIClient())
                                                    {
                                                        client.BoxEntry(b);
                                                        _boxesAnnounced.Add(b);
                                                    }
                                                validationResult = ResourceReader.GetString("ERR_NOBOXID");
                                            }
                                            else if (DBServiceWMS.FindTUByBoxID(b) != null)
                                                validationResult = ResourceReader.GetString("ERR_TUBOXEXISTS");
                                            else if (_boxList.Contains(b))
                                                validationResult = ResourceReader.GetString("ERR_TUBOXEXISTS");
                                            else
                                                _boxList.Add(b);
                                        }
                                    }
                                    if (validationResult == String.Empty && _boxList.Count > 1)
                                    {
                                        var skuid = DBServiceWMS.FindBoxByBoxID(_boxList[0]).SKU_ID;
                                        foreach (var b in _boxList)
                                            if (DBServiceWMS.FindBoxByBoxID(b).SKU_ID != skuid)
                                                validationResult = ResourceReader.GetString("ERR_NOTSAMESKUID");
                                    }
                                }
                                if (validationResult != String.Empty)
                                    _boxList.Clear();
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
