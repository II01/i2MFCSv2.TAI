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
using UserInterface.Messages;
using GalaSoft.MvvmLight.Messaging;

namespace UserInterface.ViewModel
{
    public sealed class StationActionViewModel: StationViewModel
    {
        public enum CommandType { None = 0, DropBox, PickBox };
        #region members
        private List<CommandWMSOrder> _cmds;
        private string _boxes = "";
        private List<string> _boxList;
        private int _accessLevel;
        private string _accessUser;
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
                    Regex regex = new Regex(@"[0-9]+[0-9\s]*");
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

        public CommandType Command { get; set; }

        public StationActionViewModel() : base()
        {
            _boxList = new List<string>();
        }
        public override void Initialize(BasicWarehouse warehouse)
        {
            try
            {
                base.Initialize(warehouse);
                OperationName = "Action";
                _accessUser = "";
                Messenger.Default.Register<MessageAccessLevel>(this, (mc) => { _accessLevel = mc.AccessLevel; _accessUser = mc.User; });
            }
            catch (Exception e)
            {
                Warehouse.AddEvent(Database.Event.EnumSeverity.Error, Database.Event.EnumType.Exception, e.Message);
                throw new Exception(string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
            }
        }
        public void Initialize(BasicWarehouse warehouse, List<CommandWMSOrder> cmds)
        {
            try
            {
                base.Initialize(warehouse);
                OperationName = "Action";
                _cmds = cmds;
                ValidationEnabled = true;
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
                                    var c = _cmds.Find(p => p.Box_ID == b);
                                    if ( c != null)
                                    {
                                        if (Command == CommandType.DropBox)
                                        {
                                            DBServiceWMS.AddTUs(new List<TUs>() {
                                                    new TUs
                                                    {
                                                        TU_ID = c.TU_ID,
                                                        Box_ID = c.Box_ID,
                                                        Qty = 1,
                                                        ProdDate = DateTime.Now,
                                                        ExpDate = DateTime.Now
                                                    }});
                                            DBServiceWMS.AddLog(_accessUser, EnumLogWMS.Event, "UI", $"Drop: {c.Box_ID} to {c.TU_ID}");
                                            Boxes = "";
                                        }
                                        else if (Command == CommandType.PickBox)
                                        {
                                            DBServiceWMS.DeleteBox(c.Box_ID);
                                            DBServiceWMS.AddLog(_accessUser, EnumLogWMS.Event, "UI", $"Pick: {c.Box_ID} from {c.TU_ID}");
                                            Boxes = "";
                                        }

                                        using (WMSToUIClient client = new WMSToUIClient())
                                        {
                                            client.CommandStatusChangedAsync(c.ID, (int)EnumCommandWMSStatus.Finished);
                                        }
                                    }
                                }
                                if(Boxes != "")
                                    validationResult = ResourceReader.GetString("ERR_TUID");
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
