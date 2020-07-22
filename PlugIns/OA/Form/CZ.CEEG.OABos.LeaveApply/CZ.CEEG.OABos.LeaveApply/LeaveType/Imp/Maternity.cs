using Kingdee.BOS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CZ.CEEG.OABos.LeaveApply.LeaveType.Imp
{
    /// <summary>
    /// 产假
    /// </summary>
    public class Maternity : BaseType
    {
        public Maternity(Context context, LeaveTypeName leaveType, long leaver, double days) : base(context, leaveType, leaver, days)
        {
            switch (leaveType)
            {
                case LeaveTypeName.EutocousLeave:
                    mYearAllowDays = 128;
                    break;
                case LeaveTypeName.CesareanLeave:
                    mYearAllowDays = 143;
                    break;
                case LeaveTypeName.AbortionLeave_90:
                    mYearAllowDays = 25;
                    break;
                case LeaveTypeName.AbortionLeave_210:
                    mYearAllowDays = 42;
                    break;
                case LeaveTypeName.AbortionLeave_210_UP:
                    mYearAllowDays = 90;
                    break;
                default:
                    throw new Exception("不能传入非产假的枚举类型！");
            }
        }

        public override string GetLeftLeaveMessage()
        {
            double leaveDays = getAlreadyLeaveDays();
            double leftDays = mYearAllowDays - leaveDays;
            return string.Format("{0}，剩余可请{1}天，本年可请{2}天，已请{3}天。", getLeaveName(), leftDays, mYearAllowDays, leaveDays);
        }

        public override bool ValidateLeave(ref string msg)
        {
            return base.ValidateLeave(ref msg);
        }
    }
}
