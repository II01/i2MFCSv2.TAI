using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Database;
using DatabaseWMS;
using Warehouse.DataService;

namespace UserInterface.DataServiceWMS
{
    [Serializable]
    public class DBServiceWMSException : Exception
    {
        public DBServiceWMSException(string s) : base(s)
        {
        }
    }

    class DBServiceWMS : IDBServiceWMS
    {
        private IEventLog EventLog { get; set; }
        public DBServiceWMS(IEventLog w)
        {
            EventLog = w;
        }

        public List<SKU_ID> GetSKUIDs()
        {
            try
            {
                using (var dc = new EntitiesWMS())
                {
                    var l = from p in dc.SKU_ID
                            select p;
                    return l.ToList();
                }
            }
            catch (Exception e)
            {
                throw new Exception(string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
            }
        }

        public SKU_ID FindSKUID(string skuid)
        {
            try
            {
                using (var dc = new EntitiesWMS())
                {
                    return dc.SKU_ID.FirstOrDefault(prop => prop.ID == skuid);
                }
            }
            catch (Exception ex)
            {
                EventLog?.AddEvent(Event.EnumSeverity.Error, Event.EnumType.Exception, ex.Message);
                throw new DBServiceWMSException(String.Format("DBService.FindPlace failed ({0})", skuid));
            }
        }
        public void AddSKUID(SKU_ID SKUID)
        {
            try
            {
                using (var dc = new EntitiesWMS())
                {
                    dc.SKU_ID.Add(SKUID);
                    dc.SaveChanges();
                }
            }
            catch (Exception e)
            {
                throw new Exception(string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
            }
        }
        public void UpdateSKUID(SKU_ID SKUID)
        {
            try
            {
                using (var dc = new EntitiesWMS())
                {
                    var l = dc.SKU_ID.Find(SKUID.ID);
                    if (l != null)
                    {
                        dc.SKU_ID.Attach(l);
                        l.Description = SKUID.Description;
                        l.DefaultQty = SKUID.DefaultQty;
                        l.Unit = SKUID.Unit;
                        l.Weight = SKUID.Weight;
                        l.FrequencyClass = SKUID.FrequencyClass;
                        dc.SaveChanges();
                    }
                }
            }
            catch (Exception e)
            {
                throw new Exception(string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
            }
        }


        public List<PlaceIDs> GetPlaceIDs()
        {
            try
            {
                using (var dc = new EntitiesWMS())
                {
                    var l = from p in dc.PlaceIDs
                            where p.DimensionClass >= 0
                            select p;
                    return l.ToList();
                }
            }
            catch (Exception e)
            {
                throw new Exception(string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
            }
        }

        public PlaceIDs FindPlaceID(string placeid)
        {
            try
            {
                using (var dc = new EntitiesWMS())
                {
                    return dc.PlaceIDs.FirstOrDefault(prop => prop.ID == placeid);
                }
            }
            catch (Exception ex)
            {
                EventLog?.AddEvent(Event.EnumSeverity.Error, Event.EnumType.Exception, ex.Message);
                throw new DBServiceWMSException(String.Format("DBService.FindPlace failed ({0})", placeid));
            }
        }
        public void UpdatePlaceID(PlaceIDs placeid)
        {
            try
            {
                using (var dc = new EntitiesWMS())
                {
                    var l = dc.PlaceIDs.Find(placeid.ID);
                    if (l != null)
                    {
                        dc.PlaceIDs.Attach(l);
                        l.PositionTravel = placeid.PositionTravel;
                        l.PositionHoist= placeid.PositionHoist;
                        l.DimensionClass = placeid.DimensionClass;
                        l.FrequencyClass = placeid.FrequencyClass;
                        l.Status = placeid.Status;
                        dc.SaveChanges();
                    }
                }
            }
            catch (Exception e)
            {
                throw new Exception(string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
            }
        }

        public List<PlaceTUID> GetPlaceTUIDs()
        {
            try
            {
                using (var dc = new EntitiesWMS())
                {
                    var l = from p in dc.Places
                            join t in dc.TU_ID on p.TU_ID equals t.ID
                            orderby p.TU_ID
                            select new PlaceTUID { TUID = p.TU_ID, PlaceID = p.PlaceID, DimensionClass = t.DimensionClass, Blocked = t.Blocked };
                    return l.ToList();
                }
            }
            catch (Exception e)
            {
                throw new Exception(string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
            }
        }

        public List<TUSKUID> GetTUSKUIDs(int tuid)
        {
            try
            {
                using (var dc = new EntitiesWMS())
                {
                    var l = from t in dc.TUs
                            join s in dc.SKU_ID on t.SKU_ID equals s.ID
                            where t.TU_ID == tuid
                            orderby t.SKU_ID
                            select new TUSKUID { SKUID = t.SKU_ID, Qty = t.Qty, Batch = t.Batch, ProdDate = t.ProdDate, ExpDate = t.ExpDate, Description = s.Description };
                    return l.ToList();
                }
            }
            catch (Exception e)
            {
                throw new Exception(string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
            }
        }

        public List<TUs> GetTUs(string skuid)
        {
            try
            {
                using (var dc = new EntitiesWMS())
                {
                    var l = from t in dc.TUs
                            where t.SKU_ID == skuid
                            orderby t.SKU_ID
                            select t;
                    return l.ToList();
                }
            }
            catch (Exception e)
            {
                throw new Exception(string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
            }
        }

        public Places FindPlaceByTUID(int tuid)
        {
            try
            {
                using (var dc = new EntitiesWMS())
                {
                    var place = dc.Places.FirstOrDefault(p => p.TU_ID == tuid);
                    return place;
                }
            }
            catch (Exception e)
            {
                throw new Exception(string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
            }
        }
        public void UpdatePlace(Places place)
        {
            try
            {
                using (var dc = new EntitiesWMS())
                {
                    var item = dc.Places.SingleOrDefault(p => p.TU_ID == place.TU_ID);
                    if (item != null)
                    {
                        dc.Places.Remove(item);
                        dc.Places.Add(place);
                        dc.SaveChanges();
                    }
                }
            }
            catch (Exception e)
            {
                throw new Exception(string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
            }
        }
        public void DeletePlace(Places place)
        {
            try
            {
                using (var dc = new EntitiesWMS())
                {
                    var item = dc.Places.SingleOrDefault(p => p.TU_ID == place.TU_ID && p.PlaceID == place.PlaceID);
                    if (item != null)
                    {
                        dc.Places.Remove(item);
                        dc.SaveChanges();
                    }
                }
            }
            catch (Exception e)
            {
                throw new Exception(string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
            }
        }
        public void AddPlace(Places place)
        {
            try
            {
                using (var dc = new EntitiesWMS())
                {
                    dc.Places.Add(place);
                    dc.SaveChanges();
                }
            }
            catch (Exception e)
            {
                throw new Exception(string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
            }
        }
        public void UpdateTUID(TU_ID tuid)
        {
            try
            {
                using (var dc = new EntitiesWMS())
                {
                    var l = dc.TU_ID.Find(tuid.ID);
                    if (l != null)
                    {
                        dc.TU_ID.Attach(l);
                        l.DimensionClass = tuid.DimensionClass;
                        l.Blocked = tuid.Blocked;
                        dc.SaveChanges();
                    }
                }
            }
            catch (Exception e)
            {
                throw new Exception(string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
            }
        }

        public void UpdateTUs(TU_ID tuid, List<TUs> tus)
        {
            try
            {
                using (var dc = new EntitiesWMS())
                {
                    var ldb = dc.TUs.Where(p => p.TU_ID == tuid.ID).ToList();               // db state
                    var ldel = ldb.Where(p => !tus.Any(pp => pp.SKU_ID == p.SKU_ID));       // to delete
                    var ladd = tus.Where(p => !ldb.Any(pp => pp.SKU_ID == p.SKU_ID));       // to add
                    foreach (var tu in tus)
                    {
                        var item = dc.TUs.Find(tu.TU_ID, tu.SKU_ID);
                        if( item != null)
                        {
                            item.Qty = tu.Qty;
                            item.Batch = tu.Batch;
                            item.ProdDate = tu.ProdDate;
                            item.ExpDate = tu.ExpDate;
                        }
                    }
                    dc.TUs.AddRange(ladd);
                    dc.TUs.RemoveRange(ldel);
                    dc.SaveChanges();
                }
            }
            catch (Exception e)
            {
                throw new Exception(string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
            }
        }

        public void DeleteTUs(int tuid)
        {
            try
            {
                using (var dc = new EntitiesWMS())
                {
                    var ldel = dc.TUs.Where(p => p.TU_ID == tuid).ToList();
                    dc.TUs.RemoveRange(ldel);
                    dc.SaveChanges();
                }
            }
            catch (Exception e)
            {
                throw new Exception(string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
            }
        }

        public void AddTUs(List<TUs> tus)
        {
            try
            {
                using (var dc = new EntitiesWMS())
                {
                    dc.TUs.AddRange(tus);
                    dc.SaveChanges();
                }
            }
            catch (Exception e)
            {
                throw new Exception(string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
            }
        }

        public List<Orders> GetOrders(int statusLessOrEqual)
        {
            try
            {
                using (var dc = new EntitiesWMS())
                {
                    var l = (from c in dc.Orders
                             where c.Status <= statusLessOrEqual 
                             orderby c.ERP_ID, c.OrderID descending, c.SubOrderID ascending 
                             select c).Take(5000);

                    return l.ToList();
                }
            }
            catch (Exception e)
            {
                throw new Exception(string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
            }
        }
        public List<Orders> GetOrdersDistinct(int statusLessOrEqual)
        {
            try
            {
                using (var dc = new EntitiesWMS())
                {
                    var l = (from c in dc.Orders
                             where c.Status <= statusLessOrEqual
                             orderby c.ERP_ID, c.OrderID descending, c.SubOrderID ascending
                             group c by new {c.ERP_ID, c.OrderID} into lunique
                             select lunique.FirstOrDefault()).Take(5000);
                    return l.ToList();
                }
            }
            catch (Exception e)
            {
                throw new Exception(string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
            }
        }
        public List<Orders> GetSubOrdersDistinct(int? erpid, int orderid)
        {
            try
            {
                using (var dc = new EntitiesWMS())
                {
                    var l = (from c in dc.Orders
                             where c.ERP_ID == erpid && c.OrderID == orderid
                             orderby c.SubOrderID ascending
                             group c by c.SubOrderID into lunique
                             select lunique.FirstOrDefault()).Take(5000);
                    return l.ToList();
                }
            }
            catch (Exception e)
            {
                throw new Exception(string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
            }
        }

        public List<Orders> GetSKUs(int? erpid, int orderid, int suborderid)
        {
            try
            {
                using (var dc = new EntitiesWMS())
                {
                    var l = (from c in dc.Orders
                             where c.ERP_ID == erpid && c.OrderID == orderid && c.SubOrderID == suborderid
                             orderby c.SKU_ID ascending
                             select c).Take(5000);
                    return l.ToList();
                }
            }
            catch (Exception e)
            {
                throw new Exception(string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
            }
        }

        public CommandERPs FindCommandERP(int erpid)
        {
            try
            {
                using (var dc = new EntitiesWMS())
                {
                    return dc.CommandERPs.FirstOrDefault(prop => prop.ID == erpid);
                }
            }
            catch (Exception e)
            {
                throw new Exception(string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
            }
        }

        public void AddOrder(Orders order)
        {
            try
            {
                using (var dc = new EntitiesWMS())
                {
                    dc.Orders.Add(order);
                    dc.SaveChanges();
                }
            }
            catch (Exception e)
            {
                throw new Exception(string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
            }
        }

        public void UpdateOrders(int orderid, Orders order)
        {
            try
            {
                using (var dc = new EntitiesWMS())
                {
                    var l = from or in dc.Orders
                            where or.OrderID == orderid
                            select or;
                    foreach(var o in l)
                    {
                        o.OrderID = order.OrderID;
                        o.Destination = order.Destination;
                        o.ReleaseTime = order.ReleaseTime;
                        o.Status = order.Status;
                    }
                    dc.SaveChanges();
                }
            }
            catch (Exception e)
            {
                throw new Exception(string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
            }
        }
        public void DeleteOrders(int orderid)
        {
            try
            {
                using (var dc = new EntitiesWMS())
                {
                    var items = from or in dc.Orders
                                where or.OrderID == orderid
                                select or;
                    dc.Orders.RemoveRange(items);
                    dc.SaveChanges();
                }
            }
            catch (Exception e)
            {
                throw new Exception(string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
            }
        }

        public bool ExistsOrderID(int? erpid, int orderid)
        {
            try
            {
                using (var dc = new EntitiesWMS())
                {
                    return dc.Orders.FirstOrDefault(p => p.ERP_ID == erpid && p.OrderID == orderid) != null;
                }
            }
            catch (Exception e)
            {
                throw new Exception(string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
            }
        }
        public void AddSubOrder(Orders order)
        {
            try
            {
                using (var dc = new EntitiesWMS())
                {
                    dc.Orders.Add(order);
                    dc.SaveChanges();
                }
            }
            catch (Exception e)
            {
                throw new Exception(string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
            }
        }

        public void UpdateSubOrders(int orderid, int suborderid, Orders order)
        {
            try
            {
                using (var dc = new EntitiesWMS())
                {
                    var l = from or in dc.Orders
                            where or.OrderID == orderid && or.SubOrderID == suborderid
                            select or;
                    foreach (var o in l)
                    {
                        o.SubOrderID = order.SubOrderID;
                        o.SubOrderName = order.SubOrderName;
                    }
                    dc.SaveChanges();
                }
            }
            catch (Exception e)
            {
                throw new Exception(string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
            }
        }
        public void DeleteSubOrders(int orderid, int suborderid)
        {
            try
            {
                using (var dc = new EntitiesWMS())
                {
                    var items = from or in dc.Orders
                                where or.OrderID == orderid && or.SubOrderID == suborderid
                                select or;
                    dc.Orders.RemoveRange(items);
                    dc.SaveChanges();
                }
            }
            catch (Exception e)
            {
                throw new Exception(string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
            }
        }
        public bool ExistsSubOrderID(int orderid, int suborderid)
        {
            try
            {
                using (var dc = new EntitiesWMS())
                {
                    return dc.Orders.FirstOrDefault(p => p.OrderID == orderid && p.SubOrderID == suborderid) != null;
                }
            }
            catch (Exception e)
            {
                throw new Exception(string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
            }
        }
        public void AddSKU(Orders order)
        {
            try
            {
                using (var dc = new EntitiesWMS())
                {
                    dc.Orders.Add(order);
                    dc.SaveChanges();
                }
            }
            catch (Exception e)
            {
                throw new Exception(string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
            }
        }

        public void UpdateSKU(Orders order)
        {
            try
            {
                using (var dc = new EntitiesWMS())
                {
                    var o = dc.Orders.Find(order.ID);

                    if(o != null)
                    {
                        o.SKU_ID = order.SKU_ID;
                        o.SKU_Batch = order.SKU_Batch;
                        o.SKU_Qty = order.SKU_Qty;
                    }
                    dc.SaveChanges();
                }
            }
            catch (Exception e)
            {
                throw new Exception(string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
            }
        }
        public void DeleteSKU(Orders order)
        {
            try
            {
                using (var dc = new EntitiesWMS())
                {
                    var item = dc.Orders.Find(order.ID);
                    dc.Orders.Remove(item);
                    dc.SaveChanges();
                }
            }
            catch (Exception e)
            {
                throw new Exception(string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
            }
        }
        public List<CommandERPs> GetCommandERPs(int statusLessOrEqual)
        {
            try
            {
                using (var dc = new EntitiesWMS())
                {
                    var items = (from c in dc.CommandERPs
                                 where c.Status <= statusLessOrEqual
                                 orderby c.ID descending
                                 select c).Take(5000);
                    return items.ToList();
                }
            }
            catch (Exception e)
            {
                throw new Exception(string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
            }
        }
        public void UpdateCommandERP(CommandERPs commanderp)
        {
            try
            {
                using (var dc = new EntitiesWMS())
                {
                    var item = dc.CommandERPs.FirstOrDefault(p => p.ID == commanderp.ID);
                    if(item != null)
                    {
                        item.Command = commanderp.Command;
                        item.Status = commanderp.Status;
                    }
                    dc.SaveChanges();
                }
            }
            catch (Exception e)
            {
                throw new Exception(string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
            }
        }
        public List<Commands> GetCommands(int statusLessOrEqual)
        {
            try
            {
                using (var dc = new EntitiesWMS())
                {
                    var items = (from c in dc.Commands
                                 where c.Status <= statusLessOrEqual
                                 orderby c.ID descending
                                 select c).Take(5000);
                    return items.ToList();
                }
            }
            catch (Exception e)
            {
                throw new Exception(string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
            }
        }
        public void UpdateCommand(Commands commandwms)
        {
            try
            {
                using (var dc = new EntitiesWMS())
                {
                    var item = dc.Commands.FirstOrDefault(p => p.ID == commandwms.ID);
                    if (item != null)
                    {
                        item.Order_ID = commandwms.Order_ID;
                        item.TU_ID = commandwms.TU_ID;
                        item.Source = commandwms.Source;
                        item.Target = commandwms.Target;
                        item.Status = commandwms.Status;
                    }
                    dc.SaveChanges();
                }
            }
            catch (Exception e)
            {
                throw new Exception(string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
            }
        }
        public List<CommandWMSOrder> GetCommandOrders(int statusLessOrEqual)
        {
            try
            {
                using (var dc = new EntitiesWMS())
                {
                    var items = (from c in dc.Commands
                                 where c.Status <= statusLessOrEqual
                                 orderby c.ID descending
                                 select new CommandWMSOrder
                                 {
                                     ID = c.ID,
                                     Order_ID = c.Order_ID.HasValue ? c.Order_ID.Value: 0,
                                     TU_ID = c.TU_ID,
                                     Source = c.Source,
                                     Target = c.Target,
                                     Status = c.Status,
                                     Time = c.Time,
                                     OrderERPID = c.Order_ID.HasValue ? c.Orders.ERP_ID : 0,
                                     OrderOrderID = c.Order_ID.HasValue ? c.Orders.OrderID : 0,
                                     OrderSubOrderID = c.Order_ID.HasValue ? c.Orders.SubOrderID : 0,
                                     OrderSubOrderName = c.Order_ID.HasValue ? c.Orders.SubOrderName : "",
                                     OrderSKUID = c.Order_ID.HasValue ? c.Orders.SKU_ID : "",
                                     OrderSKUBatch = c.Order_ID.HasValue ? c.Orders.SKU_Batch : ""
                                 }).Take(5000);
                    return items.ToList();
                }
            }
            catch (Exception e)
            {
                throw new Exception(string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
            }
        }

        public List<PlaceDiff> PlaceWMSandMFCSDiff()
        {
            try
            {
                using (var dcw = new EntitiesWMS())
                using (var dcm = new MFCSEntities())
                {
                    var itemsw = (from pw in dcw.Places
                                  select new PlaceDiff{ TUID = pw.TU_ID, PlaceWMS = pw.PlaceID, TimeWMS = pw.Time}).ToList();
                    var itemsm = (from pm in dcm.Places
                                  select new PlaceDiff{ TUID = pm.Material, PlaceMFCS = pm.Place1, TimeMFCS = pm.Time}).ToList();
                    var itemsu = itemsw.Union(itemsm);
                    var items = itemsu.Select(p => p.TUID).Distinct();

                    var listd = (from i in items
                                 join iw in itemsw on i equals iw.TUID into joinw
                                 from jw in joinw.DefaultIfEmpty()
                                 join im in itemsm on i equals im.TUID into joinm
                                 from jm in joinm.DefaultIfEmpty()
                                 where jw?.PlaceWMS != jm?.PlaceMFCS
                                 select new PlaceDiff
                                 {
                                     TUID = i,
                                     PlaceWMS = jw == null ? null:jw.PlaceWMS,
                                     PlaceMFCS = jm == null ? null:jm.PlaceMFCS,
                                     TimeWMS = jw == null ? null : jw.TimeWMS,
                                     TimeMFCS = jm == null ? null : jm.TimeMFCS,
                                 }).ToList();
                    return listd;
                }
            }
            catch (Exception e)
            {
                throw new Exception(string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
            }
        }
        public void UpdatePlacesMFCS(List<PlaceDiff> list)
        {
            try
            {
                using (var dcm = new MFCSEntities())
                {
                    foreach (var l in list)
                    {
                        if (dcm.MaterialIDs.FirstOrDefault(p => p.ID == l.TUID) == null)
                            dcm.MaterialIDs.Add(new MaterialID { ID = l.TUID, Size = 1, Weight = 1 });
                        var place = dcm.Places.FirstOrDefault(pp => pp.Material == l.TUID);
                        if (place != null)
                            dcm.Places.Remove(place);
                        if(l.PlaceWMS.StartsWith("W"))
                            dcm.Places.Add(new Place { Material = l.TUID, Place1 = l.PlaceWMS, Time = DateTime.Now });
                    }
                    dcm.SaveChanges();
                }
            }
            catch (Exception e)
            {
                throw new Exception(string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
            }
        }
        public void UpdatePlacesWMS(List<PlaceDiff> list)
        {
            try
            {
                using (var dcw = new EntitiesWMS())
                {
                    foreach (var l in list)
                    {
                        if (dcw.TU_ID.FirstOrDefault(p => p.ID == l.TUID) == null)
                            dcw.TU_ID.Add(new TU_ID { ID = l.TUID, DimensionClass = 0, Blocked = 0});
                        var place = dcw.Places.FirstOrDefault(pp => pp.TU_ID == l.TUID);
                        if (place != null)
                            dcw.Places.Remove(place);
                        dcw.Places.Add(new Places { TU_ID = l.TUID, PlaceID = l.PlaceMFCS, Time = DateTime.Now  });
                    }
                    dcw.SaveChanges();
                }
            }
            catch (Exception e)
            {
                throw new Exception(string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
            }
        }
        public List<Logs> GetLogs()
        {
            try
            {
                using (var dcw = new EntitiesWMS())
                {
                    var items = (from l in dcw.Logs
                                 orderby l.ID descending
                                 select l).Take(5000);
                    return items.ToList();
                }
            }
            catch (Exception e)
            {
                throw new Exception(string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
            }
        }
    }
}


