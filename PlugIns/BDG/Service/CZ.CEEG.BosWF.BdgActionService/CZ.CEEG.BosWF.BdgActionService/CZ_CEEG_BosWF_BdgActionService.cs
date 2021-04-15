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
            //个人资金借支
            {"k0c6b452fa8154c4f8e8e5f55f96bcfac", new string[] { "ora_t_PersonMoney", "" } },
            //对公资金申请
            { "k191b3057af6c4252bcea813ff644cd3a", new string[] { "ora_t_Cust100011", "" } },

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


        #region override

        public override void OnPreparePropertys(PreparePropertysEventArgs e)
        {
            base.OnPreparePropertys(e);
            e.FieldKeys.Add("DocumentStatus");
        }

        public override void EndOperationTransaction(EndOperationTransactionArgs e)
        {
            base.EndOperationTransaction(e);
            if (IsUsingBdgSys())
            {
                InsertFlow(e);
            }
        }

        #endregion

        #region Actions

        private void InsertFlow(EndOperationTransactionArgs e)
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
            Dictionary<string, string> Trans;
            if (opKey == "UNAUDIT")
            {
                FDSrcAction = "反审核";
                foreach (var d in e.DataEntitys)
                {
                    FDSrcFID = d["Id"].ToString();
                    FDSrcBNo = d["BillNo"].ToString();
                    var objs = DB_GetFormData(FDSrcFID);
                    foreach (var obj in objs)
                    {
                        string DocumentStatus = d["DocumentStatus"].ToString();

                        Trans = Transform(FDSrcBillID, obj);
                        FDSrcType = Trans["FDSrcType"];
                        FBraOffice = Trans["FBraOffice"];
                        FDSrcEntryID = Trans["FDSrcEntryID"];
                        FDSrcSEQ = Trans["FDSrcSEQ"];
                        FDCostPrj = Trans["FDCostPrj"];
                        FPreCost = Trans["FPreCost"];
                        if(FDSrcType == "立项")
                        {
                            FReCost = Trans["FPreCost"];
                        }
                        else if(FDSrcType == "报销")
                        {
                            FReCost = Trans["FReCost"];
                        }
                        FNote = Trans["FNote"];
                        if (DocumentStatus == "B") //审核中进行反审核
                        {
                            FReCost = Trans["FPreCost"];
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
            else if (opKey == "SUBMIT" || opKey == "CANCELASSIGN" || opKey == "AUDIT")
            {
                
                FDSrcAction = opKey == "SUBMIT" ? "提交" : (opKey == "CANCELASSIGN" ? "撤销" : "审核");
                foreach (var d in e.DataEntitys)
                {
                    FDSrcFID = d["Id"].ToString();
                    FDSrcBNo = d["BillNo"].ToString();
                    if (opKey == "SUBMIT" && d["DocumentStatus"].ToString() == "D")
                    {
                        throw new KDBusinessException("001", $"单据编号为：{FDSrcBNo}的单据为重新审核状态，请先提交后再进行操作！");
                    }
                    var objs = DB_GetFormData(FDSrcFID);
                    foreach (var obj in objs)
                    {
                        //string DocumentStatus = d["DocumentStatus"].ToString();
                        Trans = Transform(FDSrcBillID, obj);

                        FDSrcType = Trans["FDSrcType"];
                        FBraOffice = Trans["FBraOffice"];
                        FDSrcEntryID = Trans["FDSrcEntryID"];
                        FDSrcSEQ = Trans["FDSrcSEQ"];
                        FDCostPrj = Trans["FDCostPrj"];
                        FPreCost = Trans["FPreCost"];
                        //if (FDSrcType == "立项")
                        //{
                        //    FReCost = Trans["FPreCost"];
                        //}
                        //else if (FDSrcType == "报销")
                        //{
                        //    FReCost = Trans["FReCost"];
                        //}
                        FReCost = Trans["FReCost"];
                        FNote = Trans["FNote"];
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
            DBUtils.Execute(this.Context, sql);
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
            //throw new KDBusinessException("001", $"单据的FormId: {formid}");
            string[] tb = FormIdToTbNameMap[formid];
            string t_head = tb[0];
            string t_entry = tb[1];
            //throw new KDBusinessException("001", $"单据头表名: {t_head}，单据体表名: {t_entry}");
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
