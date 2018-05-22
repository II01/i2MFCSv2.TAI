using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Database;
using DatabaseWMS;
using UserInterface.Services;
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

        public async Task<List<SKU_ID>> GetSKUIDs()
        {
            try
            {
                using (var dc = new EntitiesWMS())
                {
                    var l = from p in dc.SKU_ID
                            select p;
                    return await l.ToListAsync();
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
            catch (Exception e)
            {
                throw new Exception(string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
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


        public async Task<List<PlaceIDs>> GetPlaceIDs(int dimensionClassMin, int dimensionClassMax)
        {
            try
            {
                using (var dc = new EntitiesWMS())
                {
                    var l = from p in dc.PlaceIDs
                            where p.DimensionClass >= dimensionClassMin && p.DimensionClass <= dimensionClassMax
                            select p;
                    return await l.ToListAsync();
                }
            }
            catch (Exception e)
            {
                throw new Exception(string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
            }
        }

        public int CountPlaceIDs(string IDStartsWith)
        {
            try
            {
                using (var dc = new EntitiesWMS())
                {
                    if (IDStartsWith == null)
                        IDStartsWith = "";
                    var l = from p in dc.PlaceIDs
                            where p.ID.StartsWith(IDStartsWith)
                            select p;
                    return l.Count();
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
            catch (Exception e)
            {
                throw new Exception(string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
            }
        }

        public async Task<List<Places>> GetPlaces(string beginsWithPlaceID)
        {
            try
            {
                string bstr = "";
                if (beginsWithPlaceID != null)
                    bstr = beginsWithPlaceID;
                using (var dc = new EntitiesWMS())
                {
                    return await dc.Places.Where(p => p.PlaceID.StartsWith(bstr)).ToListAsync();
                }
            }
            catch (Exception e)
            {
                throw new Exception(string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
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
                        l.PositionHoist = placeid.PositionHoist;
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

        public async Task<List<PlaceTUID>> GetPlaceTUIDs(bool excludeWout)
        {
            try
            {
                using (var dc = new EntitiesWMS())
                {
                    var l = from p in dc.Places
                            where !excludeWout || (excludeWout && p.PlaceID != "W:out")
                            join t in dc.TU_ID on p.TU_ID equals t.ID
                            orderby p.TU_ID
                            select new PlaceTUID
                            {
                                TUID = p.TU_ID,
                                PlaceID = p.PlaceID,
                                DimensionClass = t.DimensionClass,
                                Blocked = t.Blocked,
                                TimeStamp = p.Time
                            };
                    return await l.ToListAsync();
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
                            select new TUSKUID
                            { SKUID = t.SKU_ID, Qty = t.Qty, Batch = t.Batch, ProdDate = t.ProdDate, ExpDate = t.ExpDate, Description = s.Description };
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
                            join p in dc.Places on t.TU_ID equals p.TU_ID
                            where p.PlaceID != "W:out"
                            orderby new { t.Batch, t.SKU_ID }
                            select t;
                    return l.ToList();
                }
            }
            catch (Exception e)
            {
                throw new Exception(string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
            }
        }

        public async Task<List<TUPlaceID>> GetAvailableTUs(string skuid)
        {
            try
            {
                using (var dc = new EntitiesWMS())
                {
                    var l = from t in dc.TUs
                            where t.SKU_ID == skuid
                            join p in dc.Places on t.TU_ID equals p.TU_ID
                            where p.PlaceID != "W:out" && !p.PlaceID.StartsWith("W:32") && !p.PlaceID.StartsWith("T")
                            join pid in dc.PlaceIDs on p.PlaceID equals pid.ID
                            join tid in dc.TU_ID on t.TU_ID equals tid.ID
                            orderby new { t.Batch, t.SKU_ID }
                            select new TUPlaceID
                            {
                                TUID = t.TU_ID,
                                SKUID = t.SKU_ID,
                                Batch = t.Batch,
                                Qty = t.Qty,
                                ProdDate = t.ProdDate,
                                ExpDate = t.ExpDate,
                                Location = pid.ID,
                                Status = (EnumBlockedWMS)(pid.Status & tid.Blocked)
                            };
                    return await l.ToListAsync();
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
        public Places FindPlaceByPlace(string placeName)
        {
            try
            {
                using (var dc = new EntitiesWMS())
                {
                    var place = dc.Places.FirstOrDefault(p => p.PlaceID == placeName);
                    return place;
                }
            }
            catch (Exception e)
            {
                throw new Exception(string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
            }
        }
        public void UpdatePlaceMFCS(Place place)
        {
            try
            {
                using (var dc = new MFCSEntities())
                {
                    var item = dc.Places.SingleOrDefault(p => p.Material == place.Material);
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
        public void DeletePlaceMFCS(Place place)
        {
            try
            {
                using (var dc = new MFCSEntities())
                {
                    var item = dc.Places.SingleOrDefault(p => p.Material == place.Material && p.PlaceID == place.PlaceID);
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
        public void AddPlaceMFCS(Place place)
        {
            try
            {
                using (var dc = new MFCSEntities())
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
                        if (item != null)
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

        public int GetLastUsedOrderID()
        {
            try
            {
                using (var dc = new EntitiesWMS())
                {
                    var l = (from c in dc.Orders
                             where c.ERP_ID == null
                             orderby c.OrderID descending
                             select c).FirstOrDefault();

                    return l == null? 0: l.OrderID;
                }
            }
            catch (Exception e)
            {
                throw new Exception(string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
            }
        }

        public async Task<List<OrderReduction>> GetOrdersDistinct(DateTime timeFrom, DateTime timeTo, int statusLessOrEqual)
        {
            try
            {
                using (var dc = new EntitiesWMS())
                {
                    var l = from c in dc.Orders
                            where c.Status <= statusLessOrEqual || (c.ReleaseTime >= timeFrom && c.ReleaseTime <= timeTo)
                            orderby c.ERP_ID, c.OrderID descending, c.SubOrderID ascending
                            join ce in dc.CommandERPs on c.ERP_ID equals ce.ID into jj
                            from ce in jj.DefaultIfEmpty()
                            group c by new {ERPID = c.ERP_ID, OrderID = c.OrderID} into grpO
                            select new OrderReduction
                            {
                                ERPID = grpO.Key.ERPID,
                                ERPIDStokbar = grpO.FirstOrDefault().CommandERPs.ERP_ID,
                                OrderID = grpO.Key.OrderID,
                                Destination = grpO.FirstOrDefault().Destination,
                                ReleaseTime = grpO.FirstOrDefault().ReleaseTime,
                                Status = grpO.Any(p => p.Status > (int)EnumWMSOrderStatus.Waiting) ?
                                                       grpO.Min(p => p.Status > (int)EnumWMSOrderStatus.Waiting ? p.Status : (int)EnumWMSOrderStatus.Active) : (int)EnumWMSOrderStatus.Waiting
                            };
                    return await l.ToListAsync();
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

        public void UpdateOrders(int? erpid, int orderid, Orders order)
        {
            try
            {
                using (var dc = new EntitiesWMS())
                {
                    var l = from or in dc.Orders
                            where or.ERP_ID == erpid && or.OrderID == orderid
                            select or;
                    foreach(var o in l)
                    {
                        o.OrderID = order.OrderID;
                        o.Destination = order.Destination;
                        o.ReleaseTime = order.ReleaseTime;
                        o.Status = order.Status;
                    }
                    if(erpid != null && order.Status == (int)EnumWMSOrderStatus.Cancel)
                    {
                        var ec = dc.CommandERPs.Find(order.ERP_ID);
                        if(ec != null)
                            ec.Status = (int)EnumCommandERPStatus.Canceled;
                    }
                    dc.SaveChanges();
                }
            }
            catch (Exception e)
            {
                throw new Exception(string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
            }
        }

        public void UpdateSubOrderStatus(int id, EnumWMSOrderStatus status)
        {
            try
            {
                using (var dc = new EntitiesWMS())
                {
                    var l = from or in dc.Orders
                            where or.ID == id
                            select or;
                    foreach (var o in l)
                    {
                        o.Status = (int)status;
                    }
                    dc.SaveChanges();
                }
            }
            catch (Exception e)
            {
                throw new Exception(string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
            }
        }
        public bool ClearRamp(string destinationtStartsWith)
        {
            try
            {
                using (var dc = new EntitiesWMS())
                {

                    bool canClear = !dc.Orders.Any(p => p.Destination.StartsWith(destinationtStartsWith) && p.Status == (int)EnumWMSOrderStatus.Active);

                    if ( canClear )
                    {
                        var list = from p in dc.Places
                                   where p.PlaceID.StartsWith(destinationtStartsWith)
                                   select p;
                        foreach (var l in list)
                        {
                            dc.Places.Remove(l);
                            //dc.Places.Add(new Places
                            //{
                            //    TU_ID = l.TU_ID,
                            //    PlaceID = "W:out",
                            //    Time = DateTime.Now
                            //});
                        }
                        dc.SaveChanges();
                    }

                    return canClear;
                }
            }
            catch (Exception e)
            {
                throw new Exception(string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
            }
        }
        public void ReleaseRamp(string destinationtStartsWith)
        {
            try
            {
                using (var dc = new EntitiesWMS())
                {

                    bool canRelease = !dc.Places.Any(p => p.PlaceID.StartsWith(destinationtStartsWith));

                    if ( canRelease )
                    {
                        var l = from o in dc.Orders
                                where o.Destination.StartsWith(destinationtStartsWith) &&
                                      o.Status == (int)EnumWMSOrderStatus.OnTargetPart || o.Status == (int)EnumWMSOrderStatus.OnTargetAll
                                select o;
                        foreach (var o in l)
                            o.Status = (o.Status == (int)EnumWMSOrderStatus.OnTargetAll) ? (int)EnumWMSOrderStatus.Finished : (int)EnumWMSOrderStatus.Cancel;
                        var param = dc.Parameters.Find($"Counter[{destinationtStartsWith}]");
                        if (param != null)
                            param.Value = Convert.ToString(0);
                        dc.SaveChanges();
                    }
                }
            }
            catch (Exception e)
            {
                throw new Exception(string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
            }
        }
        public void DeleteOrders(int? erpid, int orderid)
        {
            try
            {
                using (var dc = new EntitiesWMS())
                {
                    var items = from or in dc.Orders
                                where or.ERP_ID == erpid && or.OrderID == orderid
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

        public void UpdateSubOrders(int? erpid, int orderid, int suborderid, Orders order)
        {
            try
            {
                using (var dc = new EntitiesWMS())
                {
                    var l = from or in dc.Orders
                            where or.ERP_ID == erpid && or.OrderID == orderid && or.SubOrderID == suborderid
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
        public void DeleteSubOrders(int? erpid, int orderid, int suborderid)
        {
            try
            {
                using (var dc = new EntitiesWMS())
                {
                    var items = from or in dc.Orders
                                where or.ERP_ID == erpid && or.OrderID == orderid && or.SubOrderID == suborderid
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
                    return dc.Orders.FirstOrDefault(p => p.ERP_ID == null && p.OrderID == orderid && p.SubOrderID == suborderid) != null;
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
        public async Task<List<CommandERPs>> GetCommandERPs(DateTime timeFrom, DateTime timeTo,  int statusLessOrEqual)
        {
            try
            {
                using (var dc = new EntitiesWMS())
                {
                    var items = (from c in dc.CommandERPs
                                 where c.Status <= statusLessOrEqual || (c.Time >= timeFrom && c.Time <= timeTo)
                                 orderby c.ID descending
                                 select c).Take(5000);
                    return await items.ToListAsync();
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

        public async Task<List<OrderReduction>> GetOrdersWithCount(DateTime timeFrom, DateTime timeTo, int statusLessOrEqual)
        {
            try
            {
                using (var dc = new EntitiesWMS())
                {
                    var suborders = from o in dc.Orders
                                    where o.Status <= statusLessOrEqual || (o.ReleaseTime >= timeFrom && o.ReleaseTime <= timeTo)
                                    join c in dc.Commands on o.ID equals c.Order_ID into ordersComands
                                    from oc in ordersComands.DefaultIfEmpty()
                                    select new { orders = o, lastChange = oc == null? o.ReleaseTime : oc.LastChange };
                    var single = from oc in suborders
                                 group oc by new { oc.orders.ID} into grpSO
                                 select new{ suborder = grpSO.FirstOrDefault(), lastchange = grpSO.Max(p => p.lastChange == null? grpSO.FirstOrDefault().orders.ReleaseTime : p.lastChange)};
                    var orders = from so in (await single.ToListAsync())
                                 join ce in dc.CommandERPs on so.suborder.orders.ERP_ID equals ce.ID into cenull
                                 from ce in cenull.DefaultIfEmpty()
                                 orderby so.suborder.orders.ERP_ID descending, so.suborder.orders.OrderID descending, so.suborder.orders.SubOrderID ascending
                                 group so by new { ERPID = so.suborder.orders.ERP_ID, ERPIDSB = ce?.ERP_ID, OrderID = so.suborder.orders.OrderID } into grpO
                                 select new OrderReduction
                                 {
                                     ERPID = grpO.Key.ERPID,
                                     ERPIDStokbar = grpO.Key.ERPIDSB,
                                     OrderID = grpO.Key.OrderID,
                                     Destination = grpO.FirstOrDefault().suborder.orders.Destination,
                                     ReleaseTime = grpO.FirstOrDefault().suborder.orders.ReleaseTime,
                                     LastChange = grpO.FirstOrDefault().lastchange,
                                     CountAll = grpO.Count(),
                                     CountActive = grpO.Count(p => p.suborder.orders.Status == (int)EnumWMSOrderStatus.Active),
                                     CountMoveDone = grpO.Count(p => p.suborder.orders.Status >= (int)EnumWMSOrderStatus.OnTargetAll && p.suborder.orders.Status <= (int)EnumWMSOrderStatus.OnTargetPart),
                                     CountFinished = grpO.Count(p => p.suborder.orders.Status > (int)EnumWMSOrderStatus.Cancel),
                                     Status = grpO.Any(p => p.suborder.orders.Status > (int)EnumWMSOrderStatus.Waiting) ? 
                                                       grpO.Min(p => p.suborder.orders.Status > (int)EnumWMSOrderStatus.Waiting ? p.suborder.orders.Status : (int)EnumWMSOrderStatus.Active) : (int)EnumWMSOrderStatus.Waiting
                                 };
                    return orders.ToList();
                }
            }
            catch (Exception e)
            {
                throw new Exception(string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
            }
        }

        public async Task<List<OrderReduction>> GetSubOrdersWithCount(int? erpid, int orderid)
        {
            try
            {
                using (var dc = new EntitiesWMS())
                {
                    var suborders = from o in dc.Orders
                                    where o.ERP_ID == erpid && o.OrderID == orderid
                                    join c in dc.Commands on o.ID equals c.Order_ID into subordercmds
                                    from oc in subordercmds.DefaultIfEmpty()
                                    select new { SubOrder = o, Command = oc };                    
                    var subs = from so in suborders
                               group so by so.SubOrder.SubOrderID into grp
                               select new OrderReduction
                               {
                                    ERPID = grp.FirstOrDefault().SubOrder.ERP_ID,
                                    ERPIDStokbar = null,
                                    OrderID = grp.FirstOrDefault().SubOrder.OrderID,
                                    SubOrderID = grp.FirstOrDefault().SubOrder.SubOrderID,
                                    SubOrderERPID = grp.FirstOrDefault().SubOrder.SubOrderERPID,
                                    SubOrderName = grp.FirstOrDefault().SubOrder.SubOrderName,
                                    CountAll = grp.Where(p => p.Command != null).Count(),
                                    CountActive = grp.Where(p => p.Command != null).Count(pp => pp.Command.Status == (int)EnumCommandWMSStatus.Active),
                                    CountMoveDone = grp.Where(p => p.Command != null).Count(pp => pp.Command.Status > (int)EnumCommandWMSStatus.Active),
                                    CountFinished = grp.Where(p => p.Command != null).Count(pp => pp.Command.Status > (int)EnumCommandWMSStatus.Active),
                                    Status = grp.Any(p => p.Command.Status > (int)EnumWMSOrderStatus.Waiting) ? grp.Where(p => p.Command.Status > (int)EnumWMSOrderStatus.Waiting).Min(p => p.Command.Status) : 0
                               };
                    return await subs.ToListAsync();
                }
            }
            catch (Exception e)
            {
                throw new Exception(string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
            }
        }

        public async Task<List<OrderReduction>> GetSubOrdersBySKUWithCount(int? erpid, int orderid)
        {
            try
            {
                using (var dc = new EntitiesWMS())
                {
                    var suborders = from o in dc.Orders
                                    where o.ERP_ID == erpid && o.OrderID == orderid
                                    join c in dc.Commands on o.ID equals c.Order_ID into subordercmds
                                    from oc in subordercmds.DefaultIfEmpty()
                                    select new { SubOrder = o, Command = oc };
                    var subs = from so in suborders
                               group so by so.SubOrder.ID into grp
                               select new OrderReduction
                               {
                                   ERPID = grp.FirstOrDefault().SubOrder.ERP_ID,
                                   ERPIDStokbar = null,
                                   OrderID = grp.FirstOrDefault().SubOrder.OrderID,
                                   SubOrderID = grp.FirstOrDefault().SubOrder.SubOrderID,
                                   SubOrderERPID = grp.FirstOrDefault().SubOrder.SubOrderERPID,
                                   SubOrderName = grp.FirstOrDefault().SubOrder.SubOrderName,
                                   WMSID = grp.FirstOrDefault().SubOrder.ID,
                                   SKUID = grp.FirstOrDefault().SubOrder.SKU_ID,
                                   SKUBatch = grp.FirstOrDefault().SubOrder.SKU_Batch,
                                   SKUQty = grp.FirstOrDefault().SubOrder.SKU_Qty,
                                   CountAll = grp.Where(p => p.Command != null).Count(),
                                   CountActive = grp.Where(p => p.Command != null).Count(pp => pp.Command.Status == (int)EnumCommandWMSStatus.Active),
                                   CountMoveDone = grp.Where(p => p.Command != null).Count(pp => pp.Command.Status > (int)EnumCommandWMSStatus.Active),
                                   CountFinished = grp.Where(p => p.Command != null).Count(pp => pp.Command.Status > (int)EnumCommandWMSStatus.Active),
                                   Status = grp.FirstOrDefault().SubOrder.Status
                                   // grp.Where(p => p.Command.Status > (int)EnumWMSOrderStatus.Waiting).DefaultIfEmpty().Min(p => p == null ? 0 : (p.Command.Status < 2 ? p.Command.Status : p.Command.Status + 2)))
                                   // +2: komande imajo drugačno shemo statusov kot orderji
                               };
                    return await subs.ToListAsync();
                }
            }
            catch (Exception e)
            {
                throw new Exception(string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
            }
        }


        public async Task<List<CommandWMSOrder>> GetCommandsWMSForSubOrder(int? erpid, int orderid, int suborderid, int statusLessOrEqual)
        {
            try
            {
                using (var dc = new EntitiesWMS())
                {
                    var items = (from o in dc.Orders
                                 where o.ERP_ID == erpid && o.OrderID == orderid && o.SubOrderID == suborderid && o.Status <= statusLessOrEqual
                                 join c in dc.Commands on o.ID equals c.Order_ID
                                 select new CommandWMSOrder
                                 {
                                     ID = c.ID,
                                     Order_ID = c.Order_ID ?? 0,
                                     TU_ID = c.TU_ID,
                                     Source = c.Source,
                                     Target = c.Target,
                                     Status = c.Status,
                                     Time = c.Time,
                                     OrderERPID = c.Order_ID.HasValue ? c.Orders.ERP_ID : 0,
                                     OrderOrderID = c.Order_ID.HasValue ? c.Orders.OrderID : 0,
                                     OrderSubOrderID = c.Order_ID.HasValue ? c.Orders.SubOrderID : 0,
                                     OrderSubOrderERPID = c.Order_ID.HasValue ? c.Orders.SubOrderERPID : 0,
                                     OrderSubOrderName = c.Order_ID.HasValue ? c.Orders.SubOrderName : "",
                                     OrderSKUID = c.Order_ID.HasValue ? c.Orders.SKU_ID : "",
                                     OrderSKUBatch = c.Order_ID.HasValue ? c.Orders.SKU_Batch : ""
                                 }

                                 ).Take(5000);
                    return await items.ToListAsync();
                }
            }
            catch (Exception e)
            {
                throw new Exception(string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
            }
        }
        public async Task<List<CommandWMSOrder>> GetCommandsWMSForSubOrder(int id)
        {
            try
            {
                using (var dc = new EntitiesWMS())
                {
                    var items = (from c in dc.Commands
                                 where c.Order_ID == id
                                 select new CommandWMSOrder
                                 {
                                     ID = c.ID,
                                     Order_ID = c.Order_ID ?? 0,
                                     TU_ID = c.TU_ID,
                                     Source = c.Source,
                                     Target = c.Target,
                                     Status = c.Status,
                                     Time = c.Time,
                                 }
                                 ).Take(5000);
                    return await items.ToListAsync();
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
//                        item.Order_ID = commandwms.Order_ID;
//                        item.TU_ID = commandwms.TU_ID;
//                        item.Source = commandwms.Source;
//                        item.Target = commandwms.Target;
                        item.Status = commandwms.Status;
                        item.LastChange = DateTime.Now;
                    }
                    dc.SaveChanges();
                }
            }
            catch (Exception e)
            {
                throw new Exception(string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
            }
        }
        public async Task<List<CommandWMSOrder>> GetCommandOrders(DateTime dateFrom, DateTime dateTo, int statusLessOrEqual)
        {
            try
            {
                using (var dc = new EntitiesWMS())
                {
                    var items = (from c in dc.Commands
                                 where c.Status <= statusLessOrEqual || (c.Time >= dateFrom && c.Time <= dateTo)
                                 orderby c.ID descending
                                 select new CommandWMSOrder
                                 {
                                     ID = c.ID,
                                     Order_ID = c.Order_ID ?? 0,
                                     TU_ID = c.TU_ID,
                                     Source = c.Source,
                                     Target = c.Target,
                                     Status = c.Status,
                                     Time = c.Time,
                                     OrderERPID = c.Order_ID.HasValue ? c.Orders.ERP_ID : 0,
                                     OrderOrderID = c.Order_ID.HasValue ? c.Orders.OrderID : 0,
                                     OrderSubOrderID = c.Order_ID.HasValue ? c.Orders.SubOrderID : 0,
                                     OrderSubOrderERPID = c.Order_ID.HasValue ? c.Orders.SubOrderERPID : 0,
                                     OrderSubOrderName = c.Order_ID.HasValue ? c.Orders.SubOrderName : "",
                                     OrderSKUID = c.Order_ID.HasValue ? c.Orders.SKU_ID : "",
                                     OrderSKUBatch = c.Order_ID.HasValue ? c.Orders.SKU_Batch : ""
                                 }).Take(5000);
                    return await items.ToListAsync();
                }
            }
            catch (Exception e)
            {
                throw new Exception(string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
            }
        }

        public async Task<List<PlaceDiff>> PlaceWMSandMFCSDiff()
        {
            try
            {
                using (var dcw = new EntitiesWMS())
                using (var dcm = new MFCSEntities())
                {
                    var itemsw = await (from pw in dcw.Places
                                  select new PlaceDiff{ TUID = pw.TU_ID, PlaceWMS = pw.PlaceID, TimeWMS = pw.Time}).ToListAsync();
                    var itemsm = await (from pm in dcm.Places
                                  select new PlaceDiff{ TUID = pm.Material, PlaceMFCS = pm.Place1, TimeMFCS = pm.Time}).ToListAsync();
                    var itemsu = itemsw.Union(itemsm);
                    var items = itemsu.Select(p => p.TUID).Distinct();

                    var itemsd = from i in items
                                 join iw in itemsw on i equals iw.TUID into joinw
                                 from jw in joinw.DefaultIfEmpty()
                                 join im in itemsm on i equals im.TUID into joinm
                                 from jm in joinm.DefaultIfEmpty()
                                 where (jw?.PlaceWMS != jm?.PlaceMFCS) && !(jw != null && jw.PlaceWMS == "W:out" && jm == null)
                                 select new PlaceDiff
                                 {
                                     TUID = i,
                                     PlaceWMS = jw?.PlaceWMS,
                                     PlaceMFCS = jm?.PlaceMFCS,
                                     TimeWMS = jw?.TimeWMS,
                                     TimeMFCS = jm?.TimeMFCS,
                                 };
                    return itemsd.ToList();
                }
            }
            catch (Exception e)
            {
                throw new Exception(string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
            }
        }
        public void UpdatePlacesMFCS(List<PlaceDiff> list, string user)
        {
            try
            {
                using (var dcm = new MFCSEntities())
                {
                    foreach (var l in list)
                    {
                        if (dcm.MaterialIDs.FirstOrDefault(p => p.ID == l.TUID) == null)
                        {
                            dcm.MaterialIDs.Add(new MaterialID { ID = l.TUID, Size = 1, Weight = 1 });
                            AddLog(user, EnumLogWMS.Event, "UI", $"Update place MFCS, add MaterialID: |{l.TUID:d9}|");
                        }
                        var place = dcm.Places.FirstOrDefault(pp => pp.Material == l.TUID);
                        if (place != null)
                        {
                            dcm.Places.Remove(place);
                            AddLog(user, EnumLogWMS.Event, "UI", $"Update place MFCS, delete TU: |{place.Place1}|{place.Material:d9}|");
                        }
                        if (l.PlaceWMS != null && l.PlaceWMS.StartsWith("W") && l.PlaceWMS != "W:out")
                        {
                            dcm.Places.Add(new Place { Material = l.TUID, Place1 = l.PlaceWMS, Time = DateTime.Now });
                            AddLog(user, EnumLogWMS.Event, "UI", $"Update place MFCS, add TU: |{l.PlaceWMS}|{l.TUID:d9}|");
                        }
                        dcm.SaveChanges();
                    }
                }
            }
            catch (Exception e)
            {
                throw new Exception(string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
            }
        }
        public void UpdatePlacesWMS(List<PlaceDiff> list, string user)
        {
            try
            {
                using (var dcw = new EntitiesWMS())
                {
                    foreach (var l in list)
                    {
                        if (dcw.TU_ID.FirstOrDefault(p => p.ID == l.TUID) == null)
                        {
                            var tuid = new TU_ID { ID = l.TUID, DimensionClass = 0, Blocked = 0 };
                            dcw.TU_ID.Add(tuid);
                            AddLog(user, EnumLogWMS.Event, "UI", $"Update places WMS, add TUID: {tuid.ToString()}|");
                        }
                        var place = dcw.Places.FirstOrDefault(pp => pp.TU_ID == l.TUID);
                        if (place != null)
                        {
                            dcw.Places.Remove(place);
                            AddLog(user, EnumLogWMS.Event, "UI", $"Update places WMS, remove place: {place.ToString()}");
                        }
                        if (l.PlaceMFCS != null)
                        {
                            var pl = new Places { TU_ID = l.TUID, PlaceID = l.PlaceMFCS, Time = DateTime.Now };
                            dcw.Places.Add(pl);
                            AddLog(user, EnumLogWMS.Event, "UI", $"Update places WMS, add place: {pl.ToString()}");
                        }
                    }
                    dcw.SaveChanges();
                }
            }
            catch (Exception e)
            {
                throw new Exception(string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
            }
        }
        public async Task<List<Logs>> GetLogs()
        {
            try
            {
                using (var dcw = new EntitiesWMS())
                {
                    var items = (from l in dcw.Logs
                                 orderby l.ID descending
                                 select l).Take(1000);
                    return await items.ToListAsync();
                }
            }
            catch (Exception e)
            {
                throw new Exception(string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
            }
        }
        public async Task<List<Logs>> GetLogs(DateTime dateFrom, DateTime dateTo)
        {
            try
            {
                using (var dcw = new EntitiesWMS())
                {
                    var items = (from l in dcw.Logs
                                 where l.Time >= dateFrom && l.Time <= dateTo
                                 orderby l.ID descending
                                 select l).Take(5000);
                    return await items.ToListAsync();
                }
            }
            catch (Exception e)
            {
                throw new Exception(string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
            }
        }

        public void AddLog(string user, EnumLogWMS type, string source, string message)
        {
            try
            {
                using (var dc = new EntitiesWMS())
                {
                    dc.Logs.Add(new Logs
                    {
                        Severity = (int)type,
                        Source = $"{source} # {user}",
                        Message = message.Substring(0, Math.Min(250, message.Length)),
                        Time = DateTime.Now
                    });
                    dc.SaveChanges();
                }
            }
            catch (Exception e)
            { }
        }

    }
}


