using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using System.Xml.Serialization;
using Telegrams;

namespace Communication
{


    [Serializable]
    public class CommunicationException : Exception
    {
        public CommunicationException(string msg) : base(msg)  
        { 
        }
    }

    public class DispatchNode
    {
        public Predicate<Telegram> DispatchTerm {get;set;} // lahko FUNC<in T1,in T2,out T3>
        public Action<Telegram> DispatchTo { get; set; }
    }


    public class Communicator : BasicCommunicator
    {
        [XmlIgnore]
        public Telegram CurrentSendTelegram { get; set; }
        // TimeSpan can't be Xml Serialized
 

        // IPEndPoint could not be serialized
        public string StringSendIPEndPoint
        {
            get { return String.Format("{0}:{1}", SendIPEndPoint.Address.ToString(), SendIPEndPoint.Port); }
            set {
                string[] str = value.Split(':');
                SendIPEndPoint = new IPEndPoint(IPAddress.Parse(str[0]), int.Parse(str[1]));
            }
        }

        // IPEndPoint could not be xml serialized
        public string StringRcvIPEndPoint
        {
            get { return String.Format("{0}:{1}", RcvIPEndPoint.Address.ToString(), RcvIPEndPoint.Port); }
            set
            {
                string[] str = value.Split(':');
                RcvIPEndPoint = new IPEndPoint(IPAddress.Parse(str[0]), int.Parse(str[1]));
            }
        }

        [XmlIgnore]
        public IPEndPoint SendIPEndPoint { get; set; }

        [XmlIgnore]
        public IPEndPoint RcvIPEndPoint { get; set; }


        [XmlIgnore]
        public Telegram KeepALifeTelegram { get; set; }


        // TimeSpan can't be Xml Serialized


        private short Sequence { get; set; }
        private int Retry { get; set; }


        private Socket RcvSocket { get; set; }
        private Socket SendSocket { get; set; }

        private TcpListener RcvListener { get; set; }
        private TcpListener SendListener { get; set; }

        private DateTime SendTime { get; set; }




        public Communicator() : base()
        {
            InitSendSocket();
            InitRcvSocket();
            KeepALifeTelegram = new TelegramLife();
        }


        // notify about new received telegram


        // Main thread for receiving telegrams
        public override void RcvThreading()
        {
            try
            {

                LastReceiveTime = DateTime.Now;

                // initialize from this thread

                while (Thread.CurrentThread.ThreadState == ThreadState.Background)
                {
                    try
                    {
                        lock (_lockRcvTelegram)
                            RcvTelegrams.RemoveAll(p => p.CommRcvStatus >= Telegram.CommRcvStatusEnum.NotifyDone);
                        if (DateTime.Now - LastReceiveTime > RcvTimeOut)
                        {
                            InitRcvSocket();
                            LastReceiveTime = DateTime.Now;
                            Log?.AddLog(Log.Severity.EXCEPTION, Name, "Communicator.RcvThreading", "Timeout receiving");
                        }
                        else if (DateTime.Now - LastNotifyTime > RefreshTime)
                        {
                            LastNotifyTime = DateTime.Now;
                            Log?.AddLog(Log.Severity.EVENT, Name, "Communicator.RcvThreading", "Refresh() is called");
                            CallRefresh();
                        }
                        else if (!RcvSocket.Connected)
                        {
                            InitRcvSocket();
                            ConnectRcvPartner();
                        }
                        else
                        {
                            if (RcvSocket.Available == 0)
                                Thread.Sleep(1);
                            else
                            {
                                Telegram tel = new TelegramOnlyHeader();
                                int numRead = 0;
                                int crRead = 0;
                                do
                                {
                                    crRead = RcvSocket.Receive(tel.ByteBuffer, numRead, tel.ByteBuffer.Length - numRead, SocketFlags.None);
                                    numRead += crRead;
                                    if (crRead == 0)
                                        throw new TelegramException("Receive bytes is 0.");
                                } while (numRead < tel.ByteBuffer.Length);
                                // Log?.AddLog(Log.Severity.EVENT, Name, "Communication.RcvThreading", String.Format("Received {0} bytes", numRead));
                                tel.ReadBuffer();
                                tel.Validate(false);
                                Telegram tel1 = Activator.CreateInstance(AllTelegrams[tel.TelType].GetType()) as Telegram;
                                tel.ByteBuffer.CopyTo(tel1.ByteBuffer, 0);
                                if (tel1.DesignLength() - tel.DesignLength() > 0 && tel.TelCode == 0)
                                {
                                    numRead = 0;
                                    crRead = 0;
                                    do
                                    {
                                        crRead = RcvSocket.Receive(tel1.ByteBuffer, tel.DesignLength() + numRead, tel1.DesignLength() - tel.DesignLength() - numRead, SocketFlags.None);
                                        numRead += crRead;
                                        if (crRead == 0)
                                            throw new TelegramException("Receive bytes is 0.");
                                    } while (numRead < tel1.DesignLength() - tel.DesignLength());
                                    // Log?.AddLog(Log.Severity.EVENT, Name, "Communication.RcvThreading", String.Format("Received {0} bytes", numRead));
                                }
                                tel1.ReadBuffer();
                                tel1.Validate();
                                NotifyRcv(tel1);
                                TelegramACK telACK = new TelegramACK();
                                telACK.Sequence = tel.Sequence;
                                telACK.TelCode = (System.UInt16)0xFFFF;
                                telACK.TelType = tel.TelType;
                                telACK.Sender = tel.Receiver;
                                telACK.Receiver = tel.Sender;
                                telACK.Build();
                                RcvSocket.Send(telACK.ByteBuffer);
                                tel1.CommRcvStatus = Telegram.CommRcvStatusEnum.NotifyDone;
                                LastReceiveTime = DateTime.Now;
                                LastNotifyTime = DateTime.Now;
                                Log?.AddLog(Log.Severity.EVENT, Name, "Communicator.RcvThreading", String.Format("Received finished : {0}", tel1.ToString()));
                            }
                        }
                    }
                    catch (SocketException ex)
                    {
                        Log?.AddLog(Log.Severity.EXCEPTION, Name, "Communicator.RcvThreading::SocketException", ex.Message);
                        Thread.Sleep(1000);
                    }
                    catch (TelegramException ex)
                    {
                        Log?.AddLog(Log.Severity.EXCEPTION, Name, "Communicator.RcvThreading::TelegramException", ex.Message);
                        Thread.Sleep(1000);
                    }
                    catch (KeyNotFoundException ex)
                    {
                        Log?.AddLog(Log.Severity.EXCEPTION, Name, "Communicator.RcvThreading::KeyNotFound", ex.Message);
                        Thread.Sleep(1000);
                    }
                    catch (CommunicationException ex)
                    {
                        Log?.AddLog(Log.Severity.EXCEPTION, Name, "Communicator.RcvThreading::CommunicationException", ex.Message);
                        Thread.Sleep(1000);
                    }
                    catch (ThreadAbortException ex)
                    {
                        Log?.AddLog(Log.Severity.EXCEPTION, Name, "Communicator.RcvThreading::ThreadAbortException", ex.Message);
                        return;
                    }
                    catch (ServiceCommunicatorException ex)
                    {
                        Log?.AddLog(Log.Severity.EXCEPTION, Name, "Communicator.RcvThreading::ServiceCommunicationException", ex.Message);
                        Thread.Sleep(3000);
                    }
                    catch (Exception ex)
                    {
                        Log?.AddLog(Log.Severity.EXCEPTION, Name, "Communicator.SendThread::Exception", ex.Message);
                        Thread.Sleep(1000);
                    }
                }
            }
            finally
            {
                RcvSocket.Close();
                RcvSocket.Dispose();
                SendSocket.Close();
                SendSocket.Dispose();
            }
        }


        // Main thread for sending telegrams
        public override void SendThreading()
        {
            TelegramACK telAck = null;

            try
            {

                lock (_lockSendTelegram)
                {
                    SendTelegrams.RemoveAll(p => p.CommSendStatus >= Telegram.CommSendStatusEnum.Ack);
                }

                LastSendTime = DateTime.Now;
                Telegram tel = null;
                Telegram telNew = null;

                while (Thread.CurrentThread.ThreadState == ThreadState.Background)
                    try
                    {
                        if (tel == null)
                            lock (_lockSendTelegram)
                            {
                                tel = SendTelegrams.FirstOrDefault(prop => prop.CommSendStatus < Telegram.CommSendStatusEnum.Ack);
                                CurrentSendTelegram = tel;
                            }
                        if (DateTime.Now - LastSendTime > SendTimeOut)
                        {
                            InitSendSocket();
                            if (tel != null)
                                tel.CommSendStatus = Telegram.CommSendStatusEnum.None;
                            LastSendTime = DateTime.Now;
                            Retry = 0;
                            Log?.AddLog(Log.Severity.EXCEPTION, Name, "Communicator.SendThread", "Send timeout, SendSocket reinitialized!");
                        }
                        else if (DateTime.Now - LastSendTime > KeepALifeTime && tel == null)
                        {
                            Telegram tRes = null;
                            lock (_lockSendTelegram)
                                tRes = SendTelegrams.FirstOrDefault(prop => prop.CommSendStatus < Telegram.CommSendStatusEnum.Ack);
                            if (tRes == null)
                                if (KeepALifeTelegram != null)
                                {
                                    Telegram t = Activator.CreateInstance(KeepALifeTelegram.GetType()) as Telegram;
                                    t.Sender = MFCS_ID;
                                    t.Receiver = PLC_ID;
                                    t.Build();
                                    tel = t;
                                    Log?.AddLog(Log.Severity.EVENT, Name, "Communicator.SendThread", String.Format("Adding KeepALife telegram"));
                                }
                        }
                        else if (!SendSocket.Connected)
                        {
                            InitSendSocket();
                            ConnectSendPartner();
                        }
                        else if (tel != null)
                        {
                            switch (tel.CommSendStatus)
                            {
                                case (Telegram.CommSendStatusEnum.None):
                                    CurrentSendTelegram = tel;
                                    tel.Sequence = Sequence;
                                    tel.Build();
                                    // Log?.AddLog(Log.Severity.EVENT, Name, "Communication.SendThread", String.Format("Start sending {0}", tel.ToString()));
                                    SendSocket.Send(tel.ByteBuffer, tel.Length, SocketFlags.None);
                                    Log?.AddLog(Log.Severity.EVENT, Name, "Communicator.SendThread", String.Format("Socket written {0}", tel.ToString()));
                                    tel.CommSendStatus = Telegram.CommSendStatusEnum.WaitACK;
                                    telAck = new TelegramACK();
                                    SendTime = DateTime.Now;
                                    break;
                                case (Telegram.CommSendStatusEnum.WaitACK):
                                    int numRead = 0;
                                    int crRead = 0;
                                    do
                                    {
                                        crRead = SendSocket.Receive(telAck.ByteBuffer, numRead, telAck.ByteBuffer.Length - numRead, SocketFlags.None);
                                        if (crRead == 0)
                                            throw new TelegramException("Receive bytes is 0.");
                                        numRead += crRead;
                                    } while (numRead < telAck.ByteBuffer.Length);
                                    telAck.ReadBuffer();
                                    Log?.AddLog(Log.Severity.EVENT, Name, "Communication.SendThread", String.Format("Received ACK {0}", telAck.ToString()));
                                    if (telAck.Validate() && telAck.Sequence == tel.Sequence)
                                    {
                                        tel.CommSendStatus = Telegram.CommSendStatusEnum.Ack;
                                        LastSendTime = DateTime.Now;
                                        Retry = 0;
                                        if (Sequence < 99)
                                            Sequence++;
                                        else
                                            Sequence = 0;
                                        Log?.AddLog(Log.Severity.EVENT, Name, "Communicator.SendThreading", String.Format("Send finished : {0}", tel.ToString()));
                                        NotifySend(tel);
                                        lock (_lockSendTelegram)
                                            telNew = SendTelegrams.FirstOrDefault(prop => prop.CommSendStatus < Telegram.CommSendStatusEnum.Ack);
                                        tel = null;
                                    }
                                    else
                                    {
                                        //                                      tel.CommSendStatus = Telegram.CommSendStatusEnum.None;
                                        Retry++;
                                        Log?.AddLog(Log.Severity.EXCEPTION, Name, "Communicator.SendThreading", String.Format("Retry increased - {0}", Retry));
                                    }
                                    break;
                                default:
                                    break;
                            }
                        }
                        else
                            WaitHandle.WaitAny(new WaitHandle[] { _newSendTelegram }, 100, true);
                    }
                    catch (SocketException ex)
                    {
                        Log?.AddLog(Log.Severity.EXCEPTION, Name, "Communicator.SendThread::SocketException", ex.Message);
                        Thread.Sleep(1000);
                    }
                    catch (TelegramException ex)
                    {
                        Log?.AddLog(Log.Severity.EXCEPTION, Name, "Communicator.SendThread::TelegramException", ex.Message);
                        Thread.Sleep(1000);
                    }
                    catch (ThreadAbortException ex)
                    {
                        Log?.AddLog(Log.Severity.EXCEPTION, Name, "Communicator.RcvThreading::ThreadAbortException", ex.Message);
                        return; 
                    }
                    catch (CommunicationException ex)
                    {
                        Log?.AddLog(Log.Severity.EXCEPTION, Name, "Communicator.SendThread::CommunicationException", ex.Message);
                        Thread.Sleep(1000);
                    }
                    catch (Exception ex)
                    {
                        Log?.AddLog(Log.Severity.EXCEPTION, Name, "Communicator.SendThread::Exception", ex.Message);
                        Thread.Sleep(1000);
                    }
            }
            finally
            {
                SendSocket.Close();
                SendSocket.Dispose();
                RcvSocket.Close();
                RcvSocket.Dispose();
            }
        }


        private void ConnectSendPartner()
        {
            SendSocket.Connect(SendIPEndPoint);
            Log?.AddLog(Log.Severity.EVENT, Name, "Communicator.ConnectSendPartner", "SendSocket connected");
        }


        private void ConnectRcvPartner()
        {
            RcvSocket.Connect(RcvIPEndPoint);
            Log?.AddLog(Log.Severity.EVENT, Name, "Communicator.ConnectRcvPartner", "RcvSocket connected");
        }

        // initialze RcvSocket
        private void InitRcvSocket()
        {
            if (RcvSocket != null)
            {
                RcvSocket.Close();
                Thread.Sleep(1000);
                //                RcvSocket.Dispose();
            }
            RcvSocket = new Socket(SocketType.Stream, ProtocolType.Tcp);
            RcvSocket.ReceiveTimeout = 10000;
            RcvSocket.SendTimeout = 10000;
            RcvSocket.Blocking = true;
            RcvSocket.LingerState.Enabled = true;
            RcvSocket.LingerState.LingerTime = 0;
        }

        private void InitSendSocket()
        {
            if (SendSocket != null)
            {
                SendSocket.Close();
                Thread.Sleep(1000);
                //                SendSocket.Dispose();
            }
            SendSocket = new Socket(SocketType.Stream, ProtocolType.Tcp);
            SendSocket.ReceiveTimeout = 10000;
            SendSocket.SendTimeout = 10000;
            SendSocket.Blocking = true;
            SendSocket.LingerState.Enabled = true;
            SendSocket.LingerState.LingerTime = 0;
        }

    }
}
