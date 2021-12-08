using Kingdee.BOS;
using Kingdee.BOS.App.Data;
using Kingdee.BOS.Core.DynamicForm.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Util;
using System.ComponentModel;

namespace CZ.CEEG.BosPmt.PmtSummary.PaymentDelv
{
    [HotUpdate]
    [Description("货款移交任务单-服务")]
    public class DelvTaskService: AbstractOperationServicePlugIn
    {

        public override void BeginOperationTransaction(BeginOperationTransactionArgs e)
        {
            base.BeginOperationTransaction(e);
            foreach (var dataEntity in e.DataEntitys)
            {
                string op = this.FormOperation.Operation.ToUpperInvariant();
                string fid = dataEntity["Id"].ToString();
                if (op.Equals("DELETE"))
                {
                    string sql = string.Format(@"select * from ora_PMT_DelvTask where FID='{0}' and FCreatorId='{1}'", fid, this.Context.UserId);
                    var r = DBUtils.ExecuteDynamicObject(Context, sql);
                    if (r.Count <= 0)
                    {
                        throw new KDBusinessException("001", "仅允许分配人进行删除！");
                    }
                }
            }
        }

        public override void EndOperationTransaction(EndOperationTransactionArgs e)
        {
            base.EndOperationTransaction(e);
            foreach (var dataEntity in e.DataEntitys)
            {
                string op = this.FormOperation.Operation.ToUpperInvariant();
                string fid = dataEntity["Id"].ToString();
                if (op.Equals("SAVE"))
                {
                    // 保存后反写 累计追回货款
                    string sql = string.Format(@"/*dialect*/
                        update d set d.FBackPmt=dt.FBackPmt, d.FBillStatus='B'
                        from ora_PMT_Deliver d 
                        inner join ora_PMT_DelvTask dt on d.FBillNo=dt.FSourceBillNo
                        where dt.FID={0}", fid);
                    DBUtils.Execute(Context, sql);
                }
            }
        }
    }
}
