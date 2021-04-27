using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Data;
using System.Collections;

using Kingdee.BOS.Core;
using Kingdee.BOS.Contracts;
using Kingdee.BOS.Core.Bill.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Core.Metadata;
using Kingdee.BOS.Core.SqlBuilder;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.App.Data;
using Kingdee.BOS.Core.DynamicForm;
using Kingdee.BOS.Util;


namespace CZ.CEEG.BdgBos.BdgCtrl
{
    [Description("预算占用控制")]
    [HotUpdate]
    public class CZ_CEEG_BdgBos_BdgCtrl : AbstractBillPlugIn
    {

        private Dictionary<string, string[]> FormIdToTbNameMap { get; set; } = new Dictionary<string, string[]>()
        {
            //费用立项
            { "ora_FYLX", new string[] { "ora_t_Cust100050", "" } },
            //出差申请
            { "k0c30c431418e4cf4a60d241a18cb241c", new string[] { "ora_t_TravelApply", "ora_t_TravelApplyEntry" } },
            //招待费用申请
            { "k1ae2591790044d95b9966ad0dff1d987", new string[] { "ora_t_ServeFee", "" }},
            //个人资金申请
            {"k0c6b452fa8154c4f8e8e5f55f96bcfac", new string[] { "ora_t_PersonMoney", "ora_t_PersonMoneyEntry" } },
            //对公资金申请
            { "k191b3057af6c4252bcea813ff644cd3a", new string[] { "ora_t_Cust100011", "ora_t_PublicMoneyEntry" } },
            //非生产采购合同评审
            { "kbb14985fbec4445c846533837b2eea65", new string[] { "ora_t_ContractReviewHead", "" } },
            //生产采购合同评审
            { "k3972241808034802b04c3d18d4107afd", new string[] { "ora_t_PCReview", "ora_t_PCReviewEntry" } },
            //对公费用立项
            { "kaa55d0cac0c5447bbc6700cfbdf0b11e", new string[] { "ora_t_PublicApply", "" } },
            //对公费用报销
            { "k5c88e2dc1ac14349935d452e74e152c8", new string[] { "ora_t_PublicSubmit", "ora_t_PublicSubmitEntry" } },
            //出差报销
            { "k6575db4ed77c449f88dd20cceef75a73", new string[] { "ora_t_TravelSubmit", "ora_t_TravelSubmitEntry" } },
            //个人费用立项
            { "ke6d80dfd260e4ef88d75f69f4c7ef0a1", new string[] { "ora_t_PeronCostApplyHead", "" } },
            //个人费用报销
            { "k767a317ad28e40f1b25e95b92e218fea", new string[] { "ora_t_PersonalReimburse", "ora_t_PCostReimburse" } },
            //招待费用报销
            { "kdcdde6ac18cb4d419a6924b49a593460", new string[] { "ora_t_Server", "ora_t_Server_Entry" }},
        };

        public override void DataChanged(DataChangedEventArgs e)
        {
            base.DataChanged(e);
            if (IsUsingBdgSys())
            {
                var FieldName = Transform();
                if (e.Field.Key == FieldName["FCostPrj"])
                {
                    string FBraOffice = CZ_GetBaseData(FieldName["FBraOffice"], "Id");
                    string FCostPrj = this.View.Model.GetValue(FieldName["FCostPrj"], e.Row) == null ? "" : (this.View.Model.GetValue(FieldName["FCostPrj"], e.Row) as DynamicObject)["Id"].ToString();
                    var obj = GetBudgetBalance(FBraOffice, FCostPrj);
                    if (obj.Count > 0)
                    {
                        string sql = $"select FNAME from T_BD_EXPENSE_L where FEXPID='{FCostPrj}'";
                        var items = DBUtils.ExecuteDynamicObject(this.Context, sql);
                        if (items.Count <= 0) return;
                        string FCostPrjName = items[0]["FNAME"].ToString();
                        string msg = string.Format("费用项目：{0}，本月预算占用余额为：{1:f2}元。", FCostPrjName, float.Parse(obj[0]["FEOccBal"].ToString()));
                        this.View.ShowMessage(msg);
                    }
                    else
                    {
                        string sql = $"select FNAME from T_BD_EXPENSE_L where FEXPID='{FCostPrj}'";
                        var items = DBUtils.ExecuteDynamicObject(this.Context, sql);
                        if (items.Count <= 0) return;
                        string FCostPrjName = items[0]["FNAME"].ToString();
                        string msg = string.Format("费用项目：{0}，本月预算不存在。", FCostPrjName);
                        this.View.ShowMessage(msg);
                    }
                }
            }
        }

        public override void BeforeDoOperation(BeforeDoOperationEventArgs e)
        {
            if (IsUsingBdgSys())
            {
                base.BeforeDoOperation(e);
                string opKey = e.Operation.FormOperation.Operation.ToUpperInvariant();
                switch (opKey)
                {
                    case "SUBMIT":
                        if (Check())
                        {
                            e.Cancel = true;
                        }
                        break;
                }
            }
        }

        private bool Check()
        {
            var fieldname = Transform();
            string FBraOffice = CZ_GetBaseData(fieldname["FBraOffice"], "Id");
            //查询预算余额
            var objs = GetBudgetBalance(FBraOffice);
            decimal FOccBal = 0; //预算占用余额
            if (objs.Count > 0)
            {
                FOccBal = decimal.Parse(objs[0]["FOccBal"].ToString());
                if (GetCtrlStrategy(FBraOffice) == 0) //如果总控
                {   
                    string formId = this.View.GetFormId();
                    decimal FTPreCost = 0;
                    // 如果为资金单
                    if (formId.Equals("ora_PersonMoney") || formId.Equals("ora_PublicMoney"))
                    {
                        var entity = this.Model.DataObject["FEntity"] as DynamicObjectCollection;
                        FTPreCost = entity.Where(item => item["FSourceBillNo"].IsNullOrEmptyOrWhiteSpace())
                            .Select(item => Convert.ToDecimal(item["FApplyAmt"]))
                            .Sum();
                    }
                    else 
                    {
                        //获取单据中总预计金额
                        FTPreCost = decimal.Parse(this.View.Model.GetValue(fieldname["FTPreCost"]).ToString());
                    }
                    if(FOccBal < FTPreCost)
                    {
                        string msg = string.Format("本月预算占用余额为：{0:f2}元。\n金额不能超过本月预算余额！", FOccBal);
                        this.View.ShowErrMessage(msg);
                        return true;
                    }
                }
                else if(GetCtrlStrategy(FBraOffice) == 1) //分控
                {
                    string formId = this.View.GetFormId();
                    // 如果为资金单
                    if (formId.Equals("ora_PersonMoney") || formId.Equals("ora_PublicMoney"))
                    {
                        var entity = this.Model.DataObject["FEntity"] as DynamicObjectCollection;
                        var rows = entity.Where(item => item["FSourceBillNo"].IsNullOrEmptyOrWhiteSpace())
                            .Select(i => new 
                            {
                                FCostPrj = Convert.ToInt32(i["FCostItem"]), 
                                FApplyAmt = Convert.ToDecimal(i["FApplyAmt"])
                            }).ToArray();
                        var costs = rows.Select(i => i.FCostPrj).Distinct();
                        foreach (var FCostPrj in costs)
                        {
                            //获取单一费用项目的预算余额
                            var objs1 = GetBudgetBalance(FBraOffice, FCostPrj.ToString());
                            FOccBal = decimal.Parse(objs1[0]["FEOccBal"].ToString()); //预算占用余额

                            decimal FPreCost = rows.Where(i => i.FCostPrj == FCostPrj).Sum(i => i.FApplyAmt);
                            if (FOccBal < FPreCost)
                            {
                                string sql = "select FNAME from T_BD_EXPENSE_L where FEXPID='" + FCostPrj + "'";
                                string FCostPrjName = DBUtils.ExecuteDynamicObject(this.Context, sql)[0]["FNAME"].ToString();
                                string msg = string.Format("费用项目：{0}，本月预算占用余额为：{1:f2}元。\n金额不能超过本月预算余额！", FCostPrjName, FOccBal);
                                this.View.ShowErrMessage(msg);
                                return true;
                            }
                        }
                    }
                    else
                    {
                        //获取单据中各个费用项目的合计金额
                        var datas = DB_GetFormDatas();
                        string FCostPrj = "";
                        decimal FPreCost = 0;
                        decimal FReCost = 0;

                        Dictionary<string, decimal> preMap = new Dictionary<string, decimal>();
                        
                        foreach (var data in datas)
                        {
                            FCostPrj = (data[fieldname["FCostPrj"]] as DynamicObject)["Id"].ToString();
                            FPreCost = decimal.Parse(data[fieldname["FPreCost"]].ToString());
                            FReCost = decimal.Parse(data[fieldname["FReCost"]].ToString());
                            if (preMap.ContainsKey(FCostPrj))
                            {
                                preMap[FCostPrj] += FPreCost;
                            }
                            else
                            {
                                preMap.Add(FCostPrj, FPreCost);
                            }
                        }

                        foreach(var d in preMap)
                        {
                            //获取单一费用项目的预算余额
                            var objs1 = GetBudgetBalance(FBraOffice, d.Key);
                            FOccBal = decimal.Parse(objs1[0]["FEOccBal"].ToString()); //预算占用余额

                            FCostPrj = d.Key;
                            FPreCost = d.Value;
                            if (FOccBal < FPreCost)
                            {
                                string sql = "select FNAME from T_BD_EXPENSE_L where FEXPID='" + FCostPrj + "'";
                                string FCostPrjName = DBUtils.ExecuteDynamicObject(this.Context, sql)[0]["FNAME"].ToString();
                                string msg = string.Format("费用项目：{0}，本月预算占用余额为：{1:f2}元。\n金额不能超过本月预算余额！", FCostPrjName, FOccBal);
                                this.View.ShowErrMessage(msg);
                                return true;
                            }
                        }
                    }
                }
                else
                {
                    this.View.ShowErrMessage("预算控制策略不存在，请确认是否选择了组织！");
                    return true;
                }
            }
            else
            {
                this.View.ShowErrMessage("未能查询到本月预算情况，请联系相关人员进行本月预算的生成或结转！");
                return true;
            }
            return false;
        }

        #region Functions

        /// <summary>
        /// 获取表单数据
        /// </summary>
        /// <returns></returns>
        public DynamicObjectCollection DB_GetFormDatas()
        {
            string[] tb = FormIdToTbNameMap[this.View.GetFormId()];
            bool hasEntry = tb[1] == "" ? false : true;
            DynamicObjectCollection datas;
            if (hasEntry)
            {
                datas = this.View.Model.DataObject["FEntity"] as DynamicObjectCollection;
            }
            else
            {
                BusinessInfo info = this.View.Model.BillBusinessInfo;
                datas = new DynamicObjectCollection(this.View.Model.DataObject.DynamicObjectType);
                datas.Add(this.View.Model.DataObject);
            }
            return datas;
        }

        /// <summary>
        /// 使用字典统一单据字段
        /// </summary>
        /// <returns></returns>
        private Dictionary<string, string> Transform()
        {
            string FormId = this.View.GetFormId();
            var dict = new Dictionary<string, string>();
            switch (FormId)
            {
                case "ora_FYLX"://费用立项
                    dict.Add("FBraOffice", "FUseOrgId");
                    dict.Add("FTPreCost", "FExpectCost1");
                    dict.Add("FTReCost", "FCommitAmount");
                    dict.Add("FCostPrj", "FCostType1");
                    dict.Add("FPreCost", "FExpectCost1");
                    dict.Add("FReCost", "FCommitAmount");
                    break;
                case "k0c30c431418e4cf4a60d241a18cb241c"://出差申请
                    dict.Add("FBraOffice", "FOrgId");
                    dict.Add("FTPreCost", "F_ora_Amount");
                    dict.Add("FTReCost", "FActualAmount");
                    dict.Add("FCostPrj", "FCostType");
                    dict.Add("FPreCost", "FExpectCost");
                    dict.Add("FReCost", "FActualCost");
                    break;
                case "k1ae2591790044d95b9966ad0dff1d987"://招待费用申请
                    dict.Add("FBraOffice", "F_ora_OrgId");
                    dict.Add("FTPreCost", "F_ora_Amount");
                    dict.Add("FTReCost", "FACTUALCOST");
                    dict.Add("FCostPrj", "FCostType");
                    dict.Add("FPreCost", "F_ora_Amount");
                    dict.Add("FReCost", "FACTUALCOST");
                    break;
                case "k3972241808034802b04c3d18d4107afd"://生产采购合同评审
                    dict.Add("FBraOffice", "FOrgId");
                    dict.Add("FTPreCost", "FIncludeTaxAmount");
                    dict.Add("FTReCost", "FIncludeTaxAmount");
                    dict.Add("FCostPrj", "FCostType");
                    dict.Add("FPreCost", "FIncludeTaxAmountItem");
                    dict.Add("FReCost", "FIncludeTaxAmountItem");
                    break;
                case "kbb14985fbec4445c846533837b2eea65"://非生产采购合同评审
                    dict.Add("FBraOffice", "FOrgId");
                    dict.Add("FTPreCost", "FPreAmt");
                    dict.Add("FTReCost", "FRealAmt");
                    dict.Add("FCostPrj", "FCostItem");
                    dict.Add("FPreCost", "FPreAmt");
                    dict.Add("FReCost", "FRealAmt");
                    break;
                case "k191b3057af6c4252bcea813ff644cd3a"://对公资金申请
                    dict.Add("FBraOffice", "FPayOrgId");
                    dict.Add("FTPreCost", "FPreCost");
                    dict.Add("FTReCost", "FRealMoney");
                    dict.Add("FCostPrj", "FCostItem");
                    dict.Add("FPreCost", "FApplyAmt");
                    dict.Add("FReCost", "FAllowAmt");
                    break;
                case "k0c6b452fa8154c4f8e8e5f55f96bcfac"://个人资金申请
                    dict.Add("FBraOffice", "FPayOrgId");
                    dict.Add("FTPreCost", "FAmount");
                    dict.Add("FTReCost", "FStatusAmount");
                    dict.Add("FCostPrj", "FCostItem");
                    dict.Add("FPreCost", "FApplyAmt");
                    dict.Add("FReCost", "FAllowAmt");
                    break;
                // 不使用
                case "k5c88e2dc1ac14349935d452e74e152c8"://对公费用报销
                    dict.Add("FBraOffice", "FOrgId");
                    dict.Add("FTPreCost", "FTotalAmount");
                    dict.Add("FTReCost", "FTCkAmount");
                    dict.Add("FCostPrj", "FCostType");
                    dict.Add("FPreCost", "FRealAmount");
                    dict.Add("FReCost", "FCheckAmount");
                    break;
                case "kaa55d0cac0c5447bbc6700cfbdf0b11e"://对公费用立项
                    dict.Add("FBraOffice", "FOrgId");
                    dict.Add("FTPreCost", "FExpectCost1");
                    dict.Add("FTReCost", "FCommitAmount");
                    dict.Add("FCostPrj", "FCostType1");
                    dict.Add("FPreCost", "FExpectCost1");
                    dict.Add("FReCost", "FCommitAmount");
                    break;
                // 未使用部分
                case "k6575db4ed77c449f88dd20cceef75a73"://出差报销
                    dict.Add("FBraOffice", "FOrgId");
                    dict.Add("FTPreCost", "FTotalAmount");
                    dict.Add("FTReCost", "FTCkAmount");
                    dict.Add("FCostPrj", "FCostType");
                    dict.Add("FPreCost", "FCarAmount");
                    dict.Add("FReCost", "FCheckAmount");
                    break;
                case "ke6d80dfd260e4ef88d75f69f4c7ef0a1"://个人费用立项
                    dict.Add("FBraOffice", "FOrgId");
                    dict.Add("FTPreCost", "FPREMONEY1");
                    dict.Add("FTReCost", "FCommitAmount");
                    dict.Add("FCostPrj", "FCostType1");
                    dict.Add("FPreCost", "FPREMONEY1");
                    dict.Add("FReCost", "FCommitAmount");
                    break;
                case "k767a317ad28e40f1b25e95b92e218fea"://个人费用报销
                    dict.Add("FBraOffice", "FOrgId");
                    dict.Add("FTPreCost", "FTotalAmount");
                    dict.Add("FTReCost", "FTCkAmount");
                    dict.Add("FCostPrj", "FCostItem");
                    dict.Add("FPreCost", "FAmount");
                    dict.Add("FReCost", "FCheckAmount");
                    break;
                case "kdcdde6ac18cb4d419a6924b49a593460"://招待费用报销
                    dict.Add("FBraOffice", "F_ora_OrgId");
                    dict.Add("FTPreCost", "FTotalAmount");
                    dict.Add("FTReCost", "FTCkAmount");
                    dict.Add("FCostPrj", "FCostType1");
                    dict.Add("FPreCost", "F_ora_Money");
                    dict.Add("FReCost", "FCheckAmount");
                    break;
            }
            return dict;
        }

        /// <summary>
        /// 获取当前月份预算占用及使用余额
        /// </summary>
        /// <returns></returns>
        private DynamicObjectCollection GetBudgetBalance(string FBraOffice, string FCostPrj="")
        {
            var currTime = DateTime.Now;
            string FYear = currTime.Year.ToString();
            string FMonth = currTime.Month.ToString();
            string sql = "";
            if(FCostPrj == "")
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
            
            var objs = DBUtils.ExecuteDynamicObject(this.Context, sql);
            return objs;
        }

        /// <summary>
        /// 获取控制策略：1-按费用项目控制，0-总控, -1不存在
        /// </summary>
        /// <param name="FBraOffice"></param>
        /// <returns></returns>
        private int GetCtrlStrategy(string FBraOffice)
        {
            if (FBraOffice == "0")
            {
                return -1;
            }
            string sql = string.Format(@"exec proc_czly_GetPrjCtrl @FBraOffice='{0}'", FBraOffice);
            var objs = DBUtils.ExecuteDynamicObject(this.Context, sql);

            return objs.Count > 0 ? int.Parse(objs[0]["FCtrl4Prj"].ToString()) : -1;
        }
        #endregion

        #region 基本取数方法
        /// <summary>
        /// 是否使用预算系统
        /// </summary>
        /// <returns></returns>
        private bool IsUsingBdgSys()
        {
            string sql = "EXEC proc_cz_ly_IsUsingBdgSys";
            return DBUtils.ExecuteDynamicObject(this.Context, sql)[0]["FSwitch"].ToString() == "1";
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
        
        #endregion

        
    }
}
