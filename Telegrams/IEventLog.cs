using Database;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Telegrams
{
    public interface IEventLog
    {
        void AddEvent(EnumEventSeverity s, EnumEventType t, string str);
    }

}
