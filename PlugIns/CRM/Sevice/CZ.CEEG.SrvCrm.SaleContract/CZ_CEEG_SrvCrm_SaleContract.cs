using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using Kingdee.BOS.Core.DynamicForm.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.Core.Validation;
using Kingdee.BOS.App.Data;
using Kingdee.BOS.Util;
using Kingdee.BOS.Contracts;
using Kingdee.BOS.Core.Const;
using Kingdee.BOS.Core.Metadata;
using Kingdee.BOS.Workflow.Interface;
using Kingdee.BOS.Workflow.ServiceHelper;
using Kingdee.BOS.Workflow.Models.Chart;
using Kingdee.BOS.Workflow.Kernel;
using Kingdee.BOS.Core;
using Kingdee.BOS;

namespace CZ.CEEG.SrvCrm.SaleContract
{
    [Description("销售合同流程中校验")]
    [HotUpdate]
    public class CZ_CEEG_SrvCrm_SaleContract : AbstractOperationServicePlugIn
    {
        /// <summary>
        /// 合同评审员
        /// </summary>
        private const int Contractor_NodeID = 527;
        /// <summary>
        /// 客户管理员
        /// </summary>
        private const int CustAdmin_NodeID = 534;
        /// BOM员
        /// </summary>
        private const int Bomer_NodeID = 231;

        public override void OnPreparePropertys(PreparePropertysEventArgs e)
        {
            e.FieldKeys.Add("FPayTerm"); //付款方式
            e.FieldKeys.Add("F_CZ_BillType"); //订单类型
            e.FieldKeys.Add("FSettleOrgId"); //签单组织
            e.FieldKeys.Add("FProdFactory"); //生产工厂

            e.FieldKeys.Add("FCustName"); //客户

            e.FieldKeys.Add("FEntityBPR"); //报价明细表体
            e.FieldKeys.Add("FBMtlGroup"); //大类
            e.FieldKeys.Add("FMaterialID"); //物料
        }

        /// <summary>
        /// 添加校验器
        /// </summary>
        /// <param name="e"></param>
        public override void OnAddValidators(AddValidatorsEventArgs e)
        {
            var operValidator = new PerValidator(CZ_GetFormType());
            operValidator.AlwaysValidate = true;
            operValidator.EntityKey = "FBillHead";
            e.Validators.Add(operValidator);
        }


        #region 校验器
        /// <summary>
        /// 分配执行人校验器
        /// </summary>
        private class PerValidator : AbstractValidator
        {
            private string formId;

            public PerValidator(string formId)
            {
                this.formId = formId;
            }

            /// <summary>
            /// 客户管理员
            /// </summary>
            /// <param name="dataEntity"></param>
            /// <param name="validateContext"></param>
            /// <param name="ctx"></param>
            /// <param name="FID"></param>
            private void Act_CtrlCustAdmin(ExtendedDataEntity dataEntity, ValidateContext validateContext, Context ctx, string FID)
            {
                string FCustId = dataEntity["FCustName"] == null ? "0" : (dataEntity["FCustName"] as DynamicObject)["Id"].ToString();
                if(FCustId == "0")
                {
                    ValidationErrorInfo ValidationErrorInfo = new ValidationErrorInfo(
                           string.Empty,
                           FID,
                           dataEntity.DataEntityIndex,
                           dataEntity.RowIndex,
                           FID,
                           "客户不能为空！",
                           string.Empty);
                    validateContext.AddError(null, ValidationErrorInfo);
                }
                string sql = string.Format("SELECT FISTRADE FROM T_BD_CUSTOMER WHERE FCUSTID='{0}'", FCustId);
                var objs = DBUtils.ExecuteDynamicObject(ctx, sql);
                if (objs.Count > 0)
                {
                    string FISTRADE = objs[0]["FISTRADE"].ToString();
                    if (FISTRADE == "0" || FISTRADE == "")
                    {
                        ValidationErrorInfo ValidationErrorInfo = new ValidationErrorInfo(
                           string.Empty,
                           FID,
                           dataEntity.DataEntityIndex,
                           dataEntity.RowIndex,
                           FID,
                           "当前客户为非交易客户，请转为交易客户后再进行提交！",
                           string.Empty);
                        validateContext.AddError(null, ValidationErrorInfo);
                    }
                    else
                    {
                        string FCustName = dataEntity["FCustName"] == null ? "" : (dataEntity["FCustName"] as DynamicObject)["Name"].ToString();
                        sql = "SELECT FCUSTID FROM T_BD_CUSTOMER_L WHERE FNAME='" + FCustName +"' AND FLOCALEID='2052'";
                        objs = DBUtils.ExecuteDynamicObject(ctx, sql);
                        if(objs.Count < 1)
                        {
                            ValidationErrorInfo ValidationErrorInfo = new ValidationErrorInfo(
                               string.Empty,
                               FID,
                               dataEntity.DataEntityIndex,
                               dataEntity.RowIndex,
                               FID,
                               "当前客户还未进行分配，请分配到相应组织后再进行提交！",
                               string.Empty);
                            validateContext.AddError(null, ValidationErrorInfo);
                        }
                    }
                }
                
            }

            /// <summary>
            /// 合同评审员
            /// </summary>
            /// <param name="dataEntity"></param>
            /// <param name="validateContext"></param>
            /// <param name="ctx"></param>
            /// <param name="FID"></param>
            private void Act_CtrlContractor(ExtendedDataEntity dataEntity, ValidateContext validateContext, Context ctx, string FID)
            {
                string FPayTerm = dataEntity["FPayTerm"] == null ? "0" : (dataEntity["FPayTerm"] as DynamicObject)["Id"].ToString();
                string F_CZ_BillType = dataEntity["F_CZ_BillType"] == null ? "0" : dataEntity["F_CZ_BillType"].ToString();
                string FSettleOrgId = dataEntity["FSettleOrgId"] == null ? "0" : (dataEntity["FSettleOrgId"] as DynamicObject)["Id"].ToString();
                
                if (FPayTerm == "0")
                {
                    ValidationErrorInfo ValidationErrorInfo = new ValidationErrorInfo(
                       string.Empty,
                       FID,
                       dataEntity.DataEntityIndex,
                       dataEntity.RowIndex,
                       FID,
                       "请选择付款方式！",
                       string.Empty);
                    validateContext.AddError(null, ValidationErrorInfo);
                }
                if (F_CZ_BillType == "")
                {
                    ValidationErrorInfo ValidationErrorInfo = new ValidationErrorInfo(
                       string.Empty,
                       FID,
                       dataEntity.DataEntityIndex,
                       dataEntity.RowIndex,
                       FID,
                       "请选择订单类型！",
                       string.Empty);
                    validateContext.AddError(null, ValidationErrorInfo);
                }
                if (FSettleOrgId == "0")
                {
                    ValidationErrorInfo ValidationErrorInfo = new ValidationErrorInfo(
                       string.Empty,
                       FID,
                       dataEntity.DataEntityIndex,
                       dataEntity.RowIndex,
                       FID,
                       "请选择签单组织！",
                       string.Empty);
                    validateContext.AddError(null, ValidationErrorInfo);
                }
                var entity = dataEntity["FEntityBPR"] as DynamicObjectCollection;
                bool IsNull = false;
                foreach(var row in entity)
                {
                    string FProdFactory = row["FProdFactory"] == null ? "0" : (row["FProdFactory"] as DynamicObject)["Id"].ToString();
                    if (FProdFactory == "0")
                    {
                        IsNull = true;
                    }
                }
                
                if (IsNull)
                {
                    ValidationErrorInfo ValidationErrorInfo = new ValidationErrorInfo(
                       string.Empty,
                       FID,
                       dataEntity.DataEntityIndex,
                       dataEntity.RowIndex,
                       FID,
                       "请选择表体的生产工厂！",
                       string.Empty);
                    validateContext.AddError(null, ValidationErrorInfo);
                }
            }

            /// <summary>
            /// Bom员
            /// </summary>
            /// <param name="dataEntity"></param>
            /// <param name="validateContext"></param>
            /// <param name="ctx"></param>
            /// <param name="FID"></param>
            private void Act_CtrlBomer(ExtendedDataEntity dataEntity, ValidateContext validateContext, Context ctx, string FID)
            {
                bool IsNull = false;
                var entity = dataEntity["FEntityBPR"] as DynamicObjectCollection;
                string FBMtlGroup = "0";
                string FMaterialID = "0";

                string userId = ctx.UserId.ToString();

                string sql = string.Format(@"SELECT emg.F_ORA_CRMMTLGP FROM
                        (SELECT * FROM T_SEC_USER WHERE FUSERID='{0}') u
                        INNER JOIN V_BD_CONTACTOBJECT c ON u.FLINKOBJECT=c.FID
                        INNER JOIN T_HR_EMPINFO e ON c.FNUMBER=e.FNUMBER AND e.F_ORA_ISCRMBOM=1
                        INNER JOIN T_HR_EMPINFOCrmMG emg ON e.FID=emg.FID", userId);
                var objs = DBUtils.ExecuteDynamicObject(ctx, sql);
                string MtlGp = "";
                

                for (int i = 0; i < entity.Count; i++)
                {
                    FBMtlGroup = entity[i]["FBMtlGroup"] == null ? "0" : (entity[i]["FBMtlGroup"] as DynamicObject)["Id"].ToString();
                    for (int j = 0; j < objs.Count; j++)
                    {
                        if (objs[j]["F_ORA_CRMMTLGP"].ToString() == FBMtlGroup)
                        {
                            FMaterialID = entity[i]["FMaterialID"] == null ? "0" : (entity[i]["FMaterialID"] as DynamicObject)["Id"].ToString();
                            if (FMaterialID == "0")
                            {
                                IsNull = true;
                                break;
                            }
                        }
                    }
                }

                if (IsNull)
                {
                    ValidationErrorInfo ValidationErrorInfo = new ValidationErrorInfo(
                        string.Empty,
                        FID,
                        dataEntity.DataEntityIndex,
                        dataEntity.RowIndex,
                        FID,
                        "请选择对应的物料！",
                        string.Empty);
                    validateContext.AddError(null, ValidationErrorInfo);
                }
            }

            public override void Validate(Kingdee.BOS.Core.ExtendedDataEntity[] dataEntities, ValidateContext validateContext, Kingdee.BOS.Context ctx)
            {

                foreach (var dataEntity in dataEntities)
                {
                    string FID = dataEntity["Id"].ToString();
                    string procInstId = WorkflowChartServiceHelper.GetProcInstIdByBillInst(ctx, this.formId, FID);
                    List<ChartActivityInfo> routeCollection = WorkflowChartServiceHelper.GetProcessRouter(ctx, procInstId);

                    var WFNode = routeCollection[routeCollection.Count - 1];
                    //客户管理员
                    if (WFNode.ActivityId == CustAdmin_NodeID)
                    {
                        Act_CtrlCustAdmin(dataEntity, validateContext, ctx, FID);
                    }
                    else if (WFNode.ActivityId == Contractor_NodeID)
                    {
                        Act_CtrlContractor(dataEntity, validateContext, ctx, FID);
                    }
                    else if (WFNode.ActivityId == Bomer_NodeID)
                    {
                        Act_CtrlBomer(dataEntity, validateContext, ctx, FID);
                    }
                }
            }
        }

        #endregion


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
