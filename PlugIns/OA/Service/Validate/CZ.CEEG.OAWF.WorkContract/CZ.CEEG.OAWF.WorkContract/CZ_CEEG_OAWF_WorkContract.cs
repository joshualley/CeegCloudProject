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


namespace CZ.CEEG.OAWF.WorkContract
{
    [Description("工作联系单审批中验证")]
    [HotUpdate]
    public class CZ_CEEG_OAWF_WorkContract : AbstractOperationServicePlugIn
    {
        //private const int GM_NodeID = 18; //单位总经理 确认被联系组织 被联系部门
        private const int ContractedGM_NodeID = 931; //被联系人单位总经理 分配执行人
        private const int AP_NodeID = 478; //申请人评价

        public override void OnPreparePropertys(PreparePropertysEventArgs e)
        {
            e.FieldKeys.Add("FContractOrgId"); //被联系组织
            e.FieldKeys.Add("FContractCompany"); //被联系部门
            e.FieldKeys.Add("FExecutor"); //执行人
            e.FieldKeys.Add("FSatisfied"); //满意
            e.FieldKeys.Add("FNotSatisfied"); //不满意
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
        private class PerValidator : AbstractValidator
        {
            private string formId;

            public PerValidator(string formId)
            {
                this.formId = formId;
            }

            public override void Validate(Kingdee.BOS.Core.ExtendedDataEntity[] dataEntities, ValidateContext validateContext, Kingdee.BOS.Context ctx)
            {

                foreach (var dataEntity in dataEntities)
                {
                    string FID = dataEntity["Id"].ToString();
                    string procInstId = WorkflowChartServiceHelper.GetProcInstIdByBillInst(ctx, this.formId, FID);
                    List<ChartActivityInfo> routeCollection = WorkflowChartServiceHelper.GetProcessRouter(ctx, procInstId);
                    var WFNode = routeCollection[routeCollection.Count - 1];
                    /*
                    if (WFNode.ActivityId == GM_NodeID) //单位总经理
                    {
                        string FContractOrgId = dataEntity["FContractOrgId"] == null ? "" : (dataEntity["FContractOrgId"] as DynamicObject)["Id"].ToString();
                        string FContractCompany = dataEntity["FContractCompany"] == null ? "" : (dataEntity["FContractCompany"] as DynamicObject)["Id"].ToString();
                        if (FContractOrgId == "" || FContractCompany == "")
                        {
                            ValidationErrorInfo ValidationErrorInfo = new ValidationErrorInfo(
                            string.Empty,
                            FID,
                            dataEntity.DataEntityIndex,
                            dataEntity.RowIndex,
                            FID,
                            "请选择被联系组织及部门！",
                            string.Empty);
                            validateContext.AddError(null, ValidationErrorInfo);
                        }
                    }
                    */
                    if(WFNode.ActivityId == ContractedGM_NodeID)//被联系单位总经理
                    {
                        var FExecutor = dataEntity["FExecutor"] as DynamicObjectCollection;
                        if (FExecutor.Count <= 0)
                        {
                            ValidationErrorInfo ValidationErrorInfo = new ValidationErrorInfo(
                            string.Empty,
                            FID,
                            dataEntity.DataEntityIndex,
                            dataEntity.RowIndex,
                            FID,
                            "请分配执行人！",
                            string.Empty);
                            validateContext.AddError(null, ValidationErrorInfo);
                        }
                    }
                    else if (WFNode.ActivityId == AP_NodeID)//申请人评价
                    {
                        string FNotSatisfied = dataEntity["FNotSatisfied"].ToString();
                        string FSatisfied = dataEntity["FSatisfied"].ToString();
                        if ((FNotSatisfied == "False" && FSatisfied == "False") || (FNotSatisfied == "True" && FSatisfied == "True"))
                        {
                            ValidationErrorInfo ValidationErrorInfo = new ValidationErrorInfo(
                            string.Empty,
                            FID,
                            dataEntity.DataEntityIndex,
                            dataEntity.RowIndex,
                            FID,
                            "请对服务进行评价！",
                            string.Empty);
                            validateContext.AddError(null, ValidationErrorInfo);
                        }
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
