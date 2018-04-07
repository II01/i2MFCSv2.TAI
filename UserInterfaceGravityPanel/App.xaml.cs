using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Windows;
using GalaSoft.MvvmLight.Threading;

namespace UserInterfaceGravityPanel
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application, IDisposable
    {
        private static Mutex _mutex = null;
        private EventWaitHandle _eventWaitHandle;

        private static List<string> _languageTag = new List<string> { "en-IN", "tr-TR" };
        public static int _language { get; set; }


        static App()
        {
            DispatcherHelper.Initialize();
            try
            {
                _language = Math.Max(0, Math.Min(1, int.Parse(System.Configuration.ConfigurationManager.AppSettings["StartLanguageID"])));
            }
            catch
            {
            }
            CultureInfo ci = new CultureInfo(_languageTag[_language]);
            Thread.CurrentThread.CurrentCulture = ci;
            Thread.CurrentThread.CurrentUICulture = ci;
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            bool createdNew;
            const string appName = "I2MFCSUIGravityPanel";
            const string eventName = "I2MFCSUIGravityPanelevent";

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

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects).
                    _eventWaitHandle.Close();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~App() {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }
        #endregion
    }
}
