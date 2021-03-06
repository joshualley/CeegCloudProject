﻿using System;
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


        public override void EndOperationTransaction(EndOperationTransactionArgs e)
        {
            base.EndOperationTransaction(e);
        }

        #endregion

        #region 功能函数
        private void InsertFlow(BeginOperationTransactionArgs e)
        {
            string FBraOffice = "0";
            //string FYear = "0";
            //string FMonth = "0";
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
                FDSrcAction = "反审核";
                foreach (var d in e.DataEntitys)
                {
                    FDSrcFID = d["Id"].ToString();
                    FDSrcBNo = d["BillNo"].ToString();
                    var objs = DB_GetFormData(FDSrcFID);
                    foreach (var obj in objs)
                    {
                        string DocumentStatus = d["DocumentStatus"].ToString();

                        FDSrcType = "付款";
                        FBraOffice = obj["FPAYORGID"].ToString(); //付款组织
                        FDSrcEntryID = obj["FEntryID"].ToString(); 
                        FDSrcSEQ = obj["FSeq"].ToString();
                        FDCostPrj = obj["FCOSTID"].ToString(); //费用项目
                        FDCptType = obj["FSETTLETYPEID"].ToString(); //结算方式
                        FPreCost = obj["FPAYTOTALAMOUNTFOR"].ToString(); //应付金额
                        FReCost = obj["FREALPAYAMOUNTFOR_D"].ToString(); //实付金额
                        FNote = "";
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
                foreach (var d in e.DataEntitys)
                {
                    FDSrcFID = d["Id"].ToString();
                    FDSrcBNo = d["BillNo"].ToString();
                    var objs = DB_GetFormData(FDSrcFID);
                    foreach (var obj in objs)
                    {
                        FDSrcType = "付款";
                        FBraOffice = obj["FPAYORGID"].ToString(); //付款组织
                        FDSrcEntryID = obj["FEntryID"].ToString();
                        FDSrcSEQ = obj["FSeq"].ToString();
                        FDCostPrj = obj["FCOSTID"].ToString(); //费用项目
                        FDCptType = obj["FSETTLETYPEID"].ToString(); //结算方式
                        FPreCost = obj["FPAYTOTALAMOUNTFOR"].ToString(); //应付金额
                        FReCost = obj["FREALPAYAMOUNTFOR_D"].ToString(); //实付金额
                        FNote = "";
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
            CZDB_GetData(sql);
        }
        private DynamicObjectCollection DB_GetFormData(string FID)
        {
            string sql = "select * from T_AP_PAYBILLENTRY where FID='" + FID + "'";
            return CZDB_GetData(sql);
        }
        #endregion

        #region 获取FormID
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
        /// 获取单据标识 FormType | FormID
        /// </summary>
        /// <param name="o">数据对象</param>
        /// <param name="_Key">Key DF_Val:FFormID</param>
        /// <returns></returns>
        private string CZ_GetFormType(DynamicObject o, string _Key)
        {
            return o[_Key].ToString();
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
