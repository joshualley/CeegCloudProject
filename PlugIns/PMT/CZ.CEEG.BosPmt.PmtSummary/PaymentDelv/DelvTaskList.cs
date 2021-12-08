using Kingdee.BOS.App.Data;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Core.List.PlugIn;
using Kingdee.BOS.Core.List.PlugIn.Args;
using Kingdee.BOS.Util;
using System.ComponentModel;

namespace CZ.CEEG.BosPmt.PaymentDelv
{
    [HotUpdate]
    [Description("货款移交任务单列表")]
    public class DelvTaskList : AbstractListPlugIn
    {
        private string filterStr = "";

        public override void PrepareFilterParameter(FilterArgs e)
        {
            base.PrepareFilterParameter(e);
            if (filterStr == "")
            {
                // 默认显示承办人为当前用户的
                long userId = this.Context.UserId;
                string sql = string.Format(@"select FNumber from t_SEC_role r 
		inner join T_SEC_ROLEUSER ru on ru.FRoleId=r.FRoleId
		where r.FNumber='999' and ru.FUserId={0}", userId);
                var roles = DBUtils.ExecuteDynamicObject(Context, sql);
                if (roles.Count <= 0)
                {
                    filterStr = $"(FCreatorId={userId} or FExecutorId={userId}) and FDocumentStatus<>'C'";
                }
            }
            e.AppendQueryFilter(filterStr);
        }

        public override void AfterBarItemClick(AfterBarItemClickEventArgs e)
        {
            base.AfterBarItemClick(e);
            long userId = this.Context.UserId;
            switch (e.BarItemKey.ToUpperInvariant()) 
            {
                case "ORA_TBMYMALLOC":  // 我的分配的
                case "ORA_TBIMYMALLOC": // 我的分配的
                    filterStr = $"FCreatorId={userId} and FDocumentStatus<>'C'";
                    this.View.RefreshByFilter();
                    break;
                case "ORA_TBIMYTASK":   // 我的任务
                    filterStr = $"FExecutorId={userId} and FDocumentStatus<>'C'";
                    this.View.RefreshByFilter();
                    break;
                case "ORA_TBIFINISH": // 已结案任务
                    filterStr = $"(FCreatorId={userId} or FExecutorId={userId}) and FDocumentStatus='C'";
                    this.View.RefreshByFilter();
                    break;
                case "ORA_TBALL": // 全部执行中任务
                    filterStr = $"(FCreatorId={userId} or FExecutorId={userId}) and FDocumentStatus<>'C'";
                    this.View.RefreshByFilter();
                    break;
            }
        }
    }
}