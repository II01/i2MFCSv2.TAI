using GalaSoft.MvvmLight;
using System.Globalization;
using System.Windows.Input;

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

    public class MessageValidationInfo
    {
        public bool AllPropertiesValid { get; set; }
    }
    public class MessageValidationRequestTUID
    {
        public bool Trigger { get; set; }
    }
    public class MessageValidationTUID
    {
        public int TUID;
    }

    public class MessageKeyPressed
    {
        public string KeyPressed { get; set; }
    }
}
