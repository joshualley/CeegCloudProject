

using System.Runtime.Serialization;

namespace CZ.CEEG.SheduleTask.GetClockInData.Models
{
    /// <summary>
    /// 单条签到数据
    /// </summary>
    [DataContract]
    public class ClockInData
    {
        [DataMember]
        public string lng { get; set; }
        [DataMember]
        public string openId { get; set; }
        [DataMember]
        public string bssid { get; set; }
        [DataMember]
        public string positionResult { get; set; }
        [DataMember]
        public string photoId { get; set; }
        [DataMember]
        public string remark { get; set; }
        [DataMember]
        public string userName { get; set; }
        [DataMember]
        public string ssid { get; set; }
        [DataMember]
        public ApproveResult approveResult { get; set; }
        [DataMember]
        public string clockId { get; set; }
        [DataMember]
        public string position { get; set; }
        [DataMember]
        public long time { get; set; }
        [DataMember]
        public string department { get; set; }
        [DataMember]
        public string day { get; set; }
        [DataMember]
        public string lat { get; set; }
    }
}
