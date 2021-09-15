using Kingdee.BOS;
using Kingdee.BOS.App.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CZ.CEEG.OABos.LeaveApply.LeaveType.Imp
{
    /// <summary>
    /// 婚假
    /// </summary>
    public class Marriage : BaseType
    {
        private Gender mGender = Gender.Male;
        private int mAge = 0;

        public Marriage(Context context, long leaver, double days) : base(context, LeaveTypeName.MarriageLeave, leaver, days)
        {
            InitHRInfo();
            SetAllowDays();
        }

        private void InitHRInfo()
        {
            string sql = "SELECT ISNULL(F_HR_SEX,-1) FGender," +
                "ISNULL(DATEDIFF(MONTH,F_HR_BORNDATE,GETDATE())/12,0) FAge " +
                "FROM T_HR_EMPINFO WHERE FID='" + mLeaver + "'";
            var obj = DBUtils.ExecuteDynamicObject(mContext, sql);
            if(obj.Count > 0)
            {
                mGender = (Gender)int.Parse(obj[0]["FGender"].ToString());
                mAge = (int)double.Parse(obj[0]["FAge"].ToString());
            }
        }

        /// <summary>
        /// 是否申请过婚假
        /// </summary>
        /// <returns></returns>
        private bool HasApplied()
        {
            string sql = string.Format(@"SELECT lh.FID FROM ora_t_Leave le
                inner join ora_t_LeaveHead lh on lh.FID=le.FID
                WHERE FDocumentStatus in ('B', 'C') AND
                le.FLeaveType='{0}' AND le.FNAME='{1}'", (int)mLeaveType, mLeaver);
            var obj = DBUtils.ExecuteDynamicObject(mContext, sql);

            return obj.Count > 0 ? true : false;
        }

        private void SetAllowDays()
        {
            switch (mGender)
            {
                case Gender.Male:
                    if(22 <= mAge && mAge < 25)
                    {
                        mYearAllowDays = 3;
                    }
                    else if (25 <= mAge)
                    {
                        mYearAllowDays = 15;
                    }
                    else
                    {
                        mYearAllowDays = 0;
                    }
                    break;
                case Gender.Female:
                    if (20 <= mAge && mAge < 23)
                    {
                        mYearAllowDays = 3;
                    }
                    else if (23 <= mAge)
                    {
                        mYearAllowDays = 15;
                    }
                    else
                    {
                        mYearAllowDays = 0;
                    }
                    break;
                case Gender.Other:
                    mYearAllowDays = 0;
                    break;
            }
            if(HasApplied())
            {
                mYearAllowDays = 3;
            }
            if(getAlreadyLeaveDays() > 0) //如果本年请过婚假
            {
                mYearAllowDays = 0;
            }
        }

        public override string GetLeftLeaveMessage()
        {
            if(mAge == 0 || mGender == Gender.Other)
            {
                return "您的性别或出生日期可能没有录入系统，请联系HR进行信息补录！";
            }
            string leaveName = getLeaveName();
            return string.Format("{0}(需一次性休完), 本年可请{1}天({2}岁)。\n",
                leaveName, mYearAllowDays, mAge);
        }

        public override bool ValidateLeave(ref string msg)
        {
            return base.ValidateLeave(ref msg);
        }
    }
}
