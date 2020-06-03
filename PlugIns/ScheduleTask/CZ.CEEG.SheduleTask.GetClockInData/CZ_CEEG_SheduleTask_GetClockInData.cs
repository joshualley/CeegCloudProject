using Kingdee.BOS;
using Kingdee.BOS.App.Data;
using Kingdee.BOS.Contracts;
using Kingdee.BOS.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;

namespace CZ.CEEG.SheduleTask.GetClockInData
{
    public class CZ_CEEG_SheduleTask_GetClockInData : IScheduleService
    {
        /// <summary>
        /// 请求数据失败后的重试计数，最大1次
        /// </summary>
        private int ReTryCount = 0;

        public void Run(Context ctx, Schedule schedule)
        {
            InsertClockIn(ctx);
        }

        #region 业务逻辑
        private void InsertClockIn(Context ctx)
        {
            string accToken = GetAccToken();
            var datas = ParallelGetClockInData(accToken, true);
            string sql = "";
            foreach (var data in datas)
            {
                sql += string.Format("INSERT INTO " +
                    "ora_HR_SignInData(FClockID, FPosition, FDate, FTimeStamp, FFullDate, FOpenID, FInOut, FUserNA, FDeptNA, FRemark) " +
                    "VALUES('{0}','{1}','{2}','{3}','{4}'," +
                    "'{5}','{6}','{7}','{8}','{9}','{10}','{11}','{12}');\n",
                    data.clockId, data.position, data.day, data.time, TimeStampToDateTime(data.time).ToString(),
                    data.openId, data.positionResult, data.userName, data.department, data.remark);
            }
            try
            {
                DBUtils.Execute(ctx, sql);
            }
            catch (Exception e)
            {
                Console.WriteLine("打卡数据插入出错：{0}", e.Message);
            }
        }
        #endregion

        #region Utils
        private long GetTimestamp(DateTime time)
        {
            System.DateTime startTime = TimeZone.CurrentTimeZone.ToLocalTime(new System.DateTime(1970, 1, 1, 0, 0, 0, 0));
            //除10000调整为13位
            return (time.Ticks - startTime.Ticks) / 10000;
        }

        /// <summary>
        /// 13位时间戳转为时间
        /// </summary>
        /// <param name="timeStamp"></param>
        /// <returns></returns>
        private DateTime TimeStampToDateTime(long timeStamp)
        {
            System.DateTime startTime = TimeZone.CurrentTimeZone.ToLocalTime(new System.DateTime(1970, 1, 1, 0, 0, 0));
            return startTime.AddMilliseconds(timeStamp);
        }

        private string HttpPost(string url, string postData)
        {
            var request = (HttpWebRequest)WebRequest.Create(url);
            var data = Encoding.ASCII.GetBytes(postData);

            request.Method = "POST";
            request.ContentType = "application/x-www-form-urlencoded";
            request.ContentLength = data.Length;
            using (var stream = request.GetRequestStream())
            {
                stream.Write(data, 0, data.Length);
            }
            var response = (HttpWebResponse)request.GetResponse();
            var responseStr = new StreamReader(response.GetResponseStream()).ReadToEnd();

            return responseStr;
        }
        #endregion

        #region Actions
        /// <summary>
        /// 获取云之家数据访问Token
        /// </summary>
        /// <returns></returns>
        private string GetAccToken()
        {
            string tokenUrl = "https://www.yunzhijia.com/gateway/oauth2/token/getAccessToken";
            string timestamp = GetTimestamp(DateTime.Now).ToString();
            //Console.WriteLine(timestamp);
            string data = "eid=16898719&secret=PIeYKwLdUfkLjJoVAlLLAbwy1M5XL9sL&timestamp=" + timestamp + "&scope=resGroupSecret";
            string TokenJson = HttpPost(tokenUrl, data);
            //Console.WriteLine(TokenJson);
            AccToken accToken;
            using (var ms = new MemoryStream(Encoding.Unicode.GetBytes(TokenJson)))
            {
                var deseralizer = new DataContractJsonSerializer(typeof(AccToken));
                accToken = (AccToken)deseralizer.ReadObject(ms);
            }
            if (!accToken.success)
            {
                Console.WriteLine("获取Token失败：{0}.", accToken.error);
                return "";
            }

            return accToken.data.accessToken;

        }

        /// <summary>
        /// 获取一页签到数据，数据量最大200
        /// </summary>
        /// <param name="accToken"></param>
        /// <param name="page"></param>
        /// <returns></returns>
        private ClockInResult GetClockInPage(string accToken, int page, bool isCurrMonth = false)
        {
            string clockInUrl = "https://www.yunzhijia.com/gateway/attendance-data/v1/clockIn/list?accessToken=" + accToken;
            var currDate = DateTime.Now;
            int month = isCurrMonth ? currDate.Month : currDate.Month - 1;
            int day = isCurrMonth ? currDate.Day : DateTime.DaysInMonth(currDate.Year, currDate.Month - 1);
            string workDateFrom = currDate.Year.ToString() + "-" + month.ToString() + "-01";
            string workDateTo = currDate.Year.ToString() + "-" + month.ToString() + "-" + day.ToString();
            //Console.WriteLine("s: {0}, e: {1}", workDateFrom, workDateTo);
            string data = "workDateFrom=" + workDateFrom + "&workDateTo=" + workDateTo + "&eid=16898719&start=" + page.ToString();
            string ClockInJson = HttpPost(clockInUrl, data);
            ClockInResult clockInResult = new ClockInResult();
            using (var ms = new MemoryStream(Encoding.Unicode.GetBytes(ClockInJson)))
            {
                var deseralizer = new DataContractJsonSerializer(typeof(ClockInResult));
                clockInResult = (ClockInResult)deseralizer.ReadObject(ms);
            }
            if (!clockInResult.success)
            {
                Console.WriteLine("获取签到数据失败：{0}.", clockInResult.errorMsg);
            }
            return clockInResult;
        }

        private List<ClockInData> ParallelGetClockInData(string accToken, bool isCurrMonth)
        {
            var clockInResult = GetClockInPage(accToken, 1, isCurrMonth);
            int total = int.Parse(clockInResult.total.ToString());
            int maxPage = total / 200 + 1;

            List<ClockInData> allData = new List<ClockInData>();

            Parallel.For(0, maxPage, (int i, ParallelLoopState pls) => {
                var results = GetClockInPage(accToken, i + 1, isCurrMonth);
                if (!results.success)
                {
                    Console.WriteLine("发生错误，循环中断：{0}", clockInResult.errorMsg);
                    pls.Break();
                }
                else
                {
                    Console.WriteLine("Page: {0}, MaxPage: {1}.", i + 1, maxPage);
                    lock (allData)
                    {
                        allData.AddRange(results.data);
                    }
                }
            });

            if (allData.Count != total && ReTryCount <= 1)
            {
                ReTryCount++;
                Console.WriteLine("出现错误，重新获取数据！");
                return ParallelGetClockInData(accToken, isCurrMonth);
            }
            return allData;
        }

        #endregion

    }


    #region AccToken序列化Model
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
    #endregion

    #region ClockIn 签到数据序列化Model
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
    #endregion

}
