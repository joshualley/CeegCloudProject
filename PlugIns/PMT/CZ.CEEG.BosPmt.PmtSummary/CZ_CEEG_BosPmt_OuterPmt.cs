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

namespace CZ.CEEG.BosPmt.OuterPmt
{
    [HotUpdate]
    [Description("在外货款报表")]
    public class CZ_CEEG_BosPmt_OuterPmt : AbstractDynamicFormPlugIn
    {
        #region Overrides
        public override void AfterBindData(EventArgs e)
        {
            base.AfterBindData(e);
            DateTime currDt = DateTime.Now;
            string sDt = currDt.Year.ToString() + "-" + currDt.Month.ToString() + "-01";
            string eDt = currDt.ToString();
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
            Act_QueryData();
        }

        public override void AfterButtonClick(AfterButtonClickEventArgs e)
        {
            base.AfterButtonClick(e);
            string key = e.Key.ToUpperInvariant();
            switch (key)
            {
                case "FQUERYBTN":
                    Act_QueryData();
                    break;
            }
        }

        #endregion

        #region Actions

        /// <summary>
        /// 查询在外货款数据
        /// </summary>
        private void Act_QueryData()
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
            string FIsOldSysOrder = "";
            for (int i = 0; i < objs.Count; i++)
            {
                this.View.Model.SetValue("FOrderNo", objs[i]["FOrderNo"].ToString(), i);
                this.View.Model.SetValue("FSignOrgID", objs[i]["FSignOrgID"].ToString(), i);
                this.View.Model.SetValue("FSerialNum", objs[i]["FSerialNum"].ToString(), i);
                this.View.Model.SetValue("FSellerID", objs[i]["FSellerID"].ToString(), i);
                this.View.Model.SetValue("FCustID", objs[i]["FCustID"].ToString(), i);
                this.View.Model.SetValue("FOrderAmt", objs[i]["FTOrderAmt"].ToString(), i);
                this.View.Model.SetValue("FPayWay", objs[i]["FPayWay"].ToString(), i);
                string dt = objs[i]["FLaterDelvGoodsDt"].ToString().Split(' ')[0] == "1900-01-01" ? "" : objs[i]["FLaterDelvGoodsDt"].ToString();
                this.View.Model.SetValue("FLaterDelvGoodsDt", dt, i);
                this.View.Model.SetValue("FTimeInterval", objs[i]["FIntervalDay"].ToString(), i);
                this.View.Model.SetValue("FDeliverAmt", objs[i]["FTDeliverAmt"].ToString(), i);
                this.View.Model.SetValue("FReceiverAmt", objs[i]["FTReceiverAmt"].ToString(), i);
                this.View.Model.SetValue("FInvoiceAmt", objs[i]["FTInvoiceAmt"].ToString(), i);
                this.View.Model.SetValue("FOuterPmt", objs[i]["FOuterPmt"].ToString(), i);
                this.Model.SetValue("FOuterPmtAll",
                    decimal.Parse(objs[i]["FTDeliverAmt"].ToString()) -
                    decimal.Parse(objs[i]["FTReceiverAmt"].ToString()), i);
                //this.View.Model.SetValue("FOptExpense", objs[i]["FOptExpense"].ToString(), i);
                //this.View.Model.SetValue("FInterestPenalty", objs[i]["FInterestPenalty"].ToString(), i);
                this.View.Model.SetValue("FRemark", objs[i]["FRemark"].ToString(), i);
                FIsOldSysOrder = objs[i]["FOrderNo"].ToString().StartsWith("XSDD") ? "否" : "是";
                this.View.Model.SetValue("FIsOldSysOrder", FIsOldSysOrder, i);
            }
            this.View.UpdateView("FEntity");
        }

        #endregion
    }
}
