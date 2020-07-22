using Kingdee.BOS;
using Kingdee.BOS.App.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CZ.CEEG.OABos.LeaveApply.LeaveType.Imp
{
    /// <summary>
    /// 年休假
    /// </summary>
    public class Annual : BaseType
    {
        private int mSocialWorkYear = 0;
        private int mCompanyWorkYear = 0;
        private int mCompanyWorkMonth = 0;
        public Annual(Context context,  long leaver, double days) : base(context, LeaveTypeName.AnnualLeave, leaver, days)
        {
            InitWorkAge();
            SetAllowDays();
        }
        private void SetAllowDays()
        {
            // 获取去年病假天数
            Sick sick = new Sick(mContext, mLeaver, 0);
            double lastSickDays = sick.getLastYearLeaveDays();

            DateTime now = DateTime.Now;
            // 设置本年允许的天数
            if (0 <= mSocialWorkYear && mSocialWorkYear < 10)
            {
                mYearAllowDays = 5;
                // 按月释放
                mOnceAllowDays = Math.Floor(((double)mYearAllowDays * now.Month) / 12);
                if (lastSickDays >= 60) mOnceAllowDays = 0;
            }
            else if (10 <= mSocialWorkYear && mSocialWorkYear < 20)
            {
                mYearAllowDays = 10;
                // 按月释放
                mOnceAllowDays = Math.Floor(((double)mYearAllowDays * now.Month) / 12);
                if (lastSickDays >= 90) mOnceAllowDays = 0;
            }
            else if (mSocialWorkYear >= 20)
            {
                mYearAllowDays = 15;
                // 按月释放
                mOnceAllowDays = Math.Floor(((double)mYearAllowDays * now.Month) / 12);
                if (lastSickDays >= 120) mOnceAllowDays = 0;
            }

            if (mCompanyWorkYear == 0 && mCompanyWorkMonth < 12) // 入职不满一年
            {
                // 向下取整（年假天数/12×入职月份）
                mYearAllowDays = Math.Floor(((double)mCompanyWorkMonth * mYearAllowDays) / 12);
                // 按月释放
                mOnceAllowDays = Math.Floor(((double)mYearAllowDays * now.Month) / 12);
            }

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

        public override string GetLeftLeaveMessage()
        {
            Home home = new Home(mContext, mLeaver, 0);
            double annualLeaveDays = home.getAlreadyLeaveDays();
            double carryDays = getLastYearCarryOverDays();
            double leaveDays = getAlreadyLeaveDays();
            string leaveName = getLeaveName();
            double leftDays = mOnceAllowDays + carryDays - leaveDays - annualLeaveDays;
            return string.Format("{0}, 目前剩余可请{1}天(含去年结转{2}天)，已请{3}天，探亲假已清{4}天，本年总可请{5}天，目前释放{6}天。\n",
                leaveName, leftDays, carryDays, leaveDays, annualLeaveDays, mYearAllowDays, mOnceAllowDays);
        }

        public override bool ValidateLeave(ref string msg)
        {
            Annual annual = new Annual(mContext, mLeaver, 0);
            double annualLeaveDays = annual.getAlreadyLeaveDays();
            double carryDays = annual.getLastYearCarryOverDays();
            double leaveDays = getAlreadyLeaveDays();
            double leftDays = mYearAllowDays + carryDays - leaveDays - annualLeaveDays;
            if (leftDays < mLeaveDays)
            {
                msg += string.Format("{0}的{1}提交失败, 原因：超出了目前可请假的天数{2}天。\n", getLeaver(), getLeaveName(), mOnceAllowDays);
                return false;
            }
            return true;
        }

    }
}
