using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;

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

namespace CZ.CEEG.BdgBos.SalePlanAdj
{
    [Description("销售计划调整")]
    [HotUpdate]
    public class CZ_CEEG_BdgBos_SalePlanAdj : AbstractBillPlugIn
    {
        #region override
        public override void AfterBindData(EventArgs e)
        {
            base.AfterBindData(e);
            string FDocumentStatus = CZ_GetCommonField("DocumentStatus");
            if(FDocumentStatus == "Z")
            {
                DateTime currTime = DateTime.Now;
                this.View.Model.SetValue("FYear", currTime.Year.ToString());
                this.View.Model.SetValue("FMonth", currTime.Month.ToString());
            }
        }

        public override void DataChanged(DataChangedEventArgs e)
        {
            base.DataChanged(e);
            if (e.Field.Key == "FMonPlanAdj" || e.Field.Key == "FBraOffice")
            {
                //计算月预算追加
                DateTime now = DateTime.Now;
                string FYear = now.Year.ToString();
                string FBraOffice = CZ_GetBaseData("FBraOffice", "Id");
                string sql = String.Format("select FAllRate from ora_BDG_CostRate where FYear='{0}' and FBraOffice='{1}'", FYear, FBraOffice);
                var objs = CZDB_GetData(sql);
                if (objs.Count > 0)
                {
                    float FAllRate = float.Parse(objs[0]["FAllRate"].ToString());
                    float FMonPlanAdj = float.Parse(CZ_GetCommonField("FMonPlanAdj"));
                    this.View.Model.SetValue("FBudgetMon", FMonPlanAdj * FAllRate / 100.0);
                }
                else
                {
                    this.View.ShowMessage("请确保分公司已经录入，且本年年度费用系数表已经生成！");
                }
            }
            
        }

        public override void AfterBarItemClick(AfterBarItemClickEventArgs e)
        {
            base.AfterBarItemClick(e);
            switch (e.BarItemKey.ToUpperInvariant())
            {
                case "TBDOANZBG": //tbDoAnzBG  : 更新预算
                    if (!Check())
                    {
                        Act_DoAnzBG();
                    }
                    break;
            }
        }

        public override void AfterDoOperation(AfterDoOperationEventArgs e)
        {
            base.AfterDoOperation(e);
            string opKey = e.Operation.Operation.ToUpperInvariant();
            switch (opKey)
            {
                case "SAVE":
                    Act_GeneEntry();
                    break;
                case "SUBMIT":
                    Act_GeneEntry();
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
                string FCostPrj = row["FCostPrj_Id"].ToString();
                var obj = GetBudgetBalance(FBraOffice, FCostPrj);
		        string sql = "select FNAME from T_BD_EXPENSE_L where FEXPID='" + FCostPrj + "'";
                string FCostPrjName = CZDB_GetData(sql)[0]["FNAME"].ToString();
                if (obj.Count > 0)
                {
                    float FEUseBal = float.Parse(obj[0]["FEUseBal"].ToString());  //预算使用余额
                    float FPrjBudget = float.Parse(row["FPrjBudget"].ToString()); //预算追加
                    if(FEUseBal+ FPrjBudget < 0)
                    {
                        string msg = string.Format("调整后预算不能小于0!\n费用项目：{0}，本月预算使用余额为：{1}元。", FCostPrjName, FEUseBal.ToString("f2"));
                        this.View.ShowMessage(msg);
                        return true;
                    }
                    
                }
		        else
                {
                    this.View.ShowErrMessage("预算明细单中不存在此费用项目: "+ FCostPrjName + "!");
                    return true;
                }
            }

            return false;
        }


        #region Actions
        /// <summary>
        /// 获取当前月份预算占用及使用余额
        /// </summary>
        /// <returns></returns>
        private DynamicObjectCollection GetBudgetBalance(string FBraOffice, string FCostPrj = "")
        {
            var currTime = DateTime.Now;
            string FYear = currTime.Year.ToString();
            string FMonth = currTime.Month.ToString();
            string sql = "";
            if (FCostPrj == "")
            {
                sql = string.Format(@"select FOccBal,FUseBal from ora_BDG_BudgetMD 
                                    where FBraOffice='{0}' and FYear='{1}' and FMonth='{2}'",
                                    FBraOffice, FYear, FMonth);
            }
            else
            {
                sql = string.Format(@"select FEOccBal,FEUseBal from ora_BDG_BudgetMDEntry 
                                where FEBraOffice='{0}' and FEYear='{1}' and FEMonth='{2}' and FCostPrj='{3}'",
                                FBraOffice, FYear, FMonth, FCostPrj);
            }

            var objs = CZDB_GetData(sql);
            return objs;
        }

        private void Act_DoAnzBG()
        {
            string DocumentStatus = CZ_GetCommonField("DocumentStatus");
            string FIsDoBG = CZ_GetCommonField("FIsDoBG");
            if (DocumentStatus == "C")
            {
                if (FIsDoBG == "True")
                {
                    this.View.ShowMessage("预算已经通过此单进行了调整！");
                    return;
                }
                string FBraOffice = CZ_GetBaseData("FBraOffice", "Id");
                //string FYear = CZ_GetCommonField("FYear");
                //string FMonth = CZ_GetCommonField("FMonth");
                string FDSrcType = "调整";
                string FDSrcAction = "追加";
                string FDSrcBillID = "ora_BDG_BudgetAdj";
                string FDSrcFID = CZ_GetFID();
                string FDSrcBNo = CZ_GetValue("FBillNo");

                string FDSrcEntryID = "0";
                string FDSrcSEQ = "0";
                string FDCostPrj = "";
                string FPreCost = "0";
                //string FReCost = "0";

                var FEntity = this.View.Model.DataObject["FEntity"] as DynamicObjectCollection;
                string sql = "";
                for (int i = 0; i < FEntity.Count; i++)
                {
                    FDSrcEntryID = FEntity[i]["Id"].ToString();
                    FDSrcSEQ = FEntity[i]["Seq"].ToString();
                    FDCostPrj = FEntity[i]["FCostPrj"] == null ? "0" : (FEntity[i]["FCostPrj"] as DynamicObject)["Id"].ToString();
                    FPreCost = FEntity[i]["FPrjBudget"].ToString();
                    sql += String.Format(@"exec proc_czly_InsertBudgetFlowS
	                                        @FBraOffice='{0}',@FDSrcType='{1}',
	                                        @FDSrcAction='{2}',@FDSrcBillID='{3}',
	                                        @FDSrcFID='{4}',@FDSrcBNo='{5}',
	                                        @FDSrcEntryID='{6}',@FDSrcSEQ='{7}',
	                                        @FDCostPrj='{8}',@FPreCost='{9}',@FNote='销售计划调整 ';
                                          ", FBraOffice, FDSrcType, FDSrcAction, FDSrcBillID,
                                            FDSrcFID, FDSrcBNo, FDSrcEntryID, FDSrcSEQ,
                                            FDCostPrj, FPreCost);
                }
                sql += "update ora_BDG_SalePlanAdj set FIsDoBG=1 where FID='" + FDSrcFID + "'";
                CZDB_GetData(sql);
                this.View.ShowMessage("已更新预算！");
                this.View.Refresh();
            }
            else
            {
                this.View.ShowMessage("单据审核后才能进行预算的调整！");
            }
        }

        /// <summary>
        /// 生成表体
        /// </summary>
        private void Act_GeneEntry()
        {
            string FID = CZ_GetFID();
            string sql = "exec proc_czly_GeneSalePlanAdjEntry @FID='" + FID + "'";
            CZDB_GetData(sql);
            this.View.Refresh();
        }
        #endregion

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

        #region 数据库查询
        /// <summary>
        /// 基本方法 
        /// </summary>
        /// <param name="_sql"></param>
        /// <returns></returns>
        public DynamicObjectCollection CZDB_GetData(string _sql)
        {
            try
            {
                var obj = DBUtils.ExecuteDynamicObject(this.Context, _sql);
                return obj;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        #endregion
    }
}
