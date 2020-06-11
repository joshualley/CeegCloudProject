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
            this.View.Model.SetValue("FSDate", sDt);
            this.View.UpdateView("FSDate");
            this.View.Model.SetValue("FEDate", eDt);
            this.View.UpdateView("FEDate");
            Act_QueryData(sDt, eDt);
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
                    Act_QueryData(sDt, eDt);
                    break;
            }
        }

        #endregion

        #region Actions

        /// <summary>
        /// 查询在外货款数据
        /// </summary>
        private void Act_QueryData(string sDt, string eDt)
        {
            string sql = "exec proc_czly_GetPmtSummary @SDt='" + sDt + "', @EDt='" + eDt + "'";
            var objs = DBUtils.ExecuteDynamicObject(this.Context, sql);

            this.View.Model.DeleteEntryData("FEntity");
            for (int i = 0; i < objs.Count; i++)
            {
                this.View.Model.CreateNewEntryRow("FEntity");
                this.View.Model.SetValue("FOrderNo", objs[i]["FOrderNo"].ToString(), i);
                this.View.Model.SetValue("FSellerID", objs[i]["FSellerID"].ToString(), i);
                this.View.Model.SetValue("FCustID", objs[i]["FCustID"].ToString(), i);
                this.View.Model.SetValue("FOrderAmt", objs[i]["FTOrderAmt"].ToString(), i);
                this.View.Model.SetValue("FPayWay", objs[i]["FPayWay"].ToString(), i);
                this.View.Model.SetValue("FLaterDelvGoodsDt", objs[i]["FLaterDelvGoodsDt"].ToString(), i);
                this.View.Model.SetValue("FTimeInterval", objs[i]["FIntervalDay"].ToString(), i);
                this.View.Model.SetValue("FDeliverAmt", objs[i]["FTDeliverAmt"].ToString(), i);
                this.View.Model.SetValue("FReceiverAmt", objs[i]["FTReceiverAmt"].ToString(), i);
                this.View.Model.SetValue("FInvoiceAmt", objs[i]["FTInvoiceAmt"].ToString(), i);
                this.View.Model.SetValue("FOuterPmt", objs[i]["FOuterPmt"].ToString(), i);
                //this.View.Model.SetValue("FOptExpense", objs[i]["FOptExpense"].ToString(), i);
                //this.View.Model.SetValue("FInterestPenalty", objs[i]["FInterestPenalty"].ToString(), i);
                this.View.Model.SetValue("FRemark", objs[i]["FRemark"].ToString(), i);
            }
            this.View.UpdateView("FEntity");
        }

        #endregion
    }
}
