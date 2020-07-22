using Kingdee.BOS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CZ.CEEG.OABos.LeaveApply.LeaveType.Imp
{
    /// <summary>
    /// 丧假
    /// </summary>
    public class Funeral : BaseType
    {
        public Funeral(Context context, long leaver, double days) : base(context, LeaveTypeName.FuneralLeave, leaver, days)
        {
            mOnceAllowDays = 5;
        }

        public override string GetLeftLeaveMessage()
        {
            double leaveDays = getAlreadyLeaveDays();
            string leaveName = getLeaveName();
            return string.Format("{0}, 每次最多可请{1}天, 已请{2}天。\n",
                leaveName, mOnceAllowDays, leaveDays);
        }

        public override bool ValidateLeave(ref string msg)
        {
            if(mLeaveDays > mOnceAllowDays)
            {
                msg += string.Format("{0}的{1}提交失败, 原因：超出了本次可请假的天数{2}天。\n", getLeaver(), getLeaveName(), mOnceAllowDays);
                return false;
            }
            return true;
        }
    }
}
