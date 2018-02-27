using Database;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Telegrams;
using Warehouse.ConveyorUnits;
using Warehouse.Model;
using Warehouse.WMSConsumerServiceReference;

namespace Warehouse.WMS
{
    public class BasicWMS
    {
        private BasicWarehouse Warehouse { get; set; }
        private Dictionary<string, string> SegStatus;
        public bool WMSSimulation {get; set; }

        public BasicWMS()
        {
        }

        public void Initialize(BasicWarehouse w)
        {
            Warehouse = w;
            Warehouse.OnCommandFinish = OnCommandFinished;
            Warehouse.OnMaterialMove = OnMaterialMoved;

            SegStatus = new Dictionary<string, string>();
            Warehouse.SegmentList.ForEach(s => SegStatus.Add(s.Name, ""));
            Warehouse.SegmentList.ForEach(s => s.NotifyVM.Add(OnSegmentChanged));
        }

        string ConvertLocation(string loc)
        {
            return loc;

            /*            
                        try
                        {
                            if (loc[0] == 'W')
                            {
                                string[] strl = loc.Substring(2).Split(':');
                                if (strl.Count() == 4)
                                    return string.Format("R{0:d2}{1:d2}{2:d1}{3:d1}", strl[0], strl[1], strl[2], strl[3]);
                                else
                                    return "?";

                            }
                            else if (loc[0] == 'R')
                            {
                                if (loc.Length == 7 && Int32.TryParse(loc.Substring(1), out int val))
                                    return string.Format("W:{0:d2}:{1:d3}:{2:d1}:{3:d1}", loc.Substring(1,2), loc.Substring(3, 2), loc.Substring(5, 1), loc.Substring(6, 1));
                                else
                                    return "?";
                            }
                            else
                                return loc;
                        }
                        catch(Exception e)
                        {
                            Warehouse.AddEvent(Event.EnumSeverity.Error, Event.EnumType.Exception,
                                               string.Format("{0}.{1}: {2} (Location {3})",
                                               this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message, loc));
                            return "?";
                        }
            */

        }

        public string OnRequest(string wmsid, string item, string instruction, string arguments)
        {
            try
            {
                Command.EnumCommandTask? task = null;

                item = item.ToUpper();
                instruction = instruction.ToUpper();
                arguments = arguments.ToUpper();
                string[] args = arguments.Split(';');

                if (!Int32.TryParse(wmsid, out int id))
                    return "FALSE; WMSID NOT A NUMBER";
                if (Warehouse.DBService.WMSIDExists(id))
                    return "FALSE; WMSID EXISTS";

                switch (item)
                {
                    case "JOB":
                        int commandID;
                        switch (instruction)
                        {
                            case "INFO": task = Command.EnumCommandTask.InfoCommand; break;
                            case "DELETE": task = Command.EnumCommandTask.CancelCommand; break;
                            default: return "FALSE; INSTRUCTION";
                        }
                        if (!Int32.TryParse(arguments, out commandID))
                            return "FALSE; ARGUMENTS";
                        else if (!Warehouse.DBService.WMSIDExists(commandID))
                            return "FALSE; NOWMSID";
                        else
                        {
                            Command cmd = Warehouse.DBService.FindCommandByWMS_ID(commandID);
                            if (cmd == null || cmd.Status == Command.EnumCommandStatus.Canceled || cmd.Status == Command.EnumCommandStatus.Finished)
                                return "FALSE; NODELETE";
                            else
                                Warehouse.DBService.AddCommand(new CommandCommand
                                {
                                    WMS_ID = id,
                                    Task = task.Value,
                                    CommandID = cmd.ID,
                                    Info = string.Format("{0};{1}", instruction, arguments),
                                    Status = Command.EnumCommandStatus.NotActive,
                                    Time = DateTime.Now
                                });
                        }
                        return "TRUE";

                    case "SEGMENT":
                        string segment;
                        switch (instruction)
                        {
                            case "INFO": task = Command.EnumCommandTask.SegmentInfo; break;
                            case "RESET": task = Command.EnumCommandTask.SegmentReset; break;
                            case "START": task = Command.EnumCommandTask.SegmentOn; break;
                            case "STOP": task = Command.EnumCommandTask.SegmentOff; break;
                            default: return "FALSE; INSTRUCTION";
                        }
                        if (arguments == "ALL")
                            segment = "*";
                        else
                        {
                            Segment seg = Warehouse.SegmentList.FirstOrDefault(p => p.Name == arguments);
                            if (seg == null)
                                return "FALSE; SEGMENT";
                            segment = seg.Name;
                        }
                        Warehouse.DBService.AddCommand(new CommandSegment
                        {
                            WMS_ID = id,
                            Task = task.Value,
                            Segment = segment,
                            Info = string.Format("{0};{1}", instruction, arguments),
                            Status = Command.EnumCommandStatus.NotActive,
                            Time = DateTime.Now
                        });
                        return "TRUE";

                    case "LOCATION":
                        int? mat = null;
                        string loc = ConvertLocation(args[0]);
                        if (instruction == "INFO")
                        {
                            if (args.Count() != 1)
                                return "FALSE; ARGUMENTS";
                            else if (Warehouse.DBService.FindPlaceID(loc) == null)
                                return "FALSE; LOCATION";
                            task = Command.EnumCommandTask.InfoMaterial;
                        }
                        else if (instruction == "SLOT")
                        {
                            if (args.Count() != 1)
                                return "FALSE; ARGUMENTS";
                            else if (Warehouse.DBService.FindPlaceID(loc) == null)
                                return "FALSE; LOCATION";
                            task = Command.EnumCommandTask.InfoSlot;
                        }
                        else if (instruction == "CREATE" || instruction == "DELETE")
                        {
                            int m = 0;
                            if (args.Count() != 2)
                                return "FALSE; ARGUMENTS";
                            else if (Warehouse.DBService.FindPlaceID(loc) == null)
                                return "FALSE; LOCATION";
                            else if (args[1].Length != 10 || args[1][0] != 'P' || !Int32.TryParse(args[1].Substring(1), out m))
                                return "FALSE; TUID";
                            mat = m;
                            if (instruction == "CREATE")
                                task = Command.EnumCommandTask.CreateMaterial;
                            else
                                task = Command.EnumCommandTask.DeleteMaterial;
                        }
                        if (mat != null)
                            Warehouse.DBService.FindMaterialID(mat.Value, true);

                        Warehouse.DBService.AddCommand(new CommandMaterial
                        {
                            WMS_ID = id,
                            Task = task.Value,
                            Material = mat,
                            Source = loc,
                            Target = loc,
                            Info = string.Format("{0};{1}", instruction, arguments),
                            Status = Command.EnumCommandStatus.NotActive,
                            Time = DateTime.Now,                             
                        });
                        return "TRUE";

                    case "TASK":
                        int priority;
                        int material;
                        string src = ConvertLocation(args[1]);
                        string trgt = ConvertLocation(args[2]);

                        if (args.Count() != 4)
                            return "FALSE; ARGUMENTS";
                        else if (instruction != "MOVE")
                            return "FALSE; INSTRUCTION";
                        else if (args[0].Length != 10 || args[0][0] != 'P' || !Int32.TryParse(args[0].Substring(1), out material))
                            return "FALSE; TUID";
                        else if (Warehouse.DBService.FindPlaceID(src) == null)
                            return "FALSE; SOURCE";
                        else if (Warehouse.DBService.FindPlaceID(trgt) == null)
                            return "FALSE; TARGET";
                        else if (!Warehouse.RouteExists(src, trgt, false))
                            return "FALSE; PATH";
                        else if (!Int32.TryParse(args[3], out priority))
                            return "FALSE; PRIORITY";
                        else if (priority < 0 || priority > 9)
                            return "FALSE; PRIORITY";
                        Warehouse.DBService.FindMaterialID(material, true);
                        Warehouse.DBService.AddCommand(new CommandMaterial
                        {
                            WMS_ID = id,
                            Task = Command.EnumCommandTask.Move,
                            Material = material,
                            Source = src,
                            Target = trgt,
                            Priority = priority,
                            Info = string.Format("{0};{1}", instruction, arguments),
                            Status = Command.EnumCommandStatus.NotActive,
                            Time = DateTime.Now,
                        });
                        return "TRUE";
                    default:
                        return "FALSE; ITEM";
                }
            }
            catch(Exception e)
            {
                throw new Exception(string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
            }

        }

        public bool SendCommandInfo(Command cmd)
        {
            try
            {
                if (cmd.WMS_ID == 0)
                    return false;

                string wmsid = cmd.WMS_ID.ToString();

                string item = "";
                switch (cmd.Task)
                {
                    case Command.EnumCommandTask.CancelCommand:
                    case Command.EnumCommandTask.InfoCommand:
                        item = "JOB";
                        break;
                    case Command.EnumCommandTask.SegmentInfo:
                    case Command.EnumCommandTask.SegmentOn:
                    case Command.EnumCommandTask.SegmentOff:
                    case Command.EnumCommandTask.SegmentReset:
                        item = "SEGMENT";
                        break;
                    case Command.EnumCommandTask.CreateMaterial:
                    case Command.EnumCommandTask.DeleteMaterial:
                    case Command.EnumCommandTask.InfoMaterial:
                    case Command.EnumCommandTask.InfoSlot:
                        item = "LOCATION";
                        break;
                    case Command.EnumCommandTask.Move:
                        item = "TASK";
                        break;
                }

                string status = "";
                string info = "";
                switch (cmd.Status)
                {
                    case Command.EnumCommandStatus.NotActive:
                        status = "QUEUED";
                        break;
                    case Command.EnumCommandStatus.Active:
                        status = "EXECUTING";
                        break;
                    case Command.EnumCommandStatus.Canceled:
                        if (cmd.Reason != null)
                        {
                            switch (cmd.Reason)
                            {
                                case Command.EnumCommandReason.PLC:
                                case Command.EnumCommandReason.MFCS:
                                case Command.EnumCommandReason.Operator:
                                    status = "ERROR";
                                    break;
                                case Command.EnumCommandReason.LocationEmpty:
                                    status = "ERROR;";
                                    info = "SOURCEEMPTY";
                                    break;
                                case Command.EnumCommandReason.LocationFull:
                                    status = "ERROR";
                                    info = "TARGETFULL";
                                    break;
                                default:
                                    status = "DELETED";
                                    break;                                
                            }
                        }
                        else
                            status = "DELETED";
                        break;
                    case Command.EnumCommandStatus.Finished:
                        status = "COMPLETED";
                        break;
                }

  //              if ( WMSSimulation )
                    using (WMSConsumerClient client = new WMSConsumerClient())
                    {

                        Warehouse.AddEvent(Event.EnumSeverity.Event, Event.EnumType.WMS,
                                           string.Format("WMS_Status called ({0}|{1}|{2}|{3})", wmsid, item, status, info));

                        string retVal = client.WMS_Status(wmsid, item, status, info);

                        Warehouse.AddEvent(Event.EnumSeverity.Event, Event.EnumType.WMS,
                                           string.Format("WMS_Status done ({0}|{1}|{2}|{3}): {4}", wmsid, item, status, info, retVal));

                        if (retVal.ToUpper() == "TRUE")
                            return true;
                        else
                            return false;
                    }
/*                else
                    using (Service1SoapClient client = new Service1SoapClient())
                    {

                        Warehouse.AddEvent(Event.EnumSeverity.Event, Event.EnumType.WMS,
                                           string.Format("WMS_Status called ({0}|{1}|{2}|{3})", wmsid, item, status, info));

                        string retVal = client.WMS_status(wmsid, item, status, info);

                        Warehouse.AddEvent(Event.EnumSeverity.Event, Event.EnumType.WMS,
                                           string.Format("WMS_Status done ({0}|{1}|{2}|{3}): {4}", wmsid, item, status, info, retVal));

                        if (retVal.ToUpper() == "TRUE")
                            return true;
                        else
                            return false;
                    }
*/
            }
            catch (Exception e)
            {
                Warehouse.AddEvent(Event.EnumSeverity.Error, Event.EnumType.Exception,
                                   string.Format("{0}.{1}: {2} (WMSID {3})",
                                   this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message, cmd.WMS_ID));
                return false;
            }
        }

        public void OnCommandFinished(Command cmd)
        {
            try
            {
                if (cmd.WMS_ID != 0)
                    SendCommandInfo(cmd);
            }
            catch (Exception e)
            {
                Warehouse.AddEvent(Event.EnumSeverity.Error, Event.EnumType.Exception,
                                   string.Format("{0}.{1}: {2} (WMSID {3})",
                                   this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message, cmd.WMS_ID));
            }
        }

        public async Task SendLocationInfo(Place place, Database.EnumMovementTask? task)
        {
            try
            {
                string wmsid = "0";
                string item = "LOCATION";
                string status = "COMPLETED";

                string act = "";
                if (task == null)
                    act = "INFO";
                else if (task == EnumMovementTask.Create)
                    act = "CREATE";
                else if (task == EnumMovementTask.Delete)
                    act = "DELETE";
                else if (task == EnumMovementTask.Move)
                    act = "MOVE";

                string err = "";
                if (Warehouse.Conveyor.ContainsKey(place.Place1) && Warehouse.Conveyor[place.Place1].Command_Status != null)
                {
                    err += Warehouse.Conveyor[place.Place1].Command_Status.Palette.FaultCode[0] ? "l" : "";
                    err += Warehouse.Conveyor[place.Place1].Command_Status.Palette.FaultCode[1] ? "r" : "";
                    err += Warehouse.Conveyor[place.Place1].Command_Status.Palette.FaultCode[2] ? "f" : "";
                    err += Warehouse.Conveyor[place.Place1].Command_Status.Palette.FaultCode[3] ? "b" : "";
                    err += Warehouse.Conveyor[place.Place1].Command_Status.Palette.FaultCode[4] ? "h" : "";
                    err += Warehouse.Conveyor[place.Place1].Command_Status.Palette.FaultCode[5] ? "w" : "";
                    err += Warehouse.Conveyor[place.Place1].Command_Status.Palette.FaultCode[6] ? "p" : "";
                    err += Warehouse.Conveyor[place.Place1].Command_Status.Palette.FaultCode[7] ? "n" : "";
                    err += Warehouse.Conveyor[place.Place1].Command_Status.Palette.FaultCode[9] ? "m" : "";
                    if (err != "")
                        err = string.Format("ERR:{0}", err);
                }

                string mat = "";
                if (place.Material > 0)
                    mat = String.Format("P{0:d9}", place.Material);

                string info = string.Format("{0};{1};{2};{3}", ConvertLocation(place.Place1), mat, act, err);

                if( WMSSimulation )
                    using (WMSConsumerClient client = new WMSConsumerClient())
                    {
                        if(place.Material < 1000000000 )
                        {
                            Warehouse.AddEvent(Event.EnumSeverity.Event, Event.EnumType.WMS,
                                               string.Format("WMS_Status called ({0}|{1}|{2}|{3})", wmsid, item, status, info));

                            string retVal = await client.WMS_StatusAsync(wmsid, item, status, info);

                            Warehouse.AddEvent(Event.EnumSeverity.Event, Event.EnumType.WMS,
                                               string.Format("WMS_Status done ({0}|{1}|{2}|{3}): {4}", wmsid, item, status, info, retVal));
                        }
                        else
                            Warehouse.AddEvent(Event.EnumSeverity.Event, Event.EnumType.WMS,
                                               string.Format("WMS_Status not called ({0}|{1}|{2}|{3}): {4}", wmsid, item, status, info, false));

                    }
/*                else
                    using (Service1SoapClient client = new Service1SoapClient())
                    {
                        if (place.Material < 1000000000)
                        {
                            Warehouse.AddEvent(Event.EnumSeverity.Event, Event.EnumType.WMS,
                                               string.Format("WMS_Status called ({0}|{1}|{2}|{3})", wmsid, item, status, info));

                            WMS_statusResponse sr = await client.WMS_statusAsync(wmsid, item, status, info);
                            string retVal = sr.Body.WMS_statusResult;

                            Warehouse.AddEvent(Event.EnumSeverity.Event, Event.EnumType.WMS,
                                               string.Format("WMS_Status done ({0}|{1}|{2}|{3}): {4}", wmsid, item, status, info, retVal));
                        }
                        else
                            Warehouse.AddEvent(Event.EnumSeverity.Event, Event.EnumType.WMS,
                                               string.Format("WMS_Status not called ({0}|{1}|{2}|{3}): {4}", wmsid, item, status, info, false));
                    }
*/
            }
            catch (Exception e)
            {
                Warehouse.AddEvent(Event.EnumSeverity.Error, Event.EnumType.Exception,
                                   string.Format("{0}.{1}: {2} ({3}|{4})",
                                   this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message, place.Place1, place.Material));
            }
        }

        public void SendPlaceInfo(PlaceID placeid)
        {
            try
            {
                string wmsid = "0";
                string item = "LOCATION";
                string status = "SLOT";

                string info = string.Format("{0};{1};{2}", ConvertLocation(placeid.ID), 
                                                           placeid.Size, 
                                                           placeid.Blocked? "BLOCKED": "AVAILABLE");

//                if( WMSSimulation )
                    using (WMSConsumerClient client = new WMSConsumerClient())
                    {
                        Warehouse.AddEvent(Event.EnumSeverity.Event, Event.EnumType.WMS,
                                            string.Format("WMS_Status called ({0}|{1}|{2}|{3})", wmsid, item, status, info));

                        string retVal = client.WMS_Status(wmsid, item, status, info);

                        Warehouse.AddEvent(Event.EnumSeverity.Event, Event.EnumType.WMS,
                                            string.Format("WMS_Status done ({0}|{1}|{2}|{3}): {4}", wmsid, item, status, info, retVal));
                    }
/*                else
                    using (Service1SoapClient client = new Service1SoapClient())
                    {
                        Warehouse.AddEvent(Event.EnumSeverity.Event, Event.EnumType.WMS,
                                            string.Format("WMS_Status called ({0}|{1}|{2}|{3})", wmsid, item, status, info));

                        string retVal = client.WMS_status(wmsid, item, status, info);

                        Warehouse.AddEvent(Event.EnumSeverity.Event, Event.EnumType.WMS,
                                            string.Format("WMS_Status done ({0}|{1}|{2}|{3}): {4}", wmsid, item, status, info, retVal));
                    }
*/
            }
            catch (Exception e)
            {
                Warehouse.AddEvent(Event.EnumSeverity.Error, Event.EnumType.Exception,
                                   string.Format("{0}.{1}: {2} {3})",
                                   this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message, placeid.ID));
            }
        }
        public void OnMaterialMoved(Place place, Database.EnumMovementTask task)
        {
            try
            {
                Place p = Warehouse.DBService.FindMaterial(place.Material);
                if(p == null || (p != null && p.Place1 == place.Place1))
                    SendLocationInfo(place, task);
            }
            catch (Exception e)
            {
                Warehouse.AddEvent(Event.EnumSeverity.Error, Event.EnumType.Exception,
                                   string.Format("{0}.{1}: {2} ({3}|{4})",
                                   this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message, place.Place1, place.Material));
            }
        }
        public void SendSegmentInfo(ConveyorBasicInfo inf, bool checkSimilarity)
        {
            try
            {
                if (inf == null)
                    return;

                string wmsid = "0";
                string item = "SEGMENT";
                string status = "COMPLETED";

                string name = "";
                string mode = "LOCAL";
                string active = "INACTIVE";
                string alarm = "NOALARM";
                string service = "AVAILABLE";

                if (inf is ConveyorInfo)
                {
                    name = inf.Name;
                    if (inf.Status[TelegramTransportStatus.STATUS_REMOTE])
                        mode = "REMOTE";
                    if (inf.Status[TelegramTransportStatus.STATUS_AUTOMATIC])
                        active = "ACTIVE";
                    if (inf.Status[TelegramTransportStatus.STATUS_FAULT])
                        alarm = "ALARM";
                    if (!inf.Online || inf.Status[TelegramTransportStatus.STATUS_LONGTERMFAULT])
                        service = "MAINTENANCE";
                }
                else if (inf is CraneInfo)
                {
                    if (inf.Status[TelegramCraneStatus.STATUS_REMOTE])
                        mode = "REMOTE";
                    if (inf.Status[TelegramCraneStatus.STATUS_AUTOMATIC])
                        active = "ACTIVE";
                    if (inf.Status[TelegramCraneStatus.STATUS_FAULT])
                        alarm = "ALARM";
                    if (!inf.Online || inf.Status[TelegramCraneStatus.STATUS_LONGTERMBLOCK])
                        service = "MAINTENANCE";
                }

                string info = string.Format("{0};{1};{2};{3};{4}", inf.Name, mode, active, alarm, service);

                if (checkSimilarity && (info == SegStatus[inf.Name]))
                    return;

                if( WMSSimulation )
                    using (WMSConsumerClient client = new WMSConsumerClient())
                    {
                        Warehouse.AddEvent(Event.EnumSeverity.Event, Event.EnumType.WMS,
                                            string.Format("WMS_Status called ({0}|{1}|{2}|{3})", wmsid, item, status, info));

                        string retVal = client.WMS_Status(wmsid, item, status, info);

                        Warehouse.AddEvent(Event.EnumSeverity.Event, Event.EnumType.WMS,
                                            string.Format("WMS_Status done ({0}|{1}|{2}|{3}): {4}", wmsid, item, status, info, retVal));

                        SegStatus[inf.Name] = info;
                    }
/*                else
                    using (Service1SoapClient client = new Service1SoapClient())
                    {
                        Warehouse.AddEvent(Event.EnumSeverity.Event, Event.EnumType.WMS,
                                            string.Format("WMS_Status called ({0}|{1}|{2}|{3})", wmsid, item, status, info));

                        string retVal = client.WMS_status(wmsid, item, status, info);

                        Warehouse.AddEvent(Event.EnumSeverity.Event, Event.EnumType.WMS,
                                            string.Format("WMS_Status done ({0}|{1}|{2}|{3}): {4}", wmsid, item, status, info, retVal));

                        SegStatus[inf.Name] = info;
                    }
*/
            }
            catch (Exception e)
            {
                Warehouse.AddEvent(Event.EnumSeverity.Error, Event.EnumType.Exception,
                                   string.Format("{0}.{1}: {2} ({3})",
                                   this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message, inf.Name));
            }
        }

        public void OnSegmentChanged(ConveyorBasicInfo inf)
        {
            try
            {
                SendSegmentInfo(inf, true);
            }
            catch (Exception e)
            {
                Warehouse.AddEvent(Event.EnumSeverity.Error, Event.EnumType.Exception,
                                   string.Format("{0}.{1}: {2} (Seg: {3})",
                                   this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message, inf.Name));
            }
        }
    }
}
