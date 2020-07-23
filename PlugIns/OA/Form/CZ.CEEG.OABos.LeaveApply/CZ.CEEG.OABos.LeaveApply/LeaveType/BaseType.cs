using Kingdee.BOS;
using Kingdee.BOS.App.Data;
using System;
using System.Reflection;
using System.ComponentModel;

namespace CZ.CEEG.OABos.LeaveApply.LeaveType.Imp
{
    public abstract class BaseType : ILeaveType
    {
        /// <summary>
        /// 请假类型
        /// </summary>
        protected LeaveTypeName mLeaveType;
        /// <summary>
        /// 该类假本年已请假天数
        /// </summary>
        private double mAlreadyLeaveDays = -1;
        /// <summary>
        /// 去年请假天数
        /// </summary>
        private double mLastYearLeaveDays = -1;
        /// <summary>
        /// 去年结转天数
        /// </summary>
        private double mLastYearCarryOverDays = -1;
        /// <summary>
        /// 请假的用户FID
        /// </summary>
        protected long mLeaver { get; set; }
        /// <summary>
        /// 用户提交的请假天数
        /// </summary>
        protected double mLeaveDays { get; set; }
        /// <summary>
        /// 本年允许请假天数
        /// </summary>
        protected double mYearAllowDays { get; set; }
        /// <summary>
        /// 一次请假允许天数
        /// </summary>
        protected double mOnceAllowDays { get; set; }
        /// <summary>
        /// 数据库连接上下文
        /// </summary>
        protected Context mContext;

        private int mIsKaiman = -1;


        public BaseType(Context context, LeaveTypeName leaveType, long leaver, double days)
        {
            mLeaveType = leaveType;
            mLeaver = leaver;
            mLeaveDays = days;
            mContext = context;
        }

        public abstract string GetLeftLeaveMessage();

        public virtual bool ValidateLeave(ref string msg)
        {
            double leftDays = mYearAllowDays - getAlreadyLeaveDays();
            if(leftDays < mLeaveDays)
            {
                
                msg += string.Format("{0}的{1}提交失败, 原因：超出了本年可请假的天数{2}天。\n", getLeaver(), getLeaveName(), mYearAllowDays);
                return false;
            }
            return true;
            
        }
        

        public void MergeLeaveDays(double days)
        {
            mLeaveDays += days;
        }
        /// <summary>
        /// 获取去年请假天数
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public double getLastYearLeaveDays()
        {
            if (mLastYearLeaveDays != -1)
            {
                return mLastYearLeaveDays;
            }
            int year = DateTime.Now.Year - 1;
            string sql = string.Format("SELECT ISNULL(SUM(FDayNum),0) days FROM ora_t_Leave le " +
                "INNER JOIN ora_t_LeaveHead lh ON le.FID=lh.FID AND FIsOrigin=0 AND FDocumentStatus='C' " +
                "WHERE FName='{0}' AND FLeaveType='{1}' AND YEAR(FStartDate)='{2}' AND FName='{3}' ",
                mLeaver, (int)mLeaveType, year, mLeaver);
            var obj = DBUtils.ExecuteDynamicObject(mContext, sql);
            if (obj.Count <= 0)
            {
                mLastYearLeaveDays = 0;
            }
            else
            {
                mLastYearLeaveDays = double.Parse(obj[0]["days"].ToString());
            }
            
            return mLastYearLeaveDays;
        }
        /// <summary>
        /// 获取上年结转天数
        /// </summary>
        /// <param name="context"></param>
        /// <param name="leaver">请假员工FID</param>
        /// <returns></returns>
        public double getLastYearCarryOverDays()
        {
            if(mLastYearCarryOverDays != -1)
            {
                return mLastYearCarryOverDays;
            }
            int year = DateTime.Now.Year;
            string sql = string.Format("SELECT ISNULL(SUM(FDayNum),0) days FROM ora_t_Leave le " +
                "INNER JOIN ora_t_LeaveHead lh ON le.FID=lh.FID AND FIsOrigin=1 AND FDocumentStatus='C' " +
                "WHERE FName='{0}' AND FLeaveType='{1}' AND YEAR(FStartDate)='{2}' AND FName='{3}' ",
                mLeaver, (int)mLeaveType, year, mLeaver);
            var obj = DBUtils.ExecuteDynamicObject(mContext, sql);
            if(obj.Count <= 0)
            {
                mLastYearCarryOverDays = 0;
            }
            else
            {
                mLastYearCarryOverDays = -double.Parse(obj[0]["days"].ToString());
            }
            
            return mLastYearCarryOverDays;
        }
        /// <summary>
        /// 获取已请假天数
        /// </summary>
        /// <returns></returns>
        public double getAlreadyLeaveDays()
        {
            if (mAlreadyLeaveDays != -1)
            {
                return mAlreadyLeaveDays;
            }
            int year = DateTime.Now.Year;
            string sql = string.Format("SELECT ISNULL(SUM(FDayNum),0) days FROM ora_t_Leave le " +
                "INNER JOIN ora_t_LeaveHead lh ON le.FID=lh.FID AND FIsOrigin=0 AND FDocumentStatus='C' " +
                "WHERE FName='{0}' AND FLeaveType='{1}' AND YEAR(FStartDate)='{2}' AND FName='{3}' ",
                mLeaver, (int)mLeaveType, year, mLeaver);
            var obj = DBUtils.ExecuteDynamicObject(mContext, sql);
            if (obj.Count <= 0)
            {
                mAlreadyLeaveDays = 0;
            }
            else
            {
                mAlreadyLeaveDays = double.Parse(obj[0]["days"].ToString());
            }
            
            return mAlreadyLeaveDays;
        }

        /// <summary>
        /// 获取请假人名称
        /// </summary>
        /// <returns></returns>
        public string getLeaver()
        {
            string sql = "SELECT FName FROM T_HR_EMPINFO_L WHERE FID='" + mLeaver + "'";
            var obj = DBUtils.ExecuteDynamicObject(mContext, sql);
            
            return obj.Count > 0 ? obj[0]["FName"].ToString() : "";
        }

        /// <summary>
        /// 获取请假类型描述
        /// </summary>
        /// <returns></returns>
        public string getLeaveName()
        {
            string value = mLeaveType.ToString();
            FieldInfo field = mLeaveType.GetType().GetField(value);
            object[] objs = field.GetCustomAttributes(typeof(DescriptionAttribute), false);
            if (objs == null || objs.Length == 0)
                return value;
            DescriptionAttribute descriptionAttribute = (DescriptionAttribute)objs[0];
            return descriptionAttribute.Description;
        }

        /// <summary>
        /// 是否为开曼集团
        /// </summary>
        /// <returns></returns>
        public bool IsKaiMan()
        {
            if(mIsKaiman != -1)
            {
                return mIsKaiman == 1 ? true : false;
            }
            string sql = "SELECT FORGID FROM T_ORG_ORGANIZATIONS";
            var obj = DBUtils.ExecuteDynamicObject(mContext, sql);
            foreach(var o in obj)
            {
                if(o["FORGID"].ToString() == "100003")
                {
                    mIsKaiman = 1;
                    return true;
                }
            }
            mIsKaiman = 0;
            return false;
        }
    }
}
