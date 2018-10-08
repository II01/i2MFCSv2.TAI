using System.Windows.Controls;

namespace UserInterface.View
{
    /// <summary>
    /// Description for AlarmOverviewView.
    /// </summary>
    public partial class StationsView : UserControl

    {
        /// <summary>
        /// Initializes a new instance of the AlarmOverviewView class.
        /// </summary>
        public StationsView()
        {
            InitializeComponent();
            dgCmd.LoadingRow += DataGrid_LoadingRow;
        }

        void DataGrid_LoadingRow(object sender, System.Windows.Controls.DataGridRowEventArgs e)
        {
            UserInterface.ViewModel.CommandWMSViewModel r = e.Row.Item as UserInterface.ViewModel.CommandWMSViewModel;
            if( r.Status == Services.EnumCommandWMSStatus.Active)
                dgCmd.ScrollIntoView(e.Row.Item);
        }
    }
}