using System;
using System.Collections.Generic;
using System.ComponentModel;
using Kingdee.BOS.Core;
using Kingdee.BOS.Core.Bill.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.App.Data;
using Kingdee.BOS.Util;

namespace CZ.CEEG.CptBos.CptCtrl
{
    [Description("资金占用控制")]
    [HotUpdate]
    public class CZ_CEEG_CptBos_CptCtrl : AbstractBillPlugIn
    {
        #region override
        public override void DataChanged(DataChangedEventArgs e)
        {
            base.DataChanged(e);
            if (IsUsingBdgSys())
            {
                switch (e.Field.Key.ToString())
                {
                    case "FPayType":
                    case "FPayWay":
                        var trans = Transform();
                        string FBraOffice = (this.Model.GetValue(trans["FBraOffice"]) as DynamicObject)?["Id"].ToString() ?? "0";
                        string FDCptType = (this.Model.GetValue(trans["FDCptType"]) as DynamicObject)?["Id"].ToString() ?? "0";
                        string FDCptTypeName = (this.Model.GetValue(trans["FDCptType"]) as DynamicObject)?["Name"].ToString() ?? "";
                        decimal CptBalance = GetCapitalBalance(FBraOffice, FDCptType);
                        string msg = string.Format("结算方式{0}的资金余额为：{1:f2}元。", FDCptTypeName, CptBalance);
                        this.View.ShowMessage(msg);
                        break;
                }
            }
        }

        public override void BeforeDoOperation(BeforeDoOperationEventArgs e)
        {
            base.BeforeDoOperation(e);
            if (IsUsingBdgSys())
            {
                switch (e.Operation.FormOperation.Operation.ToUpperInvariant())
                {
                    case "SAVE":
                    case "SUBMIT":
                        if (Check())
                        {
                            e.Cancel = true;
                        }
                        break;
                }
            }
            
        }

        #endregion

        #region 业务函数
        /// <summary>
        /// 获取资金余额
        /// </summary>
        /// <param name="orgId">子公司</param>
        /// <param name="cptTypeId">结算方式</param>
        /// <returns></returns>
        private decimal GetCapitalBalance(string orgId, string cptTypeId)
        {
            string year = DateTime.Now.Year.ToString();
            string month = DateTime.Now.Month.ToString();
            string sql = string.Format(@"select FEOccBal from ora_BDG_CapitalMDEntry 
where FEYear='{0}' and FEMonth='{1}' and FEBraOffice='{2}' and FECptType='{3}';",
                year, month, orgId, cptTypeId);

            var r = DBUtils.ExecuteDynamicObject(this.Context, sql);

            return r.Count > 0 ? Convert.ToDecimal(r[0]["FEOccBal"]) : 0;
        }

        private bool Check()
        {
            var trans = Transform();
            string FBraOffice = (this.Model.GetValue(trans["FBraOffice"]) as DynamicObject)?["Id"].ToString() ?? "0";
            string FDCptType = (this.Model.GetValue(trans["FDCptType"]) as DynamicObject)?["Id"].ToString() ?? "0";
            string FDCptTypeName = (this.Model.GetValue(trans["FDCptType"]) as DynamicObject)?["Name"].ToString() ?? "";
            decimal CptBalance = GetCapitalBalance(FBraOffice, FDCptType);
            decimal PayTotalMount = Convert.ToDecimal(this.Model.GetValue(trans["FPreCost"])); //申请金额
            decimal RealPayMount = Convert.ToDecimal(this.Model.GetValue(trans["FReCost"]));   //实付金额

            if(CptBalance < PayTotalMount)
            {
                string msg = $"结算方式{FDCptTypeName}的资金余额不足！\n目前资金余额为：{CptBalance.ToString("0.00")}元。";
                this.View.ShowErrMessage(msg);
                return true;
            }
            
            return false;
        }
        
        /// <summary>
        /// 获取不同单据上字段的映射
        /// </summary>
        /// <returns></returns>
        private Dictionary<string, string> Transform()
        {
            string formId = this.View.GetFormId();
            var dict = new Dictionary<string, string>();
            switch (formId)
            {
                case "k0c6b452fa8154c4f8e8e5f55f96bcfac": //个人资金借支
                    dict.Add("FBraOffice", "FPayOrgId"); // 付款公司名称
                    dict.Add("FDSrcEntryID", "FEntryID");
                    dict.Add("FDSrcSEQ", "FSeq");
                    dict.Add("FDCostPrj", "FCostType");
                    dict.Add("FDCptType", "FPayType");
                    dict.Add("FPreCost", "FAmount");
                    dict.Add("FReCost", "FStatusAmount");
                    break;
                case "k191b3057af6c4252bcea813ff644cd3a": //对公资金申请
                    dict.Add("FBraOffice", "FPayOrgId"); // 付款公司名称
                    dict.Add("FDSrcEntryID", "FEntryID");
                    dict.Add("FDSrcSEQ", "FSeq");
                    dict.Add("FDCostPrj", "FCostItem");
                    dict.Add("FDCptType", "FPayWay");
                    dict.Add("FPreCost", "FPreCost");
                    dict.Add("FReCost", "FRealMoney");
                    break;
            }
            return dict;
        }

        /// <summary>
        /// 是否使用预算系统
        /// </summary>
        /// <returns></returns>
        private bool IsUsingBdgSys()
        {
            string sql = "EXEC proc_cz_ly_IsUsingBdgSys";
            return DBUtils.ExecuteDynamicObject(this.Context, sql)[0]["FSwitch"].ToString() == "1";
        }

        #endregion
    }
}
