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

namespace CZ.CEEG.SrvOA.Renewal
{
    [Description("续签反写")]
    [HotUpdate]
    public class CZ_CEEG_SrvOA_Renewal : AbstractOperationServicePlugIn
    {
        public override void OnPreparePropertys(PreparePropertysEventArgs e)
        {
            base.OnPreparePropertys(e);
            e.FieldKeys.Add("FEndDate");           //合同结束日期
            e.FieldKeys.Add("F_ora_SignYear");     //续签年限
            e.FieldKeys.Add("F_ora_Level");        //职级
            e.FieldKeys.Add("F_ora_Workplace");    //工作地点
            e.FieldKeys.Add("F_ora_ContractType"); //员工合同类型
            e.FieldKeys.Add("FApplyID");           //申请人
        }

        public override void EndOperationTransaction(EndOperationTransactionArgs e)
        {
            foreach (var entity in e.DataEntitys)
            {
                string FID = entity["Id"].ToString();
                string sql = "select * from ora_t_Renewal where FID='" + FID + "'";
                var objs = DBUtils.ExecuteDynamicObject(this.Context, sql);
                if(objs.Count > 0)
                {
                    string FEndDate = objs[0]["FEndDate"] == null ? "" : objs[0]["FEndDate"].ToString();
                    string F_ora_SignYear = objs[0]["F_ora_SignYear"].ToString();
                    if(FEndDate != "")
                    {
                        FEndDate = DateTime.Parse(FEndDate).AddYears(int.Parse(F_ora_SignYear)).ToString();
                    }

                    string F_ora_Level = objs[0]["F_ora_Level"].ToString();
                    string F_ora_ContractType = objs[0]["F_ora_ContractType"].ToString();
                    string F_ora_Workplace = objs[0]["F_ora_Workplace"].ToString();
                    string FApplyID = objs[0]["FApplyID"].ToString();
                    sql = string.Format(@"UPDATE T_HR_EMPINFO SET F_HR_RANK='{0}' WHERE FID='{1}';
			        UPDATE T_BD_PERSON SET FWORKADDRESS='{2}', FCONTRACTTYPE='{3}', FHTDateEnd='{4}' WHERE FID='{5}';",
                    F_ora_Level, FApplyID, F_ora_Workplace, F_ora_ContractType, FEndDate, FApplyID);
                    DBUtils.Execute(this.Context, sql);
                }

                
            }
        }
    }
}
