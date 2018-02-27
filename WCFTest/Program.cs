using System;
using System.Collections.Generic;
using Telegrams;
using System.Runtime.Serialization;
using System.IO;
using Warehouse.ConveyorUnits;
using Database;
using Warehouse.DataService;

namespace WCFTest
{
    class Program
    {
        public class CallBack 
        {
            public void TelegramSend(Telegram t)
            {
                Console.WriteLine(t.ToString());
            }
        }



        static void Main(string[] args)
        {

            DBService db = new DBService(null);



            CraneInfo tel = new CraneInfo
            {
                ActiveAlarms = new List<int> { 1,2,3},
                AlarmID = 100, 
                AlarmList = new List<Warehouse.ConveyorUnits.ALARM> { new Warehouse.ConveyorUnits.ALARM { ID = 1, Offset = 2, Range = 100} },
                Name = "test",
                Online = true,
                SensorList = new List<Sensor> { new Sensor { Description = "descr"} },
                State = new System.Collections.BitArray(10,false),                
                Fault = 111
            };

            MemoryStream ms = new MemoryStream();
            DataContractSerializer bf = new DataContractSerializer(typeof(ConveyorBasicInfo));

            try
            {
                bf.WriteObject(ms, tel);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            ms.Position = 0;

            ConveyorBasicInfo ci = (ConveyorBasicInfo) bf.ReadObject(ms); 

        }
    }
}
