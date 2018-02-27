using System.ServiceProcess;

namespace ServiceTelegrams
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main()
        {
            ServiceBase[] ServicesToRun;
            ServicesToRun = new ServiceBase[]
            {
                new ServiceTelegrams()
            };
            ServiceBase.Run(ServicesToRun);
        }
    }
}
