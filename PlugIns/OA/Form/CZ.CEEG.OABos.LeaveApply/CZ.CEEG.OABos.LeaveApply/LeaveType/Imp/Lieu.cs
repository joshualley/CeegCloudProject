using Kingdee.BOS;
using Kingdee.BOS.App.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CZ.CEEG.OABos.LeaveApply.LeaveType.Imp
{
    /// <summary>
    /// 调休假
    /// </summary>
    class Lieu : BaseType
    {
        public Lieu(Context context, long leaver, double days) : base(context, LeaveTypeName.LieuLeave, leaver, days)
        {
        }

        public override string GetLeftLeaveMessage()
        {
            string sql = String.Format(@"exec proc_czly_GetHolidayShiftSituation @EmpID='{0}'", mLeaver);
            var obj = DBUtils.ExecuteDynamicObject(mContext, sql);
            string overHours = obj[0]["FOverHours"].ToString();
            string restHours = obj[0]["FRestHours"].ToString();
            string leftHours = obj[0]["FLeftHours"].ToString();
            string day = (float.Parse(leftHours) / 8.0).ToString();
            return string.Format("共加班{0}小时，已调休{1}小时，剩余{2}小时，折合{3}天。", overHours, restHours, leftHours, day);
        }

        public override bool ValidateLeave(ref string msg)
        {
            string sql = String.Format(@"exec proc_czly_GetHolidayShiftSituation @EmpID='{0}'", mLeaver);
            var obj = DBUtils.ExecuteDynamicObject(mContext, sql);
            string leftHours = obj[0]["FLeftHours"].ToString();
            double leftDays = double.Parse(leftHours) / 8;
            if (leftDays < mLeaveDays)
            {
                msg += string.Format("{0}的调休提交失败, 原因：超出了可调休的天数{1}天。\n", getLeaver(), leftDays);
                return false;
            }
            return true;
        }
    }
}
