using Kingdee.BOS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CZ.CEEG.OABos.LeaveApply.LeaveType.Imp
{
    /// <summary>
    /// 哺乳假
    /// </summary>
    public class Breastfeeding : BaseType
    {
        public Breastfeeding(Context context, long leaver, double days) : base(context, LeaveTypeName.BreastfeedingLeave, leaver, days)
        {
            mOnceAllowDays = 1 / 8.0;
        }

        public override string GetLeftLeaveMessage()
        {
            return string.Format("{0}, 每次可请1小时。\n", getLeaveName());
        }

        public override bool ValidateLeave(ref string msg)
        {
            if(mLeaveDays > mOnceAllowDays)
            {
                msg += string.Format("{0}的{1}提交失败, 原因：超出了本次可请假的时间1小时。\n", getLeaver(), getLeaveName());
                return false;
            }
            return true;
        }
    }
}
