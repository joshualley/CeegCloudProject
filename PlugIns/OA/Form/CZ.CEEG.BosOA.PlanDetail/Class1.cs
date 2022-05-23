using System.ComponentModel;
using System.Data;
using Kingdee.BOS.App.Data;
using Kingdee.BOS.Core.DynamicForm.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Core.Metadata.EntityElement;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.Util;

namespace CZ.CEEG.BosOA.PlanDetail
{
    [Description("[计划明细]计划明细列表动态表单"), HotUpdate]
    public class Class1 : AbstractDynamicFormPlugIn
    {
        DataTable dt;
        // 客户排除中电

        // 中电ID：102449
        string sql = string.Format("/*dialect*/ SELECT" +
            "     FORDERID," +
            "     FCustId," +
            "     FCustName," +
            "     FSalerId," +
            "     FSalerName," +
            "     FProductModel," +
            "     FProductModel2," +
            "     FOrderNum," +
            "     FPlanDeliveryDate," +
            "     FPlannedDeliveryDate," +
            "     FLateDelivery," +
            "     FDeliverySchedule," +
            "     FSCMManager," +
            "     FSCMPro1," +
            "     FSCMPro2," +
            "     F_ora_sup_iron," +
            "     F_ora_sup_shell," +
            "     FPurchase1," +
            "     FPurchase2," +
            "     FPurchase3," +
            "     F_ora_pur_iron," +
            "     F_ora_pur_shell," +
            "     FAuxiliaryMaterial2," +
            "     FAuxiliaryMaterial1," +
            "     FProductionFeedback," +
            "     F_ora_am," +
            "     FORDERFID," +
            "     FORDERENTRYID," +
            "     FSTOCKOUTQTY" +
            "  FROM" +
            "     ora_t_Cust_Entry100025");

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

                DataBind(sql + " where " +
                    "" + (FOrderId == "" ? "" : " FORDERID like '%" + FOrderId + "%' and ") +
                    "" + (FCustId == "" ? "" : " FCustID = " + FCustId + " and ") +
                    "" + (FSalerId == "" ? "" : " FSalerId = " + FSalerId + " and ") +
                    "" + (FIsDeliveryCheckBox ? " FSTOCKOUTQTY < FOrderNum AND " : "") +
                    "" + " 1 = 1 ", true);

            }
        }

        public void DataBind(string sql, bool flag)
        {
            //this.View.ShowMessage(sql);

            var items = DBUtils.ExecuteDynamicObject(Context, sql);
            this.Model.DeleteEntryData("F_ora_Entity");
            this.Model.BatchCreateNewEntryRow("F_ora_Entity", items.Count);
            for (int i = 0; i < items.Count; i++)
            {
                this.Model.SetValue("F_ora_OrderId", items[i]["FORDERID"], i);
                this.Model.SetValue("F_ora_CustId", items[i]["FCustId"], i);
                this.Model.SetValue("F_ora_CustName", items[i]["FCustName"], i);
                this.Model.SetValue("F_ora_SALERID", items[i]["FSalerId"], i);
                this.Model.SetValue("F_ora_SalerName", items[i]["FSalerName"], i);
                this.Model.SetValue("F_ora_ProductModel", items[i]["FProductModel"], i);
                this.Model.SetValue("F_ora_ProductModel2", items[i]["FProductModel2"], i);
                this.Model.SetValue("F_ora_OrderNum", items[i]["ForderNum"], i);
                this.Model.SetValue("F_ora_PlanDeliveryDate", items[i]["FPlanDeliveryDate"], i);
                this.Model.SetValue("F_ora_PlannedDeliveryDate", items[i]["FPlannedDeliveryDate"], i);
                this.Model.SetValue("F_ora_LateDelivery", items[i]["FLateDelivery"], i);
                this.Model.SetValue("F_ora_DeliverySchedule", items[i]["FDeliverySchedule"], i);
                this.Model.SetValue("F_ora_SCMManager", items[i]["FSCMManager"], i);
                this.Model.SetValue("F_ora_SCMPro1", items[i]["FSCMPro1"], i);
                this.Model.SetValue("F_ora_SCMPro2", items[i]["FSCMPro2"], i);
                this.Model.SetValue("F_ora_sup_iron", items[i]["F_ora_sup_iron"], i);
                this.Model.SetValue("F_ora_sup_shell", items[i]["F_ora_sup_shell"], i);
                this.Model.SetValue("F_ora_Purchase1", items[i]["FPurchase1"], i);
                this.Model.SetValue("F_ora_Purchase2", items[i]["FPurchase2"], i);
                this.Model.SetValue("F_ora_Purchase3", items[i]["FPurchase3"], i);
                this.Model.SetValue("F_ora_pur_iron", items[i]["F_ora_pur_iron"], i);
                this.Model.SetValue("F_ora_pur_shell", items[i]["F_ora_pur_shell"], i);
                this.Model.SetValue("F_ora_AuxiliaryMaterial1", items[i]["FAuxiliaryMaterial1"], i);
                this.Model.SetValue("F_ora_AuxiliaryMaterial2", items[i]["FAuxiliaryMaterial2"], i);
                this.Model.SetValue("F_ora_ProductionFeedback", items[i]["FProductionFeedback"], i);
                this.Model.SetValue("F_ora_am", items[i]["F_ora_am"], i);
                this.Model.SetValue("F_ora_ORDERFID", items[i]["FORDERFID"], i);
                this.Model.SetValue("F_ora_ORDERENTRYID", items[i]["FORDERENTRYID"], i);
                this.Model.SetValue("F_ora_STOCKOUTQTY", items[i]["FSTOCKOUTQTY"], i);
            }
            if (flag)
                this.View.UpdateView("F_ora_Entity");
        }

        int count = 0;
        public override void BarItemClick(BarItemClickEventArgs e)
        {
            base.BarItemClick(e);
            if (e.BarItemKey.Equals("tbReturnData"))
            {
                Entity entity = this.View.BillBusinessInfo.GetEntity("F_ora_Entity");
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
