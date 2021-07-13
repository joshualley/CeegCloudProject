using Kingdee.BOS.App.Data;
using Kingdee.BOS.Core.Bill.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.Util;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Text;

namespace CZ.CEEG.BosOa.GetIntercourse
{
    [HotUpdate]
    [Description("获取往来")]
    public class CZ_CEEG_BosOa_GetIntercourse : AbstractBillPlugIn
    {
        public override void DataChanged(DataChangedEventArgs e)
        {
            base.DataChanged(e);
            string key = e.Field.Key.ToString();
            switch (key)
            {
                case "FContractParty":  //往来单位-对公资金
                    PubFundGetIntercourse();
                    break;
                case "FApply":          //申请人-个人资金借支
                    PersonalFundIntercourse();
                    break;
            }
        }


        #region Actions
        /// <summary>
        /// 个人资金获取个人往来
        /// </summary>
        private void PersonalFundIntercourse()
        {
            double _rndAmt = 0;
            string FApply = this.View.Model.GetValue("FApply") == null ? "0" : (this.View.Model.GetValue("FApply") as DynamicObject)["Id"].ToString();
            string _resultMsg = CZ_FB_GetObjBal(CZ_FB_EnFBObject.BD_Empinfo, FApply, ref _rndAmt);
            this.View.Model.SetValue("FIntercourse", -_rndAmt);
            string msg = "";
            if(_rndAmt >= 0)
            {
                msg = "贷方，往来余额：" + Math.Abs(_rndAmt).ToString("f2") + "元。";
            }
            else
            {
                msg = "借方，往来余额：" + Math.Abs(_rndAmt).ToString("f2") + "元。";
            }

            this.View.Model.SetValue("FIntercourseInfo", msg);
        }

        /// <summary>
        /// 对公资金获取往来
        /// </summary>
        private void PubFundGetIntercourse()
        {
            double _rndAmt = 0;
            string FContractPartyType = this.View.Model.GetValue("FContractPartyType") == null ? "" : this.View.Model.GetValue("FContractPartyType").ToString();
            string FContractParty = this.View.Model.GetValue("FContractParty") == null ? "0" : (this.View.Model.GetValue("FContractParty") as DynamicObject)["Id"].ToString();
            if (FContractPartyType == "BD_Supplier")
            {
                string _resultMsg = CZ_FB_GetObjBal(CZ_FB_EnFBObject.BD_Supplier, FContractParty, ref _rndAmt);
            }
            else if (FContractPartyType == "BD_Customer")
            {
                string _resultMsg = CZ_FB_GetObjBal(CZ_FB_EnFBObject.BD_Customer, FContractParty, ref _rndAmt);
            }

            this.View.Model.SetValue("FIntercourse", -_rndAmt);
            string msg = "";
            if (_rndAmt >= 0) //-_rndAmt --> 借+贷-
            {
                msg = "贷方，往来余额：" + Math.Abs(_rndAmt).ToString("f2") + "元。";
            }
            else
            {
                msg = "借方，往来余额：" + Math.Abs(_rndAmt).ToString("f2") + "元。";
            }
            this.View.Model.SetValue("FIntercourseInfo", msg);

        }
        #endregion

        /// <summary>
        /// 调用示例
        /// </summary>
        private void Act_Exp_UseFinBal()
        {
            double _rndAmt = 0;
            string _FObjID = this.CZ_GetValue_DF("FObjID", "0");
            //CZ_FB_GetObjBal(CZ_FB_EnFBObject.BD_Supplier, _FObjID, "", "", ref _rndAmt);

            //预设条件调用 不指定组织，使用预设置的科目条件
            string _resultMsg1 = CZ_FB_GetObjBal(CZ_FB_EnFBObject.BD_Empinfo, _FObjID, ref _rndAmt);
            //自定义调用 指定组织、指定科目ID（或不设置科目条件）调用
            string _resultMsg2 = CZ_FB_GetObjBal(CZ_FB_EnFBObject.BD_Empinfo, _FObjID, "", "", ref _rndAmt);

            this.View.Model.SetValue("FObjAmt", _rndAmt);
        }

        #region FinBal 财务方法 获取指定对象的往来余额[期初往来余额+本期凭证]
        /// <summary>
        /// FinBal_财务方法 获取指定对象 往来余额 [期初往来余额+本期凭证] 简化方法 取全部组织，过滤预设置的科目
        /// </summary>
        /// <param name="_fbo">枚举类型 CZ_FB_EnFBObject 对象</param>
        /// <param name="_FObjID">对象的ID</param>
        /// <param name="_backAmt">往来余额 取得后按实际需求调整显示</param>
        /// <returns></returns>
        private string CZ_FB_GetObjBal(CZ_FB_EnFBObject _fbo, string _FObjID, ref double _backAmt)
        {
            return CZ_FB_GetObjBal(_fbo, _FObjID, "%", "", ref _backAmt);
        }

        /// <summary>
        /// FinBal_财务方法 获取指定对象 往来余额 [期初往来余额+本期凭证]
        /// </summary>
        /// <param name="_fbo">枚举类型 CZ_FB_EnFBObject 对象</param>
        /// <param name="_FObjID">对象的ID</param>
        /// <param name="_FAcctOrgID">指定组织ID 全部组织='%' or ''</param>
        /// <param name="_FAcctID">指定科目ID 全科目='%' ｜ ''=使用预设置的科目条件 ｜ id=指定科目ID</param>
        /// <param name="_backAmt">往来余额 取得后按实际需求调整显示</param>
        /// <returns>方法值行结果备注 用于未取得金额分析</returns>
        private string CZ_FB_GetObjBal(CZ_FB_EnFBObject _fbo, string _FObjID, string _FAcctOrgID, string _FAcctID, ref double _backAmt)
        {
            if (_FObjID == "" || _FObjID == "0")
            {
                _backAmt = 0;
                return "warning:未传入有效的查询对象ID";
            }

            string _FValueSource = "";  //t_bd_FlexItemProperty：FValueSource
            string _ObjMstIDSch = "";   //查询对象表主键FMasterID的语句 
            string _ObjAcctWhile = "";  //对象查询科目定义 如果为空 不拼入查询语句 [每个公司需求不同]
            string _FFlexNumber = "";   //t_bd_FlexItemProperty：FFLexNumber，应用于t_bd_FlexItemDetailV的列名
            CZ_FB_GetParam(_fbo, ref _FValueSource, ref _ObjMstIDSch, ref _ObjAcctWhile);
            _ObjMstIDSch = _ObjMstIDSch.Replace("#objID#", _FObjID);

            #region step 01:获取 t_bd_FlexItemProperty-FFLexNumber
            string _sqlGetFFlex = "select FValueSource,FFLexNumber from t_bd_FlexItemProperty where FValueSource='" + _FValueSource + "'";
            DataTable _dtFFlex = new DataTable();
            try
            {
                _dtFFlex = DBUtils.ExecuteDataSet(this.Context, _sqlGetFFlex).Tables[0];
            }
            catch (Exception _ex)
            {
                _backAmt = 0;
                return "Err:获取FFlexNumber时发生错误 ErrMsg:" + _ex.Message;
                //this.View.ShowErrMessage(_ex.Message + " SQL:" + _sqlGetFFlex, "获取FFlexNumber时发生错误");
            }
            if (_dtFFlex.Rows.Count == 0)
            {
                _backAmt = 0;
                return "warning:t_bd_FlexItemProperty 未设置核算对象";

            }
            _FFlexNumber = _dtFFlex.Rows[0]["FFLexNumber"].ToString();
            #endregion

            #region 原始语句
            /*
            select i.FORGID,ab.FBookID,sy.FValue FCBYear,sp.FValue FCBPeriod,
            convert(datetime,CONVERT(varchar,sy.FValue)+'-'+CONVERT(varchar,sp.FValue)+'-01')FBegDate
            into #aco from T_ORG_Organizations i
            inner join t_bd_AccountBook ab on i.FOrgID=ab.FAccountOrgID 
            inner join T_BAS_SystemProFile sy on ab.FBOOKID=sy.FAccountBookID and sy.FCategory='GL' and sy.FKEY='CurrentYear'
            inner join T_BAS_SystemProFile sp on ab.FBOOKID=sp.FAccountBookID and sp.FCategory='GL' and sp.FKEY='CurrentPeriod';

            select sum(FGOBAmt)FGOBAmt
            from(
            --select o1.FObjMID,aco.FOrgID,aco.FBookID,aco.FCBYear,aco.FCBPeriod,b.FDetailID,b.FAccountID,b.FBeginBalance,a.FNUMBER 
            select b.FBeginBalance FGOBAmt
            from(select FMasterID FObjMID from t_bd_Supplier where FSupplierID=315438)o1 
            inner join t_bd_FlexItemDetailV f on o1.FObjMID=f.FFLEX4 
            inner join #aco aco on 1=1 
            inner join T_GL_BALANCE b on f.FID=b.FDetailID and aco.FBOOKID=b.FACCOUNTBOOKID and aco.FCBYear=b.FYEAR and aco.FCBPeriod=b.FPeriod and b.FCURRENCYID=0 
            inner join T_BD_ACCOUNT a on b.FACCOUNTID=a.FACCTID 
            where aco.FOrgID like('%') and a.FAcctID like('%')
            union all
            --select o1.FObjMID,aco.*,ve.FAccountID,ve.FDC*ve.FAmount FVeAmount 
            select ve.FDC*ve.FAmount FGOBAmt
            from(select FMasterID FObjMID from t_bd_Supplier where FSupplierID=837574)o1  
            inner join t_bd_FlexItemDetailV f on o1.FObjMID=f.FFLEX4 
            inner join #aco aco on 1=1
            inner join T_GL_VOUCHER v on aco.FBookID=v.FAccountBookID and aco.FBegDate<=v.FBusDate and v.FInvalid=0 
            inner join T_GL_VOUCHERENTRY ve on v.FVoucherID=ve.FVoucherID and f.FID=ve.FDetailID 
            inner join T_BD_ACCOUNT a on ve.FAccountID=a.FACCTID
            where aco.FOrgID like('%') and a.FAcctID like('%')
            )t
            */
            #endregion

            #region step 02:获取往来余额
            StringBuilder _sb = new StringBuilder();
            _FAcctOrgID = _FAcctOrgID == "" ? "%" : _FAcctOrgID;
            //_FAcctID = _FAcctID == "" ? "%" : _FAcctID;
            if (_FAcctID == "%")
            {
                _ObjAcctWhile = "";
            }
            else if (_FAcctID != "")
            {
                _ObjAcctWhile = " and a.FAcctID='" + _FAcctID + "' ";
            }

            
            _sb.Append("/*dialect*/ ");
            _sb.Append("select i.FORGID,ab.FBookID,sy.FValue FCBYear,sp.FValue FCBPeriod, ");
            _sb.Append("convert(datetime,CONVERT(varchar,sy.FValue)+'-'+CONVERT(varchar,sp.FValue)+'-01') FBegDate ");
            _sb.Append("into #aco from T_ORG_Organizations i inner join t_bd_AccountBook ab on i.FOrgID=ab.FAccountOrgID ");
            _sb.Append("inner join T_BAS_SystemProFile sy on ab.FBOOKID=sy.FAccountBookID and sy.FCategory='GL' and sy.FKEY='CurrentYear' ");
            _sb.Append("inner join T_BAS_SystemProFile sp on ab.FBOOKID=sp.FAccountBookID and sp.FCategory='GL' and sp.FKEY='CurrentPeriod';");
            _sb.Append("select isnull(sum(FGOBAmt),0)FGOBAmt from(select b.FBeginBalance FGOBAmt from(" + _ObjMstIDSch + ")o1 ");
            _sb.Append("inner join t_bd_FlexItemDetailV f on o1.FObjMID=f." + _FFlexNumber + " inner join #aco aco on 1=1 ");
            _sb.Append("inner join T_GL_BALANCE b on f.FID=b.FDetailID and aco.FBOOKID=b.FACCOUNTBOOKID and aco.FCBYear=b.FYEAR and aco.FCBPeriod=b.FPeriod and b.FCURRENCYID=0 ");
            _sb.Append("inner join T_BD_ACCOUNT a on b.FACCOUNTID=a.FACCTID ");
            _sb.Append("where aco.FOrgID like('" + _FAcctOrgID + "') " + _ObjAcctWhile + " union all ");
            _sb.Append("select ve.FDC*ve.FAmount FGOBAmt from(" + _ObjMstIDSch + ")o1 ");
            _sb.Append("inner join t_bd_FlexItemDetailV f on o1.FObjMID=f." + _FFlexNumber + " inner join #aco aco on 1=1 ");
            _sb.Append("inner join T_GL_VOUCHER v on aco.FBookID=v.FAccountBookID and aco.FBegDate<=v.FBusDate and v.FInvalid=0 ");
            _sb.Append("inner join T_GL_VOUCHERENTRY ve on v.FVoucherID=ve.FVoucherID and f.FID=ve.FDetailID ");
            _sb.Append("inner join T_BD_ACCOUNT a on ve.FAccountID=a.FACCTID ");


            if (!_fbo.ToString().Equals("BD_Empinfo"))
            {
                _sb.Append("where aco.FOrgID not in (156140,156141,293071,293073,1088184,156142,293065) " + _ObjAcctWhile);
            }
            else {
                _sb.Append("where aco.FOrgID like('" + _FAcctOrgID + "') " + _ObjAcctWhile);
            }

            
            //_sb.Append("union all select 888 FGOBAmt ");  //测试用行
            _sb.Append(")t ");
            string _sqlGetFOBAmt = _sb.ToString();

            DataTable _dtFGOBAmt = new DataTable();
            try
            {
                _dtFGOBAmt = DBUtils.ExecuteDataSet(this.Context, _sqlGetFOBAmt).Tables[0];
            }
            catch (Exception _ex)
            {
                _backAmt = 0;
                return "Err:获取往来余额时发生错误 ErrMsg:" + _ex.Message;
                //this.View.ShowErrMessage(_ex.Message + " SQL:" + _sqlGetFOBAmt, "获取往来余额时发生错误");
            }
            if (_dtFGOBAmt.Rows.Count == 0)
            {
                _backAmt = 0;
                return "warning:获取往来余额时,返回行数为0行";
            }

            _backAmt = -double.Parse(_dtFGOBAmt.Rows[0]["FGOBAmt"].ToString());

            //this.View.ShowMessage(_sqlGetFOBAmt);

            return "success";
            #endregion
        }

        /// <summary>
        /// FinBal_财务方法 传入对象 获取拼装SQL的参数
        /// </summary>
        /// <param name="_fbo">枚举 CZ_FB_EnFBObject 传入对象</param>
        /// <param name="_FValueSource">t_bd_FlexItemProperty：FValueSource</param>
        /// <param name="_ObjMstIDSch">查询对象表主键FMasterID的语句</param>
        /// <param name="_ObjAcctWhile">对象查询科目定义 如果为空 不拼入查询语句</param>
        private void CZ_FB_GetParam(CZ_FB_EnFBObject _fbo, ref string _FValueSource, ref string _ObjMstIDSch, ref string _ObjAcctWhile)
        {
            switch (_fbo.ToString())
            {
                case "BD_Customer":
                    _FValueSource = "BD_Customer";
                    _ObjAcctWhile = " and a.FNumber in('1221.03') ";  //客户  取科目1221.03，借+贷-
                    _ObjMstIDSch = "select FMasterID FObjMID from t_BD_Customer where FCUSTID=#objID# ";
                    break;
                case "BD_Department":
                    _FValueSource = "BD_Department";
                    _ObjAcctWhile = "";
                    _ObjMstIDSch = "select FMasterID FObjMID from t_BD_Department where FDeptID=#objID# ";
                    break;
                case "BD_Empinfo":
                    _FValueSource = "BD_Empinfo";
                    _ObjAcctWhile = " and a.FNumber in('1221.01') ";   //员工 取科目1221.01，借+贷-
                    _ObjMstIDSch = "select FMasterID FObjMID from t_Hr_Empinfo where FID=#objID# ";
                    break;
                case "BD_Supplier":
                    _FValueSource = "BD_Supplier";
                    _ObjAcctWhile = " and a.FNumber in('2202.01','2241.01') ";     //供应商 取科目2202.01及2241.01，按余额进行合并后借+贷-
                    _ObjMstIDSch = "select FMasterID FObjMID from t_BD_Supplier where FSupplierID=#objID# ";
                    break;
                case "ORG_Organizations":
                    _FValueSource = "ORG_Organizations";
                    _ObjAcctWhile = "";
                    _ObjMstIDSch = "select FOrgID FObjMID from t_ORG_Organizations where FOrgID=#objID# ";
                    break;
                case "CN_BANKACNT":
                    _FValueSource = "CN_BANKACNT";
                    _ObjAcctWhile = "";
                    _ObjMstIDSch = "select FMasterID FObjMID from t_CN_BANKACNT where FBankAcntID=#objID# ";
                    break;
                default:
                    break;

            }
        }

        /// <summary>
        /// FinBal_财务方法 枚举 核算对象 *配置到不同客户-数据中心时 可调整
        /// </summary>
        private enum CZ_FB_EnFBObject
        {
            /// <summary>
            /// 客户          中电变压器：取科目1221.03，借+贷-
            /// </summary>
            BD_Customer,           //FFLEX6
            /// <summary>
            /// 部门
            /// </summary>
            BD_Department,         //FFLEX5
            /// <summary>
            /// 职员          中电变压器：取科目1221.01，借+贷-
            /// </summary>
            BD_Empinfo,            //FFLEX7
            /// <summary>
            /// 供应商        中电变压器：取科目2202.01及2241.01，按余额进行合并后借+贷-
            /// </summary>
            BD_Supplier,           //FFLEX4
            /// <summary>
            /// 组织
            /// </summary>
            ORG_Organizations,     //FFLEX11
            /// <summary>
            /// 银行
            /// </summary>
            CN_BANKACNT            //FF100002
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
                dt = Kingdee.BOS.App.Data.DBUtils.ExecuteDataSet(this.Context, _sql).Tables[0];
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
