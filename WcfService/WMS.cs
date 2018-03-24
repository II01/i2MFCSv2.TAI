using Database;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;
using Warehouse.WCF;
using Warehouse.WMS;
using WcfService.DTO;

namespace WcfService
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.PerSession, ConcurrencyMode = ConcurrencyMode.Reentrant)]
    public class WMS : IWMS
    {
        public void MFCS_Submit(IEnumerable<DTOCommand> cmds)
        {
            try
            {
                ServiceHostBase sh = OperationContext.Current.Host;
                if (!(sh is WarehouseServiceHost))
                    throw new WCFServiceException("Host is wrong type.");

                var warehouse = (sh as WarehouseServiceHost).Warehouse;

                try
                {
                    using (MFCSEntities dc = new MFCSEntities())
                    {
                        foreach (var c in cmds)
                        {
                            if (c.Status != (int)Command.EnumCommandStatus.Canceled)
                            {
                                MaterialID matID = dc.MaterialIDs.Find((int)c.TU_ID);
                                if (matID == null)
                                    dc.MaterialIDs.Add(new MaterialID { ID = c.TU_ID, Size = 1, Weight = 1 });
                                dc.Commands.Add(new CommandMaterial
                                {
                                    WMS_ID = c.Order_ID,
                                    Source = c.Source,
                                    Target = c.Target,
                                    Info = "WMS",
                                    Material = c.TU_ID,
                                    Priority = 0,
                                    Status = (Command.EnumCommandStatus)c.Status,
                                    Task = Command.EnumCommandTask.Move,
                                    Time = DateTime.Now,
                                    Reason = Command.EnumCommandReason.OK
                                });
                                warehouse.AddEvent(Event.EnumSeverity.Event, Event.EnumType.WMS, $"MFCS_AddMoveCommands called {c.ToString()}");
                            }
                            else
                            {
                                Command cc = dc.Commands.FirstOrDefault(p => p.WMS_ID == c.Order_ID);
                                if(cc != null)
                                {
                                    dc.Commands.Add(new CommandCommand
                                    {
                                        Task = Command.EnumCommandTask.CancelCommand,
                                        CommandID = cc.WMS_ID,
                                        Priority = 0,
                                        Status = Command.EnumCommandStatus.NotActive,
                                        Time = DateTime.Now
                                    });
                                }
                            }
                        }
                        dc.SaveChanges();
                    }
                }
                catch (Exception e)
                {
                    warehouse.AddEvent(Event.EnumSeverity.Error, Event.EnumType.Exception,
                                        string.Format("{0}.{1}: {2}, MFCS_Submit exception.",
                                        this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
                    throw new FaultException(e.Message);
                }
            }
            catch (Exception ee)
            {
                throw new FaultException(ee.Message);
            }
        }
        public void MFCS_PlaceBlock(IEnumerable<string> locs, int blocktype)
        {
            try
            {
                ServiceHostBase sh = OperationContext.Current.Host;
                if (!(sh is WarehouseServiceHost))
                    throw new WCFServiceException("Host is wrong type.");

                var warehouse = (sh as WarehouseServiceHost).Warehouse;
                try
                {
                    using (MFCSEntities dc = new MFCSEntities())
                    {
                        foreach (var l in locs)
                        {
                            dc.PlaceIDs.Where(p => p.ID.StartsWith(l)).ToList().ForEach(pl => pl.Blocked = true);
                            warehouse.AddEvent(Event.EnumSeverity.Event, Event.EnumType.WMS, $"MFCS_PlaceBlock called ({l})");
                        }
                        dc.SaveChanges();
                    }
                }
                catch (Exception e)
                {
                    warehouse.AddEvent(Event.EnumSeverity.Error, Event.EnumType.Exception,
                                        string.Format("{0}.{1}: {2}, MFCS_PlaceBlock exception.",
                                        this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
                    throw new FaultException(e.Message);
                }
            }
            catch (Exception ee)
            {
                throw new FaultException(ee.Message);
            }
        }

        public void MFCS_PlaceUnblock(IEnumerable<string> locs, int blocktype)
        {
            try
            {
                ServiceHostBase sh = OperationContext.Current.Host;
                if (!(sh is WarehouseServiceHost))
                    throw new WCFServiceException("Host is wrong type.");

                var warehouse = (sh as WarehouseServiceHost).Warehouse;
                try
                {
                    using (MFCSEntities dc = new MFCSEntities())
                    {
                        foreach (var l in locs)
                        {
                            dc.PlaceIDs.Where(p => p.ID.StartsWith(l)).ToList().ForEach(pl => pl.Blocked = false);
                            warehouse.AddEvent(Event.EnumSeverity.Event, Event.EnumType.WMS, $"MFCS_PlaceBlock called ({l})");
                        }
                        dc.SaveChanges();
                    }
                }
                catch (Exception e)
                {
                    warehouse.AddEvent(Event.EnumSeverity.Error, Event.EnumType.Exception,
                                        string.Format("{0}.{1}: {2}, MFCS_Submit exception.",
                                        this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
                    throw new FaultException(e.Message);
                }
            }
            catch (Exception ee)
            {
                throw new FaultException(ee.Message);
            }
        }
    }



}
