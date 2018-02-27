using System.Windows;
using GalaSoft.MvvmLight.Threading;
using System.Collections.Generic;
using System.Threading;
using System;

namespace UserInterface
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private static List<string> _languageTag = new List<string> { "en-IN", "tr-TR" };

        public static List<string> LanguageTag
        {
            get
            {
                return _languageTag;
            }
        }

        public static int AccessLevel { get; set; }
        public static int Language { get; set; }

        private static Mutex _mutex = null;

        private EventWaitHandle _eventWaitHandle;

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            const string appName = "I2MFCSUI";
            const string eventName = "I2MFCSUIevent";
            bool createdNew;

            _mutex = new Mutex(true, appName, out createdNew);
            _eventWaitHandle = new EventWaitHandle(false, EventResetMode.AutoReset, eventName);

            if (createdNew)
            {
                // prepare thread which waits for foreground event
                var thread = new Thread(() =>
                {
                    while (_eventWaitHandle.WaitOne())
                    {
                        Current.Dispatcher.BeginInvoke(
                            (Action)(() => ((MainWindow)Current.MainWindow).BringToForeground()));
                    }
                });
                thread.IsBackground = true;
                thread.Start();
                return;
            }
            else
            {
                // if one app exists, send event and shutdown
                _eventWaitHandle.Set();
                Shutdown();
            }
        }


        static App()
        {
            DispatcherHelper.Initialize();

            App.AccessLevel = 0;
            App.Language = 0;

            try
            {
                App.Language = Math.Max(0, Math.Min(1, int.Parse(System.Configuration.ConfigurationManager.AppSettings["StartLanguageID"])));
            }
            catch
            {
            }
            System.Threading.Thread.CurrentThread.CurrentUICulture = new System.Globalization.CultureInfo(LanguageTag[App.Language]);
        }
    }
}
