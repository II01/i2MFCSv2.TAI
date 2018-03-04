﻿using System;
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
    }
}
