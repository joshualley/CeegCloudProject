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

        // 中电ID：102449
        string sql = string.Format("/*dialect*/SELECT " +
            "     FBILLNO as ForderId," +
            "     c.FCUSTID," +
            "     cf.FINVOICETITLE as FCustName," +
            "     s.FID as FSalerId," +
            "     sl.FName as FSALERNAME," +
            "     oe.FMATERIALID," +
            "     FQTY," +
            "     FPLANDELIVERYDATE," +
            "     m.fname matName," +
            "     o.FID," +
            "     oe.FENTRYID," +
            "     FSTOCKOUTQTY " +
            " FROM" +
            "     t_SAL_ORDERENTRY oe " +
            "     JOIN T_SAL_ORDER o ON oe.FID = o.FID " +
            "     JOIN T_BD_MATERIAL_L m ON oe.FMATERIALID = m.FMATERIALID " +
            "     JOIN T_SAL_ORDERENTRY_R r ON r.FENTRYID = oe.FENTRYID " +
            "     JOIN T_BD_CUSTOMER c ON c.FCUSTID = o.FCUSTID " +
            "     JOIN T_BD_CUSTOMER_F cf ON o.FCUSTID = cf.FCUSTID " +
            "     JOIN V_BD_SALESMAN s ON s.FID = o.FSALERID " +
            "     JOIN V_BD_SALESMAN_L sl ON sl.FID = s.FID " +
            " WHERE " +
            "     c.FmasterId != 102450 ");

        public override void AfterButtonClick(AfterButtonClickEventArgs e)
        {
            base.AfterButtonClick(e);
            if (e.Key.Equals("FQUERYBTN"))
            {
                string FOrder = this.Model.GetValue("FOrderId_Head") == null ? null : this.Model.GetValue("FOrderId_Head").ToString();
                DynamicObject FCust = (DynamicObject)this.View.Model.GetValue("FCustId_Head");
                DynamicObject FSaler = (DynamicObject)this.Model.GetValue("FSalerId_Head");
                string FOrderId = FOrder == null ? "" : FOrder.ToString();
                string FCustId = FCust == null ? "" : FCust["msterId"].ToString();
                string FSalerId = FSaler == null ? "" : FSaler["Id"].ToString();
                string DeliveryCheckBox = this.View.Model.GetValue("FIsDeliveryCheckBox").ToString().ToUpper();
                bool FIsDeliveryCheckBox = DeliveryCheckBox == "TRUE";

                // 本机环境中FUserOrgId:1
                // ZD186环境中FUse人OrgId:100680
                // TODO:sql字符串拼接
                DataBind(sql + " and " +
                    "" + (FOrderId == "" ? "" : " FBILLNO like '%" + FOrderId + "%' and ") +
                    "" + (FCustId == "" ? "" : " c.FCustID = (select FCustId from T_BD_CUSTOMER where FMasterId = " + FCustId + " and fuseOrgId = 100680) and ") +
                    "" + (FSalerId == "" ? "" : " FSalerId = " + FSalerId + " and ") +
                    "" + (FIsDeliveryCheckBox ? " r.FSTOCKOUTQTY < oe.FQTY AND " : "") +
                    "" + " 1 = 1 ", true);

            }
        }

        public void DataBind(string sql, bool flag)
        {
            //this.View.ShowMessage(sql);

            var items = DBUtils.ExecuteDynamicObject(Context, sql);
            this.Model.DeleteEntryData("FEntity");
            this.Model.BatchCreateNewEntryRow("FEntity", items.Count);
            for (int i = 0; i < items.Count; i++)
            {
                this.Model.SetValue("FORDERID", items[i]["FOrderId"], i);
                this.Model.SetValue("FCUSTID", items[i]["FCUSTID"], i);
                this.Model.SetValue("FCustName", items[i]["FCustName"], i);
                this.Model.SetValue("FSALERID", items[i]["FSALERID"], i);
                this.Model.SetValue("FSalerName", items[i]["FSALERNAME"], i);
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
