using Kingdee.BOS.App.Data;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Core.List;
using Kingdee.BOS.Core.List.PlugIn.Args;
using Kingdee.BOS.Mobile.PlugIn;
using Kingdee.BOS.ServiceHelper;
using Kingdee.BOS.Util;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace CZ.CEEG.MblCrm.SaleOrderLst
{
    [Description("销售订单列表过滤")]
    [HotUpdate]
    public class CZ_CEEG_MblCrm_SaleOrderLst : AbstractMobileListPlugin
    {
        public override void PrepareFilterParameter(FilterArgs e)
        {
            var filter = Act_SetCustFilter();
            if (this.Context.UserId.ToString() == "100560")
            {
                return;
            }
            //e.CustomFilter["FSelectAllOrg"] = true;
            e.AppendQueryFilter(filter);
            e.AppendQueryOrderby(" FCreateDate DESC");
            // this.View.ShowMessage(filter);
            
        }

        public override void AfterCreateSqlBuilderParameter(SqlBuilderParameterArgs e)
        {
            base.AfterCreateSqlBuilderParameter(e);
            e.sqlBuilderParameter.IsIsolationOrg = true;

            var orgs = PermissionServiceHelper.GetUserOrg(this.Context);
            var list = new List<long>();
            orgs.ForEach(org => list.Add(org.Id));
            e.sqlBuilderParameter.IsolationOrgList = list;
            
        }

        private string Act_SetCustFilter()
        {
            string userId = this.Context.UserId.ToString();
            string sql = string.Format(@"select u.FUserId, u.FName, c.FNumber
from (select FUserId,FName,FLinkObject from T_SEC_USER where FUSERID={0}) u
inner join V_bd_ContactObject c on u.FLINKOBJECT=c.FID", userId);
            var items = DBUtils.ExecuteDynamicObject(Context, sql).FirstOrDefault();
            string filter = items == null ? "" : $" FSALERID.FNumber='{items["FNumber"]}' ";

            return filter;
        }
    }
}
