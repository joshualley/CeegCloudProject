using Kingdee.BOS.App.Data;
using Kingdee.BOS.Core.Bill;
using Kingdee.BOS.Core.Bill.PlugIn;
using Kingdee.BOS.Core.Metadata;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.Util;
using System;
using System.ComponentModel;


namespace CZ.CEEG.BosPmt.PmtDeliver
{
    [HotUpdate]
    [Description("货款移交单")]
    public class CZ_CEEG_BosPmt_PmtDeliver : AbstractBillPlugIn
    {
        public override void AfterBindData(EventArgs e)
        {
            base.AfterBindData(e);
            
            string FDocumentStatus = this.View.Model.GetValue("FDocumentStatus").ToString();
            if (FDocumentStatus == "Z")
            {
                string FOrderNo = this.View.OpenParameter.GetCustomParameter("FOrderNo") == null ? "" : this.View.OpenParameter.GetCustomParameter("FOrderNo").ToString();
                string FSerialNum = this.View.OpenParameter.GetCustomParameter("FSerialNum") == null ? "" : this.View.OpenParameter.GetCustomParameter("FSerialNum").ToString();
                string FSellerID = this.View.OpenParameter.GetCustomParameter("FSellerID") == null ? "0" : this.View.OpenParameter.GetCustomParameter("FSellerID").ToString();
                string FDeptID = this.View.OpenParameter.GetCustomParameter("FDeptID") == null ? "0" : this.View.OpenParameter.GetCustomParameter("FDeptID").ToString();
                string FDelvPmt = this.View.OpenParameter.GetCustomParameter("FDelvPmt") == null ? "0" : this.View.OpenParameter.GetCustomParameter("FDelvPmt").ToString();
                this.View.Model.SetValue("FOrderNo", FOrderNo);
                this.View.Model.SetValue("FSellerID", FSellerID);
                this.View.Model.SetValue("FDeptID", FDeptID);
                this.View.Model.SetValue("FSerialNum", FSerialNum);
                this.View.Model.SetValue("FDeliverAmt", FDelvPmt);
            }

        }

    }
}
