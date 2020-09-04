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

namespace CZ.CEEG.WFTask.PersonalReport
{
    [Description("个人汇报工作流")]
    [HotUpdate]
    public class CZ_CEEG_WFTask_PersonalReport : AbstractOperationServicePlugIn
    {
        private const int Director_NodeID = 5;  //直接领导节点ID
        private const int GManager_NodeID = 18;  //单位总经理节点ID

        #region override
        public override void OnPreparePropertys(PreparePropertysEventArgs e)
        {
            e.FieldKeys.Add("FID");
            e.FieldKeys.Add("FEntityL");
            e.FieldKeys.Add("FLSrcID");
            e.FieldKeys.Add("FLSrcEntryID");
            e.FieldKeys.Add("FLWeight");
            e.FieldKeys.Add("FLPerformance");
            e.FieldKeys.Add("FLResult");
            e.FieldKeys.Add("FLSelfGrade");
            e.FieldKeys.Add("FLDirectorGrade");
            e.FieldKeys.Add("FLDirectorIdea");
            e.FieldKeys.Add("FLGManagerGrade");
            e.FieldKeys.Add("FLGManagerIdea");

            //e.FieldKeys.Add("FEntityA");
            e.FieldKeys.Add("FEntityAL");
            e.FieldKeys.Add("FALSrcEntryID");
            e.FieldKeys.Add("FALSrcID");
            e.FieldKeys.Add("FALIsResp");
            e.FieldKeys.Add("FALWeight");
            e.FieldKeys.Add("FALPerformance");
            e.FieldKeys.Add("FALSelfGrade");
            e.FieldKeys.Add("FALResult");
            e.FieldKeys.Add("FALDirectorGrade");
            e.FieldKeys.Add("FALDirectorIdea");
            e.FieldKeys.Add("FALGManagerGrade");
            e.FieldKeys.Add("FALGManagerIdea");
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

        public override void EndOperationTransaction(EndOperationTransactionArgs e)
        {
            base.EndOperationTransaction(e);
            
            foreach (var dataEntity in e.DataEntitys)
            {
                string FID = dataEntity["Id"].ToString();
                //string procInstId = WorkflowChartServiceHelper.GetProcInstIdByBillInst(this.Context, CZ_GetFormType(), FID);
                //List<ChartActivityInfo> routeCollection = WorkflowChartServiceHelper.GetProcessRouter(this.Context, procInstId);
                //var WFNode = routeCollection[routeCollection.Count - 1];
                //if (WFNode.ActivityId == Director_NodeID || WFNode.ActivityId == GManager_NodeID)
                //{
                    
                //}
                //if (WFNode.ActivityId == GManager_NodeID)
                //{
                    
                //}
                ReWriteLastDailyTask(dataEntity);
                ReWriteAssignTask(dataEntity, true);
                //ReWriteAssignTask(dataEntity, false); //不反写本月
                CalScore(dataEntity);
            }
        }
        #endregion

        #region 业务方法
        /// <summary>
        /// 计算得分
        /// </summary>
        private void CalScore(DynamicObject dataEntity)
        {
            var entityL = dataEntity["FEntityL"] as DynamicObjectCollection;
            var entityAL = dataEntity["FEntityAL"] as DynamicObjectCollection;
            double sumScore = 0;
            if (entityAL.Count > 0 && entityAL[0]["FALSrcID"].ToString() != "0")
            {
                double dailyScore = 0;
                foreach (var row in entityL)
                {
                    //dailyScore += double.Parse(row["FLWeight"].ToString()) * double.Parse(row["FLGManagerGrade"].ToString());
                    dailyScore += double.Parse(row["FLGManagerGrade"].ToString());
                }
                double dispatchScore = 0;
                foreach (var row in entityAL)
                {
                    dispatchScore += double.Parse(row["FALWeight"].ToString()) * double.Parse(row["FALGManagerGrade"].ToString()) * 0.01;
                }
                sumScore = dailyScore * 0.7 + dispatchScore * 0.3;
            }
            else
            {
                foreach (var row in entityL)
                {
                    //sumScore += double.Parse(row["FLWeight"].ToString()) * double.Parse(row["FLGManagerGrade"].ToString()) * 0.01;
                    sumScore += double.Parse(row["FLGManagerGrade"].ToString());
                }
            }
            //反写上月
            if(entityL.Count > 0)
            {
                string FLSrcID = entityL[0]["FLSrcID"].ToString();
                if (FLSrcID != "0")
                {
                    //反写上月考核得分
                    string sql = string.Format("update ora_Task_PersonalReport set FScore='{0}' where FID='{1}'", sumScore, FLSrcID);
                    CZDB_GetData(sql);
                }
            }
            
        }

        /// <summary>
        /// 反写上月日常工作数据
        /// </summary>
        private void ReWriteLastDailyTask(DynamicObject dataEntity)
        {
            var entity = dataEntity["FEntityL"] as DynamicObjectCollection;
            string sql = "";
            foreach (var row in entity)
            {
                string FLSrcID = row["FLSrcID"] == null ? "" : row["FLSrcID"].ToString();
                string FLSrcEntryID = row["FLSrcEntryID"] == null ? "" : row["FLSrcEntryID"].ToString();
                if (FLSrcID == "0") continue;

                string FLWeight = row["FLWeight"].ToString();
                string FLPerformance = row["FLPerformance"] == null ? "" : row["FLPerformance"].ToString();
                string FLResult = row["FLResult"] == null ? "" : row["FLResult"].ToString();
                string FLSelfGrade = row["FLSelfGrade"].ToString();

                string FLDirectorGrade = row["FLDirectorGrade"].ToString();
                string FLDirectorIdea = row["FLDirectorIdea"] == null ? "" : row["FLDirectorIdea"].ToString();

                string FLGManagerGrade = row["FLGManagerGrade"].ToString();
                string FLGManagerIdea = row["FLGManagerIdea"] == null ? "" : row["FLGManagerIdea"].ToString();

                sql += string.Format(@"update ora_Task_DailyEntry 
                                        set FWeight='{0}',FPerformance='{1}',FResult='{2}',FSelfGrade='{3}',
                                            FDirectorGrade='{4}',FDirectorIdea='{5}',
                                            FGManagerGrade='{6}',FGManagerIdea='{7}'
                                        where FID='{8}' and FEntryID='{9}';
                                      ",
                                        FLWeight, FLPerformance, FLResult, FLSelfGrade,
                                        FLDirectorGrade, FLDirectorIdea, FLGManagerGrade, FLGManagerIdea,
                                        FLSrcID, FLSrcEntryID);
            }
            if (sql != "") CZDB_GetData(sql);

        }

        /// <summary>
        /// 反写本(上)月任务派遣单
        /// </summary>
        private void ReWriteAssignTask(DynamicObject dataEntity, bool IsLast)
        {
            string _EntrySign = IsLast ? "FEntityAL" : "FEntityA";
            string _SrcIDSign = IsLast ? "FALSrcID" : "FASrcID";
            string _SrcEntryIDSign = IsLast ? "FALSrcEntryID" : "FASrcEntryID";
            string _IsRespSign = IsLast ? "FALIsResp" : "FAIsResp";
            string _WeightSign = IsLast ? "FALWeight" : "FAWeight";
            string _PerformanceSign = IsLast ? "FALPerformance" : "FAPerformance";
            string _ResultSign = IsLast ? "FALResult" : "FAResult";
            string _SelfGradeSign = IsLast ? "FALSelfGrade" : "FASelfGrade";
            string _DirectorGradeSign = IsLast ? "FALDirectorGrade" : "FADirectorGrade";
            string _DirectorIdeaSign = IsLast ? "FALDirectorIdea" : "FADirectorIdea";
            string _GManagerGradeSign = IsLast ? "FALGManagerGrade" : "FAGManagerGrade";
            string _GManagerIdeaSign = IsLast ? "FALGManagerIdea" : "FAGManagerIdea";
            var entity = dataEntity[_EntrySign] as DynamicObjectCollection;
            string sql = "";
            foreach (var row in entity)
            {
                string FASrcID = row[_SrcIDSign] == null ? "" : row[_SrcIDSign].ToString();
                string FASrcEntryID = row[_SrcEntryIDSign] == null ? "" : row[_SrcEntryIDSign].ToString();
                string FAIsResp = row[_IsRespSign].ToString();
                if (FASrcID == "0") continue;

                string FAWeight = row[_WeightSign].ToString();
                string FAPerformance = row[_PerformanceSign] == null ? "" : row[_PerformanceSign].ToString();
                string FAResult = row[_ResultSign] == null ? "" : row[_ResultSign].ToString();
                string FASelfGrade = row[_SelfGradeSign].ToString();

                string FADirectorGrade = row[_DirectorGradeSign].ToString();
                string FADirectorIdea = row[_DirectorIdeaSign] == null ? "" : row[_DirectorIdeaSign].ToString();

                string FAGManagerGrade = row[_GManagerGradeSign].ToString();
                string FAGManagerIdea = row[_GManagerIdeaSign] == null ? "" : row[_GManagerIdeaSign].ToString();

                if (FAIsResp == "True")
                {
                    sql += string.Format(@"update ora_Task_Dispatch 
                                        set FWeight='{0}',FPerformance='{1}',FResult='{2}',FSelfGrade='{3}',
                                            FDirectorGrade='{4}',FDirectorIdea='{5}',
                                            FGManagerGrade='{6}',FGManagerIdea='{7}'
                                        where FID='{8}';
                                      ",
                                        FAWeight, FAPerformance, FAResult, FASelfGrade,
                                        FADirectorGrade, FADirectorIdea, FAGManagerGrade, FAGManagerIdea,
                                        FASrcID);
                }
                else if (FAIsResp == "False")
                {
                    sql += string.Format(@"update ora_Task_DispatchEntry 
                                        set FEWeight='{0}',FEPerformance='{1}',FEResult='{2}',FESelfGrade='{3}',
                                            FEDirectorGrade='{4}',FEDirectorIdea='{5}',
                                            FEGManagerGrade='{6}',FEGManagerIdea='{7}'
                                        where FID='{8}' and FEntryID='{9}';
                                      ",
                                        FAWeight, FAPerformance, FAResult, FASelfGrade,
                                        FADirectorGrade, FADirectorIdea, FAGManagerGrade, FAGManagerIdea,
                                        FASrcID, FASrcEntryID);
                }
                if (sql != "") CZDB_GetData(sql);
            }
        }
        
        #endregion

        #region 评分校验器
        /// <summary>
        /// 评分校验器
        /// </summary>
        private class PerValidator : AbstractValidator
        {
            private string formId;

            public PerValidator(string formId)
            {
                this.formId = formId;
            }

            /// <summary>
            /// 验证上月负责任务的权重和等于100
            /// </summary>
            /// <returns></returns>
            private bool ValidateRespWeightSum(Kingdee.BOS.Core.ExtendedDataEntity dataEntity)
            {
                var entity = dataEntity["FEntityAL"] as DynamicObjectCollection;
                if (entity.Count > 0)
                {
                    float SumWeight = 0;
                    int respCount = 0;
                    foreach (var row in entity)
                    {
                        if (row["FALIsResp"].ToString().Equals("True"))
                        {
                            SumWeight += float.Parse(row["FALWeight"].ToString());
                            respCount++;
                        }
                        
                    }
                    if (respCount > 0 && SumWeight != 100)
                    {
                        return false;
                    }
                }
                
                return true;
            }

            /// <summary>
            /// 验证上月日常任务的权重和等于100
            /// </summary>
            /// <returns></returns>
            private bool ValidateDailyWeightSum(Kingdee.BOS.Core.ExtendedDataEntity dataEntity)
            {
                var entityL = dataEntity["FEntityL"] as DynamicObjectCollection;
                if (entityL.Count > 0)
                {
                    float SumWeight = 0;
                    foreach (var row in entityL)
                    {
                        if (row["FLSrcID"].ToString() != "0")
                        {
                            SumWeight += float.Parse(row["FLWeight"].ToString());
                        }
                    }
                    if (SumWeight != 100)
                    {
                        return false;
                    }
                }

                return true;
            }

            //打分校验,不能超过105分
            private bool ValidatePreSum(Kingdee.BOS.Core.ExtendedDataEntity dataEntity)
            {
                var entityL = dataEntity["FEntityL"] as DynamicObjectCollection;
                if (entityL.Count > 0)
                {
                    float SumPre = 0;
                    foreach (var row in entityL)
                    {
                        if (row["FLSrcID"].ToString() != "0")
                        {
                            SumPre += float.Parse(row["FLDirectorGrade"].ToString());
                        }
                    }

                    if (SumPre > 105)
                    {
                        return false;
                    }
                }

                return true;
            }


            public override void Validate(Kingdee.BOS.Core.ExtendedDataEntity[] dataEntities, ValidateContext validateContext, Kingdee.BOS.Context ctx)
            {

                foreach (var dataEntity in dataEntities)
                {
                    string FID = dataEntity["Id"].ToString();

                    if (!ValidateDailyWeightSum(dataEntity))
                    {
                        ValidationErrorInfo ValidationErrorInfo = new ValidationErrorInfo(
                                string.Empty,
                                FID,
                                dataEntity.DataEntityIndex,
                                dataEntity.RowIndex,
                                FID,
                                "汇报人日常工作的权重和必须等于100！",
                                string.Empty);
                        validateContext.AddError(null, ValidationErrorInfo);
                    }
                    /*
                    if (!ValidateRespWeightSum(dataEntity))
                    {
                        ValidationErrorInfo ValidationErrorInfo = new ValidationErrorInfo(
                                string.Empty,
                                FID,
                                dataEntity.DataEntityIndex,
                                dataEntity.RowIndex,
                                FID,
                                "汇报人负责工作的权重和必须等于100！",
                                string.Empty);
                        validateContext.AddError(null, ValidationErrorInfo);
                    }
                    */
                    //获取当前流程节点

                    //string procInstId = WorkflowChartServiceHelper.GetProcInstIdByBillInst(ctx, this.formId, FID);
                    //List<ChartActivityInfo> routeCollection = WorkflowChartServiceHelper.GetProcessRouter(ctx, procInstId);
                    //var WFNode = routeCollection[routeCollection.Count - 1];

                    //if (WFNode.ActivityId == Director_NodeID)
                    //{
                        
                    //}

                    var entityL = dataEntity["FEntityL"] as DynamicObjectCollection;
                    foreach (var row in entityL)
                    {
                        if (row["FLSrcID"].ToString() != "0")
                        {
                            if (row["FLDirectorGrade"].ToString() == "0")//|| row["FLDirectorIdea"].ToString().IsNullOrEmptyOrWhiteSpace())
                            {
                                ValidationErrorInfo ValidationErrorInfo = new ValidationErrorInfo(
                                        string.Empty,
                                        FID,
                                        dataEntity.DataEntityIndex,
                                        dataEntity.RowIndex,
                                        FID,
                                        "请对汇报人上月工作进行评分！",
                                        string.Empty);
                                validateContext.AddError(null, ValidationErrorInfo);
                            }
                        }
                    }

                    if (!ValidatePreSum(dataEntity))
                    {
                        ValidationErrorInfo ValidationErrorInfo = new ValidationErrorInfo(
                                string.Empty,
                                FID,
                                dataEntity.DataEntityIndex,
                                dataEntity.RowIndex,
                                FID,
                                "汇报人上月工作总评分不能超过105！",
                                string.Empty);
                        validateContext.AddError(null, ValidationErrorInfo);
                    }                 
                }
            }
        }

        #endregion

        #region 基本方法
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
