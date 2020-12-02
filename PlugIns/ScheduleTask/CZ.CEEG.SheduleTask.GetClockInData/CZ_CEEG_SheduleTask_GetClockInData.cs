using CZ.CEEG.SheduleTask.GetClockInData.Utils;
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
        
        public void Run(Context ctx, Schedule schedule)
        {
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
            var req = new SignInSyncUtils(ctx);
            req.InsertDataWithinDate(from, to);
        }

    }

}
