using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseWMS
{
    public partial class Orders
    {
        override public string ToString()
        {             
            string erpid = ERP_ID.HasValue ? ERP_ID.Value.ToString() : "-";
            return $"|{erpid}|{OrderID}|{SubOrderID}|{SubOrderERPID}|{SubOrderName}|{SKU_ID}|{SKU_Batch}|{Destination}|{ReleaseTime}|{Status}|";
        }
    }

    public partial class Commands
    {
        override public string ToString()
        {
            string order = Order_ID.HasValue ? Order_ID.Value.ToString() : "-";
            return $"|{ID}|{Order_ID}|{TU_ID}|{Source}|{Target}|{Status}|";
        }
    }


    public partial class CommandERPs 
    {
        override public string ToString()
        {
            return $"|{ID}|{Reference}|{Status}||";
        }
    }

    public partial class Places
    {
        override public string ToString()
        {
            return $"|{PlaceID}|{TU_ID}|";
        }
    }

    public partial class TU_ID
    {
        override public string ToString()
        {
            return $"|{ID}|{DimensionClass}|{Blocked}|";
        }
    }
    public partial class SKU_ID
    {
        override public string ToString()
        {
            return $"|{ID}|{Description}|{DefaultQty}|{Unit}|{Weight}|{FrequencyClass}|";
        }
    }
}
