using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System;

using Kingdee.BOS.Core;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Core.DynamicForm.PlugIn.ControlModel;
using Kingdee.BOS.Core.Metadata;
using Kingdee.BOS.Core.Bill.PlugIn;
using Kingdee.BOS.FileServer.Core;
using Kingdee.BOS.FileServer.Core.Object;
using Kingdee.BOS.FileServer.ProxyService;
using Kingdee.BOS.KDThread;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.ServiceHelper;
using Kingdee.BOS.Util;
using Kingdee.BOS.App.Data;
using Kingdee.BOS.ServiceHelper.ApplicationInitialization;
using Kingdee.BOS.Core.DynamicForm;

namespace CZ.CEEG.BosPM.PersonalReport
{
    [Description("个人汇报单")]
    [HotUpdate]
    public class CZ_CEEG_BosPM_PersonalReport : AbstractBillPlugIn
    {
        #region override
        public override void OnInitialize(InitializeEventArgs e)
        {
            base.OnInitialize(e);
            this.View.GetControl<EntryGrid>("FEntity").SetRowHeight(50);
            this.View.GetControl<EntryGrid>("FEntityL").SetRowHeight(50);
            this.View.GetControl<EntryGrid>("FEntityA").SetRowHeight(50);
            this.View.GetControl<EntryGrid>("FEntityAL").SetRowHeight(50);
        }

        public override void AfterBindData(EventArgs e)
        {
            base.AfterBindData(e);
            
            //var tab = this.View.GetControl<TabControl>("F_ora_Tab");
            //tab.SetFireSelChanged(true);
            if (CZ_GetValue("FDocumentStatus") == "Z")
            {
                SetAudit();
                //this.View.InvokeFormOperation(FormOperationEnum.Save);
                GetLastDailyTask();
                //上月领导交办
                GetAssignTask("1");
            }
            //本月领导交办
            GetAssignTask("0");
        }

        public override void BeforeDoOperation(BeforeDoOperationEventArgs e)
        {
            base.BeforeDoOperation(e);
            switch (e.Operation.FormOperation.Operation.ToUpperInvariant())
            {
                case "SAVE":
                    if (Act_BDO_AlreadySubmit())
                    {
                        e.Cancel = true;
                    }
                    if (!IsValidatePass())
                    {
                        e.Cancel = true;
                    }
                    break;
                case "SUBMIT":
                    if (Act_BDO_AlreadySubmit())
                    {
                        e.Cancel = true;
                    }
                    if (!IsValidatePass())
                    {
                        e.Cancel = true;
                    }
                    break;
                case "AUDIT":
                    CalScore();
                    break;
            }
        }

        public override void AfterDoOperation(AfterDoOperationEventArgs e)
        {
            base.AfterDoOperation(e);
            switch (e.Operation.Operation.ToUpperInvariant())
            {
                case "SAVE":
                    ReWriteLastDailyTask();
                    ReWriteAssignTask(true);
                    //无需反写本月
                    //ReWriteAssignTask(false);
                    break;
                case "SUBMIT":
                    ReWriteLastDailyTask();
                    ReWriteAssignTask(true);
                    //无需反写本月
                    //ReWriteAssignTask(false);
                    break;
                case "AUDIT":
                    ReWriteLastDailyTask();
                    ReWriteAssignTask(true);
                    //无需反写本月
                    //ReWriteAssignTask(false);
                    break;
            }
            
        }

        public override void DataChanged(DataChangedEventArgs e)
        {
            base.DataChanged(e);
            string key = e.Field.Key.ToString();
            switch(key)
            {
                case "FLDirectorGrade": //直接领导评分
                    Act_DC_WarnScoreOverWeight(e);
                    break;
            }
        }

        #endregion

        #region Actions
        /// <summary>
        /// 不允许重复保存、提交
        /// </summary>
        /// <returns></returns>
        private bool Act_BDO_AlreadySubmit()
        {
            var now = DateTime.Now;
            int year = now.Year;
            int month = now.Month;
            string userId = this.Context.UserId.ToString();
            string sql = string.Format(@"select FID from ora_Task_PersonalReport where FCreatorId='{0}' and 
            YEAR(FCreateDate)='{1}' and MONTH(FCreateDate)='{2}' and FDocumentStatus in ('B', 'C')", userId, year, month);
            var res = CZDB_GetData(sql);
            if(res.Count > 0)
            {
                this.View.ShowWarnningMessage("您已经提交过本月的工作计划，无需再次提交！");
                return true;
            }
            return false;
        }

        /// <summary>
        /// 提醒打分超过权重
        /// </summary>
        private void Act_DC_WarnScoreOverWeight(DataChangedEventArgs e)
        {
            float FLWeight = float.Parse(this.View.Model.GetValue("FLWeight", e.Row).ToString());
            float FLDirectorGrade = float.Parse(this.View.Model.GetValue("FLDirectorGrade", e.Row).ToString());
            if(FLDirectorGrade > FLWeight)
            {
                this.View.Model.SetValue("FLDirectorGrade", FLWeight, e.Row);
                this.View.ShowWarnningMessage("评分不能超过权重！");
            }
        }
        #endregion

        #region 业务功能

        /// <summary>
        /// 设置审核人，直接领导岗位、单位总经理
        /// </summary>
        private void SetAudit()
        {
            //string orgId = CZ_GetBaseData("FCreateOrgID", "Id");
            if(this.Context.ClientType.ToString() == "Mobile")
            {
                return;
            }
            string userId = this.Context.UserId.ToString();
            string sql = string.Format("exec proc_czty_GetLoginUser2Emp @FUserID='{0}'", userId);
            var objs = CZDB_GetData(sql);
            if(objs.Count > 0)
            {
                this.View.Model.SetValue("FGManager", objs[0]["FGManager"].ToString());
                this.View.Model.SetValue("FDirectorPost", objs[0]["FSuperiorPost"].ToString());
                this.View.Model.SetValue("FCreateOrgID", objs[0]["FORGID"].ToString());
            }
        }

        /// <summary>
        /// 计算得分
        /// </summary>
        private void CalScore()
        {
            var entityL = this.View.Model.DataObject["FEntityL"] as DynamicObjectCollection;
            var entityAL = this.View.Model.DataObject["FEntityAL"] as DynamicObjectCollection;
            double sumScore = 0;
            if (entityAL.Count > 0 && entityAL[0]["FALSrcID"].ToString() != "0")
            {
                double dailyScore = 0;
                foreach (var row in entityL)
                {
                    //dailyScore += float.Parse(row["FLWeight"].ToString()) * float.Parse(row["FLGManagerGrade"].ToString());
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
                    //sumScore += float.Parse(row["FLWeight"].ToString()) * float.Parse(row["FLGManagerGrade"].ToString()) * 0.01;
                    sumScore += double.Parse(row["FLGManagerGrade"].ToString());
                }
            }
            if(entityL.Count > 0)
            {
                string FLSrcID = entityL[0]["FLSrcID"].ToString();
                if(FLSrcID != "0")
                {
                    //反写上月考核得分
                    string sql = string.Format("update ora_Task_PersonalReport set FScore='{0}' where FID='{1}'", sumScore, FLSrcID);
                    CZDB_GetData(sql);
                }
            }
        }

        /// <summary>
        /// 是否验证通过
        /// </summary>
        /// <returns></returns>
        private bool IsValidatePass()
        {
            if (ValidateWeightSum())
            {
                //return true;
                if (DynamicValidateField())
                {
                    return true;
                }
            }
            
            return false;
        }

        /// <summary>
        /// 验证权重和
        /// </summary>
        /// <returns></returns>
        private bool ValidateWeightSum()
        {
            var entity = this.View.Model.DataObject["FEntity"] as DynamicObjectCollection;
            float SumWeight = 0;
            foreach (var row in entity)
            {
                SumWeight += float.Parse(row["FWeight"].ToString());
            }
            if (SumWeight != 100)
            {
                this.View.ShowErrMessage("本月日常工作权重之和必须为100！");
                return false;
            }
            var entityL = this.View.Model.DataObject["FEntityL"] as DynamicObjectCollection;
            SumWeight = 0;
            int rowNum = 0;
            foreach (var row in entityL)
            {
                if (row["FLSrcID"].ToString() != "0")
                {
                    rowNum++;
                    SumWeight += float.Parse(row["FLWeight"].ToString());
                }
            }
            if (rowNum !=0 && SumWeight != 100)
            {
                this.View.ShowErrMessage("上月日常工作权重之和必须为100！");
                return false;
            }

            return true;
        }

        /// <summary>
        /// 校验上月完成情况、完成结果必录
        /// </summary>
        /// <returns></returns>
        private bool DynamicValidateField()
        {
            //上月日常
            var entityL = this.View.Model.DataObject["FEntityL"] as DynamicObjectCollection;
            if (entityL.Count > 0 && entityL[0]["FLSrcID"].ToString() != "0")
            {
                foreach (var row in entityL)
                {
                    if (row["FLSrcID"].ToString() != "0")
                    {
                        if (GetObjValue(row, "FLPerformance").IsNullOrEmptyOrWhiteSpace())
                        {
                            this.View.ShowMessage("上月日常工作的“完成情况”是必录字段！");
                            return false;
                        }
                        if (GetObjValue(row, "FLResult").IsNullOrEmptyOrWhiteSpace())
                        {
                            this.View.ShowMessage("上月日常工作的“完成结果”是必录字段！");
                            return false;
                        }
                    }
                }
            }
            //上月交办
            var entityAL = this.View.Model.DataObject["FEntityAL"] as DynamicObjectCollection;
            if (entityAL.Count > 0 && entityAL[0]["FALSrcID"].ToString() != "0")
            {
                foreach (var row in entityAL)
                {
                    if (row["FALSrcID"].ToString() != "0")
                    {
                        if (GetObjValue(row, "FALPerformance").IsNullOrEmptyOrWhiteSpace())
                        {
                            this.View.ShowMessage("上月交办工作的“完成情况”是必录字段！");
                            return false;
                        }
                        if (GetObjValue(row, "FALResult").IsNullOrEmptyOrWhiteSpace())
                        {
                            this.View.ShowMessage("上月交办工作的“完成结果”是必录字段！");
                            return false;
                        }
                    }
                }
            }

            return true;
        }


        /// <summary>
        /// 获取上月日常工作数据
        /// </summary>
        private void GetLastDailyTask()
        {
            string FUserId = this.Context.UserId.ToString();
            string sql = String.Format(@"EXEC GetLastMonthDailyTask @FUserId='{0}'", FUserId);
            var objs = CZDB_GetData(sql);
            if (objs.Count > 0)
            {
                this.View.Model.DeleteEntryData("FEntityL");
                this.View.Model.BatchCreateNewEntryRow("FEntityL", objs.Count);
                for (int i = 0; i < objs.Count; i++)
                {
                    //this.View.Model.CreateNewEntryRow("FEntity");
                    //this.View.Model.SetValue("FLSEQ", GetObjValue(objs[i], "FSEQ"), i);
                    this.View.Model.SetValue("FLSrcID", GetObjValue(objs[i], "FSrcID"), i);
                    this.View.Model.SetValue("FLSrcEntryID", GetObjValue(objs[i], "FSrcEntryID"), i);
                    this.View.Model.SetValue("FLEvaContent", GetObjValue(objs[i], "FEvaContent"), i);
                    this.View.Model.SetValue("FLEvaDetail", GetObjValue(objs[i], "FEvaDetail"), i);
                    this.View.Model.SetValue("FLWeight", GetObjValue(objs[i], "FWeight"), i);
                    this.View.Model.SetValue("FLNote", GetObjValue(objs[i], "FNote"), i);
                    this.View.Model.SetValue("FLPerformance", GetObjValue(objs[i], "FPerformance"), i);
                    this.View.Model.SetValue("FLResult", GetObjValue(objs[i], "FResult"), i);
                    this.View.Model.SetValue("FLSelfGrade", GetObjValue(objs[i], "FSelfGrade"), i);
                    this.View.Model.SetValue("FLDirectorGrade", GetObjValue(objs[i], "FDirectorGrade"), i);
                    this.View.Model.SetValue("FLDirectorIdea", GetObjValue(objs[i], "FDirectorIdea"), i);
                    this.View.Model.SetValue("FLGManagerGrade", GetObjValue(objs[i], "FGManagerGrade"), i);
                    this.View.Model.SetValue("FLGManagerIdea", GetObjValue(objs[i], "FGManagerIdea"), i);
                }
                this.View.UpdateView("FEntityL");
            }
        }

        
        /// <summary>
        /// 反写上月日常工作数据
        /// </summary>
        private void ReWriteLastDailyTask()
        {
            var entity = this.View.Model.DataObject["FEntityL"] as DynamicObjectCollection;
            string sql = "";
            foreach(var row in entity)
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
        /// 获取本(上)月领导交办任务
        /// </summary>
        /// <param name="IsLastMonth">0为本月，1为上月</param>
        private void GetAssignTask(string IsLastMonth)
        {
            string FUserId = this.Context.UserId.ToString();
            string sql = String.Format(@"EXEC GetAssignTask @FUserId='{0}',@IsLastMonth='{1}'",
                                        FUserId, IsLastMonth);
            var objs = CZDB_GetData(sql);
            if (objs.Count > 0)
            {
                if (IsLastMonth == "0")
                {
                    this.View.Model.DeleteEntryData("FEntityA");
                    this.View.Model.BatchCreateNewEntryRow("FEntityA", objs.Count);
                    for (int i = 0; i < objs.Count; i++)
                    {
                        //this.View.Model.CreateNewEntryRow("FEntityA");
                        //this.View.Model.SetValue("FASEQ", i, i);
                        this.View.Model.SetValue("FASrcID", GetObjValue(objs[i], "FSrcID"), i);
                        this.View.Model.SetValue("FASrcEntryID", GetObjValue(objs[i], "FSrcEntryID"), i);
                        this.View.Model.SetValue("FATask", GetObjValue(objs[i], "FTask"), i);
                        this.View.Model.SetValue("FATarget", GetObjValue(objs[i], "FTarget"), i);
                        this.View.Model.SetValue("FAPlanDt", GetObjValue(objs[i], "FPlanDt"), i);
                        this.View.Model.SetValue("FAIsResp", GetObjValue(objs[i], "FIsResp"), i);
                        this.View.Model.SetValue("FAWeight", GetObjValue(objs[i], "FWeight"), i);
                        this.View.Model.SetValue("FAPerformance", GetObjValue(objs[i], "FPerformance"), i);
                        this.View.Model.SetValue("FAResult", GetObjValue(objs[i], "FResult"), i);
                        this.View.Model.SetValue("FASelfGrade", GetObjValue(objs[i], "FSelfGrade"), i);
                        this.View.Model.SetValue("FADirectorGrade", GetObjValue(objs[i], "FDirectorGrade"), i);
                        this.View.Model.SetValue("FADirectorIdea", GetObjValue(objs[i], "FDirectorIdea"), i);
                        this.View.Model.SetValue("FAGManagerGrade", GetObjValue(objs[i], "FGManagerGrade"), i);
                        this.View.Model.SetValue("FAGManagerIdea", GetObjValue(objs[i], "FGManagerIdea"), i);
                    }
                    this.View.UpdateView("FEntityA");
                }
                else
                {
                    this.View.Model.DeleteEntryData("FEntityAL");
                    this.View.Model.BatchCreateNewEntryRow("FEntityAL", objs.Count);
                    for (int i = 0; i < objs.Count; i++)
                    {
                        //this.View.Model.CreateNewEntryRow("FEntityAL");
                        //this.View.Model.SetValue("FASEQ", i, i);
                        this.View.Model.SetValue("FALSrcID", GetObjValue(objs[i], "FSrcID"), i);
                        this.View.Model.SetValue("FALSrcEntryID", GetObjValue(objs[i], "FSrcEntryID"), i);
                        this.View.Model.SetValue("FALTask", GetObjValue(objs[i], "FTask"), i);
                        this.View.Model.SetValue("FALTarget", GetObjValue(objs[i], "FTarget"), i);
                        this.View.Model.SetValue("FALPlanDt", GetObjValue(objs[i], "FPlanDt"), i);
                        this.View.Model.SetValue("FALIsResp", GetObjValue(objs[i], "FIsResp"), i);
                        this.View.Model.SetValue("FALWeight", GetObjValue(objs[i], "FWeight"), i);
                        this.View.Model.SetValue("FALPerformance", GetObjValue(objs[i], "FPerformance"), i);
                        this.View.Model.SetValue("FALResult", GetObjValue(objs[i], "FResult"), i);
                        this.View.Model.SetValue("FALSelfGrade", GetObjValue(objs[i], "FSelfGrade"), i);
                        this.View.Model.SetValue("FALDirectorGrade", GetObjValue(objs[i], "FDirectorGrade"), i);
                        this.View.Model.SetValue("FALDirectorIdea", GetObjValue(objs[i], "FDirectorIdea"), i);
                        this.View.Model.SetValue("FALGManagerGrade", GetObjValue(objs[i], "FGManagerGrade"), i);
                        this.View.Model.SetValue("FALGManagerIdea", GetObjValue(objs[i], "FGManagerIdea"), i);
                    }
                    this.View.UpdateView("FEntityAL");
                }
            }
            
        }

        /// <summary>
        /// 反写本(上)月任务派遣单
        /// </summary>
        private void ReWriteAssignTask(bool IsLast)
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
            var entity = this.View.Model.DataObject[_EntrySign] as DynamicObjectCollection;
            string sql = "";
            foreach(var row in entity)
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
                if(sql != "") CZDB_GetData(sql);
            }
        }

        private string GetObjValue(DynamicObject obj, string sign)
        {
            return obj[sign] == null ? "" : obj[sign].ToString();
        }
        #endregion

        #region 基本取数方法
        /// <summary>
        /// 获取当前单据FID
        /// </summary>
        /// <returns></returns>
        public string CZ_GetFID()
        {
            return (this.View.Model.DataObject as DynamicObject)["Id"] == null ? "0" : (this.View.Model.DataObject as DynamicObject)["Id"].ToString();
        }
        public string CZ_GetValue(string sign)
        {
            return this.View.Model.GetValue(sign) == null ? "" : this.View.Model.GetValue(sign).ToString();
        }
        /// <summary>
        /// 获取基础资料
        /// </summary>
        /// <param name="sign">标识</param>
        /// <param name="property">属性</param>
        /// <returns></returns>
        private string CZ_GetBaseData(string sign, string property)
        {
            return this.View.Model.DataObject[sign] == null ? "" : (this.View.Model.DataObject[sign] as DynamicObject)[property].ToString();
        }
        /// <summary>
        /// 获取一般字段
        /// </summary>
        /// <param name="sign">标识</param>
        /// <returns></returns>
        private string CZ_GetCommonField(string sign)
        {
            return this.View.Model.DataObject[sign] == null ? "" : this.View.Model.DataObject[sign].ToString();
        }
        #endregion

        #region 数据库查询
        /// <summary>
        /// 基本方法 
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
