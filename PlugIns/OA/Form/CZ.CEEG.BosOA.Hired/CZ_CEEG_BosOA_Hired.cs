using Kingdee.BOS.App.Data;
using Kingdee.BOS.Core.Bill.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.Util;
using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace CZ.CEEG.BosOA.Hired
{
    [Description("录用获取单位总经理")]
    [HotUpdate]
    public class CZ_CEEG_BosOA_Hired : AbstractBillPlugIn
    {
        public override void DataChanged(DataChangedEventArgs e)
        {
            base.DataChanged(e);
            string key = e.Field.Key.ToString();
            switch (key)
            {
                case "FJoinOrgId":
                    Act_GetGManager();
                    break;
                case "FJoinDept":
                    Act_GetGManager();
                    break;
            }
        }


        public override void BeforeDoOperation(BeforeDoOperationEventArgs e)
        {
            base.BeforeDoOperation(e);
            string op = e.Operation.FormOperation.Operation.ToUpperInvariant();
            switch (op)
            {
                case "SAVE":
                    Act_GetGManager();
                    break;
                case "SUBMIT":
                    Act_GetGManager();
                    break;
            }

        }

        /// <summary>
        /// 获取并设置单位总经理
        /// </summary>
        private void Act_GetGManager()
        {
            if (this.Context.ClientType.ToString() != "Mobile")
            {
                string FJoinOrgId = this.View.Model.GetValue("FJoinOrgId") == null ? "0" : (this.View.Model.GetValue("FJoinOrgId") as DynamicObject)["Id"].ToString();
                string FJoinDept = this.View.Model.GetValue("FJoinDept") == null ? "0" : (this.View.Model.GetValue("FJoinDept") as DynamicObject)["Id"].ToString();
                string sql = string.Format("EXEC proc_czly_GetGManager @FOrgId='{0}', @FDeptId='{1}'", FJoinOrgId, FJoinDept);
                var data = DBUtils.ExecuteDynamicObject(this.Context, sql);
                if (data.Count > 0)
                {
                    string FManager = data[0]["FID"].ToString();
                    this.View.Model.SetValue("FManager", FManager);
                }
            }


        }

    }
}
