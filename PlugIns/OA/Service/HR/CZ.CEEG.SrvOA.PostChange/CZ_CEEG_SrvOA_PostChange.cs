using Kingdee.BOS.Util;
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

namespace CZ.CEEG.SrvOA.PostChange
{
    [Description("调职反写")]
    [HotUpdate]
    public class CZ_CEEG_SrvOA_PostChange : AbstractOperationServicePlugIn
    {
        public override void OnPreparePropertys(PreparePropertysEventArgs e)
        {
            base.OnPreparePropertys(e);
            e.FieldKeys.Add("F_ora_InPost"); //调入岗位
            e.FieldKeys.Add("FInLevel");     //调入职级
            e.FieldKeys.Add("F_ora_InDept"); //调入部门
            e.FieldKeys.Add("F_ora_AfterAddr"); //调职后工作地点
            e.FieldKeys.Add("F_ora_Type"); //员工合同类型
            e.FieldKeys.Add("FApplyID"); //申请人
        }

        public override void EndOperationTransaction(EndOperationTransactionArgs e)
        {
            foreach (var entity in e.DataEntitys)
            {
                string F_ora_InPost = entity["F_ora_InPost"] == null ? "0" : (entity["F_ora_InPost"] as DynamicObject)["Id"].ToString();
                string FInLevel = entity["FInLevel"] == null ? "0" : (entity["FInLevel"] as DynamicObject)["Id"].ToString();
                string F_ora_InDept = entity["F_ora_InDept"] == null ? "0" : (entity["F_ora_InDept"] as DynamicObject)["Id"].ToString();
                string F_ora_AfterAddr = entity["F_ora_AfterAddr"] == null ? "" : entity["F_ora_AfterAddr"].ToString();
                string F_ora_Type = entity["F_ora_Type"] == null ? "0" : (entity["F_ora_Type"] as DynamicObject)["Id"].ToString(); 
                string FApplyID = entity["FApplyID"] == null ? "0" : (entity["FApplyID"] as DynamicObject)["Id"].ToString();
                string sql = string.Format(@"UPDATE T_HR_EMPINFO SET F_HR_RANK = '{0}', F_ORA_POST = '{1}', F_ORA_DEPTID = '{2}'
                WHERE FID = '{3}';
                UPDATE T_BD_PERSON SET FWORKADDRESS = '{4}', FCONTRACTTYPE = '{5}'
                WHERE FID = '{6}';", FInLevel, F_ora_InPost, F_ora_InDept, FApplyID, F_ora_AfterAddr, F_ora_Type, FApplyID);
                DBUtils.ExecuteDynamicObject(this.Context, sql);
            }
        }

    }
}
