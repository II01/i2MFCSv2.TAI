using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace UserInterface.UserControls
{
    public partial class UCDimensionCheck : UserControl
    {
        public static DependencyProperty DeviceNameProperty = DependencyProperty.Register("DeviceName", typeof(string), typeof(UCDimensionCheck));
        public static DependencyProperty LeftProperty = DependencyProperty.Register("LeftValue", typeof(bool), typeof(UCDimensionCheck));
        public static DependencyProperty RightProperty = DependencyProperty.Register("RightValue", typeof(bool), typeof(UCDimensionCheck));
        public static DependencyProperty FrontProperty = DependencyProperty.Register("FrontValue", typeof(bool), typeof(UCDimensionCheck));
        public static DependencyProperty BackProperty = DependencyProperty.Register("BackValue", typeof(bool), typeof(UCDimensionCheck));
        public static DependencyProperty TopProperty = DependencyProperty.Register("TopValue", typeof(bool), typeof(UCDimensionCheck));
        public static DependencyProperty BarCodeProperty = DependencyProperty.Register("BarCodeValue", typeof(bool), typeof(UCDimensionCheck));
        public static DependencyProperty MFCSProperty = DependencyProperty.Register("MFCSValue", typeof(bool), typeof(UCDimensionCheck));

        public static DependencyProperty InfoCommandProperty = DependencyProperty.Register("InfoCommand", typeof(ICommand), typeof(UCDimensionCheck));

        public UCDimensionCheck()
        {
            InitializeComponent();
        }
        public ICommand InfoCommand
        {
            get { return (ICommand)GetValue(InfoCommandProperty); }
            set { SetValue(InfoCommandProperty, value); }
        }

        public string DeviceName
        {
            get { return (string)GetValue(DeviceNameProperty); }
            set
            {
                SetValue(DeviceNameProperty, value);
            }
        }
        public bool LeftValue
        {
            get { return (bool)GetValue(LeftProperty); }
            set { SetValue(LeftProperty, value); }
        }
        public bool RightValue
        {
            get { return (bool)GetValue(RightProperty); }
            set { SetValue(RightProperty, value); }
        }
        public bool FrontValue
        {
            get { return (bool)GetValue(FrontProperty); }
            set { SetValue(FrontProperty, value); }
        }
        public bool BackValue
        {
            get { return (bool)GetValue(BackProperty); }
            set { SetValue(BackProperty, value); }
        }
        public bool TopValue
        {
            get { return (bool)GetValue(TopProperty); }
            set { SetValue(TopProperty, value); }
        }
        public bool BarCodeValue
        {
            get { return (bool)GetValue(BarCodeProperty); }
            set { SetValue(BarCodeProperty, value); }
        }
        public bool MFCSValue
        {
            get { return (bool)GetValue(MFCSProperty); }
            set { SetValue(MFCSProperty, value); }
        }
    }
}
