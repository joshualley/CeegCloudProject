using Kingdee.BOS.App.Data;
using Kingdee.BOS.Core.DynamicForm.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.Util;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace CZ.CEEG.SrvOA.RegularWork
{
    [Description("转正反写员工信息")]
    [HotUpdate]
    public class CZ_CEEG_SrvOA_RegularWork : AbstractOperationServicePlugIn
    {
        public override void OnPreparePropertys(PreparePropertysEventArgs e)
        {
            base.OnPreparePropertys(e);
            e.FieldKeys.Add("FAfterDeptID");   //转正后部门
            e.FieldKeys.Add("FAfterPost");     //转正后岗位
            e.FieldKeys.Add("FAfterLevel");    //职级
            e.FieldKeys.Add("FProbation");     //试用期
            e.FieldKeys.Add("F_ora_toDate");   //转正日期
            e.FieldKeys.Add("FApplyID");       //申请人
        }

        public override void EndOperationTransaction(EndOperationTransactionArgs e)
        {
            foreach (var entity in e.DataEntitys)
            {
                string FAfterDeptID = entity["FAfterDeptID"] == null ? "0" : (entity["FAfterDeptID"] as DynamicObject)["Id"].ToString();
                string FAfterPost = entity["FAfterPost"] == null ? "0" : (entity["FAfterPost"] as DynamicObject)["Id"].ToString();
                string FAfterLevel = entity["FAfterLevel"] == null ? "0" : (entity["FAfterLevel"] as DynamicObject)["Id"].ToString();
                string FProbation = entity["FProbation"] == null ? "" : entity["FProbation"].ToString();
                string F_ora_toDate = entity["F_ora_toDate"] == null ? "" : entity["F_ora_toDate"].ToString();
                string FApplyID = entity["FApplyID"] == null ? "0" : (entity["FApplyID"] as DynamicObject)["Id"].ToString();
                string sql = string.Format(@"UPDATE T_HR_EMPINFO SET F_HR_RANK='{0}', F_ORA_POST='{1}', F_ORA_DEPTID='{2}' WHERE FID='{3}';
			    UPDATE T_BD_PERSON SET FProbation='{4}', F_ora_toDate='{5}' WHERE FID='{6}';",
                FAfterLevel, FAfterPost, FAfterDeptID, FApplyID, FProbation, F_ora_toDate, FApplyID);
                DBUtils.ExecuteDynamicObject(this.Context, sql);
            }
        }
    }
}
