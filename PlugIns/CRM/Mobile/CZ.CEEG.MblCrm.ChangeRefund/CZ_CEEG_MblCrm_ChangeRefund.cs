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

namespace CZ.CEEG.MblCrm.ChangeRefund
{
    [Description("Mbl退换货维修")]
    [HotUpdate]
    public class CZ_CEEG_MblCrm_ChangeRefund : AbstractMobileBillPlugin
    {
        #region overrides
        public override void AfterCreateNewData(EventArgs e)
        {
            base.AfterCreateNewData(e);
            Act_CND_SetPushData();
        }

        public override void AfterBindData(EventArgs e)
        {
            base.AfterBindData(e);
            Act_ADB_SetBtnEnable();
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
        #endregion

        #region Actions
        private void Act_ADB_SetBtnEnable()
        {
            string _FDocumentStatus = this.View.BillModel.GetValue("FDocumentStatus").ToString();
            if (_FDocumentStatus == "Z" || _FDocumentStatus == "A" || _FDocumentStatus == "D")
            {
                this.View.GetControl("FSubmitBtn").Enabled = true;
            }
            else
            {
                this.View.GetControl("FSubmitBtn").Enabled = false;
            }
        }

        /// <summary>
        /// 设置售后服务下推数据
        /// </summary>
        private void Act_CND_SetPushData()
        {
            string flag = this.View.OpenParameter.GetCustomParameter("Flag") == null ? "" : this.View.OpenParameter.GetCustomParameter("Flag").ToString();
            if (flag == "ADD")
            {
                string srcFID = this.View.OpenParameter.GetCustomParameter("FID") == null ? "0" : this.View.OpenParameter.GetCustomParameter("FID").ToString();
                string sql = string.Format(@"SELECT * FROM ora_CRM_CCRP WHERE FID='{0}'", srcFID);
                var objs = DBUtils.ExecuteDynamicObject(this.Context, sql);
                if (objs.Count <= 0)
                {
                    return;
                }
                this.View.BillModel.SetValue("F_ora_SourceBillNo", objs[0]["FBillNo"].ToString());
                this.View.BillModel.SetValue("FSaleOrderID", objs[0]["F_ora_SourceBillNo"].ToString());
                this.View.BillModel.SetValue("FSaleCntNo", objs[0]["FContractNo"].ToString());
                this.View.BillModel.SetValue("FNicheNo", objs[0]["FNicheNo"].ToString());
                this.View.BillModel.SetValue("FCustID", objs[0]["FCustID"].ToString());
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
            string lktable = "ora_CRM_AfterSaleSrv_LK";
            string targetfid = tgtFID;
            string targettable = "ora_CRM_AfterSaleSrv";
            string targetformid = "ora_CRM_AfterSaleSrv";
            string sourcefid = srcFID;
            string sourcetable = "ora_CRM_CCRP";
            string sourceformid = "ora_CRM_CCRP";
            string sourcefentryid = "0";
            string sourcefentrytable = "";
            string sql = String.Format(@"exec proc_czly_CreateBillRelation 
                   @lktable='{0}',@targetfid='{1}',@targettable='{2}',
                   @targetformid='{3}',@sourcefid='{4}',@sourcetable='{5}',
                   @sourceformid='{6}',@sourcefentryid='{7}',@sourcefentrytable='{8}'",
                   lktable, targetfid, targettable, targetformid, sourcefid, sourcetable, sourceformid, sourcefentryid, sourcefentrytable);
            DBUtils.ExecuteDynamicObject(this.Context, sql);
        }
        #endregion
    }
}
