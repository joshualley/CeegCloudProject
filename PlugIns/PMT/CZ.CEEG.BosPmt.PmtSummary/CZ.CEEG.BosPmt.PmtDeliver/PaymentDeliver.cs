using Kingdee.BOS.App.Data;
using Kingdee.BOS.Core.Bill;
using Kingdee.BOS.Core.Bill.PlugIn;
using Kingdee.BOS.Core.DynamicForm;
using Kingdee.BOS.Core.DynamicForm.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Core.Metadata;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.Util;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace CZ.CEEG.BosPmt.PmtDeliver
{
    [HotUpdate]
    [Description("货款移交单")]
    class PaymentDeliver : AbstractBillPlugIn
    {
        public override void AfterBindData(EventArgs e)
        {
            base.AfterBindData(e);
            string FDocumentStatus = this.Model.GetValue("FDocumentStatus").ToString();
            if (FDocumentStatus != "Z")
                return;
            string FOrderNo = this.View.OpenParameter.GetCustomParameter("FOrderNo") == null ? "" :
                this.View.OpenParameter.GetCustomParameter("FOrderNo").ToString();
            string FSellerID = this.View.OpenParameter.GetCustomParameter("FSellerID") == null ? "" :
                this.View.OpenParameter.GetCustomParameter("FSellerID").ToString();
            string FDeptID = this.View.OpenParameter.GetCustomParameter("FOrderNo") == null ? "" :
                this.View.OpenParameter.GetCustomParameter("FDeptID").ToString();
            this.Model.SetValue("FOrderNo", FOrderNo);
            this.Model.SetValue("FSellerID", FSellerID);
            this.Model.SetValue("FDeptID", FDeptID);
        }
    }
}
