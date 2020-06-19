using Kingdee.BOS;
using Kingdee.BOS.App.Data;
using Kingdee.BOS.Core.DynamicForm;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Core.DynamicForm.PlugIn.ControlModel;
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
    [Description("Mbl维修合同")]
    [HotUpdate]
    public class CZ_CEEG_MblCrm_MaintainContract : AbstractMobileBillPlugin
    {
        #region overrides

        public override void AfterBindData(EventArgs e)
        {
            base.AfterBindData(e);
            EntryEditEnable();
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
                case "FNEWROW": //新增行
                    AddNewEntryRow();
                    break;
                case "FTRACKUP": //上查
                    Act_TrackUp();
                    break;
            }
        }

        #endregion

        #region Actions
        private void Act_TrackUp()
        {
            string FBillNo = this.View.BillModel.GetValue("FSourceBillNo").ToString();
            string sql = "SELECT FID FROM ora_CRM_MantainOffer WHERE FBillNo='" + FBillNo + "'";
            var objs = DBUtils.ExecuteDynamicObject(this.Context, sql);
            if(objs.Count > 0)
            {
                var para = new MobileShowParameter();
                para.FormId = "ora_CRM_MBL_MaintainOffer"; //源单FormId
                para.OpenStyle.ShowType = ShowType.Modal;
                para.ParentPageId = this.View.PageId;
                para.Status = OperationStatus.EDIT;
                para.PKey = objs[0]["FID"].ToString();
                string strTitle = "维修报价";
                var formTitle = new LocaleValue();
                formTitle.Add(new KeyValuePair<int, string>(this.Context.UserLocale.LCID, strTitle));
                this.View.SetFormTitle(formTitle);
                this.View.ShowForm(para);
            }
            
        }


        /// <summary>
        /// 表体可编辑
        /// </summary>
        private void EntryEditEnable()
        {
            Control entryCtl = null;
            try
            {
                entryCtl = this.View.GetControl("F_ora_MobileProxyEntryEntity");
            }
            catch (Exception) { }

            if (entryCtl != null)
            {
                if (this.View.OpenParameter.Status.ToString() != "VIEW")
                {
                    entryCtl.SetCustomPropertyValue("listEditable", true);
                }
                else
                {
                    entryCtl.SetCustomPropertyValue("listEditable", false);
                }
            }
        }

        /// <summary>
        /// 新增表体行
        /// </summary>
        private void AddNewEntryRow()
        {
            this.View.BillModel.BeginIniti();
            this.View.BillModel.CreateNewEntryRow("FEntity");
            this.View.BillModel.EndIniti();
            this.View.UpdateView("F_ora_MobileProxyEntryEntity");
        }

        /// <summary>
        /// 设置按钮，页签显示隐藏
        /// </summary>
        private void Act_DB_SetVisible()
        {
            string _FDocumentStatus = this.View.BillModel.GetValue("FDocumentStatus").ToString();
            if (_FDocumentStatus == "Z" || _FDocumentStatus == "A" || _FDocumentStatus == "D")
            {
                this.View.GetControl("FTrackUp").Visible = false;
                var submitBtn = this.View.GetControl("FSubmitBtn");
                submitBtn.SetCustomPropertyValue("width", 310);
            }
            else
            {
                this.View.GetControl("FSubmitBtn").Visible = false;
                var FTrackUp = this.View.GetControl("FTrackUp");
                FTrackUp.SetCustomPropertyValue("width", 310);
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
                string sql = string.Format(@"SELECT * FROM ora_CRM_MantainOffer WHERE FID='{0}'", srcFID);
                var objs = DBUtils.ExecuteDynamicObject(this.Context, sql);
                if (objs.Count <= 0)
                {
                    return;
                }
                this.View.BillModel.SetValue("FSourceBillNo", objs[0]["FBillNo"].ToString());
                this.View.BillModel.SetValue("FSaleOrderNo", objs[0]["FSaleOrderNo"].ToString());
                this.View.BillModel.SetValue("FNicheNo", objs[0]["FNicheNo"].ToString());
                this.View.BillModel.SetValue("FContractNo", objs[0]["FContractNo"].ToString());
                this.View.BillModel.SetValue("FCustID", objs[0]["FCustID"].ToString());
                this.View.BillModel.SetValue("FProjName", objs[0]["FProjName"].ToString());
                this.View.BillModel.SetValue("FSalerID", objs[0]["FSalerID"].ToString());
                this.View.BillModel.SetValue("FDept", objs[0]["FDept"].ToString());
                sql = "SELECT * FROM ora_CRM_MantainOfferEntry WHERE FID='" + srcFID + "'";
                objs = DBUtils.ExecuteDynamicObject(this.Context, sql);
                for (int i = 0;  i < objs.Count; i++)
                {
                    if(i != 0)
                    {
                        this.View.BillModel.CreateNewEntryRow("FEntity");
                    }
                    this.View.BillModel.SetValue("FMaterialID", objs[i]["FMaterialID"].ToString(), i);
                    this.View.BillModel.SetValue("FUnitID", objs[i]["FUnitID"].ToString(), i);
                    this.View.BillModel.SetValue("FTaxRate", objs[i]["FTaxRate"].ToString(), i);
                    this.View.BillModel.SetValue("FQty", objs[i]["FQty"].ToString(), i);

                    decimal FBRptPrice = decimal.Parse(objs[i]["FBasePrice"].ToString());
                    decimal FQty = decimal.Parse(objs[i]["FQty"].ToString());
                    decimal FTaxRate = decimal.Parse(objs[i]["FTaxRate"].ToString());

                    this.View.BillModel.SetValue("FUtPriceTax", FBRptPrice/FQty, i);
                    this.View.BillModel.SetValue("FUtPrice", FBRptPrice / FQty / (1 + FTaxRate / 100), i);
                    this.View.BillModel.SetValue("FBTaxAmt", FBRptPrice - FBRptPrice / (1 + FTaxRate / 100), i);
                    this.View.BillModel.SetValue("FBNTAmt", FBRptPrice / (1 + FTaxRate / 100), i);

                    this.View.BillModel.SetValue("FNote", objs[i]["FNote"].ToString(), i);
                    this.View.BillModel.SetValue("FBasePrice", objs[i]["FPurPrice"].ToString(), i);
                    this.View.BillModel.SetValue("FBPAmtGroup", objs[i]["FCost"].ToString(), i);
                    this.View.BillModel.SetValue("FBRptPrice", FBRptPrice, i);
                }
                this.View.UpdateView("FEntity");
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
            string lktable = "ora_CRM_MtnCont_LK";
            string targetfid = tgtFID;
            string targettable = "ora_CRM_MtnCont";
            string targetformid = "ora_CRM_MantainContract";
            string sourcefid = srcFID;
            string sourcetable = "ora_CRM_MantainOffer";
            string sourceformid = "ora_CRM_MantainOffer";
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
