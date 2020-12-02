using System.Runtime.Serialization;

namespace CZ.CEEG.SheduleTask.GetClockInData.Models
{
    /// <summary>
    /// 用于解析获取到的token
    /// </summary>
    [DataContract]
    public class AccTokenInfo
    {
        [DataMember]
        public string accessToken { get; set; }
        [DataMember]
        public int expireIn { get; set; }
        [DataMember]
        public string refreshToken { get; set; }
    }
}
