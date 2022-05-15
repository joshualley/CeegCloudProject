using Kingdee.BOS.App.Data;
using Kingdee.BOS.Core.Bill.PlugIn;
using Kingdee.BOS.Core.DynamicForm;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Core.DynamicForm.PlugIn.ControlModel;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.Util;
using System;
using System.ComponentModel;

namespace CZ.CEEG.BosOA.DeliveryPlan.SalesmanButton
{
    [Description("[发货计划]销售新增行打开动态表单事件"), HotUpdate]
    public class Class1 : AbstractBillPlugIn
    {

        public override void AfterEntryBarItemClick(AfterBarItemClickEventArgs e)
        {
            base.AfterEntryBarItemClick(e);
            if (e.BarItemKey.Equals("addEntry"))
            {
                if (CanModify())
                {
                    this.View.InvokeFormOperation("NewEntry");
                }
            }
            if (e.BarItemKey.Equals("tbBatchFill"))
            {
                if (CanModify())
                {
                    CreateDynamicFromEntry();
                }
            }
            if (e.BarItemKey.Equals("tbDeleteEntry"))
            {
                if (CanModify())
                {
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
            //formPa.CustomParams.Add("FSALERID", Convert.ToString(this.View.Model.DataObject["FAPPLYID"]));
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
                    }
                }
            });
        }
    }
}
