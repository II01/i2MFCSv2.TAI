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

namespace UserInterface.ViewModel
{
    public sealed class StationPickBoxViewModel: StationViewModel
    {
        #region members
        private string _boxes;
        private List<string> _boxList;
        #endregion

        #region properties

        public string Boxes
        {
            get { return _boxes; }
            set
            {
                if (_boxes != value)
                {
                    _boxes = value;
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
        public StationPickBoxViewModel() : base()
        {
            _boxList = new List<string>();
        }
        public override void Initialize(BasicWarehouse warehouse)
        {
            try
            {
                base.Initialize(warehouse);
                OperationName = "Pick box";
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
                            case "Boxes":
                                string[] boxArray = Regex.Split(Boxes, @"[,|;\s\n]+");
                                _boxList.Clear();
                                foreach (var b in boxArray)
                                {
                                    if (b != "")
                                    {
                                        if (DBServiceWMS.FindBoxByBoxID(b) == null)
                                            validationResult = ResourceReader.GetString("ERR_NOBOXID");
                                        else if (DBServiceWMS.FindTUByBoxID(b) == null)
                                            validationResult = ResourceReader.GetString("ERR_TUBOXNOEXISTS");
                                        else if (_boxList.Contains(b))
                                            validationResult = ResourceReader.GetString("ERR_TUBOXEXISTS");
                                        else
                                            _boxList.Add(b);
                                    }
                                }
                                if (validationResult != String.Empty)
                                    _boxList.Clear();
                                else
                                {
                                    var x = DBServiceWMS.GetTUIDsForBoxes(_boxList);
                                    if (x.Count != 1)
                                        validationResult = ResourceReader.GetString("ERR_MANYTUIDFORBOXES");
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
