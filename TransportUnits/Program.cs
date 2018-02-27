using System;
using System.Net;
using System.Configuration;
using System.Net.Sockets;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegrams;

namespace Telegrams
{
    class Program
    {


        static void Main(string[] args)
        {
            string p = System.Configuration.ConfigurationManager.ConnectionStrings["ConnectionMFCS"].ConnectionString;


            Log.ToFile = false;
            Console.WriteLine("Creating telegram...");

            Communication Voz2Vil1 = new Communication
            {
                Version = "CRANE",
                Name = "Voz2Vil1",
                ID = 1,
                PartnerID = 208,
                SimulateMFCS = false,
                SendTimeOut = TimeSpan.FromSeconds(20),
                RcvTimeOut = TimeSpan.FromSeconds(20),
                KeepALifeTelegram = new TelegramLife(),
                KeepALifeTime = TimeSpan.FromSeconds(3),
                SendPoint = new IPEndPoint(IPAddress.Parse("192.168.1.241"), 2003),
                RcvPoint = new IPEndPoint(IPAddress.Parse("192.168.1.241"), 2004)
//                SendPoint = new IPEndPoint(IPAddress.Parse("10.150.104.24"), 2003),
//                RcvPoint = new IPEndPoint(IPAddress.Parse("10.150.104.24"), 2004)
            };

            Communication Voz2Vil2 = new Communication
            {
                Version = "CRANE",
                Name = "Voz2Vil2",
                ID = 1,
                PartnerID = 209,
                SimulateMFCS = false,
                SendTimeOut = TimeSpan.FromSeconds(20),
                RcvTimeOut = TimeSpan.FromSeconds(20),
                KeepALifeTelegram = new TelegramLife(),
                KeepALifeTime = TimeSpan.FromSeconds(3),
                SendPoint = new IPEndPoint(IPAddress.Parse("10.150.104.25"), 2003),
                RcvPoint = new IPEndPoint(IPAddress.Parse("10.150.104.25"), 2004)
            };

            Communication Voz2Vrt = new Communication
            {
                Version = "CRANE",
                Name = "Voz2Vrt",
                ID = 1,
                PartnerID = 107,
                SimulateMFCS = false,
                SendTimeOut = TimeSpan.FromSeconds(20),
                RcvTimeOut = TimeSpan.FromSeconds(20),
                KeepALifeTelegram = new TelegramLife(),
                KeepALifeTime = TimeSpan.FromSeconds(3),
                SendPoint = new IPEndPoint(IPAddress.Parse("10.150.104.22"), 2003),
                RcvPoint = new IPEndPoint(IPAddress.Parse("10.150.104.22"), 2004)
            };


            Communication Pvp1Gvt = new Communication
            {
                Version = "CRANE",
                Name = "Pvp1Gvt",
                ID = 1,
                PartnerID = 103,
                SimulateMFCS = false,
                SendTimeOut = TimeSpan.FromSeconds(20),
                RcvTimeOut = TimeSpan.FromSeconds(20),
                KeepALifeTelegram = new TelegramLife(),
                KeepALifeTime = TimeSpan.FromSeconds(3),
                SendPoint = new IPEndPoint(IPAddress.Parse("10.150.104.23"), 2003),
                RcvPoint = new IPEndPoint(IPAddress.Parse("10.150.104.23"), 2004)
            };

            Communication Conv1 = new Communication
            {
                Version = "CRANE",
                Name = "Conv1",
                ID = 1,
                PartnerID = 101,
                SimulateMFCS = false,
                SendTimeOut = TimeSpan.FromSeconds(20),
                RcvTimeOut = TimeSpan.FromSeconds(20),
                KeepALifeTelegram = new TelegramLife(),
                KeepALifeTime = TimeSpan.FromSeconds(3),
                SendPoint = new IPEndPoint(IPAddress.Parse("10.150.104.20"), 2003),
                RcvPoint = new IPEndPoint(IPAddress.Parse("10.150.104.20"), 2004)
            };

            Communication Conv2 = new Communication
            {
                Version = "CRANE",
                Name = "Conv2",
                ID = 1,
                PartnerID = 102,
                SimulateMFCS = false,
                SendTimeOut = TimeSpan.FromSeconds(20),
                RcvTimeOut = TimeSpan.FromSeconds(20),
                KeepALifeTelegram = new TelegramLife(),
                KeepALifeTime = TimeSpan.FromSeconds(3),
                SendPoint = new IPEndPoint(IPAddress.Parse("10.150.104.21"), 2003),
                RcvPoint = new IPEndPoint(IPAddress.Parse("10.150.104.21"), 2004)
            };

            return;

            Voz2Vil1.StartCommunication();
            Voz2Vil2.StartCommunication();
            Voz2Vrt.StartCommunication();
            Pvp1Gvt.StartCommunication();
            Conv1.StartCommunication();
            Conv2.StartCommunication();

            Console.WriteLine("Communication started...");
            do
            {
            }
            while (Console.ReadKey().KeyChar != 'K');

            Console.WriteLine("Communication stopped...");

            Voz2Vil1.StopCommunication();
            Voz2Vil2.StopCommunication();
            Voz2Vrt.StopCommunication();
            Pvp1Gvt.StopCommunication();
            Conv1.StopCommunication();
            Conv2.StopCommunication();


            /*            Communication plc1 = new Communication
                        {
                            Version = "CRANE",
                            Name = "PLC_CRANE1",
                            ID = 100, 
                            PartnerID = 1,
                            SimulateMFCS = false,
                            SendTimeOut = TimeSpan.FromSeconds(20),
                            RcvTimeOut = TimeSpan.FromSeconds(20),
                            KeepALifeTelegram = new TelegramLife(),
                            KeepALifeTime = TimeSpan.FromSeconds(3),
                            SendPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 1000),
                            RcvPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"),1001)
                        };

                        Communication client = new Communication
                        {
                            Version = "CRANE",
                            Name = "MFCS",
                            ID = 1,
                            PartnerID = 100,
                            SimulateMFCS = true,
                            SendTimeOut = TimeSpan.FromSeconds(20),
                            RcvTimeOut = TimeSpan.FromSeconds(20),
                            KeepALifeTelegram = new TelegramLife(),
                            KeepALifeTime = TimeSpan.FromSeconds(3),
                            SendPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 1001),
                            RcvPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 1000)
                        };

                        plc1.StartCommuncation();
                        client.StartCommuncation();

                        char ch;
                        do
                        {
                            ch = Console.ReadKey().KeyChar;
                            if (ch == '1')
                                if (client.MutexSendBuffer.WaitOne(1000))
                                {
                                    client.SendTelegrams.Add(new TelegramPLCStatus {Name="client->plc::Toni"});
                                    client.MutexSendBuffer.ReleaseMutex();
                                }
                            if (ch == '2')
                                if (plc1.MutexSendBuffer.WaitOne(1000))
                                {
                                    plc1.SendTelegrams.Add(new TelegramPLCStatus {Name="plc->client::Bracic" });
                                    plc1.MutexSendBuffer.ReleaseMutex();
                                }
                        }
                        while (ch != ' ');

                        plc1.StopCommunication();
                        client.StopCommunication();
            */
        }
    }
}
