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

        private DateTime mJoinDate = DateTime.Now;
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
                mOnceAllowDays = mYearAllowDays;
                // 按月释放
                if (IsKaiMan()) mOnceAllowDays = Math.Floor((double)(mYearAllowDays * now.Month) / 12.0);
                if (lastSickDays >= 60) mOnceAllowDays = 0;
            }
            else if (mSocialWorkYear == 10)
            {
                if (now.Year - mJoinDate.Year == 10)
                {
                    mYearAllowDays = 5 + Math.Floor(5.0 * (12 - mJoinDate.Month) / 12.0);
                }
                else if (now.Year - mJoinDate.Year == 11)
                {
                    mYearAllowDays = 10;
                }
                mOnceAllowDays = mYearAllowDays;
                // 按月释放
                if (IsKaiMan()) mOnceAllowDays = Math.Floor((double)(mYearAllowDays * now.Month) / 12.0);
                if (lastSickDays >= 90) mOnceAllowDays = 0;
            }
            else if (10 < mSocialWorkYear && mSocialWorkYear < 20)
            {
                mYearAllowDays = 10;
                mOnceAllowDays = mYearAllowDays;
                // 按月释放
                if (IsKaiMan()) mOnceAllowDays = Math.Floor((double)(mYearAllowDays * now.Month) / 12.0);
                if (lastSickDays >= 90) mOnceAllowDays = 0;
            }
            else if (mSocialWorkYear == 20)
            {
                if (now.Year - mJoinDate.Year == 20)
                {
                    mYearAllowDays = 10 + Math.Floor(5.0 * (12 - mJoinDate.Month) / 12.0);
                }
                else if (now.Year - mJoinDate.Year == 21)
                {
                    mYearAllowDays = 15;
                }
                mOnceAllowDays = mYearAllowDays;
                // 按月释放
                if (IsKaiMan()) mOnceAllowDays = Math.Floor((double)(mYearAllowDays * now.Month) / 12.0);
                if (lastSickDays >= 120) mOnceAllowDays = 0;
            }
            else if (mSocialWorkYear > 20)
            {
                mYearAllowDays = 15;
                mOnceAllowDays = mYearAllowDays;
                // 按月释放
                if (IsKaiMan()) mOnceAllowDays = Math.Floor((double)(mYearAllowDays * now.Month) / 12.0);
                if (lastSickDays >= 120) mOnceAllowDays = 0;
            }

            if (mCompanyWorkYear == 0 && mCompanyWorkMonth < 12) // 入职不满一年
            {
                // 向下取整（年假天数/12×入职月份）
                mYearAllowDays = Math.Floor((double)(mCompanyWorkMonth * mYearAllowDays) / 12.0);
                mOnceAllowDays = mYearAllowDays;
                // 按月释放
                if (IsKaiMan()) mOnceAllowDays = Math.Floor((double)(mYearAllowDays * now.Month) / 12.0);
            }

        }

        private void InitWorkAge()
        {
            string sql = string.Format(@"/*dialect*/ SELECT [dbo].[fn_GetWorkYear](FJoinDate,GETDATE()) FSoYear,
[dbo].[fn_GetWorkYear](F_HR_BOBDATE,GETDATE()) FCpYear, FJoinDate, 
DATEDIFF(MONTH,F_HR_BOBDATE,GETDATE()) FCpMonth 
FROM T_HR_EMPINFO WHERE FID='{0}'", mLeaver);
            var obj = DBUtils.ExecuteDynamicObject(mContext, sql);
            if (obj.Count > 0)
            {
                mSocialWorkYear = int.Parse(obj[0]["FSoYear"].ToString());
                mCompanyWorkYear = int.Parse(obj[0]["FCpYear"].ToString());
                mCompanyWorkMonth = int.Parse(obj[0]["FCpMonth"].ToString());
                string FJoinDate = obj[0]["FJoinDate"].ToString();
                if (!FJoinDate.Equals(""))
                {
                    mJoinDate = DateTime.Parse(FJoinDate);
                }
            }
        }

        public override string GetLeftLeaveMessage()
        {
            string msg = "";
            Home home = new Home(mContext, mLeaver, 0);
            double homeLeaveDays = home.getAlreadyLeaveDays();
            double carryDays = getLastYearCarryOverDays();
            double leaveDays = getAlreadyLeaveDays();
            string leaveName = getLeaveName();
            double leftDays = 0;
            
            if (IsKaiMan())
            {
                leftDays = mOnceAllowDays + carryDays - leaveDays - homeLeaveDays;
                msg = string.Format("{0}, 目前剩余可请{1}天(含去年结转{2}天)，已请{3}天，探亲假已请{4}天，本年总可请{5}天，目前释放{6}天。\n",
                leaveName, leftDays, carryDays, leaveDays, homeLeaveDays, mYearAllowDays, mOnceAllowDays);
            }
            else
            {
                leftDays = mOnceAllowDays - leaveDays - homeLeaveDays;
                msg = string.Format("{0}, 目前剩余可请{1}天，已请{2}天，探亲假已请{3}天，本年总可请{4}天。\n",
                leaveName, leftDays, leaveDays, homeLeaveDays, mYearAllowDays);
            }
            return msg;
        }

        public override bool ValidateLeave(ref string msg)
        {
            Home home = new Home(mContext, mLeaver, 0);
            double homeLeaveDays = home.getAlreadyLeaveDays();
            double carryDays = home.getLastYearCarryOverDays();
            double leaveDays = getAlreadyLeaveDays();
            double leftDays = IsKaiMan() ? mYearAllowDays + carryDays - leaveDays - homeLeaveDays :
                mYearAllowDays - leaveDays - homeLeaveDays;
            if (leftDays < mLeaveDays)
            {
                msg += string.Format("{0}的{1}提交失败, 原因：超出了目前可请假的天数{2}天。\n", getLeaver(), getLeaveName(), leftDays);
                return false;
            }
            return true;
        }

    }
}
