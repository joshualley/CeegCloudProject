using System.Runtime.Serialization;

namespace CZ.CEEG.SheduleTask.GetClockInData.Models
{
    /// <summary>
    /// token数据包
    /// </summary>
    [DataContract]
    public class AccToken
    {
        [DataMember]
        public AccTokenInfo data { get; set; }
        [DataMember]
        public string error { get; set; }
        [DataMember]
        public int errorCode { get; set; }
        [DataMember]
        public bool success { get; set; }
    }
}
