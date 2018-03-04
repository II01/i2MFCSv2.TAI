using System;
using System.Collections.Generic;
using DatabaseWMS;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UserInterface.DataServiceWMS
{
    interface IDBServiceWMS
    {
        List<SKU_ID> GetSKUIDs();
        SKU_ID FindSKUID(string skuid);
    }
}
