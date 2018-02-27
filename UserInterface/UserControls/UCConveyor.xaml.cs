using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace UserInterface.UserControls
{
    public partial class UCConveyor : UserControl
    {
        public static DependencyProperty DeviceNameProperty = DependencyProperty.Register("DeviceName", typeof(string), typeof(UCConveyor));
        public static DependencyProperty TransportUnitProperty = DependencyProperty.Register("TransportUnit", typeof(String), typeof(UCConveyor), new PropertyMetadata(""));
        public static DependencyProperty TaskProperty = DependencyProperty.Register("Task", typeof(String), typeof(UCConveyor));
        public static DependencyProperty DeviceDetailsProperty = DependencyProperty.Register("DeviceDetails", typeof(String), typeof(UCConveyor));
        public static DependencyProperty Sensor1ValueProperty = DependencyProperty.Register("Sensor1Value", typeof(bool), typeof(UCConveyor));
        public static DependencyProperty Sensor2ValueProperty = DependencyProperty.Register("Sensor2Value", typeof(bool), typeof(UCConveyor));
        public static DependencyProperty Sensor3ValueProperty = DependencyProperty.Register("Sensor3Value", typeof(bool), typeof(UCConveyor));
        public static DependencyProperty Sensor1VisibilityProperty = DependencyProperty.Register("Sensor1Visibility", typeof(Visibility), typeof(UCConveyor), new PropertyMetadata(Visibility.Hidden));
        public static DependencyProperty Sensor2VisibilityProperty = DependencyProperty.Register("Sensor2Visibility", typeof(Visibility), typeof(UCConveyor), new PropertyMetadata(Visibility.Hidden));
        public static DependencyProperty Sensor3VisibilityProperty = DependencyProperty.Register("Sensor3Visibility", typeof(Visibility), typeof(UCConveyor), new PropertyMetadata(Visibility.Hidden));

        public static DependencyProperty InfoCommandProperty = DependencyProperty.Register("InfoCommand", typeof(ICommand), typeof(UCConveyor));

        public UCConveyor()
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

        public String TransportUnit
        {
            get { return (String)GetValue(TransportUnitProperty); }
            set { SetValue(TransportUnitProperty, value); }
        }
        public String Task
        {
            get { return (String)GetValue(TaskProperty); }
            set { SetValue(TaskProperty, value); }
        }
        public String DeviceDetails
        {
            get { return (String)GetValue(DeviceDetailsProperty); }
            set { SetValue(DeviceDetailsProperty, value); }
        }
        public bool Sensor1Value
        {
            get { return (bool)GetValue(Sensor1ValueProperty); }
            set { SetValue(Sensor1ValueProperty, value); }
        }
        public bool Sensor2Value
        {
            get { return (bool)GetValue(Sensor2ValueProperty); }
            set { SetValue(Sensor2ValueProperty, value); }
        }
        public bool Sensor3Value
        {
            get { return (bool)GetValue(Sensor3ValueProperty); }
            set { SetValue(Sensor3ValueProperty, value); }
        }
        public Visibility Sensor1Visibility
        {
            get { return (Visibility)GetValue(Sensor1VisibilityProperty); }
            set { SetValue(Sensor1VisibilityProperty, value); }
        }
        public Visibility Sensor2Visibility
        {
            get { return (Visibility)GetValue(Sensor2VisibilityProperty); }
            set { SetValue(Sensor2VisibilityProperty, value); }
        }
        public Visibility Sensor3Visibility
        {
            get { return (Visibility)GetValue(Sensor3VisibilityProperty); }
            set { SetValue(Sensor3VisibilityProperty, value); }
        }
    }
}
