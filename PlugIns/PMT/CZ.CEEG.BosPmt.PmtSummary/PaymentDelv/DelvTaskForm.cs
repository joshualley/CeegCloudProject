using Kingdee.BOS;
using Kingdee.BOS.App.Data;
using Kingdee.BOS.Core.Bill;
using Kingdee.BOS.Core.Bill.PlugIn;
using Kingdee.BOS.Core.DynamicForm;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Core.Metadata;
using Kingdee.BOS.Msg;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.ServiceHelper;
using Kingdee.BOS.ServiceHelper.Messages;
using Kingdee.BOS.Util;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace CZ.CEEG.BosPmt.PmtSummary.PaymentDelv
{
    [HotUpdate]
    [Description("货款移交任务单表单")]
    public class DelvTaskForm: AbstractBillPlugIn
    {
        private string status = "Z";
        public override void AfterBindData(EventArgs e)
        {
            base.AfterBindData(e);
            status = this.Model.GetValue("FDocumentStatus").ToString();
        }

        public override void AfterDoOperation(AfterDoOperationEventArgs e)
        {
            base.AfterDoOperation(e);
            switch (e.Operation.Operation.ToUpperInvariant())
            {
                case "SAVE":
                    if (status.Equals("Z")) Act_SendYunzhijiaMessage();
                    break;
            }
        }

        public override void AfterBarItemClick(AfterBarItemClickEventArgs e)
        {
            base.AfterBarItemClick(e);
            switch (e.BarItemKey.ToUpperInvariant())
            {
                case "ORA_TBCHANGEEXECUTOR": // 调整承办人
                    Act_ChangeExecutor();
                    break;
            }
        }

        public override void EntityRowDoubleClick(EntityRowClickEventArgs e)
        {
            base.EntityRowDoubleClick(e);
            string billno = this.Model.GetValue("FReportNo", e.Row).ToString();
            string sql = $"select FID from ora_PMT_DelvReport where FBillNo='{billno}'";
            var id = DBUtils.ExecuteDynamicObject(Context, sql).FirstOrDefault()?["FID"].ToString() ?? "0";
            BillShowParameter param = new BillShowParameter();
            param.FormId = "ora_PMT_DelvReport";
            param.OpenStyle.ShowType = ShowType.Modal;
            param.ParentPageId = this.View.PageId;
            param.PKey = id;
            param.Status = OperationStatus.VIEW;

            this.View.ShowForm(param);
        }

        public override void AfterEntryBarItemClick(AfterBarItemClickEventArgs e)
        {
            base.AfterEntryBarItemClick(e);
            switch (e.BarItemKey.ToUpperInvariant())
            {
                case "ORA_TBREPORT": //汇报进度
                    Act_OpenReportBill();
                    break;
            }
        }

        private void Act_SendYunzhijiaMessage()
        {
            string userId = (this.Model.GetValue("FExecutorId") as DynamicObject)?["Id"].ToString() ?? "0";
            string billNo = this.Model.GetValue("FBillNo").ToString();
            string taskIntro = this.Model.GetValue("FTaskIntro").ToString();

            // 第一次保存时，发送云之家消息提示承办人
            string title = $"您被分配了一条新的货款移交任务，请及时登录系统处理，单号：{billNo}";
            string content = title;
            string url = "http://erp.ceegpower.com/k3cloud/html5/index.aspx?ud=eyJkYmlkIjoiNWYyZjliYzgxMDY4MTQiLCJlbnRyeXJvbGUiOiJYVCIsImxjaWQiOjIwNTIsIm9yaWdpbnR5cGUiOiJYVCIsImZvcm1pZCI6Im9yYV9QTVRfRGVsdlRhc2siLCJmb3JtdHlwZSI6Imxpc3QiLCJwa2lkIjoiIiwib3RoZXJhcmdzIjoiIn0%3D&appid=500728930&acctid=5f2f9bc8106814";
            SendYZJMessage(Context, new List<string> { userId }, title, content, url);

            this.View.ShowMessage("您发布的任务已通过消息提醒承办人办理。");
        }

        private void SendYZJMessage(Context ctx, List<string> users, string title, string content, string url = "")
        {
            XunTongMessage message = new XunTongMessage();
            message.msgSource = "货款移交任务单";
            message.msgSummary = "任务分配提醒";
            message.Title = title;//标题
            message.Date = DateTime.Now.ToString("yyyy-MM-dd");//日期
            message.Text = content;//内容
            message.PubAcctCode = "XT-4249be9f-5181-4a2a-a01f-7a9be93f1b3a";//公共号帐号，移动BOS
            message.PubAcctKey = "4839223c64dff23b04bf4e3376b7a082";//公共号密钥
            message.Users = users;//接收用户
            message.AppId = "500728930";//轻应用ID，货款移交任务
            // message.Url = url;
            message.Todo = 0;//标识任务为非待办
            message.ToEid = "";//企业号ID
            message.TodoMsgIds = "";//待办任务ID
            XunTongServiceHelper.SendMessage(ctx, message);
        }

        /// <summary>
        /// 调整承办人
        /// </summary>
        private void Act_ChangeExecutor()
        {
            string status = this.Model.GetValue("FDocumentStatus").ToString();
            if (status == "Z")
            {
                this.View.ShowErrMessage("保存后才能进行操作！");
                return;
            }
            string FCreatorId = (this.Model.GetValue("FCreatorId") as DynamicObject)?["Id"].ToString() ?? "0";
            if (!FCreatorId.Equals(this.Context.UserId.ToString()))
            {
                this.View.ShowErrMessage($"任务分配人才能进行调整！");
                return;
            }

            string FExecutorId = (this.Model.GetValue("FExecutorId") as DynamicObject)?["Id"].ToString() ?? "0";
            var param = new DynamicFormShowParameter();
            param.FormId = "ora_PMT_ChangeExecutor";
            param.OpenStyle.ShowType = ShowType.Modal;
            param.ParentPageId = this.View.PageId;
            param.CustomParams.Add("FExecutorId", FExecutorId);
            this.View.ShowForm(param, result => 
            {
                if (result.ReturnData == null) return;
                FExecutorId = result.ReturnData.ToString();
                string id = this.Model.DataObject["Id"].ToString();
                string today = DateTime.Today.ToString();
                string sql = $"update ora_PMT_DelvTask set FExecutorId={FExecutorId},FDate='{today}' where FID={id}";
                DBUtils.Execute(Context, sql);
                this.View.Refresh();
                Act_SendYunzhijiaMessage();
            });
        }

        /// <summary>
        /// 打开汇报单
        /// </summary>
        private void Act_OpenReportBill()
        {
            string status = this.Model.GetValue("FDocumentStatus").ToString();
            if (status == "Z")
            {
                this.View.ShowErrMessage("保存后才能进行操作！");
                return;
            }
            string executorId = (this.Model.GetValue("FExecutorId") as DynamicObject)?["Id"].ToString() ?? "0";
            if (!executorId.Equals(this.Context.UserId.ToString()))
            {
                this.View.ShowErrMessage("您不是任务的承办人！");
                return;
            }

            string id = this.Model.DataObject["Id"].ToString();

            BillShowParameter param = new BillShowParameter();
            param.FormId = "ora_PMT_DelvReport";
            param.OpenStyle.ShowType = ShowType.Modal;
            param.ParentPageId = this.View.PageId;
            param.CustomParams.Add("fid", id);
            this.View.ShowForm(param, (result) =>
            {
                if (result.ReturnData == null) return;
                string fid = result.ReturnData.ToString();
                string sql = $"select FBillNo,FCreatorId,FCreateDate,FBackPmt,FStage,FDetial from ora_PMT_DelvReport where FID={fid}";
                var item = DBUtils.ExecuteDynamicObject(Context, sql).FirstOrDefault();
                if (item == null) return;
                int cnt = this.Model.GetEntryRowCount("FEntity");
                this.Model.CreateNewEntryRow("FEntity");
                this.Model.SetValue("FRptDate", item["FCreateDate"], cnt);
                this.Model.SetValue("FReportorId", item["FCreatorId"], cnt);
                this.Model.SetValue("FProgress", item["FDetial"], cnt);
                this.Model.SetValue("FEBackPmt", item["FBackPmt"], cnt);
                this.Model.SetValue("FEStage", item["FStage"], cnt);
                this.Model.SetValue("FReportNo", item["FBillNo"], cnt);

                this.Model.SetValue("FStage", item["FStage"], cnt);
                // 保存单据
                IOperationResult saveResult = BusinessDataServiceHelper.Save(
                    this.Context,
                    this.View.BillBusinessInfo,
                    this.View.Model.DataObject
                );
                this.View.Refresh();
            });
        }
    }
}
