using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System;

using Kingdee.BOS.Core;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Core.DynamicForm.PlugIn.ControlModel;
using Kingdee.BOS.Core.Metadata;
using Kingdee.BOS.Core.Bill.PlugIn;
using Kingdee.BOS.KDThread;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.ServiceHelper;
using Kingdee.BOS.Util;
using Kingdee.BOS.App.Data;
using Kingdee.BOS.ServiceHelper.ApplicationInitialization;
using Kingdee.BOS.Core.DynamicForm;
using Kingdee.BOS.Core.Bill;

/*
1.获取用户默认部门					
2.由默认部门查询部门下员工及其主任岗信息，并获取其用户ID					
3.通过用户id及日期筛选获取个人汇报单主键和考核得分					
4.将数据写入表体行					
5.查询明细时由行中汇报单主键打开汇报单					
 */

namespace CZ.CEEG.BosTask.PfmReview
{
    [Description("绩效复核单")]
    [HotUpdate]
    public class CZ_CEEG_BosTask_PfmReview : AbstractBillPlugIn
    {
        #region overrides

        public override void AfterBindData(EventArgs e)
        {
            base.AfterBindData(e);
            /*string orgId = CZ_GetBaseData("FOrgId", "Id");
            var currdate = DateTime.Now;
            string date = "";
            if (currdate.Month <= 10)
            {
                if (currdate.Month == 1)
                {
                    date = string.Format("{0}-{1}-01", currdate.Year, 12);
                }
                else
                {
                    date = string.Format("{0}-0{1}-01", currdate.Year, currdate.Month - 1);
                }
            }
            else
            {
                date = string.Format("{0}-{1}-01", currdate.Year, currdate.Month - 1);
            }
            this.View.Model.SetValue("FPeriod", date);*/
        }

        public override void DataChanged(DataChangedEventArgs e)
        {
            base.DataChanged(e);
            switch(e.Field.Key)
            {
                case "FOrgId":
                case "FDeptId":
                case "FPeriod":
                    Act_GetPerformanceInfo();
                    break;
            }
        }

        public override void EntryBarItemClick(BarItemClickEventArgs e)
        {
            base.EntryBarItemClick(e);
            switch (e.BarItemKey.ToString())
            {
                case "tbViewPr":
                    OpenPsnReportForm();
                    break;
            }
            
        }

        public override void EntityRowDoubleClick(EntityRowClickEventArgs e)
        {
            base.EntityRowDoubleClick(e);
            OpenPsnReportForm();
        }

        public override void BeforeDoOperation(BeforeDoOperationEventArgs e)
        {
            base.BeforeDoOperation(e);
            string key = e.Operation.FormOperation.Operation.ToUpperInvariant();
            switch(key)
            {
                case "SAVE":
                case "SUBMIT":
                    if(!Validate())
                    {
                        e.Cancel = true;
                    }
                    break;
            }
        }

        #endregion

        #region 业务方法

        private bool Validate()
        {
            var entity = this.View.Model.DataObject["FEntity"] as DynamicObjectCollection;
            var names = entity.Where(row => Convert.ToDecimal(row["FESCORE"]) == 0)
                .Select(row => (row["FEEmpId"] as DynamicObject)?["Name"].ToString() ?? "")
                .ToArray();

            if (names.Length > 0)
            {
                string msg = "员工:\n" + string.Join(",", names) + "\n的工作计划还未提交！";
                this.View.ShowWarnningMessage(msg);
                return false;
            }
            return true;
        }

        private void OpenPsnReportForm()
        {
            DynamicObject rowData;
            int rowIndex;
            this.Model.TryGetEntryCurrentRow("FEntity", out rowData, out rowIndex);
            if (rowData != null)
            {
                string fid = rowData["FEPrPk"].ToString();
                BillShowParameter para = new BillShowParameter();
                para.FormId = "ora_Task_PersonalReport";
                para.OpenStyle.ShowType = ShowType.Modal;
                para.ParentPageId = this.View.PageId;
                para.PKey = fid;
                para.Status = OperationStatus.VIEW;
                this.View.ShowForm(para);
            }
        }

        /// <summary>
        /// 获取绩效信息
        /// </summary>
        /// <param name="FOrgId"></param>
        /// <param name="Date"></param>
        private void Act_GetPerformanceInfo()
        {
            this.View.Model.DeleteEntryData("FEntity");

            string FOrgId = CZ_GetBaseData("FOrgId", "Id");
            string FDept = CZ_GetBaseData("FDeptId", "Id");
            string FPeriod = CZ_GetCommonField("FPeriod");
            if (FOrgId == "0" && FPeriod == "")
            {
                return;
            }
            string sql = "";
            //设置单位总经理
            sql = string.Format("EXEC proc_czly_GetGManager @FOrgId='{0}', @FDeptId='{1}'", FOrgId, FDept);
            var data = CZDB_GetData(sql);
            if(data.Count > 0)
            {
                this.View.Model.SetValue("FGManager", data[0]["FID"].ToString());
            }

            //获取员工绩效
            sql = string.Format("exec proc_czly_GetPerformanceInfo @FOrgId='{0}',@FDeptId='{1}',@Date='{2}'", FOrgId, FDept, FPeriod);
            var objs = CZDB_GetData(sql);
            
            
            for (int i = 0; i < objs.Count; i++)
            {
                this.View.Model.CreateNewEntryRow("FEntity");
                this.View.Model.SetValue("FEOrgId", FOrgId, i);
                this.View.Model.SetValue("FEEmpId", objs[i]["FEmpId"].ToString(), i);
                this.View.Model.SetValue("FEDeptId", objs[i]["FDeptId"].ToString(), i);
                this.View.Model.SetValue("FEPostId", objs[i]["FPostId"].ToString(), i);
                this.View.Model.SetValue("FEScore", objs[i]["FScore"].ToString(), i);
                this.View.Model.SetValue("FEPrPk", objs[i]["FPrPk"].ToString(), i);
            }
            this.View.UpdateView("FEntity");
        }

        #endregion

        #region 基本取数方法
        /// <summary>
        /// 获取当前单据FID
        /// </summary>
        /// <returns></returns>
        public string CZ_GetFID()
        {
            return (this.View.Model.DataObject as DynamicObject)["Id"] == null ? "0" : (this.View.Model.DataObject as DynamicObject)["Id"].ToString();
        }
        public string CZ_GetValue(string sign)
        {
            return this.View.Model.GetValue(sign) == null ? "" : this.View.Model.GetValue(sign).ToString();
        }
        /// <summary>
        /// 获取基础资料
        /// </summary>
        /// <param name="sign">标识</param>
        /// <param name="property">属性</param>
        /// <returns></returns>
        private string CZ_GetBaseData(string sign, string property)
        {
            return this.View.Model.DataObject[sign] == null ? "0" : (this.View.Model.DataObject[sign] as DynamicObject)[property].ToString();
        }
        /// <summary>
        /// 获取一般字段
        /// </summary>
        /// <param name="sign">标识</param>
        /// <returns></returns>
        private string CZ_GetCommonField(string sign)
        {
            return this.View.Model.DataObject[sign] == null ? "" : this.View.Model.DataObject[sign].ToString();
        }
        #endregion

        #region 数据库查询
        /// <summary>
        /// 基本方法 
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
