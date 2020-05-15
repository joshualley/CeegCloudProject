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

namespace CZ.CEEG.SrvCrm.SaleOffer
{
    [Description("报价标书流程中控制")]
    [HotUpdate]
    public class CZ_CEEG_SrvCrm_SaleOffer : AbstractOperationServicePlugIn
    {
        /// <summary>
        /// 销售员最终报价
        /// </summary>
        private const int SalemanOffer_NodeID = 18;
        /// <summary>
        /// 标书分配
        /// </summary>
        private const int BidAlloc_NodeID = 523;
        /// <summary>
        /// 报价员节点
        /// </summary>
        private const int OfferBaseprice_NodeID = 121;

        public override void OnPreparePropertys(PreparePropertysEventArgs e)
        {
            e.FieldKeys.Add("FBidMaker"); //标书制作员
            e.FieldKeys.Add("FMaxDnPts"); //行最大下浮
            e.FieldKeys.Add("ora_CRM_SaleOfferBPR"); //表体
            e.FieldKeys.Add("FBMtlGroup"); //大类
            e.FieldKeys.Add("FBMtlItem"); //组成 
            e.FieldKeys.Add("FBRptPrice"); //报价
            e.FieldKeys.Add("FIS2W"); //是否隐藏
            e.FieldKeys.Add("FBGUID"); //E表GU码
            e.FieldKeys.Add("FGUID"); //GU码

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
                    if (WFNode.ActivityId == OfferBaseprice_NodeID)
                    {
                        var entityBPR = dataEntity["ora_CRM_SaleOfferBPR"] as DynamicObjectCollection;
                        var entity = dataEntity["FEntity"] as DynamicObjectCollection;
                        List<string> guids = new List<string>();
                        foreach(var row in entity)
                        {
                            if(row["FIS2W"].ToString() == "0" || row["FIS2W"].ToString() == "")
                            {
                                guids.Add(row["FGUID"].ToString());
                            }
                        }
                        string FBGUID = "";
                        foreach(var row in entityBPR)
                        {
                            FBGUID = row["FBGUID"].ToString();
                            if (guids.Contains(FBGUID))
                            {
                                guids.Remove(FBGUID);
                            }
                        }
                        if(guids.Count > 0)
                        {
                            ValidationErrorInfo ValidationErrorInfo = new ValidationErrorInfo(
                                string.Empty,
                                FID,
                                dataEntity.DataEntityIndex,
                                dataEntity.RowIndex,
                                FID,
                                "您还存在未完成的报价，请报价完成后再尝试提交！",
                                string.Empty);
                            validateContext.AddError(null, ValidationErrorInfo);
                        }
                    }
                    else if (WFNode.ActivityId == SalemanOffer_NodeID)
                    {
                        bool IsNull = false;
                        var entity = dataEntity["ora_CRM_SaleOfferBPR"] as DynamicObjectCollection;
                        //string FBMtlGroup = "";
                        string FBMtlItem = "0";
                        double FBRptPrice = -1;

                        foreach (var row in entity)
                        {
                            FBMtlItem = row["FBMtlItem"] == null ? "0" : (row["FBMtlItem"] as DynamicObject)["Name"].ToString();
                            if(FBMtlItem == "本体")
                            {
                                FBRptPrice = Double.Parse(row["FBRptPrice"].ToString());
                                if(FBRptPrice <= 0)
                                {
                                    IsNull = true;
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
                                "请对产品本体进行报价！",
                                string.Empty);
                            validateContext.AddError(null, ValidationErrorInfo);
                        }
                    }
                    else if (WFNode.ActivityId == BidAlloc_NodeID)
                    {
                        string FBidMaker = dataEntity["FBidMaker"] == null ? "0" : (dataEntity["FBidMaker"] as DynamicObject)["Id"].ToString();
                        if (FBidMaker == "0")
                        {
                            ValidationErrorInfo ValidationErrorInfo = new ValidationErrorInfo(
                               string.Empty,
                               FID,
                               dataEntity.DataEntityIndex,
                               dataEntity.RowIndex,
                               FID,
                               "请分配标书制作员！",
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
