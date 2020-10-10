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

//计划任务注册内容
//CZ.CEEG.SheduleTask.GetClockInData.CZ_CEEG_SheduleTask_GetClockInData,CZ.CEEG.SheduleTask.GetClockInData

namespace CZ.CEEG.SheduleTask.GetClockInData
{
    public class CZ_CEEG_SheduleTask_GetClockInData : IScheduleService
    {
        private Context context;
        
        public void Run(Context ctx, Schedule schedule)
        {
            context = ctx;
            DateTime now = DateTime.Now;
            string from = "";
            string to = "";
            if (now.Day == 1)
            {
                int fromYear = now.Month == 1 ? now.Year - 1 : now.Year;
                int fromMonth = now.Month == 1 ? 12 : now.Month - 1;
                from = string.Format("{0}-{1}-01", fromYear, fromMonth);
                to = string.Format("{0}-{1}-01", now.Year, now.Month);
            }
            else
            {
                from = string.Format("{0}-{1}-01", now.Year, now.Month);
                to = now.ToString();
            }
            InsertDataWithinDate(from, to);
        }

        #region 业务逻辑

        private void InsertDataWithinDate(string fromDt, string toDt)
        {
            string accToken = GetAccToken();
            if (accToken == "")
                return;
            string sql = string.Format("SELECT FID FROM ora_HR_SignInData WHERE FDate BETWEEN '{0}' AND '{1}'", fromDt, toDt);
            var objs = DBUtils.ExecuteDynamicObject(context, sql);
            sql = string.Format("DELETE FROM ora_HR_SignInData WHERE FDate BETWEEN '{0}' AND '{1}'", fromDt, toDt);
            DBUtils.Execute(context, sql);
            Log(context, "info", "删除本月数据：" + objs.Count.ToString() + "条。");

            var datas = GetClockInDatas(accToken, fromDt, toDt);
            if(datas.Count <= 0)
            {
                Log(context, "info", "未获取到签到数据。");
                return;
            }
            sql = "";
            foreach (var data in datas)
            {
                sql += string.Format("INSERT INTO " +
                    "ora_HR_SignInData(FClockID, FPosition, FDate, FTimeStamp, FFullDate, FOpenID, FInOut, FUserNA, FDeptNA, FRemark) " +
                    "VALUES('{0}','{1}','{2}','{3}','{4}','{5}','{6}','{7}','{8}','{9}');\n",
                    data.clockId, data.position.Replace("'", "''"), data.day, data.time, TimeStampToDateTime(data.time).ToString(),
                    data.openId, data.positionResult, data.userName, data.department, data.remark);
            }
            
            try
            {
                DBUtils.Execute(context, sql);
                Log(context, "info", string.Format("插入{0}-{1}的数据：{2}条。", fromDt, toDt, datas.Count));
            }
            catch (Exception e)
            {
                Log(context, "error", "签到数据插入出错：" + e.Message);
            }
        }

        
        #endregion

        #region Utils
        private void Log(Context ctx, string level, string msg)
        {
            string _namespase = System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Namespace;
            string _classname = System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.FullName;
            string time = DateTime.Now.ToString();
            //msg = "\"" + msg.Replace("'", "\"") + "\"";
            string sql = string.Format("insert into ora_CZ_LogRecord(FLogLevel, FNameSpace, FClassName, FErrTime, FErrMessage) " +
                "values('{0}', '{1}', '{2}', '{3}', '{4}')", level, _namespase, _classname, time, msg);
            DBUtils.Execute(ctx, sql);
        }

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
                Log(context, "error", "获取Token失败：" + accToken.error);
                return "";
            }

            return accToken.data.accessToken;

        }

        /// <summary>
        /// 获取一页签到数据，数据量最大200
        /// </summary>
        /// <param name="accToken"></param>
        /// <param name="lastId"></param>
        /// <returns></returns>
        private ClockInResult GetClockInPage(string accToken, string lastId, string workDateFrom, string workDateTo)
        {
            string clockInUrl = "https://www.yunzhijia.com/gateway/attendance-data/v1/clockIn/clockintime/list?accessToken=" + accToken;
            var currDate = DateTime.Now;
            
            lastId = lastId == "" ? "" : "&lastId=" + lastId;
            string data = "workDateFrom=" + GetTimestamp(DateTime.Parse(workDateFrom)).ToString() + 
                "&workDateTo=" + GetTimestamp(DateTime.Parse(workDateTo)).ToString() + lastId;
            string ClockInJson = HttpPost(clockInUrl, data);
            ClockInResult clockInResult = new ClockInResult();
            using (var ms = new MemoryStream(Encoding.Unicode.GetBytes(ClockInJson)))
            {
                var deseralizer = new DataContractJsonSerializer(typeof(ClockInResult));
                clockInResult = (ClockInResult)deseralizer.ReadObject(ms);
            }
            if (!clockInResult.success)
            {
                Log(context, "error", "获取签到数据失败：" + clockInResult.errorMsg);
            }
            return clockInResult;
        }

        private List<ClockInData> GetClockInDatas(string accToken, string workDateFrom, string workDateTo)
        {
            List<ClockInData> allData = new List<ClockInData>();
            var clockInResult = GetClockInPage(accToken, "", workDateFrom, workDateTo);
            if (!clockInResult.success)
            {
                return allData;
            }
            
            allData.AddRange(clockInResult.data);
            while (true)
            {
                var results = GetClockInPage(accToken, allData[allData.Count - 1].clockId, workDateFrom, workDateTo);
                if (!results.success)
                {
                    Log(context, "error", "发生错误，循环中断：" + clockInResult.errorMsg);
                    break;
                }
                if (results.data.Count == 0)
                {
                    Log(context, "info", "数据获取完成。");
                    break;
                }
                allData.AddRange(results.data);
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
