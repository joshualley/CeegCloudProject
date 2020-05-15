using Kingdee.BOS.Core.DynamicForm.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Core.Validation;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.Util;
using Kingdee.BOS.Workflow.Models.Chart;
using Kingdee.BOS.Workflow.ServiceHelper;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace CZ.CEEG.SrvCrm.AfterSaleService
{
    [Description("售后服务流程控制")]
    [HotUpdate]
    public class CZ_CEEG_SrvCrm_AfterSaleService : AbstractOperationServicePlugIn
    {
        private const int AllocProcessor_NodeID = 5;       //分配执行人节点ID
        private const int ProcessorInputResult_NodeID = 18; //执行人填写执行结果节点ID
        private const int AP_NodeID = 32; //申请人评价 满意 不满意

        public override void OnPreparePropertys(PreparePropertysEventArgs e)
        {
            
            e.FieldKeys.Add("FQstDoer"); //执行人
            e.FieldKeys.Add("FServeContent"); //服务内容
            e.FieldKeys.Add("FQstAnz"); //问题原因分析
            e.FieldKeys.Add("FQstPlan"); //问题处理方案
            e.FieldKeys.Add("FPlanEndDt"); //计划完成日期


            e.FieldKeys.Add("FFinEndDt"); //实际完成日期
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

            public override void Validate(Kingdee.BOS.Core.ExtendedDataEntity[] dataEntities, ValidateContext validateContext, Kingdee.BOS.Context ctx)
            {

                foreach (var dataEntity in dataEntities)
                {
                    string FID = dataEntity["Id"].ToString();
                    string procInstId = WorkflowChartServiceHelper.GetProcInstIdByBillInst(ctx, this.formId, FID);
                    List<ChartActivityInfo> routeCollection = WorkflowChartServiceHelper.GetProcessRouter(ctx, procInstId);

                    var WFNode = routeCollection[routeCollection.Count - 1];

                    if (WFNode.ActivityId == AllocProcessor_NodeID)
                    {
                        string FExecutor = dataEntity["FQstDoer"] == null ? "0" : (dataEntity["FQstDoer"] as DynamicObject).ToString();
                        if (FExecutor == "0")
                        {
                            ValidationErrorInfo ValidationErrorInfo = new ValidationErrorInfo(
                                string.Empty,
                                FID,
                                dataEntity.DataEntityIndex,
                                dataEntity.RowIndex,
                                FID,
                                "请先分配执行人！",
                                string.Empty);
                            validateContext.AddError(null, ValidationErrorInfo);
                        }
                    }
                    else if (WFNode.ActivityId == ProcessorInputResult_NodeID)
                    {
                        //if (dataEntity["FPlanEndDt"].ToString().IsNullOrEmptyOrWhiteSpace())
                        //{
                        //    ValidationErrorInfo ValidationErrorInfo = new ValidationErrorInfo(
                        //       string.Empty,
                        //       FID,
                        //       dataEntity.DataEntityIndex,
                        //       dataEntity.RowIndex,
                        //       FID,
                        //       "请填写计划完成日期！",
                        //       string.Empty);
                        //    validateContext.AddError(null, ValidationErrorInfo);
                        //}
                        if (dataEntity["FServeContent"].ToString().IsNullOrEmptyOrWhiteSpace())
                        {
                            ValidationErrorInfo ValidationErrorInfo = new ValidationErrorInfo(
                               string.Empty,
                               FID,
                               dataEntity.DataEntityIndex,
                               dataEntity.RowIndex,
                               FID,
                               "请填写服务内容！",
                               string.Empty);
                            validateContext.AddError(null, ValidationErrorInfo);
                        }
                        if (dataEntity["FQstAnz"].ToString().IsNullOrEmptyOrWhiteSpace())
                        {
                            ValidationErrorInfo ValidationErrorInfo = new ValidationErrorInfo(
                               string.Empty,
                               FID,
                               dataEntity.DataEntityIndex,
                               dataEntity.RowIndex,
                               FID,
                               "请填写问题原因分析！",
                               string.Empty);
                            validateContext.AddError(null, ValidationErrorInfo);
                        }
                        if (dataEntity["FQstPlan"].ToString().IsNullOrEmptyOrWhiteSpace())
                        {
                            ValidationErrorInfo ValidationErrorInfo = new ValidationErrorInfo(
                               string.Empty,
                               FID,
                               dataEntity.DataEntityIndex,
                               dataEntity.RowIndex,
                               FID,
                               "请填写问题处理方案！",
                               string.Empty);
                            validateContext.AddError(null, ValidationErrorInfo);
                        }
                    }
                    else if (WFNode.ActivityId == AP_NodeID)//申请人评价
                    {
                        //if (dataEntity["FFinEndDt"].ToString().IsNullOrEmptyOrWhiteSpace())
                        //{
                        //    ValidationErrorInfo ValidationErrorInfo = new ValidationErrorInfo(
                        //       string.Empty,
                        //       FID,
                        //       dataEntity.DataEntityIndex,
                        //       dataEntity.RowIndex,
                        //       FID,
                        //       "请填写实际完成日期！",
                        //       string.Empty);
                        //    validateContext.AddError(null, ValidationErrorInfo);
                        //}
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
