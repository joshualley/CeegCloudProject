using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
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
            long userId = this.Context.UserId;
            // 设置行高
            // 不会换行，等于没用……
            this.View.GetControl<EntryGrid>("FEntity").SetRowHeight(80);
            // 兼容不同部门单据字段不同自动锁定
            var bosFields = this.View.BillBusinessInfo.GetBosFields();
            for (int i = 0; i < bosFields.Length; i++)
            {
                var bosField = bosFields[i];
                // 排除单据头，只对单据体操作
                if (bosField.Field.EntityKey.ToString().Equals("FENTITY"))
                {
                    string bosFieldKey = bosField.Field.Key.ToString();
                    this.View.LockField(bosFieldKey, false);
                }
            }
            // 判定是否是销售权限
            if (CanModify())
            {
                FieldUnLock();
            }
            // 列权限
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

        public override void AfterDoOperation(AfterDoOperationEventArgs e)
        {
            base.AfterDoOperation(e);
            if (e.Operation.Operation.EqualsIgnoreCase("Save"))
            {
                // TODO: 生成订单号
                //Entity entity = this.View.BillBusinessInfo.GetEntity("FEntity");
                //DynamicObjectCollection entityObject = this.View.Model.GetEntityDataObject(entity);
                //for (int i = 0; i < entityObject.Count; i++)
                //{
                //    if (entityObject[i]["FOrderId"] == null)
                //    {
                //    }
                //}

                var isSuccess = e.OperationResult != null && e.OperationResult.IsSuccess;
                if (isSuccess && CanModify())
                {
                    SendMsg();
                }
            }
        }

        public override void AfterEntryBarItemClick(AfterBarItemClickEventArgs e)
        {
            base.AfterEntryBarItemClick(e);
            if (e.BarItemKey.Equals("tbNewList") || e.BarItemKey.Equals("tbNewEntry"))
            {
                if (CanModify())
                {
                    // 新建行
                    this.View.InvokeFormOperation("NewEntry");
                }
                else
                {
                    this.View.ShowMessage("权限不足。");
                }
            }
            else if (e.BarItemKey.Equals("tbInsertEntry"))
            {
                if (CanModify())
                {
                    // 插入行
                    this.View.InvokeFormOperation("InsertEntry");
                }
                else
                {
                    this.View.ShowMessage("权限不足。");
                }
            }
            else if (e.BarItemKey.Equals("tbDeleteEntry"))
            {
                if (CanModify())
                {
                    // 删除行
                    this.View.InvokeFormOperation("DeleteEntry");
                }
                else
                {
                    this.View.ShowMessage("权限不足。");
                }
            }
            // 单击订单明细按钮
            else if (e.BarItemKey.Equals("tbBatchFill"))
            {
                if (CanModify())
                {
                    // 创建动态表单
                    CreateDynamicFromEntry();
                }
                else
                {
                    this.View.ShowMessage("权限不足。");
                }
            }
            else if (e.BarItemKey.Equals("tbBatchFillPlan"))
            {
                if (CanModify())
                {
                    CreateDynamicFromEntryPlan();
                }
                else
                {
                    this.View.ShowMessage("权限不足。");
                }
            }
        }

        public override void DataChanged(DataChangedEventArgs e)
        {
            base.DataChanged(e);
            if (e.Field.OriginKey.Equals("FPlannedDeliveryDate") || e.Field.OriginKey.Equals("FPlanDeliveryDate"))
            {
                Entity entity = this.View.BillBusinessInfo.GetEntity("FEntity");
                DynamicObjectCollection entityObject = this.View.Model.GetEntityDataObject(entity);
                for (int i = 0; i < entityObject.Count; i++)
                {
                    if (entityObject[i]["FPlannedDeliveryDate"] != null && entityObject[i]["FPlanDeliveryDate"] != null)
                    {
                        string date;
                        // 计划交货日期
                        date = entityObject[i]["FPlannedDeliveryDate"].ToString();
                        DateTime FPlannedDeliveryDate = ParseDateTime(date);
                        // 合同交货日期
                        date = entityObject[i]["FPlanDeliveryDate"].ToString();
                        DateTime FPlanDeliveryDate = ParseDateTime(date);
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
                return false;
            }
        }

        /// <summary>
        /// 创建动态表单
        /// </summary>
        public void CreateDynamicFromEntry()
        {
            DynamicFormShowParameter formPa = new DynamicFormShowParameter
            {
                FormId = "ora_delivery_order_detail"
            };
            this.View.ShowForm(formPa, delegate (FormResult result)
            {
                DynamicObjectCollection resultData = result.ReturnData as DynamicObjectCollection;
                if (resultData != null)
                {
                    this.Model.BatchCreateNewEntryRow("FEntity", resultData.Count);
                }
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
        /// 创建动态表单
        /// </summary>
        public void CreateDynamicFromEntryPlan()
        {
            DynamicFormShowParameter formPa = new DynamicFormShowParameter
            {
                FormId = "ora_plan_order_detail"
            };
            this.View.ShowForm(formPa, delegate (FormResult result)
            {
                DynamicObjectCollection resultData = result.ReturnData as DynamicObjectCollection;
                if (resultData != null)
                {
                    this.Model.BatchCreateNewEntryRow("FEntity", resultData.Count);
                }
                if (resultData != null)
                {
                    int rowLen = this.Model.GetEntryRowCount("FEntity") - resultData.Count;
                    for (int i = 0; i < resultData.Count; i++)
                    {
                        DynamicObject entryRow = resultData[i];
                        this.Model.SetValue("FOrderId", resultData[i]["FORDERID"], i + rowLen);
                        this.Model.SetValue("FCustId", resultData[i]["FCustId"], i + rowLen);
                        this.Model.SetValue("FCustName", resultData[i]["FCustName"], i + rowLen);
                        this.Model.SetValue("FSALERID", resultData[i]["FSalerId"], i + rowLen);
                        this.Model.SetValue("FSalerName", resultData[i]["FSalerName"], i + rowLen);
                        this.Model.SetValue("FProductModel", resultData[i]["FProductModel"], i + rowLen);
                        this.Model.SetValue("FProductModel2", resultData[i]["FProductModel2"], i + rowLen);
                        this.Model.SetValue("FOrderNum", resultData[i]["ForderNum"], i + rowLen);
                        this.Model.SetValue("FPlanDeliveryDate", resultData[i]["FPlanDeliveryDate"], i + rowLen);
                        this.Model.SetValue("FPlannedDeliveryDate", resultData[i]["FPlannedDeliveryDate"], i + rowLen);
                        this.Model.SetValue("FLateDelivery", resultData[i]["FLateDelivery"], i + rowLen);
                        this.Model.SetValue("FDeliverySchedule", resultData[i]["FDeliverySchedule"], i + rowLen);
                        this.Model.SetValue("FSCMManager", resultData[i]["FSCMManager"], i + rowLen);
                        this.Model.SetValue("FSCMPro1", resultData[i]["FSCMPro1"], i + rowLen);
                        this.Model.SetValue("FSCMPro2", resultData[i]["FSCMPro2"], i + rowLen);
                        this.Model.SetValue("Fsup_iron", resultData[i]["F_ora_sup_iron"], i + rowLen);
                        this.Model.SetValue("Fsup_shell", resultData[i]["F_ora_sup_shell"], i + rowLen);
                        this.Model.SetValue("FPurchase1", resultData[i]["FPurchase1"], i + rowLen);
                        this.Model.SetValue("FPurchase2", resultData[i]["FPurchase2"], i + rowLen);
                        this.Model.SetValue("FPurchase3", resultData[i]["FPurchase3"], i + rowLen);
                        this.Model.SetValue("F_ora_pur_iron", resultData[i]["F_ora_pur_iron"], i + rowLen);
                        this.Model.SetValue("F_ora_pur_shell", resultData[i]["F_ora_pur_shell"], i + rowLen);
                        this.Model.SetValue("FAuxiliaryMaterial1", resultData[i]["FAuxiliaryMaterial1"], i + rowLen);
                        this.Model.SetValue("FAuxiliaryMaterial2", resultData[i]["FAuxiliaryMaterial2"], i + rowLen);
                        this.Model.SetValue("FProductionFeedback", resultData[i]["FProductionFeedback"], i + rowLen);
                        this.Model.SetValue("F_ora_am", resultData[i]["F_ora_am"], i + rowLen);
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
        private DateTime ParseDateTime(String date)
        {
            DateTime dt = DateTime.ParseExact(date, "yyyy-MM-dd HH:mm:ss", System.Globalization.CultureInfo.CurrentCulture);
            return dt;
        }

        private void FieldUnLock()
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

        /// <summary>
        /// 发送消息
        /// </summary>
        private void SendMsg()
        {
            // 发送消息给多人       
            var items = DBUtils.ExecuteDynamicObject(Context, "/*dialect*/ " +
                " select distinct dc.FUSERID,sx.FOPENID,u.FNAME " +
                " from T_Delivery_Context_Control dc " +
                " join T_SEC_XTUSERMAP sx on dc.FUserId = sx.FUserId " +
                " join T_SEC_USER u on u.FUSERID = dc.FUserId " +
                " where dc.FuserId != null or dc.FuserId != '' ");
            List<Dictionary<string, string>> sendInfos = new List<Dictionary<string, string>>();
            for (int i = 0; i < items.Count; i++)
            {
                if (items[i]["FuserId"] != null)
                {
                    Dictionary<string, string> info = new Dictionary<string, string>
                        {
                            { "FUSERID",items[i]["FUSERID"].ToString()},
                            { "FNAME",items[i]["FNAME"].ToString()},
                            {"FOPENID",items[i]["FOPENID"].ToString() },
                        {"FBILLNO",this.View.Model.GetValue("FBillNo").ToString() },
                        {"FSDATE",this.View.Model.GetValue("FSDate").ToString()},
                        {"FEDATE",this.View.Model.GetValue("FEDate").ToString() }
                        };
                    sendInfos.Add(info);
                }
            }


            String now = DateTime.Now.ToString("yyyy-MM-dd");
            String appId = "DeliveryPlan";
            String title = "title";
            String PubAcctCode = "XT-4249be9f-5181-4a2a-a01f-7a9be93f1b3a";
            String PubAcctKey = "4839223c64dff23b04bf4e3376b7a082";

            sendInfos.ForEach(info =>
            {
                string text = "";
                //判断是否超期,选择不同模板
                text = string.Format(@"亲爱的中电家人，您有新的交货计划【{0}-{1}】待更新，请及时跟踪并反馈采购、生产相关情况。谢谢！", info["FSDATE"], info["FEDATE"]);
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
            });
        }
        #endregion
    }
}
