﻿using Kingdee.BOS.App.Data;
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
            if(obj.Count > 0)
            {
                sDt = obj[0]["FDate"].ToString();
            }
           
            this.View.Model.SetValue("FSDate", sDt);
            this.View.UpdateView("FSDate");
            this.View.Model.SetValue("FEDate", eDt);
            this.View.UpdateView("FEDate");
            Act_QuerySummaryData();
            //Act_QueryDetailData();
        }

        public override void AfterButtonClick(AfterButtonClickEventArgs e)
        {
            base.AfterButtonClick(e);
            string key = e.Key.ToUpperInvariant();
            switch (key)
            {
                case "FQUERYBTN":
                    Act_QuerySummaryData();
                    //Act_QueryDetailData();
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

            string FOrderNo = this.Model.GetValue("FOrderNo", Row).ToString();
            string FSerialNum = this.Model.GetValue("FSerialNum", Row).ToString();
            string FSellerID = this.Model.GetValue("FSellerID", Row) == null ? "0" : (this.Model.GetValue("FSellerID") as DynamicObject)["Id"].ToString();
            string FDeptID = this.Model.GetValue("FDeptID", Row) == null ? "0" : (this.Model.GetValue("FDeptID") as DynamicObject)["Id"].ToString();
            para.CustomParams.Add("FOrderNo", FOrderNo);
            para.CustomParams.Add("FSellerID", FSellerID);
            para.CustomParams.Add("FDeptID", FDeptID);
            para.CustomParams.Add("FSerialNum", FSerialNum);

            this.View.ShowForm(para);
        }

        /// <summary>
        /// 查询货款汇总数据
        /// </summary>
        private void Act_QuerySummaryData()
        {
            string formid = this.View.GetFormId();
            string FSDate = this.View.Model.GetValue("FSDate") == null ? "" : this.View.Model.GetValue("FSDate").ToString();
            string FEDate = this.View.Model.GetValue("FEDate") == null ? "" : this.View.Model.GetValue("FEDate").ToString();
            string FQDeptId = this.View.Model.GetValue("FQDeptId") == null ? "0" : (this.View.Model.GetValue("FQDeptId") as DynamicObject)["Id"].ToString();
            string FQSalerId = this.View.Model.GetValue("FQSalerId") == null ? "0" : (this.View.Model.GetValue("FQSalerId") as DynamicObject)["Id"].ToString();
            string FQCustId = this.View.Model.GetValue("FQCustId") == null ? "0" : (this.View.Model.GetValue("FQCustId") as DynamicObject)["Id"].ToString();
            string FQFactoryId = this.View.Model.GetValue("FQFactoryId") == null ? "0" : (this.View.Model.GetValue("FQFactoryId") as DynamicObject)["Id"].ToString();
            string FQOrderNo = this.View.Model.GetValue("FQOrderNo") == null ? "" : this.View.Model.GetValue("FQOrderNo").ToString().Trim();


            string sql = string.Format(@"exec proc_czly_GetPmt @FormId='{0}', @SDt='{1}', @EDt='{2}', 
@FQDeptId={3}, @FQSalerId={4}, @FQCustId={5}, @FQFactoryId='{6}', @FQOrderNo='{7}'",
            formid, FSDate, FEDate, FQDeptId, FQSalerId, FQCustId, FQFactoryId, FQOrderNo);
            var objs = DBUtils.ExecuteDynamicObject(this.Context, sql);
            this.View.Model.DeleteEntryData("FEntity");
            if (objs.Count <= 0)
            {
                return;
            }
            this.View.Model.BatchCreateNewEntryRow("FEntity", objs.Count);
            for (int i = 0; i < objs.Count; i++)
            {
                this.View.Model.SetValue("FOrderNo", objs[i]["FOrderNo"].ToString(), i);
                this.View.Model.SetValue("FSerialNum", objs[i]["FSerialNum"].ToString(), i);
                string[] FStrDirectors = objs[i]["FDirectors"].ToString().Split(',');
                List<long> FDirectors = new List<long>();
                foreach (var d in FStrDirectors)
                {
                    FDirectors.Add(int.Parse(d));
                }
                this.View.Model.SetValue("FDirectors", FDirectors, i);
                this.View.Model.SetValue("FSaleOrgID", objs[i]["FSaleOrgID"].ToString(), i);
                this.View.Model.SetValue("FSellerID", objs[i]["FSellerID"].ToString(), i);
                this.View.Model.SetValue("FDeptID", objs[i]["FDeptID"].ToString(), i);
                this.View.Model.SetValue("FOrgID", objs[i]["FOrgID"].ToString(), i);
                this.View.Model.SetValue("FCustID", objs[i]["FCustID"].ToString(), i);
                this.View.Model.SetValue("FTOrderAmt", objs[i]["FTOrderAmt"].ToString(), i);
                this.View.Model.SetValue("FPayWay", objs[i]["FPayWay"].ToString(), i);
                this.View.Model.SetValue("FLaterDelvGoodsDt", objs[i]["FLaterDelvGoodsDt"].ToString(), i);
                this.View.Model.SetValue("FTDeliverAmt", objs[i]["FTDeliverAmt"].ToString(), i);
                this.View.Model.SetValue("FTReceiverAmt", objs[i]["FTReceiverAmt"].ToString(), i);
                this.View.Model.SetValue("FTInvoiceAmt", objs[i]["FTInvoiceAmt"].ToString(), i);
                this.View.Model.SetValue("FOuterPmt", objs[i]["FOuterPmt"].ToString(), i);
                this.View.Model.SetValue("FNormOverduePmt", objs[i]["FNormOverduePmt"].ToString(), i);
                this.View.Model.SetValue("FNormUnoverduePmt", objs[i]["FNormUnoverduePmt"].ToString(), i);
                this.View.Model.SetValue("FOverduePmt", objs[i]["FOverduePmt"].ToString(), i);
                this.View.Model.SetValue("FTOverduePmt", objs[i]["FTOverduePmt"].ToString(), i);
                this.View.Model.SetValue("FTUnoverduePmt", objs[i]["FTUnoverduePmt"].ToString(), i);
                this.View.Model.SetValue("FTExceedePmt", objs[i]["FTExceedePmt"].ToString(), i);
                this.View.Model.SetValue("FOverdueWarranty", objs[i]["FOverdueWarranty"].ToString(), i);
                this.View.Model.SetValue("FUnoverdueWarranty", objs[i]["FUnoverdueWarranty"].ToString(), i);
                this.View.Model.SetValue("FTWarranty", objs[i]["FTWarranty"].ToString(), i);
                this.View.Model.SetValue("FIntervalMonth", objs[i]["FIntervalMonth"].ToString(), i);
                this.View.Model.SetValue("FIntervalDay", objs[i]["FIntervalDay"].ToString(), i);
                this.View.Model.SetValue("FRemarks", objs[i]["FRemark"].ToString(), i);
            }
            this.View.UpdateView("FEntity");
        }

        /// <summary>
        /// 查询货款明细
        /// </summary>
        private void Act_QueryDetailData()
        {
            string FSDate = this.View.Model.GetValue("FSDate") == null ? "" : this.View.Model.GetValue("FSDate").ToString();
            string FEDate = this.View.Model.GetValue("FEDate") == null ? "" : this.View.Model.GetValue("FEDate").ToString();
            string FQDeptId = this.View.Model.GetValue("FQDeptId") == null ? "0" : (this.View.Model.GetValue("FQDeptId") as DynamicObject)["Id"].ToString();
            string FQSalerId = this.View.Model.GetValue("FQSalerId") == null ? "0" : (this.View.Model.GetValue("FQSalerId") as DynamicObject)["Id"].ToString();
            string FQCustId = this.View.Model.GetValue("FQCustId") == null ? "0" : (this.View.Model.GetValue("FQCustId") as DynamicObject)["Id"].ToString();
            string FQFactoryId = this.View.Model.GetValue("FQFactoryId") == null ? "0" : (this.View.Model.GetValue("FQFactoryId") as DynamicObject)["Id"].ToString();
            string FQOrderNo = this.View.Model.GetValue("FQOrderNo") == null ? "" : this.View.Model.GetValue("FQOrderNo").ToString().Trim();

            string sql = string.Format(@"exec proc_czly_GetPmtDetail2 @SDt='{0}', @EDt='{1}', 
@FQDeptId={2}, @FQSalerId={3}, @FQCustId={4}, @FQFactoryId={5}, @FQOrderNo='{6}'",
            FSDate, FEDate, FQDeptId, FQSalerId, FQCustId, FQFactoryId, FQOrderNo);
            var objs = DBUtils.ExecuteDynamicObject(this.Context, sql);
            this.View.Model.DeleteEntryData("FDetailEntity");
            if (objs.Count <= 0)
            {
                return;
            }
            this.View.Model.BatchCreateNewEntryRow("FDetailEntity", objs.Count);
            for (int i = 0; i < objs.Count; i++)
            {
                this.View.Model.SetValue("FEOrderNo", objs[i]["FOrderNo"].ToString(), i);
                this.View.Model.SetValue("FEOrderSeq", objs[i]["FOrderSeq"].ToString(), i);
                this.View.Model.SetValue("FESellerID", objs[i]["FSellerID"].ToString(), i);
                this.View.Model.SetValue("FEDeptID", objs[i]["FDeptID"].ToString(), i);
                this.View.Model.SetValue("FEOrgID", objs[i]["FOrgID"].ToString(), i);
                this.View.Model.SetValue("FESaleOrgID", objs[i]["FSaleOrgID"].ToString(), i);
                this.View.Model.SetValue("FECustID", objs[i]["FCustID"].ToString(), i);
                this.View.Model.SetValue("FEFactoryID", objs[i]["FFactoryID"].ToString(), i);
                this.View.Model.SetValue("FEPayWay", objs[i]["FPayWay"].ToString(), i);
                this.View.Model.SetValue("FERemark", objs[i]["FRemark"].ToString(), i);
                this.View.Model.SetValue("FEEarlyDelvGoodsDt", objs[i]["FEarlyDelvGoodsDt"].ToString(), i);
                this.View.Model.SetValue("FELaterDelvGoodsDt", objs[i]["FLaterDelvGoodsDt"].ToString(), i);

                this.View.Model.SetValue("FEOrderAmt", objs[i]["FOrderAmt"].ToString(), i);
                this.View.Model.SetValue("FETOrderAmt", objs[i]["FTOrderAmt"].ToString(), i);

                this.View.Model.SetValue("FEDeliverAmt", objs[i]["FDeliverAmt"].ToString(), i);
                this.View.Model.SetValue("FEReceiverAmt", objs[i]["FReceiverAmt"].ToString(), i);
                this.View.Model.SetValue("FEInvoiceAmt", objs[i]["FInvoiceAmt"].ToString(), i);
                this.View.Model.SetValue("FEOuterPmt", objs[i]["FOuterPmt"].ToString(), i);
                this.View.Model.SetValue("FENormOverduePmt", objs[i]["FNormOverduePmt"].ToString(), i);
                this.View.Model.SetValue("FENormUnoverduePmt", objs[i]["FNormUnoverduePmt"].ToString(), i);
                this.View.Model.SetValue("FEOverduePmt", objs[i]["FOverduePmt"].ToString(), i);
                this.View.Model.SetValue("FETOverduePmt", objs[i]["FTOverduePmt"].ToString(), i);
                this.View.Model.SetValue("FETUnoverduePmt", objs[i]["FTUnoverduePmt"].ToString(), i);
                this.View.Model.SetValue("FETExceedePmt", objs[i]["FTExceedePmt"].ToString(), i);
                this.View.Model.SetValue("FEOverdueWarranty", objs[i]["FOverdueWarranty"].ToString(), i);
                this.View.Model.SetValue("FEUnoverdueWarranty", objs[i]["FUnoverdueWarranty"].ToString(), i);
                this.View.Model.SetValue("FEWarranty", objs[i]["FTWarranty"].ToString(), i);
                this.View.Model.SetValue("FEIntervalMonth", objs[i]["FIntervalMonth"].ToString(), i);
                this.View.Model.SetValue("FEIntervalDay", objs[i]["FIntervalDay"].ToString(), i);
            }
            
            this.View.UpdateView("FDetailEntity");

        }

        #endregion
    }
}
