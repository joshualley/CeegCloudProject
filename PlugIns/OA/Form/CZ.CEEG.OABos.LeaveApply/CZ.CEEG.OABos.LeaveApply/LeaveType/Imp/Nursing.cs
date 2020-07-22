using Kingdee.BOS;
using Kingdee.BOS.App.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CZ.CEEG.OABos.LeaveApply.LeaveType.Imp
{
    /// <summary>
    ///  陪护假
    /// </summary>
    public class Nursing : BaseType
    {

        private Gender mGender = Gender.Male;

        public Nursing(Context context,  long leaver, double days) : base(context, LeaveTypeName.NursingLeave, leaver, days)
        {
            mYearAllowDays = 15;
            InitHRInfo();
        }

        private void InitHRInfo()
        {
            string sql = "SELECT ISNULL(F_HR_SEX,-1) FGender," +
                "ISNULL(DATEDIFF(MONTH,F_HR_BORNDATE,GETDATE())/12,0) FAge " +
                "FROM T_HR_EMPINFO WHERE FID='" + mLeaver + "'";
            var obj = DBUtils.ExecuteDynamicObject(mContext, sql);
            if (obj.Count > 0)
            {
                mGender = (Gender)int.Parse(obj[0]["FGender"].ToString());
            }
        }

        public override string GetLeftLeaveMessage()
        {
            double leaveDays = getAlreadyLeaveDays();
            double leftDays = mYearAllowDays - leaveDays;
            return string.Format("{0}, 剩余可请{1}天，本年可请{2}天，已请{3}天。", getLeaveName(), leftDays, mYearAllowDays, leaveDays);
        }

        public override bool ValidateLeave(ref string msg)
        {
            if (mGender != Gender.Male)
            {
                msg += string.Format("{0}, 仅男员工可请！", getLeaveName());
                return false;
            }
            return base.ValidateLeave(ref msg);
        }
    }
}
