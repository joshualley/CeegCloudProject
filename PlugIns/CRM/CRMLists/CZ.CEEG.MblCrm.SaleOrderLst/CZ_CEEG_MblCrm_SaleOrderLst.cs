using Kingdee.BOS.App.Data;
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
            base.PrepareFilterParameter(e);
            string _filter = Act_SetCustFilter();
            e.AppendQueryFilter(_filter);
        }

        private string Act_SetCustFilter()
        {
            string userId = this.Context.UserId.ToString();

            string sql = string.Format("EXEC proc_czly_GetSalesmanIdByUserId @FUserId='{0}'", userId);

            var objs = DBUtils.ExecuteDynamicObject(this.Context, sql);
            var ids = new List<string>();
            foreach(var obj in objs)
            {
                ids.Add(obj["FSalesmanId"].ToString());
            }
            string ids_str = "-1";
            if(ids.Count > 0)
            {
                ids_str = string.Join(",", ids);
            }

            string _filter = " FSalerId in (" + ids_str + ")";

            //for (int i = 0; i < objs.Count; i++)
            //{
            //    if (i < objs.Count - 1)
            //        _filter += "'" + objs[i]["FSalesmanId"].ToString() + "',";
            //    else
            //        _filter += "'" + objs[i]["FSalesmanId"].ToString() + "'";
            //}

            //if (objs.Count <= 0)
            //    _filter += "'-1'";

            //_filter += ")";
            return _filter;
        }
    }
}
