using Kingdee.BOS.App.Data;
using Kingdee.BOS.Core.Bill.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Util;
using System;
using System.Linq;
using System.ComponentModel;


namespace CZ.CEEG.BosPmt.PmtSummary.PaymentDelv
{
    [HotUpdate]
    [Description("移交任务汇报单表单")]
    public class TaskReportForm : AbstractBillPlugIn
    {
        private bool isReturnData = false;

        public override void AfterBindData(EventArgs e)
        {
            base.AfterBindData(e);
            string fid = this.View.OpenParameter.GetCustomParameter("fid")?.ToString() ?? "";
            if (fid != "")
            {
                string sql = $"select * from ora_PMT_DelvTask where FID={fid}";
                var item = DBUtils.ExecuteDynamicObject(Context, sql).FirstOrDefault();
                if (item == null) return;

                this.Model.SetValue("FSourceBillNo", item["FBillNo"]);
                this.Model.SetValue("FDelvPmt", item["FDelvPmt"]);
                this.Model.SetValue("FCustId", item["FCustId"]);
                this.Model.SetValue("FTaskIntro", item["FTaskIntro"]);
                this.Model.SetValue("FStage", item["FStage"]);
                this.Model.SetValue("FAllocId", item["FCreatorId"]);
            }
        }

        public override void AfterDoOperation(AfterDoOperationEventArgs e)
        {
            base.AfterDoOperation(e);
            switch (e.Operation.Operation.ToUpperInvariant())
            {
                case "SUBMIT":
                    string fid = this.Model.DataObject["Id"].ToString();
                    isReturnData = true;
                    this.View.ReturnToParentWindow(fid);
                    this.View.Close();
                    break;
            }
        }

        public override void BeforeClosed(BeforeClosedEventArgs e)
        {
            base.BeforeClosed(e);
            if (!isReturnData)
            {
                this.View.ReturnToParentWindow(null);
            }
        }
    }
}
