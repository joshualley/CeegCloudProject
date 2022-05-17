using Kingdee.BOS.App.Data;
using Kingdee.BOS.Core.DynamicForm.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Core.Metadata.EntityElement;
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
        // 客户排除中电
        string sql = string.Format("/*dialect*/SELECT FBILLNO,FCUSTID,FSALERID,oe.FMATERIALID,FQTY,FPLANDELIVERYDATE,m.fname matName,o.FID,oe.FENTRYID,FSTOCKOUTQTY " +
            "FROM t_SAL_ORDERENTRY oe JOIN T_SAL_ORDER o ON oe.FID = o.FID " +
            "LEFT JOIN T_BD_MATERIAL_L m ON oe.FMATERIALID = m.FMATERIALID " +
            "LEFT JOIN T_SAL_ORDERENTRY_R r ON r.FENTRYID = oe.FENTRYID " +
            "WHERE FCustId != 102449 ");


        public override void OnLoad(EventArgs e)
        {

            base.OnLoad(e);

            // 执行SQL
            //dt = DBUtils.ExecuteDataSet(this.Context, sql).Tables[0];
            //if (dt.Rows.Count > 0)
            //{
            //    DataBind(sql + "AND r.FSTOCKOUTQTY < oe.FQTY", false);
            //}
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
                string DeliveryCheckBox = this.View.Model.GetValue("FIsDeliveryCheckBox").ToString().ToUpper();
                bool FIsDeliveryCheckBox = DeliveryCheckBox == "TRUE";

                // TODO:sql字符串拼接
                DataBind(sql + " and " +
                    "" + (FOrderId == "" ? "" : " FBILLNO like '%" + FOrderId + "%' and ") +
                    "" + (FCustId == "" ? "" : " FCustId = " + FCustId + " and ") +
                    "" + (FSalerId == "" ? "" : " FSalerId = " + FSalerId + " and ") +
                    "" + (FIsDeliveryCheckBox ? "r.FSTOCKOUTQTY < oe.FQTY AND" : "") +
                    "" + " 1 = 1 ", true);
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
                this.Model.SetValue("FPRODUCTMODEL2", items[i]["matName"], i);
                this.Model.SetValue("FORDERNUM", items[i]["FQTY"], i);
                this.Model.SetValue("FPLANDELIVERYDATE", items[i]["FPLANDELIVERYDATE"], i);
                this.Model.SetValue("FORDERFID", items[i]["FID"], i);
                this.Model.SetValue("FORDERENTRYID", items[i]["FENTRYID"], i);
                this.Model.SetValue("FSTOCKOUTQTY", items[i]["FSTOCKOUTQTY"], i);
            }
            if (flag)
                this.View.UpdateView("FEntity");
        }

        int count = 0;
        public override void BarItemClick(BarItemClickEventArgs e)
        {
            base.BarItemClick(e);
            if (e.BarItemKey.Equals("tbReturnData"))
            {
                Entity entity = this.View.BillBusinessInfo.GetEntity("FEntity");
                DynamicObjectCollection entityObject = this.View.Model.GetEntityDataObject(entity);
                DynamicObjectCollection dymat = new DynamicObjectCollection(entity.DynamicObjectType);
                foreach (DynamicObject current in entityObject)
                {
                    if (current["F_ora_CheckBox"].ToString().ToUpper() == "TRUE")
                    {
                        dymat.Add(current);
                        count++;
                    }
                }
                if (count == 0)
                {
                    this.View.ShowMessage("请选择数据。");
                }
                else
                {
                    this.View.ReturnToParentWindow(dymat);
                    this.View.Close();
                }
            }
        }
    }
}
