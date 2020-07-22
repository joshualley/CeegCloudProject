using Kingdee.BOS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CZ.CEEG.OABos.LeaveApply.LeaveType.Imp
{
    public class JoinArmy : BaseType
    {
        public JoinArmy(Context context, long leaver, double days) : base(context, LeaveTypeName.JoinArmy, leaver, days)
        {
        }

        public override string GetLeftLeaveMessage()
        {
            return string.Format("{0}, 不限天数。", getLeaveName());
        }

        public override bool ValidateLeave(ref string msg)
        {
            return true;
        }
    }
}
