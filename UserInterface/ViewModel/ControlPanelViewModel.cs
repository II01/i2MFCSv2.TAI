using System;
using System.Collections.Generic;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using UserInterface.Messages;
using Database;
using GalaSoft.MvvmLight.Ioc;
using Warehouse.Model;
using System.Diagnostics;

namespace UserInterface.ViewModel
{
    public sealed class ControlPanelViewModel : ViewModelBase
    {
        #region members
        private BasicWarehouse _warehouse;
        private ControlPanelModeViewModel _modes;
        #endregion

        #region properties
        public Dictionary<String, ControlPanelSegmentsViewModel> Device { get; set; }
        public RelayCommand OnLoaded { get; set; }

        public ControlPanelModeViewModel Modes
        {
            get { return _modes; }
            set
            {
                if(_modes != value)
                {
                    _modes = value;
                    RaisePropertyChanged("Modes");
                }
            }
        }            
    
        #endregion

        #region initialization
        public void Initialize(BasicWarehouse warehouse)
        {
            try
            {
                _warehouse = warehouse;
                foreach (var d in Device)
                    d.Value.Initialize(_warehouse);
                if(Modes == null)
                {
                    Modes = new ControlPanelModeViewModel();
                    Modes.Initialize(_warehouse);
                }
            }
            catch (Exception e)
            {
                _warehouse.AddEvent(Database.Event.EnumSeverity.Error, Database.Event.EnumType.Exception,
                                    string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
            }
        }

        public void ExecuteOnLoaded()
        {
            try
            {
                Initialize(SimpleIoc.Default.GetInstance<MainViewModel>().Warehouse);
                Messenger.Default.Send<MessageLoadingCompleted>(new MessageLoadingCompleted());
            }
            catch (Exception e)
            {
                _warehouse.AddEvent(Database.Event.EnumSeverity.Error, Database.Event.EnumType.Exception,
                                    string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
            }
        }

        public ControlPanelViewModel()
        {
            Device = new Dictionary<string, ControlPanelSegmentsViewModel>();
            OnLoaded = new RelayCommand(() => ExecuteOnLoaded());
        }
        #endregion
    }
}