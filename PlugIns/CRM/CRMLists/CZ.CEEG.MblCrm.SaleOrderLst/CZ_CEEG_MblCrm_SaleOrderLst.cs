using Kingdee.BOS.App.Data;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Core.List;
using Kingdee.BOS.Core.List.PlugIn.Args;
using Kingdee.BOS.Mobile.PlugIn;
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
            string _filter = Act_SetCustFilter();
            e.FilterString = _filter;
            e.AppendQueryOrderby(" FCreateDate DESC");
            e.CustomFilter["FSelectAllOrg"] = true;
        }

        private string Act_SetCustFilter()
        {
            string userId = this.Context.UserId.ToString();

            string sql = string.Format("EXEC proc_czly_GetSalesmanIdByUserId @FUserId='{0}'", userId);

            var objs = DBUtils.ExecuteDynamicObject(this.Context, sql);
            var ids = new List<string>();
            foreach(var obj in objs)
            {
                ids.Add("'" + obj["FSalesmanId"].ToString() + "'");
            }
            string ids_str = "-1";
            if(ids.Count > 0)
            {
                ids_str = string.Join(",", ids);
            }

            string _filter = " FSALERID in (" + ids_str + ")";

            return _filter;
        }

        public override void OnInitialize(InitializeEventArgs e)
        {
            base.OnInitialize(e);
            
        }

        public override void BeforeF7Select(BeforeF7SelectEventArgs e)
        {
            base.BeforeF7Select(e);
            //取消组织隔离
            ((ListShowParameter)e.DynamicFormShowParameter).IsIsolationOrg = false;
        }
    }
}
