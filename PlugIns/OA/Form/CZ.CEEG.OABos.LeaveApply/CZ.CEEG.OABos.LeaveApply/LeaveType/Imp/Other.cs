using Kingdee.BOS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CZ.CEEG.OABos.LeaveApply.LeaveType.Imp
{
    /// <summary>
    /// 其他
    /// </summary>
    public class Other : BaseType
    {
        public Other(Context context, long leaver, double days) : base(context, LeaveTypeName.OtherLeave, leaver, days)
        {
        }

        public override string GetLeftLeaveMessage()
        {
            return "";
        }

        public override bool ValidateLeave(ref string msg)
        {
            return true;
        }
    }
}
