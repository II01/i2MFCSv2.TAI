using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UserInterfaceGravityPanel.Model
{
    public enum EnumWMSOrderStatus : int
    {
        Waiting = 0,
        Active = 1,
        OnTarget = 2,
        ReadyToTake = 3,
        Cancel = 4,
        Finished = 5
    }
    public enum EnumWMSCommandStatus : int
    {
        Waiting = 0,
        Active = 1,
        Canceled = 2,
        Finished = 3,
    }

}
