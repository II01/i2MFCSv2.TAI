using System;

namespace UserInterface.Services
{
    [Serializable]
    public class UIException: Exception
    {
        public UIException(string message) : base (message)
        {
        }
    }
}
