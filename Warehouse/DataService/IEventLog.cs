using Database;

namespace Warehouse.DataService
{
    public interface IEventLog
    {
        void AddEvent(Event.EnumSeverity s, Event.EnumType t, string str);
    }

}
