using CZ.Utils;
using Kingdee.BOS.App.Data;
using Kingdee.BOS.Core.DynamicForm.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.Util;
using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace CZ.CEEG.BosCrm.CustCopy
{
    [Description("客户复制到其他系统")]
    [HotUpdate]
    public class CZ_CEEG_BosCrm_CustCopy : AbstractOperationServicePlugIn
    {
        public override void EndOperationTransaction(EndOperationTransactionArgs e)
        {
           
        }

        public override void AfterExecuteOperationTransaction(AfterExecuteOperationTransaction e)
        {
            CopyCus(e.DataEntitys[0]);
        }

        public string genInsertSql(DynamicObject obj,string TABLE_NAME) {

            List<string> fieldNameList = new List<string>();
            List<string> fieldValueList = new List<string>();

            foreach (var propery in obj.DynamicObjectType.Properties)
            {
                fieldNameList.Add(propery.Name);
                string value = obj[propery.Name].ToString();
                if(value.Equals("0001-01-01 00:00:00")){
                    value = "";
                }
                fieldValueList.Add("'"+value+"'");
            }

            return string.Format(
                "INSERT INTO {0}({1}) VALUES({2});\n",
                TABLE_NAME,
                string.Join(", ", fieldNameList),
                string.Join(", ", fieldValueList)
            );
        }


        private void CopyCus(DynamicObject cus)
        {
            //根据客户id查数据库
            String cusSql = "select * from T_BD_CUSTOMER where FCUSTID = " + cus["id"];

            var objs = DBUtils.ExecuteDynamicObject(this.Context, cusSql);

            if (objs.Count > 0) {
                var cusDb = objs[0];
                String sql = this.genInsertSql(cusDb, "T_BD_CUSTOMER2");
                SqlUtil.ExecuteNonQuery(sql);
            }
        }
    }
}
