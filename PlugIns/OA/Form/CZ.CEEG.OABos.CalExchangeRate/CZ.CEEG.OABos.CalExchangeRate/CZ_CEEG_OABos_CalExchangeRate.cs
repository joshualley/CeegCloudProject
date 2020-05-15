using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.ComponentModel;

using Kingdee.BOS;
using Kingdee.BOS.Core;
using Kingdee.BOS.Core.DynamicForm;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.Util;
using Kingdee.BOS.App.Data;
using Kingdee.BOS.Core.Bill;
using Kingdee.BOS.Core.Bill.PlugIn;
using Kingdee.BOS.Core.SqlBuilder;
using Kingdee.BOS.Core.Metadata;

namespace CZ.CEEG.OABos.CalExchangeRate
{
    [Description("计算汇率")]
    [HotUpdate]
    public class CZ_CEEG_OABos_CalExchangeRate : AbstractBillPlugIn
    {
        #region 汇率 窗体变量组
        /// <summary>
        /// 单据指定币别 控件标识		=换算前的币别
        /// </summary>
        string Mkt_FCnyID = "";
        /// <summary>
        /// 单据指定本位币 控件标识		=换算成的币别
        /// </summary>
        string Mkt_FCnyIDCN = "";
        /// <summary>
        /// 单据指定 汇率类型 控件标识	[页面无此控件可置空]
        /// </summary>
        string Mkt_FCnyRType = "";
        /// <summary>
        /// 单据指定 汇率 值 控件标识
        /// </summary>
        string Mkt_FRate = "";

        /// <summary>
        /// 设置汇率相关的字段标识
        /// </summary>
        private void SetRateSigns()
        {
            //string formId = this.View.Model.GlobalParameter.FORMID;
            string formId = this.View.GetFormId();
            switch (formId)
            {
                case "k3972241808034802b04c3d18d4107afd": //采购合同
                    Mkt_FCnyID = "FCurrencyID";
                    Mkt_FCnyIDCN = "FSelfCurrency";
                    Mkt_FCnyRType = "FExchangeRateType";
                    Mkt_FRate = "FExchangeRate";
                    break;
                case "kdb6ae742543a4f6da09dfed7ba4e02dd": //销售合同
                    Mkt_FCnyID = "FCurrency";
                    Mkt_FCnyIDCN = "FSelfCurrency";
                    Mkt_FCnyRType = "FExchangeRateType";
                    Mkt_FRate = "FExchangeRate";
                    break;
                case "k191b3057af6c4252bcea813ff644cd3a": //对公资金
                    Mkt_FCnyID = "F_ora_Currency";
                    Mkt_FCnyIDCN = "F_ora_SelfCurrency";
                    Mkt_FCnyRType = "F_ora_RateType";
                    Mkt_FRate = "F_ora_Rate";
                    break;
                default:
                    Mkt_FCnyID = "FCurrencyID";
                    Mkt_FCnyIDCN = "FCurrencyCN";
                    Mkt_FCnyRType = "FRateType";
                    Mkt_FRate = "FRate";
                    break;

            }
        }

        /// <summary>
        /// 对不同单据进行表单汇率字段绑定的值更新计算
        /// </summary>
        private void CalFieldValue(double rate)
        {
            string formId = this.View.Model.GlobalParameter.FORMID;
            switch (formId)
            {
                //赋值暂时不起作用-----
                case "k3972241808034802b04c3d18d4107afd": //采购合同
                    //计算本币税额、本币金额、本币价税合计
                    this.View.Model.SetValue("FSelfTax", double.Parse(this.View.Model.GetValue("FTaxAmount").ToString()) * rate);
                    this.View.Model.SetValue("FTotalAmount", double.Parse(this.View.Model.GetValue("FSelfCnyAnt").ToString()) * rate);
                    this.View.Model.SetValue("FIncludeTaxAmount", double.Parse(this.View.Model.GetValue("FSelfTaxAnt").ToString()) * rate);
                    break;
                case "kdb6ae742543a4f6da09dfed7ba4e02dd": //销售合同
                    //计算本币税额、本币金额、本币价税合计
                    this.View.Model.SetValue("FTotalTaxAmount", double.Parse(this.View.Model.GetValue("FTaxAmount").ToString()) * rate);
                    this.View.Model.SetValue("FTotalAmount", double.Parse(this.View.Model.GetValue("FSelfCnyAnt").ToString()) * rate);
                    this.View.Model.SetValue("FTotal", double.Parse(this.View.Model.GetValue("FSelfTaxAnt").ToString()) * rate);
                    break;
                case "k191b3057af6c4252bcea813ff644cd3a": //对公资金
                    //计算申请税额、本币批准金额、本币实付金额
                    this.View.Model.SetValue("FSelfApplyAnt", double.Parse(this.View.Model.GetValue("FPreCost").ToString()) * rate);
                    this.View.Model.SetValue("FSelfAllowAnt", double.Parse(this.View.Model.GetValue("FRealMoney").ToString()) * rate);
                    this.View.Model.SetValue("FSelfRealAnt", double.Parse(this.View.Model.GetValue("FRealAmt").ToString()) * rate);
                    break;
            }
        }

        #endregion

        /// <summary>
        /// 数据加载完毕
        /// </summary>
        /// <param name="e"></param>
        public override void AfterBindData(EventArgs e)
        {
            if (this.Context.ClientType.ToString() != "Mobile")
            {
                if (this.CZ_GetFormStatus() == "Z")
                {
                    SetRateSigns();
                    //汇率 初始计算 依需要
                    Act_Rate_GetRate(this.CZ_GetValue_DF("FCreateDate", ""));
                }
            }
        }


        /// <summary>
        /// 值更新 依需要开放 币别|原币
        /// </summary>
        /// <param name="e"></param>
        public override void DataChanged(DataChangedEventArgs e)
        {
            if (this.Context.ClientType.ToString() != "Mobile")
            {
                //SetRateSigns();
                string _key = e.Field.Key.ToUpperInvariant();
                if (_key == Mkt_FCnyID.ToUpperInvariant()) //币别
                {
                    Act_Rate_GetRate(this.CZ_GetValue_DF("FCreateDate", ""));
                }
                else if (_key == Mkt_FCnyIDCN.ToUpperInvariant()) //本位币
                {
                    Act_Rate_GetRate(this.CZ_GetValue_DF("FCreateDate", ""));
                }
                base.DataChanged(e);
            }
        }

        /// <summary>
        /// 计算汇率
        /// </summary>
        /// <param name="_date">取汇率日期</param>
        /// <returns>double 汇率</returns>
        private double Act_Rate_GetRate(string _date)
        {
            double _rate = 0;
            string _FCnyID = this.CZ_GetValue_DF(Mkt_FCnyID, "Id", "0");
            string _FCnyCN = this.CZ_GetValue_DF(Mkt_FCnyIDCN, "Id", "0");
            string _FCnyRType = this.CZ_GetValue_DF(Mkt_FCnyRType, "Id", "1");
            if (_FCnyID == "0" || _FCnyCN == "0" || _FCnyRType == "0" || _date == "")
            {
                _rate = 0;
                this.View.Model.SetValue(Mkt_FRate, _rate);
                return _rate;
            }

            // exec proc_cztyBD_GetRate @FRateTypeID=1,@FGetDate='2019-09-17',@FCyForID='7',@FFCyToID='1'
            string _sql = "exec proc_cztyBD_GetRate @FRateTypeID=" + _FCnyRType + ",@FGetDate='" + _date + "',@FCyForID='" + _FCnyID + "',@FFCyToID='" + _FCnyCN + "'";
            DataTable _dt = this.CZDB_SearchBase(_sql);
            if (_dt.Rows.Count == 0)
            {
                _rate = 0;
                this.View.Model.SetValue(Mkt_FRate, _rate);
               
                //启动字段值更新
                this.View.InvokeFieldUpdateService(Mkt_FRate, 0);
                CalFieldValue(_rate);
                return _rate;
            }
            _rate = Double.Parse(_dt.Rows[0]["FRate"].ToString());
            this.View.Model.SetValue(Mkt_FRate, _rate);
            this.View.InvokeFieldUpdateService(Mkt_FRate, 0);
            //CalFieldValue(_rate);

            return _rate;
        }

        //需复制CZ方法组
        #region CZTY Action Base
        /// <summary>
        /// 获取变量值 一般值
        /// </summary>
        /// <param name="_prm">普通控件名称</param>
        /// <returns></returns>
        public string CZ_GetValue(string _prm)
        {
            return this.View.Model.GetValue(_prm) == null ? "" : this.View.Model.GetValue(_prm).ToString();
        }

        /// <summary>
        /// 获取变量值 一般值
        /// </summary>
        /// <param name="_prm">普通控件名称</param>
        /// <param name="_dfVal">Default Val</param>
        /// <returns></returns>
        public string CZ_GetValue_DF(string _prm, string _dfVal)
        {
            string _backVal = this.View.Model.GetValue(_prm) == null ? "" : this.View.Model.GetValue(_prm).ToString();
            return _backVal == "" ? _dfVal : _backVal;
        }

        /// <summary>
        /// 获取变量值 基础资料关联
        /// </summary>
        /// <param name="_obj">基础资料控件名称</param>
        /// <param name="_prm">值序列名称</param>
        /// <returns></returns>
        public string CZ_GetValue(string _obj, string _prm)
        {
            return (this.View.Model.GetValue(_obj) as DynamicObject) == null ? "" : (this.View.Model.GetValue(_obj) as DynamicObject)[_prm].ToString();
        }

        /// <summary>
        /// 获取变量值 基础资料关联
        /// </summary>
        /// <param name="_obj">基础资料控件名称</param>
        /// <param name="_prm">值序列名称</param>
        /// <param name="_dfVal">Default Val</param>
        /// <returns></returns>
        public string CZ_GetValue_DF(string _obj, string _prm, string _dfVal)
        {
            string _backVal = (this.View.Model.GetValue(_obj) as DynamicObject) == null ? "" : (this.View.Model.GetValue(_obj) as DynamicObject)[_prm].ToString();
            return _backVal == "" ? _dfVal : _backVal;
        }

        /// <summary>
        /// 获取变量值 一般值 列表
        /// </summary>
        /// <param name="_prm">普通控件名称</param>
        /// <param name="_rIdx">行 Index</param>
        /// <returns></returns>
        public string CZ_GetRowValue(string _prm, int _rIdx)
        {
            return this.View.Model.GetValue(_prm, _rIdx) == null ? "" : this.View.Model.GetValue(_prm, _rIdx).ToString();
        }

        /// <summary>
        /// 获取变量值 一般值 列表    默认值替代
        /// </summary>
        /// <param name="_prm">普通控件名称</param>
        /// <param name="_rIdx">行 Index</param>
        /// <returns></returns>
        public string CZ_GetRowValue_DF(string _prm, int _rIdx, string _dfVal)
        {
            string _backVal = "";
            _backVal = this.View.Model.GetValue(_prm, _rIdx) == null ? "" : this.View.Model.GetValue(_prm, _rIdx).ToString();
            return _backVal == "" ? _dfVal : _backVal;
        }

        /// <summary>
        /// 获取变量值 基础资料关联 列表
        /// </summary>
        /// <param name="_obj">基础资料控件名称</param>
        /// <param name="_prm">值序列名称</param>
        /// <param name="_rIdx">row index</param>
        /// <returns></returns>
        public string CZ_GetRowValue(string _obj, string _prm, int _rIdx)
        {
            return (this.View.Model.GetValue(_obj, _rIdx) as DynamicObject) == null ? "" : (this.View.Model.GetValue(_obj, _rIdx) as DynamicObject)[_prm].ToString();
        }

        /// <summary>
        /// 获取变量值 基础资料关联 列表 默认值替代
        /// </summary>
        /// <param name="_obj">基础资料控件名称</param>
        /// <param name="_prm">值序列名称</param>
        /// <param name="_rIdx">row index</param>
        /// <param name="_dfVal">默认值替代</param>
        /// <returns></returns>
        public string CZ_GetRowValue_DF(string _obj, string _prm, int _rIdx, string _dfVal)
        {
            string _backVal = "";
            _backVal = (this.View.Model.GetValue(_obj, _rIdx) as DynamicObject) == null ? "" : (this.View.Model.GetValue(_obj, _rIdx) as DynamicObject)[_prm].ToString();
            return _backVal == "" ? _dfVal : _backVal;
        }

        /// <summary>
        /// 获取当前单据ID
        /// </summary>
        /// <returns></returns>
        public string CZ_GetFormID()
        {
            return (this.View.Model.DataObject as DynamicObject)["Id"] == null ? "0" : (this.View.Model.DataObject as DynamicObject)["Id"].ToString();
        }

        /// <summary>
        /// 获取当前单据状态    新增时为 Z  审核：C
        /// </summary>
        /// <returns></returns>
        public string CZ_GetFormStatus()
        {
            return this.View.Model.GetValue("FDocumentStatus").ToString();
        }

        /// <summary>
        /// search 基本方法
        /// </summary>
        /// <param name="_sql"></param>
        /// <returns></returns>
        public DataTable CZDB_SearchBase(string _sql)
        {
            DataTable dt;
            try
            {
                dt = DBUtils.ExecuteDataSet(this.Context, _sql).Tables[0];
                return dt;
            }
            catch (Exception _ex)
            {
                return null;
                throw _ex;
            }
        }
        #endregion
    }
}
