using Kingdee.BOS.App.Data;
using Kingdee.BOS.Core.DynamicForm.PlugIn.ControlModel;
using Kingdee.BOS.Core.List.PlugIn;
using Kingdee.BOS.Core.Metadata;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.Util;
using System;
using System.ComponentModel;
using System.Data;

namespace CZ.CEEG.BosOA.DeliveryPlan.Freeze
{

    [Description("[发货计划]根据用户ID锁定列"), HotUpdate]
    public class Class1 : AbstractListPlugIn
    {
        DataTable dt;
        public override void AfterBindData(EventArgs e)
        {
            base.AfterBindData(e);
            // 设置行高
            // 不会换行，等于没用……
            this.View.GetControl<EntryGrid>("FEntity").SetRowHeight(80);

            // 绑定完数据后先把所有的输入框都锁定，后面根据角色信息或用户信息解锁
            this.View.LockField("FOrderId", false);
            this.View.LockField("FCustId", false);
            this.View.LockField("FSALERID", false);
            this.View.LockField("FProductModel", false);
            this.View.LockField("FProductModel2", false);
            this.View.LockField("FOrderNum", false);
            this.View.LockField("FPlanDeliveryDate", false);
            this.View.LockField("FPlannedDeliveryDate", false);
            this.View.LockField("FLateDelivery", false);
            this.View.LockField("FDeliverySchedule", false);
            this.View.LockField("FSCMManager", false);
            this.View.LockField("FSCMPro1", false);
            this.View.LockField("FSCMPro2", false);
            this.View.LockField("FPurchase1", false);
            this.View.LockField("FPurchase2", false);
            this.View.LockField("FPurchase3", false);
            this.View.LockField("FAuxiliaryMaterial1", false);
            this.View.LockField("FProductionFeedback", false);

            long userId = this.Context.UserId;

            string countSalesmanSql = string.Format("/*dialect*/select FUserId from (SELECT FUserId FROM T_SEC_USER u JOIN V_BD_CONTACTOBJECT c " +
                "ON u.FLINKOBJECT = c.fid JOIN V_BD_SALESMAN s ON s.fempnumber = c.FNUMBER " +
                "union all (select fuserid from DPCreator)) users WHERE FUserId = {0} ", userId);

            int salesman = DBUtils.ExecuteDataSet(this.Context, countSalesmanSql).Tables[0].Rows.Count;
            if (salesman > 0)
            {
                this.View.LockField("FOrderId", true);
                this.View.LockField("FCustId", true);
                this.View.LockField("FSALERID", true);
                this.View.LockField("FOrderNum", true);
                this.View.LockField("FProductModel", true);
                this.View.LockField("FProductModel2", true);
                this.View.LockField("FPlanDeliveryDate", true);
                this.View.LockField("FPlannedDeliveryDate", true);
                this.View.LockField("FLateDelivery", true);
                this.View.LockField("FDeliverySchedule", true);
            }

            string contextPermissions = string.Format("/*dialect*/select FContext from T_Delivery_Context_Control where FUserId = {0};", userId);
            dt = DBUtils.ExecuteDataSet(Context, contextPermissions).Tables[0];
            if (dt.Rows.Count > 0)
            {
                var items = DBUtils.ExecuteDynamicObject(Context, contextPermissions);
                for (int i = 0; i < items.Count; i++)
                {
                    if (items[i]["FContext"] != null)
                    {
                        this.View.LockField(items[i]["FContext"].ToString(), true);
                    }
                }
            }
        }
    }
}
