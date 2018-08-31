using GalaSoft.MvvmLight.Messaging;
using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using Telegrams;
using UserInterface.Messages;
using UserInterface.Services;
using Warehouse.ConveyorUnits;
using Warehouse.Model;

namespace UserInterface.ViewModel
{
    public class VisuConveyorViewModel: VisuDeviceBasicViewModel
    {
        #region members
        private int _transportUnit;
        private string _extraInfo;
        private bool _sensor1Value;
        private bool _sensor2Value;
        private bool _sensor3Value;
        private ConveyorInfo _info;
        #endregion

        #region properties
        public int TransportUnit
        {
            get { return _transportUnit; }
            set
            {
                if (_transportUnit != value)
                {
                    _transportUnit = value;
                    RaisePropertyChanged("TransportUnit");
                }
            }
        }
        public String ExtraInfo
        {
            get { return _extraInfo; }
            set
            {
                if (_extraInfo != value)
                {
                    _extraInfo = value;
                    RaisePropertyChanged("ExtraInfo");
                }
            }
        }
        public bool Sensor1Value
        {
            get { return _sensor1Value; }
            set
            {
                if (_sensor1Value != value)
                {
                    _sensor1Value = value;
                    RaisePropertyChanged("Sensor1Value");
                }
            }
        }
        public bool Sensor2Value
        {
            get { return _sensor2Value; }
            set
            {
                if (_sensor2Value != value)
                {
                    _sensor2Value = value;
                    RaisePropertyChanged("Sensor2Value");
                }
            }
        }
        public bool Sensor3Value
        {
            get { return _sensor3Value; }
            set
            {
                if (_sensor3Value != value)
                {
                    _sensor3Value = value;
                    RaisePropertyChanged("Sensor3Value");
                }
            }
        }
        #endregion

        #region initialization
        public void InitializeDetails()
        {
            try
            {
                int i = 0;

                DetailsAddOrUpdate("MODE", 0, "Mode", "", ResourceReader.GetString("Offline"));
                DetailsAddOrUpdate("TU", 0, "TU", "", "");
                DetailsAddOrUpdate("CMD", 0, "Task", "", "");
                DetailsAddOrUpdate("SENS", 0, "Sensors", "", "");
                if (Model != null && (Model as Conveyor).ConveyorInfo != null && (Model as Conveyor).ConveyorInfo.SensorList != null)
                    (Model as Conveyor).ConveyorInfo.SensorList.ForEach(s => DetailsAddOrUpdate(string.Format("SENS_{0}", i++), 4, s.Description, s.Reference, false));
            }
            catch(Exception e)
            {
                _warehouse.AddEvent(Database.Event.EnumSeverity.Error, Database.Event.EnumType.Exception, e.Message);
                throw new Exception(string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
            }
        }

        public override void Initialize(BasicWarehouse warehouse)
        {
            try
            {
                base.Initialize(warehouse);
                Model = _warehouse.Conveyor.ContainsKey(DeviceName) ? _warehouse.Conveyor[DeviceName] : null;
                if (Model != null)
                    ((ConveyorBasic)Model).NotifyVM.Add(new Action<ConveyorBasicInfo>((p) => OnDataChange(p as ConveyorInfo)));
                InitializeDetails();
                Messenger.Default.Register<MessageLanguageChanged>(this, (mc) => ExecuteLanguageChanged(mc.Culture));
            }
            catch (Exception e)
            {
                _warehouse.AddEvent(Database.Event.EnumSeverity.Error, Database.Event.EnumType.Exception, e.Message);
                throw new Exception(string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
            }
        }

        public VisuConveyorViewModel()
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
        public void RefreshDetails()
        {
            try
            {
                if (_info == null)
                    return;
                
                TransportUnit = _info.Material;
                ExtraInfo = _info.LastCommand != null ? _info.LastCommand : "";
                Sensor1Value = _info.SensorList != null && _info.SensorList.Count() > 0 ? _info.SensorList[0].Active : false;

                int i = 0;
                DetailsValueUpdate("MODE", _info.Status != null ? 
                    StatusToString(_info.Online, 
                                   _info.Status[TelegramTransportStatus.STATUS_REMOTE], 
                                   _info.Status[TelegramTransportStatus.STATUS_FAULT], 
                                   _info.Status[TelegramTransportStatus.STATUS_AUTOMATIC], 
                                   _info.Status[TelegramTransportStatus.STATUS_LONGTERMFAULT]) : "");
//                DetailsValueUpdate("TU", string.Format("P{0:d9}", TransportUnit));
                DetailsValueUpdate("TU", string.Format("{0:d9}", TransportUnit));
                DetailsValueUpdate("CMD", _info.LastCommand != null ? _info.LastCommand : "");
                if (_info != null && _info.SensorList != null)
                    _info.SensorList.ForEach(s => DetailsValueUpdate(string.Format("SENS_{0}", i++), s.Active));
            }
            catch (Exception e)
            {
                _warehouse.AddEvent(Database.Event.EnumSeverity.Error, Database.Event.EnumType.Exception,
                                    string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
            }
        }

        public void OnDataChange(ConveyorInfo info)
        {
            try
            {
                _info = info;

                RefreshDetails();
            }
            catch (Exception e)
            {
                _warehouse.AddEvent(Database.Event.EnumSeverity.Error, Database.Event.EnumType.Exception, 
                                    string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
            }
        }
        #endregion

        #region functions
        public void ExecuteLanguageChanged(CultureInfo ci)
        {
            InitializeDetails();
            RefreshDetails();
        }
        #endregion
    }
}
