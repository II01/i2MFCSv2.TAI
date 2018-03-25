using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;
using Telegrams.Communication;


namespace Telegrams.WcfService
{
    // NOTE: You can use the "Rename" command on the "Refactor" menu to change the class name "Service1" in both code and config file together.
    [ServiceBehavior(ConcurrencyMode = ConcurrencyMode.Single, InstanceContextMode = InstanceContextMode.PerSession)]
    public class WCF_RcvTelProxy : IWCF_RcvTelProxy, IDisposable
    {
        private MyRcvTCPClient _comm;

        private IPAddress _IP;
        private int _port;
        private TimeSpan _timeout;
        private string _name;
        private string _version;

        public void Init(string name, string addr, int SendPort, int timeoutSec, string version)
        {
            try
            {
                _IP = IPAddress.Parse(addr);
                _port = SendPort;
                _timeout = TimeSpan.FromSeconds(timeoutSec);
                _name = name;
                _version = version;
                _comm = new MyRcvTCPClient { Name = _name, IP = _IP, Port = _port, TimeOut = _timeout, Version = _version };
            }
            catch (Exception ex)
            {
                throw new FaultException(ex.Message);
            }
        }


        public async Task<Telegram> ReadAsync()
        {
            try
            {
                if(_comm == null)
                    _comm = new MyRcvTCPClient { Name = _name, IP = _IP, Port = _port, TimeOut = _timeout, Version = _version };

                return await _comm.Receive().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                try
                {
                    _comm.Close();
                }
                catch { }
                _comm = null;
                throw new FaultException(ex.Message);
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
                    _comm?.Dispose();
                    _comm = null;
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~WCF_RcvTelProxy() {
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


    // NOTE: You can use the "Rename" command on the "Refactor" menu to change the class name "Service1" in both code and config file together.
    [ServiceBehavior(ConcurrencyMode = ConcurrencyMode.Single, InstanceContextMode = InstanceContextMode.PerSession)]
    public class WCF_SendTelProxy : IWCF_SendTelProxy, IDisposable
    {
        private MySendTCPClient _comm;
        private IPAddress _IP;
        private int _port;
        private TimeSpan _timeout;
        private string _name;

        public void Init(string name, string addr, int SendPort, int timeoutSec, string version)
        {
            try
            {
                _IP = IPAddress.Parse(addr);
                _port = SendPort;
                _timeout = TimeSpan.FromSeconds(timeoutSec);
                _name = name;
                _comm = new MySendTCPClient { Name = _name, IP = _IP, Port = _port, TimeOut = _timeout};
            }
            catch (Exception ex)
            {
                throw new FaultException(ex.Message);
            }
        }


        public async Task<Telegram> SendAsync(Telegram t)
        {
            try
            {
                if (_comm == null)
                    _comm = new MySendTCPClient { Name = _name, IP = _IP, Port = _port, TimeOut = _timeout };
                return await _comm.Send(t).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                try
                {
                    _comm.Close();
                }
                catch { }
                _comm = null;
                throw new FaultException(ex.Message);
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
                }

                _comm?.Dispose();
                _comm = null;
                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~WCF_SendTelProxy() {
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
