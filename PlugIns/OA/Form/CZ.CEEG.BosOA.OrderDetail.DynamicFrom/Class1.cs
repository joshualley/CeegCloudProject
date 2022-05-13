using Kingdee.BOS.App.Data;
using Kingdee.BOS.Core.DynamicForm.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.Util;
using System;
using System.ComponentModel;
using System.Data;

namespace CZ.CEEG.BosOA.OrderDetail.DynamicFrom
{
    [Description("[订单明细]发货处动态表单订单明细"), HotUpdate]
    public class Class1 : AbstractDynamicFormPlugIn
    {
        DataTable dt;
        string sql = string.Format("/*dialect*/SELECT FBILLNO , FCUSTID , FSALERID , FMATERIALID , FQTY , FPLANDELIVERYDATE " +
                "FROM t_SAL_ORDERENTRY sorddet JOIN T_SAL_ORDER sord ON sorddet.FID = sord.FID");

        public override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            // 执行SQL
            dt = DBUtils.ExecuteDataSet(this.Context, sql).Tables[0];
            if (dt.Rows.Count > 0)
            {
                DataBind(sql, false);
            }
        }



        public override void AfterButtonClick(AfterButtonClickEventArgs e)
        {
            base.AfterButtonClick(e);
            if (e.Key.Equals("FQUERYBTN"))
            {
                string FOrder = this.Model.GetValue("FOrderId_Head") == null ? null : this.Model.GetValue("FOrderId_Head").ToString();
                DynamicObject FCust = (DynamicObject)this.Model.GetValue("FCustId_Head");
                DynamicObject FSaler = (DynamicObject)this.Model.GetValue("FSalerId_Head");
                string FOrderId = FOrder == null ? "" : FOrder.ToString();
                string FCustId = FCust == null ? "" : FCust["Id"].ToString();
                string FSalerId = FSaler == null ? "" : FSaler["Id"].ToString();

                // TODO:sql字符串拼接
                sql = "/*dialect*/SELECT FBILLNO , FCUSTID , FSALERID , FMATERIALID , FQTY , FPLANDELIVERYDATE " +
                    "FROM t_SAL_ORDERENTRY sod JOIN T_SAL_ORDER so ON sod.FID = so.FID where " +
                    "" + (FOrderId == "" ? "" : " FBILLNO like '%" + FOrderId + "%' and ") +
                    "" + (FCustId == "" ? "" : " FCustId = " + FCustId + " and ") +
                    "" + (FSalerId == "" ? "" : " FSalerId = " + FSalerId + " and ") +
                    "" + " 1 = 1 ";
                DataBind(sql, true);
            }
        }

        public void DataBind(string sql, bool flag)
        {
            var items = DBUtils.ExecuteDynamicObject(Context, sql);
            this.Model.DeleteEntryData("FEntity");
            this.Model.BatchCreateNewEntryRow("FEntity", items.Count);
            for (int i = 0; i < items.Count; i++)
            {
                this.Model.SetValue("ForderId", items[i]["FBILLNO"], i);
                this.Model.SetValue("FCUSTID", items[i]["FCUSTID"], i);
                this.Model.SetValue("FSALERID", items[i]["FSALERID"], i);
                this.Model.SetValue("FPRODUCTMODEL", items[i]["FMATERIALID"], i);
                this.Model.SetValue("FORDERNUM", items[i]["FQTY"], i);
                this.Model.SetValue("FPLANDELIVERYDATE", items[i]["FPLANDELIVERYDATE"], i);
            }
            if (flag)
                this.View.UpdateView();
        }
    }
}
