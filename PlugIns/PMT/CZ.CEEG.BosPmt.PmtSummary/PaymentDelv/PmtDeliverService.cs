using Kingdee.BOS.App.Data;
using Kingdee.BOS.Core.DynamicForm.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Util;
using System.ComponentModel;

namespace CZ.CEEG.BosPmt.PmtSummary.PaymentDelv
{
    [HotUpdate]
    [Description("货款移交单-服务")]
    public class PmtDeliverService: AbstractOperationServicePlugIn
    {
        public override void EndOperationTransaction(EndOperationTransactionArgs e)
        {
            base.EndOperationTransaction(e);
            string sql;
            foreach (var dataEntity in e.DataEntitys)
            {
                string fid = dataEntity["Id"].ToString();
                switch (this.FormOperation.Operation.ToUpperInvariant())
                {
                    case "DONOTHINGZJ": // 转交
                        sql = $"update ora_PMT_Deliver set FDeliverType=2 where FID={fid}";
                        DBUtils.Execute(Context, sql);
                        break;
                    case "DONOTHINGYJ": // 移交
                        sql = $"update ora_PMT_Deliver set FDeliverType=1 where FID={fid}";
                        DBUtils.Execute(Context, sql);
                        break;
                    case "DONOTHINGWB": // 外包
                        sql = $"update ora_PMT_Deliver set FDeliverType=3 where FID={fid}";
                        DBUtils.Execute(Context, sql);
                        break;
                    case "UNAUDIT": // 退回
                        sql = $"update ora_PMT_Deliver set FDeliverType=4 where FID={fid}";
                        DBUtils.Execute(Context, sql);
                        break;
                }
            }
        }
    }
}