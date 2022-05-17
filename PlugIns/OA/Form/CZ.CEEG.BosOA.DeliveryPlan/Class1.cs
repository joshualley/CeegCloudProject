using Kingdee.BOS.App.Data;
using Kingdee.BOS.Core.Bill.PlugIn;
using Kingdee.BOS.Core.DynamicForm;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Core.DynamicForm.PlugIn.ControlModel;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.Util;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CZ.CEEG.BosOA.DeliveryPlan
{
    [Description("[发货计划]发货计划页面插件"),HotUpdate]
    public class Class1 : AbstractBillPlugIn
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
            this.View.LockField("FSTOCKOUTQTY", false);
            

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
                this.View.LockField("FProductModel", true);
                this.View.LockField("FProductModel2", true);
                this.View.LockField("FOrderNum", true);
                this.View.LockField("FPlanDeliveryDate", true);
                this.View.LockField("FPlannedDeliveryDate", true);
                this.View.LockField("FLateDelivery", true);
                this.View.LockField("FDeliverySchedule", true);
                this.View.LockField("FSTOCKOUTQTY", true);
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

        public override void AfterEntryBarItemClick(AfterBarItemClickEventArgs e)
        {

            this.View.ShowMessage(e.BarItemKey.ToString());


            base.AfterEntryBarItemClick(e);
            if (e.BarItemKey.Equals("addEntry") || e.BarItemKey.Equals("tbNewList") || e.BarItemKey.Equals("tbNewEntry"))
            {
                if (CanModify())
                {
                    // 新建行
                    this.View.InvokeFormOperation("NewEntry");
                }
            }
            else if (e.BarItemKey.Equals("tbInsertEntry"))
            {
                // 插入行
                this.View.InvokeFormOperation("InsertEntry");
            }
            // 单击订单明细按钮
            else if (e.BarItemKey.Equals("tbBatchFill"))
            {
                if (CanModify())
                {
                    // 创建动态表单
                    CreateDynamicFromEntry();
                }
            }
            else if (e.BarItemKey.Equals("tbDeleteEntry"))
            {
                if (CanModify())
                {
                    // 删除行
                    this.View.InvokeFormOperation("DeleteEntry");
                }
            }
        }

        public bool CanModify()
        {
            long userId = this.Context.UserId;

            string countSalesmanSql = string.Format("/*dialect*/select FUserId from (SELECT FUserId FROM T_SEC_USER u JOIN V_BD_CONTACTOBJECT c " +
                "ON u.FLINKOBJECT = c.fid JOIN V_BD_SALESMAN s ON s.fempnumber = c.FNUMBER " +
                "union all (select fuserid from DPCreator)) users WHERE FUserId = {0} ", userId);

            int salesman = DBUtils.ExecuteDataSet(this.Context, countSalesmanSql).Tables[0].Rows.Count;
            if (salesman > 0)
            {
                return true;
            }
            else
            {
                this.View.ShowMessage("权限不足。");
                return false;
            }
        }

        public void CreateDynamicFromEntry()
        {
            DynamicFormShowParameter formPa = new DynamicFormShowParameter();
            formPa.FormId = "ora_delivery_order_detail";
            this.View.ShowForm(formPa, delegate (FormResult result)
            {
                DynamicObjectCollection resultData = result.ReturnData as DynamicObjectCollection;
                this.Model.BatchCreateNewEntryRow("FEntity", resultData.Count);
                if (resultData != null)
                {
                    int rowLen = this.Model.GetEntryRowCount("FEntity") - resultData.Count;
                    for (int i = 0; i < resultData.Count; i++)
                    {
                        DynamicObject entryRow = resultData[i];
                        this.Model.SetValue("ForderId", resultData[i]["FORDERID"], i + rowLen);
                        this.Model.SetValue("FCUSTID", resultData[i]["FCUSTID"], i + rowLen);
                        this.Model.SetValue("FSALERID", resultData[i]["FSALERID"], i + rowLen);
                        this.Model.SetValue("FPRODUCTMODEL", resultData[i]["FProductModel"], i + rowLen);
                        this.Model.SetValue("FPRODUCTMODEL2", resultData[i]["FProductModel2"], i + rowLen);
                        this.Model.SetValue("FORDERNUM", resultData[i]["FORDERNUM"], i + rowLen);
                        this.Model.SetValue("FPLANDELIVERYDATE", resultData[i]["FPLANDELIVERYDATE"], i + rowLen);
                        this.Model.SetValue("FORDERFID", resultData[i]["FORDERFID"], i + rowLen);
                        this.Model.SetValue("FORDERENTRYID", resultData[i]["FORDERENTRYID"], i + rowLen);
                        this.Model.SetValue("FSTOCKOUTQTY", resultData[i]["FSTOCKOUTQTY"], i + rowLen);
                    }
                }
            });
        }
    }
}
