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

namespace CZ.CEEG.BosPmt.DiffKindsPmt
{
    [HotUpdate]
    [Description("各类别货款")]
    public class CZ_CEEG_BosPmt_PmtDepartment : AbstractDynamicFormPlugIn
    {
        public override void AfterBindData(EventArgs e)
        {
            base.AfterBindData(e);
            DateTime currDt = DateTime.Now;
            string sDt = currDt.Year.ToString() + "-" + currDt.Month.ToString() + "-01";
            string eDt = currDt.ToString();
            this.View.Model.SetValue("FSDate", sDt);
            this.View.UpdateView("FSDate");
            this.View.Model.SetValue("FEDate", eDt);
            this.View.UpdateView("FEDate");
            Act_QueryPmt(sDt, eDt);
        }

        public override void AfterButtonClick(AfterButtonClickEventArgs e)
        {
            base.AfterButtonClick(e);
            string key = e.Key.ToUpperInvariant();
            switch (key)
            {
                case "FQUERYBTN":
                    string sDt = this.View.Model.GetValue("FSDate").ToString();
                    string eDt = this.View.Model.GetValue("FEDate").ToString();
                    Act_QueryPmt(sDt, eDt);
                    break;
            }
        }


        #region Actions
        private void Act_QueryPmt(string sDt, string eDt)
        {
            string formId = this.View.GetFormId();
            string sql = string.Format("EXEC proc_czly_GetPmt @FormId='{0}', @sDt='{1}', @eDt='{2}'",
                formId, sDt, eDt);
            var objs = DBUtils.ExecuteDynamicObject(this.Context, sql);
            Act_CreateEntry(objs);
        }

        private void Act_CreateEntry(DynamicObjectCollection objs)
        {
            this.View.Model.DeleteEntryData("FEntity");
            string formId = this.View.GetFormId();
            switch (formId)
            {
                case "ora_PMT_OfficePmt": //办事处
                    for (int i = 0; i < objs.Count; i++)
                    {
                        this.View.Model.CreateNewEntryRow("FEntity");
                        this.View.Model.SetValue("FDeptID", objs[i]["FDeptID"].ToString(), i);
                        this.View.Model.SetValue("FOuterPmt", objs[i]["FOuterPmt"].ToString(), i);
                        this.View.Model.SetValue("FRatioForTAmt", objs[i]["FRatioForTAmt"].ToString(), i);
                        this.View.Model.SetValue("FTOverduePmt", objs[i]["FTOverduePmt"].ToString(), i);
                        this.View.Model.SetValue("FRatioForTPmt", objs[i]["FRatioForTPmt"].ToString(), i);
                        this.View.Model.SetValue("FRatioForTExceedePmt", objs[i]["FRatioForTExceedePmt"].ToString(), i);
                        this.View.Model.SetValue("FTUnoverduePmt", objs[i]["FTUnoverduePmt"].ToString(), i);
                        this.View.Model.SetValue("FWarranty", objs[i]["FWarranty"].ToString(), i);
                    }
                    break;
                case "ora_PMT_CustomerPmt": //客户
                    for (int i = 0; i < objs.Count; i++)
                    {
                        this.View.Model.CreateNewEntryRow("FEntity");
                        this.View.Model.SetValue("FDeptID", objs[i]["FDeptID"].ToString(), i);
                        this.View.Model.SetValue("FCustID", objs[i]["FCustID"].ToString(), i);
                        this.View.Model.SetValue("FOuterPmt", objs[i]["FOuterPmt"].ToString(), i);
                        this.View.Model.SetValue("FRatioForTAmt", objs[i]["FRatioForTAmt"].ToString(), i);
                        this.View.Model.SetValue("FTOverduePmt", objs[i]["FTOverduePmt"].ToString(), i);
                        this.View.Model.SetValue("FRatioForTPmt", objs[i]["FRatioForTPmt"].ToString(), i);
                        this.View.Model.SetValue("FRatioForTExceedePmt", objs[i]["FRatioForTExceedePmt"].ToString(), i);
                    }
                    break;
                case "ora_PMT_SalesmanPmt": //销售员
                    for (int i = 0; i < objs.Count; i++)
                    {
                        this.View.Model.CreateNewEntryRow("FEntity");
                        this.View.Model.SetValue("FSellerID", objs[i]["FSellerID"].ToString(), i);
                        this.View.Model.SetValue("FOuterPmt", objs[i]["FOuterPmt"].ToString(), i);
                        this.View.Model.SetValue("FNormOverduePmt", objs[i]["FNormOverduePmt"].ToString(), i);
                        this.View.Model.SetValue("FNormUnoverduePmt", objs[i]["FNormUnoverduePmt"].ToString(), i);
                        this.View.Model.SetValue("FTExceedePmt", objs[i]["FTExceedePmt"].ToString(), i);
                        this.View.Model.SetValue("FTOverduePmt", objs[i]["FTOverduePmt"].ToString(), i);
                        this.View.Model.SetValue("FOverdueWarranty", objs[i]["FOverdueWarranty"].ToString(), i);
                        this.View.Model.SetValue("FUnoverdueWarranty", objs[i]["FUnoverdueWarranty"].ToString(), i);
                        this.View.Model.SetValue("FWarranty", objs[i]["FWarranty"].ToString(), i);
                    }
                    break;
                case "ora_PMT_FactoryPmt": //子公司
                    for (int i = 0; i < objs.Count; i++)
                    {
                        this.View.Model.CreateNewEntryRow("FEntity");
                        this.View.Model.SetValue("FFactoryID", objs[i]["FFactoryID"].ToString(), i);
                        this.View.Model.SetValue("FOuterPmt", objs[i]["FOuterPmt"].ToString(), i);
                        this.View.Model.SetValue("FNormOverduePmt", objs[i]["FNormOverduePmt"].ToString(), i);
                        //this.View.Model.SetValue("FNormUnoverduePmt", objs[i]["FNormUnoverduePmt"].ToString(), i);
                        this.View.Model.SetValue("FTExceedePmt", objs[i]["FTExceedePmt"].ToString(), i);
                        this.View.Model.SetValue("FTOverduePmt", objs[i]["FTOverduePmt"].ToString(), i);
                        this.View.Model.SetValue("FWarranty", objs[i]["FWarranty"].ToString(), i);
                    }
                    break;
                case "ora_PMT_FactoryCustPmt": //子公司客户
                    for (int i = 0; i < objs.Count; i++)
                    {
                        this.View.Model.CreateNewEntryRow("FEntity");
                        this.View.Model.SetValue("FFactoryID", objs[i]["FFactoryID"].ToString(), i);
                        this.View.Model.SetValue("FCustID", objs[i]["FCustID"].ToString(), i);
                        this.View.Model.SetValue("FOuterPmt", objs[i]["FOuterPmt"].ToString(), i);
                        this.View.Model.SetValue("FNormOverduePmt", objs[i]["FNormOverduePmt"].ToString(), i);
                        this.View.Model.SetValue("FNormUnoverduePmt", objs[i]["FNormUnoverduePmt"].ToString(), i);
                        this.View.Model.SetValue("FTExceedePmt", objs[i]["FTExceedePmt"].ToString(), i);
                        this.View.Model.SetValue("FTOverduePmt", objs[i]["FTOverduePmt"].ToString(), i);
                        this.View.Model.SetValue("FOverdueWarranty", objs[i]["FOverdueWarranty"].ToString(), i);
                        this.View.Model.SetValue("FUnoverdueWarranty", objs[i]["FUnoverdueWarranty"].ToString(), i);
                        this.View.Model.SetValue("FWarranty", objs[i]["FWarranty"].ToString(), i);
                    }
                    break;
            }
            this.View.UpdateView("FEntity");
        }

        #endregion
    }
}
