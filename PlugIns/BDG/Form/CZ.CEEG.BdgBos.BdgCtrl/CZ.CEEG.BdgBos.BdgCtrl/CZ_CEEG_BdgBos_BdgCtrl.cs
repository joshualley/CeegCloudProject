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
                        string sql = "select FNAME from T_BD_EXPENSE_L where FEXPID='" + FCostPrj + "'";
                        string FCostPrjName = CZDB_GetData(sql)[0]["FNAME"].ToString();
                        string msg = string.Format("费用项目：{0}，本月预算占用余额为：{1}元。", FCostPrjName, float.Parse(obj[0]["FEOccBal"].ToString()).ToString("f2"));
                        this.View.ShowMessage(msg);
                    }
                }
            }
        }

        public override void BeforeDoOperation(BeforeDoOperationEventArgs e)
        {
            if (this.View.Context.ClientType.ToString() != "Mobile" && IsUsingBdgSys())
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
            string FormId = this.View.GetFormId();
            var fieldname = Transform();
            string FBraOffice = CZ_GetBaseData(fieldname["FBraOffice"], "Id");
            //查询预算余额
            var objs = GetBudgetBalance(FBraOffice);
            float FOccBal = 0; //预算占用余额
            float FUseBal = 0; //使用余额
            if (objs.Count > 0)
            {
                FOccBal = float.Parse(objs[0]["FOccBal"].ToString());
                FUseBal = float.Parse(objs[0]["FUseBal"].ToString());
                if (GetCtrlStrategy(FBraOffice) == 0) //如果总控
                {
                    //获取单据中总预计金额
                    float FTPreCost = float.Parse(this.View.Model.GetValue(fieldname["FTPreCost"]).ToString());
                    //float FTReCost = float.Parse(this.View.Model.GetValue(fieldname["FTReCost"]).ToString());
                    if(FOccBal < FTPreCost)
                    {
                        string msg = string.Format("本月预算占用余额为：{0}元。\n金额不能超过本月预算余额！", FOccBal.ToString("f2"));
                        this.View.ShowErrMessage(msg);
                        return true;
                    }
                }
                else if(GetCtrlStrategy(FBraOffice) == 1) //分控
                {
                    //获取单据中各个费用项目的合计金额
                    var datas = DB_GetFormDatas();
                    string FCostPrj = "";
                    float FPreCost = 0;
                    float FReCost = 0;

                    Dictionary<string, float> preMap = new Dictionary<string, float>();
                    //Dictionary<string, float> reMap = new Dictionary<string, float>();
                    
                    foreach (var data in datas)
                    {
                        FCostPrj = (data[fieldname["FCostPrj"]] as DynamicObject)["Id"].ToString();
                        FPreCost = float.Parse(data[fieldname["FPreCost"]].ToString());
                        FReCost = float.Parse(data[fieldname["FReCost"]].ToString());
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
                        FOccBal = float.Parse(objs1[0]["FEOccBal"].ToString()); //预算占用余额
                        FUseBal = float.Parse(objs1[0]["FEUseBal"].ToString()); //使用余额

                        FCostPrj = d.Key;
                        FPreCost = d.Value;
                        //FReCost = 
                        if (FOccBal < FPreCost)
                        {
                            string sql = "select FNAME from T_BD_EXPENSE_L where FEXPID='" + FCostPrj + "'";
                            string FCostPrjName = CZDB_GetData(sql)[0]["FNAME"].ToString();
                            string msg = string.Format("费用项目：{0}，本月预算占用余额为：{1}元。\n金额不能超过本月预算余额！", FCostPrjName, FOccBal.ToString("f2"));
                            this.View.ShowErrMessage(msg);
                            return true;
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
        /// 根据formID获取表名
        /// </summary>
        /// <returns></returns>
        private Dictionary<string, string[]> GetTbNameByFormId()
        {
            var dict = new Dictionary<string, string[]>();
            //费用立项
            dict.Add("ora_FYLX", new string[] { "ora_t_Cust100050", "" });
            //对公费用立项
            dict.Add("kaa55d0cac0c5447bbc6700cfbdf0b11e", new string[] { "ora_t_PublicApply", "" });
            //对公费用报销
            dict.Add("k5c88e2dc1ac14349935d452e74e152c8", new string[] { "ora_t_PublicSubmit", "ora_t_PublicSubmitEntry" });
            //对公资金申请
            dict.Add("k191b3057af6c4252bcea813ff644cd3a", new string[] { "ora_t_Cust100011", "" });
            //出差申请
            dict.Add("k0c30c431418e4cf4a60d241a18cb241c", new string[] { "ora_t_TravelApply", "ora_t_TravelApplyEntry" });
            //出差报销
            dict.Add("k6575db4ed77c449f88dd20cceef75a73", new string[] { "ora_t_TravelSubmit", "ora_t_TravelSubmitEntry" });
            //个人费用立项
            dict.Add("ke6d80dfd260e4ef88d75f69f4c7ef0a1", new string[] { "ora_t_PeronCostApplyHead", "" });
            //个人费用报销
            dict.Add("k767a317ad28e40f1b25e95b92e218fea", new string[] { "ora_t_PersonalReimburse", "ora_t_PCostReimburse" });
            //个人资金借支
            dict.Add("k0c6b452fa8154c4f8e8e5f55f96bcfac", new string[] { "ora_t_PersonMoney", "" });
            //招待费用申请
            dict.Add("k1ae2591790044d95b9966ad0dff1d987", new string[] { "ora_t_ServeFee", "" });
            //招待费用报销
            dict.Add("kdcdde6ac18cb4d419a6924b49a593460", new string[] { "ora_t_Server", "ora_t_Server_Entry" });
            //采购合同评审
            dict.Add("k3972241808034802b04c3d18d4107afd", new string[] { "ora_t_PCReview", "ora_t_PCReviewEntry" });
            //销售合同评审
            dict.Add("kdb6ae742543a4f6da09dfed7ba4e02dd", new string[] { "ora_t_SellContractHead", "ora_t_SellContractEntry" });

            return dict;
        }

        /// <summary>
        /// 获取表单数据
        /// </summary>
        /// <returns></returns>
        public DynamicObjectCollection DB_GetFormDatas()
        {
            string[] tb = GetTbNameByFormId()[this.View.GetFormId()];
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
                case "k0c6b452fa8154c4f8e8e5f55f96bcfac"://个人资金借支
                    dict.Add("FBraOffice", "FOrgId");
                    dict.Add("FTPreCost", "FExpectCost1");
                    dict.Add("FTReCost", "FCommitAmount");
                    dict.Add("FCostPrj", "FCostType1");
                    dict.Add("FPreCost", "FExpectCost1");
                    dict.Add("FReCost", "FCommitAmount");
                    break;
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
            
            var objs = CZDB_GetData(sql);
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
            var objs = CZDB_GetData(sql);

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
            return CZDB_GetData(sql)[0]["FSwitch"].ToString() == "1" ? true : false;
        }

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

        #region 数据库查询方法
        /// <summary>
        /// 基本方法 数据库查询
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
