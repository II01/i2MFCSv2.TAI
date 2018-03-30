using System;
using System.Collections.Generic;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.CommandWpf;
using Warehouse.Model;
using GalaSoft.MvvmLight.Messaging;
using GalaSoft.MvvmLight.Ioc;
using System.Diagnostics;
using Database;
using UserInterface.Messages;

namespace UserInterface.ViewModel
{
    public class VisualizationViewModel: ViewModelBase
    {
        #region members
        private BasicWarehouse _warehouse;
        #endregion

        #region properties
        public Dictionary<String, VisuDeviceBasicViewModel> Device { get; set; }
        public RelayCommand OnLoaded { get; private set; }
        #endregion

        #region initialization
        public void Initialize(BasicWarehouse warehouse)
        {
            _warehouse = warehouse;
            try
            {
            }
            catch (Exception e)
            {
                _warehouse.AddEvent(Database.Event.EnumSeverity.Error, Database.Event.EnumType.Exception, e.Message);
                throw new Exception(string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
            }
        }

        public void ExecuteOnLoaded()
        {
            try
            {
                Initialize(SimpleIoc.Default.GetInstance<MainViewModel>().Warehouse);
                try
                {
                    foreach (var d in Device)
                        d.Value.Initialize(_warehouse);
                }
                catch (Exception e)
                {
                    _warehouse.AddEvent(Database.Event.EnumSeverity.Error, Database.Event.EnumType.Exception, e.Message);
                    throw new Exception(string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
                }

                Messenger.Default.Send<MessageLoadingCompleted>(new MessageLoadingCompleted());
            }
            catch (Exception e)
            {
                _warehouse.AddEvent(Database.Event.EnumSeverity.Error, Database.Event.EnumType.Exception, 
                                    string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
            }
        }

        public VisualizationViewModel()
        {
            try
            {
                Device = new Dictionary<string, VisuDeviceBasicViewModel>();
                OnLoaded = new RelayCommand(() => ExecuteOnLoaded());
            }
            catch (Exception e)
            {
                throw new Exception(string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
            }
        }
        #endregion
    }
}
