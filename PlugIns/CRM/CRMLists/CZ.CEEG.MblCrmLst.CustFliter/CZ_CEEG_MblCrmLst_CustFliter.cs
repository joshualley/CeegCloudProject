using Kingdee.BOS.App.Data;
using Kingdee.BOS.Core;
using Kingdee.BOS.Core.List.PlugIn.Args;
using Kingdee.BOS.Mobile.PlugIn;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.Util;
using System.ComponentModel;

namespace CZ.CEEG.MblCrmLst.CustFliter
{
    [Description("过滤客户")]
    [HotUpdate]
    public class CZ_CEEG_MblCrmLst_CustFliter : AbstractMobileListPlugin
    {
        public override void PrepareFilterParameter(FilterArgs e)
        {
            base.PrepareFilterParameter(e);
            string _filter = Act_SetCustFilter();
            e.AppendQueryFilter(_filter);
            //this.View.GetFormId();
            //var x = this.ListView.Model.DataObject["BillHead"];
            //string[] fids = this.ListView.CurrentPageRowsInfo.GetPrimaryKeyValues();
            //foreach(var fid in fids)
            //{
            //    string procInstId = WorkflowChartServiceHelper.GetProcInstIdByBillInst(this.Context, this.View.GetFormId(), fid);
            //    this.View.ShowMessage(procInstId);
            //}
        }

        

        private string Act_SetCustFilter()
        {
            string userId = this.Context.UserId.ToString();

            string sql = string.Format("EXEC proc_czly_GetSalesmanIdByUserId @FUserId='{0}'", userId);

            var objs = DBUtils.ExecuteDynamicObject(this.Context, sql);

            string _filter = " FSELLER in (";

            for(int i = 0; i < objs.Count; i++)
            {
                if (i < objs.Count - 1)
                    _filter += "'" + objs[i]["FSalesmanId"].ToString() + "',";
                else
			        _filter += "'" + objs[i]["FSalesmanId"].ToString() + "'";
            }

            if (objs.Count <= 0)
                _filter += "'-1'";

            _filter += ")";
            return _filter;
        }

    }
}
