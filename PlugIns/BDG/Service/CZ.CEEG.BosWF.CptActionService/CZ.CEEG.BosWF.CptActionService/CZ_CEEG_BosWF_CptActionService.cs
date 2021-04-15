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

namespace CZ.CEEG.BosWF.CptActionService
{
    [Description("资金流水服务插件")]
    [HotUpdate]
    public class CZ_CEEG_BosWF_CptActionService : AbstractOperationServicePlugIn
    {
        #region override

        public override void OnPreparePropertys(PreparePropertysEventArgs e)
        {
            base.OnPreparePropertys(e);
            e.FieldKeys.Add("DocumentStatus");
        }

        public override void BeginOperationTransaction(BeginOperationTransactionArgs e)
        {
            base.BeginOperationTransaction(e);
            if(IsUsingBdgSys())
            {
                InsertFlow(e);
            }
        }

        #endregion

        #region 功能函数
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
            string FDCptType = "";
            string FDCostPrj = "";
            string FPreCost = "0";
            string FReCost = "0";
            string FNote = "";

            string opKey = this.FormOperation.Operation.ToUpperInvariant();
            string sql = "";
            if (opKey == "UNAUDIT")
            {
                string formId = CZ_GetFormType();
                FDSrcAction = "反审核";
                foreach (var d in e.DataEntitys)
                {
                    FDSrcFID = d["Id"].ToString();
                    FDSrcBNo = d["BillNo"].ToString();
                    var objs = DB_GetFormData(FDSrcFID);
                    foreach (var obj in objs)
                    {
                        var trans = Transform(formId, obj);
                        string DocumentStatus = d["DocumentStatus"].ToString();

                        FDSrcType = "付款";
                        FBraOffice = trans["FBraOffice"].ToString(); //付款组织
                        FDSrcEntryID = trans["FDSrcEntryID"].ToString();
                        FDSrcSEQ = trans["FDSrcSEQ"].ToString();
                        FDCostPrj = trans["FDCostPrj"].ToString(); //费用项目
                        FDCptType = trans["FDCptType"].ToString(); //结算方式
                        FPreCost = trans["FPreCost"].ToString(); //应付金额
                        FReCost = trans["FReCost"].ToString(); //实付金额
                        FNote = trans["FNote"];
                        if (DocumentStatus == "B") //审核中进行反审核
                        {
                            FReCost = FPreCost;
                        }
                        sql += String.Format(@"exec proc_czly_InsertCapitalFlowS
	                             @FBraOffice='{0}',@FDSrcType='{1}',@FDSrcAction='{2}',@FDSrcBillID='{3}',
	                             @FDSrcFID='{4}',@FDSrcBNo='{5}',@FDSrcEntryID='{6}',@FDSrcSEQ='{7}',@FDCptType='{8}',
	                             @FDCostPrj='{9}',@FPreCost='{10}',@FReCost='{11}',@FNote='{12}';
                                ", FBraOffice, FDSrcType, FDSrcAction, FDSrcBillID,
                                    FDSrcFID, FDSrcBNo, FDSrcEntryID, FDSrcSEQ, FDCptType,
                                    FDCostPrj, FPreCost, FReCost, FNote);
                    }
                }
            }
            else if (opKey == "SUBMIT" || opKey == "CANCELASSIGN" || opKey == "AUDIT")
            {
                FDSrcAction = opKey == "SUBMIT" ? "提交" : (opKey == "CANCELASSIGN" ? "撤销" : "审核");
                string formId = CZ_GetFormType();
                
                foreach (var d in e.DataEntitys)
                {
                    FDSrcFID = d["Id"].ToString();
                    FDSrcBNo = d["BillNo"].ToString();
                    if(opKey == "AUDIT" && d["DocumentStatus"].ToString() == "D")
                    {
                        throw new KDBusinessException("001", $"单据编号为：{FDSrcBNo}单据为重新审核状态，请提交后再进行审核操作！");
                    }
                    var objs = DB_GetFormData(FDSrcFID);
                    
                    foreach (var obj in objs)
                    {
                        var trans = Transform(formId, obj);
                        FDSrcType = "付款";
                        FBraOffice = trans["FBraOffice"].ToString(); //付款组织
                        FDSrcEntryID = trans["FDSrcEntryID"].ToString();
                        FDSrcSEQ = trans["FDSrcSEQ"].ToString();
                        FDCostPrj = trans["FDCostPrj"].ToString(); //费用项目
                        FDCptType = trans["FDCptType"].ToString(); //结算方式
                        FPreCost = trans["FPreCost"].ToString(); //应付金额
                        FReCost = trans["FReCost"].ToString(); //实付金额
                        FNote = trans["FNote"];
                        sql += String.Format(@"exec proc_czly_InsertCapitalFlowS
	                             @FBraOffice='{0}',@FDSrcType='{1}',@FDSrcAction='{2}',@FDSrcBillID='{3}',
	                             @FDSrcFID='{4}',@FDSrcBNo='{5}',@FDSrcEntryID='{6}',@FDSrcSEQ='{7}',@FDCptType='{8}',
	                             @FDCostPrj='{9}',@FPreCost='{10}',@FReCost='{11}',@FNote='{12}';
                                ", FBraOffice, FDSrcType, FDSrcAction, FDSrcBillID,
                                    FDSrcFID, FDSrcBNo, FDSrcEntryID, FDSrcSEQ, FDCptType,
                                    FDCostPrj, FPreCost, FReCost, FNote);
                    }
                }
            }
            DBUtils.Execute(this.Context, sql);
        }

        private Dictionary<string, string> Transform(string formId, DynamicObject obj)
        {
            var dict = new Dictionary<string, string>();
            switch (formId)
            {
                case "k0c6b452fa8154c4f8e8e5f55f96bcfac": //个人资金借支
                    dict.Add("FNote", "个人资金借支");
                    dict.Add("FBraOffice", obj["FPayOrgId"].ToString());
                    dict.Add("FDSrcEntryID", "0");
                    dict.Add("FDSrcSEQ", "0");
                    dict.Add("FDCostPrj", obj["FCostType"].ToString());
                    dict.Add("FDCptType", obj["FPayType"].ToString());
                    dict.Add("FPreCost", obj["FAmount"].ToString());
                    dict.Add("FReCost", obj["FStatusAmount"].ToString());
                    break;
                case "k191b3057af6c4252bcea813ff644cd3a": //对公资金申请
                    dict.Add("FNote", "对公资金申请");
                    dict.Add("FBraOffice", obj["FPayOrgId"].ToString());
                    dict.Add("FDSrcEntryID", "0");
                    dict.Add("FDSrcSEQ", "0");
                    dict.Add("FDCostPrj", obj["FCostItem"].ToString());
                    dict.Add("FDCptType", obj["FPayWay"].ToString());
                    dict.Add("FPreCost", obj["FPreCost"].ToString());
                    dict.Add("FReCost", obj["FRealMoney"].ToString());
                    break;
            }
            return dict;
        }

        private DynamicObjectCollection DB_GetFormData(string FID)
        {
            string formId = CZ_GetFormType();
            string sql = "";
            switch (formId)
            {
                case "k0c6b452fa8154c4f8e8e5f55f96bcfac": //个人资金借支
                    sql = "select * from ora_t_PersonMoney where FID='" + FID + "'";
                    break;
                case "k191b3057af6c4252bcea813ff644cd3a": //对公资金申请
                    sql = "select * from ora_t_Cust100011 where FID='" + FID + "'";
                    break;
            }
            return DBUtils.ExecuteDynamicObject(this.Context, sql);
        }
        #endregion

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

       
    }
}
