

using System.Collections.Generic;
using System.Runtime.Serialization;

namespace CZ.CEEG.SheduleTask.GetClockInData.Models
{
    /// <summary>
    /// 签到数据包
    /// </summary>
    [DataContract]
    public class ClockInResult
    {
        [DataMember]
        public int errorCode { get; set; }
        [DataMember]
        public int total { get; set; }
        [DataMember]
        public List<ClockInData> data { get; set; }
        [DataMember]
        public bool success { get; set; }
        [DataMember]
        public string errorMsg { get; set; }
    }
}
