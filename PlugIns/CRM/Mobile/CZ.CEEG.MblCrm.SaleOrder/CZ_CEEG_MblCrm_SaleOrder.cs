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

namespace CZ.CEEG.MblCrm.SaleOrder
{
    [Description("Mbl销售订单")]
    [HotUpdate]
    public class CZ_CEEG_MblCrm_SaleOrder : AbstractMobileBillPlugin
    {

        #region overrides
        public override void AfterBindData(EventArgs e)
        {
            base.AfterBindData(e);
            Act_DB_SetBtnEnable();

        }
        public override void AfterButtonClick(AfterButtonClickEventArgs e)
        {
            base.AfterButtonClick(e);
            string key = e.Key.ToUpperInvariant();
            switch (key)
            {
                case "FPUSH":
                    Act_BC_OpenPushOptions();
                    break;
            }
        }

        #endregion




        #region Actions
        /// <summary>
        /// 设置下推按钮可用
        /// </summary>
        private void Act_DB_SetBtnEnable()
        {
            string _FDocumentStatus = this.View.BillModel.GetValue("FDocumentStatus").ToString();
            if(_FDocumentStatus == "C")
            {
                this.View.GetControl("FPush").Enabled = true;
            }
            else
            {
                this.View.GetControl("FPush").Enabled = false;
            }
        }

        /// <summary>
        /// 打卡下推选择界面
        /// </summary>
        private void Act_BC_OpenPushOptions()
        {
            //打开选择界面
            var para = new MobileShowParameter();
            para.FormId = "ora_CRM_SaleOrderOptions";
            para.OpenStyle.ShowType = ShowType.Modal;
            para.Height = 132;
            para.Width = 240;
            para.ParentPageId = this.View.PageId;
            para.Status = OperationStatus.EDIT;
            string pushFormId = "";
            this.View.ShowForm(para, (formResult) =>
            {
                if (formResult.ReturnData == null)
                {
                    return;
                }
                pushFormId = formResult.ReturnData.ToString();
                string FID = "0";
                string _FBillNo = this.View.BillModel.GetValue("FBillNo").ToString();
                string sql;
                /*
                if (pushFormId == "ora_CRM_MBL_DeliverNotify") //发货通知
                {
                    sql = "select Distinct FID from T_SAL_DELIVERYNOTICEENTRY where FSrcBillNo='" + _FBillNo + "'";
                    var objs = DBUtils.ExecuteDynamicObject(this.Context, sql);
                    if (objs.Count > 0)
                    {
                        FID = objs[0]["FID"].ToString();
                    }
                    Act_Push_Common("ora_CRM_MBL_DeliverNotify", "发货通知", FID);
                }
                else 
                */
                if (pushFormId == "ora_CRM_MBL_SaleInvoice") //销售开票
                {
                    sql = "select FID from ora_CRM_SaleInvoice where FSaleOrderID='" + _FBillNo + "'";
                    var objs = DBUtils.ExecuteDynamicObject(this.Context, sql);
                    if (objs.Count > 0)
                    {
                        FID = objs[0]["FID"].ToString();
                    }
                    Act_Push_Common("ora_CRM_MBL_SaleInvoice", "销售开票", FID);
                }
                else if (pushFormId == "ora_CRM_MBL_CCRP") //售后服务
                {
                    sql = "select FID from ora_CRM_CCRP where F_ora_SourceBillNo='" + _FBillNo + "'";
                    var objs = DBUtils.ExecuteDynamicObject(this.Context, sql);
                    if (objs.Count > 0)
                    {
                        FID = objs[0]["FID"].ToString();
                    }
                    Act_Push_Common("ora_CRM_MBL_CCRP", "售后服务", FID);
                }
                else if (pushFormId == "ora_CRM_MBL_TSDDJS") //特殊订单
                {
                    sql = "select FID from ora_CRM_SpcBussCost where FSaleOrderID='" + _FBillNo + "'";
                    var objs = DBUtils.ExecuteDynamicObject(this.Context, sql);
                    if(objs.Count > 0)
                    {
                        FID = objs[0]["FID"].ToString();
                    }
                    Act_Push_Common("ora_CRM_MBL_TSDDJS", "特殊订单业务费结算", FID);
                }
            });
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
                    if(result == MessageBoxResult.Yes)
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
                    if(result == MessageBoxResult.Yes)
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
