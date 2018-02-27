namespace Database
{
    public partial class Event
    {
      //  [DataContract]
        public enum EnumSeverity {Event=0, Error }
     //   [DataContract]
        public enum EnumType {Material = 0, Command, Program, Exception, WMS }
    }
}
