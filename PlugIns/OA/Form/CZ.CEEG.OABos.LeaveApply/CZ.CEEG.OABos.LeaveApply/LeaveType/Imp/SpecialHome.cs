using Kingdee.BOS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CZ.CEEG.OABos.LeaveApply.LeaveType.Imp
{
    /// <summary>
    /// 特殊探亲假
    /// </summary>
    public class SpecialHome : BaseType
    {
        public SpecialHome(Context context, long leaver, double days) : base(context, LeaveTypeName.SpecialHomeLeave, leaver, days)
        {
        }

        public override string GetLeftLeaveMessage()
        {
            return string.Format("{0}，由人力审核，不作限制。", getLeaveName());
        }

        public override bool ValidateLeave(ref string msg)
        {
            return true;
        }
    }
}
