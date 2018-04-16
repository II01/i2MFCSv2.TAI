using CommonServiceLocator;
using GalaSoft.MvvmLight.Ioc;
using UserInterface.Model;

namespace UserInterface.ViewModel
{
    public class ViewModelLocator
    {
        #region properties
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", 
         Justification = "This non-static member is needed for data binding purposes.")]
        public MainViewModel Main
        {
            get { return ServiceLocator.Current.GetInstance<MainViewModel>(); }
        }

        public AlarmsViewModel Alarms
        {
            get { return ServiceLocator.Current.GetInstance<AlarmsViewModel>(); }
        }

        public SettingsViewModel Settings
        {
            get { return ServiceLocator.Current.GetInstance<SettingsViewModel>(); }
        }

        public SimpleCommandsViewModel SimpleCommands
        {
            get { return ServiceLocator.Current.GetInstance<SimpleCommandsViewModel>(); }
        }

        public MaterialsViewModel Materials
        {
            get { return ServiceLocator.Current.GetInstance<MaterialsViewModel>();  }
        }

        public LocationsViewModel Locations
        {
            get { return ServiceLocator.Current.GetInstance<LocationsViewModel>(); }
        }

        public EventsViewModel Events
        {
            get { return ServiceLocator.Current.GetInstance<EventsViewModel>(); }
        }

        public ControlPanelViewModel ControlPanel
        {
            get { return ServiceLocator.Current.GetInstance<ControlPanelViewModel>(); }
        }
        public CommandsViewModel Commands
        {
            get { return ServiceLocator.Current.GetInstance<CommandsViewModel>(); }
        }
        public VisualizationViewModel Visualization
        {
            get { return ServiceLocator.Current.GetInstance<VisualizationViewModel>(); }
        }
        public HistoryEventsViewModel HistoryEvents
        {
            get { return ServiceLocator.Current.GetInstance<HistoryEventsViewModel>(); }
        }
        public HistoryAlarmsViewModel HistoryAlarms
        {
            get { return ServiceLocator.Current.GetInstance<HistoryAlarmsViewModel>(); }
        }
        public HistoryMovementsViewModel HistoryMovements
        {
            get { return ServiceLocator.Current.GetInstance<HistoryMovementsViewModel>(); }
        }
        public HistoryCommandsViewModel HistoryCommands
        {
            get { return ServiceLocator.Current.GetInstance<HistoryCommandsViewModel>(); }
        }
        public HistorySimpleCommandsViewModel HistorySimpleCommands
        {
            get { return ServiceLocator.Current.GetInstance<HistorySimpleCommandsViewModel>(); }
        }
        public SKUIDsViewModel SKUIDs
        {
            get { return ServiceLocator.Current.GetInstance<SKUIDsViewModel>(); }
        }
        public PlaceIDsViewModel PlaceIDs
        {
            get { return ServiceLocator.Current.GetInstance<PlaceIDsViewModel>(); }
        }
        public PlaceTUIDsViewModel PlaceTUIDs
        {
            get { return ServiceLocator.Current.GetInstance<PlaceTUIDsViewModel>(); }
        }
        public OrderViewModel Order
        {
            get { return ServiceLocator.Current.GetInstance<OrderViewModel>(); }
        }
        public OrdersViewModel Orders
        {
            get { return ServiceLocator.Current.GetInstance<OrdersViewModel>(); }
        }
        public ReleaseOrderViewModel ReleaseOrder
        {
            get { return ServiceLocator.Current.GetInstance<ReleaseOrderViewModel>(); }
        }
        public ReleaseOrdersViewModel ReleaseOrders
        {
            get { return ServiceLocator.Current.GetInstance<ReleaseOrdersViewModel>(); }
        }
        public CommandERPViewModel CommandERP
        {
            get { return ServiceLocator.Current.GetInstance<CommandERPViewModel>(); }
        }
        public CommandERPsViewModel CommandERPs
        {
            get { return ServiceLocator.Current.GetInstance<CommandERPsViewModel>(); }
        }
        public CommandWMSViewModel CommandWMS
        {
            get { return ServiceLocator.Current.GetInstance<CommandWMSViewModel>(); }
        }
        public CommandWMSsViewModel CommandWMSs
        {
            get { return ServiceLocator.Current.GetInstance<CommandWMSsViewModel>(); }
        }
        public PlaceDiffsViewModel PlaceDiffs
        {
            get { return ServiceLocator.Current.GetInstance<PlaceDiffsViewModel>(); }
        }
        public LogsViewModel Logs
        {
            get { return ServiceLocator.Current.GetInstance<LogsViewModel>(); }
        }
        public HistoryLogsViewModel HistoryLogs
        {
            get { return ServiceLocator.Current.GetInstance<HistoryLogsViewModel>(); }
        }
        public HistoryCommandWMSsViewModel HistoryCommandWMSs
        {
            get { return ServiceLocator.Current.GetInstance<HistoryCommandWMSsViewModel>(); }
        }
        public HistoryCommandERPsViewModel HistoryCommandERPs
        {
            get { return ServiceLocator.Current.GetInstance<HistoryCommandERPsViewModel>(); }
        }
        public HistoryReleaseOrdersViewModel HistoryReleaseOrders
        {
            get { return ServiceLocator.Current.GetInstance<HistoryReleaseOrdersViewModel>(); }
        }
        #endregion

        #region initialization
        static ViewModelLocator()
        {
            ServiceLocator.SetLocatorProvider(() => SimpleIoc.Default);
            SimpleIoc.Default.Register<IDataService, DataService>();

            SimpleIoc.Default.Register<MainViewModel>();
            SimpleIoc.Default.Register<AlarmsViewModel>();
            SimpleIoc.Default.Register<SettingsViewModel>();
            SimpleIoc.Default.Register<SimpleCommandsViewModel>();
            SimpleIoc.Default.Register<MaterialsViewModel>();
            SimpleIoc.Default.Register<LocationsViewModel>();
            SimpleIoc.Default.Register<EventsViewModel>();
            SimpleIoc.Default.Register<ControlPanelViewModel>();
            SimpleIoc.Default.Register<CommandsViewModel>();
            SimpleIoc.Default.Register<VisualizationViewModel>();
            SimpleIoc.Default.Register<HistoryEventsViewModel>();
            SimpleIoc.Default.Register<HistoryAlarmsViewModel>();
            SimpleIoc.Default.Register<HistoryMovementsViewModel>();
            SimpleIoc.Default.Register<HistoryCommandsViewModel>();
            SimpleIoc.Default.Register<HistorySimpleCommandsViewModel>();
            SimpleIoc.Default.Register<SKUIDsViewModel>();
            SimpleIoc.Default.Register<PlaceIDsViewModel>();
            SimpleIoc.Default.Register<PlaceTUIDsViewModel>();
            SimpleIoc.Default.Register<OrdersViewModel>();
            SimpleIoc.Default.Register<ReleaseOrdersViewModel>();
            SimpleIoc.Default.Register<CommandERPsViewModel>();
            SimpleIoc.Default.Register<CommandWMSsViewModel>();
            SimpleIoc.Default.Register<PlaceDiffsViewModel>();
            SimpleIoc.Default.Register<LogsViewModel>();
            SimpleIoc.Default.Register<HistoryLogsViewModel>();
            SimpleIoc.Default.Register<HistoryCommandWMSsViewModel>();
            SimpleIoc.Default.Register<HistoryCommandERPsViewModel>();
            SimpleIoc.Default.Register<HistoryReleaseOrdersViewModel>();
        }
        public static void Cleanup()
        {
        }
        #endregion
    }
}
