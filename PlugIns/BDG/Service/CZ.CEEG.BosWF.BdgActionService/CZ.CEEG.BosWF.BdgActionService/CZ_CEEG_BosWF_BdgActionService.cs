using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;

using Kingdee.BOS.Contracts;
using Kingdee.BOS.Core.Const;
using Kingdee.BOS.Core.Metadata;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.Workflow.Interface;

using Kingdee.BOS.Core.DynamicForm.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.App.Data;
using Kingdee.BOS.Util;
using Kingdee.BOS;

namespace CZ.CEEG.BosWF.BdgActionService
{
    [Description("预算流水服务插件")]
    [HotUpdate]
    public class CZ_CEEG_BosWF_BdgActionService : AbstractOperationServicePlugIn
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


        public override void OnPreparePropertys(PreparePropertysEventArgs e)
        {
            base.OnPreparePropertys(e);
            e.FieldKeys.Add("DocumentStatus");
        }

        public override void BeginOperationTransaction(BeginOperationTransactionArgs e)
        {
            base.BeginOperationTransaction(e);
            if (IsUsingBdgSys())
            {
                InsertFlow(e);
            }
        }

        #region Actions

        private void InsertFlow(BeginOperationTransactionArgs e)
        {
            string FBraOffice = "0";
            string FDSrcType = "";
            string FDSrcAction = "";
            string FDSrcBillID = CZ_GetFormType();
            string FDSrcFID = "";
            string FDSrcBNo = "";

            string FDSrcEntryID = "0";
            string FDSrcSEQ = "0";
            string FDCostPrj = "";
            string FPreCost = "0";
            string FReCost = "0";
            string FNote = "";

            string opKey = this.FormOperation.Operation.ToUpperInvariant();
            string sql = "";
            string backSql = "";
            Dictionary<string, string> Trans;
            if (opKey == "SUBMIT" || opKey == "CANCELASSIGN" || opKey == "AUDIT" || opKey == "UNAUDIT")
            {
                var map = new Dictionary<string, string>()
                {
                    { "SUBMIT", "提交" },
                    { "CANCELASSIGN", "撤销" },
                    { "AUDIT", "审核" },
                    { "UNAUDIT", "反审核" },
                };
                FDSrcAction = map[opKey];
                foreach (var d in e.DataEntitys)
                {
                    FDSrcFID = d["Id"].ToString();
                    FDSrcBNo = d["BillNo"].ToString();
                    string DocumentStatus = d["DocumentStatus"].ToString();
                    if (opKey == "AUDIT" && DocumentStatus == "D")
                    {
                        throw new KDBusinessException("001", $"单据编号为：{FDSrcBNo}的单据为重新审核状态，请先提交后再进行操作！");
                    }
                    var objs = DB_GetFormData(FDSrcFID);
                    foreach (var obj in objs)
                    {
                        Trans = Transform(FDSrcBillID, obj);

                        FDSrcType = Trans["FDSrcType"];
                        FBraOffice = Trans["FBraOffice"];
                        FDSrcEntryID = Trans["FDSrcEntryID"];
                        FDSrcSEQ = Trans["FDSrcSEQ"];
                        FDCostPrj = Trans["FDCostPrj"];
                        FPreCost = Trans["FPreCost"];
                        FReCost = Trans["FReCost"];
                        FNote = Trans["FNote"];
                        decimal amt = Convert.ToDecimal(FReCost) - Convert.ToDecimal(FPreCost);
                        if (FDSrcType == "资金") // 资金要确认仅注册审核、反审核操作
                        {
                            string srcBillNo = obj["FSourceBillNo"].ToString();
                            
                            switch(FDSrcAction)
                            {
                                case "提交":
                                case "撤销":
                                    // 判断源单单号是否存在，若存在，则跳过，不需要占用、撤回预算占用流水
                                    if (!srcBillNo.IsNullOrEmptyOrWhiteSpace()) continue;
                                    FDSrcType = "立项";
                                    break;
                                case "审核":
                                    if (amt != 0) 
                                    {
                                        if(srcBillNo.IsNullOrEmptyOrWhiteSpace()) 
                                        {
                                            string srcType = "立项";
                                            // 源单不存在时，增加一条立项审核的预算占用调整记录
                                            sql += String.Format(@"exec proc_czly_InsertBudgetFlowS
                                                @FBraOffice='{0}',@FDSrcType='{1}',@FDSrcAction='{2}',@FDSrcBillID='{3}',
                                                @FDSrcFID='{4}',@FDSrcBNo='{5}',@FDSrcEntryID='{6}',@FDSrcSEQ='{7}',
                                                @FDCostPrj='{8}',@FPreCost='{9}',@FReCost='{10}',@FNote='{11}';
                                                ", FBraOffice, srcType, FDSrcAction, FDSrcBillID,
                                                    FDSrcFID, FDSrcBNo, FDSrcEntryID, FDSrcSEQ,
                                                    FDCostPrj, FPreCost, amt, FNote);
                                        }
                                        else
                                        {
                                            // 源单存在时，反写源单
                                            backSql += GeneBackWriteSQL(FDSrcBillID, FDSrcEntryID, amt);
                                        }
                                    }
                                    break;
                                case "反审核":
                                    // 若源单不存在
                                    if (srcBillNo.IsNullOrEmptyOrWhiteSpace())
                                    {
                                        // 增加一条立项审核的预算占用撤回记录
                                        string realCost = FReCost;
                                        if (DocumentStatus == "B") realCost = FPreCost;
                                        string srcType = "立项";
                                        sql += String.Format(@"exec proc_czly_InsertBudgetFlowS
                                                @FBraOffice='{0}',@FDSrcType='{1}',@FDSrcAction='{2}',@FDSrcBillID='{3}',
                                                @FDSrcFID='{4}',@FDSrcBNo='{5}',@FDSrcEntryID='{6}',@FDSrcSEQ='{7}',
                                                @FDCostPrj='{8}',@FPreCost='{9}',@FReCost='{10}',@FNote='{11}';
                                                ", FBraOffice, srcType, FDSrcAction, FDSrcBillID,
                                                    FDSrcFID, FDSrcBNo, FDSrcEntryID, FDSrcSEQ,
                                                    FDCostPrj, FPreCost, realCost, FNote);
                                    } 
                                    // 若源单存在
                                    else 
                                    {
                                        // 资金在审核中反审核时，则跳过处理
                                        if (DocumentStatus == "B") continue;
                                        // 撤回审核时对源单的反写
                                        if(amt != 0) backSql += GeneBackWriteSQL(FDSrcBillID, FDSrcEntryID, amt);
                                    }
                                    
                                    break;
                            }
                        }
                        //审核中进行反审核
                        else if (FDSrcAction == "反审核" && DocumentStatus == "B") FReCost = Trans["FPreCost"];
                        
                        // 立项审核时如果金额没有变动，则不再进行预算占用调整
                        if(FDSrcType == "立项" && FDSrcAction == "审核") 
                        {
                            if (amt == 0) continue;
                            FReCost = amt.ToString();
                        }
                        
                        sql += String.Format(@"exec proc_czly_InsertBudgetFlowS
	                             @FBraOffice='{0}',@FDSrcType='{1}',@FDSrcAction='{2}',@FDSrcBillID='{3}',
	                             @FDSrcFID='{4}',@FDSrcBNo='{5}',@FDSrcEntryID='{6}',@FDSrcSEQ='{7}',
	                             @FDCostPrj='{8}',@FPreCost='{9}',@FReCost='{10}',@FNote='{11}';
                                ", FBraOffice, FDSrcType, FDSrcAction, FDSrcBillID,
                                    FDSrcFID, FDSrcBNo, FDSrcEntryID, FDSrcSEQ,
                                    FDCostPrj, FPreCost, FReCost, FNote);
                        
                    }
                }
            }
            if (!sql.Equals(""))
            {
                DBUtils.Execute(this.Context, sql);
            }
            if (!backSql.Equals("")) 
            {
                backSql = "/*dialect*/" + backSql;
                DBUtils.Execute(this.Context, backSql);
            }
            
        }

        /// <summary>
        /// 反写源单的金额
        /// </summary>
        /// <param name="amt"></param>
        private string GeneBackWriteSQL(string formId, string entryId, decimal amt)
        {
            if(formId == "k0c6b452fa8154c4f8e8e5f55f96bcfac") // 个人资金
            {
                string sql = $"select FSId, FSTableName from ora_t_PersonMoneyEntry_LK where FEntryId={entryId}";
                var item = DBUtils.ExecuteDynamicObject(Context, sql).FirstOrDefault();
                if (item == null) return "";
                sql = "";
                switch(item["FSTableName"].ToString())
                {
                    case "ora_t_TravelApplyEntry": //出差
                        sql = string.Format(
@"update ora_t_TravelApplyEntry set FAppliedMoney=FAppliedMoney+{0} where FEntryId={1};
update ora_t_TravelApplyEntry set FSourceStatus=case when FAppliedMoney<FActualCost then 'A' else 'B' end where FEntryId={1};
", amt, item["FSId"]);
                        break;
                    case "ora_t_Cust100050": // 费用立项
                        sql = string.Format(
@"update ora_t_Cust100050 set FAppliedMoney=FAppliedMoney+{0} where FID={1};
update ora_t_Cust100050 set FCommitStatus=case when FAppliedMoney<FCommitAmount then 'A' else 'B' end where FID={1};
", amt, item["FSId"]);
                        break;
                    case "ora_t_ServeFee": // 招待费用申请
                        sql = string.Format(
@"update ora_t_ServeFee set FAppliedMoney=FAppliedMoney+{0} where FID={1};
update ora_t_ServeFee set FSourceStatus=case when FAppliedMoney<FACTUALCOST then 'A' else 'B' end where FID={1};
", amt, item["FSId"]);
                        break;
                }
                return sql;
            }
            else if(formId == "k191b3057af6c4252bcea813ff644cd3a") // 对公资金
            {
                string sql = $"select FSId, FSTableName from ora_t_PublicMoneyEntry_LK where FEntryId={entryId}";
                var item = DBUtils.ExecuteDynamicObject(Context, sql).FirstOrDefault();
                if (item == null) return "";
                sql = "";
                switch(item["FSTableName"].ToString())
                {
                    case "ora_t_PublicSubmitEntry": // 对公费用报销
                        sql = string.Format(
@"update ora_t_PublicSubmitEntry set FAppliedMoney=FAppliedMoney+{0} where FEntryId={1};
update ora_t_PublicSubmitEntry set FBillStatus=case when FAppliedMoney<FRealAmount then 'A' else 'B' end where FEntryId={1};
", amt, item["FSId"]);
                        break;
                    case "ora_t_ContractReviewHead": // 非生产采购合同评审
                        sql = string.Format(
@"update ora_t_ContractReviewHead set FAppliedMoney=FAppliedMoney+{0} where FID={1};
update ora_t_ContractReviewHead set FCloseStatus=case when FAppliedMoney<FRealAmt then 'A' else 'B' end where FID={1};
", amt, item["FSId"]);
                        break;
                    case "ora_t_PCReviewEntry": // 生产采购合同评审
                        sql = string.Format(
@"update ora_t_PCReviewEntry set FAppliedMoney=FAppliedMoney+{0} where FEntryId={1};
update ora_t_PCReviewEntry set FCloseStatus=case when FAppliedMoney<FIncludeTaxAmountItem then 'A' else 'B' end where FEntryId={1};
", amt, item["FSId"]);
                        break;
                }
                
                return sql;
            }

            return "";
        }

        private Dictionary<string, string> Transform(string FormId, DynamicObject obj)
        {
            var dict = new Dictionary<string, string>();
            switch (FormId)
            {
                case "ora_FYLX"://费用立项
                    dict.Add("FDSrcType", "立项");
                    dict.Add("FBraOffice", obj["FUseOrgId"].ToString());
                    dict.Add("FDSrcEntryID", "0");
                    dict.Add("FDSrcSEQ", "0");
                    dict.Add("FDCostPrj", obj["FCostType1"].ToString());
                    dict.Add("FPreCost", obj["FExpectCost1"].ToString());
                    dict.Add("FReCost", obj["FCommitAmount"].ToString());
                    dict.Add("FNote", "费用立项 ");
                    break;
                case "k1ae2591790044d95b9966ad0dff1d987"://招待费用申请
                    dict.Add("FDSrcType", "立项");
                    dict.Add("FBraOffice", obj["F_ora_OrgId"].ToString());
                    dict.Add("FDSrcEntryID", "0");
                    dict.Add("FDSrcSEQ", "0");
                    dict.Add("FDCostPrj", obj["FCostType"].ToString());
                    dict.Add("FPreCost", obj["F_ora_Amount"].ToString());
                    dict.Add("FReCost", obj["FACTUALCOST"].ToString());
                    dict.Add("FNote", "招待费用申请 ");
                    break;
                case "k0c30c431418e4cf4a60d241a18cb241c"://出差申请
                    dict.Add("FDSrcType", "立项");
                    dict.Add("FBraOffice", obj["FOrgId"].ToString());
                    dict.Add("FDSrcEntryID", obj["FEntryId"].ToString());
                    dict.Add("FDSrcSEQ", obj["FSEQ"].ToString());
                    dict.Add("FDCostPrj", obj["FCostType"].ToString());
                    dict.Add("FPreCost", obj["FExpectCost"].ToString());
                    dict.Add("FReCost", obj["FActualCost"].ToString());
                    dict.Add("FNote", "出差申请 ");
                    break;
                case "k3972241808034802b04c3d18d4107afd"://生产采购合同评审
                    dict.Add("FDSrcType", "立项");
                    dict.Add("FBraOffice", obj["FOrgId"].ToString());
                    dict.Add("FDSrcEntryID", obj["FEntryId"].ToString());
                    dict.Add("FDSrcSEQ", obj["FSEQ"].ToString());
                    dict.Add("FDCostPrj", obj["FCostType"].ToString());
                    dict.Add("FPreCost", obj["FIncludeTaxAmountItem"].ToString());
                    dict.Add("FReCost", obj["FIncludeTaxAmountItem"].ToString());
                    dict.Add("FNote", "生产采购合同评审 ");
                    break;
                case "kbb14985fbec4445c846533837b2eea65"://非生产采购合同评审
                    dict.Add("FDSrcType", "立项");
                    dict.Add("FBraOffice", obj["FOrgId"].ToString());
                    dict.Add("FDSrcEntryID", "0");
                    dict.Add("FDSrcSEQ", "0");
                    dict.Add("FDCostPrj", obj["FCostItem"].ToString());
                    dict.Add("FPreCost", obj["FPreAmt"].ToString());
                    dict.Add("FReCost", obj["FRealAmt"].ToString());
                    dict.Add("FNote", "非生产采购合同评审 ");
                    break;
                case "k0c6b452fa8154c4f8e8e5f55f96bcfac"://个人资金申请
                    dict.Add("FDSrcType", "资金");
                    dict.Add("FBraOffice", obj["FPayOrgId"].ToString());
                    dict.Add("FDSrcEntryID", obj["FEntryID"].ToString());
                    dict.Add("FDSrcSEQ", obj["FSEQ"].ToString());
                    dict.Add("FDCostPrj", obj["FCostItem"].ToString());
                    dict.Add("FPreCost", obj["FApplyAmt"].ToString());
                    dict.Add("FReCost", obj["FAllowAmt"].ToString());
                    dict.Add("FNote", "个人资金申请 ");
                    break;
                case "k191b3057af6c4252bcea813ff644cd3a"://对公资金申请
                    dict.Add("FDSrcType", "资金");
                    dict.Add("FBraOffice", obj["FPayOrgId"].ToString());
                    dict.Add("FDSrcEntryID", obj["FEntryID"].ToString());
                    dict.Add("FDSrcSEQ", obj["FSEQ"].ToString());
                    dict.Add("FDCostPrj", obj["FCostItem"].ToString());
                    dict.Add("FPreCost", obj["FApplyAmt"].ToString());
                    dict.Add("FReCost", obj["FAllowAmt"].ToString());
                    dict.Add("FNote", "对公资金申请 ");
                    break;
                // 不用
                case "k5c88e2dc1ac14349935d452e74e152c8"://对公费用报销
                    dict.Add("FDSrcType", "报销");
                    dict.Add("FBraOffice", obj["FOrgId"].ToString());
                    dict.Add("FDSrcEntryID", obj["FEntryID"].ToString());
                    dict.Add("FDSrcSEQ", obj["FSEQ"].ToString());
                    dict.Add("FDCostPrj", obj["FCostType"].ToString());
                    dict.Add("FPreCost", obj["FAmount"].ToString());
                    dict.Add("FReCost", obj["FCheckAmount"].ToString()); //财务复核金额
                    dict.Add("FNote", "对公费用报销 ");
                    break;
                case "kaa55d0cac0c5447bbc6700cfbdf0b11e"://对公费用立项
                    dict.Add("FDSrcType", "立项");
                    dict.Add("FBraOffice", obj["FOrgId"].ToString());
                    dict.Add("FDSrcEntryID", "0");
                    dict.Add("FDSrcSEQ", "0");
                    dict.Add("FDCostPrj", obj["FCostType1"].ToString());
                    dict.Add("FPreCost", obj["FExpectCost1"].ToString());
                    dict.Add("FReCost", obj["FCommitAmount"].ToString());
                    dict.Add("FNote", "对公费用立项 ");
                    break;
                case "k6575db4ed77c449f88dd20cceef75a73"://出差报销
                    dict.Add("FDSrcType", "报销");
                    dict.Add("FBraOffice", obj["FOrgId"].ToString());
                    dict.Add("FDSrcEntryID", obj["FEntryID"].ToString());
                    dict.Add("FDSrcSEQ", obj["FSEQ"].ToString());
                    dict.Add("FDCostPrj", obj["FCostType"].ToString());
                    dict.Add("FPreCost", obj["FCarAmount"].ToString());
                    dict.Add("FReCost", obj["FCheckAmount"].ToString()); //财务复核金额
                    dict.Add("FNote", "出差报销 ");
                    break;
                case "ke6d80dfd260e4ef88d75f69f4c7ef0a1"://个人费用立项
                    dict.Add("FDSrcType", "立项");
                    dict.Add("FBraOffice", obj["FOrgId"].ToString());
                    dict.Add("FDSrcEntryID", "0");
                    dict.Add("FDSrcSEQ", "0");
                    dict.Add("FDCostPrj", obj["FCostType1"].ToString());
                    dict.Add("FPreCost", obj["FPREMONEY1"].ToString());
                    dict.Add("FReCost", obj["FCommitAmount"].ToString());
                    dict.Add("FNote", "个人费用立项 ");
                    break;
                case "k767a317ad28e40f1b25e95b92e218fea"://个人费用报销
                    dict.Add("FDSrcType", "报销");
                    dict.Add("FBraOffice", obj["F_ora_OrgId"].ToString());
                    dict.Add("FDSrcEntryID", obj["FEntryID"].ToString());
                    dict.Add("FDSrcSEQ", obj["FSEQ"].ToString());
                    dict.Add("FDCostPrj", obj["FCostItem"].ToString());
                    dict.Add("FPreCost", obj["FAmount"].ToString());
                    dict.Add("FReCost", obj["FCheckAmount"].ToString()); //财务复核金额
                    dict.Add("FNote", "个人费用报销 ");
                    break;
                case "kdcdde6ac18cb4d419a6924b49a593460"://招待费用报销
                    dict.Add("FDSrcType", "报销");
                    dict.Add("FBraOffice", obj["F_ora_OrgId"].ToString());
                    dict.Add("FDSrcEntryID", obj["FEntryID"].ToString());
                    dict.Add("FDSrcSEQ", obj["FSEQ"].ToString());
                    dict.Add("FDCostPrj", obj["FCostItem"].ToString());
                    dict.Add("FPreCost", obj["F_ora_Money"].ToString());
                    dict.Add("FReCost", obj["FCheckAmount"].ToString()); //财务复核金额
                    dict.Add("FNote", "招待费用报销 ");
                    break;
            }
            return dict;
        }
        
        /// <summary>
        /// 通过FID获取单据数据
        /// </summary>
        /// <param name="FID"></param>
        /// <returns></returns>
        private DynamicObjectCollection DB_GetFormData(string FID)
        {
            string formid = CZ_GetFormType();
            string[] tb = FormIdToTbNameMap[formid];
            string t_head = tb[0];
            string t_entry = tb[1];
            string sql = "";
            if (t_entry == "")
            {
                sql = string.Format("select * from {0} where FID='{1}'", t_head, FID);
            }
            else
            {
                sql = string.Format(@"select * from {0} h 
                                    inner join {1} e on h.FID=e.FID
                                    where h.FID='{2}'", t_head, t_entry, FID);
            }
            var objs = DBUtils.ExecuteDynamicObject(this.Context, sql);
            return objs;
        }

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
        /// 获取单据标识 FormType | FormID
        /// </summary>
        /// <returns></returns>
        private string CZ_GetFormType()
        {
            //string _BI_DTONS = this.BusinessInfo.DTONS;     //"Kingdee.BOS.ServiceInterface.Temp.ora_test_Table002"
            string[] _BI_DTONS_C = this.BusinessInfo.DTONS.Split('.');
            return _BI_DTONS_C[_BI_DTONS_C.Length - 1].ToString();
        }

        #endregion
    }
}
