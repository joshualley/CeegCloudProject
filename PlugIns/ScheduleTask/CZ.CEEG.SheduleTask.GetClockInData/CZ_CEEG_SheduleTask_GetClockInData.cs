using Kingdee.BOS;
using Kingdee.BOS.Contracts;
using Kingdee.BOS.Core;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace CZ.CEEG.SheduleTask.GetClockInData
{
    public class CZ_CEEG_SheduleTask_GetClockInData : IScheduleService
    {
        public void Run(Context ctx, Schedule schedule)
        {
            string accToken = GetAccToken();
            ParallelGetClockInData(accToken);
        }

        public static long GetTimestamp(DateTime time)
        {
            System.DateTime startTime = TimeZone.CurrentTimeZone.ToLocalTime(new System.DateTime(1970, 1, 1, 0, 0, 0, 0));
            long t = (time.Ticks - startTime.Ticks) / 10000;   //除10000调整为13位      

            return t;
        }

        public static string HttpPost(string url, string postData)
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

        public static string GetAccToken()
        {
            string tokenUrl = "https://www.yunzhijia.com/gateway/oauth2/token/getAccessToken";
            string timestamp = GetTimestamp(DateTime.Now).ToString();
            //Console.WriteLine(timestamp);
            string data = "eid=16898719&secret=PIeYKwLdUfkLjJoVAlLLAbwy1M5XL9sL&timestamp=" + timestamp + "&scope=resGroupSecret";
            string TokenJson = HttpPost(tokenUrl, data);
            var accToken = (JObject)JsonConvert.DeserializeObject(TokenJson);
            if ((bool)accToken["success"])
            {
                Console.WriteLine(accToken["error"]);
                return "";
            }
            return accToken["data"]["accessToken"].ToString();
        }

        public static JObject GetClockInPage(string accToken, int page)
        {
            string clockInUrl = "https://www.yunzhijia.com/gateway/attendance-data/v1/clockIn/list?accessToken=" + accToken;
            var currDate = DateTime.Now;
            string workDateFrom = currDate.Year.ToString() + "-" + (currDate.Month - 1).ToString() + "-01";
            string workDateTo = currDate.Year.ToString() + "-" + (currDate.Month - 1).ToString() + "-" + DateTime.DaysInMonth(currDate.Year, currDate.Month - 1).ToString();
            string data = "workDateFrom=" + workDateFrom + "&workDateTo=" + workDateTo + "&eid=16898719&start=" + page.ToString();
            string ClockInJson = HttpPost(clockInUrl, data);
            var clockInResult = (JObject)JsonConvert.DeserializeObject(ClockInJson);
            return clockInResult;
        }

        public static List<JObject> ParallelGetClockInData(string accToken)
        {
            var clockInResult = GetClockInPage(accToken, 1);
            int total = int.Parse(clockInResult["total"].ToString());
            int maxPage = total / 200 + 1;
            List<JObject> clockInData = new List<JObject>();
            Parallel.For(0, maxPage, (int i, ParallelLoopState pls) =>
            {
                var results = GetClockInPage(accToken, i + 1);
                if (!(Boolean)results["success"])
                {
                    Console.WriteLine((string)results["errorMsg"]);
                    pls.Break();
                }
                foreach (var result in results["data"])
                {
                    lock (clockInData)
                    {
                        clockInData.Add((JObject)result);
                    }
                }
                Console.WriteLine("Page: {0}", i + 1);
            });
            if (clockInData.Count != total)
            {
                Console.WriteLine("出现错误，重新获取数据！");
                return ParallelGetClockInData(accToken);
            }
            return clockInData;
        }
    }
    
}
