using Kingdee.BOS;

namespace CZ.CEEG.OABos.LeaveApply.LeaveType
{
    public interface ILeaveType
    {
        /// <summary>
        /// 获取已请假及假期剩余信息
        /// </summary>
        /// <returns></returns>
        string GetLeftLeaveMessage();
        /// <summary>
        /// 合并请假天数
        /// </summary>
        /// <param name="days"></param>
        void MergeLeaveDays(double days);
        /// <summary>
        /// 校验请假是否超时
        /// </summary>
        /// <param name="msg"></param>
        /// <returns></returns>
        bool ValidateLeave(ref string msg);

        ReportData GetReportData();
    }
}
