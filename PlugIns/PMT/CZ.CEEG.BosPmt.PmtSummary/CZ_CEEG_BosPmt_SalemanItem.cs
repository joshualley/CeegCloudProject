using Kingdee.BOS.App.Data;
using Kingdee.BOS.Core;
using Kingdee.BOS.Core.Bill;
using Kingdee.BOS.Core.Bill.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Core.Metadata;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.Util;
using System;
using System.ComponentModel;

namespace CZ.CEEG.BosPmt.SalemanItem
{
    [HotUpdate]
    [Description("销售员货款明细")]
    public class CZ_CEEG_BosPmt_SalemanItem : AbstractDynamicFormPlugIn
    {
        public override void AfterBindData(EventArgs e)
        {
            base.AfterBindData(e);

            string FSDate = this.View.OpenParameter.GetCustomParameter("FSDate") == null ? "" : this.View.OpenParameter.GetCustomParameter("FSDate").ToString();
            string FEDate = this.View.OpenParameter.GetCustomParameter("FEDate") == null ? "" : this.View.OpenParameter.GetCustomParameter("FEDate").ToString();
            string FSellerID = this.View.OpenParameter.GetCustomParameter("FSellerID") == null ? "0" : this.View.OpenParameter.GetCustomParameter("FSellerID").ToString();
            string FQDeptId = this.View.OpenParameter.GetCustomParameter("FQDeptId") == null ? "0" : this.View.OpenParameter.GetCustomParameter("FQDeptId").ToString();
            string FQSalerId = this.View.OpenParameter.GetCustomParameter("FQSalerId") == null ? "0" : this.View.OpenParameter.GetCustomParameter("FQSalerId").ToString();
            string FQCustId = this.View.OpenParameter.GetCustomParameter("FQCustId") == null ? "0" : this.View.OpenParameter.GetCustomParameter("FQCustId").ToString();
            string FQFactoryId = this.View.OpenParameter.GetCustomParameter("FQFactoryId") == null ? "0" : this.View.OpenParameter.GetCustomParameter("FQFactoryId").ToString();
            string FQOrderNo = this.View.OpenParameter.GetCustomParameter("FQOrderNo") == null ? "" : this.View.OpenParameter.GetCustomParameter("FQOrderNo").ToString();

            string formId = this.View.GetFormId();
            string sql = string.Format(@"EXEC proc_czly_GetPmt @FormId='{0}', @sDt='{1}', @eDt='{2}', @FSellerID='{3}',
@FQDeptId={4}, @FQSalerId={5}, @FQCustId={6}, @FQFactoryId={7}, @FQOrderNo='{8}'",
                formId, FSDate, FEDate, FSellerID, FQDeptId, FQSalerId, FQCustId, FQFactoryId, FQOrderNo);
            var objs = DBUtils.ExecuteDynamicObject(this.Context, sql);
            Act_QueryPmt(objs);
        }


        #region Actions
        private void Act_QueryPmt(DynamicObjectCollection objs)
        {
           
            for (int i = 0; i < objs.Count; i++)
            {
                this.View.Model.CreateNewEntryRow("FEntity");
                this.View.Model.SetValue("FSellerID", objs[i]["FSellerID"].ToString(), i);
                this.View.Model.SetValue("FOrderNo", objs[i]["FOrderNo"].ToString(), i);
                this.View.Model.SetValue("FOuterPmt", objs[i]["FOuterPmt"].ToString(), i);
                this.View.Model.SetValue("FNormOverduePmt", objs[i]["FNormOverduePmt"].ToString(), i);
                this.View.Model.SetValue("FNormUnoverduePmt", objs[i]["FNormUnoverduePmt"].ToString(), i);
                this.View.Model.SetValue("FExceedePmt", objs[i]["FExceedePmt"].ToString(), i);
                this.View.Model.SetValue("FOverduePmt", objs[i]["FOverduePmt"].ToString(), i);
                this.View.Model.SetValue("FOverdueWarranty", objs[i]["FOverdueWarranty"].ToString(), i);
                this.View.Model.SetValue("FUnoverdueWarranty", objs[i]["FUnoverdueWarranty"].ToString(), i);
                this.View.Model.SetValue("FWarranty", objs[i]["FWarranty"].ToString(), i);
            }
            this.View.UpdateView("FEntity");
        }

        #endregion
    }
}
