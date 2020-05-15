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

namespace CZ.CEEG.OAWF.InnerEating 
{
    [Description("内部就餐审批中验证")]
    [HotUpdate]
    public class CZ_CEEG_OAWF_InnerEating : AbstractOperationServicePlugIn
    {
        private const int AllocRoom_NodeID = 293;       //分配包厢节点ID
        private const int RealAmount_NodeID = 306;      //确认金额节点ID

        public override void OnPreparePropertys(PreparePropertysEventArgs e)
        {
            e.FieldKeys.Add("FBALCONYNUM2"); //包厢号
            e.FieldKeys.Add("FRealAmount"); //实际金额
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

                    if (WFNode.ActivityId == AllocRoom_NodeID)
                    {
                        var room = dataEntity["FBALCONYNUM2"];
                        if (room == null)
                        {
                            ValidationErrorInfo ValidationErrorInfo = new ValidationErrorInfo(
                                string.Empty,
                                FID,
                                dataEntity.DataEntityIndex,
                                dataEntity.RowIndex,
                                FID,
                                "请分配包厢号！",
                                string.Empty);
                            validateContext.AddError(null, ValidationErrorInfo);
                        }
                    }
                    else if (WFNode.ActivityId == RealAmount_NodeID)
                    {
                        float FRealAmount = float.Parse(dataEntity["FRealAmount"].ToString());
                        if (FRealAmount <= 0)
                        {
                            ValidationErrorInfo ValidationErrorInfo = new ValidationErrorInfo(
                                string.Empty,
                                FID,
                                dataEntity.DataEntityIndex,
                                dataEntity.RowIndex,
                                FID,
                                "请确认实际金额！",
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
