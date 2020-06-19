using Kingdee.BOS.App.Data;
using Kingdee.BOS.Core.Bill;
using Kingdee.BOS.Core.Bill.PlugIn;
using Kingdee.BOS.Core.Metadata;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.Core.DynamicForm;
using Kingdee.BOS.Core.DynamicForm.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Util;
using System;
using System.ComponentModel;


namespace CZ.CEEG.BosPmt.PmtDeliver
{
    [HotUpdate]
    [Description("货款账龄")]
    public class CZ_CEEG_BosPmt_PmtAging : AbstractDynamicFormPlugIn
    {
        public override void AfterBindData(EventArgs e)
        {
            base.AfterBindData(e);
            QueryDeptAging();
            QueryFactoryAging();

        }

        /// <summary>
        ///  查询办事处账龄
        /// </summary>
        private void QueryDeptAging()
        {
            string sql = "EXEC proc_czly_GetAging @Type='Dept'";
            var objs =  DBUtils.ExecuteDynamicObject(this.Context, sql);
            this.View.Model.DeleteEntryData("FDEntity");
            for (int i = 0; i < objs.Count; i++)
            {
                this.View.Model.CreateNewEntryRow("FDEntity");
                this.View.Model.SetValue("FDeptID", objs[i]["FDeptID"].ToString(), i);
                this.View.Model.SetValue("FAging", objs[i]["FAging"].ToString(), i);
                this.View.Model.SetValue("FOuterPmt", objs[i]["FOuterPmt"].ToString(), i);
            }
            this.View.UpdateView("FDEntity");
        }

        /// <summary>
        /// 查询子公司账龄
        /// </summary>
        private void QueryFactoryAging()
        {
            string sql = "EXEC proc_czly_GetAging @Type='Factory'";
            var objs = DBUtils.ExecuteDynamicObject(this.Context, sql);
            this.View.Model.DeleteEntryData("FFEntity");
            for (int i = 0; i < objs.Count; i++)
            {
                this.View.Model.CreateNewEntryRow("FFEntity");
                this.View.Model.SetValue("FFactoryID", objs[i]["FFactoryID"].ToString(), i);
                this.View.Model.SetValue("FFAging", objs[i]["FAging"].ToString(), i);
                this.View.Model.SetValue("FFOuterPmt", objs[i]["FOuterPmt"].ToString(), i);
            }
            this.View.UpdateView("FFEntity");

        }

    }
}
