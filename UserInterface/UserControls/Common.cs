using System;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace UserInterface.UserControls
{
    public enum DeviceCommandEnum { Refresh, Reset, AutoOn, AutoOff, Home, LongTermBlockOn, LongTermBlockOff, SetTime }
    public enum ModeCommandEnum { ToggleWMS, ModeWMS, ModeMFCS, ToggleAuto, SetAuto, SetNotAuto, Start, Stop }
    public enum DeviceStateEnum { None, Offline, Alarm, LongTermBlock, Local, Remote, AutoRun}

    public class ScalingPoint
    {
        public double Pixel { get; set; }
        public double Physical { get; set; }
    }

    public class TUToVisibility : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            try
            {
                return ((int)value != 0) ? Visibility.Visible : Visibility.Hidden;
            }
            catch
            {
                return Visibility.Hidden;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    public class TUToThickness : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            try
            {
                return ((int)value != 0) ? 4 : 1;
            }
            catch
            {
                return 1;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    public class TUToColor : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            try
            {
                return ((int)value != 0) ? Brushes.Black : Brushes.Gray;
            }
            catch
            {
                return Brushes.Black;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    public class BoolToBrush : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            try
            {
                return (bool)value ? Brushes.Gray : Brushes.White;
            }
            catch
            {
                return Brushes.White;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class DeviceStateEnumToBrush : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            try
            {
                switch ((DeviceStateEnum)value)
                {
                    case DeviceStateEnum.Offline: return Brushes.SkyBlue;
                    case DeviceStateEnum.Alarm: return Brushes.Red;
                    case DeviceStateEnum.LongTermBlock: return Brushes.Gray;
                    case DeviceStateEnum.Local: return Brushes.Black;
                    case DeviceStateEnum.Remote: return Brushes.Yellow;
                    case DeviceStateEnum.AutoRun: return Brushes.Green;
                    default: return Brushes.SkyBlue;
                }
            }
            catch
            {
                return Brushes.SkyBlue;
            }
        }
        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    public class ModeToBrush : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            try
            {
                switch ((bool)value)
                {
                    case true: return Brushes.Green;
                    default: return Brushes.Gray;
                }
            }
            catch
            {
                return Brushes.SkyBlue;
            }
        }
        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    public class PhysicalToPixel : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            try
            {
                int v = (int)value;
                ScalingPoint [] p = (ScalingPoint [])parameter;

                if (v < p[0].Physical)
                    return p[0].Pixel;
                for (int i = 1; i < p.Count(); i++)
                    if (v < p[i].Physical)
                        return (int)(p[i - 1].Pixel + (p[i].Pixel - p[i - 1].Pixel) / (double)(p[i].Physical - p[i - 1].Physical) * (v - p[i - 1].Physical));
                return p[p.Count() - 1].Pixel;
            }
            catch
            {
                return 0;
            }
        }
        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
