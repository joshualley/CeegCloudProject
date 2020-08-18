using Kingdee.BOS;
using Kingdee.BOS.App.Data;
using Kingdee.BOS.Core.DynamicForm;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Core.Metadata;
using Kingdee.BOS.Mobile;
using Kingdee.BOS.Mobile.PlugIn;
using Kingdee.BOS.Util;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace CZ.CEEG.MblCrm.SaleInvoice
{
    [Description("Mbl销售开票")]
    [HotUpdate]
    public class CZ_CEEG_MblCrm_SaleInvoice : AbstractMobileBillPlugin
    {

        #region overrides

        public override void AfterBindData(EventArgs e)
        {
            base.AfterBindData(e);
            string FDocumentStatus = this.View.BillModel.GetValue("FDocumentStatus").ToString();
            if (FDocumentStatus == "Z")
            {
                Act_CND_SetPushData();
            }
            Act_BD_BtnEnable();

        }

        public override void BeforeDoOperation(BeforeDoOperationEventArgs e)
        {
            base.BeforeDoOperation(e);
            string key = e.Operation.FormOperation.Operation.ToUpperInvariant();
            switch (key)
            {
                case "SUBMIT":
                    if (!Act_CheackPass())
                    {
                        e.Cancel = true;
                    }
                    break;
            }
        }

        public override void AfterDoOperation(AfterDoOperationEventArgs e)
        {
            base.AfterDoOperation(e);
            
            string key = e.Operation.Operation.ToUpperInvariant();
            switch (key)
            {
                case "SUBMIT":
                    Act_CreateBillRelation();
                    break;
            }
        }

        public override void DataChanged(DataChangedEventArgs e)
        {
            base.DataChanged(e);
            string key = e.Field.Key.ToString();
            switch (key)
            {
                case "FIsSelfGet":
                    Act_DB_NotSelf();
                    break;
            }
        }

        #endregion

        #region Actions
        /// <summary>
        /// 非本人领取显示收件人字段
        /// </summary>
        private void Act_DB_NotSelf()
        {
            string FIsSelfGet = this.View.BillModel.GetValue("FIsSelfGet").ToString();
            if(FIsSelfGet == "True")
            {
                this.View.GetControl("FSendToFL").Visible = false;
                this.View.GetControl("FSTPhoneFL").Visible = false;
                this.View.GetControl("FSTAddressFL").Visible = false;
            }
            else
            {
                this.View.GetControl("FSendToFL").Visible = true;
                this.View.GetControl("FSTPhoneFL").Visible = true;
                this.View.GetControl("FSTAddressFL").Visible = true;
            }
            
        }
        private bool Act_CheackPass()
        {
            string FIsSelfGet = this.View.BillModel.GetValue("FIsSelfGet").ToString();
            if (FIsSelfGet == "False")
            {
                string FSendTo = this.View.BillModel.GetValue("FSendTo") == null ? "" : this.View.BillModel.GetValue("FSendTo").ToString();
                string FSTPhone = this.View.BillModel.GetValue("FSTPhone") == null ? "" : this.View.BillModel.GetValue("FSTPhone").ToString();
                string FSTAddress = this.View.BillModel.GetValue("FSTAddress") == null ? "" : this.View.BillModel.GetValue("FSTAddress").ToString();
                if(FSendTo.IsNullOrEmptyOrWhiteSpace() || FSTAddress.IsNullOrEmptyOrWhiteSpace() || FSTPhone.IsNullOrEmptyOrWhiteSpace())
                {
                    this.View.ShowMessage("收件人信息是必录的！");
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// 设置下推数据
        /// </summary>
        private void Act_CND_SetPushData()
        {
            string flag = this.View.OpenParameter.GetCustomParameter("Flag") == null ? "" : this.View.OpenParameter.GetCustomParameter("Flag").ToString();
            if (flag == "ADD")
            {
                
                string srcFID = this.View.OpenParameter.GetCustomParameter("FID") == null ? "0" : this.View.OpenParameter.GetCustomParameter("FID").ToString();
                string sql = string.Format(@"EXEC proc_czly_GetSaleOrderSrcInfo @FID='{0}'", srcFID);
                var objs = DBUtils.ExecuteDynamicObject(this.Context, sql);
                if (objs.Count <= 0)
                {
                    return;
                }
                
                this.View.BillModel.SetValue("FSaleOrgID", objs[0]["FSaleOrgId"].ToString());
                this.View.BillModel.SetValue("FSaleOrderID", objs[0]["FSaleOrderBillNo"].ToString());
                this.View.BillModel.SetValue("FSaleOrderNo", objs[0]["FSaleOrderBillNo"].ToString());
                this.View.BillModel.SetValue("FNicheNo", objs[0]["FNicheBillNo"].ToString());
                this.View.BillModel.SetValue("FSaleCntNo", objs[0]["FContractBillNo"].ToString());
                this.View.BillModel.SetValue("FCustOrgId", objs[0]["FCustOrgID"].ToString());
                this.View.BillModel.SetValue("FCustID", objs[0]["FCustID"].ToString());
                this.View.BillModel.SetValue("FOrderAmt", objs[0]["FOrderAmt"].ToString());
                this.View.BillModel.SetValue("FSendPdtAmt", objs[0]["FSendPdtAmt"].ToString());
                this.View.BillModel.SetValue("FRecAmt", objs[0]["FRecAmt"].ToString());
                this.View.BillModel.SetValue("FInvAmt", objs[0]["FInvAmt"].ToString());
                sql = "SELECT FADDRESS,FTEL,FTAXREGISTERCODE,ISNULL(FBANKCODE,'')FBANKCODE,ISNULL(FACCOUNTNAME,'')FACCOUNTNAME FROM T_BD_CUSTOMER c " +
                      "LEFT JOIN T_BD_CUSTBANK cb ON c.FCUSTID=cb.FCUSTID WHERE c.FCUSTID='" + objs[0]["FCustID"].ToString() + "'";
                var cust = DBUtils.ExecuteDynamicObject(this.Context, sql);
                if(cust.Count > 0)
                {
                    this.View.BillModel.SetValue("FCustAddress", cust[0]["FADDRESS"].ToString());
                    this.View.BillModel.SetValue("FCustCPhone", cust[0]["FTEL"].ToString());
                    this.View.BillModel.SetValue("FCustBank", cust[0]["FACCOUNTNAME"].ToString());
                    this.View.BillModel.SetValue("FCustBankNo", cust[0]["FBANKCODE"].ToString());
                    this.View.BillModel.SetValue("FTaxNum", cust[0]["FTAXREGISTERCODE"].ToString());
                }
            }
        }

        /// <summary>
        /// 接收下推并保存时创建单据关联，需要在保存完成后执行
        /// </summary>
        private void Act_CreateBillRelation()
        {
            /*
                @lktable varchar(30),--下游单据关联表
                @targetfid int,--下游单据头内码
                @targettable varchar(30),--下游单据头表名
                @targetformid varchar(36),--下游单据标识
                @sourcefid int,--上游单据头内码
                @sourcetable varchar(30),--上游单据头表名
                @sourceformid varchar(36),--上游单据标识
                @sourcefentryid int = 0, --上游单据体内码
                @sourcefentrytable varchar(30) = '' -- 上游单据体表名
             */
            string flag = this.View.OpenParameter.GetCustomParameter("Flag") == null ? "" : this.View.OpenParameter.GetCustomParameter("Flag").ToString();
            if (flag != "ADD")
            {
                return;
            }
            string srcFID = this.View.OpenParameter.GetCustomParameter("FID") == null ? "0" : this.View.OpenParameter.GetCustomParameter("FID").ToString();
            string tgtFID = this.View.BillModel.DataObject["Id"] == null ? "0" : this.View.BillModel.DataObject["Id"].ToString();
            if (srcFID == "0" || tgtFID == "0")
            {
                return;
            }
            string lktable = "ora_CRM_SaleInvoice_LK";
            string targetfid = tgtFID;
            string targettable = "ora_CRM_SaleInvoice";
            string targetformid = "ora_CRM_SaleInvoice";
            string sourcefid = srcFID;
            string sourcetable = "T_SAL_ORDER";
            string sourceformid = "ora_Cust_SaleOrder";
            string sourcefentryid = "0";
            string sourcefentrytable = "";
            string sql = String.Format(@"exec proc_czly_CreateBillRelation 
                   @lktable='{0}',@targetfid='{1}',@targettable='{2}',
                   @targetformid='{3}',@sourcefid='{4}',@sourcetable='{5}',
                   @sourceformid='{6}',@sourcefentryid='{7}',@sourcefentrytable='{8}'",
                   lktable, targetfid, targettable, targetformid, sourcefid, sourcetable, sourceformid, sourcefentryid, sourcefentrytable);
            DBUtils.ExecuteDynamicObject(this.Context, sql);
        }

        private void Act_BD_BtnEnable()
        {
            string _FDocumentStatus = this.View.BillModel.GetValue("FDocumentStatus").ToString();
            if(_FDocumentStatus == "Z" || _FDocumentStatus == "A" || _FDocumentStatus == "D")
            {
                this.View.GetControl("FSubmitBtn").Enabled = true;
            }
            else
            {
                this.View.GetControl("FSubmitBtn").Enabled = false;
            }
        }

        #endregion
    }
}
