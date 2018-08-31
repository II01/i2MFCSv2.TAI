using Database;
using GalaSoft.MvvmLight;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Windows.Data;
using UserInterface.Services;
using Warehouse.Model;
using static Database.SimpleCommand;

namespace UserInterface.ViewModel
{
    public abstract class VisuDeviceBasicViewModel : ViewModelBase
    {
        #region members
        protected BasicWarehouse _warehouse;
        private string _deviceName;
        private ObservableCollection<DetailBasic> _deviceDetails;
        private Dictionary<string, DetailBasic> _dictDetails;
        private static object _lock;

        #endregion

        #region properties
        public object Model { get; set; }

        public string DeviceName
        {
            get { return _deviceName; }
            set
            {
                if (_deviceName != value)
                {
                    _deviceName = value;
                    RaisePropertyChanged("DeviceName");
                }
            }
        }
        public ObservableCollection<DetailBasic> DeviceDetails
        {
            get { return _deviceDetails; }
            set
            {
                if (_deviceDetails != value)
                {
                    _deviceDetails = value;
                    RaisePropertyChanged("DeviceDetails");
                }
            }
        }

        #endregion

        #region initialization

        public virtual void Initialize(BasicWarehouse warehouse)
        {
            _warehouse = warehouse;
            try
            {
                _dictDetails = new Dictionary<string, DetailBasic>();
                DeviceDetails = new ObservableCollection<DetailBasic>();
                _lock = new object();
                BindingOperations.EnableCollectionSynchronization(DeviceDetails, _lock);
            }
            catch (Exception e)
            {
                _warehouse.AddEvent(Database.Event.EnumSeverity.Error, Database.Event.EnumType.Exception, e.Message);
                throw new Exception(string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
            }
        }
        public VisuDeviceBasicViewModel()
        {
            try
            {
            }
            catch (Exception e)
            {
                throw new Exception(string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
            }
        }
        #endregion

        #region functions
        public string StatusToString(bool online, bool remote, bool alarm, bool auto, bool ltb)
        {
            string str = "";
            if (!online)
                str += ResourceReader.GetString("Offline");
            else
            {
                if (remote)
                    str += ResourceReader.GetString("Remote");
                else
                    str += ResourceReader.GetString("Local");
                if (alarm)
                    str += "|" + ResourceReader.GetString("Alarm");
                else if (auto)
                    str += "|" + ResourceReader.GetString("Active");
                else
                    str += "|" + ResourceReader.GetString("Ready");
                if (ltb)
                    str += "|" + ResourceReader.GetString("LongTermBlock");
            }
            return str;
        }

        public void DetailsAddOrUpdate(string key, int indent, string description, string suffix, object value)
        {
            try
            {
                string descr = new String(' ', indent) + ResourceReader.GetString(description) + " " + suffix;
                if (!_dictDetails.ContainsKey(key))
                {
                    if (value is bool)
                        DeviceDetails.Add(new DetailBool { Description = descr, Value = (bool)value });
                    else
                        DeviceDetails.Add(new DetailString { Description = descr, Value = value.ToString() });
                    _dictDetails.Add(key, DeviceDetails.LastOrDefault());
                }
                else
                {
                    _dictDetails[key].Description = descr;
                    DetailsValueUpdate(key, value);
                }
            }
            catch(Exception e)
            {
                throw new Exception(string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
            }
        }

        public void DetailsValueUpdate(string key, object value)
        {
            try
            {
                if (_dictDetails.ContainsKey(key))
                {
                    if (value is bool)
                        (_dictDetails[key] as DetailBool).Value = (bool)value;
                    else
                        (_dictDetails[key] as DetailString).Value = value.ToString();
                }
            }
            catch (Exception e)
            {
                throw new Exception(string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
            }
        }

        public string ConveyorCommandToString(SimpleConveyorCommand cmd)
        {
            return cmd != null ? String.Format("{0}: {1} {2} {3} {4}", cmd.ID, cmd.Task, cmd.Material, ResourceReader.GetString("To"), cmd.Target) : "";
        }
        public string CraneCommandToString(SimpleCraneCommand cmd)
        {
            if(cmd!=null)
                switch (cmd.Task)
                {
                    case EnumTask.Cancel:
                    case EnumTask.Create:
                    case EnumTask.Delete: return String.Format("{0}: {1} {2} {3} {4}", cmd.ID, cmd.Task, cmd.Material, ResourceReader.GetString("From"), cmd.Source);
                    case EnumTask.Move: return String.Format("{0}: {1} {2} {3}", cmd.ID, cmd.Task, ResourceReader.GetString("To"), cmd.Source);
                    case EnumTask.Drop:
                    case EnumTask.Pick: return String.Format("{0}: {1} {2} {3} {4}", cmd.ID, ResourceReader.GetString(cmd.Task.ToString()), cmd.Material,
                                                                                     ResourceReader.GetString(cmd.Task == EnumTask.Drop ? "To" : "From"), cmd.Source);
                }
            return "";
        }
        #endregion
    }
}
