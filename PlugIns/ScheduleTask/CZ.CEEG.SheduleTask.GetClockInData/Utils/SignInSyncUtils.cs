using CZ.CEEG.SheduleTask.GetClockInData.Models;
using Kingdee.BOS;
using Kingdee.BOS.App.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Runtime.Serialization.Json;
using System.Text;

namespace CZ.CEEG.SheduleTask.GetClockInData.Utils
{
    public class SignInSyncUtils
    {

        private Context ctx;
        public SignInSyncUtils(Context ctx)
        {
            this.ctx = ctx;
        }

        #region private
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

        #region 工具函数
        /// <summary>
        /// 日志记录工具
        /// </summary>
        /// <param name="level"></param>
        /// <param name="msg"></param>
        public void Log(string level, string msg)
        {
            string _namespase = System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Namespace;
            string _classname = System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.FullName;
            string time = DateTime.Now.ToString();
            //msg = "\"" + msg.Replace("'", "\"") + "\"";
            string sql = string.Format("insert into ora_CZ_LogRecord(FLogLevel, FNameSpace, FClassName, FErrTime, FErrMessage) " +
                "values('{0}', '{1}', '{2}', '{3}', '{4}')", level, _namespase, _classname, time, msg);
            DBUtils.Execute(ctx, sql);
        }

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
                Log("error", "获取Token失败：" + accToken.error);
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
                Log("error", "获取签到数据失败：" + clockInResult.errorMsg);
            }
            return clockInResult;
        }

        /// <summary>
        /// 获取签到所有数据
        /// </summary>
        /// <param name="accToken"></param>
        /// <param name="workDateFrom"></param>
        /// <param name="workDateTo"></param>
        /// <returns></returns>
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
                    Log("error", "发生错误，循环中断：" + clockInResult.errorMsg);
                    break;
                }
                if (results.data.Count == 0)
                {
                    Log("info", "数据获取完成。");
                    break;
                }
                allData.AddRange(results.data);
            }

            return allData;
        }
        #endregion

        #region Actions
        /// <summary>
        /// 根据日期段获取考勤数据
        /// </summary>
        /// <param name="fromDt"></param>
        /// <param name="toDt"></param>
        public void InsertDataWithinDate(string fromDt, string toDt)
        {
            string accToken = GetAccToken();
            if (accToken == "")
                return;

            var datas = GetClockInDatas(accToken, fromDt, toDt);
            if (datas.Count <= 0)
            {
                Log("info", "未获取到签到数据。");
                return;
            }
            Log("info", "获取到签到数据" + datas.Count + "条。");

            // 先删除数据
            string sql1 = string.Format("SELECT FID FROM ora_HR_SignInData WHERE FDate BETWEEN '{0}' AND '{1}'", fromDt, toDt);
            var objs = DBUtils.ExecuteDynamicObject(ctx, sql1);
            sql1 = string.Format("DELETE FROM ora_HR_SignInData WHERE FDate BETWEEN '{0}' AND '{1}'", fromDt, toDt);
            DBUtils.Execute(ctx, sql1);
            Log("info", "删除本月数据：" + objs.Count.ToString() + "条。");

            string sql = "";
            // 分成4段插入数据，避免数据量过大
            long count = 0;
            long lastCount = count;
            double time = 4;
            int size = (int)Math.Ceiling(datas.Count / time);
            foreach (var data in datas)
            {
                sql += string.Format("INSERT INTO " +
                    "ora_HR_SignInData(FClockID, FPosition, FDate, FTimeStamp, FFullDate, FOpenID, FInOut, FUserNA, FDeptNA, FRemark) " +
                    "VALUES('{0}','{1}','{2}','{3}','{4}','{5}','{6}','{7}','{8}','{9}');\n",
                    data.clockId, data.position.Replace("'", "''"), data.day, data.time, TimeStampToDateTime(data.time).ToString(),
                    data.openId, data.positionResult, data.userName, data.department, data.remark);
                count++;
                if (count % size == 0 || count == datas.Count)
                {
                    // 将本月数据插入
                    long para = count / size;
                    try
                    {
                        DBUtils.Execute(ctx, sql);
                        Log("info", string.Format("插入{0}至{1}的第{2}段数据：{3}条。",
                            fromDt, toDt, para, count - lastCount));
                    }
                    catch (Exception e)
                    {
                        Log("error", "签到数据插入出错：" + e.Message);
                    }
                    sql = "";
                    lastCount = count;
                }
            }

        }


        public void InsertDataWithinDate(string fromDt, string toDt, Action<string> cbFunc)
        {
            string accToken = GetAccToken();
            if (accToken == "")
                return;

            var datas = GetClockInDatas(accToken, fromDt, toDt);
            string msg = "";
            if (datas.Count <= 0)
            {
                msg = "[手动同步]未获取到签到数据！";
                Log("info", msg);
                cbFunc(msg);
                return;
            }
            msg = "[手动同步]获取到签到数据" + datas.Count + "条。";
            Log("info", msg);
            cbFunc(msg);

            // 先删除数据
            string sql1 = string.Format("SELECT FID FROM ora_HR_SignInData WHERE FDate BETWEEN '{0}' AND '{1}'", fromDt, toDt);
            var objs = DBUtils.ExecuteDynamicObject(ctx, sql1);
            sql1 = string.Format("DELETE FROM ora_HR_SignInData WHERE FDate BETWEEN '{0}' AND '{1}'", fromDt, toDt);
            DBUtils.Execute(ctx, sql1);

            msg = "[手动同步]删除本月数据：" + objs.Count.ToString() + "条。";
            Log("info", msg);
            cbFunc(msg);

            string sql = "";
            // 分成4段插入数据，避免数据量过大
            long count = 0;
            long lastCount = count;
            double time = 4;
            int size = (int)Math.Ceiling(datas.Count / time);
            foreach (var data in datas)
            {
                sql += string.Format("INSERT INTO " +
                    "ora_HR_SignInData(FClockID, FPosition, FDate, FTimeStamp, FFullDate, FOpenID, FInOut, FUserNA, FDeptNA, FRemark) " +
                    "VALUES('{0}','{1}','{2}','{3}','{4}','{5}','{6}','{7}','{8}','{9}');\n",
                    data.clockId, data.position.Replace("'", "''"), data.day, data.time, TimeStampToDateTime(data.time).ToString(),
                    data.openId, data.positionResult, data.userName, data.department, data.remark);
                count++;
                if (count % size == 0 || count == datas.Count)
                {
                    // 将本月数据插入
                    long para = count / size;
                    try
                    {
                        DBUtils.Execute(ctx, sql);
                        msg = string.Format("[手动同步]插入{0}至{1}的第{2}段数据：{3}条。",
                            fromDt, toDt, para, count - lastCount);
                        Log("info", msg);
                        cbFunc(msg);
                    }
                    catch (Exception e)
                    {
                        msg = "[手动同步]签到数据插入出错：" + e.Message;
                        Log("error", msg);
                        cbFunc(msg);
                    }
                    sql = "";
                    lastCount = count;
                }
            }
        }
        #endregion
    }
}
