using Kingdee.BOS.Util;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

using Kingdee.BOS.Core.Const;
using Kingdee.BOS.Core.Metadata;
using Kingdee.BOS.Workflow.Interface;
using Kingdee.BOS.Workflow.ServiceHelper;
using Kingdee.BOS.Workflow.Models.Chart;
using Kingdee.BOS.Workflow.Kernel;

using Kingdee.BOS.Core;
using Kingdee.BOS.Core.Bill.PlugIn;
using Kingdee.BOS.KDThread;
using Kingdee.BOS.Mobile;
using Kingdee.BOS.Mobile.Metadata.ControlDataEntity;
using Kingdee.BOS.Mobile.PlugIn;
using Kingdee.BOS.Mobile.PlugIn.ControlModel;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.ServiceHelper;
using Kingdee.BOS.App.Data;
using Kingdee.BOS.Mobile.Metadata;

namespace CZ.CEEG.OAMbl.FieldVisibleCtrl
{
    [Description("Mbl按钮显示隐藏控制")]
    [HotUpdate]
    public class CZ_CEEG_OAMbl_FieldVisibleCtrl : AbstractMobileBillPlugin
    {

        /// <summary>
        /// 根据流程显示隐藏按钮
        /// </summary>
        private void HideBtn()
        {
            string _FDocumentStatus = this.View.BillModel.GetValue("FDocumentStatus").ToString();

            if (_FDocumentStatus == "Z" || _FDocumentStatus == "A")
            {
                try
                {
                    var pushBtn = this.View.GetControl("FPushBtn");
                    pushBtn.Visible = false;
                }
                catch (Exception) { }
                var submitBtn = this.View.GetControl("FSubmitBtn");
                var saveBtn = this.View.GetControl("FSaveBtn");
                saveBtn.Visible = false;
                submitBtn.SetCustomPropertyValue("width", 310);
            }
            else if (_FDocumentStatus == "B")
            {
                try
                {
                    var pushBtn = this.View.GetControl("FPushBtn");
                    pushBtn.Visible = false;
                }
                catch (Exception) { }
                var submitBtn = this.View.GetControl("FSubmitBtn");
                var saveBtn = this.View.GetControl("FSaveBtn");
                submitBtn.Visible = false;
                saveBtn.SetCustomPropertyValue("width", 310);
            }
            else if (_FDocumentStatus == "C")
            {
                var submitBtn = this.View.GetControl("FSubmitBtn");
                var saveBtn = this.View.GetControl("FSaveBtn");
                submitBtn.Visible = false;
                saveBtn.Visible = false;
                try
                {
                    var pushBtn = this.View.GetControl("FPushBtn");
                    pushBtn.Visible = true;
                    pushBtn.SetCustomPropertyValue("width", 310);
                }
                catch (Exception) { }
               
            }
            else if (_FDocumentStatus == "D")
            {
                try
                {
                    var pushBtn = this.View.GetControl("FPushBtn");
                    pushBtn.Visible = false;
                }
                catch (Exception) { }
                string FID = this.View.BillModel.DataObject["Id"].ToString();
                string procInstId = WorkflowChartServiceHelper.GetProcInstIdByBillInst(this.Context, this.View.GetFormId(), FID);
                string sql = "select FSTATUS from t_WF_ProcInst where FPROCINSTID='" + procInstId + "'";
                var data = DBUtils.ExecuteDynamicObject(this.Context, sql);
                if (data.Count > 0 && data[0]["FSTATUS"].ToString() == "1")
                {
                    var submitBtn = this.View.GetControl("FSubmitBtn");
                    var saveBtn = this.View.GetControl("FSaveBtn");
                    saveBtn.Visible = false;
                    submitBtn.SetCustomPropertyValue("width", 310);
                }
                else
                {
                    var submitBtn = this.View.GetControl("FSubmitBtn");
                    var saveBtn = this.View.GetControl("FSaveBtn");
                    submitBtn.Visible = false;
                    saveBtn.SetCustomPropertyValue("width", 310);
                }
            }

        }

        public override void AfterBindData(EventArgs e)
        {
            base.AfterBindData(e);
            HideBtn();
        }

    }
}
