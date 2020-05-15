using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System;

using Kingdee.BOS;
using Kingdee.BOS.Core;
using Kingdee.BOS.Core.DynamicForm;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Core.Metadata;
using Kingdee.BOS.Core.Bill.PlugIn;
using Kingdee.BOS.FileServer.Core;
using Kingdee.BOS.FileServer.Core.Object;
using Kingdee.BOS.FileServer.ProxyService;
using Kingdee.BOS.KDThread;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.ServiceHelper;
using Kingdee.BOS.Util;
using Kingdee.BOS.App.Data;
using Kingdee.BOS.Core.Bill;
using Kingdee.BOS.Core.DynamicForm.PlugIn.ControlModel;
using Kingdee.BOS.Workflow;
using Kingdee.BOS.Workflow.Elements;
using Kingdee.BOS.Workflow.Interface;
using Kingdee.BOS.Workflow.ServiceHelper;
using Kingdee.BOS.Workflow.Models.Chart;
using Kingdee.BOS.Workflow.Kernel;
using Kingdee.BOS.Workflow.Assignment;



namespace CZ.CEEG.BosTask.Dispatch
{
    [Description("任务派遣单")]
    [HotUpdate]
    public class CZ_CEEG_BosTask_Dispatch : AbstractBillPlugIn
    {

        #region override
        public override void AfterBindData(EventArgs e)
        {
            base.AfterBindData(e);
            if (CZ_GetValue("FDocumentStatus") != "Z")
            {
                HidenEntryRow();
            }
            else
            {
                this.View.Model.SetValue("FRespID", this.Context.UserId.ToString());
            }
            
        }

        public override void EntryBarItemClick(BarItemClickEventArgs e)
        {
            base.EntryBarItemClick(e);
            if (!isResp())
            {
                e.Cancel = true;
                this.View.ShowMessage("你没有操作的权限！");
            }
        }

        public override void DataChanged(DataChangedEventArgs e)
        {
            base.DataChanged(e);
            if (e.Field.Key == "FParticipant")
            {
                string FUserID = this.View.Model.GetValue("FParticipant", e.Row) == null ? "0" : (this.View.Model.GetValue("FParticipant", e.Row) as DynamicObject)["Id"].ToString();
                this.View.Model.SetValue("FUserID", FUserID, e.Row);
            }
        }

        public override void BeforeDoOperation(BeforeDoOperationEventArgs e)
        {
            base.BeforeDoOperation(e);
            switch (e.Operation.FormOperation.Operation.ToUpperInvariant())
            {
                case "SAVE":
                    if (!ValidateWeightSum())
                    {
                        e.Cancel = true;
                    }
                    SetParticipants();
                    break;
                case "SUBMIT":
                    if (!ValidateWeightSum())
                    {
                        e.Cancel = true;
                    }
                    SetParticipants();
                    break;
            }
        }

        #endregion

        #region 业务方法

        /// <summary>
        /// 设置参与人
        /// </summary>
        private void SetParticipants()
        {
            var entity = this.View.Model.DataObject["FEntity"] as DynamicObjectCollection;
            List<string> users = new List<string>();
            string ids = "";
            foreach (var row in entity)
            {
                string userId = (row["FParticipant"] as DynamicObject)["Id"].ToString();
                users.Add(userId);
                ids += userId + ",";
            }
            ids.TrimEnd(',');
            this.View.Model.SetValue("FUserIds", ids);
            this.View.Model.SetValue("FParticipants", users);
        }

        /// <summary>
        /// 验证权重和
        /// </summary>
        /// <returns></returns>
        private bool ValidateWeightSum()
        {
            var entity = this.View.Model.DataObject["FEntity"] as DynamicObjectCollection;
            float SumWeight = 0;
            foreach (var row in entity)
            {
                SumWeight += float.Parse(row["FEWeight"].ToString());
            }
            if (SumWeight != 100)
            {
                this.View.ShowErrMessage("权重之和必须为100！");
                return false;
            }

            return true;
        }


        /// <summary>
        /// 根据用户隐藏表体行
        /// </summary>
        private void HidenEntryRow()
        {
            if (!isResp())
            {
                string filterString = "FUserID='" + this.Context.UserId.ToString() + "'";
                EntryGrid grid = this.View.GetControl<EntryGrid>("FEntity");
                grid.SetFilterString(filterString);
            }
        }

        private bool isResp()
        {
            string userId = this.Context.UserId.ToString();
            if(userId == CZ_GetBaseData("FRespID", "Id"))
                return true;
            return false;
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
            return this.View.Model.DataObject[sign] == null ? "" : (this.View.Model.DataObject[sign] as DynamicObject)[property].ToString();
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


        #region 测试流程
        public override void AfterBarItemClick(AfterBarItemClickEventArgs e)
        {
            base.AfterBarItemClick(e);
            if (e.BarItemKey.ToUpperInvariant() == "TBWF")
            {
                string procInstId = WorkflowChartServiceHelper.GetProcInstIdByBillInst(this.Context, this.View.GetFormId(), CZ_GetFID());
                List<ChartActivityInfo> routeCollection = WorkflowChartServiceHelper.GetProcessRouter(this.Context, procInstId);
                var WFNode = routeCollection[routeCollection.Count - 1];
                //AssignmentServiceHelper.
                string name = this.Context.UserName;
                //GetApproveActions(this.View.GetFormId(), CZ_GetFID(), name);
            }
        }
        #endregion

    }
}