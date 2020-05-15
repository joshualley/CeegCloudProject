using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.ComponentModel;
using System.Data;
using System.Collections;

using Kingdee.BOS.Contracts;
using Kingdee.BOS.Core.Bill.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Core.Metadata;
using Kingdee.BOS.Core.SqlBuilder;
using Kingdee.BOS.Orm.DataEntity;

using Kingdee.BOS.Core.DynamicForm.PlugIn.ControlModel;
using Kingdee.BOS.App.Data;

namespace CZ.CEEG.BosCrmBD.BPScheme
{
    /// <summary>
    /// BOS_CrmBD_本体基价方案
    /// </summary>
    [Description("BOS_CrmBD_本体基价方案")]
    [Kingdee.BOS.Util.HotUpdate]
    public class CZ_CEEG_BosCrmBD_BPScheme : AbstractBillPlugIn 
    {
        #region 汇率 窗体变量组
        /// <summary>
        /// 1、表单上的创建组织 控件标识  
        /// 2、如果以其他组织类控件决定币别 
        /// 3、使用该控件标识 无控件置空""   
        /// </summary>
        string Mkt_FCreateOrg = "FOrgID";
        /// <summary>
        /// 单据指定币别 控件标识
        /// </summary>
        string Mkt_FCnyID = "FCurrencyID";
        /// <summary>
        /// 单据指定本位币 控件标识
        /// </summary>
        string Mkt_FCnyIDCN = "FCurrencyCN";
        /// <summary>
        /// 单据指定 汇率类型 控件标识 [页面无此控件可置空]
        /// </summary>
        string Mkt_FCnyRType = "FRateType";
        /// <summary>
        /// 单据指定 汇率 值 控件标识
        /// </summary>
        string Mkt_FRate = "FRate";
        #endregion

        #region K3 Override
        /// <summary>
        /// 数据加载完毕
        /// </summary>
        /// <param name="e"></param>
        public override void AfterBindData(EventArgs e)
        {
            Act_GeneScheme();
            Act_Rate_GetOrgCny();
        }

        /// <summary>
        /// 值更新事件
        /// </summary>
        /// <param name="e"></param>
        public override void DataChanged(DataChangedEventArgs e)
        {
            string _key = e.Field.Key.ToUpperInvariant();

            switch (_key)
            {
                //多币别 汇率相关属性 变动
                case "FCURRENCYID":
                    //FCurrencyID 币别
                    Act_Rate_GetRate(this.CZ_GetValue_DF("FCreateDate", ""));
                    break;
                case "FRATE":
                    //汇率    FRate
                    Act_DC_FRate(e);
                    break;
                //表单控件
                case "FGPAMTB":
                    //FGpAmtB 
                    Act_DC_BPR_FBGpAmtB(e);
                    Act_DcDo_RndFGpAmt();
                    break;
                case "FCOSTRATE":
                    //FCostRate    基价计算-费用率%
                    Act_DC_BPR_FBCostRate(e);
                    break;
                case "FCOST":
                    //FCost        基价计算-费用
                    Act_DC_BPR_FBCost(e);
                    Act_DcDo_RndFGpAmt();
                    break;
                case "FGPRATE":
                    //FGPRate      基价计算-毛利率%
                    Act_DC_BPR_FBGPRate(e);
                    break;
                case "FGP":
                    //FGP  毛利
                    Act_DC_BPR_FBGP(e);
                    break;
                default:
                    break;
            }
            base.DataChanged(e);
        }

        /// <summary>
        /// 添加行后
        /// </summary>
        /// <param name="e"></param>
        public override void AfterCreateNewEntryRow(CreateNewEntryEventArgs e)
        {
            base.AfterCreateNewEntryRow(e);
            Act_AfterCNER(e);
        }
        #endregion

        #region Actions

        /// <summary>
        /// 生成方案
        /// </summary>
        private void Act_GeneScheme()
        {
            string FSrcID = this.View.OpenParameter.GetCustomParameter("FSrcID") == null ? "" : this.View.OpenParameter.GetCustomParameter("FSrcID").ToString();
            if (FSrcID == "")
            {
                return;
            }
            string sql = "SELECT * FROM ora_CRM_BPRnd b " +
                "INNER JOIN ora_CRM_BPRndEntryB beb ON b.FID=beb.FID " +
                "WHERE b.FID='" + FSrcID + "'";
            var Rows = DBUtils.ExecuteDynamicObject(this.Context, sql);
            for(int i = 0; i < Rows.Count; i++)
            {
                if(i == 0)
                {
                    this.View.Model.SetValue("FMtlGroup", Rows[0]["FMtlGroup"].ToString()); //大类
                    this.View.Model.SetValue("FGpAmt", Rows[0]["FBGpAmt"].ToString());      //基价汇总
                    this.View.Model.SetValue("FGpAmtLc", Rows[0]["FBGpAmtLc"].ToString());      //基价本币
                    this.View.Model.SetValue("FGpAmtB", Rows[0]["FBGpAmtB"].ToString());    //材料汇总
                    this.View.Model.SetValue("FCostRate", Rows[0]["FBCostRate"].ToString()); //费用率
                    this.View.Model.SetValue("FCost", Rows[0]["FBCost"].ToString());   //费用
                    this.View.Model.SetValue("FGPRate", Rows[0]["FBGPRate"].ToString()); //毛利率%
                    this.View.Model.SetValue("FGP", Rows[0]["FBGP"].ToString());     //毛利
                }
                this.View.Model.CreateNewEntryRow("FEntity");
                this.View.Model.SetValue("FClass", Rows[i]["FBClass"].ToString(), i); //材料类别
                this.View.Model.SetValue("FMtl", Rows[i]["FBMtl"].ToString(), i);     //材料
                this.View.Model.SetValue("FModel", Rows[i]["FBModel"].ToString(), i); //规格型号
                this.View.Model.SetValue("FQty", Rows[i]["FBQty"].ToString(), i);     //数量
                this.View.Model.SetValue("FUnit", Rows[i]["FBUnit"].ToString(), i);   //单位
                this.View.Model.SetValue("FPrice", Rows[i]["FBPrice"].ToString(), i); //单价
                this.View.Model.SetValue("FAmt", Rows[i]["FBAmt"].ToString(), i);     //行金额
            }
            this.View.UpdateView("FEntity");
        }

        #region DataChange 事件方法组
        /// <summary>
        /// 组 总价合计
        /// </summary>
        /// <param name="e"></param>
        private void Act_DC_BPR_FBGpAmtB(DataChangedEventArgs e)
        {
            Act_DC_BPR_RndFBCost(e.Row);
        }

        /// <summary>
        /// 基价计算-费用率%
        /// </summary>
        /// <param name="e"></param>
        private void Act_DC_BPR_FBCostRate(DataChangedEventArgs e)
        {
            Act_DC_BPR_RndFBCost(e.Row);
        }

        /// <summary>
        /// 基价计算 计算费用
        /// </summary>
        /// <param name="_row"></param>
        private void Act_DC_BPR_RndFBCost(int _row)
        {
            double _FGpAmtB = Double.Parse(this.CZ_GetRowValue_DF("FGpAmtB", _row, "0"));
            double _FCostRate = Double.Parse(this.CZ_GetRowValue_DF("FCostRate", _row, "0")) / 100;
            //double _FCost = _FGpAmtB * _FCostRate;
            double _FCost = _FGpAmtB / (1 - _FCostRate) - _FGpAmtB;
            this.View.Model.SetValue("FCost", _FCost.ToString(), _row);
        }

        /// <summary>
        /// 基价计算-费用
        /// </summary>
        /// <param name="e"></param>
        private void Act_DC_BPR_FBCost(DataChangedEventArgs e)
        {
            Act_DC_BPR_RndFBGP(e.Row);
        }

        /// <summary>
        /// 基价计算-毛利率%
        /// </summary>
        /// <param name="e"></param>
        private void Act_DC_BPR_FBGPRate(DataChangedEventArgs e)
        {
            Act_DC_BPR_RndFBGP(e.Row);
        }

        /// <summary>
        /// 基价计算-计算毛利
        /// </summary>
        /// <param name="_row"></param>
        private void Act_DC_BPR_RndFBGP(int _row)
        {
            double _FGpAmtB = Double.Parse(this.CZ_GetRowValue_DF("FGpAmtB", _row, "0"));
            double _FCost = Double.Parse(this.CZ_GetRowValue_DF("FCost", _row, "0"));
            double _FGPRate = Double.Parse(this.CZ_GetRowValue_DF("FGPRate", _row, "0")) / 100;

            //double _FBGP = (Double.Parse(_FBGpAmtB) + _FBCost) / (1 - _FBGPRate/ 100);
            //double _FGP = (_FGpAmtB + _FCost) * _FGPRate / (1 - _FGPRate);
            double _FGP = (_FGpAmtB + _FCost) / (1 - _FGPRate) - (_FGpAmtB + _FCost);
            this.View.Model.SetValue("FGP", _FGP.ToString(), _row);
        }

        /// <summary>
        /// 基价计算-毛利
        /// </summary>
        /// <param name="e"></param>
        private void Act_DC_BPR_FBGP(DataChangedEventArgs e)
        {
            //double _FGpAmtB = Double.Parse(this.CZ_GetRowValue_DF("FGpAmtB", e.Row, "0"));
            //double _FCost = Double.Parse(this.CZ_GetRowValue_DF("FCost", e.Row, "0"));
            //double _FGP = Double.Parse(this.CZ_GetRowValue_DF("FGP", e.Row, "0"));
            //double _FGpAmt = _FGpAmtB + _FCost + _FGP;
            //this.View.Model.SetValue("FGpAmt", _FGpAmt.ToString(), e.Row);

            //double _FRate = Double.Parse(this.CZ_GetValue_DF("FRate", "0"));
            //double _FGpAmtLc = _FGpAmt * _FRate;
            //this.View.Model.SetValue("FGpAmtLc", _FGpAmtLc.ToString(), e.Row);
            Act_DcDo_RndFGpAmt();
        }

        /// <summary>
        /// 计算汇总
        /// </summary>
        private void Act_DcDo_RndFGpAmt()
        {
            double _FGpAmtB = Double.Parse(this.CZ_GetValue_DF("FGpAmtB", "0"));
            double _FCost = Double.Parse(this.CZ_GetValue_DF("FCost", "0"));
            double _FGP = Double.Parse(this.CZ_GetValue_DF("FGP",  "0"));
            double _FGPRate = Double.Parse(this.CZ_GetRowValue_DF("FGPRate", 0, "0")) / 100;
            double _FGpAmt = _FGpAmtB + _FCost + _FGP;
            this.View.Model.SetValue("FGpAmt", _FGpAmt.ToString());

            double _FRate = Double.Parse(this.CZ_GetValue_DF("FRate", "0"));
            double _FGpAmtLc = _FGpAmt * _FRate;
            this.View.Model.SetValue("FGpAmtLc", _FGpAmtLc.ToString());
        }
        #endregion

        /// <summary>
        /// 添加行后
        /// </summary>
        /// <param name="e"></param>
        private void Act_AfterCNER(CreateNewEntryEventArgs e)
        {
            string _EntryName = e.Entity.DynamicObjectType.ToString();      //引发事件的表体名 FEntity-明细信息 ｜ FEntityBPR-基价计算表体

            if (e.Row > 0)
            {
                string _curFClass = this.CZ_GetRowValue_DF("FClass", e.Row - 1, "0");
                this.View.Model.SetValue("FClass", _curFClass, e.Row);
                return;
            }
        }
        #endregion

        #region Actions CnyRate
        /// <summary>
        /// 新建单据 获取财务主货币 初始加载 创建组织｜当前组织
        /// 如需要在初始加载后生成本位币，在AfterBindData中调用
        /// </summary>
        private void Act_Rate_GetOrgCny()
        {
            string _FCreateOrgID = this.CZ_GetValue_DF(Mkt_FCreateOrg, "Id", "0");
            string _FCurrentOrgID = this.Context.CurrentOrganizationInfo.ID.ToString();
            string _FDocStatus = this.CZ_GetFormStatus();
            string _FRndOrgID = _FCreateOrgID == "0" ? _FCurrentOrgID : _FCreateOrgID;
            string _FOrgCnyID = this.CZ_GetValue_DF(Mkt_FCnyIDCN, "Id", "0");
            //单据状态=暂存           有组织定义           单据上本位币未设置（一般在BOS中设置默认值）  
            if (_FDocStatus == "Z" && _FRndOrgID != "0" && _FOrgCnyID == "0")
            {
                Act_Rate_GetOrgCny(_FRndOrgID);
            }
        }

        /// <summary>
        /// 获取指定组织的财务主货币 （本位币，默认币别）
        /// 如果窗体上用于控制本位币的控件可自选，需在DataChange中调用
        /// </summary>
        /// <param name="_FOrgID">组织ID</param>
        private void Act_Rate_GetOrgCny(string _FOrgID)
        { 
            //exec proc_czty_GetOrgCurrency @FOrgID='1'
            string _sql = "exec proc_czty_GetOrgCurrency @FOrgID='" + _FOrgID + "'";
            string _FOrgCnyID = "1";
            DataTable _dt = this.CZDB_SearchBase(_sql);
            if (_dt.Rows.Count > 0)
            {
                _FOrgCnyID = _dt.Rows[0]["FCurrencyID"].ToString();
            }

            this.View.Model.SetValue(Mkt_FCnyID, _FOrgCnyID);
            this.View.Model.SetValue(Mkt_FCnyIDCN, _FOrgCnyID);
            this.View.Model.SetValue(Mkt_FRate, "1");
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
                return _rate;
            }
            _rate = Double.Parse(_dt.Rows[0]["FRate"].ToString());
            this.View.Model.SetValue(Mkt_FRate, _rate);

            return _rate;
        }

        /// <summary>
        /// 汇率变动 DataChanged- FRATE
        /// </summary>
        /// <param name="e"></param>
        private void Act_DC_FRate(DataChangedEventArgs e)
        {
            double _FRate = Double.Parse(this.CZ_GetValue_DF("FRate", "0"));

            //表头 
            double _FGpAmt = Double.Parse(this.CZ_GetValue_DF("FGpAmt", "0"));
            double _FGpAmtLc = _FGpAmt * _FRate;
            this.View.Model.SetValue("FGpAmtLc", _FGpAmtLc.ToString());
            
            //明细信息 
        }
        #endregion

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

        /// <summary>
        /// ArrayList：序列化字符串
        /// </summary>
        /// <param name="_objAL">传入ArrayList</param>
        /// <param name="_backStr">返回序列化字符串</param>
        private void CZ_Rnd_AL2Str(ArrayList _objAL, ref string _backStr)
        {
            _backStr = "";
            foreach (object o in _objAL)
            {
                if (_backStr == "")
                {
                    _backStr = o.ToString();
                }
                else
                {
                    _backStr = _backStr + "," + o.ToString();

                }
            }
        }
        #endregion
    }
}
