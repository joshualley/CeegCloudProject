using Kingdee.BOS;
using Kingdee.BOS.App.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CZ.CEEG.OABos.LeaveApply.LeaveType.Imp
{
    public class SalemanHome : BaseType
    {

        private int mAllowLeaveTime = 6;

        public SalemanHome(Context context, long leaver, double days)
            : base(context, LeaveTypeName.SalemanHomeLeave, leaver, days)
        {
        }

        /// <summary>
        /// 是否为销售员
        /// </summary>
        /// <returns></returns>
        private bool IsSaleman()
        {
            string sql = "SELECT s.FID FROM V_BD_SALESMAN s INNER JOIN T_HR_EMPINFO e ON e.FNUMBER=s.FNUMBER WHERE e.FID='" + mLeaver + "'"; ;
            var obj = DBUtils.ExecuteDynamicObject(mContext, sql);

            return obj.Count > 0 ? true : false;
        }

        /// <summary>
        /// 获取请假次数
        /// </summary>
        /// <returns></returns>
        private int getLeaveTime()
        {
            int year = DateTime.Now.Year;
            string sql = string.Format("SELECT COUNT(*) times FROM ora_t_Leave le " +
                "INNER JOIN ora_t_LeaveHead lh ON le.FID=lh.FID AND FIsOrigin=0 AND FDocumentStatus='C' " +
                "WHERE FName='{0}' AND FLeaveType='{1}' AND YEAR(FStartDate)='{2}' AND FName='{3}' ",
                mLeaver, (int)mLeaveType, year, mLeaver);
            var obj = DBUtils.ExecuteDynamicObject(mContext, sql);
            return int.Parse(obj[0]["times"].ToString());
        }

        public override string GetLeftLeaveMessage()
        {
            if (!IsSaleman())
            {
                return getLeaver() + "没有销售员身份，请选择“探亲假”！";
            }
            int leaveTimes = getLeaveTime();
            int leftTimes = mAllowLeaveTime - leaveTimes;
            return string.Format("{0}, 剩余请假{1}次，本年允许请假{2}次，已请{3}次。", getLeaveName(), leftTimes, mAllowLeaveTime, leaveTimes);
        }

        public override bool ValidateLeave(ref string msg)
        {
            if (!IsSaleman())
            {
                msg += string.Format("{0}的{1}提交失败, 原因：该员工无销售员身份，请选择“探亲假”！\n", getLeaver(), getLeaveName());
                return false;
            }
            if (getLeaveTime() + 1 > mAllowLeaveTime)
            {
                msg += string.Format("{0}的{1}提交失败, 原因：超出了本年可请假的次数{2}次。\n", getLeaver(), getLeaveName(), mAllowLeaveTime);
                return false;
            }
            return true;
        }
    }
}
