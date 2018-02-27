using System.ComponentModel;

namespace Database
{
    public partial class Alarm
    {
        //  [DataContract]
        public enum EnumAlarmSeverity {Info=0, Warning, Error }
        //   [DataContract]
        public enum EnumAlarmStatus {None=0, Active, Ack, Removed}
    }
}
