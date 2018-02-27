using MahApps.Metro.Controls;
using UserInterface.ViewModel;
using Infralution.Localization.Wpf;
using System.Threading;
using System.Globalization;
using System.Windows;

namespace UserInterface
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MetroWindow
    {

        /// <summary>
        /// Initializes a new instance of the MainWindow class.
        /// </summary>
        /// 

        public void BringToForeground()
        {
            if (WindowState == WindowState.Minimized || Visibility == Visibility.Hidden)
            {
                Show();
                WindowState = WindowState.Normal;
            }

            // According to some sources these steps gurantee that an app will be brought to foreground.
            Activate();
            Topmost = true;
            Topmost = false;
            Focus();
        }

        public MainWindow()
        {
            InitializeComponent();
            Closing += (s, e) => ViewModelLocator.Cleanup();
            CultureInfo ci = new CultureInfo("en-IN");
            Thread.CurrentThread.CurrentCulture = ci;
            CultureManager.UICulture = ci;
        }
    }
}