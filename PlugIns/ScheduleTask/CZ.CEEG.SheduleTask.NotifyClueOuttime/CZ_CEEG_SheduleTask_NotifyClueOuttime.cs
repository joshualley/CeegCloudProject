using Kingdee.BOS;
using Kingdee.BOS.App.Data;
using Kingdee.BOS.Contracts;
using Kingdee.BOS.Core;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.Util;
using Kingdee.BOS.Msg;
using Kingdee.BOS.ServiceHelper.Messages;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using Kingdee.BOS.ServiceHelper;
using Kingdee.BOS.Core.Metadata;
using Kingdee.BOS.Core.Bill.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Core.DynamicForm;
using Kingdee.BOS.Core.Bill;

//CZ.CEEG.SheduleTask.NotifyClueOuttime.CZ_CEEG_SheduleTask_NotifyClueOuttime,CZ.CEEG.SheduleTask.NotifyClueOuttime

namespace CZ.CEEG.SheduleTask.NotifyClueOuttime
{
    [Description("提醒线索即将超时")]
    [HotUpdate]
    public class CZ_CEEG_SheduleTask_NotifyClueOuttime : IScheduleService
    {

        public void Run(Context ctx, Schedule schedule)
        {
            string sql = string.Format(@"SELECT c.FID,FBILLNO,FCLUEENDDT,FUSERID,FOPENID,FUSERNAME FROM ora_CRM_Clue c
            INNER JOIN T_SEC_XTUSERMAP m on c.FCREATORID=m.FUSERID
            WHERE FCREATORID<>FCRMHOLDER AND FCLUESTATUS=2
            AND DATEDIFF(DAY, FCLUEENDDT, GETDATE())<2");
            var objs = DBUtils.ExecuteDynamicObject(ctx, sql);
            foreach(var obj in objs)
            {
                string fid = obj["FID"].ToString();
                string userId = obj["FUSERID"].ToString();
                string billNo = obj["FBILLNO"].ToString();
                string clueEndDt = DateTime.Parse(obj["FCLUEENDDT"].ToString()).ToString("yyyy-MM-dd");
                string openId = obj["FOPENID"].ToString();

                string title = "您的线索编号为：" + billNo + "，即将超过有效期！";
                string content = string.Format("您的线索编号为：{0}，即将超过有效期：{1}，请尽快处理！", billNo, clueEndDt);
                List<string> users = new List<string>();
                users.Add(openId);
                //var bv = OpenWebView(ctx, "ora_CRM_MBL_XS", fid);
                //string pageId = bv.PageId.ToString();
                string url = string.Format(@"http://erp.ceegpower.com/K3Cloud/mobile/k3cloud.html?entryrole=XT
                &appid=500341833&formId=ora_CRM_MBL_XS&formType=mobilelist&acctid=5d3ea17f85b053");//&pageId="+pageId
                SendYZJMessage(ctx, fid, users, title, content, url);
            }

            
        }

        /// <summary>
        /// 打开PC的Web页面，暂时不好用
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="FormId"></param>
        /// <param name="pkid"></param>
        /// <returns></returns>
        private IDynamicFormView OpenWebView(Context ctx, string FormId, string pkid = null)
        {
            var meta = MetaDataServiceHelper.Load(ctx, FormId) as FormMetadata;
            BusinessInfo info = meta.BusinessInfo;
            var form = info.GetForm();

            BillOpenParameter param = new BillOpenParameter(form.Id, null);
            //param.SetCustomParameter("formID", form.Id);
            //param.SetCustomParameter("status", (pkid != null ? "View" : "AddNew"));
            //param.SetCustomParameter("formID", form.CreateFormPlugIns());
            param.Context = ctx;
            param.ServiceName = form.FormServiceName;
            param.PageId = Guid.NewGuid().ToString();
            param.FormMetaData = meta;
            param.LayoutId = param.FormMetaData.GetLayoutInfo().Id;
            param.Status = pkid != null ? OperationStatus.EDIT : OperationStatus.ADDNEW;
            param.PkValue = pkid;
            param.CreateFrom = CreateFrom.Default;
            param.ParentId = 0;
            param.GroupId = "";
            param.DefaultBillTypeId = null;
            param.DefaultBusinessFlowId = null;
            param.SetCustomParameter("ShowConfirmDialogWhenChangeOrg", false);

            //IResourceServiceProvider provider = form.GetFormServiceProvider();
            //IDynamicFormView bv = provider.GetService(typeof(IDynamicFormView)) as IDynamicFormView;
            //(bv as IBillViewService).Initialize(param, provider);
            //(bv as IDynamicFormView).RegisterPlugIn(getFormStatePlugIn);
            Type type = Type.GetType("Kingdee.BOS.Web.Import.ImportBillView,Kingdee.BOS.Web");
            IDynamicFormView bv = (IDynamicFormView)Activator.CreateInstance(type);
            var provider = form.GetFormServiceProvider();
            (bv as IBillViewService).Initialize(param, provider);
            (bv as IBillView).CreateNewModelData();
            if (param.Status != OperationStatus.ADDNEW)
            {
                (bv as IBillViewService).LoadData();
            }
            return bv;
        }

        private void SendYZJMessage(Context ctx, string fid, List<string> users, string title, string content, string url="")
        {
            XunTongMessage message = new XunTongMessage();
            message.msgSource = "定时任务";
            message.msgSummary = "线索即将逾期提醒";
            message.Title = title;//标题
            message.Date = DateTime.Now.ToString("yyyy-MM-dd");//日期
            message.Text = content;//内容
            message.PubAcctCode = "XT-4249be9f-5181-4a2a-a01f-7a9be93f1b3a";//公共号帐号，移动BOS
            message.PubAcctKey = "4839223c64dff23b04bf4e3376b7a082";//公共号密钥
            message.Users = users;//接收用户
            message.AppId = "500341833";//轻应用ID，线索
            message.Url = url;
            message.Todo = 0;//标识任务为非待办
            message.ToEid = "";//企业号ID
            message.PKValue = fid;
            message.TodoMsgIds = "";//待办任务ID
            //XTSendResult result = XunTongServiceHelper.SendMessage(ctx, message);
            XunTongServiceHelper.SendMessage(ctx, message);
            
        }

    }
}
