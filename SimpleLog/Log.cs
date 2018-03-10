using System;
using System.Configuration;
using System.IO;
using System.Runtime.CompilerServices;

namespace SimpleLog
{


    public interface ILog
    {
        void AddLog(Log.Severity severity, string device, string module, string Message);
        void ExceptionDeviceMessage(Exception ex, string device, [CallerMemberName] string member = "", [CallerFilePath] string fileName = "", [CallerLineNumber] int line = 0);
    }

    public static class Log 
    {

        public enum Severity { EVENT, EXCEPTION }

        public static string FileName { get; set; }
        private static object _Logging;

        public static bool On { get; set; }

        static Log()
        {
            _Logging = new object();
            On = false;
            try
            {
                FileName = ConfigurationManager.AppSettings["txtlog"];
                On = Convert.ToBoolean(ConfigurationManager.AppSettings["logtofile"]);
            }
            catch
            { }
        }

        public static void AddLog(Severity severity, string device, string Message, [CallerMemberName] string member = "", [CallerFilePath] string fileName = "", [CallerLineNumber] int line = 0)
        {
            try
            {
                string[] str = fileName.Split('\\');
                AddLog(severity, device, $"{member} ({str[str.Length - 1]} {line})", Message);
            }
            catch
            { }
        }

        public static void AddLog(Severity severity, string device, string module, string Message)
        {

            if (!On)
                return;

            try
            {
                lock (_Logging)
                {
                    using (StreamWriter sw = new StreamWriter(String.Format(FileName, DateTime.Now), true))
                    {
                        sw.WriteLine(String.Format("{0:yyyy-MM-dd HH:mm:ss:fff}->{1}:{2}:{3}:{4}", DateTime.Now, severity, device, Message, module));
                        sw.WriteLine();
                    }
                }
            }
            catch
            { }
        }
    }
}
