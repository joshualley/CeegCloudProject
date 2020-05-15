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

namespace CZ.CEEG.MblCrm.MaintainOffer
{
    [Description("Mbl维修报价")]
    [HotUpdate]
    public class CZ_CEEG_MblCrm_MaintainOffer : AbstractMobileBillPlugin
    {
        #region overrides

        public override void AfterBindData(EventArgs e)
        {
            base.AfterBindData(e);
            Act_CND_SetPushData();
            Act_DB_SetVisible();
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

        public override void AfterButtonClick(AfterButtonClickEventArgs e)
        {
            base.AfterButtonClick(e);
            string key = e.Key.ToUpperInvariant();
            switch (key)
            {
                case "FPUSHBTN":
                    Act_ABC_PushChangeRefund();
                    break;
            }
        }

        #endregion

        #region Actions
        /// <summary>
        /// 设置按钮，页签显示隐藏
        /// </summary>
        private void Act_DB_SetVisible()
        {
            string _FDocumentStatus = this.View.BillModel.GetValue("FDocumentStatus").ToString();
            if (_FDocumentStatus == "Z" || _FDocumentStatus == "A" || _FDocumentStatus == "D")
            {
                var submitBtn = this.View.GetControl("FSubmitBtn");
                var pushBtn = this.View.GetControl("FPushBtn");
                submitBtn.Visible = true;
                pushBtn.Visible = false;
                submitBtn.SetCustomPropertyValue("width", 310);
            }
            else if (_FDocumentStatus == "B")
            {
                var submitBtn = this.View.GetControl("FSubmitBtn");
                var pushBtn = this.View.GetControl("FPushBtn");
                submitBtn.Visible = false;
                pushBtn.Visible = false;
            }
            else
            {
                var pushBtn = this.View.GetControl("FPushBtn");
                pushBtn.Visible = true;
                pushBtn.SetCustomPropertyValue("width", 310);
                this.View.GetControl("FSubmitBtn").Visible = false;
            }
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
                string sql = string.Format(@"SELECT * FROM ora_CRM_CCRP WHERE FID='{0}'", srcFID);
                var objs = DBUtils.ExecuteDynamicObject(this.Context, sql);
                if (objs.Count <= 0)
                {
                    return;
                }
                this.View.BillModel.SetValue("FSourceBillNo", objs[0]["FBillNo"].ToString());
                this.View.BillModel.SetValue("FSaleOrderNo", objs[0]["F_ora_SourceBillNo"].ToString());
                this.View.BillModel.SetValue("FNicheNo", objs[0]["FNicheNo"].ToString());
                this.View.BillModel.SetValue("FContractNo", objs[0]["FContractNo"].ToString());
                this.View.BillModel.SetValue("FCustID", objs[0]["FCustID"].ToString());
                this.View.BillModel.SetValue("FPrjName", objs[0]["FPrjName"].ToString());
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
            string lktable = "ora_CRM_MantainOffer_LK";
            string targetfid = tgtFID;
            string targettable = "ora_CRM_MantainOffer";
            string targetformid = "ora_CRM_MantainOffer";
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

        /// <summary>
        /// 下推维修合同
        /// </summary>
        private void Act_ABC_PushChangeRefund()
        {
            string FID = "0";
            string _FBillNo = this.View.BillModel.GetValue("FBillNo").ToString();
            string sql = "select FID from ora_CRM_RepairContract where FNicheID='" + _FBillNo + "'";
            var objs = DBUtils.ExecuteDynamicObject(this.Context, sql);
            if (objs.Count > 0)
            {
                FID = objs[0]["FID"].ToString();
            }
            Act_Push_Common("ora_CRM_MBL_MaintainContract", "维修合同评审", FID);

        }


        /// <summary>
        /// 通用下推
        /// </summary>
        private void Act_Push_Common(string formId, string title, string distFID)
        {
            var para = new MobileShowParameter();
            para.FormId = formId;
            para.OpenStyle.ShowType = ShowType.Modal;
            para.ParentPageId = this.View.PageId;
            para.Status = OperationStatus.EDIT;
            string srcFID = this.View.BillModel.DataObject["Id"] == null ? "0" : this.View.BillModel.DataObject["Id"].ToString();
            para.CustomParams.Add("FID", srcFID);
            if (distFID != "0")
            {
                this.View.ShowMessage("已存在下推的单据，是否打开？", MessageBoxOptions.YesNo, new Action<MessageBoxResult>((result) =>
                {
                    if (result == MessageBoxResult.Yes)
                    {
                        para.PKey = distFID;
                        para.CustomParams.Add("Flag", "EDIT");
                        //设置表单Title
                        string strTitle = title;
                        var formTitle = new LocaleValue();
                        formTitle.Add(new KeyValuePair<int, string>(this.Context.UserLocale.LCID, strTitle));
                        this.View.SetFormTitle(formTitle);
                        this.View.ShowForm(para);

                    }
                }));

            }
            else
            {
                para.CustomParams.Add("Flag", "ADD");
                this.View.ShowMessage("是否要下推生成" + title + "?", MessageBoxOptions.YesNo, (result) =>
                {
                    if (result == MessageBoxResult.Yes)
                    {
                        //设置表单Title
                        string strTitle = title;
                        var formTitle = new LocaleValue();
                        formTitle.Add(new KeyValuePair<int, string>(this.Context.UserLocale.LCID, strTitle));
                        this.View.SetFormTitle(formTitle);
                        this.View.ShowForm(para);
                    }
                });

            }

        }

        #endregion
    }
}
