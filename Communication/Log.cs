using System;
using System.IO;

namespace Communication
{


    public interface ILog
    {
        void AddLog(Log.Severity severity, string device, string module, string Message);
    }

    public class Log : ILog
    {

        public enum Severity { EVENT, EXCEPTION}

        public string FileName { get; set; }
        private object _Logging;

        public bool On { get; set; }

        public Log(string fileName, bool on)
        {
            _Logging = new object();
            FileName = fileName;
            On = on;
        }
        public void AddLog(Severity severity, string device, string module, string Message)
        {

            if (!On)
                return;

            lock (_Logging)
            {
                using (StreamWriter sw = new StreamWriter(String.Format(FileName, DateTime.Now), true))
                {
                    sw.WriteLine(String.Format("{0:yyyy-MM-dd HH:mm:ss:fff}->{1}:{2}:{3}:{4}", DateTime.Now, severity, device, module, Message));
                    sw.WriteLine();
                }
            }
        }
    }
}
