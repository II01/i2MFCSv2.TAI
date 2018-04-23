using Database;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace Warehouse.DataService
{


    [Serializable]
    public class DBServiceException : Exception
    {
        public DBServiceException(string s) : base(s)
        {
        }
    }

    public class DBService : IDBService
    {
        private IEventLog EventLog { get; set; }
        public const int PLACEID_MULTIPLE = 999;
        private Random Random { get; set; }
        
        public DBService(IEventLog w)
        {
            EventLog = w;
            Random = new Random();
        }

        public bool CheckIfPlaceBlocked(string place)
        {
            try
            {
                using (var dc = new MFCSEntities())
                    return dc.PlaceIDs.Find(place).Blocked;
            }
            catch (Exception ex)
            {
                EventLog?.AddEvent(Event.EnumSeverity.Error, Event.EnumType.Exception, ex.Message);
                throw new DBServiceException(String.Format("DBService.CheckIfPlaceBlocked failed ({0})", place));
            }
        }


        public SimpleSegmentCommand FindSimpleSegmentCommand(string segment)
        {
            try
            {
                using (var dc = new MFCSEntities())
                    return dc.SimpleCommands.FirstOrDefault(prop => (prop is SimpleSegmentCommand) && (prop as SimpleSegmentCommand).Segment == segment && prop.Status == SimpleCommand.EnumStatus.NotActive) as SimpleSegmentCommand;
            }
            catch (Exception ex)
            {
                EventLog?.AddEvent(Event.EnumSeverity.Error, Event.EnumType.Exception, ex.Message);
                throw new DBServiceException(String.Format("DBService.FindSimpleSegmentCommand failed ({0})", segment));
            }
        }

        public void TestFillReck(string reckLike, int num)
        {
            try
            {
                using (var dc = new MFCSEntities())
                {

                    var l = (from p in dc.PlaceIDs
                             where p.ID.StartsWith(reckLike) && !p.ID.StartsWith("W:32") && p.ID.EndsWith(":2")
                             join m in dc.Places on p.ID equals m.Place1 into inners
                             from od in inners.DefaultIfEmpty()
                             where od == null
                             select new {Location = p.ID}).ToList();


                    num = num > l.Count() ? l.Count() : num;
                    int max = l.Count();

                    List<int> list = new List<int>();

                    for (int i=0;i<num;i++)
                    {
                        int n = Random.Next(0, max--);
                        list.Add(n);
                    }

                    for (int i=0;i<num;i++)
                    {
                        int mat = Random.Next(1000, 1000000000);
                        while (FindMaterial(mat) != null)
                            mat = Random.Next(1000, 1000000000);
                        FindMaterialID(mat, true);
                        dc.Places.Add(new Place { Material = mat, Place1 = l[list[i]].Location, Time = DateTime.Now });
                        l.Remove(l[list[i]]);
                    }
                    dc.SaveChanges();
                }

            }
            catch (Exception ex)
            {
                EventLog?.AddEvent(Event.EnumSeverity.Error, Event.EnumType.Exception, ex.Message);
                throw new DBServiceException(String.Format("DBService.TestFillReck failed ({0},{1})", reckLike, num));
            }
        }


        public void TestToOut(string reckLike, int num)
        {
            try
            {
                using (var dc = new MFCSEntities())
                {
                    var target = (from p in dc.PlaceIDs
                                  where p.ID.StartsWith("W:32")
                                  select p).ToList();

                    var l = (from p in dc.Places
                             where p.Place1.StartsWith(reckLike) && !p.Place1.StartsWith("W:32")
                             select p).ToList();                

                    num = num > l.Count() ? l.Count() : num;
                    int max = l.Count();

                    List<int> list = new List<int>();

                    for (int i = 0; i < num; i++)
                    {
                        int n = Random.Next(0, max--);
                        list.Add(n);
                    }

                    for (int i = 0; i < num; i++)
                    {
                        dc.Commands.Add(new CommandMaterial
                        {
                            WMS_ID = 0,
                            Material = l[list[i]].Material,
                            Source = l[list[i]].Place1,
                            Target = target[Random.Next(target.Count)].ID,
                            Priority = 0, 
                            Task = Command.EnumCommandTask.Move, 
                            Status = Command.EnumCommandStatus.NotActive, 
                            Time = DateTime.Now,
                            Info = "test OUT"                            
                        });
                        l.Remove(l[list[i]]);
                    }
                    dc.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                EventLog?.AddEvent(Event.EnumSeverity.Error, Event.EnumType.Exception, ex.Message);
                throw new DBServiceException(String.Format("DBService.TestToOut failed ({0},{1})", reckLike, num));
            }

        }

        public void TestToIn(int bcrID, string reckLike, int num)
        {
            try
            {
                using (var dc = new MFCSEntities())
                {

                    var l = (from p in dc.PlaceIDs
                             where p.ID.StartsWith(reckLike) && !p.ID.StartsWith("W:32") && p.ID.EndsWith(":2")
                             join m in dc.Places on p.ID equals m.Place1 into inners
                             from od in inners.DefaultIfEmpty()
                             where od == null
                             select new { Location = p.ID }).ToList();


                    num = num > l.Count() ? l.Count() : num;
                    int max = l.Count();

                    List<int> list = new List<int>();

                    for (int i = 0; i < num; i++)
                    {
                        int n = Random.Next(0, max--);
                        list.Add(n);
                    }

                    for (int i = 0; i < num; i++)
                    {
                        int mat = bcrID + i;
                        FindMaterialID(mat, true);
                        dc.Commands.Add(new CommandMaterial
                        {
                            WMS_ID = 0,
                            Material = mat,
                            Source = "T014",
                            Target = l[list[i]].Location,
                            Priority = 0,
                            Task = Command.EnumCommandTask.Move,
                            Status = Command.EnumCommandStatus.NotActive,
                            Time = DateTime.Now,
                            Info = "test IN"
                        });
                        l.Remove(l[list[i]]);
                    }
                    dc.SaveChanges();
                }

            }
            catch (Exception ex)
            {
                EventLog?.AddEvent(Event.EnumSeverity.Error, Event.EnumType.Exception, ex.Message);
                throw new DBServiceException(String.Format("DBService.TestToIN failed ({0},{1},{2})", bcrID, reckLike, num));
            }
        }

        public SimpleCommand FindSimpleCommandByID(int id)
        {
            try
            {
                using (var dc = new MFCSEntities())
                {
                    return dc.SimpleCommands.Find(id);
                }
            }
            catch (Exception ex)
            {
                EventLog?.AddEvent(Event.EnumSeverity.Error, Event.EnumType.Exception, ex.Message);
                throw new DBServiceException(String.Format("DBService.FindSimpleConveyorCommandByID failed ({0})", id));
            }

        }

        public SimpleCraneCommand FindSimpleCraneCommandByID(int id)
        {
            try
            {
                using (var dc = new MFCSEntities())
                {
                    return dc.SimpleCommands.Find(id) as SimpleCraneCommand;
//                    return dc.SimpleCommands.FirstOrDefault(prop => prop.ID == id && prop is SimpleCraneCommand) as SimpleCraneCommand;
                }
            }
            catch (Exception ex)
            {
                EventLog?.AddEvent(Event.EnumSeverity.Error, Event.EnumType.Exception, ex.Message);
                throw new DBServiceException(String.Format("DBService.FindSimpleCraneCommandByID failed ({0})", id));
            }
        }

        public SimpleConveyorCommand FindSimpleConveyorCommandByID(int id)
        {
            try
            {
                using (var dc = new MFCSEntities())
                {
                    return dc.SimpleCommands.Find(id) as SimpleConveyorCommand;
//                    return dc.SimpleCommands.FirstOrDefault(prop => prop.ID == id && prop is SimpleConveyorCommand) as SimpleConveyorCommand;
                }
            }
            catch (Exception ex)
            {
                EventLog?.AddEvent(Event.EnumSeverity.Error, Event.EnumType.Exception, ex.Message);
                throw new DBServiceException(String.Format("DBService.FindSimpleConveyorCommandByID failed ({0})", id));
            }

        }
        public List<SimpleCommand> GetSimpleCommands(int? commandId, SimpleCommand.EnumStatus statusLessOrEqual, DateTime? dateFrom, DateTime? dateTo)
        {
            try
            {
                using (var dc = new MFCSEntities())
                {
                    var l = (from c in dc.SimpleCommands
                             where (c.Status <= statusLessOrEqual || 
                                    (!dateFrom.HasValue || (c.Time >= dateFrom.Value && c.Time <= dateTo.Value))) &&
                                   (!commandId.HasValue || (c.Command_ID == commandId.Value))
                             orderby c.ID descending
                             select c).Take(5000);
                    return l.ToList();
                }
            }
            catch (Exception e)
            {
                throw new Exception(string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
            }
        }
        public Command FindCommandByID(int id)
        {
            try
            {
                using (var dc = new MFCSEntities())
                {
                    return dc.Commands.Find(id);
                }
            }
            catch (Exception ex)
            {
                EventLog?.AddEvent(Event.EnumSeverity.Error, Event.EnumType.Exception, ex.Message);
                throw new DBServiceException(String.Format("DBService.FindCommandByID failed ({0})", id));
            }
        }
        public void CommandSimpleCommandsCancel(Command command)
        {
            try
            {
                using (var dc = new MFCSEntities())
                {
                    var l = from c in dc.SimpleCommands
                            where c.Command_ID == command.ID && c.Status == SimpleCommand.EnumStatus.NotActive
                            select c;
                    foreach (var c in l)
                    {
                        c.Reason = SimpleCommand.EnumReason.Command;
                        c.Status = SimpleCommand.EnumStatus.Canceled;
                    }
                    dc.SaveChanges();
                }
            }
            catch (Exception e)
            {
                throw new Exception(string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
            }
        }

        public Command FindCommandByWMS_ID(int wmsid)
        {
            try
            {
                using (var dc = new MFCSEntities())
                {
                    return dc.Commands.FirstOrDefault(c => c.WMS_ID == wmsid);
                }
            }
            catch (Exception ex)
            {
                EventLog?.AddEvent(Event.EnumSeverity.Error, Event.EnumType.Exception, ex.Message);
                throw new DBServiceException(String.Format("DBService.FindCommandByWMS_ID failed ({0})", wmsid));
            }
        }

        public void GenerateSegmentSimpleCommands(Command cmd, List<string> seg)
        {
            try
            {
                SimpleCommand.EnumTask sct;

                switch (cmd.Task)
                {
                    case Command.EnumCommandTask.SegmentOn:
                        sct = SimpleCommand.EnumTask.AutoOn;
                        break;
                    case Command.EnumCommandTask.SegmentOff:
                        sct = SimpleCommand.EnumTask.AutoOff;
                        break;
                    case Command.EnumCommandTask.SegmentReset:
                        sct = SimpleCommand.EnumTask.Reset;
                        break;
                    default:
                        sct = SimpleCommand.EnumTask.Info;
                        break;
                }

                seg.ForEach(s => AddSimpleCommand( new SimpleSegmentCommand
                {
                    Command_ID = cmd.ID,
                    Segment = s,
                    Task = sct,
                    Status = SimpleCommand.EnumStatus.NotActive,
                    Time = DateTime.Now
                }));
            }
            catch (Exception ex)
            {
                EventLog?.AddEvent(Event.EnumSeverity.Error, Event.EnumType.Exception, ex.Message);
                throw new DBServiceException(String.Format("DBService.CommandHasSimpleCommands failed ({0})", cmd.ID));
            }
        }

        public Command ExecuteCancelCommand(CommandCommand cmd)
        {
            try
            {
                Command c = null;

                using (var dc = new MFCSEntities())
                {
                    c = dc.Commands.Find(cmd.CommandID);
                    if(c!= null)
                    {
                        if(c.Task == Command.EnumCommandTask.CancelCommand)
                        {
                            var lna = from sc in dc.SimpleCommands
                                      where (sc.Command_ID == c.ID)
                                      select sc;
                            lna.ToList().ForEach(sc => { sc.Reason = SimpleCommand.EnumReason.Command; sc.Status = SimpleCommand.EnumStatus.Canceled; });
                            c.Status = Command.EnumCommandStatus.Canceled;
                            cmd.Status = Command.EnumCommandStatus.Finished;
                        }
                        else
                        {
                            var lna = from sc in dc.SimpleCommands
                                      where (sc.Command_ID == c.ID) && (sc.Status == SimpleCommand.EnumStatus.NotActive)
                                      select sc;
                            lna.ToList().ForEach(sc => sc.Status = SimpleCommand.EnumStatus.Canceled);
                            var la = from sc in dc.SimpleCommands
                                     where (sc.Command_ID == c.ID) && 
                                     (sc.Status > SimpleCommand.EnumStatus.NotActive && sc.Status <= SimpleCommand.EnumStatus.InPlc) &&
                                     (sc.Task >= SimpleCommand.EnumTask.Move && sc.Task <= SimpleCommand.EnumTask.Drop)
                                     select sc;
                            if (la.Count() == 0)
                            {
                                c.Status = Command.EnumCommandStatus.Canceled;
                                dc.Commands.Attach(c);
                                dc.Entry(c).State = System.Data.Entity.EntityState.Modified;
                                dc.SaveChanges();
                                cmd.Status = Command.EnumCommandStatus.Finished;
                            }
                            else
                            {
                                foreach (var sc in la.ToList())
                                {
                                    if (sc is SimpleCraneCommand)
                                    {
                                        dc.SimpleCommands.Add(new SimpleCraneCommand
                                        {
                                            Command_ID = cmd.ID,
                                            CancelID = sc.ID,
                                            Task = SimpleCommand.EnumTask.Cancel,
                                            Unit = (sc as SimpleCraneCommand).Unit,
                                            Source = sc.Source,
                                            Status = SimpleCommand.EnumStatus.NotActive,
                                            Time = DateTime.Now
                                        });
                                    }
                                }
                                if (cmd.Status < Command.EnumCommandStatus.Active)
                                {
                                    cmd.Status = Command.EnumCommandStatus.Active;
                                    UpdateCommand(cmd);
                                }
                                // TODO: discuss Toni
//                                c.Status = Command.EnumCommandStatus.Canceled;
//                                dc.Commands.Attach(c);
//                                dc.Entry(c).State = System.Data.Entity.EntityState.Modified;
//                                dc.SaveChanges();
//                                cmd.Status = Command.EnumCommandStatus.Finished;
                            }
                        }
                    }
                    dc.SaveChanges();
                    return c;
                }
            }
            catch (Exception ex)
            {
                EventLog?.AddEvent(Event.EnumSeverity.Error, Event.EnumType.Exception, ex.Message);
                throw new DBServiceException(String.Format("DBService.CancelCommand failed ({0}, {1})", cmd.ID, cmd.CommandID));
            }
        }



        public void AddEvent(Event.EnumSeverity s, Event.EnumType t, string str, DateTime dt)
        {
            using (var dc = new MFCSEntities())
            {
                dc.Events.Add(new Event
                {
                    Severity = s,
                    Type = t,
                    Text = str,
                    Time = dt
                });
                dc.SaveChanges();
            }
        }


        public void AddSimpleCommand(SimpleCommand cmd)
        {
            try
            {
                using (var dc = new MFCSEntities())
                {
                    dc.SimpleCommands.Add(cmd);
                    dc.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                EventLog?.AddEvent(Event.EnumSeverity.Error, Event.EnumType.Exception, ex.Message);
                throw new DBServiceException(String.Format("DBService.AddSimpleCommand failed ({0})", cmd != null ? cmd.ToString() : "null"));
            }

        }


        public void UpdateSimpleCommand(SimpleCommand cmd)
        {
            try
            {
                using (var dc = new MFCSEntities())
                {
                    dc.SimpleCommands.Attach(cmd);
                    dc.Entry(cmd).State = System.Data.Entity.EntityState.Modified;
                    dc.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                EventLog?.AddEvent(Event.EnumSeverity.Error, Event.EnumType.Exception, ex.Message);
                throw new DBServiceException(String.Format("DBService.UpdateSimpleCommand failed ({0})", cmd != null ? cmd.ToString() : "null"));
            }
        }


        public SimpleConveyorCommand FindFirstFastConveyorSimpleCommand(List<string> devices , bool automatic)
        {
            try
            {
                using (var dc = new MFCSEntities())
                {
                    var cmdLinq = (from sc in dc.SimpleCommands
                                   where sc.Status == SimpleCommand.EnumStatus.NotActive &&
                                         sc is SimpleConveyorCommand &&
                                         ((automatic && sc.Command_ID != null) || (!automatic && sc.Command_ID == null)) &&
                                         sc.Task >= SimpleCommand.EnumTask.Delete &&
                                         devices.Any(prop => prop == sc.Source)
                                   select sc).FirstOrDefault() as SimpleConveyorCommand;
                    return cmdLinq;
                }                
            }
            catch (Exception ex)
            {
                EventLog?.AddEvent(Event.EnumSeverity.Error, Event.EnumType.Exception, ex.Message);
                throw new DBServiceException(String.Format("DBService.FindFirstFastConveyorSimpleCommand failed ({0})", automatic));
            }
        }

        public SimpleCraneCommand FindFirstFastSimpleCraneCommand(string unit, bool automatic)
        {
            try
            {
                using (var dc = new MFCSEntities())
                {
                    return dc.SimpleCommands.FirstOrDefault(sc =>
                                       sc.Status == SimpleCommand.EnumStatus.NotActive &&
                                       sc is SimpleCraneCommand &&
                                       ((automatic && sc.Command_ID != null) || (!automatic && sc.Command_ID == null)) &&
                                       (sc as SimpleCraneCommand).Unit == unit &&
                                       sc.Task >= SimpleCommand.EnumTask.Delete) as SimpleCraneCommand;
                }
            }
            catch (Exception ex)
            {
                EventLog?.AddEvent(Event.EnumSeverity.Error, Event.EnumType.Exception, ex.Message);
                throw new DBServiceException(String.Format("DBService.FindFirstFastCraneSimpleCommand failed ({0},{1})", unit, automatic));
            }
        }


        public SimpleConveyorCommand FindFirstSimpleConveyorCommand(int material, string source, bool automatic)
        {
            try
            {
                using (var dc = new MFCSEntities())
                {
                    return dc.SimpleCommands.FirstOrDefault(p => p is SimpleConveyorCommand && p.Material == material &&
                                                              ((automatic && p.Command_ID != null) || (!automatic && p.Command_ID == null)) &&
                                                              p.Status == SimpleCommand.EnumStatus.NotActive && p.Source == source &&
                                                              p.Task == SimpleCommand.EnumTask.Move) as SimpleConveyorCommand;
                }
            }
            catch (Exception ex)
            {
                EventLog?.AddEvent(Event.EnumSeverity.Error, Event.EnumType.Exception, ex.Message);
                throw new DBServiceException(String.Format("DBService.FindFirstSimpleConveyorCommand failed ({0},{1},{2})", material, source, automatic));
            }
        }


        public SimpleCraneCommand FindFirstSimpleCraneCommand(string unit, bool automatic)
        {
            try
            {
                using (var dc = new MFCSEntities())
                {
                    return dc.SimpleCommands.FirstOrDefault(p => p is SimpleCraneCommand &&
                                                              ((automatic && p.Command_ID != null) || (!automatic && p.Command_ID == null)) &&
                                                              p.Status == SimpleCommand.EnumStatus.NotActive && 
                                                              (p as SimpleCraneCommand).Unit == unit && 
                                                              p.Task <= SimpleCommand.EnumTask.Create) as SimpleCraneCommand;  // Uros, da pobira tudi delete
                }
            }
            catch (Exception ex)
            {
                EventLog?.AddEvent(Event.EnumSeverity.Error, Event.EnumType.Exception, ex.Message);
                throw new DBServiceException(String.Format("DBService.FindFirstSimpleCraneCommand failed ({0},{1})", unit, automatic));
            }

        }

        public CommandMaterial FindFirstCommandStillInWarehouse(List<short> shelve, bool modeWMS, List<string> bannedPlaces)
        {
            try
            {
                using (var dc = new MFCSEntities())
                {
                    List<string> term = new List<string>();
                    foreach (short s in shelve)
                        term.Add(String.Format("W:{0:00}", s));
                    if (term.Count() == 0)
                        return null;
                    var command =  (from cmd in dc.Commands.OrderByDescending(k => k.Priority).ThenBy(kk => kk.ID)
                                    where cmd is CommandMaterial &&
                                          cmd.Task == Command.EnumCommandTask.Move &&
                                          ((modeWMS) || (!modeWMS && cmd.WMS_ID == 0))
                                    join p in dc.Places on (cmd as CommandMaterial).Material equals p.Material
                                    where p.Place1 == (cmd as CommandMaterial).Source &&
                                          cmd.Status < Command.EnumCommandStatus.Canceled &&
                                          term.Any(p => (cmd as CommandMaterial).Source.StartsWith(p))
                                    where !(from cmdSimple in dc.SimpleCommands where 
                                           cmdSimple.Source == (cmd as CommandMaterial).Source &&
                                           cmdSimple.Material == (cmd as CommandMaterial).Material &&
                                           cmdSimple.Status < SimpleCommand.EnumStatus.Canceled
                                           select cmdSimple).Any()
                                    where !bannedPlaces.Contains((cmd as CommandMaterial).Source)
                                    select (cmd as CommandMaterial)).FirstOrDefault();
                    return command;
                }
            }
            catch (Exception ex)
            {
                EventLog?.AddEvent(Event.EnumSeverity.Error, Event.EnumType.Exception, ex.Message);
                throw new DBServiceException(String.Format("DBService.FindFirstCommandStillInWarehouse failed ({0})", modeWMS));
            }
        }

        public CommandMaterial FindFirstCommand(int material, bool modeWMS)
        {
            try
            {
                using (var dc = new MFCSEntities())
                {
                    return (CommandMaterial)dc.Commands.OrderByDescending(k => k.Priority).ThenBy(kk => kk.ID).
                           FirstOrDefault(prop => (prop is CommandMaterial) && (prop as CommandMaterial).Material == material &&
                                                  prop.Task == Command.EnumCommandTask.Move &&
                                                  ((modeWMS) || (!modeWMS && prop.WMS_ID == 0)) &&
                                                   prop.Status < Command.EnumCommandStatus.Canceled);
                }
            }
            catch (Exception ex)
            {
                EventLog?.AddEvent(Event.EnumSeverity.Error, Event.EnumType.Exception, ex.Message);
                throw new DBServiceException(String.Format("DBService.FindFirstCommand failed ({0},{1})", material, modeWMS));
            }
        }

        public Command FindFirstNotMoveCommand(bool modeWMS)
        {
            try
            {
                using (var dc = new MFCSEntities())
                {
                    return dc.Commands.OrderByDescending(k => k.Priority).ThenBy(kk => kk.ID).
                           FirstOrDefault(p => p.Task != Command.EnumCommandTask.Move && 
                                               p.Status == Command.EnumCommandStatus.NotActive && 
                                               ((modeWMS) || (!modeWMS && p.WMS_ID == 0))); 
                }
            }
            catch (Exception ex)
            {
                EventLog?.AddEvent(Event.EnumSeverity.Error, Event.EnumType.Exception, ex.Message);
                throw new DBServiceException(String.Format("DBService.FindFirstNotMoveCommand failed."));
            }
        }

        public void AddCommand(Command cmd)
        {
            try
            {
                using (var dc = new MFCSEntities())
                {
                    dc.Commands.Add(cmd);
                    dc.SaveChanges();
                }                
            }
            catch (Exception ex)
            {
                EventLog?.AddEvent(Event.EnumSeverity.Error, Event.EnumType.Exception, ex.Message);
                throw new DBServiceException(String.Format("DBService.AddCommand failed ({0})", cmd != null ? cmd.ToString() : "null"));
            }
        }

        public void UpdateCommand(Command cmd)
        {
            try
            {
                using (var dc = new MFCSEntities())
                {
                    dc.Commands.Attach(cmd);
                    dc.Entry(cmd).State = System.Data.Entity.EntityState.Modified;
                    dc.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                EventLog?.AddEvent(Event.EnumSeverity.Error, Event.EnumType.Exception, ex.Message);
                throw new DBServiceException(String.Format("DBService.UpdateCommand failed ({0})", cmd != null ? cmd.ToString() : "null"));
            }
        }

        public List<Command> GetCommands(Command.EnumCommandStatus statusLessOrEqual, DateTime? dateFrom, DateTime? dateTo)
        {
            try
            {
                using (var dc = new MFCSEntities())
                {
                    var l = (from c in dc.Commands
                             where c.Status <= statusLessOrEqual || 
                                   (!dateFrom.HasValue || (c.Time >= dateFrom.Value && c.Time <= dateTo.Value))
                             orderby c.ID descending
                             select c).Take(5000);

                    return l.ToList();
                }
            }
            catch (Exception e)
            {
                throw new Exception(string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
            }
        }



        public void UpdateMovement(Movement move)
        {
            try
            {
                using (var dc = new MFCSEntities())
                {
                    dc.Movements.Attach(move);
                    dc.Entry(move).State = System.Data.Entity.EntityState.Modified;
                    dc.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                EventLog?.AddEvent(Event.EnumSeverity.Error, Event.EnumType.Exception, ex.Message);
                throw new DBServiceException(String.Format("DBService.UpdateMovement failed ({0})", move != null ? move.ID.ToString() : "null"));
            }
        }

        public Place MaterialCreate(string place, int material, bool create)
        {
            try
            {
                FindMaterialID(material, create);
                using (var dc = new MFCSEntities())
                {
                    Place p = new Place { Place1 = place, Material = material, Time = DateTime.Now };
                    dc.Places.Add(p);

                    Movement move = new Movement { Position = place, Material = material, Task = EnumMovementTask.Create, Time = DateTime.Now };
                    dc.Movements.Add(move);

                    dc.SaveChanges();
                    EventLog?.AddEvent(Event.EnumSeverity.Event, Event.EnumType.Material, String.Format("{0} created ({1})", place, material));
                    return p;
                }
            }
            catch (Exception ex)
            {
                EventLog?.AddEvent(Event.EnumSeverity.Error, Event.EnumType.Exception, ex.Message);
                throw new DBServiceException(String.Format("DBService.MaterialCreate failed ({0},{1},{2})", place, material, create));
            }

        }
   

        public Place FindPlace(string source)
        {
            try
            {
                using (var dc = new MFCSEntities())
                {
                    return dc.Places.FirstOrDefault(prop => prop.Place1 == source);
                }
            }
            catch (Exception ex)
            {
                EventLog?.AddEvent(Event.EnumSeverity.Error, Event.EnumType.Exception, ex.Message);
                throw new DBServiceException(String.Format("DBService.FindPlace failed ({0})", source));
            }
        }

        public void MaterialDelete(string place, int material)
        {
            try
            {
                using (var dc = new MFCSEntities())
                {
                    Place p = dc.Places.FirstOrDefault( prop => prop.Place1 == place && prop.Material == material);
                    if (p != null)
                    {
                        dc.Places.Attach(p);
                        dc.Places.Remove(p);

                        Movement move = new Movement { Position = place, Material = material, Task = EnumMovementTask.Delete, Time = DateTime.Now };
                        dc.Movements.Add(move);

                        dc.SaveChanges();
                    }
                    else
                        throw new DBServiceException("Place not correctly occupied.");
                }
                EventLog?.AddEvent(Event.EnumSeverity.Event, Event.EnumType.Material, String.Format("{0} removed ({1}) ", place, material));
            }
            catch (Exception ex)
            {
                EventLog?.AddEvent(Event.EnumSeverity.Error, Event.EnumType.Exception, ex.Message);
                throw new DBServiceException(String.Format("DBService.MaterialDelete failed ({0},{1})", place, material));
            }
        }

        public void InitialNotify(string source, int material)
        {
            try
            {
                Place s = FindPlace(source);
                Place s1 = FindMaterial(material);

                if (s == null && material != 0)
                {
                    if (s1 != null)
                        MaterialDelete(s1.Place1, s1.Material);
                    MaterialCreate(source, material, true);
                }
                else if (s != null && material != s.Material)
                {
                    MaterialDelete(s.Place1, s.Material);
                    if (material != 0)
                        MaterialCreate(source, material, true);
                }
                EventLog?.AddEvent(Event.EnumSeverity.Event, Event.EnumType.Material, String.Format("{0} InitialNotify({1})", source, material));
            }
            catch (Exception ex)
            {
                EventLog?.AddEvent(Event.EnumSeverity.Error, Event.EnumType.Exception, ex.Message);
                throw new DBServiceException(String.Format("DBService.InitialNotify failed ({0},{1})", source, material));
            }

        }
    
        public void MaterialMove(int material, string source, string target)
        {
            try
            {
                using (var dc = new MFCSEntities())
                {
                    Place s = dc.Places.FirstOrDefault( prop => prop.Place1 == source && prop.Material == material);
                    Place t = dc.Places.FirstOrDefault( prop => prop.Place1 == target);
                    PlaceID tID = dc.PlaceIDs.Find(target);
                    if (tID == null)
                        throw new DBServiceException(String.Format("{0} PlaceID unknown", target));
                    if (s == null)
                        throw new DBServiceException(String.Format("{0} source empty or wrong barcode", source));
                    if (t != null && tID.Size != PLACEID_MULTIPLE)
                        throw new DBServiceException(String.Format("{0} is not empty", target));

                    dc.Places.Attach(s);
                    dc.Places.Remove(s);
                    dc.SaveChanges();
                    t = new Place { Place1 = target, Material = material, Time = DateTime.Now };
                    dc.Places.Add(t);

                    Movement move = new Movement { Position = target, Material = material, Task = EnumMovementTask.Move, Time = DateTime.Now};
                    dc.Movements.Add(move);

                    dc.SaveChanges();
                    EventLog?.AddEvent(Event.EnumSeverity.Event, Event.EnumType.Material, String.Format("{0} -> {1} ({2})", source, target, material));
                }
            }
            catch (Exception ex)
            {
                EventLog?.AddEvent(Event.EnumSeverity.Error, Event.EnumType.Exception, ex.Message);
                throw new DBServiceException(String.Format("DBService.MaterialMove failed ({0},{1},{2})", material, source, target));
            }
        }

        public PlaceID FindPlaceID(string source)
        {
            try
            {
                using (var dc = new MFCSEntities())
                {
                    return dc.PlaceIDs.Find(source);
                }
            }
            catch (Exception ex)
            {
                EventLog?.AddEvent(Event.EnumSeverity.Error, Event.EnumType.Exception, ex.Message);
                throw new DBServiceException(String.Format("DBService.FindPlace failed ({0})", source));
            }
        }

        public Place FindMaterial(int material)
        {
            try
            {
                using (var dc = new MFCSEntities())
                {
                    return dc.Places.FirstOrDefault(prop => prop.Material == material);
                }
            }
            catch (Exception ex)
            {
                EventLog?.AddEvent(Event.EnumSeverity.Error, Event.EnumType.Exception, ex.Message);
                throw new DBServiceException(String.Format("DBService.FindMaterial failed ({0})", material));
            }
        }
        public int CountSimpleCraneCommandForTarget(string target, bool automatic)
        {
            try
            {
                using (var dc = new MFCSEntities())
                    return      (from sc in dc.SimpleCommands
                                 where sc.Status < SimpleCommand.EnumStatus.Canceled &&
                                  ((automatic && sc.Command_ID != null) || (!automatic && sc.Command_ID == null)) &&
                                  sc.Source == target && sc.Task == SimpleCommand.EnumTask.Drop
                                 select sc).Count();
            }
            catch (Exception ex)
            {
                EventLog?.AddEvent(Event.EnumSeverity.Error, Event.EnumType.Exception, ex.Message);
                throw new DBServiceException(String.Format("DBService.CountSimpleCraneCommandForTarget failed ({0})", target));
            }

        }


        public bool CheckIfSimpleCraneCommandToPlaceMaterialExist(Place p, bool automatic)
        {
            try
            {
                using (var dc = new MFCSEntities())
                    return dc.SimpleCommands.Where(prop => (prop is SimpleCraneCommand) &&  prop.Task == SimpleCraneCommand.EnumTask.Pick && prop.Status < SimpleCraneCommand.EnumStatus.Canceled && prop.Source == p.Place1 && 
                                                            prop.Material == p.Material).Count() > 0;
            }
            catch (Exception ex)
            {
                EventLog?.AddEvent(Event.EnumSeverity.Error, Event.EnumType.Exception, ex.Message);
                throw new DBServiceException(String.Format("DBService.CheckIfSimpleCraneCommandToPlaceMaterialExist ({0})", p.ToString()));
            }
        }

        public bool CheckIfSimpleConveyorCommandToPlaceMaterialExis(Place p, bool automatic)
        {
            try
            {
                using (var dc = new MFCSEntities())
                    return dc.SimpleCommands.Where(prop => (prop is SimpleConveyorCommand) &&  prop.Task == SimpleConveyorCommand.EnumTask.Move && prop.Status < SimpleConveyorCommand.EnumStatus.Canceled && prop.Source == p.Place1).Count() > 0;
            }
            catch (Exception ex)
            {
                EventLog?.AddEvent(Event.EnumSeverity.Error, Event.EnumType.Exception, ex.Message);
                throw new DBServiceException(String.Format("DBService.CheckIfSimpleConveyorCommandToPlaceMaterialExis ({0})", p.ToString()));
            }
        }


        public bool AllSimpleCommandWithCommandIDFinished(int id)
        {
            try
            {
                using (var dc = new MFCSEntities())
                {
                    return (from sc in dc.SimpleCommands
                            where sc.Command_ID == id && sc.Status != SimpleCommand.EnumStatus.Finished
                            select sc).Count() == 0;
                }
            }
            catch (Exception ex)
            {
                EventLog?.AddEvent(Event.EnumSeverity.Error, Event.EnumType.Exception, ex.Message);
                throw new DBServiceException(String.Format("DBService.AllSimpleCommandWithCommandIDFinished failed ({0})", id));
            }
        }


        public MaterialID FindMaterialID(int material, bool create)
        {
            try
            {
                using (var dc = new MFCSEntities())
                {
                    MaterialID matID = dc.MaterialIDs.Find((int)material);
                    // only for test - unkwnon palets are automatically created
                    if (matID == null && create)
                    {
                        matID = new MaterialID { ID = (int)material, Size = 1, Weight = 1 };
                        dc.MaterialIDs.Add(matID);
                        dc.SaveChanges();
                    }
                    return matID;
                }
            }
            catch (Exception ex)
            {
                EventLog?.AddEvent(Event.EnumSeverity.Error, Event.EnumType.Exception, ex.Message);
                throw new DBServiceException(String.Format("DBService.FindMaterialID failed ({0},{1})", material, create));
            }
        }

        public void AddAlarm(string unit, string alarmid, Alarm.EnumAlarmStatus stat, Alarm.EnumAlarmSeverity sev)
        {
            try
            {
                using (var dc = new MFCSEntities())
                {
                    Alarm alarm = new Alarm
                    {
                        Unit = unit,
                        Status = stat,
                        Severity = sev,
                        AlarmID = alarmid,
                        ArrivedTime = DateTime.Now,
                        AckTime = null,
                        RemovedTime = null
                    };
                    dc.Alarms.Add(alarm);
                    dc.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                EventLog?.AddEvent(Event.EnumSeverity.Error, Event.EnumType.Exception, ex.Message);
                throw new DBServiceException(String.Format("DBService.AddAlarm failed ({0},{1},{2},{3})", unit, alarmid, stat, sev ));
            }

        }

        public void UpdateAlarm(string unit, string alarmid, Alarm.EnumAlarmStatus stat)
        {
            try
            {
                using (var dc = new MFCSEntities())
                {
                    // check if a record exists
                    Alarm alarm = dc.Alarms.OrderByDescending(p => p.ID).
                                  FirstOrDefault(a => a.Unit == unit && a.AlarmID == alarmid && a.Status != Alarm.EnumAlarmStatus.Removed);
                    if (alarm != null)
                    {
                        dc.Alarms.Attach(alarm);
                        if (stat == Alarm.EnumAlarmStatus.Ack && alarm.Status == Alarm.EnumAlarmStatus.Active)
                        {
                            alarm.Status = stat;
                            alarm.AckTime = DateTime.Now;
                            dc.SaveChanges();
                        }
                        else if (stat == Alarm.EnumAlarmStatus.Removed)
                        {
                            alarm.Status = stat;
                            alarm.RemovedTime = DateTime.Now;
                            dc.SaveChanges();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                EventLog?.AddEvent(Event.EnumSeverity.Error, Event.EnumType.Exception, ex.Message);
                throw new DBServiceException(String.Format("DBService.UpdateAlarm failed ({0},{1},{2})", unit, alarmid, stat));
            }
        }

        public bool WMSIDExists(int wmsid)
        {
            try
            {
                using (var dc = new MFCSEntities())
                {
                    return dc.Commands.Any(p => p.WMS_ID == wmsid);
                }
            }
            catch (Exception ex)
            {
                EventLog?.AddEvent(Event.EnumSeverity.Error, Event.EnumType.Exception, ex.Message);
                throw new DBServiceException(String.Format("DBService.WMSIDExists failed ({0})", wmsid));
            }
        }

        public List<Alarm> GetAlarms(DateTime timeFrom, DateTime timeTo, string unit, string id, string stat)
        {
            try
            {
                using (var dc = new MFCSEntities())
                {
                    var l = (from a in dc.Alarms
                             where (a.ArrivedTime >= timeFrom) && (a.ArrivedTime <= timeTo) &&
                                   (unit == null || a.Unit.Contains(unit)) &&
                                   (id == null || a.AlarmID.Contains(id))
                             orderby a.ArrivedTime descending
                             select a).Take(5000);
                    return l.ToList();
                }
            }
            catch (Exception e)
            {
                throw new Exception(string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
            }
        }
        public List<Event> GetEvents(DateTime timeFrom, DateTime timeTo, string sev, string typ)
        {
            try
            {
                using (var dc = new MFCSEntities())
                {
                    var l = (from e in dc.Events
                             where (e.Time >= timeFrom) && (e.Time <= timeTo)
                             orderby e.Time descending
                             select e).Take(5000);
                    return l.ToList();
                }
            }
            catch (Exception e)
            {
                throw new Exception(string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
            }
        }
        public List<Movement> GetMovements(DateTime timeFrom, DateTime timeTo, string loc, string tu)
        {
            try
            {
                using (var dc = new MFCSEntities())
                {
                    if( tu != null)
                        tu = tu.TrimStart('0');
                    var l = (from m in dc.Movements
                             where (m.Time >= timeFrom) && (m.Time <= timeTo) &&
                                   (loc == null || m.Position.Contains(loc)) &&
                                   (tu == null || m.Material.ToString().Contains(tu))
                             orderby m.Time descending
                             select m).Take(5000);
                    return l.ToList();
                }
            }
            catch (Exception e)
            {
                throw new Exception(string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
            }
        }
        public List<PlaceID> GetPlaceIDs()
        {
            try
            {
                using (var dc = new MFCSEntities())
                {
                    var l = from p in dc.PlaceIDs
                            select p;
                    return l.ToList();
                }
            }
            catch (Exception e)
            {
                throw new Exception(string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
            }
        }

        public void UpdateLocation(PlaceID place)
        {
            try
            {
                using (var dc = new MFCSEntities())
                {
                    var l = dc.PlaceIDs.Find(place.ID);
                    if (l != null)
                    {
                        dc.PlaceIDs.Attach(l);
                        l.Size = place.Size;
                        l.Blocked = place.Blocked;
                        l.Reserved = place.Reserved;
                        dc.SaveChanges();
                    }
                }
            }
            catch (Exception e)
            {
                throw new Exception(string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
            }
        }
        public List<Place> GetPlaces(bool excludeWout)
        {
            try
            {
                using (var dc = new MFCSEntities())
                {
                    var l = from m in dc.Places
                            //where !excludeWout || (excludeWout && m.Place1 != "W:out")
                            select m;
                    return l.ToList();
                }
            }
            catch (Exception e)
            {
                throw new Exception(string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
            }
        }

        public User GetUserPassword(string user)
        {
            try
            {
                using (var dc = new MFCSEntities())
                {
                    return dc.Users.FirstOrDefault(p => p.User1.ToUpper() == user.ToUpper());
                }
            }
            catch (Exception e)
            {
                throw new Exception(string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
            }
        }

        public double FileSizeStringToGB(string sizeStr)
        {
            string[] size = sizeStr.Split(' ');
            double value = Double.Parse(size[0], CultureInfo.InvariantCulture);
            int power;

            switch (size[1].ToUpper())
            {
                case "KB": power = 1; break;
                case "MB": power = 2; break;
                case "GB": power = 3; break;
                case "TB": power = 4; break;
                case "PB": power = 5; break;
                case "EB": power = 6; break;
                case "ZB": power = 7; break;
                case "YB": power = 8; break;
                default: power = 0; break;
            }
            return value * Math.Pow(1024, power - 3);
        }
        public double GetDBSizeInGB()
        {
            try
            {
                var dc = new MFCSEntities();
                var sqlConn = dc.Database.Connection as SqlConnection;
                var cmd = new SqlCommand("sp_spaceused")
                {
                    CommandType = System.Data.CommandType.StoredProcedure,
                    Connection = sqlConn as SqlConnection
                };
                var adp = new SqlDataAdapter(cmd);
                var dataset = new DataSet();
                sqlConn.Open();
                adp.Fill(dataset);
                sqlConn.Close();

                // Output:
                // T[0]: [0][0] database_name,  [0][1] database_size,  [0][2] unallocated space
                // T[1]: [0][0] reserved,  [0][1] data_size,  [0][2] index_size,  [0][3] unused

                return FileSizeStringToGB((string)dataset.Tables[1].Rows[0][1]) +
                       FileSizeStringToGB((string)dataset.Tables[1].Rows[0][2]);
            }
            catch (Exception e)
            {
                throw new Exception(string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
            }
        }

        public async Task DBCleaning(double removePortion)
        {
            int [] records;
            int recordsToDelete;
            int chunk;
            int chunkSize = 100000;
            string[] tables = { "MOVEMENT", "ALARM", "EVENT", "COMMAND" };
            try
            {
                using (var dc = new MFCSEntities())
                {
                    records = new int[]
                    {
                            dc.Movements.Count(),
                            dc.Alarms.Count(),
                            dc.Events.Count(),
                            dc.Commands.Count()
                    };
                    for (int i=0; i<tables.Count(); i++)
                    {
                        recordsToDelete = (int)Math.Max(0, records[i]*removePortion);
                        while (recordsToDelete > 0)
                        {
                            chunk = Math.Min(recordsToDelete, chunkSize);
                            await dc.Database.ExecuteSqlCommandAsync(string.Format("DELETE TOP({1}) FROM {0}", tables[i], chunk));
                            recordsToDelete -= chunk;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                throw new Exception(string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
            }
        }

        public void ClearRamp(string ramp)
        {
            try
            {
                using (var dc = new MFCSEntities())
                {
                    var pl = dc.Places.Where(p => p.Place1.StartsWith(ramp)).ToList();
                    foreach (var p in pl)
                    {
                        MaterialDelete(p.Place1, p.Material);
                        //                        MaterialMove(p.Material, p.Place1, "W:out");
                    }
                }
            }
            catch (Exception e)
            {
                throw new Exception(string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
            }
        }

        public List<User> GetUsers()
        {
            try
            {
                using (var dc = new MFCSEntities())
                {
                    return dc.Users.ToList();
                }
            }
            catch (Exception e)
            {
                throw new Exception(string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
            }
        }
        public User GetUser(string name)
        {
            try
            {
                using (var dc = new MFCSEntities())
                {
                    var u = dc.Users.FirstOrDefault(p => p.User1.ToUpper() == name.ToUpper());
                    return u;
                }
            }
            catch (Exception e)
            {
                throw new Exception(string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
            }
        }
        public void DeleteUser(string name)
        {
            try
            {
                using (var dc = new MFCSEntities())
                {
                    var u = dc.Users.FirstOrDefault(p => p.User1.ToUpper() == name.ToUpper());
                    dc.Users.Remove(u);
                    dc.SaveChanges();
                }
            }
            catch (Exception e)
            {
                throw new Exception(string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
            }
        }

        public void UpdateUser(string name, int accessLevel)
        {
            try
            {
                using (var dc = new MFCSEntities())
                {
                    var u = dc.Users.FirstOrDefault(p => p.User1.ToUpper() == name.ToUpper());
                    u.AccessLevel = accessLevel;
                    dc.SaveChanges();
                }
            }
            catch (Exception e)
            {
                throw new Exception(string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
            }
        }
    }
}
