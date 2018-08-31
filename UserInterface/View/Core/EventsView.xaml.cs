using System;
using System.Windows;
using System.Windows.Controls;
using UserInterface.ViewModel;

namespace UserInterface.View
{
    /// <summary>
    /// Description for AlarmOverviewView.
    /// </summary>
    public partial class EventsView : UserControl
    {
        /// <summary>
        /// Initializes a new instance of the AlarmOverviewView class.
        /// </summary>
        public EventsView()
        {
            InitializeComponent();

            ExternalFilter = item => ((EventViewModel)item).Description.Contains("P");
        }

        public Predicate<object> ExternalFilter
        {
            get { return (Predicate<object>)GetValue(ExternalFilterProperty); }
            set { SetValue(ExternalFilterProperty, value); }
        }
        public static readonly DependencyProperty ExternalFilterProperty = DependencyProperty.Register("ExternalFilter", typeof(Predicate<object>), typeof(EventsView));


    }
}