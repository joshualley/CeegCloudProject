using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System;

using Kingdee.BOS.Core;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
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

using Kingdee.BOS.Core.DynamicForm;

namespace CZ.CEEG.BdgBos.SalePlan
{
    [Description("年销售计划")]
    [HotUpdate]
    public class CZ_CEEG_BdgBos_SalePlan : AbstractBillPlugIn
    {
        #region override
        public override void AfterBarItemClick(AfterBarItemClickEventArgs e)
        {
            base.AfterBarItemClick(e);
            if(this.Context.ClientType.ToString() != "Mobile")
            {
                switch (e.BarItemKey.ToUpperInvariant())
                {
                    case "TBDOANZBG": //年度销售计划，生成预算
                        Act_GeneJanPlan();
                        break;
                    case "TBDOANZBG1": //年度预算计划，生成预算
                        Act_GeneJanPlan1();
                        break;
                }
            }
        }

        public override void AfterEntryBarItemClick(AfterBarItemClickEventArgs e)
        {
            base.AfterEntryBarItemClick(e);
            switch (e.BarItemKey.ToUpperInvariant())
            {
                case "TBSPLITBDG": //tbSplitBdg 预算拆分
                    Act_GeneAnzEntry();
                    break;
                case "TBSUMMONBDG": //tbSumMonBdg 每月预算汇总至明细
                    Act_SumMonthBudgetPlanEntry();
                    break;
                case "TBGENEROW": //tbGeneRow 生成每月预算表体
                    Act_GeneMonBdgSumEntry();
                    break;
            }
        }

        public override void AfterBindData(EventArgs e)
        {
            base.AfterBindData(e);
            if (this.Context.ClientType.ToString() != "Mobile" && CZ_GetCommonField("DocumentStatus") == "Z")
            {
                this.View.Model.SetValue("FYear", DateTime.Today.Year);
                this.View.Model.SetValue("FBegMon", DateTime.Today.Month);
            }
        }

        public override void BeforeDoOperation(BeforeDoOperationEventArgs e)
        {
            base.BeforeDoOperation(e);
            if (this.Context.ClientType.ToString() != "Mobile")
            {
                switch (e.Operation.FormOperation.Operation.ToUpperInvariant())
                {
                    case "SAVE":
                        if (Act_Check())
                        {
                            e.Cancel = true;
                        }
                        break;
                    case "SUBMIT":
                        if (Act_Check())
                        {
                            e.Cancel = true;
                        }
                        break;
                }
            }
        }
        #endregion

        #region Actions
        /// <summary>
        /// 由预算拆分表，汇总每月预算计划（明细）表体
        /// </summary>
        private void Act_SumMonthBudgetPlanEntry()
        {
            var AnzEntry = this.View.Model.DataObject["FEntityAnz"] as DynamicObjectCollection;
            int _FBegMon = int.Parse(this.View.Model.GetValue("FBegMon").ToString());
            int _FEndMon = int.Parse(this.View.Model.GetValue("FEndMon").ToString());
            float budget = 0;
            int entryCount = 0;
            this.View.Model.DeleteEntryData("FEntity");
            for (int i = _FBegMon; i <= _FEndMon; i++)
            {
                budget = 0;
                foreach(var row in AnzEntry)
                {
                    if (int.Parse(row["FAMonth"].ToString()) == i)
                    {
                        budget += float.Parse(row["FAPrjBudget"].ToString());
                    }
                }
                //为明细表体创建一行
                entryCount = this.View.Model.GetEntryRowCount("FEntity");
                this.View.Model.CreateNewEntryRow("FEntity");
                this.View.Model.SetValue("FMonth", i, entryCount - 1);
                this.View.Model.SetValue("FBudgetMon", budget, entryCount - 1);
            }
            this.View.Model.DeleteEntryRow("FEntity", _FEndMon - _FBegMon + 1);
            this.View.UpdateView("FEntity");
            this.View.ShowMessage("预算汇总完成！");
        }

        /// <summary>
        /// 生成月度预算明细表体
        /// </summary>
        private void Act_GeneMonBdgSumEntry()
        {
            float _FSalePlanYear = float.Parse(this.View.Model.GetValue("FSalePlanYear").ToString());
            int _FBegMon = int.Parse(this.View.Model.GetValue("FBegMon").ToString());
            int _FEndMon = int.Parse(this.View.Model.GetValue("FEndMon").ToString());
            int entryCount = 0;
            this.View.Model.DeleteEntryData("FEntity");
            float avg = _FSalePlanYear / (_FEndMon - _FBegMon + 1);
            for (int i = _FBegMon; i <= _FEndMon; i++)
            {
                entryCount = this.View.Model.GetEntryRowCount("FEntity");
                this.View.Model.CreateNewEntryRow("FEntity");
                this.View.Model.SetValue("FMonth", i, entryCount - 1);
                this.View.Model.SetValue("FSalePlanMon", avg, entryCount - 1);
            }
            this.View.Model.DeleteEntryRow("FEntity", _FEndMon - _FBegMon + 1);
            this.View.UpdateView("FEntity");
        }

        /// <summary>
        /// 生成预算拆分表体
        /// </summary>
        private void Act_GeneAnzEntry()
        {
            string _FYear = CZ_GetCommonField("FYear");
            string _FBraOffice = CZ_GetBaseData("FBraOffice", "Id");
            string _FDocumentStatus_CostRate = "";
            string sql = string.Format("select * from ora_BDG_CostRate where FYear='{0}' and FBraOffice='{1}'", _FYear, _FBraOffice);
            var obj = CZDB_GetData(sql);
            if (obj.Count > 0)
            {
                _FDocumentStatus_CostRate = obj[0]["FDocumentStatus"].ToString();
            }
            if (_FDocumentStatus_CostRate == "C")
            {
                string FID = CZ_GetFID();
                if (FID == "0")
                {
                    this.View.ShowMessage("请先保存单据后再进行尝试！");
                    return;
                }
                sql = "exec proc_czly_GeneSalePlanAnzEntry @FID_SalePlan='" + FID + "'";
                CZDB_GetData(sql);
                this.View.Refresh();
                this.View.ShowMessage("生成完毕，可点击顶部“生成预算”按钮，生成首月预算明细单！");
            }
            else
            {
                this.View.ShowWarnningMessage("请检查此分公司本年度的费用系数表是否通过审核，如若没有，可先提交此单，等待费用系数确定后，再进行操作！");
            }
            
        }

        /// <summary>
        /// 销售计划生成首月份预算明细单
        /// </summary>
        private void Act_GeneJanPlan()
        {

            //验证年销售计划（本单）及其对应的费用系数表是否已经审核
            string _FDocumentStatus = CZ_GetCommonField("DocumentStatus");
            string _FYear = CZ_GetCommonField("FYear");
            string _FBraOffice = CZ_GetBaseData("FBraOffice", "Id");
            string _FDocumentStatus_CostRate = "";
            string sql = string.Format("select * from ora_BDG_CostRate where FYear='{0}' and FBraOffice='{1}'", _FYear, _FBraOffice);
            var obj = CZDB_GetData(sql);
            if(obj.Count > 0)
            {
                _FDocumentStatus_CostRate = obj[0]["FDocumentStatus"].ToString();
            }
            if(_FDocumentStatus == "C" && _FDocumentStatus_CostRate == "C")
            {
                if(CZ_GetCommonField("FIsDoBG") == "False")
                {
                    this.View.ShowMessage("本操作不可逆，确定生成首月预算明细吗？", MessageBoxOptions.YesNo, new Action<MessageBoxResult>((result) =>
                    {
                        if (result == MessageBoxResult.Yes)
                        {
                            //生成预算明细单
                            string _FID = CZ_GetFID();
                            //创建人传入点击按钮的用户
                            string _FCreatorId = this.Context.UserId.ToString();
                            //string _FCreateOrgId = CZ_GetBaseData("FCreateOrgId", "Id");
                            //生成预算及资金
                            sql = string.Format("exec proc_czly_GeneFirstMon_BDG_CPT @FID_SalePlan='{0}', @FCreatorId='{1}'",
                                                 _FID, _FCreatorId);
                            CZDB_GetData(sql);
                        }
                    }));
                    
                }
                else
                {
                    this.View.ShowErrMessage("首月预算明细表已经生成，请勿重新生成！");
                }
            }
            else
            {
                this.View.ShowMessage("请检查此分公司本年度的费用系数表和年度销售计划是否通过审核！");
            }
        }


        /// <summary>
        /// 预算计划生成首月份预算明细单
        /// </summary>
        private void Act_GeneJanPlan1()
        {
            //验证年销售计划（本单）及其对应的费用系数表是否已经审核
            string _FDocumentStatus = CZ_GetCommonField("DocumentStatus");
            string _FYear = CZ_GetCommonField("FYear");
            string _FBraOffice = CZ_GetBaseData("FBraOffice", "Id");
            string sql = "";
            if (_FDocumentStatus == "C")
            {
                if (CZ_GetCommonField("FIsDoBG") == "False")
                {
                    this.View.ShowMessage("本操作不可逆，确定生成首月预算明细吗？", MessageBoxOptions.YesNo, new Action<MessageBoxResult>((result) =>
                    {
                        if (result == MessageBoxResult.Yes)
                        {
                            //生成预算明细单
                            string _FID = CZ_GetFID();
                            //创建人传入点击按钮的用户
                            string _FCreatorId = this.Context.UserId.ToString();
                            //生成预算及资金
                            sql = string.Format("exec proc_czly_GeneFirstMon_BDG_CPT @FID_SalePlan='{0}', @FCreatorId='{1}'",
                                                 _FID, _FCreatorId);
                            CZDB_GetData(sql);
                            this.View.ShowMessage("首月份预算明细单已生成，请");
                        }
                    }));

                }
                else
                {
                    this.View.ShowErrMessage("首月预算明细表已经生成，请勿重新生成！");
                }
            }
            else
            {
                this.View.ShowErrMessage("本单还未通过审核！");
            }
        }

        /// <summary>
        /// 检查本公司、本年计划是否存在
        /// </summary>
        /// <returns></returns>
        private bool Act_Check()
        {
            string _FYear = CZ_GetCommonField("FYear");
            string _FBraOffice = CZ_GetBaseData("FBraOffice", "Id");
            string sql = string.Format("select * from ora_BDG_SalePlan where FYear='{0}' and FBraOffice='{1}' and FDocumentStatus in ('B','C')", 
                                        _FYear, _FBraOffice);
            var obj = CZDB_GetData(sql);
            if(obj.Count > 0)
            {
                this.View.ShowMessage("此公司当前年度的销售计划已经存在!");
                return true;
            }
            return false;
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
