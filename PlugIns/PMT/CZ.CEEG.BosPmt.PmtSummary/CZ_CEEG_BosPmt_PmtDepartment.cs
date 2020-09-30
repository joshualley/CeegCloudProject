using Kingdee.BOS.App.Data;
using Kingdee.BOS.Core;
using Kingdee.BOS.Core.Bill;
using Kingdee.BOS.Core.Bill.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn;
using Kingdee.BOS.Core.DynamicForm;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Core.Metadata;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.Util;
using System;
using System.ComponentModel;
using System.Threading.Tasks;

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
            //设置开始日期为订单最早日期
            string sql = "SELECT TOP 1 FDate FROM T_SAL_ORDER ORDER BY FDate ASC";
            var obj = DBUtils.ExecuteDynamicObject(this.Context, sql);
            if (obj.Count > 0)
            {
                sDt = obj[0]["FDate"].ToString();
            }
            this.View.Model.SetValue("FSDate", sDt);
            this.View.UpdateView("FSDate");
            this.View.Model.SetValue("FEDate", eDt);
            this.View.UpdateView("FEDate");
            Act_QueryPmt();
        }

        public override void AfterButtonClick(AfterButtonClickEventArgs e)
        {
            base.AfterButtonClick(e);
            string key = e.Key.ToUpperInvariant();
            switch (key)
            {
                case "FQUERYBTN":
                    Act_QueryPmt();
                    break;
            }
        }

        /// <summary>
        /// 销售员货款，查看明细
        /// </summary>
        /// <param name="e"></param>
        public override void EntryBarItemClick(BarItemClickEventArgs e)
        {
            base.EntryBarItemClick(e);
            string key = e.BarItemKey.ToUpperInvariant();
            switch(key)
            {
                case "TBALLITEMS": //tbAllItems
                    Act_ShowSellerDetail("0");
                    break;
                case "TBONEITEM": //tbOneItem
                    DynamicObject rowData;
                    int rowIndex;
                    this.Model.TryGetEntryCurrentRow("FEntity", out rowData, out rowIndex);
                    if (rowData != null)
                    {
                        string FSellerID = rowData == null ? "0" : (rowData["FSellerID"] as DynamicObject)["Id"].ToString();
                        Act_ShowSellerDetail(FSellerID);
                    }
                    else
                    {
                        this.View.ShowMessage("未选择有效的单据体行！");
                    }
                        
                    break;
            }
        }

        /// <summary>
        /// 销售员货款，双击行打开明细
        /// </summary>
        /// <param name="e"></param>
        public override void EntityRowDoubleClick(EntityRowClickEventArgs e)
        {
            base.EntityRowDoubleClick(e);
            string formId = this.View.GetFormId();
            if("ora_PMT_SalesmanPmt".Equals(formId))
            {
                string FSellerID = this.View.Model.GetValue("FSellerID", e.Row) == null ? "0" :
                    (this.View.Model.GetValue("FSellerID", e.Row) as DynamicObject)["Id"].ToString();
                Act_ShowSellerDetail(FSellerID);
            }
        }


        #region Actions

        /// <summary>
        /// 显示销售员货款详情
        /// </summary>
        /// <param name="FSellerID">为0值时显示所有的销售员详情</param>
        private void Act_ShowSellerDetail(string FSellerID)
        {
            string FSDate = this.View.Model.GetValue("FSDate") == null ? "" : this.View.Model.GetValue("FSDate").ToString();
            string FEDate = this.View.Model.GetValue("FEDate") == null ? "" : this.View.Model.GetValue("FEDate").ToString();
            string FQDeptId = this.View.Model.GetValue("FQDeptId") == null ? "0" : (this.View.Model.GetValue("FQDeptId") as DynamicObject)["Id"].ToString();
            string FQSalerId = this.View.Model.GetValue("FQSalerId") == null ? "0" : (this.View.Model.GetValue("FQSalerId") as DynamicObject)["Id"].ToString();
            string FQCustId = this.View.Model.GetValue("FQCustId") == null ? "0" : (this.View.Model.GetValue("FQCustId") as DynamicObject)["Id"].ToString();
            string FQFactoryId = this.View.Model.GetValue("FQFactoryId") == null ? "0" : (this.View.Model.GetValue("FQFactoryId") as DynamicObject)["Id"].ToString();
            string FQOrderNo = this.View.Model.GetValue("FQOrderNo") == null ? "" : this.View.Model.GetValue("FQOrderNo").ToString().Trim();
            var para = new DynamicFormShowParameter();
            para.FormId = "ora_PMT_SalesmanItemPmt";
            para.OpenStyle.ShowType = ShowType.Modal;
            para.ParentPageId = this.View.PageId;
            para.CustomParams.Add("FSDate", FSDate);
            para.CustomParams.Add("FEDate", FEDate);
            para.CustomParams.Add("FSellerID", FSellerID);
            para.CustomParams.Add("FQDeptId", FQDeptId);
            para.CustomParams.Add("FQSalerId", FQSalerId);
            para.CustomParams.Add("FQCustId", FQCustId);
            para.CustomParams.Add("FQFactoryId", FQFactoryId);
            para.CustomParams.Add("FQOrderNo", FQOrderNo);
            this.View.ShowForm(para);
        }

        private void Act_QueryPmt()
        {
            string FSDate = this.View.Model.GetValue("FSDate") == null ? "" : this.View.Model.GetValue("FSDate").ToString();
            string FEDate = this.View.Model.GetValue("FEDate") == null ? "" : this.View.Model.GetValue("FEDate").ToString();
            string FQDeptId = this.View.Model.GetValue("FQDeptId") == null ? "0" : (this.View.Model.GetValue("FQDeptId") as DynamicObject)["Id"].ToString();
            string FQSalerId = this.View.Model.GetValue("FQSalerId") == null ? "0" : (this.View.Model.GetValue("FQSalerId") as DynamicObject)["Id"].ToString();
            string FQCustId = this.View.Model.GetValue("FQCustId") == null ? "0" : (this.View.Model.GetValue("FQCustId") as DynamicObject)["Id"].ToString();
            string FQFactoryId = this.View.Model.GetValue("FQFactoryId") == null ? "0" : (this.View.Model.GetValue("FQFactoryId") as DynamicObject)["Id"].ToString();
            string FQOrderNo = this.View.Model.GetValue("FQOrderNo") == null ? "" : this.View.Model.GetValue("FQOrderNo").ToString().Trim();

            string formId = this.View.GetFormId();
            string sql = string.Format(@"EXEC proc_czly_GetPmt @FormId='{0}', @sDt='{1}', @eDt='{2}', 
@FQDeptId={3}, @FQSalerId={4}, @FQCustId={5}, @FQFactoryId={6}, @FQOrderNo='{7}'",
                formId, FSDate, FEDate, FQDeptId, FQSalerId, FQCustId, FQFactoryId, FQOrderNo);
            
            var objs = DBUtils.ExecuteDynamicObject(this.Context, sql);
            Act_CreateEntry(objs);
        }

        private void Act_CreateEntry(DynamicObjectCollection objs)
        {
            this.View.Model.DeleteEntryData("FEntity");
            if (objs.Count <= 0)
            {
                return;
            }
            this.View.Model.BatchCreateNewEntryRow("FEntity", objs.Count);
            string formId = this.View.GetFormId();
            switch (formId)
            {
                case "ora_PMT_OfficePmt": //办事处
                    for(int i = 0; i < objs.Count; i++)
                    {
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
