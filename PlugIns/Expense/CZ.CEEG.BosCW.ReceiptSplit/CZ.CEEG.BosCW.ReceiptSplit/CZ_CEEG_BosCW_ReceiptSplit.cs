using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.ComponentModel;
using System.Data;
using System.Collections;

//using Kingdee.BOS.Contracts;
using Kingdee.BOS.App.Data;
using Kingdee.BOS.Core.Bill;
using Kingdee.BOS.Core.Bill.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Core.Metadata;
using Kingdee.BOS.Core.SqlBuilder;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.ServiceHelper;
using Kingdee.BOS.Core.DynamicForm;

namespace CZ.CEEG.BosCW.ReceiptSplit
{
    /// <summary>
    /// BOS_到款拆分单
    /// </summary>
    [Description("BOS_到款拆分单")]
    [Kingdee.BOS.Util.HotUpdate]
    public class CZ_CEEG_BosCW_ReceiptSplit : AbstractBillPlugIn
    {
        #region 其他窗体变量 及 预置值
        /// <summary>
        /// 表体  记账明细     =FEntity
        /// </summary>
        string Str_EntryKey_Main = "FEntity";
        /// <summary>
        /// 表体  订单收款计划
        /// </summary>
        string Str_EntryKey_OP = "FEntityOP";
        #endregion

        #region K3 Override
        /// <summary>
        /// 数据加载完毕
        /// </summary>
        /// <param name="e"></param>
        public override void AfterBindData(EventArgs e)
        {
            //加载初始模拟值 记帐单数据
            //Act_ABD_SetTestData();
            //分情况处理表单控件样式
            //Act_ABD_SetOpenStyle();
        }

        /// <summary>
        /// 值更新 物料组-附件-分组号[项次]   ｜  基本单价-数量-报价 ： 基价合计 ： 总基价-总报价-【扣款】
        /// </summary>
        /// <param name="e"></param>
        public override void DataChanged(DataChangedEventArgs e)
        {
            string _key = e.Field.Key.ToUpperInvariant();
            switch (_key)
            {
                case "FSPLITFLAG":
                    //FSplitFlag 拆分类型
                    Act_DC_FSplitFlag(e);
                    break;
                case "":
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
                case "ORABTNCHSOP":
                    //oraBtnChsOP 订单收款计划
                    Act_AfterEBIC_oraBtnChsOP(e);
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// 单据持有事件发生前需要完成的功能
        /// </summary>
        /// <param name="e"></param>
        public override void BeforeDoOperation(BeforeDoOperationEventArgs e)
        {
            string _opKey = e.Operation.FormOperation.Operation.ToUpperInvariant();
            switch (_opKey)
            {
                //case "SAVE": 表单定义的事件都可以在这里执行，需要通过事件的代码[大写]区分不同事件
                //break;
                case "SAVE":
                    Act_BDO_BeforeSave(e);
                    break;
                case "AUDIT":
                    //Act_BDO_BeforeAudit(e);
                    break;
                default:
                    break;
            }
            base.BeforeDoOperation(e);
        }

        /// <summary>
        /// 单据持有事件发生后需要完成的功能
        /// </summary>
        /// <param name="e"></param>
        public override void AfterDoOperation(AfterDoOperationEventArgs e)
        {
            string _opKey = e.Operation.Operation.ToUpperInvariant();
            switch (_opKey)
            {
                //case "SAVE": 表单定义的事件都可以在这里执行，需要通过事件的代码[大写]区分不同事件
                //break;
                case "AUDIT":    //操作代码  Audit
                    Act_ABO_AfterAudit(e);
                    break;
                case "UNAUDIT":    //操作代码  UnAudit
                    Act_ABO_AfterAudit(e);
                    break;
                default:
                    break;
            }
            base.AfterDoOperation(e);
        }
        #endregion

        #region Action
        /// <summary>
        /// 设置开发测试用模拟数据
        /// </summary>
        private void Act_ABD_SetTestData()
        {
            string _FReceiptEID_0 = this.CZ_GetRowValue_DF("FReceiptEID", 0, "0");
            if (_FReceiptEID_0 != "0")
            {
                return;
            }

            string _FSourceBillType = "CN_JOURNAL";     //源单类型
            string _FSourceBillNo = "XXX0001";          //FSourceBillNo
            string _FSourceBillSeq = "1";               //日记账行号
            string _FReceiptDate = "2020-04-01";        //记账日期
            string _FReCustID = "542939";               //到款客户
            string _FReOrgID = "156140";                //销售组织
            string _FReSettleCurrId = "1";              //结算币别
            string _FReEXChangeTypeID = "1";            //汇率类型
            string _FReEXChangeRate = "1";              //汇率
            string _FReAmount = "200000.00";             //到款金额
            string _FReAmountFor = "200000";             //到款金额本位币
            string _FReSplitAmount = "180000";           //未拆分金额
            string _FReSplitAmountFor = "180000";        //未拆分金额本位币
            string _FReceiptEID = "100006";             //记账明细ID

            this.View.Model.SetValue("FSourceBillType", _FSourceBillType, 0);
            this.View.Model.SetValue("FSourceBillNo", _FSourceBillNo, 0);
            this.View.Model.SetValue("FSourceBillSeq", _FSourceBillSeq, 0);
            this.View.Model.SetValue("FReceiptDate", _FReceiptDate, 0);
            this.View.Model.SetValue("FReCustID", _FReCustID, 0);
            this.View.Model.SetValue("FReOrgID", _FReOrgID, 0);
            this.View.Model.SetValue("FReSettleCurrId", _FReSettleCurrId, 0);
            this.View.Model.SetValue("FReEXChangeTypeID", _FReEXChangeTypeID, 0);
            this.View.Model.SetValue("FReEXChangeRate", _FReEXChangeRate, 0);
            this.View.Model.SetValue("FReAmount", _FReAmount, 0);
            this.View.Model.SetValue("FReAmountFor", _FReAmountFor, 0);
            this.View.Model.SetValue("FReSplitAmount", _FReSplitAmount, 0);
            this.View.Model.SetValue("FReSplitAmountFor", _FReSplitAmountFor, 0);
            this.View.Model.SetValue("FReceiptEID", _FReceiptEID, 0);
        }

        /// <summary>
        /// 数据加载后 设置表单样式
        /// </summary>
        private void Act_ABD_SetOpenStyle()
        {
            string _FSplitFlag = this.CZ_GetValue_DF("FSplitFlag", "0");
            if (_FSplitFlag == "1")
            {
                //锁定   FSplitAmt 当选项=转货款时 
                this.View.GetControl("FSplitAmt").Enabled = false;
                this.View.GetControl("F_ora_Tab1_P0").Visible = true;

            }
            else
            {
                //反锁定 FSplitAmt 当选项为其他时
                this.View.GetControl("FSplitAmt").Enabled = true;
                this.View.GetControl("F_ora_Tab1_P0").Visible = false;
            }
        }

        /// <summary>
        /// 值更新处理 FSplitFlag 拆分类型
        /// </summary>
        /// <param name="e"></param>
        private void Act_DC_FSplitFlag(DataChangedEventArgs e)
        {
            string _FSplitFlag = e.NewValue == null ? "0" : e.NewValue.ToString();
            if (_FSplitFlag == "1")
            {
                //锁定   FSplitAmt 当选项=转货款时 
                this.View.GetControl("FSplitAmt").Enabled = false;
                this.View.GetControl("F_ora_Tab1_P0").Visible = true;
                this.View.Model.SetValue("FSplitAmt", 0);

            }
            else
            {
                //反锁定 FSplitAmt 当选项为其他时
                this.View.GetControl("FSplitAmt").Enabled = true;
                this.CZ_EntityClear(Str_EntryKey_OP, false);
                this.View.GetControl("F_ora_Tab1_P0").Visible = false;
            }
        }

        /// <summary>
        /// 操作事件前 保存 执行前 表单数据验证
        /// </summary>
        /// <param name="e"></param>
        private void Act_BDO_BeforeSave(BeforeDoOperationEventArgs e)
        {
            //bool _valIsErr = false;        //true=存在错误;false=校验无误
            //收款计划表体行 验证
            decimal _FSplitAmt = decimal.Parse(this.CZ_GetValue_DF("FSplitAmt", "0"));                    //基本信息:本次拆分金额
            decimal _FReAmount = decimal.Parse(this.CZ_GetRowValue_DF("FReAmount", 0, "0"));              //记账明细:到款金额
            decimal _FReSplitAmount = decimal.Parse(this.CZ_GetRowValue_DF("FReSplitAmount", 0, "0"));    //记账明细:未拆分金额
            decimal _HisReAmount = -(_FReAmount - _FReSplitAmount);                                      //计算:历史拆分金额*-1
            if (_FSplitAmt > _FReSplitAmount || _FSplitAmt < _HisReAmount)
            {
                this.View.ShowErrMessage("可拆分区间 " + _HisReAmount.ToString() + " —— " + _FReSplitAmount.ToString(),
                    "错误：基本信息: 本次拆分金额 超出限制,不能保存");
                e.Cancel = true;
            }

            //单据金额 验证
            string _FSplitFlag = this.CZ_GetValue_DF("FSplitFlag", "0");
            if (_FSplitFlag != "1")
            {
                //仅转货款时有后续动作
                return;
            }
            int _maxCnt = this.View.Model.GetEntryRowCount(Str_EntryKey_OP);
            decimal _FSplitAmount = 0;           //收款计划-本次拆分金额
            decimal _FReceiptAmount = 0;         //收款计划-应收金额
            decimal _FRemainAmount = 0;          //收款计划-未到款金额
            decimal _HisSptAmount = 0;           //收款计划-历史拆入金额*-1

            string msg = "";
            for (int i = 0; i < _maxCnt; i++)
            {
                _FSplitAmount = decimal.Parse(this.CZ_GetRowValue_DF("FSplitAmount", i, "0"));
                _FReceiptAmount = decimal.Parse(this.CZ_GetRowValue_DF("FReceiptAmount", i, "0"));
                _FRemainAmount = decimal.Parse(this.CZ_GetRowValue_DF("FRemainAmount", i, "0"));
                _HisSptAmount = -(_FReceiptAmount - _FRemainAmount);
                
                //msg += string.Format("本次拆分金额: {0}, 应收金额: {1}, 未到款金额: {2}, 历史拆入金额: {3}。{4}, {5}\n",
                //_FSplitAmount, _FReceiptAmount, _FRemainAmount, _HisSptAmount,
                //_FSplitAmount - _FRemainAmount, _FSplitAmount - _HisSptAmount);
               
                if (_FSplitAmount > _FRemainAmount || _FSplitAmount < _HisSptAmount)
                {
                    this.View.ShowErrMessage("可拆分区间 " + _HisSptAmount.ToString() + " —— " + _FRemainAmount.ToString(),
                    "错误：收款计划第 " + (i + 1).ToString() + " 行: 本次拆分金额 超出限制,不能保存");
                    e.Cancel = true;
                    break;
                }
            }
            //this.View.ShowMessage(msg);
        }

        /// <summary>
        /// 操作事件前 审核 执行前 提示 验证
        /// </summary>
        private void Act_BDO_BeforeAudit(BeforeDoOperationEventArgs e)
        {
            //this.View.ShowMessage("如果需要调整分配的金额，请在新的拆分单中调整（正负数）", MessageBoxOptions.YesNo, (res) =>
            //{
            //    if (res == MessageBoxResult.Yes)
            //    {
            //        //View.ShowMessage("你选了是！");
            //    }
            //    if (res == MessageBoxResult.No)
            //    {
            //        //e.Cancel = true;
            //        //View.ShowMessage("你选了否！");
            //    }
            //}, "警告:到款拆分单核后不可反审核,是否确认继续执行审核");
        }

        /// <summary>
        /// 操作事件后 审核 执行后
        /// </summary>
        /// <param name="e"></param>
        private void Act_ABO_AfterAudit(AfterDoOperationEventArgs e)
        {
            string _FSplitFlag = this.CZ_GetValue_DF("FSplitFlag", "0");
            //if (_FSplitFlag != "1")
            //{
            //    //仅转货款时有后续动作
            //    return;
            //}
            
            #region 原始SQL语句 2020-04-23 此SQL语句已校验
            /*
            ----复写手工记帐单 已收款金额\r\n       Spilt｜Split
            --select d.FReceiptEID,je.F_Ora_SpiltAmount,isnull(d.FSplitAmt,0),je.F_Ora_SpiltAmountFor,j.FExchangeRate*isnull(d.FSplitAmt,0) 
            update je set je.F_Ora_SpiltAmount=isnull(d.FSplitAmt,0),je.F_Ora_SpiltAmountFor=j.FExchangeRate*isnull(d.FSplitAmt,0) 
            from(select FReceiptEID from T_CZ_ReceiptSplitDetail where FID=100005)z 
            inner join T_CN_JOURNALENTRY je on z.FReceiptEID=je.FEntryID inner join T_CN_JOURNAL j on je.FID=j.FID 
            left join(select z.FReceiptEID,sum(r.FSplitAmt)FSplitAmt 
	            from(select FReceiptEID from T_CZ_ReceiptSplitDetail where FID=100005)z 
	            inner join T_CZ_ReceiptSplitDetail rd on z.FReceiptEID=rd.FReceiptEID 
	            inner join T_CZ_ReceiptSplit r on rd.FID=r.FID and r.FDocumentStatus='C' 
	            group by z.FReceiptEID )d on z.FReceiptEID=d.FReceiptEID ;

            ----复写销售订单收款计划 已收款金额 \r\n
            --select op.FEntryID,op.F_Ora_SplitAmount,isnull(d.FSplitAmount,0),F_Ora_SplitAmountFor,isnull(d.FSplitAmountFor,0) 
            update op set op.F_Ora_SplitAmount=isnull(d.FSplitAmount,0),op.FRecAmount=isnull(d.FSplitAmount,0),F_Ora_SplitAmountFor=isnull(d.FSplitAmountFor,0) 
            from(select FOrderPlanEID from T_CZ_ReceiptSplitOrderPlan where FID=100005)z 
            inner join t_Sal_OrderPlan op on z.FOrderPlanEID=op.FEntryID 
            left join(select z.FOrderPlanEID,sum(rp.FSplitAmount)FSplitAmount,sum(FSplitAmountFor)FSplitAmountFor 
	            from(select FOrderPlanEID from T_CZ_ReceiptSplitOrderPlan where FID=100005)z 
	            inner join T_CZ_ReceiptSplitOrderPlan rp on z.FOrderPlanEID=rp.FOrderPlanEID 
	            inner join T_CZ_ReceiptSplit r on rp.FID=r.FID and r.FDocumentStatus='C' 
	            group by z.FOrderPlanEID )d on z.FOrderPlanEID=d.FOrderPlanEID 
             */
            #endregion
            string _FID = this.CZ_GetFormID();
            StringBuilder _sb = new StringBuilder();
            _sb.Append("/*dialect*/");
            //----复写手工记帐单 已收款金额
            //--select d.FReceiptEID,je.F_Ora_SpiltAmount,isnull(d.FSplitAmt,0),je.F_Ora_SpiltAmountFor,j.FExchangeRate*isnull(d.FSplitAmt,0) 
            _sb.Append("update je set je.F_Ora_SplitAmount=isnull(d.FSplitAmt,0),je.F_Ora_SplitAmountFor=j.FExchangeRate*isnull(d.FSplitAmt,0) ");
            _sb.Append("from(select FReceiptEID from T_CZ_ReceiptSplitDetail where FID=" + _FID + ")z ");
            _sb.Append("inner join T_CN_JOURNALENTRY je on z.FReceiptEID=je.FEntryID inner join T_CN_JOURNAL j on je.FID=j.FID ");
            _sb.Append("left join(select z.FReceiptEID,sum(r.FSplitAmt)FSplitAmt ");
            _sb.Append("from(select FReceiptEID from T_CZ_ReceiptSplitDetail where FID=" + _FID + ")z ");
            _sb.Append("inner join T_CZ_ReceiptSplitDetail rd on z.FReceiptEID=rd.FReceiptEID ");
            _sb.Append("inner join T_CZ_ReceiptSplit r on rd.FID=r.FID and r.FDocumentStatus='C' ");
            _sb.Append("group by z.FReceiptEID )d on z.FReceiptEID=d.FReceiptEID ;");
            //----复写销售订单收款计划 已收款金额
            //--select op.FEntryID,op.F_Ora_ReceiveAmount,isnull(d.FSplitAmount,0),F_Ora_ReceiveAmountFor,isnull(d.FSplitAmountFor,0)
            //_sb.Append("update op set op.F_Ora_SplitAmount=isnull(d.FSplitAmount,0),op.FRecAmount=isnull(d.FSplitAmount,0), ");
            //_sb.Append("F_Ora_SplitAmountFor=isnull(d.FSplitAmountFor,0) ");
            _sb.Append("update op set op.F_Ora_SplitAmount=isnull(d.FSplitAmount,0),F_Ora_SplitAmountFor=isnull(d.FSplitAmountFor,0) ");
            _sb.Append("from(select FOrderPlanEID from T_CZ_ReceiptSplitOrderPlan where FID=" + _FID + ")z ");
            _sb.Append("inner join t_Sal_OrderPlan op on z.FOrderPlanEID=op.FEntryID ");
            _sb.Append("left join(select z.FOrderPlanEID,sum(rp.FSplitAmount)FSplitAmount,sum(FSplitAmountFor)FSplitAmountFor ");
            _sb.Append("from(select FOrderPlanEID from T_CZ_ReceiptSplitOrderPlan where FID=" + _FID + ")z ");
            _sb.Append("inner join T_CZ_ReceiptSplitOrderPlan rp on z.FOrderPlanEID=rp.FOrderPlanEID ");
            _sb.Append("inner join T_CZ_ReceiptSplit r on rp.FID=r.FID and r.FDocumentStatus='C' ");
            _sb.Append("group by z.FOrderPlanEID )d on z.FOrderPlanEID=d.FOrderPlanEID ");
            try
            {
                DBServiceHelper.Execute(base.Context, _sb.ToString());
            }
            catch (Exception ex)
            {
                this.View.ShowErrMessage("执行更新发生错误");
            }
        }

        /// <summary>
        /// 单据体 菜单按钮事件 订单收款计划
        /// </summary>
        /// <param name="e"></param>
        private void Act_AfterEBIC_oraBtnChsOP(AfterBarItemClickEventArgs e)
        {
            string _FBillStatus = this.CZ_GetFormStatus();
            string _FCdnSaleOrg = this.CZ_GetRowValue_DF("FReOrgID", "Id", 0, "0");
            string _FCdnCust = this.CZ_GetRowValue_DF("FReCustID", "Id", 0, "0");
            string _FSplitFlag = this.CZ_GetValue_DF("FSplitFlag", "0");

            if (_FBillStatus == "C" || _FBillStatus == "B")
            {
                return;
            }

            if (_FCdnSaleOrg == "0" || _FCdnCust == "0")
            {
                return;
            }

            if (_FSplitFlag != "1")
            {
                return;
            }

            string _BspParamStr = "Flag=AddNew;FCdnSaleOrg=" + _FCdnSaleOrg + ";FCdnCust=" + _FCdnCust;

            //show Form new 
            BillShowParameter _bsp = new BillShowParameter();
            _bsp.FormId = "ora_dk_GetSOP4ReSplit";
            _bsp.OpenStyle.ShowType = Kingdee.BOS.Core.DynamicForm.ShowType.MainNewTabPage;
            //_bsp.OpenStyle.ShowType = Kingdee.BOS.Core.DynamicForm.ShowType.Modal;
            _bsp.Status = Kingdee.BOS.Core.Metadata.OperationStatus.ADDNEW;
            _bsp.PKey = "0";
            _bsp.CustomParams.Add("InitParam", _BspParamStr);

            //this.View.LockBill();
            this.View.StyleManager.SetEnabled("F_ora_SpliteContainer", null, false);
            //打开基价计算单 有返回值则更新 无返回值则返回
            this.View.ShowForm(_bsp, (Kingdee.BOS.Core.DynamicForm.FormResult frt) =>
            {
                this.View.StyleManager.SetEnabled("F_ora_SpliteContainer", null, true);
                if (frt.ReturnData == null)
                {
                    return;
                }
                string _value = frt.ReturnData.ToString();
                //this.View.ShowMessage(_value);
                this.Act_Do_oraBtnChsOP(_value);
            });
        }

        /// <summary>
        /// 销售订单收款计划 调用窗口后 处理返回值 方法SQL已校正
        /// </summary>
        /// <param name="_value"></param>
        private void Act_Do_oraBtnChsOP(string _value)
        {
            if (_value == "")
            {
                return;
            }

            ArrayList _alOrderPlanEIDs = new ArrayList();
            int _maxRow = this.View.Model.GetEntryRowCount(Str_EntryKey_OP);
            for (int k = 0; k < _maxRow; k++)
            {
                string _rowOPEID = this.CZ_GetRowValue_DF("FOrderPlanEID", k, "0");
                if (!_alOrderPlanEIDs.Contains(_rowOPEID))
                {
                    _alOrderPlanEIDs.Add("|" + _rowOPEID + "|");
                }
            }

            #region 原始SQL语句 2020-04-23 此语句已校正
            /*
             * ----V1.0 废弃
            select b.FBillNo,b.FID,bp.FEntryID,bp.FSEQ,bp.FNeedRecAdvance,bp.FReceiveType,b.FSaleOrgID,b.FCustID,bf.FSettleCurrId,bf.FEXChangeTypeID,bf.FEXChangeRate,
            bp.FRecAdvanceAmount,bp.FRecAdvanceAmount*bf.FEXChangeRate FReceiptAmountFor,bp.FReMark,isnull(convert(varchar(10),bp.FMustDate,20),'null')FMustDate,
            FRecAdvanceAmount-F_ora_SplitAmount FRemainAmount,bp.FRecAdvanceAmount*bf.FEXChangeRate-F_ora_SplitAmountFor FRemainAmountFor 
            from(select item FEntryID from Fun_Split('101654,101655,101659',','))t inner join T_SAL_ORDERPLAN bp on t.FEntryID=bp.FEntryID 
            inner join T_SAL_ORDER b on bp.FID=b.FID inner join T_SAL_ORDERFIN bf on b.FID=bf.FID order by b.FID,bp.FSEQ 
            
             * ----V1.1
            select item FEntryID into #p from Fun_Split('107930,107888',',');
            select b.FBillNo,b.FID,bp.FEntryID,bp.FSEQ,bp.FNeedRecAdvance,bp.FReceiveType,b.FSaleOrgID,b.FCustID,bf.FSettleCurrId,bf.FEXChangeTypeID,bf.FEXChangeRate,
            bp.FRecAdvanceAmount,bp.FRecAdvanceAmount*bf.FEXChangeRate FReceiptAmountFor,bp.FReMark,isnull(convert(varchar(10),bp.FMustDate,20),'null')FMustDate,
            FRecAdvanceAmount-F_ora_SplitAmount FRemainAmount,bp.FRecAdvanceAmount*bf.FEXChangeRate-F_ora_SplitAmountFor FRemainAmountFor,
            bf.FBillAllAmount,isnull(od.FOutAmt,0)FOutAmt,isnull(convert(varchar(10),od.FOutDate,20),'')FOutDate 
            from #p t 
            inner join T_SAL_ORDERPLAN bp on t.FEntryID=bp.FEntryID 
            inner join T_SAL_ORDER b on bp.FID=b.FID inner join T_SAL_ORDERFIN bf on b.FID=bf.FID 
            inner join T_SAL_ORDERFIN bfn on b.FID=bfn.FID 
            left join( 
	            select b.FID,MAX(o.FDate)FOutDate,SUM(oef.FALLAMOUNT)FOutAmt from #p p 
	            inner join T_SAL_ORDERPLAN bp on p.FEntryID=bp.FEntryID 
	            inner join T_SAL_ORDER b on bp.FID=b.FID 
	            inner join T_SAL_ORDERENTRY be on b.FID=be.FID 
	            inner join T_SAL_OUTSTOCKENTRY_R oer on be.FENTRYID=oer.FSOEntryId 
	            inner join T_SAL_OUTSTOCKENTRY oe on oer.FENTRYID=oe.FEntryID inner join T_SAL_OUTSTOCKENTRY_F oef on oe.FEntryID=oef.FEntryID 
	            inner join T_SAL_OUTSTOCK o on oe.FID=o.FID and o.FDOCUMENTSTATUS='C' inner join T_SAL_OUTSTOCKFIN ofn on o.FID=ofn.FID 
	            group by b.FID 
            )od on b.FID=od.FID order by b.FID,bp.FSEQ 
            --drop table #p 
            
             */
            #endregion
            StringBuilder _sb = new StringBuilder();
            _sb.Append("/*dialect*/");
            _sb.Append("select item FEntryID into #p from Fun_Split('" + _value + "',',');");
            _sb.Append("select b.FBillNo,b.FID,bp.FEntryID,bp.FSEQ,bp.FNeedRecAdvance,bp.FReceiveType,b.FSaleOrgID,b.FCustID,bf.FSettleCurrId,bf.FEXChangeTypeID,bf.FEXChangeRate,");
            _sb.Append("bp.FRecAdvanceAmount,bp.FRecAdvanceAmount*bf.FEXChangeRate FReceiptAmountFor,bp.FReMark,isnull(convert(varchar(10),bp.FMustDate,20),'null')FMustDate,");
            _sb.Append("FRecAdvanceAmount-F_ora_SplitAmount FRemainAmount,bp.FRecAdvanceAmount*bf.FEXChangeRate-F_ora_SplitAmountFor FRemainAmountFor,");
            _sb.Append("bf.FBillAllAmount,isnull(od.FOutAmt,0)FOutAmt,isnull(convert(varchar(10),od.FOutDate,20),'')FOutDate ");
            _sb.Append("from #p t inner join T_SAL_ORDERPLAN bp on t.FEntryID=bp.FEntryID ");
            _sb.Append("inner join T_SAL_ORDER b on bp.FID=b.FID inner join T_SAL_ORDERFIN bf on b.FID=bf.FID inner join T_SAL_ORDERFIN bfn on b.FID=bfn.FID ");
            _sb.Append("left join( select b.FID,MAX(o.FDate)FOutDate,SUM(oef.FALLAMOUNT)FOutAmt from #p p ");
            _sb.Append("inner join T_SAL_ORDERPLAN bp on p.FEntryID=bp.FEntryID inner join T_SAL_ORDER b on bp.FID=b.FID ");
            _sb.Append("inner join T_SAL_ORDERENTRY be on b.FID=be.FID inner join T_SAL_OUTSTOCKENTRY_R oer on be.FENTRYID=oer.FSOEntryId ");
            _sb.Append("inner join T_SAL_OUTSTOCKENTRY oe on oer.FENTRYID=oe.FEntryID ");
            _sb.Append("inner join T_SAL_OUTSTOCKENTRY_F oef on oe.FEntryID=oef.FEntryID and oef.FALLAMOUNT>0 ");
            _sb.Append("inner join T_SAL_OUTSTOCK o on oe.FID=o.FID and o.FDOCUMENTSTATUS='C' inner join T_SAL_OUTSTOCKFIN ofn on o.FID=ofn.FID ");
            _sb.Append("group by b.FID )od on b.FID=od.FID order by b.FID,bp.FSEQ ");
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
                this.View.ShowErrMessage(_ex.Message + "\r\n sql:" + _schSql, "展开返回收款计划勾选结果时发生错误");
                return;
            }

            string _FOrderNo = "";              //销售订单 NO
            string _FOrderPSeq = "";            //计划行号
            string _FOrderInterID = "";         //订单内码
            string _FOrderPlanEID = "";         //计划行ID plan EntryID
            string _FNeedRecAdvance = "";       //计划行   是否预收
            string _FReceiptName = "";          //收款类型
            string _FCustID = "";               //客户
            string _FSettleCurrId = "";         //结算币别
            string _FEXChangeTypeID = "";       //汇率类型
            string _FEXChangeRate = "";         //汇率
            string _FReceiptAmount = "";        //应收金额
            string _FReceiptAmountFor = "";     //应收金额For
            string _FNote = "";                 //备注
            string _FMustDate = "";             //到款日
            string _FRemainAmount = "";         //未到款金额
            string _FRemainAmountFor = "";      //未到款金额For
            string _FSplitAmount = "";          //本次拆分金额
            string _FSplitAmountFor = "";       //本次拆分金额本位币
            string _FPURPOSEID = "";            //基础资料-收款类型
            string _FRECEIVEITEMTYPE = "";      //下拉列表-预收项目类型
            string _FBillAllAmount = "";        //订单价税合计
            string _FOutAmt = "";               //销售出库单 已发货金额合计
            string _FOutDate = "";              //销售出库单 最近发货日期

            int _key = _maxRow;
            for (int i = 0; i < _objDT.Rows.Count; i++)
            {
                //int _key = _maxRow + i;
                _FOrderPlanEID = _objDT.Rows[i]["FEntryID"].ToString();
                if (_alOrderPlanEIDs.Contains("|" + _FOrderPlanEID + "|"))
                {
                    continue;
                }
                else
                {
                    this.View.Model.CreateNewEntryRow(Str_EntryKey_OP);   //新增一行
                    _alOrderPlanEIDs.Add("|" + _FOrderPlanEID + "|");
                }
                _FOrderNo = _objDT.Rows[i]["FBillNo"].ToString();
                _FOrderPSeq = _objDT.Rows[i]["FSEQ"].ToString();
                _FOrderInterID = _objDT.Rows[i]["FID"].ToString();
                //_FOrderPlanEID
                _FNeedRecAdvance = _objDT.Rows[i]["FNeedRecAdvance"].ToString();
                _FReceiptName = _objDT.Rows[i]["FReceiveType"].ToString();
                _FCustID = _objDT.Rows[i]["FCustID"].ToString();
                _FSettleCurrId = _objDT.Rows[i]["FSettleCurrId"].ToString();
                _FEXChangeTypeID = _objDT.Rows[i]["FEXChangeTypeID"].ToString();
                _FEXChangeRate = _objDT.Rows[i]["FEXChangeRate"].ToString();
                _FReceiptAmount = _objDT.Rows[i]["FRecAdvanceAmount"].ToString();
                _FReceiptAmountFor = _objDT.Rows[i]["FReceiptAmountFor"].ToString();
                _FNote = _objDT.Rows[i]["FReMark"].ToString();
                _FMustDate = _objDT.Rows[i]["FMustDate"].ToString();
                _FRemainAmount = _objDT.Rows[i]["FRemainAmount"].ToString();
                _FRemainAmountFor = _objDT.Rows[i]["FRemainAmountFor"].ToString();
                _FBillAllAmount = _objDT.Rows[i]["FBillAllAmount"].ToString();
                _FOutAmt = _objDT.Rows[i]["FOutAmt"].ToString();
                _FOutDate = _objDT.Rows[i]["FOutDate"].ToString();

                _FSplitAmount = "0";
                _FSplitAmountFor = "0";
                //预收 FPURPOSEID =20011  ,FRECEIVEITEMTYPE=1         否  FPURPOSEID=20010,FRECEIVEITEMTYPE=''
                _FPURPOSEID = _FNeedRecAdvance == "0" ? "20010" : "20011";
                _FRECEIVEITEMTYPE = _FNeedRecAdvance == "0" ? "" : "1";

                this.View.Model.SetValue("FOrderNo", _FOrderNo, _key);
                this.View.Model.SetValue("FOrderPSeq", _FOrderPSeq, _key);
                this.View.Model.SetValue("FOrderInterID", _FOrderInterID, _key);
                this.View.Model.SetValue("FOrderPlanEID", _FOrderPlanEID, _key);
                this.View.Model.SetValue("FReceiptName", _FReceiptName, _key);
                this.View.Model.SetValue("FCustID", _FCustID, _key);
                this.View.Model.SetValue("FSettleCurrId", _FSettleCurrId, _key);
                this.View.Model.SetValue("FEXChangeTypeID", _FEXChangeTypeID, _key);
                this.View.Model.SetValue("FEXChangeRate", _FEXChangeRate, _key);
                this.View.Model.SetValue("FReceiptAmount", _FReceiptAmount, _key);
                this.View.Model.SetValue("FReceiptAmountFor", _FReceiptAmountFor, _key);
                this.View.Model.SetValue("FNote", _FNote, _key);
                if (_FMustDate != "" && _FMustDate != "null")
                {
                    this.View.Model.SetValue("FMustDate", _FMustDate, _key);
                }
                this.View.Model.SetValue("FRemainAmount", _FRemainAmount, _key);
                this.View.Model.SetValue("FRemainAmountFor", _FRemainAmountFor, _key);
                this.View.Model.SetValue("FSplitAmount", _FSplitAmount, _key);
                this.View.Model.SetValue("FSplitAmountFor", _FSplitAmountFor, _key);
                this.View.Model.SetValue("FPURPOSEID", _FPURPOSEID, _key);
                this.View.Model.SetValue("FRECEIVEITEMTYPE", _FRECEIVEITEMTYPE, _key);
                this.View.Model.SetValue("FBillAllAmount", _FBillAllAmount, _key);
                this.View.Model.SetValue("FOutAmt", _FOutAmt, _key);
                this.View.Model.SetValue("FOutDate", _FOutDate, _key);
                _key++;
            }
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

        /// <summary>
        /// 清空表体行
        /// </summary>
        /// <param name="_entity">单据体 标识</param>
        /// <param name="_addNewRow">清空后是否添加新行</param>
        private void CZ_EntityClear(string _entity,bool _addNewRow)
        {
            //清除行
            int _rowCnt = this.View.Model.GetEntryRowCount(_entity);
            for (int i = _rowCnt - 1; i >= 0; i--)
            {
                this.View.Model.DeleteEntryRow(_entity, i);
            }
            if (_addNewRow)
            {
                this.View.Model.CreateNewEntryRow(_entity);   //新增一行
            }
        }
        #endregion

    }
}
