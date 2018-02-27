using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Telegrams;

namespace MFCS.Communication
{
    public class EmptyCommunicator : BasicCommunicator
    {
        public async override Task SendThreading(CancellationToken ct) { }
        public async override Task RcvThreading(CancellationToken ct) { }

        override public void AddSendTelegram(Telegram t) { }
    }
}
