using Kingdee.BOS.App.Data;
using Kingdee.BOS.Core.Bill.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.Util;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace CZ.CEEG.BosOA.WorkContract
{
    [HotUpdate]
    [Description("工作联系单，设置被联系组织单位总经理")]
    public class CZ_CEEG_BosOA_WorkContract : AbstractBillPlugIn
    {

        public override void DataChanged(DataChangedEventArgs e)
        {
            base.DataChanged(e);
            string key = e.Field.Key.ToString();
            switch (key)
            {
                case "FContractOrgId":
                    Act_GetGManager();
                    break;
                case "FContractCompany":
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
        /// 获取并设置被联系单位总经理
        /// </summary>
        private void Act_GetGManager()
        {
            //if (this.Context.ClientType.ToString() != "Mobile")
            //{
            string FContractOrgId = this.View.Model.GetValue("FContractOrgId") == null ? "0" : (this.View.Model.GetValue("FContractOrgId") as DynamicObject)["Id"].ToString();
            string FContractCompany = this.View.Model.GetValue("FContractCompany") == null ? "0" : (this.View.Model.GetValue("FContractCompany") as DynamicObject)["Id"].ToString();
            string sql = string.Format("EXEC proc_czly_GetGManager @FOrgId='{0}', @FDeptId='{1}'", FContractOrgId, FContractCompany);
            var data = DBUtils.ExecuteDynamicObject(this.Context, sql);
            if (data.Count > 0)
            {
                string FManager = data[0]["FID"].ToString();
                this.View.Model.SetValue("FManager1", FManager);
            }
            //}


        }
    }
}
