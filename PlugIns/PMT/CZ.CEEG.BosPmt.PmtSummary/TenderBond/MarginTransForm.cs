using System;
using System.ComponentModel;
using Kingdee.BOS.App.Data;
using Kingdee.BOS.Core.Bill;
using Kingdee.BOS.Core.Bill.PlugIn;
using Kingdee.BOS.Core.DynamicForm;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Util;

namespace CZ.CEEG.BosPmt.PmtSummary.TenderBond
{
    [Description("保证金转移单")]
    [HotUpdate]
    public class MarginTransForm: AbstractBillPlugIn
    {
        public override void AfterBindData(EventArgs e)
        {
            base.AfterBindData(e);
            string status = this.Model.GetValue("FDocumentStatus").ToString();
            if (status != "Z") return;

            string FPubFundID = this.View.OpenParameter.GetCustomParameter("FPubFundID")?.ToString() ?? "0";
            string FPubFundNo = this.View.OpenParameter.GetCustomParameter("FPubFundNo")?.ToString() ?? "";
            string FPrjName = this.View.OpenParameter.GetCustomParameter("FPrjName")?.ToString() ?? "";
            string FMargin = this.View.OpenParameter.GetCustomParameter("FMargin")?.ToString() ?? "0";

            this.Model.SetValue("FPubFundID", FPubFundID);
            this.Model.SetValue("FPubFundNo", FPubFundNo);
            this.Model.SetValue("FPrjName", FPrjName);
            this.Model.SetValue("FMargin", FMargin);

            this.View.GetMainBarItem("ora_tbCancel").Visible = false;
        }

        public override void AfterBarItemClick(AfterBarItemClickEventArgs e)
        {
            base.AfterBarItemClick(e);
            switch (e.BarItemKey.ToUpperInvariant())
            {
                case "ORA_TBCANCEL": // ora_tbCancel 撤回
                    Act_Cancel();
                    break;
            }
        }

        private void Act_Cancel()
        {
            string status = this.Model.GetValue("FDocumentStatus").ToString();
            if (status == "Z") return;

            string fid = this.Model.DataObject["Id"].ToString();

            this.View.ShowWarnningMessage("确定要撤回本单吗？", "确定要撤回本单吗？", MessageBoxOptions.OK, (r) => {
                if (r == MessageBoxResult.OK)
                {
                    string sql = $"update ora_PMT_MarginTrans set FBillStatus='B' where FID={fid}";
                    DBUtils.Execute(Context, sql);
                    this.View.Refresh();
                    this.View.ShowMessage("单据已撤回。");
                }
            });
        }
    }
}