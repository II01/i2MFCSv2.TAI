using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.ServiceModel;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Telegrams;
using SimpleLog;

namespace Telegrams.Communication
{


    public class MyTCPClient : TcpClient, IDisposable
    {
        public IPAddress IP { get; set; }
        public int Port { get; set; }
        public string Name { get; set; }

        public TimeSpan TimeOut { get; set; }
        protected NetworkStream Stream { get; set; }


        protected async Task MyConnect()
        {
            Task delay = Task.Delay(TimeOut);
            Task connect = ConnectAsync(IP, Port);
            var t = await Task.WhenAny(connect, delay).ConfigureAwait(false);
            await t.ConfigureAwait(false);
            if (connect.Status != TaskStatus.RanToCompletion)
            {
                Log.AddLog(Log.Severity.EXCEPTION, Name, $"Timeout...");
                throw new TimeoutException($"{Name}:{nameof(MyConnect)}");
            }
            Stream = GetStream();
            Log.AddLog(Log.Severity.EVENT, Name, $"Sucessfully connected...");
        }

        protected async Task<int> MyRead(byte[] buffer, int offset, int count)
        {
            Task delay = Task.Delay(TimeOut);
            Task<int> read = Stream.ReadAsync(buffer, offset, count);
            var t = await Task.WhenAny(read, delay).ConfigureAwait(false);
            await t.ConfigureAwait(false);
            if (read.Status != TaskStatus.RanToCompletion)
            {
                Log.AddLog(Log.Severity.EXCEPTION, Name,  $"Timeout...");
                throw new TimeoutException($"{Name}:{nameof(MyRead)}");
            }
            return read.Result;
        }

        protected async Task MyWrite(byte[] buffer, int offset, int count)
        {
            Task delay = Task.Delay(TimeOut);
            Task write = Stream.WriteAsync(buffer, offset, count);
            var t = await Task.WhenAny(write, delay).ConfigureAwait(false);
            await t.ConfigureAwait(false);
            if (write.Status != TaskStatus.RanToCompletion)
            {
                Log.AddLog(Log.Severity.EXCEPTION, Name, $"Timeout...");
                throw new TimeoutException($"{Name}:{nameof(MyConnect)}");
            }
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            Stream?.Close();
        }
    }


    public class MySendTCPClient : MyTCPClient
    {
        public DateTime LastSendTime { get; set; }
        public short Sequence { get; set; }

        // Main thread for sending telegrams
        public async Task Send(Telegram tel)
        {
            try
            {
                if (!Connected)
                    await MyConnect().ConfigureAwait(false);

                TelegramACK telAck = null;
                LastSendTime = DateTime.Now;
                tel.Sequence = Sequence;
                tel.Build();                    
                // Log?.AddLog(Log.Severity.EVENT, Name, "Communication.SendThread", String.Format("Start sending {0}", tel.ToString()));
                await MyWrite(tel.ByteBuffer, 0, tel.Length).ConfigureAwait(false);
                Log.AddLog(Log.Severity.EVENT, Name, $"Socket written {tel.ToString()}");
                tel.CommSendStatus = Telegram.CommSendStatusEnum.WaitACK;
                telAck = new TelegramACK();

                await MyRead(telAck.ByteBuffer, 0, telAck.DesignLength()).ConfigureAwait(false);
                telAck.ReadBuffer();
                telAck.Validate();
                if (telAck.Sequence != tel.Sequence)
                {
                    Log.AddLog(Log.Severity.EXCEPTION, Name, $"Sequence does not correspond");
                    throw new TelegramException("Sequence does not correspond");
                }
                Sequence = (Sequence < 99) ? (short)(Sequence + 1) : (short)0;
                Log.AddLog(Log.Severity.EVENT, Name, $"Send finished : {tel.ToString()}");
            }
            catch( Exception ex)
            {
                Log.AddLog(Log.Severity.EXCEPTION, Name, ex.Message);
                throw;
            }
        }
    }

    public class MyRcvTCPClient : MyTCPClient
    {
        private string _version;
        public string Version
        {
            get
            {
                return _version;
            }
            set
            {
                _version = value;
                AssignAllTelegrams(); // based on current version 
            }
        }


        public DateTime LastReceiveTime { get; set; }
        public Dictionary<int, Telegram> AllTelegrams { get; set; }
        protected void AssignAllTelegrams()
        {
            AllTelegrams = (from typ in Assembly.GetAssembly(typeof(Telegram)).GetExportedTypes()
                                         where (typ.GetCustomAttribute<TelegramSettingsAttr>() != null)
                                         where (typ.GetCustomAttribute<TelegramSettingsAttr>().ValidFor.Split(';').Contains(Version))
                                         select new
                                         {
                                             key = (int)typ.GetCustomAttribute<TelegramSettingsAttr>().Type,
                                             value = Activator.CreateInstance(typ) as Telegram
                                         }).ToDictionary(p => p.key, p => p.value);
        }


        // Main thread for receiving telegrams
        public async Task<Telegram> Receive()
        {
            try
            {
                if (!Connected)
                    await MyConnect().ConfigureAwait(false);

                // Read header
                Telegram tel = new TelegramOnlyHeader();
                int numRead = 0;
                do
                {
                    numRead += await MyRead(tel.ByteBuffer, numRead, tel.ByteBuffer.Length - numRead).ConfigureAwait(false);
                } while (numRead < tel.ByteBuffer.Length);

                // Read body 
                tel.ReadBuffer();
                tel.Validate(false);
                Telegram tel1 = Activator.CreateInstance(AllTelegrams[tel.TelType].GetType()) as Telegram;
                tel.ByteBuffer.CopyTo(tel1.ByteBuffer, 0);
                if (tel1.DesignLength() - tel.DesignLength() > 0 && tel.TelCode == 0)
                {
                    numRead = 0;
                    do
                    {
                        numRead += await MyRead(tel1.ByteBuffer, tel.DesignLength() + numRead, tel1.DesignLength() - tel.DesignLength() - numRead).ConfigureAwait(false);
                    } while (numRead < tel1.DesignLength() - tel.DesignLength());
                }

                // Prepare ACK telegram
                tel1.ReadBuffer();
                tel1.Validate();
                TelegramACK telACK = new TelegramACK { Sequence = tel.Sequence, TelCode = 0xFFFF, TelType = tel.TelType, Sender = tel.Receiver, Receiver = tel.Sender };
                telACK.Build();

                // Send ACK telegram
                await MyWrite(telACK.ByteBuffer, 0, telACK.ByteBuffer.Length).ConfigureAwait(false);

                tel1.CommRcvStatus = Telegram.CommRcvStatusEnum.NotifyDone;
                LastReceiveTime = DateTime.Now;
                Log.AddLog(Log.Severity.EVENT, Name, String.Format("Received finished : {0}", tel1.ToString()));
                return tel1;
            }
            catch( Exception ex)
            {
                Log.AddLog(Log.Severity.EXCEPTION, Name, ex.Message);
                throw;
            }
        }
    }
   

}
