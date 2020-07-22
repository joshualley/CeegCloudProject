using Kingdee.BOS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CZ.CEEG.OABos.LeaveApply.LeaveType.Imp
{
    // 事假
    public class PersonalAffairs : BaseType
    {
        
        public PersonalAffairs(Context context, long leaver, double days) : base(context, LeaveTypeName.PersonalAffairsLeave, leaver, days)
        {
            mYearAllowDays = 15;
            mOnceAllowDays = 5;
        }

        public override string GetLeftLeaveMessage()
        {
            double leaveDays = getAlreadyLeaveDays();
            double leftDays = mYearAllowDays - leaveDays;
            string leaveName = getLeaveName();
            return string.Format("{0}, 剩余可请{1}天，已请{2}天，本年可请{3}天，本次可请{4}天。\n", 
                leaveName, leftDays, leaveDays, mYearAllowDays, mOnceAllowDays);
        }

        public override bool ValidateLeave(ref string msg)
        {
            if(mLeaveDays > mOnceAllowDays)
            {
                msg += string.Format("{0}的{1}提交失败, 原因：超出了本次可请假的天数{2}天。\n", getLeaver(), getLeaveName(), mOnceAllowDays);
                return false;
            }
            // 验证本年请假是否超时
            return base.ValidateLeave(ref msg);
        }
    }
}
