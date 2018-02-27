using System;
using System.IO;
using System.Threading;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Telegrams
{

    public static class Log
    {

        public enum Severity { EVENT, EXCEPTION}

        private static string _fileNamePrefix = ""; //  c:\\Logs\\LogTel{0:yyyy-MM-dd-H}.txt";   // yyyy-mm-dd-hh
        private static Object _Logging = new Object();


        public static bool On = true;
        public static bool ToFile = true;



        public static void AddLog(Severity severity, string device, string module, string Message)
        {

            if (!On)
                return;
            lock (_Logging)
            {
                //            if (_fileNamePrefix == "")
                _fileNamePrefix = System.Configuration.ConfigurationManager.AppSettings["txtlog"];
                if (ToFile)
                {
                    using (StreamWriter sw = new StreamWriter(String.Format(_fileNamePrefix, DateTime.Now), true))
                    {
                        sw.WriteLine(String.Format("{0:yyyy-MM-dd HH:mm:ss:fff}->{1}:{2}:{3}:{4}", DateTime.Now, severity, device, module, Message));
                        sw.WriteLine();
                        //Console.WriteLine(String.Format("{0}:{1}:{2}:{3}", DateTime.Now, severity, module, Message));
                    }
                }
/*                else
                {
                    Console.WriteLine(String.Format("{0:yyyy-MM-dd HH:mm:ss:fff}->{1}:{2}:{3}:{4}", DateTime.Now, severity, device, module, Message));
                    Console.WriteLine();
                }*/
            }
        }
    }
}
