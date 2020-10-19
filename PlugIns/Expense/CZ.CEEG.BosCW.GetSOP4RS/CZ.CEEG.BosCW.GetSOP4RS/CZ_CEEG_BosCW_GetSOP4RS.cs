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

namespace CZ.CEEG.BosCW.GetSOP4RS
{
    /// <summary>
    /// BOS_到款拆分 销售订单收款计划 筛选
    /// </summary>
    [Description("BOS_到款拆分 销售订单收款计划 筛选")]
    [Kingdee.BOS.Util.HotUpdate]
    public class CZ_CEEG_BosCW_GetSOP4RS : AbstractBillPlugIn 
    {
        /// <summary>
        /// 初始传入
        /// </summary>
        string Val_OpenPrm = "";

        /// <summary>
        /// 设置是否更新父单据 Is Return To Parent Window
        /// </summary>
        bool Val_IsReturn2PW = false;

        #region K3 Override
        /// <summary>
        /// 初始化，对其他界面传来的参数进行处理，对控件某些属性进行处理
        /// 这里不宜对数据DataModel进行处理
        /// </summary>
        /// <param name="e"></param>
        public override void OnInitialize(InitializeEventArgs e)
        {
            if (this.View.OpenParameter.GetCustomParameter("InitParam") != null)
            {
                Val_OpenPrm = this.View.OpenParameter.GetCustomParameter("InitParam").ToString();
            }
        }

        /// <summary>
        /// 数据加载完毕
        /// </summary>
        /// <param name="e"></param>
        public override void AfterBindData(EventArgs e)
        {
            this.View.Model.SetValue("FCdnInitStr", Val_OpenPrm);
            Act_ABD_IsIntiOpen(Val_OpenPrm);
            Act_Grid_NoSort("FEntity");
        }

        /// <summary>
        /// 菜单
        /// </summary>
        /// <param name="e"></param>
        public override void AfterBarItemClick(AfterBarItemClickEventArgs e)
        {
            base.AfterBarItemClick(e);

            string _key = e.BarItemKey.ToUpperInvariant();
            switch (_key)
            {
                case "FBTNDOSCH":
                    //FBtnDoSch     查询数据
                    Act_ABIC_FBtnDoSch(e);
                    break;
                case "FBTNBACKT":
                    //FBtnBackT     返回勾选数据
                    Act_AEBIC_FBtnBack(e);
                    break;
                case "TBCLOSE":
                    //tbClose       退出
                    Val_IsReturn2PW = false;
                    this.View.ReturnToParentWindow(null);
                    this.View.Close();
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// 单据体 菜单按钮事件
        /// </summary>
        /// <param name="e"></param>
        public override void AfterEntryBarItemClick(AfterBarItemClickEventArgs e)
        {
            base.AfterEntryBarItemClick(e);

            string _key = e.BarItemKey.ToUpperInvariant();
            switch (_key)
            {
                case "FBTNBACK":
                    //FBtnBack       返回勾选数据
                    //Act_AEBIC_FBtnBack(e);
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// 单据关闭前 判定是否需返回 写入返回值 
        /// </summary>
        /// <param name="e"></param>
        public override void BeforeClosed(BeforeClosedEventArgs e)
        {
            base.BeforeClosed(e);
            //string _FBillID = this.CZ_GetFormID();
            //this.View.ReturnToParentWindow(new Kingdee.BOS.Core.DynamicForm.FormResult(_FBillID));
            if (Val_IsReturn2PW == false)
            {
                this.View.ReturnToParentWindow(null);
            }
        }
        #endregion

        #region Action
        /// <summary>
        /// 判定是报价单传入 格式正确 且为新增时 解析单号
        /// </summary>
        /// <param name="_Val_OpenPrm"></param>
        private void Act_ABD_IsIntiOpen(string _Val_OpenPrm)
        {
            if (_Val_OpenPrm == null || _Val_OpenPrm == "")
            {
                return;
            }
            //Flag=AddNew;FCdnSaleOrg=1;FCdnCust=12345
            Hashtable _htObj = Cz_Rnd_Str2Ht(_Val_OpenPrm, ';', '=');
            string _FCdnSaleOrg = _htObj["FCdnSaleOrg"] == null ? "" : _htObj["FCdnSaleOrg"].ToString();
            string _FCdnCust = _htObj["FCdnCust"] == null ? "" : _htObj["FCdnCust"].ToString();
            string _FCdnBegDt = DateTime.Now.AddYears(-1).ToString("yyyy-MM-dd");
            string _FCdnEndDt = DateTime.Now.ToString("yyyy-MM-dd");

            this.View.Model.SetValue("FCdnSaleOrg", _FCdnSaleOrg);
            this.View.Model.SetValue("FCdnCust", _FCdnCust);
            this.View.Model.SetValue("FCdnBegDt", _FCdnBegDt);
            this.View.Model.SetValue("FCdnEndDt", _FCdnEndDt);
        }

        /// <summary>
        /// 禁止排序
        /// </summary>
        /// <param name="_entityID"></param>
        private void Act_Grid_NoSort(string _entityID)
        {
            EntryGrid grid = this.View.GetControl<EntryGrid>(_entityID);
            grid.SetCustomPropertyValue("AllowSorting", false);
            this.View.UpdateView(_entityID);
        }

        /// <summary>
        /// 菜单按钮事件处理 查询数据
        /// </summary>
        /// <param name="e"></param>
        private void Act_ABIC_FBtnDoSch(AfterBarItemClickEventArgs e)
        {
            //清除行
            int _rowCnt = this.View.Model.GetEntryRowCount("FEntity");
            for (int i = _rowCnt - 1; i >= 0; i--)
            {
                this.View.Model.DeleteEntryRow("FEntity", i);
            }

            //查询
            string _FCdnSaleOrg = this.CZ_GetValue_DF("FCdnSaleOrg", "Id", "0");
            string _FCdnCust = this.CZ_GetValue_DF("FCdnCust", "Id", "0");
            string _FCdnBegDt = this.CZ_GetValue_DF("FCdnBegDt", "0");
            string _FCdnEndDt = this.CZ_GetValue_DF("FCdnEndDt", "0");
            string _FCdnOrderNo = this.CZ_GetValue_DF("FCdnOrderNo", "");
            string _FCdnAmtGT0 = this.CZ_GetValue_DF("FCdnAmtGT0", "True");     //未到款金额>0

            #region 原始SQL语句
            /*
             * ----V1.0 废弃
                select 0 FChk,b.FBillNo,b.FID,bp.FEntryID,bp.FSEQ,bp.FReceiveType,b.FCustID,bf.FSettleCurrId,bf.FEXChangeTypeID,bf.FEXChangeRate,
                bp.FRecAdvanceAmount,bp.FRecAdvanceAmount*bf.FEXChangeRate FReceiptAmountFor,bp.FReMark,isnull(convert(varchar(10),bp.FMustDate,20),'null')FMustDate,
                FRecAdvanceAmount-F_ora_SplitAmount FRemainAmount,bp.FRecAdvanceAmount*bf.FEXChangeRate-F_ora_SplitAmountFor FRemainAmountFor 
                from (select * from T_SAL_ORDER where FDate between '2019-01-01' and '2020-05-01' 
                and FSaleOrgID='156140' and FCUSTID='340766' and FDocumentStatus='C' and FBillNo like('%86%'))b 
                inner join T_SAL_ORDERFIN bf on b.FID=bf.FID inner join T_SAL_ORDERPLAN bp on b.FID=bp.FID 
                --where FRecAdvanceAmount>F_ora_SplitAmount
                order by b.FSaleOrgID,b.FCustID 
             
             * ----V1.1
                //select FID,FBillNO,FCustID into #b from T_SAL_ORDER where FDate between '2019-01-01' and '2020-05-01' 
                //and FSaleOrgID='156140' and FCUSTID='340766' and FDocumentStatus='C' and FBillNo like('%%'); 
             
                --技术处理 绕过部门ID的组织 通过FMASTERID 找对应的部门
                select o.FID,o.FBillNO,o.FCustID,o.FSALEORGID into #b from T_SAL_ORDER o 
                inner join T_BD_CUSTOMER c on o.FCUSTID=c.FCUSTID --and o.FSALEORGID=c.FUseOrgID 
                inner join T_BD_CUSTOMER cm on c.FMASTERID=cm.FMASTERID 
                where o.FDate between '2000-08-17' and '2020-08-17' and o.FSaleOrgID like('175325') and cm.FCUSTID like('494883') and o.FDocumentStatus='C' 
                and FBillNo like('%6155%') 
             
                select 0 FChk,b.FBillNo,b.FID,bp.FEntryID,bp.FSEQ,bp.FReceiveType,b.FCustID,bf.FSettleCurrId,bf.FEXChangeTypeID,bf.FEXChangeRate,
                bp.FRecAdvanceAmount,bp.FRecAdvanceAmount*bf.FEXChangeRate FReceiptAmountFor,bp.FReMark,isnull(convert(varchar(10),bp.FMustDate,20),'null')FMustDate,
                FRecAdvanceAmount-F_ora_SplitAmount FRemainAmount,bp.FRecAdvanceAmount*bf.FEXChangeRate-F_ora_SplitAmountFor FRemainAmountFor, 
                bf.FBillAllAmount,isnull(od.FOutAmt,0)FOutAmt,isnull(convert(varchar(10),od.FOutDate,20),'')FOutDate 
                from #b b 
                inner join T_SAL_ORDERFIN bf on b.FID=bf.FID inner join T_SAL_ORDERPLAN bp on b.FID=bp.FID 
                left join(select b.FID,MAX(o.FDate)FOutDate,SUM(oef.FALLAMOUNT)FOutAmt 
                from #b b 
                inner join T_SAL_ORDERENTRY be on b.FID=be.FID inner join T_SAL_OUTSTOCKENTRY_R oer on be.FENTRYID=oer.FSOEntryId 
                inner join T_SAL_OUTSTOCKENTRY oe on oer.FENTRYID=oe.FEntryID inner join T_SAL_OUTSTOCKENTRY_F oef on oe.FEntryID=oef.FEntryID --and oef.FTAXPRICE>0 
                inner join T_SAL_OUTSTOCK o on oe.FID=o.FID and o.FDOCUMENTSTATUS='C' inner join T_SAL_OUTSTOCKFIN ofn on o.FID=ofn.FID 
                group by b.FID )od on b.FID=od.FID 
                where FRecAdvanceAmount>F_ora_SplitAmount 
                order by b.FBillNO,bp.FSEQ 
                --drop table #b
             */
            #endregion
            StringBuilder _sb = new StringBuilder();
            _sb.Append("/*dialect*/");
            //_sb.Append("select FID,FBillNO,FCustID into #b from T_SAL_ORDER where FDate between '" + _FCdnBegDt + "' and '" + _FCdnEndDt + "' ");
            //_sb.Append("and FSaleOrgID='" + _FCdnSaleOrg + "' and FCUSTID='" + _FCdnCust + "' and FDocumentStatus='C' and FBillNo like('%" + _FCdnOrderNo + "%'); ");

            _sb.Append("select o.FID,o.FBillNO,o.FCustID,o.FSALEORGID into #b from T_SAL_ORDER o ");
            _sb.Append("inner join T_BD_CUSTOMER c on o.FCUSTID=c.FCUSTID ");
            _sb.Append("inner join T_BD_CUSTOMER cm on c.FMASTERID=cm.FMASTERID ");
            _sb.Append("where o.FDate between '" + _FCdnBegDt + "' and '" + _FCdnEndDt + "' and o.FSaleOrgID like('" + _FCdnSaleOrg + "') and cm.FCUSTID like('" + _FCdnCust + "') and o.FDocumentStatus='C' ");
            _sb.Append("and FBillNo like('%" + _FCdnOrderNo + "%'); ");

            _sb.Append("select 0 FChk,b.FBillNo,b.FID,bp.FEntryID,bp.FSEQ,bp.FReceiveType,b.FCustID,bf.FSettleCurrId,bf.FEXChangeTypeID,bf.FEXChangeRate,");
            _sb.Append("bp.FRecAdvanceAmount,bp.FRecAdvanceAmount*bf.FEXChangeRate FReceiptAmountFor,bp.FReMark,isnull(convert(varchar(10),bp.FMustDate,20),'null')FMustDate,");
            _sb.Append("FRecAdvanceAmount-F_ora_SplitAmount FRemainAmount,bp.FRecAdvanceAmount*bf.FEXChangeRate-F_ora_SplitAmountFor FRemainAmountFor, ");
            _sb.Append("bf.FBillAllAmount,isnull(od.FOutAmt,0)FOutAmt,isnull(convert(varchar(10),od.FOutDate,20),'')FOutDate ");
            _sb.Append("from #b b inner join T_SAL_ORDERFIN bf on b.FID=bf.FID inner join T_SAL_ORDERPLAN bp on b.FID=bp.FID ");
            _sb.Append("left join(select b.FID,MAX(o.FDate)FOutDate,SUM(oef.FALLAMOUNT)FOutAmt ");
            _sb.Append("from #b b inner join T_SAL_ORDERENTRY be on b.FID=be.FID inner join T_SAL_OUTSTOCKENTRY_R oer on be.FENTRYID=oer.FSOEntryId ");
            _sb.Append("inner join T_SAL_OUTSTOCKENTRY oe on oer.FENTRYID=oe.FEntryID ");
            _sb.Append("inner join T_SAL_OUTSTOCKENTRY_F oef on oe.FEntryID=oef.FEntryID and oef.FALLAMOUNT>0 ");
            _sb.Append("inner join T_SAL_OUTSTOCK o on oe.FID=o.FID and o.FDOCUMENTSTATUS='C' inner join T_SAL_OUTSTOCKFIN ofn on o.FID=ofn.FID ");
            _sb.Append("group by b.FID )od on b.FID=od.FID ");
            if (_FCdnAmtGT0 == "True")
            {
                _sb.Append("where FRecAdvanceAmount>F_ora_SplitAmount ");
            }
            _sb.Append("order by b.FBillNO,bp.FSEQ");
            string _schSql = _sb.ToString();
            DataTable _objDT = new DataTable();
            try
            {
                _objDT = DBUtils.ExecuteDataSet(this.Context, _schSql).Tables[0];
                if (_objDT.Rows.Count == 0)
                {
                    return;
                }
            }
            catch (Exception _ex)
            {
                this.View.ShowErrMessage(_ex.Message + "\r\n sql:" + _schSql, "获得销售订单-收款计划查询结果时发生错误");
                return;
            }

            //string _FChk = "";            //复选框 False
            string _FOrderNo = "";          //销售订单号
            string _FOrderInterID = "";     //销售订单内码
            string _FOrderPlanID = "";      //收款计划行ID
            string _FOrderPSeq = "";        //收款计划行号
            string _FReceiptName = "";      //收款类型
            string _FCustID = "";           //客户
            string _FSettleCurrId = "";     //结算币别
            string _FEXChangeTypeID = "";   //汇率类型
            string _FEXChangeRate = "";     //汇率
            string _FReceiptAmount = "";    //应收金额
            string _FReceiptAmountFor = ""; //应收金额本位币
            string _FNote = "";             //备注
            string _FReceiptDate = "";      //到款日期
            string _FRemainAmount = "";     //未到款金额
            string _FRemainAmountFor = "";  //未到款金额本位币
            string _FBillAllAmount = "";    //订单价税合计
            string _FOutAmt = "";           //销售出库单 已发货金额合计
            string _FOutDate = "";          //销售出库单 最近发货日期

            //向单据体加载数据
            for (int i = 0; i < _objDT.Rows.Count; i++)
            {
                this.View.Model.CreateNewEntryRow("FEntity");   //新增一行

                _FOrderNo = _objDT.Rows[i]["FBillNo"].ToString();
                _FOrderInterID = _objDT.Rows[i]["FID"].ToString();
                _FOrderPlanID = _objDT.Rows[i]["FEntryID"].ToString();
                _FOrderPSeq = _objDT.Rows[i]["FSEQ"].ToString();
                _FReceiptName = _objDT.Rows[i]["FReceiveType"].ToString();
                _FCustID = _objDT.Rows[i]["FCustID"].ToString();
                _FSettleCurrId = _objDT.Rows[i]["FSettleCurrId"].ToString();
                _FEXChangeTypeID = _objDT.Rows[i]["FEXChangeTypeID"].ToString();
                _FEXChangeRate = _objDT.Rows[i]["FEXChangeRate"].ToString();
                _FReceiptAmount = _objDT.Rows[i]["FRecAdvanceAmount"].ToString();
                _FReceiptAmountFor = _objDT.Rows[i]["FReceiptAmountFor"].ToString();
                _FNote = _objDT.Rows[i]["FReMark"].ToString();
                _FReceiptDate = _objDT.Rows[i]["FMustDate"].ToString();
                _FRemainAmount = _objDT.Rows[i]["FRemainAmount"].ToString();
                _FRemainAmountFor = _objDT.Rows[i]["FRemainAmountFor"].ToString();
                _FBillAllAmount = _objDT.Rows[i]["FBillAllAmount"].ToString();
                _FOutAmt = _objDT.Rows[i]["FOutAmt"].ToString();
                _FOutDate = _objDT.Rows[i]["FOutDate"].ToString();

                this.View.Model.SetValue("FOrderNo", _FOrderNo, i);
                this.View.Model.SetValue("FOrderInterID", _FOrderInterID, i);
                this.View.Model.SetValue("FOrderPlanID", _FOrderPlanID, i);
                this.View.Model.SetValue("FOrderPSeq", _FOrderPSeq, i);
                this.View.Model.SetValue("FReceiptName", _FReceiptName, i);
                this.View.Model.SetValue("FCustID", _FCustID, i);
                this.View.Model.SetValue("FSettleCurrId", _FSettleCurrId, i);
                this.View.Model.SetValue("FEXChangeTypeID", _FEXChangeTypeID, i);
                this.View.Model.SetValue("FEXChangeRate", _FEXChangeRate, i);
                this.View.Model.SetValue("FReceiptAmount", _FReceiptAmount, i);
                this.View.Model.SetValue("FReceiptAmountFor", _FReceiptAmountFor, i);
                this.View.Model.SetValue("FNote", _FNote, i);
                if (_FReceiptDate != "" && _FReceiptDate != "null")
                {
                    this.View.Model.SetValue("FReceiptDate", _FReceiptDate, i);
                }
                this.View.Model.SetValue("FRemainAmount", _FRemainAmount, i);
                this.View.Model.SetValue("FRemainAmountFor", _FRemainAmountFor, i);
                this.View.Model.SetValue("FBillAllAmount", _FBillAllAmount, i);
                this.View.Model.SetValue("FOutAmt", _FOutAmt, i);
                this.View.Model.SetValue("FOutDate", _FOutDate, i);
            }
        }

        /// <summary>
        /// 单据体 菜单按钮事件处理 返回数据
        /// </summary>
        /// <param name="e"></param>
        private void Act_AEBIC_FBtnBack(AfterBarItemClickEventArgs e)
        {
            if (Val_OpenPrm == "")
            {
                return;
            }

            int _rowCnt = this.View.Model.GetEntryRowCount("FEntity");
            string _rowFChk = "";
            int _chkRows = 0;
            string _backStr = "";
            string _rowStr = "";
            
            for (int i = 0; i < _rowCnt; i++)
            {
                _rowFChk = this.CZ_GetRowValue_DF("FChk", i, "False").ToUpperInvariant();
                if (_rowFChk == "FALSE")
                {
                    continue;
                }
                _chkRows++;
                //将行信息加入返回队列
                _rowStr = this.CZ_GetRowValue_DF("FOrderPlanID", i, "0");
                _backStr = _backStr == "" ? _rowStr : _backStr + "," + _rowStr;
            }

            //返回值
            Val_IsReturn2PW = true;
            this.View.ReturnToParentWindow(new Kingdee.BOS.Core.DynamicForm.FormResult(_backStr));
            this.View.Close();
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

        /// <summary>
        /// 将字符串 序列化为 Hashtable
        /// </summary>
        /// <param name="_prm">要序列化的字符串</param>
        /// <param name="_ObjectKey"></param>
        /// <param name="_itemKey"></param>
        /// <returns></returns>
        private Hashtable Cz_Rnd_Str2Ht(string _prm, char _ObjectKey, char _itemKey)
        {
            Hashtable _htObj = new Hashtable();
            ArrayList _alObj = new ArrayList();

            _alObj.AddRange(_prm.Split(_ObjectKey));
            string _item = "";
            foreach (object o in _alObj)
            {
                _item = o.ToString();
                _htObj.Add(_item.Split(_itemKey)[0], _item.Split(_itemKey)[1]);
            }
            return _htObj;
        }
        #endregion
    }
}
