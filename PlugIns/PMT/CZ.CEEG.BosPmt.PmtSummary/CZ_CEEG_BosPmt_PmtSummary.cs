using Kingdee.BOS.App.Data;
using Kingdee.BOS.Core;
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
using System.Threading.Tasks;

namespace CZ.CEEG.BosPmt.PmtSummary
{
    [HotUpdate]
    [Description("货款汇总报表")]
    public class CZ_CEEG_BosPmt_PmtSummary : AbstractDynamicFormPlugIn
    {

        #region Overrides
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

            this.Model.SetValue("FSDate", sDt);
            this.View.UpdateView("FSDate");
            this.Model.SetValue("FEDate", eDt);
            this.View.UpdateView("FEDate");
            Act_QuerySummaryData();
        }

        public override void AfterButtonClick(AfterButtonClickEventArgs e)
        {
            base.AfterButtonClick(e);
            string key = e.Key.ToUpperInvariant();
            switch (key)
            {
                case "FQUERYBTN":
                    Act_QuerySummaryData();
                    break;
            }
        }

        public override void EntityRowDoubleClick(EntityRowClickEventArgs e)
        {
            base.EntityRowDoubleClick(e);
            string key = e.ColKey.ToUpperInvariant();
            switch (key)
            {
                case "FORDERNO":
                    Act_ShowDeliverForm(e.Row);
                    break;
            }
        }

        public override void AfterEntryBarItemClick(AfterBarItemClickEventArgs e)
        {
            base.AfterEntryBarItemClick(e);
            string key = e.BarItemKey.ToUpperInvariant();
            switch (key)
            {
                case "ORA_TBDELV": //ora_tbDelv
                    DynamicObject rowData;
                    int rowIndex = -1;
                    this.Model.TryGetEntryCurrentRow("FEntity", out rowData, out rowIndex);
                    if (rowIndex == -1)
                    {
                        this.View.ShowWarnningMessage("未选中明细表中的行！");
                        return;
                    }
                    Act_ShowDeliverForm(rowIndex);
                    break;
            }
        }

        #endregion

        #region Actions

        /// <summary>
        /// 货款移交
        /// </summary>
        private void Act_ShowDeliverForm(int Row)
        {
            var para = new BillShowParameter();
            para.FormId = "ora_PMT_Deliver";
            para.OpenStyle.ShowType = ShowType.Modal;
            para.ParentPageId = this.View.PageId;

            para.Status = OperationStatus.ADDNEW;

            string FDeliverNote = this.Model.GetValue("FDeliverNote", Row)?.ToString() ?? "";
            if (!FDeliverNote.IsNullOrEmptyOrWhiteSpace())
            {
                this.View.ShowMessage("订单已移交处理，请勿重复操作！");
                return;
            }
            string FOrderNo = this.Model.GetValue("FOrderNo", Row).ToString();
            string FSerialNum = this.Model.GetValue("FSerialNum", Row).ToString();
            string FSignOrgID = (this.Model.GetValue("FSignOrgID", Row) as DynamicObject)?["Id"].ToString() ?? "0";
            string FCustID = (this.Model.GetValue("FCustID", Row) as DynamicObject)?["Id"].ToString() ?? "0";
            string FSellerID = (this.Model.GetValue("FSellerID", Row) as DynamicObject)?["Id"].ToString() ?? "0";
            string FDeptID = (this.Model.GetValue("FDeptID", Row) as DynamicObject)?["Id"].ToString() ?? "0";
            string FDelvPmt = this.Model.GetValue("FOuterPmt", Row).ToString();
            string FOrderAmt = this.Model.GetValue("FTOrderAmt", Row).ToString();
            para.CustomParams.Add("FOrderNo", FOrderNo);
            para.CustomParams.Add("FSignOrgID", FSignOrgID);
            para.CustomParams.Add("FCustID", FCustID);
            para.CustomParams.Add("FSellerID", FSellerID);
            para.CustomParams.Add("FDeptID", FDeptID);
            para.CustomParams.Add("FSerialNum", FSerialNum);
            para.CustomParams.Add("FDelvPmt", FDelvPmt);
            para.CustomParams.Add("FOrderAmt", FOrderAmt);

            this.View.ShowForm(para);
        }

        /// <summary>
        /// 查询货款汇总数据
        /// </summary>
        private void Act_QuerySummaryData()
        {
            string formid = this.View.GetFormId();
            string FSDate = this.Model.GetValue("FSDate") == null ? "" : this.Model.GetValue("FSDate").ToString();
            string FEDate = this.Model.GetValue("FEDate") == null ? "" : this.Model.GetValue("FEDate").ToString();

            string FQDeptId = this.Model.GetValue("FQDeptId") == null ? "0" : (this.Model.GetValue("FQDeptId") as DynamicObject)["Id"].ToString();

            string deptSql = "0";
            DynamicObjectCollection deptIds = this.Model.GetValue("FQDeptIds") as DynamicObjectCollection;
            if (deptIds.Count > 0)
            {
                deptSql = string.Join(",", deptIds.Select(d => d["FQDeptIds_Id"].ToString()));
            }


            DynamicObjectCollection regionIds = this.Model.GetValue("FRegionIds") as DynamicObjectCollection;

            string FQSalerId = this.Model.GetValue("FQSalerId") == null ? "0" : (this.Model.GetValue("FQSalerId") as DynamicObject)["Id"].ToString();
            string FQCustId = this.Model.GetValue("FQCustId") == null ? "0" : (this.Model.GetValue("FQCustId") as DynamicObject)["Id"].ToString();
            string FQFactoryId = this.Model.GetValue("FQFactoryId") == null ? "0" : (this.Model.GetValue("FQFactoryId") as DynamicObject)["Id"].ToString();
            string FQOrderNo = this.Model.GetValue("FQOrderNo") == null ? "" : this.Model.GetValue("FQOrderNo").ToString().Trim();


            string sql = string.Format(@"exec proc_czly_GetPmt @FormId='{0}', @SDt='{1}', @EDt='{2}', 
@FQDeptId='{3}', @FQSalerId={4}, @FQCustId={5}, @FQFactoryId='{6}', @FQOrderNo='{7}'",
            formid, FSDate, FEDate, deptSql, FQSalerId, FQCustId, FQFactoryId, FQOrderNo);
            var objsDB = DBUtils.ExecuteDynamicObject(this.Context, sql);
            this.Model.DeleteEntryData("FEntity");
            if (objsDB.Count <= 0)
            {
                return;
            }
            string FIsOldSysOrder;
            

            List<DynamicObject> objs = objsDB.ToList();

            //过滤大区
            if (regionIds.Count > 0)
            {
                objs = objs.Where(o => regionIds.Any(r =>
                {

                    DynamicObject dept = r["FRegionIds"] as DynamicObject;

                    return o["FDeptNumber"].ToString().StartsWith(dept["Number"].ToString());

                }
                )).ToList();
            }

            this.Model.BatchCreateNewEntryRow("FEntity", objs.Count);

            for (int i = 0; i < objs.Count; i++)
            {
                //this.Model.CreateNewEntryRow("FEntity");
                this.Model.SetValue("FOrderNo", objs[i]["FOrderNo"].ToString(), i);
                this.Model.SetValue("FSerialNum", objs[i]["FSerialNum"].ToString(), i);
                this.Model.SetValue("FSaleOrgID", objs[i]["FSaleOrgID"].ToString(), i);
                this.View.Model.SetValue("FSignOrgID", objs[i]["FSignOrgID"].ToString(), i);
                this.Model.SetValue("FSellerID", objs[i]["FSellerID"].ToString(), i);
                this.Model.SetValue("FDeptID", objs[i]["FDeptID"].ToString(), i);
                this.Model.SetValue("FOrgID", objs[i]["FOrgID"].ToString(), i);
                this.Model.SetValue("FCustID", objs[i]["FCustID"].ToString(), i);
                this.Model.SetValue("FTOrderAmt", objs[i]["FTOrderAmt"].ToString(), i);
                this.Model.SetValue("FPayWay", objs[i]["FPayWay"].ToString(), i);
                string dt = objs[i]["FLaterDelvGoodsDt"].ToString().Split(' ')[0] == "1900-01-01" ? "" : objs[i]["FLaterDelvGoodsDt"].ToString();
                this.Model.SetValue("FLaterDelvGoodsDt", dt, i);
                this.Model.SetValue("FTDeliverAmt", objs[i]["FTDeliverAmt"].ToString(), i);
                this.Model.SetValue("FTReceiverAmt", objs[i]["FTReceiverAmt"].ToString(), i);
                this.Model.SetValue("FTInvoiceAmt", objs[i]["FTInvoiceAmt"].ToString(), i);
                this.Model.SetValue("FOuterPmt", objs[i]["FOuterPmt"].ToString(), i);
                this.Model.SetValue("FOuterPmtAll",
                    decimal.Parse(objs[i]["FTDeliverAmt"].ToString()) -
                    decimal.Parse(objs[i]["FTReceiverAmt"].ToString()), i);
                this.Model.SetValue("FNormOverduePmt", objs[i]["FNormOverduePmt"].ToString(), i);
                this.Model.SetValue("FNormUnoverduePmt", objs[i]["FNormUnoverduePmt"].ToString(), i);
                this.Model.SetValue("FOverduePmt", objs[i]["FOverduePmt"].ToString(), i);
                this.Model.SetValue("FTOverduePmt", objs[i]["FTOverduePmt"].ToString(), i);
                this.Model.SetValue("FTUnoverduePmt", objs[i]["FTUnoverduePmt"].ToString(), i);
                this.Model.SetValue("FTExceedePmt", objs[i]["FTExceedePmt"].ToString(), i);
                this.Model.SetValue("FOverdueWarranty", objs[i]["FOverdueWarranty"].ToString(), i);
                this.Model.SetValue("FUnoverdueWarranty", objs[i]["FUnoverdueWarranty"].ToString(), i);
                this.Model.SetValue("FTWarranty", objs[i]["FTWarranty"].ToString(), i);
                this.Model.SetValue("FIntervalMonth", objs[i]["FIntervalMonth"].ToString(), i);
                this.Model.SetValue("FIntervalDay", objs[i]["FIntervalDay"].ToString(), i);
                this.Model.SetValue("FRemarks", objs[i]["FRemark"].ToString(), i);
                FIsOldSysOrder = objs[i]["FOrderNo"].ToString().StartsWith("XSDD") ? "否" : "是";
                this.Model.SetValue("FIsOldSysOrder", FIsOldSysOrder, i);
                this.Model.SetValue("FDeliverNote", objs[i]["FDeliverNote"].ToString(), i);
                this.Model.SetValue("FDelvPmt", objs[i]["FDelvPmt"].ToString(), i);
            }
            this.View.UpdateView("FEntity");
        }

        #endregion

    }
}
