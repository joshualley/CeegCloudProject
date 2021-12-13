using System;
using System.ComponentModel;
using Kingdee.BOS.App.Data;
using Kingdee.BOS.Core.Bill;
using Kingdee.BOS.Core.Bill.PlugIn;
using Kingdee.BOS.Util;

namespace CZ.CEEG.BosPmt.PmtSummary.TenderBond
{
    [Description("中标单")]
    [HotUpdate]
    public class BinddingForm: AbstractBillPlugIn
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
        }
    }
}