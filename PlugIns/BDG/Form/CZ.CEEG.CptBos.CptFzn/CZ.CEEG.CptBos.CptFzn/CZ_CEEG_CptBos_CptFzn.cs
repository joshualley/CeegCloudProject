using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System;

using Kingdee.BOS.Core;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Core.Metadata;
using Kingdee.BOS.Core.Bill.PlugIn;
using Kingdee.BOS.FileServer.Core;
using Kingdee.BOS.FileServer.Core.Object;
using Kingdee.BOS.FileServer.ProxyService;
using Kingdee.BOS.KDThread;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.ServiceHelper;
using Kingdee.BOS.Util;
using Kingdee.BOS.App.Data;

namespace CZ.CEEG.CptBos.CptFzn
{
    [Description("资金冻结")]
    [HotUpdate]
    public class CZ_CEEG_CptBos_CptFzn : AbstractBillPlugIn
    {
        #region override
        public override void AfterBindData(EventArgs e)
        {
            base.AfterBindData(e);
            string FDocumentStatus = CZ_GetCommonField("DocumentStatus");
            if (FDocumentStatus == "Z")
            {
                DateTime currTime = DateTime.Now;
                this.View.Model.SetValue("FYear", currTime.Year.ToString());
                this.View.Model.SetValue("FMonth", currTime.Month.ToString());
            }
        }

        public override void AfterBarItemClick(AfterBarItemClickEventArgs e)
        {
            base.AfterBarItemClick(e);
            switch (e.BarItemKey.ToUpperInvariant())
            {
                case "TBFZNCPT": //tbFznCpt
                    Act_CptFrozen();
                    break;
            }
        }

        public override void BeforeDoOperation(BeforeDoOperationEventArgs e)
        {
            base.BeforeDoOperation(e);
            switch (e.Operation.FormOperation.Operation.ToUpperInvariant())
            {
                case "SAVE":
                case "SUBMIT":
                    if (Check())
                    {
                        e.Cancel = true;
                    }
                    break;
            }
        }
        #endregion

        private bool Check()
        {
            var FEntity = this.View.Model.DataObject["FEntity"] as DynamicObjectCollection;
            string FBraOffice = CZ_GetBaseData("FBraOffice", "Id");
            foreach (var row in FEntity)
            {
                string FCptType = (row["FECptType"] as DynamicObject)?["Id"].ToString() ?? "0";
                var obj = GetCapitalBalance(FBraOffice, FCptType);
                string FCptTypeName = (row["FECptType"] as DynamicObject)?["Name"].ToString() ?? "";
                if (obj.Count > 0)
                {
                    decimal FEUseBal = decimal.Parse(obj[0]["FEUseBal"].ToString());  //资金使用余额
                    decimal FECptFzn = decimal.Parse(obj[0]["FECptFzn"].ToString());  //资金已冻结金额
                    decimal FEFrozenAmt = decimal.Parse(row["FEFrozenAmt"].ToString()); //冻结金额
                    string FDirection = CZ_GetCommonField("FDirection");

                    if (FDirection == "1" && FEUseBal - FECptFzn - FEFrozenAmt < 0) // 冻结
                    {
                        string msg = string.Format("不能超过可冻结的资金!\n结算方式：{0}，本月可冻结资金余额为：{1}元。", FCptTypeName, (FEUseBal - FECptFzn).ToString("f2"));
                        this.View.ShowErrMessage(msg);
                        return true;
                    } 
                    else if(FDirection == "2" && FECptFzn - FEFrozenAmt < 0) // 释放
                    {
                        string msg = string.Format("释放金额不能超过已冻结金额！\n结算方式：{0}，本月资金冻结金额为：{1}元。", FCptTypeName, FECptFzn.ToString("f2"));
                        this.View.ShowErrMessage(msg);
                        return true;
                    }

                }
                else
                {
                    this.View.ShowErrMessage("当前月份资金明细单中不存在此资金类型: " + FCptTypeName + "!");
                    return true;
                }
            }
            return false;
        }


        private DynamicObjectCollection GetCapitalBalance(string FBraOffice, string FCptType = "")
        {
            //var currTime = DateTime.Now;
            string FYear = CZ_GetCommonField("FYear");
            string FMonth = CZ_GetCommonField("FMonth");
            string sql = "";
            if (FCptType == "")
            {
                sql = string.Format(@"select FOccBal,FUseBal,FCptFzn from ora_BDG_CapitalMD 
                                    where FBraOffice='{0}' and FYear='{1}' and FMonth='{2}'",
                                    FBraOffice, FYear, FMonth);
            }
            else
            {
                sql = string.Format(@"select FEOccBal,FEUseBal,FECptFzn from ora_BDG_CapitalMDEntry 
                                where FEBraOffice='{0}' and FEYear='{1}' and FEMonth='{2}' and FECptType='{3}'",
                                FBraOffice, FYear, FMonth, FCptType);
            }

            var obj = DBUtils.ExecuteDynamicObject(this.Context, sql);
            return obj;
        }

        private void Act_CptFrozen()
        {
            string DocumentStatus = CZ_GetCommonField("DocumentStatus");
            string FIsDoBG = CZ_GetCommonField("FIsDoBG");
            if (DocumentStatus == "C")
            {
                if (FIsDoBG == "True")
                {
                    this.View.ShowMessage("此单已经对资金进行了冻结调整！");
                    return;
                }
                string FBraOffice = CZ_GetBaseData("FBraOffice", "Id");
                string FDSrcType = "冻结";
                string FDSrcAction = "调整";
                string FDSrcBillID = "ora_BDG_BudgetAdj";
                string FDSrcFID = CZ_GetFID();
                string FDSrcBNo = CZ_GetValue("FBillNo");

                string FDSrcEntryID = "0";
                string FDSrcSEQ = "0";
                string FDCptType = "";
                string FPreCost = "0";
                string FNote = "";
                var FEntity = this.View.Model.DataObject["FEntity"] as DynamicObjectCollection;
                string FDirection = CZ_GetCommonField("FDirection");
                string sql = "";
                for (int i = 0; i < FEntity.Count; i++)
                {
                    FDSrcEntryID = FEntity[i]["Id"].ToString();
                    FDSrcSEQ = FEntity[i]["Seq"].ToString();
                    FDCptType = (FEntity[i]["FECptType"] as DynamicObject)?["Id"].ToString() ?? "0";
                    FPreCost = FDirection == "1" ? FEntity[i]["FEFrozenAmt"].ToString() : "-" + FEntity[i]["FEFrozenAmt"].ToString();
                    FNote = FEntity[i]["FEText"] == null ? "" : FEntity[i]["FEText"].ToString();
                    sql += String.Format(@"exec proc_czly_InsertCapitalFlowS
	                                        @FBraOffice='{0}',@FDSrcType='{1}',
	                                        @FDSrcAction='{2}',@FDSrcBillID='{3}',
	                                        @FDSrcFID='{4}',@FDSrcBNo='{5}',
	                                        @FDSrcEntryID='{6}',@FDSrcSEQ='{7}',
	                                        @FDCptType='{8}',@FPreCost='{9}',@FNote='{10}';
                                          ", FBraOffice, FDSrcType, FDSrcAction, FDSrcBillID,
                                            FDSrcFID, FDSrcBNo, FDSrcEntryID, FDSrcSEQ,
                                            FDCptType, FPreCost, FNote);
                }
                sql += "update ora_BDG_CptFrozen set FIsDoBG=1 where FID='" + FDSrcFID + "'";
                DBUtils.Execute(Context, sql);
                this.View.ShowMessage("已更新资金！");
                this.View.Refresh();
            }
            else
            {
                this.View.ShowMessage("单据审核后才能进行操作！");
            }
        }


        #region 基本取数方法
        /// <summary>
        /// 获取当前单据FID
        /// </summary>
        /// <returns></returns>
        public string CZ_GetFID()
        {
            return (this.View.Model.DataObject as DynamicObject)["Id"] == null ? "0" : (this.View.Model.DataObject as DynamicObject)["Id"].ToString();
        }
        public string CZ_GetValue(string sign)
        {
            return this.View.Model.GetValue(sign) == null ? "" : this.View.Model.GetValue(sign).ToString();
        }
        /// <summary>
        /// 获取基础资料
        /// </summary>
        /// <param name="sign">标识</param>
        /// <param name="property">属性</param>
        /// <returns></returns>
        private string CZ_GetBaseData(string sign, string property)
        {
            return this.View.Model.DataObject[sign] == null ? "" : (this.View.Model.DataObject[sign] as DynamicObject)[property].ToString();
        }
        /// <summary>
        /// 获取一般字段
        /// </summary>
        /// <param name="sign">标识</param>
        /// <returns></returns>
        private string CZ_GetCommonField(string sign)
        {
            return this.View.Model.DataObject[sign] == null ? "" : this.View.Model.DataObject[sign].ToString();
        }
        #endregion

    }
}
