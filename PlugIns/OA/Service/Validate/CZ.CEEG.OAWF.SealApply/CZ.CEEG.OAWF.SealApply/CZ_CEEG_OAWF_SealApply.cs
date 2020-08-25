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

namespace CZ.CEEG.OAWF.SealApply
{
    
    [Description("印章使用审批中验证")]
    [HotUpdate]
    public class CZ_CEEG_OAWF_SealApply : AbstractOperationServicePlugIn
    {
        private const int Seal_NodeID = 457;       //股份盖章节点ID

        public override void OnPreparePropertys(PreparePropertysEventArgs e)
        {
            e.FieldKeys.Add("FRealOffTime"); //实际带离
            e.FieldKeys.Add("FRealBackTime"); //实际归还
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

                    if (WFNode.ActivityId == Seal_NodeID)
                    {
                        string realOffTime = dataEntity["FRealOffTime"] == null ? "" : dataEntity["FRealOffTime"].ToString();
                        string realBackTime = dataEntity["FRealBackTime"] == null ? "" : dataEntity["FRealBackTime"].ToString();
                        if (realOffTime.IsNullOrEmptyOrWhiteSpace())
                        {
                            ValidationErrorInfo ValidationErrorInfo = new ValidationErrorInfo(
                            string.Empty,
                            FID,
                            dataEntity.DataEntityIndex,
                            dataEntity.RowIndex,
                            FID,
                            "请填写印章实际带离时间！",
                            string.Empty);
                            validateContext.AddError(null, ValidationErrorInfo);
                        }else if (realBackTime.IsNullOrEmptyOrWhiteSpace())
                        {
                            ValidationErrorInfo ValidationErrorInfo = new ValidationErrorInfo(
                            string.Empty,
                            FID,
                            dataEntity.DataEntityIndex,
                            dataEntity.RowIndex,
                            FID,
                            "请填写印章实际归还时间！",
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
