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

namespace CZ.CEEG.OAWF.InfoService
{
    [Description("信息服务审批中验证")]
    [HotUpdate]
    public class CZ_CEEG_OAWF_InfoService : AbstractOperationServicePlugIn
    {
        private const int GM_NodeID = 18; //单位总经理 分配执行人
        private const int AP_NodeID = 478; //申请人评价 满意 不满意

        public override void OnPreparePropertys(PreparePropertysEventArgs e)
        {
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
                    if(WFNode.ActivityId == GM_NodeID) //选择执行人
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
                    }else if (WFNode.ActivityId == AP_NodeID)//申请人评价
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
