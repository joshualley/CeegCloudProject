using Kingdee.BOS;
using Kingdee.BOS.App.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CZ.CEEG.OABos.LeaveApply.LeaveType.Imp
{
    /// <summary>
    /// 病假
    /// </summary>
    public class Sick : BaseType
    {
        private int mSocialWorkYear = 0;
        private int mCompanyWorkYear = 0;
        private int mCompanyWorkMonth = 0;
        public Sick(Context context, long leaver, double days) : base(context, LeaveTypeName.SickLeave, leaver, days)
        {
            InitWorkAge();
        }

        private void InitWorkAge()
        {
            string sql = "/*dialect*/SELECT [dbo].[fn_GetWorkYear](FJoinDate,GETDATE()) FSoYear," +
                "[dbo].[fn_GetWorkYear](F_HR_BOBDATE,GETDATE()) FCpYear," +
                "DATEDIFF(MONTH,F_HR_BOBDATE,GETDATE()) FCpMonth " +
                "FROM T_HR_EMPINFO WHERE FID='" + mLeaver + "'";
            var obj = DBUtils.ExecuteDynamicObject(mContext, sql);
            if (obj.Count > 0)
            {
                mSocialWorkYear = int.Parse(obj[0]["FSoYear"].ToString());
                mCompanyWorkYear = int.Parse(obj[0]["FCpYear"].ToString());
                mCompanyWorkMonth = int.Parse(obj[0]["FCpMonth"].ToString());
            }
        }

        private void SetAllowDays()
        {
            if(0 <= mSocialWorkYear && mSocialWorkYear < 10 && 0 <= mCompanyWorkYear && mCompanyWorkYear < 5)
            {
                mYearAllowDays = 90;
            }
            else if (0 <= mSocialWorkYear && mSocialWorkYear < 10 && 5 <= mCompanyWorkYear)
            {
                mYearAllowDays = 180;
            }
            else if (10 <= mSocialWorkYear && 0 <= mCompanyWorkYear && mCompanyWorkYear < 5)
            {
                mYearAllowDays = 180;
            }
            else if (10 <= mSocialWorkYear && 5 <= mCompanyWorkYear && mCompanyWorkYear < 10)
            {
                mYearAllowDays = 270;
            }
            else if (10 <= mSocialWorkYear && 10 <= mCompanyWorkYear && mCompanyWorkYear < 15)
            {
                mYearAllowDays = 365;
            }
            else if (10 <= mSocialWorkYear && 15 <= mCompanyWorkYear && mCompanyWorkYear < 20)
            {
                mYearAllowDays = 545;
            }
            else if (10 <= mSocialWorkYear && 20 <= mCompanyWorkYear)
            {
                mYearAllowDays = 730;
            }
        }

        public override string GetLeftLeaveMessage()
        {
            double leaveDays = getAlreadyLeaveDays();
            double leftDays = mYearAllowDays - leaveDays;
            return string.Format("{0}, 剩余可请{1}天，已请{2}天，本年可请{3}天。\n",
                getLeaveName(), leaveDays, leftDays, mYearAllowDays);
        }

        public override bool ValidateLeave(ref string msg)
        {
            return base.ValidateLeave(ref msg);
        }
    }
}
