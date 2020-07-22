using CZ.CEEG.OABos.LeaveApply.LeaveType.Imp;
using Kingdee.BOS;
using Kingdee.BOS.App.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CZ.CEEG.OABos.LeaveApply.LeaveType
{
    //产检假
    public class AntenatalCare : BaseType
    {
        public AntenatalCare(Context context, long leaver, double days) 
            : base(context, LeaveTypeName.AntenatalCareLeave, leaver, days)
        {
            mYearAllowDays = 10;
        }

        public override string GetLeftLeaveMessage()
        {
            double leaveDays = getAlreadyLeaveDays();
            double leftDays = mYearAllowDays - leaveDays;
            string leaveName = getLeaveName();
            return string.Format("{0}, 剩余可请{1}天，本年已请{2}天。\n", leaveName, leftDays, leaveDays);
        }

        public override bool ValidateLeave(ref string msg)
        {
            return base.ValidateLeave(ref msg);
        }
    }
}
