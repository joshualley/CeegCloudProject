using Kingdee.BOS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CZ.CEEG.OABos.LeaveApply.LeaveType.Imp
{
    /// <summary>
    /// 工伤
    /// </summary>
    public class OccupationalInjury : BaseType
    {
        public OccupationalInjury(Context context, long leaver, double days) : base(context, LeaveTypeName.OccupationalInjury, leaver, days)
        {

        }

        public override string GetLeftLeaveMessage()
        {
            return string.Format("{0}, 不限请假天数。", getLeaveName());
        }

        public override bool ValidateLeave(ref string msg)
        {
            return true;
        }
    }
}
