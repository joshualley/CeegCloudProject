using Kingdee.BOS.App.Data;
using Kingdee.BOS.Core.DynamicForm.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.JSON;
using Kingdee.BOS.Util;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace CZ.CEEG.Report.CostAccount
{
    [Description("费用台账报表表单构建")]
    [HotUpdate]
    public class CZ_CEEG_Report_CostAccountBuilder : AbstractDynamicWebFormBuilderPlugIn
    {
        public override void CreateControl(CreateControlEventArgs e)
        {
            base.CreateControl(e);
            if(e.ControlAppearance.OriginKey == "FEntity")
            {
                // 显示表体过滤行
                //e.Control.Put("showFilterRow", true);
                // 查询数据
                
                string FSDate = this.ParentPageView.OpenParameter.GetCustomParameter("FSDate") == null ? "" :
                this.ParentPageView.OpenParameter.GetCustomParameter("FSDate").ToString();
                string FEDate = this.ParentPageView.OpenParameter.GetCustomParameter("FEDate") == null ? "" :
                    this.ParentPageView.OpenParameter.GetCustomParameter("FEDate").ToString();
                string FOrgId = this.ParentPageView.OpenParameter.GetCustomParameter("FOrgId") == null ? "0" :
                    this.ParentPageView.OpenParameter.GetCustomParameter("FOrgId").ToString();
                string FDeptID = this.ParentPageView.OpenParameter.GetCustomParameter("FDeptID") == null ? "0" :
                    this.ParentPageView.OpenParameter.GetCustomParameter("FDeptID").ToString();
                string FAccountId = this.ParentPageView.OpenParameter.GetCustomParameter("FAccountId") == null ? "0" :
                    this.ParentPageView.OpenParameter.GetCustomParameter("FAccountId").ToString();

                string sql = string.Format(@"EXEC proc_czly_AccountDept @SDt='{0}', @EDt='{1}', 
@FOrgId='{2}', @FDeptId='{3}', @FAccountId='{4}'",
                        FSDate, FEDate, FOrgId, FDeptID, FAccountId);
                var entityData = DBUtils.ExecuteDataSet(this.Context, sql).Tables[0];
                
                // 动态添加合计列
                JSONArray sumFields = new JSONArray();
                for (int i = 1; i < entityData.Columns.Count; i++)
                {
                    string name = "FField_" + (i + 1).ToString();
                    JSONObject sumObj = new JSONObject();
                    sumObj["fieldKey"] = name;
                    sumObj["sumType"] = 1;
                    sumFields.Add(sumObj);
                }

                JSONArray columnsInfo = new JSONArray();
                JSONObject infoObj = new JSONObject();
                infoObj["groupSumColums"] = sumFields;
                columnsInfo.Add(infoObj);
                e.Control["groupColumnsInfo"] = columnsInfo;
                
            }
        }
    }
}
