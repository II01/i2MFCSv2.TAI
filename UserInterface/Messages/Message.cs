using GalaSoft.MvvmLight;
using System.Globalization;

namespace UserInterface.Messages
{
    public class MessageAccessLevel
    {
        public int AccessLevel { get; set; }
        public string User { get; set; }
    }

    public class MessageLoadingCompleted { }

    public class MessageLanguageChanged
    {
        public CultureInfo Culture { get; set; }
    }

    public class MessageViewChanged
    {
        public ViewModelBase ViewModel { get; set; }
    }
}
