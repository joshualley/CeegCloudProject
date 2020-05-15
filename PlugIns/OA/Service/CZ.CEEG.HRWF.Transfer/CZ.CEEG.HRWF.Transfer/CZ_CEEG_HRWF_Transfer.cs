using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using Kingdee.BOS.Core.DynamicForm.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.Core.Validation;
using Kingdee.BOS.App.Data;
using Kingdee.BOS.Util;
using Kingdee.BOS.Core.Const;
using Kingdee.BOS.Core.Metadata;

namespace CZ.CEEG.HRWF.Transfer
{
    [Description("调职审核后修改员工职位")]
    [HotUpdate]
    public class CZ_CEEG_HRWF_Transfer : AbstractOperationServicePlugIn
    {
        public override void OnPreparePropertys(PreparePropertysEventArgs e)
        {
            base.OnPreparePropertys(e);
            e.FieldKeys.Add("FApplyID"); //#申请人
	        e.FieldKeys.Add("FInLevel"); //#调入职级
	        e.FieldKeys.Add("F_ora_InPost"); //#调入岗位
	        e.FieldKeys.Add("F_ora_InDept"); //#调入部门
        }

        public override void EndOperationTransaction(EndOperationTransactionArgs e)
        {
            base.EndOperationTransaction(e);
            string sql = "";
            foreach (var dataEntity in e.DataEntitys)
            {
                string FApplyID = (dataEntity["FApplyID"] as DynamicObject)["Id"].ToString();
                string FInLevel = (dataEntity["FInLevel"] as DynamicObject)["Id"].ToString();
                string F_ora_InPost = (dataEntity["F_ora_InPost"] as DynamicObject)["Id"].ToString();
                string F_ora_InDept = (dataEntity["F_ora_InDept"] as DynamicObject)["Id"].ToString();

                sql = string.Format(@"update T_HR_EMPINFO set F_HR_Rank='{0}',F_ORA_Post='{1}',F_ora_DeptID='{2}' where FID='{3}';",
                                             FInLevel, F_ora_InPost, F_ora_InDept, FApplyID);
                
            }
            DBUtils.ExecuteDynamicObject(this.Context, sql);

        }
    }
}
