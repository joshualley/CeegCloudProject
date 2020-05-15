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

namespace CZ.CEEG.OAWF.UseCar
{
    [Description("用车申请审批中验证")]
    [HotUpdate]
    public class CZ_CEEG_OAWF_UseCar : AbstractOperationServicePlugIn
    {
        private const int AdminAlloc_1_NodeID = 18; //行政分配 司机 车牌号 出发地点 出发时间
        private const int AdminAlloc_2_NodeID = 506; //行政分配2 实际公里数 实际费用(自动计算) 补贴

        public override void OnPreparePropertys(PreparePropertysEventArgs e)
        {
            e.FieldKeys.Add("F_ora_rcvDiver"); //司机
            e.FieldKeys.Add("F_ora_CarNum"); //车牌
            e.FieldKeys.Add("F_ora_StAddr"); //出发地址
            e.FieldKeys.Add("F_ora_StTime"); //出发时间
            e.FieldKeys.Add("F_ora_Mileage"); //公里数
            e.FieldKeys.Add("F_ora_Subsidy"); //补贴
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

                    var entity = dataEntity["FEntry"] as DynamicObjectCollection;
                    bool IsNull = false;
                    if (WFNode.ActivityId == AdminAlloc_1_NodeID) //司机 车牌号 出发地点 出发时间
                    {
                        foreach(var row in entity)
                        {
                            string F_ora_rcvDiver = row["F_ora_rcvDiver"] == null ? "" : row["F_ora_rcvDiver"].ToString();
                            string F_ora_CarNum = row["F_ora_CarNum"] == null ? "" : row["F_ora_CarNum"].ToString();
                            string F_ora_StAddr = row["F_ora_StAddr"] == null ? "" : row["F_ora_StAddr"].ToString();
                            string F_ora_StTime = row["F_ora_StTime"] == null ? "" : row["F_ora_StTime"].ToString();
                            if(F_ora_rcvDiver == "" || F_ora_CarNum == "" || F_ora_StAddr == "" || F_ora_StTime == "")
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
                                "请分配司机并填写车牌号、出发地点及出发时间！",
                                string.Empty);
                            validateContext.AddError(null, ValidationErrorInfo);
                        }
                    }
                    /*
                    else if (WFNode.ActivityId == AdminAlloc_2_NodeID)//实际公里数 实际费用(自动计算) 补贴
                    {
                        foreach (var row in entity)
                        {
                            float F_ora_Mileage = float.Parse(row["F_ora_Mileage"].ToString());
                            float F_ora_Subsidy = float.Parse(row["F_ora_Subsidy"].ToString());
                            if (F_ora_Mileage <= 0 || F_ora_Subsidy <= 0)
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
                                "请确认实际行车公里数及补贴！",
                                string.Empty);
                            validateContext.AddError(null, ValidationErrorInfo);
                        }
                    }
                    */
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
