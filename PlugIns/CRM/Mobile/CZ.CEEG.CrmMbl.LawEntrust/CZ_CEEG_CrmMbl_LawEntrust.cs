using System;
using System.ComponentModel;

using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.Util;
using Kingdee.BOS.App.Data;

using Kingdee.BOS.Mobile.PlugIn;


namespace CZ.CEEG.CrmMbl.LawEntrust
{
    [Description("Mobile开具法委")]
    [HotUpdate]
    public class CZ_CEEG_CrmMbl_LawEntrust : AbstractMobileBillPlugin
    {
        #region overrides
        public override void AfterBindData(EventArgs e)
        {
            base.AfterBindData(e);
            Act_BD_SetPushData();
            Act_DB_NotSelf();
        }

        public override void AfterDoOperation(AfterDoOperationEventArgs e)
        {
            base.AfterDoOperation(e);
            string key = e.Operation.Operation.ToLowerInvariant();
            switch (key)
            {
                case "SUBMIT":
                    Act_ADO_CreateBillRelation();
                    break;
                case "SAVE":
                    Act_ADO_CreateBillRelation();
                    break;

            }
        }

        public override void DataChanged(DataChangedEventArgs e)
        {
            base.DataChanged(e);
            string key = e.Field.Key.ToString();
            switch (key)
            {
                case "FNeedMail":
                    Act_DB_NotSelf();
                    break;
                case "F_ora_auth":
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
            string FIsSelfGet = this.View.BillModel.GetValue("FNeedMail").ToString();
            if (FIsSelfGet == "True")
            {
                this.View.GetControl("FSendToFL").Visible = true;
                this.View.GetControl("FSTPhoneFL").Visible = true;
                this.View.GetControl("FSTAddressFL").Visible = true;
            }
            else
            {
                this.View.GetControl("FSendToFL").Visible = false;
                this.View.GetControl("FSTPhoneFL").Visible = false;
                this.View.GetControl("FSTAddressFL").Visible = false;
            }
            string F_ora_auth = this.View.BillModel.GetValue("F_ora_auth") == null ? "" : this.View.BillModel.GetValue("F_ora_auth").ToString();
            if(F_ora_auth == "2")
            {
                this.View.GetControl("FAuthFL").Visible = true;
            }
            else if(F_ora_auth == "1")
            {
                this.View.GetControl("FAuthFL").Visible = false;
            }
        }

        /// <summary>
        /// 设置商机下推数据
        /// </summary>
        private void Act_BD_SetPushData()
        {
            string flag = this.View.OpenParameter.GetCustomParameter("Flag") == null ? "" : this.View.OpenParameter.GetCustomParameter("Flag").ToString();
            if (flag == "ADD")
            {
                string _FBillNo = this.View.OpenParameter.GetCustomParameter("FBillNo") == null ? "" : this.View.OpenParameter.GetCustomParameter("FBillNo").ToString();
                string _FCustID = this.View.OpenParameter.GetCustomParameter("FCustID") == null ? "" : this.View.OpenParameter.GetCustomParameter("FCustID").ToString();
                string _FPrjName = this.View.OpenParameter.GetCustomParameter("FPrjName") == null ? "" : this.View.OpenParameter.GetCustomParameter("FPrjName").ToString();
                string _FCrmSN = this.View.OpenParameter.GetCustomParameter("FCrmSN") == null ? "" : this.View.OpenParameter.GetCustomParameter("FCrmSN").ToString();

                this.View.BillModel.SetValue("FNicheID", _FBillNo);
                this.View.BillModel.SetValue("FNicheNo", _FBillNo);
                this.View.BillModel.SetValue("FCustName", _FCustID);
                this.View.BillModel.SetValue("FPrjName", _FPrjName);
                this.View.BillModel.SetValue("FCrmSN", _FCrmSN);
            }
        }

        /// <summary>
        /// 接收下推并保存时创建单据关联，需要在保存完成后执行
        /// </summary>
        private void Act_ADO_CreateBillRelation()
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
            string _FNicheNo = this.View.OpenParameter.GetCustomParameter("FBillNo").ToString();
            string _sql = "select FID from ora_CRM_Niche where FBILLNO='" + _FNicheNo + "'";

            string lktable = "ora_CRM_LawEntrust_LK";
            string targetfid = this.View.BillModel.DataObject["Id"].ToString();
            string targettable = "ora_CRM_LawEntrust";
            string targetformid = "ora_CRM_LawEntrust";
            string sourcefid = CZDB_GetData(_sql)[0]["FID"].ToString();
            string sourcetable = "ora_CRM_Niche";
            string sourceformid = "ora_CRM_Niche";
            string sourcefentryid = "0";
            string sourcefentrytable = "";
            string sql = String.Format(@"exec proc_czly_CreateBillRelation 
                   @lktable='{0}',@targetfid='{1}',@targettable='{2}',
                   @targetformid='{3}',@sourcefid='{4}',@sourcetable='{5}',
                   @sourceformid='{6}',@sourcefentryid='{7}',@sourcefentrytable='{8}'",
                   lktable, targetfid, targettable, targetformid, sourcefid, sourcetable, sourceformid, sourcefentryid, sourcefentrytable);
            var obj = CZDB_GetData(sql);

        }
        #endregion

        #region 数据库查询方法
        /// <summary>
        /// 基本方法 数据库查询
        /// </summary>
        /// <param name="_sql"></param>
        /// <returns></returns>
        public DynamicObjectCollection CZDB_GetData(string _sql)
        {
            try
            {
                var obj = DBUtils.ExecuteDynamicObject(this.Context, _sql);
                return obj;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        #endregion

    }
}
