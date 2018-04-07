using CommonServiceLocator;
using GalaSoft.MvvmLight.Ioc;
using UserInterfaceGravityPanel.Model;

namespace UserInterfaceGravityPanel.ViewModel
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
        #endregion

        #region initialization
        static ViewModelLocator()
        {
            ServiceLocator.SetLocatorProvider(() => SimpleIoc.Default);
            SimpleIoc.Default.Register<IDataService, DataService>();

            SimpleIoc.Default.Register<MainViewModel>();
        }
        public static void Cleanup()
        {
        }
        #endregion
    }
}
