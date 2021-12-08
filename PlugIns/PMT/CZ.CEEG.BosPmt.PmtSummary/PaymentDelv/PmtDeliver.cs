using Kingdee.BOS.App.Data;
using Kingdee.BOS.Core.Bill;
using Kingdee.BOS.Core.Bill.PlugIn;
using Kingdee.BOS.Core.Metadata;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.Util;
using System;
using System.ComponentModel;
using System.Linq;

namespace CZ.CEEG.BosPmt.PmtSummary.PaymentDelv
{
    [HotUpdate]
    [Description("货款移交单")]
    public class PmtDeliver : AbstractBillPlugIn
    {
        public override void AfterBindData(EventArgs e)
        {
            base.AfterBindData(e);
            
            string FDocumentStatus = this.View.Model.GetValue("FDocumentStatus").ToString();
            if (FDocumentStatus == "Z")
            {
                string FOrderNo = this.View.OpenParameter.GetCustomParameter("FOrderNo")?.ToString() ?? "";
                string FSerialNum = this.View.OpenParameter.GetCustomParameter("FSerialNum")?.ToString() ?? "";
                string FSignOrgID = this.View.OpenParameter.GetCustomParameter("FSignOrgID")?.ToString() ?? "0";
                string FCustID = this.View.OpenParameter.GetCustomParameter("FCustID")?.ToString() ?? "0";
                string FSellerID = this.View.OpenParameter.GetCustomParameter("FSellerID")?.ToString() ?? "0";
                string FDeptID = this.View.OpenParameter.GetCustomParameter("FDeptID")?.ToString() ?? "0";
                string FDelvPmt = this.View.OpenParameter.GetCustomParameter("FDelvPmt")?.ToString() ?? "0";
                string FOrderAmt = this.View.OpenParameter.GetCustomParameter("FOrderAmt")?.ToString() ?? "0";
                this.View.Model.SetValue("FOrderNo", FOrderNo);
                this.View.Model.SetValue("FSignOrgId", FSignOrgID);
                this.View.Model.SetValue("FCustID", FCustID);
                this.View.Model.SetValue("FSellerID", FSellerID);
                this.View.Model.SetValue("FDeptID", FDeptID);
                this.View.Model.SetValue("FSerialNum", FSerialNum);
                this.View.Model.SetValue("FDeliverAmt", FDelvPmt);
                this.View.Model.SetValue("FOrderAmt", FOrderAmt);

                SetAudit();
            }

            string FDeliverType = this.Model.GetValue("FDeliverType")?.ToString() ?? "";
            if (!FDeliverType.Equals("4"))
            {
                this.View.GetControl("FBackReason").Visible = false;
            }

            

        }


        /// <summary>
        /// 设置审核人，直接领导岗位、单位总经理
        /// </summary>
        private void SetAudit()
        {
            string userId = this.Context.UserId.ToString();
            string sql = string.Format("exec proc_czty_GetLoginUser2Emp @FUserID='{0}'", userId);
            var item = DBUtils.ExecuteDynamicObject(Context, sql).FirstOrDefault();
            if(item != null)
            {
                this.View.Model.SetValue("FGManager", item["FGManager"].ToString());
            }
        }

    }
}
