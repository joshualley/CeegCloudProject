using Kingdee.BOS;
using Kingdee.BOS.App.Data;
using Kingdee.BOS.Core.Bill;
using Kingdee.BOS.Core.DynamicForm;
using Kingdee.BOS.Core.DynamicForm.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Core.DynamicForm.PlugIn.ControlModel;
using Kingdee.BOS.Core.Metadata;
using Kingdee.BOS.Core.Metadata.ControlElement;
using Kingdee.BOS.Core.Metadata.EntityElement;
using Kingdee.BOS.Core.Metadata.FieldElement;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.Util;
using System;
using System.ComponentModel;
using System.Data;

namespace CZ.CEEG.Report.AccountQueryCond
{
    [Description("查看凭证明细")]
    [HotUpdate]
    public class CZ_CEEG_Report_VounterDetail : AbstractDynamicFormPlugIn
    {
        public override void AfterBindData(EventArgs e)
        {
            base.AfterBindData(e);
            string FSDate = this.View.OpenParameter.GetCustomParameter("FSDate") == null ? "" :
                this.View.OpenParameter.GetCustomParameter("FSDate").ToString();
            string FEDate = this.View.OpenParameter.GetCustomParameter("FEDate") == null ? "" :
                this.View.OpenParameter.GetCustomParameter("FEDate").ToString();
            string FOrgId = this.View.OpenParameter.GetCustomParameter("FOrgId") == null ? "0" :
                this.View.OpenParameter.GetCustomParameter("FOrgId").ToString();
            string FAccountId = this.View.OpenParameter.GetCustomParameter("FAccountId") == null ? "0" :
                this.View.OpenParameter.GetCustomParameter("FAccountId").ToString();
            string FDeptName = this.View.OpenParameter.GetCustomParameter("FDeptName") == null ? "" :
                this.View.OpenParameter.GetCustomParameter("FDeptName").ToString();
            string FCostItemId = this.View.OpenParameter.GetCustomParameter("FCostItemId") == null ? "0" :
                this.View.OpenParameter.GetCustomParameter("FCostItemId").ToString();

            string sql = string.Format(@"EXEC proc_czly_AccountVocunter @SDt='{0}', @EDt='{1}', 
@FOrgId='{2}', @FAccountId='{3}', @FDeptName='{4}', @FCostItemId='{5}'", 
            FSDate, FEDate, FOrgId, FAccountId, FDeptName, FCostItemId);
            
            var objs = DBUtils.ExecuteDynamicObject(this.Context, sql);

            this.View.Model.BatchCreateNewEntryRow("FEntity", objs.Count);
            for (int i = 0; i < objs.Count; i++)
            {
                this.View.Model.SetValue("FVoucherID", objs[i]["FVOUCHERID"], i);
                this.View.Model.SetValue("FVounterNo", objs[i]["FBillNO"], i);
                this.View.Model.SetValue("FVOUCHERGROUPNO", objs[i]["FVOUCHERGROUPNO"], i);
                this.View.Model.SetValue("FDate", objs[i]["FDate"], i);
                this.View.Model.SetValue("FEXPLANATION", objs[i]["FEXPLANATION"], i);
                this.View.Model.SetValue("FACCOUNTID", objs[i]["FACCOUNTID"], i);
                this.View.Model.SetValue("FDetailID", objs[i]["FDetailID"], i);
                this.View.Model.SetValue("FDEBIT", objs[i]["FDEBIT"], i);
                this.View.Model.SetValue("FCREDIT", objs[i]["FCREDIT"], i);
                this.View.Model.SetValue("FAMOUNTFOR", objs[i]["FAMOUNTFOR"], i);
                this.View.Model.SetValue("FEntrySeq", objs[i]["FEntrySeq"], i);
                this.View.Model.SetValue("FRealCostItem", objs[i]["FCostItem"], i);
            }
            this.View.UpdateView("FEntity");

        }


        public override void EntityRowDoubleClick(EntityRowClickEventArgs e)
        {
            string FVoucherID = this.View.Model.GetValue("FVoucherID", e.Row).ToString();
            BillShowParameter param = new BillShowParameter();
            param.ParentPageId = this.View.PageId;
            param.FormId = "GL_VOUCHER";
            param.PKey = FVoucherID;
            param.OpenStyle.ShowType = ShowType.Modal;
            this.View.ShowForm(param);
        }

    }
}
