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

namespace CZ.CEEG.OAWF.AdminService
{
    [Description("行政服务审批中验证")]
    [HotUpdate]
    public class CZ_CEEG_OAWF_AdminService : AbstractOperationServicePlugIn
    {
        private const int AllocProcessor_NodeID = 308;       //分配执行人节点ID
        private const int ProcessorInputResult_NodeID = 293; //执行人填写执行结果节点ID

        public override void OnPreparePropertys(PreparePropertysEventArgs e)
        {
            e.FieldKeys.Add("FCategory"); //申请类别
            e.FieldKeys.Add("FRemarks"); //处理结果
            e.FieldKeys.Add("FExecutor"); //执行人
            e.FieldKeys.Add("FEntity"); //表体
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
                        var FExecutor = dataEntity["FExecutor"] as DynamicObjectCollection;
                        if (FExecutor.Count <= 0)
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
                        else
                        {
                            
                        }
                    }
                    else if (WFNode.ActivityId == ProcessorInputResult_NodeID)
                    {
                        if (dataEntity["FResult"].ToString().IsNullOrEmptyOrWhiteSpace())
                        {
                            ValidationErrorInfo ValidationErrorInfo = new ValidationErrorInfo(
                               string.Empty,
                               FID,
                               dataEntity.DataEntityIndex,
                               dataEntity.RowIndex,
                               FID,
                               "请填写执行结果！",
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