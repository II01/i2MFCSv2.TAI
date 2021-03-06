﻿using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace UserInterface.View
{
    /// <summary>
    /// Description for AlarmOverviewView.
    /// </summary>
    public partial class StationRemoveTrayView : UserControl

    {
        /// <summary>
        /// Initializes a new instance of the AlarmOverviewView class.
        /// </summary>
        private static readonly Regex regex = new Regex(@"[0-9]+");

        public StationRemoveTrayView()
        {
            InitializeComponent();
            this.Loaded += ViewLoaded;
        }

        protected override void OnPreviewTextInput(TextCompositionEventArgs e)
        {
            if (!regex.IsMatch(e.Text))
                e.Handled = true;
            else
                base.OnPreviewTextInput(e);
        }

        void ViewLoaded(object sender, RoutedEventArgs e)
        {
            tbTUID.Focus();
        }
    }
}