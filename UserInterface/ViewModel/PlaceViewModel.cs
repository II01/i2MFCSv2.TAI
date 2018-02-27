using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using System.Collections.ObjectModel;
using UserInterface.Services;
using UserInterface.Messages;
using Infralution.Localization.Wpf;
using System.Globalization;
using Database;
using System.Windows.Controls;

namespace UserInterface.ViewModel
{
    public sealed class PlaceViewModel: ViewModelBase
    {

        private bool _enabled;

        private PlaceID _place;

        public bool Enabled
        {
            get { return _enabled; }
            set
            {
                if (_enabled != value)
                {
                    _enabled = value;
                    RaisePropertyChanged("Enabled");
                }
            }
        }

        public PlaceID Place
        {
            get { return _place; }
            set
            {
                if (_place != value)
                {
                    _place = value;
                    RaisePropertyChanged("Place");
                }
            }
        }

        public string ID
        {
            get { return _place.ID;  }
            set
            {
                if( _place.ID != value )
                {
                    _place.ID = value;
                    RaisePropertyChanged("ID");
                }
            }
        }

        public bool Blocked
        {
            get { return _place.Blocked; }
            set
            {
                if( _place.Blocked != value)
                {
                    _place.Blocked = value;
                    RaisePropertyChanged("Blocked");
                }
            }
        }

        public int Size
        {
            get { return _place.Size; }
            set
            {
                if (_place.Size != value)
                {
                    _place.Size = value;
                    RaisePropertyChanged("Size");
                }
            }
        }

        public PlaceViewModel()
        {
            _place = new PlaceID();
        }
    }


    public class InRangeRule : ValidationRule
    {
        private int _min;
        private int _max;

        public InRangeRule()
        {
        }

        public int Min
        {
            get { return _min; }
            set { _min = value; }
        }

        public int Max
        {
            get { return _max; }
            set { _max = value; }
        }

        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            int age = 0;

            try
            {
                if (((string)value).Length > 0)
                    age = Int32.Parse((String)value);
            }
            catch (Exception e)
            {
                return new ValidationResult(false, e.Message);                
            }

            if ((age < Min) || (age > Max))
            {
                return new ValidationResult(false, Min + " - " + Max);
            }
            else
            {
                return new ValidationResult(true, null);
            }
        }
    }
}

