using GalaSoft.MvvmLight;

namespace UserInterface.ViewModel
{
    public class DetailBasic : ViewModelBase
    {
        private string _description;

        public string Description
        {
            get { return _description; }
            set
            {
                if (_description != value)
                {
                    _description = value;
                    RaisePropertyChanged("Description");
                }
            }
        }
    }
    public class DetailString : DetailBasic
    {
        private string _value;

        public string Value
        {
            get { return _value; }
            set
            {
                if (_value != value)
                {
                    _value = value;
                    RaisePropertyChanged("Value");
                }
            }
        }
    }
    public class DetailBool : DetailBasic
    {
        private bool _value;

        public bool Value
        {
            get { return _value; }
            set
            {
                if (_value != value)
                {
                    _value = value;
                    RaisePropertyChanged("Value");
                }
            }
        }
    }

}
