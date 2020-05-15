using Kingdee.BOS;
using Kingdee.BOS.App.Data;
using Kingdee.BOS.Contracts;
using Kingdee.BOS.Core;
using Kingdee.BOS.Msg;
using Kingdee.BOS.ServiceHelper.Messages;
using Kingdee.BOS.Util;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

//CZ.CEEG.SheduleTask.WorkPlanNotify.CZ_CEEG_SheduleTask_WorkPlanNotify,CZ.CEEG.SheduleTask.WorkPlanNotify

namespace CZ.CEEG.SheduleTask.WorkPlanNotify
{
    [Description("提醒线索即将超时")]
    [HotUpdate]
    public class CZ_CEEG_SheduleTask_WorkPlanNotify : IScheduleService
    {
        public void Run(Context ctx, Schedule schedule)
        {
            string days = "3,5,7";
            if (days.Contains(DateTime.Now.Day.ToString()))
            {
                string sql = @"exec proc_czly_GetNeedNotifiedUsers";
                var objs = DBUtils.ExecuteDynamicObject(ctx, sql);

                foreach (var obj in objs)
                {
                    //string userId = obj["FUSERID"].ToString();
                    string openId = obj["FOPENID"].ToString();

                    string title = "您本月的工作计划还未提交，请尽快进入系统进行提交！";
                    string content = "您本月的工作计划还未提交，请尽快进入系统进行提交！";
                    List<string> users = new List<string>();
                    users.Add(openId);
                    //var bv = OpenWebView(ctx, "ora_CRM_MBL_XS", fid);
                    //string pageId = bv.PageId.ToString();
                    string url = @"http://erp.ceegpower.com/k3cloud/html5/index.aspx?ud=eyJkYmlkIjoiNWQzZWExN2Y4NWIwNTMiLCJlbnRyeXJvbGUiOiJYVCIsImxjaWQiOjIwNTIsIm9yaWdpbnR5cGUiOiJYVCIsImZvcm1pZCI6Im9yYV9UYXNrX1BlcnNvbmFsUmVwb3J0IiwiZm9ybXR5cGUiOiJiaWxsIiwicGtpZCI6IiJ9&acctid=5d3ea17f85b053";
                    SendYZJMessage(ctx, users, title, content, url);
                }
            }
        }


        private void SendYZJMessage(Context ctx, List<string> users, string title, string content, string url = "", string fid = "")
        {
            XunTongMessage message = new XunTongMessage();
            message.msgSource = "定时任务";
            message.msgSummary = "工作计划未提交提醒";
            message.Title = title;//标题
            message.Date = DateTime.Now.ToString("yyyy-MM-dd");//日期
            message.Text = content;//内容
            message.PubAcctCode = "XT-4249be9f-5181-4a2a-a01f-7a9be93f1b3a";//公共号帐号，移动BOS
            message.PubAcctKey = "4839223c64dff23b04bf4e3376b7a082";//公共号密钥
            message.Users = users;//接收用户
            message.AppId = "500408614";//轻应用ID，工作计划
            message.Url = url;
            message.Todo = 0;//标识任务为非待办
            message.ToEid = "";//企业号ID
            //message.PKValue = fid;
            message.TodoMsgIds = "";//待办任务ID
            //XTSendResult result = XunTongServiceHelper.SendMessage(ctx, message);
            XunTongServiceHelper.SendMessage(ctx, message);

        }
    }
}
