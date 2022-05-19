using Kingdee.BOS.App.Data;
using Kingdee.BOS.Core.Bill.PlugIn;
using Kingdee.BOS.Core.DynamicForm;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Core.DynamicForm.PlugIn.ControlModel;
using Kingdee.BOS.Core.Metadata.EntityElement;
using Kingdee.BOS.Msg;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.ServiceHelper.Messages;
using Kingdee.BOS.Util;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;

namespace CZ.CEEG.BosOA.DeliveryPlan
{
    [Description("[发货计划]发货计划页面插件"), HotUpdate]
    public class Class1 : AbstractBillPlugIn
    {
        DataTable dt;

        #region override

        public override void AfterBindData(EventArgs e)
        {
            base.AfterBindData(e);
            // 设置行高
            // 不会换行，等于没用……
            this.View.GetControl<EntryGrid>("FEntity").SetRowHeight(80);

            // 绑定完数据后先把所有的输入框都锁定，后面根据角色信息或用户信息解锁
            this.View.LockField("FOrderId", false);
            this.View.LockField("FCustId", false);
            this.View.LockField("FCustName", false);
            this.View.LockField("FSALERID", false);
            this.View.LockField("FSalerName", false);
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
                this.View.LockField("FCustName", true);
                this.View.LockField("FSalerName", true);
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

        public override void BarItemClick(BarItemClickEventArgs e)
        {
            base.BarItemClick(e);
            if (e.BarItemKey.Equals("tbTest"))
            {
                SendMsg();
            }
        }

        public override void AfterEntryBarItemClick(AfterBarItemClickEventArgs e)
        {
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

        public override void DataChanged(DataChangedEventArgs e)
        {
            base.DataChanged(e);
            if (e.Key.Equals("FPlannedDeliveryDate") || e.Key.Equals("FPlanDeliveryDate"))
            {
                Entity entity = this.View.BillBusinessInfo.GetEntity("FEntity");
                DynamicObjectCollection entityObject = this.View.Model.GetEntityDataObject(entity);
                DynamicObjectCollection dymat = new DynamicObjectCollection(entity.DynamicObjectType);
                //foreach (DynamicObject current in entityObject)
                for (int i = 0; i < entityObject.Count; i++)
                {
                    if (entityObject[i]["FPlannedDeliveryDate"] != null && entityObject[i]["FPlanDeliveryDate"] != null)
                    {
                        string date;
                        // 计划交货日期
                        date = entityObject[i]["FPlannedDeliveryDate"].ToString();
                        DateTime FPlannedDeliveryDate = parseDateTime(date);
                        // 合同交货日期
                        date = entityObject[i]["FPlanDeliveryDate"].ToString();
                        DateTime FPlanDeliveryDate = parseDateTime(date);
                        // String FLateDelivery = (FPlanDeliveryDate.Day - FPlannedDeliveryDate.Day).ToString();
                        TimeSpan FLateDelivery = FPlanDeliveryDate.Subtract(FPlannedDeliveryDate);
                        this.View.Model.SetValue("FLateDelivery", FLateDelivery.Days, i);
                        this.View.UpdateView("FLateDelivery", i);
                    }
                }
            }
        }

        #endregion


        #region 自定义方法
        /// <summary>
        /// 判定是否有修改权限
        /// </summary>
        /// <returns></returns>
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

        /// <summary>
        /// 创建动态表单
        /// </summary>
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
                        this.Model.SetValue("FCustName", resultData[i]["FcustName"], i + rowLen);
                        this.Model.SetValue("FSALERID", resultData[i]["FSALERID"], i + rowLen);
                        this.Model.SetValue("FSalerName", resultData[i]["FSalerName"], i + rowLen);
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

        /// <summary>
        /// 字符串日期转换
        /// </summary>
        /// <param name="date"></param>
        /// <returns></returns>
        private DateTime parseDateTime(String date)
        {
            DateTime dt = DateTime.ParseExact(date, "yyyy-MM-dd HH:mm:ss", System.Globalization.CultureInfo.CurrentCulture);
            return dt;
        }

        /// <summary>
        /// 发送消息
        /// </summary>
        private void SendMsg()
        {
            int rowNum = this.View.Model.GetEntryRowCount("FEntity");

            List<Dictionary<string, string>> sendInfos = new List<Dictionary<string, string>>();

            Dictionary<string, string> infos = new Dictionary<string, string>
                {
                        //{ "FOPENID", this.Model.GetValue("FOPENID", i)==null?"":this.Model.GetValue("FOPENID", i).ToString() }
                        {"FOPENID","5d16db8be4b00068220a1f31" },
                        {"FUserId","192614" }
                };
            sendInfos.Add(infos);


            String now = DateTime.Now.ToString("yyyy-MM-dd");
            String appId = "DeliveryPlan";
            String title = "title";
            String PubAcctCode = "XT-4249be9f-5181-4a2a-a01f-7a9be93f1b3a";
            String PubAcctKey = "4839223c64dff23b04bf4e3376b7a082";

            List<Dictionary<string, string>> sendLogs = new List<Dictionary<string, string>>();

            sendInfos.ForEach(info =>
            {
                string text = "";
                //判断是否超期,选择不同模板
                text = string.Format(@"亲爱的中电家人[{0}]，您有一条新的交货计划待更新，请及时跟踪并反馈交货情况.谢谢", info["FUserId"]);
                Kingdee.BOS.Msg.XunTongMessage message = new Kingdee.BOS.Msg.XunTongMessage
                {
                    AppId = appId,
                    Title = title,
                    Date = DateTime.Now.ToString("yyyy-MM-dd"),
                    Text = text,
                    PubAcctCode = PubAcctCode,//公共号帐号，移动BOS
                    PubAcctKey = PubAcctKey,//公共号密钥
                    Users = new List<string> { info["FOPENID"] }
                };

                XTSendResult res = XunTongServiceHelper.SendMessage(this.Context, message);

                if (res.IsSuccess)
                {
                    Console.WriteLine("1111");
                }
                else
                {

                }
            });
        }
        #endregion
    }
}
