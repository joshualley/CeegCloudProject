using Kingdee.BOS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CZ.CEEG.OABos.LeaveApply.LeaveType.Imp
{
    /// <summary>
    /// 献血，同拆迁
    /// </summary>
    class DonateBlood : BaseType
    {
        public DonateBlood(Context context, long leaver, double days) : base(context, LeaveTypeName.DonateBlood, leaver, days)
        {
            mOnceAllowDays = 2;
        }

        public override string GetLeftLeaveMessage()
        {
            return string.Format("{0}，本次可请{1}天，已请{2}天。", getLeaveName(), mOnceAllowDays, getAlreadyLeaveDays());
        }

        public override bool ValidateLeave(ref string msg)
        {
            if (mLeaveDays > mOnceAllowDays)
            {
                msg += string.Format("{0}的{1}提交失败, 原因：超出了本次可请假的天数{2}天。\n", getLeaver(), getLeaveName(), mOnceAllowDays);
                return false;
            }
            return true;
        }
    }
}
