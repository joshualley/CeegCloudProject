

namespace CZ.CEEG.OABos.LeaveApply.LeaveType
{
    /// <summary>
    /// 报表数据
    /// </summary>
    public class ReportData
    {
        public int Year { get; set; }
        public int LeaveType { get; set; }
        public long Name { get; set; }
        /// <summary>
        /// 本年可请
        /// </summary>
        public double AllowDays { get; set; }
        /// <summary>
        /// 目前可请
        /// </summary>
        public double CurrDays { get; set; }
        /// <summary>
        /// 上年结转
        /// </summary>
        public double LastYearLeft { get; set; }
        /// <summary>
        /// 已请天数
        /// </summary>
        public double LeftDays { get; set; }
        /// <summary>
        /// 剩余可请
        /// </summary>
        public double SurplusDays { get; set; }
    }
}
