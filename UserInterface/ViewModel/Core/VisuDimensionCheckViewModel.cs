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
    public class VisuDimensionCheckViewModel: VisuDeviceBasicViewModel
    {
        #region members
        private int _transportUnit;
        private bool _left;
        private bool _right;
        private bool _front;
        private bool _back;
        private bool _top;
        private bool _hclass;
        private bool _barcode;
        private bool _mfcs;
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
        public bool LeftValue
        {
            get { return _left; }
            set
            {
                if (_left != value)
                {
                    _left = value;
                    RaisePropertyChanged("LeftValue");
                }
            }
        }
        public bool RightValue
        {
            get { return _right; }
            set
            {
                if (_right != value)
                {
                    _right = value;
                    RaisePropertyChanged("RightValue");
                }
            }
        }

        public bool FrontValue
        {
            get { return _front; }
            set
            {
                if (_front != value)
                {
                    _front = value;
                    RaisePropertyChanged("FrontValue");
                }
            }
        }
        public bool BackValue
        {
            get { return _back; }
            set
            {
                if (_back != value)
                {
                    _back = value;
                    RaisePropertyChanged("BackValue");
                }
            }
        }

        public bool TopValue
        {
            get { return _top; }
            set
            {
                if (_top != value)
                {
                    _top = value;
                    RaisePropertyChanged("TopValue");
                }
            }
        }

        public bool HClassValue
        {
            get { return _hclass; }
            set
            {
                if (_hclass != value)
                {
                    _hclass = value;
                    RaisePropertyChanged("HClassValue");
                }
            }
        }

        public bool BarCodeValue
        {
            get { return _barcode; }
            set
            {
                if (_barcode != value)
                {
                    _barcode = value;
                    RaisePropertyChanged("BarCodeValue");
                }
            }
        }
        public bool MFCSValue
        {
            get { return _mfcs; }
            set
            {
                if (_mfcs != value)
                {
                    _mfcs = value;
                    RaisePropertyChanged("MFCSValue");
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

        public VisuDimensionCheckViewModel()
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

                LeftValue = _info.Material != 0 && _info.MaterialError != null && _info.MaterialError[0];
                RightValue = _info.Material != 0 && _info.MaterialError != null && _info.MaterialError[1];
                FrontValue = _info.Material != 0 && _info.MaterialError != null && _info.MaterialError[2];
                BackValue = _info.Material != 0 && _info.MaterialError != null && _info.MaterialError[3];
                TopValue = _info.Material != 0 && _info.MaterialError != null && _info.MaterialError[4];
                HClassValue = _info.Material != 0 && _info.MaterialError != null && _info.MaterialError[10];
                BarCodeValue = _info.Material != 0 && _info.MaterialError != null && _info.MaterialError[7];
                MFCSValue = _info.Material != 0 && _info.MaterialError != null && _info.MaterialError[9];
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
