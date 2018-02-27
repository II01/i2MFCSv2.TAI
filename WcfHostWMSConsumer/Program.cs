using System;
using System.ServiceModel;
using WcfService;

namespace WcfHostWMSConsumer
{
    class Program
    {
        static void Main(string[] args)
        {
            ServiceHost sh;

            Console.WriteLine("WMS consumer\n------------\n");
            Console.WriteLine("Press SPACE and ENTER to stop ...");

            sh = new ServiceHost(typeof(WMSConsumer));
            sh.Open();

            char ch = 'k';
            do
            {
                try
                {
                    ch = Console.ReadKey().KeyChar;
                }
                catch { }
            }
            while (ch != ' ');

            Console.ReadLine();
            sh.Close();
            Console.WriteLine("... stopped.");
        }
    }
}
