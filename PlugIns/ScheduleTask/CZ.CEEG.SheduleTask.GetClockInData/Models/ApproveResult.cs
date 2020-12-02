using System.Runtime.Serialization;


namespace CZ.CEEG.SheduleTask.GetClockInData.Models
{
    /// <summary>
    /// 打卡审批信息
    /// </summary>
    [DataContract]
    public class ApproveResult
    {
        [DataMember]
        public string approveStatus { get; set; }
        [DataMember]
        public string approveType { get; set; }
        [DataMember]
        public long approveTime { get; set; }
        [DataMember]
        public string approveUserOpenId { get; set; }
        [DataMember]
        public string approveId { get; set; }
    }
}
