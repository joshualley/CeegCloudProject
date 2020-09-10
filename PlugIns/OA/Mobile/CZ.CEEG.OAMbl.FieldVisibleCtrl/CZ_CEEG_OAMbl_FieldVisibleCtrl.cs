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

        #region Actions
        /// <summary>
        /// 根据流程显示隐藏按钮
        /// </summary>
        private void HideBtn()
        {
            string _FDocumentStatus = this.CZ_GetValue("FDocumentStatus");

            if (_FDocumentStatus == "Z" || _FDocumentStatus == "A")
            {
                var submitBtn = this.View.GetControl("FSubmitBtn");
                var saveBtn = this.View.GetControl("FSaveBtn");
                saveBtn.Visible = false;
                submitBtn.SetCustomPropertyValue("width", 310);
            }
            else if (_FDocumentStatus == "B")
            {
                var submitBtn = this.View.GetControl("FSubmitBtn");
                var saveBtn = this.View.GetControl("FSaveBtn");
                submitBtn.Visible = false;
                saveBtn.SetCustomPropertyValue("width", 310);
            }else if (_FDocumentStatus == "D")
            {
                string FID = this.View.BillModel.DataObject["Id"].ToString();
                string procInstId = WorkflowChartServiceHelper.GetProcInstIdByBillInst(this.Context, this.View.GetFormId(), FID);
                string sql = "select FSTATUS from t_WF_ProcInst where FPROCINSTID='" + procInstId + "'";
                var data = CZDB_GetData(sql);
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
        #endregion

        #region overrides
        public override void AfterBindData(EventArgs e)
        {
            base.AfterBindData(e);
            HideBtn();
        }

        #endregion

        #region 基本取数方法
        /// <summary>
        /// 获取当前单据FID
        /// </summary>
        /// <returns></returns>
        public string CZ_GetFID()
        {
            return (this.View.BillModel.DataObject as DynamicObject)["Id"] == null ? "0" : (this.View.BillModel.DataObject as DynamicObject)["Id"].ToString();
        }
        public string CZ_GetValue(string sign)
        {
            return this.View.BillModel.GetValue(sign) == null ? "" : this.View.BillModel.GetValue(sign).ToString();
        }
        /// <summary>
        /// 获取基础资料
        /// </summary>
        /// <param name="sign">标识</param>
        /// <param name="property">属性</param>
        /// <returns></returns>
        private string CZ_GetBaseData(string sign, string property)
        {
            return this.View.BillModel.DataObject[sign] == null ? "" : (this.View.BillModel.DataObject[sign] as DynamicObject)[property].ToString();
        }
        /// <summary>
        /// 获取一般字段
        /// </summary>
        /// <param name="sign">标识</param>
        /// <returns></returns>
        private string CZ_GetCommonField(string sign)
        {
            return this.View.BillModel.DataObject[sign] == null ? "" : this.View.BillModel.DataObject[sign].ToString();
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
