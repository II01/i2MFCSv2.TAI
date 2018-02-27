using Database;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Warehouse.Common;
using Warehouse.Model;

namespace Warehouse.ConveyorUnits
{
    [DataContract]    // big cased because Alarm already exists
    public class ALARM
    {
        [DataMember]
        [XmlAttribute]
        public int ID { get; set; }
        [DataMember]
        [XmlAttribute]
        public int Offset { get; set; }
        [DataMember]
        [XmlAttribute]
        public int Range { get; set; }


    }


    [DataContract]
    public class Sensor
    {
        [DataMember]
        [XmlAttribute]
        public int Offset { get; set; }
        [DataMember]
        [XmlAttribute]
        public string Description { get; set; }
        [DataMember]
        [XmlAttribute]
        public string Reference { get; set; }
        [DataMember]
        [XmlIgnore]
        public bool Active { get; set; }
    }

    [DataContract]
    public class State
    {
        [DataMember]
        [XmlAttribute]
        public int Offset { get; set; }
        [DataMember]
        [XmlAttribute]
        public string Description { get; set; }
        [DataMember]
        [XmlIgnore]
        public bool Active { get; set; }
    }

    [DataContract]
    [KnownType(typeof(CraneInfo))]
    [KnownType(typeof(ConveyorInfo))]
    [KnownType(typeof(SegmentInfo))]
    [KnownType(typeof(SimpleCommand))]
    [KnownType(typeof(SimpleCraneCommand))]
    [KnownType(typeof(SimpleConveyorCommand))]
    [KnownType(typeof(LPosition))]
    public class ConveyorBasicInfo
    {
        [XmlIgnore]
        [DataMember]
        public Int16 AlarmID { get; set; }
        [XmlIgnore]
        [DataMember]
        public Int16 Fault { get; set; }
        [DataMember]
        public List<ALARM> AlarmList { get; set; }
        [DataMember]
        public List<Sensor> SensorList { get; set; }
        [XmlIgnore]
        [DataMember]
        public Dictionary<string, Sensor> Sensor { get; set; }
        [DataMember]
        [XmlIgnore]
        public List<int> ActiveAlarms { get; set; }
        [DataMember]
        [XmlIgnore]
        public BitArray Status { get; set; }
        [DataMember]
        [XmlIgnore]
        public BitArray State { get; set; }
        [DataMember]
        [XmlIgnore]
        public bool Online { get; set; }
        [XmlIgnore]
        [DataMember]
        public string Name { get; set; }

        public ConveyorBasicInfo()
        {
            ActiveAlarms = new List<int>();
        }

        /*        public ConveyorBasicInfo(SerializationInfo info, StreamingContext context) : base()
                {
                    AlarmID = info.GetInt16("AlarmID");
                    Fault = info.GetInt16("Fault");
                    AlarmList = (List<ALARM>) info.GetValue("AlarmList", AlarmList.GetType());
                    SensorList = (List<Sensor>)info.GetValue("SensorList", SensorList.GetType());
                    ActiveAlarms = (List<int>) info.GetValue("ActiveAlarms", ActiveAlarms.GetType());
                }*/


        public void SetAlarms(BitArray alarms, BasicWarehouse wh)
        {
            try
            {
                if (AlarmList != null)
                {
                    //                    ActiveAlarms.Clear();
                    foreach (ALARM a in AlarmList)
                    {
                        for (int i = 0; i < a.Range; i++)
                        {
                            if (alarms[a.Offset + i] && !ActiveAlarms.Any(aa => aa == a.ID + i))
                            {
                                ActiveAlarms.Add(a.ID + i);
                                wh.DBService.AddAlarm(Name, (a.ID + i).ToString(), Alarm.EnumAlarmStatus.Active, Alarm.EnumAlarmSeverity.Error);
                            }
                            else if (!alarms[a.Offset + i] && ActiveAlarms.Any(aa => aa == a.ID + i))
                            {
                                ActiveAlarms.Remove(a.ID + i);
                                wh.DBService.UpdateAlarm(Name, (a.ID + i).ToString(), Alarm.EnumAlarmStatus.Removed);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new ConveyorBasicException(String.Format("ConveyorBasicInfo.SetAlarms failed. Reason:{0}", ex.Message));
            }
        }

        public void SetSensors(BitArray sensors)
        {
            try
            {
                SensorList?.ForEach(prop => prop.Active = sensors[prop.Offset]);
            }
            catch (Exception ex)
            {
                throw new ConveyorBasicException(String.Format("ConveyorBasicInfo.SetSensors failed. Reason:{0}", ex.Message));
            }
        }


        public virtual void Initialize()
        {
            try
            {
                Sensor = SensorList?.ToDictionary(prop => prop.Reference);
            }
            catch (Exception ex)
            {
                throw new ConveyorBasicException(String.Format("ConveyorBasicInfo.Initialize failed. Reason:{0}", ex.Message));
            }
        }
    }
}
