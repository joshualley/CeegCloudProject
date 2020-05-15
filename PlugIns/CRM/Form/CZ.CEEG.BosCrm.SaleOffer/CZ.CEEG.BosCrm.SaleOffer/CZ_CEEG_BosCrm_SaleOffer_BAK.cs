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

namespace CZ.CEEG.BosCrm.SaleOffer
{
    /// <summary>
    /// BOS_CRM_报价
    /// </summary>
    [Description("BOS_CRM_报价")]
    [Kingdee.BOS.Util.HotUpdate]
    public class CZ_CEEG_BosCrm_SaleOffer_BAK : AbstractBillPlugIn         //AbstractDynamicFormPlugIn
    {
        #region 明细表体 CRM物料相关基础资料 窗体变量组
        /// <summary>
        /// 删除对象中存在主体的，记录分组号[项次]
        /// </summary>
        ArrayList Al_EntryDelGroup = new ArrayList();
        /// <summary>
        /// CRM分类   -     独立附件=123323
        /// </summary>
        string Val_FMtlGroup_XX = "123323";
        /// <summary>
        /// CRM组件（附件）-主体=123328
        /// </summary>
        string Val_FMtlItem_Base = "123328";
        /// <summary>
        /// 锁定引发变更，当一个流程在执行时，排除其他值更新事件
        /// </summary>
        string Lock_ChangeVal = "";
        /// <summary>
        /// 跟据明细表体 确定基价计算表体可显示内容（分组号） 集合
        /// </summary>
        ArrayList AL_EntryBPRFilter;
        #endregion

        #region 汇率 窗体变量组
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

        #region 其他窗体变量 及 预置值
        /// <summary>
        /// 表体 明细信息  ID     =FEntity
        /// </summary>
        string Str_EntryKey_Main = "FEntity";
        /// <summary>
        /// 表体 基价计算单 ID   =FEntityBPR   (BasePriceRound)
        /// </summary>
        string Str_EntryKey_BPR = "FEntityBPR";
        #endregion

        #region K3 Override
        /// <summary>
        /// 数据加载完毕
        /// </summary>
        /// <param name="e"></param>
        public override void AfterBindData(EventArgs e)
        {
            /*
            ////获取单据体表格, 参数为单据体Key，示例代码假设为FEntity
            //Kingdee.BOS.Core.DynamicForm.PlugIn.ControlModel.EntryGrid grid = this.View.GetControl<EntryGrid>("FEntity");
            ////设置第一行的背景色，参数：颜色，6位16进制符号，每2位代表一种基色；从0开始，行序号
            //grid.SetRowBackcolor("#FFC080", 0);
            ////设置第二行F1字段的背景色，参数：字段Key；颜色；行序号
            //grid.SetBackcolor("F1", "#FFC080", 1);
            */

            //取用户授权 明细信息列表授权 物料分类
            string _FUserID = this.View.Context.UserId.ToString();
            string _sql = "exec proc_cztyCrm_OfferGetMtlGroup @FUserID='" + _FUserID + "'";
            string _crmFilter = "";
            try
            {
                DataTable _dt = this.CZDB_SearchBase(_sql);
                _crmFilter = _dt.Rows[0]["FIDFilter"].ToString();
                this.View.Model.SetValue("FLocalFilter", _crmFilter);
            }
            catch (Exception _ex)
            {
                return;
            }

            if (this.CZ_GetFormStatus() == "Z")
            {
                Act_Hold_GetHoldInfo(this.Context.UserId.ToString());
                //汇率
                Act_Rate_GetRate(this.CZ_GetValue_DF("FCreateDate", ""));
            }

            //if (this.CZ_GetFormStatus() == "Z" || this.CZ_GetFormStatus() == "A")
            //{
            //    return;
            //}

            Act_CtrlRow_SetHideMkt();
            Act_EntityFilter();
            Act_EntityFilter_GetBPR();
            //Act_EntityFilter_SetBPR();
            Act_CtrlRow_SetHideMkt_BPR();
            Act_EntityFilter_BPR();
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

        /// <summary>
        /// 删除行前 记录 是否存在主体 存在则记录-按钮事件后处理
        /// </summary>
        /// <param name="e"></param>
        public override void BeforeDeleteRow(BeforeDeleteRowEventArgs e)
        {
            base.BeforeDeleteRow(e);
            string _EntryName = e.EntityKey;
            if (_EntryName == Str_EntryKey_Main)
            {
                //Al_EntryDelGroup = new ArrayList();   //多行删除 一次性调用多次 不能重新实例化
                string _rowFMtlItem = "";       //123328-本体
                string _rowFDataGroup = "";

                _rowFMtlItem = this.CZ_GetRowValue_DF("FMtlItem", "Id", e.Row, "0");
                _rowFDataGroup = this.CZ_GetRowValue_DF("FDATAGROUP", e.Row, "0");
                //  附件=主体                   分组号[项次]!=0                   集合不包含分组号[项次]   
                if (_rowFMtlItem == Val_FMtlItem_Base && _rowFDataGroup != "0" && !Al_EntryDelGroup.Contains(_rowFDataGroup))
                {
                    Al_EntryDelGroup.Add(_rowFDataGroup);
                }

                //this.View.ShowMessage(e.Row.ToString());
            }
        }

        /// <summary>
        /// 值更新 物料组-附件-分组号[项次]   ｜  基本单价-数量-报价 ： 基价合计 ： 总基价-总报价-【扣款】
        /// </summary>
        /// <param name="e"></param>
        public override void DataChanged(DataChangedEventArgs e)
        {
            string _key = e.Field.Key.ToUpperInvariant();

            if (Lock_ChangeVal != "")
            {
                return;
            }

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
                //基本信息｜ 表头信息
                case "FRANGEAMT":   //表头    FRangeAmt 扣款金额
                    Act_DC_FRangeAmt(e);
                    break;
                case "FCRMHDDEPT":  //FCrmHdDept
                    //Act_DC_FCrmHdDept_T(e);
                    break;
                //明细信息
                case "FMTLGROUP":   //行 FMtlGroup CRM物料分组
                    Act_DC_FMtlGroup(e);
                    break;
                case "FMTLITEM":    //行 FMtlItem CRM物料
                    Act_DC_FMtlItem(e);
                    break;
                case "FDATAGROUP":  //行 FDataGroup 分组号[项次]
                    Act_DC_FDataGroup(e);
                    break;
                case "FBASEPRICE":  //行 FBasePrice 基本单价
                    Act_DC_BP8Qty(e);
                    Act_DC_RndAmount();
                    break;
                case "FQTY":        //行 FQty 数量
                    Act_DC_BP8Qty(e);
                    Act_DC_RndAmount();
                    break;
                case "FRPTPRICE":   //行 FRptPrice 报价
                    Act_DC_FRptPrice(e);
                    ////Act_DC_RndAmount();
                    break;
                //基价计算单 值更新方法组
                case "FBDATAGROUP":
                    //FBDataGroup
                    //Act_DC_FBDataGroup(e);
                    break;
                case "FBQTY":
                    //FBQty     基价计算-数量
                    Act_DC_BPR_FBQty(e);
                    break;
                case "FBPRICE":
                    //FBPrice   基价计算-单价
                    Act_DC_BPR_FBPrice(e);
                    break;
                case "FBAMT":
                    //FBAmt     基价计算-总价
                    Act_DC_BPR_FBAmt(e);
                    break;
                case "FBGPAMTB":
                    //FBGpAmtB
                    Act_DC_BPR_FBGpAmtB(e);
                    break;
                case "FBCOSTRATE":
                    //FBCostRate    基价计算-费用率%
                    Act_DC_BPR_FBCostRate(e);
                    break;
                case "FBCOST":
                    //FBCost        基价计算-费用
                    Act_DC_BPR_FBCost(e);
                    break;
                case "FBGPRATE":
                    //FBGPRate      基价计算-毛利率%
                    Act_DC_BPR_FBGPRate(e);
                    break;
                case "FBGP":
                    //FBGP  毛利
                    Act_DC_BPR_FBGP(e);
                    break;
                default:
                    break;
            }
            base.DataChanged(e);
        }

        /// <summary>
        /// 值更新前 验证用 条件排除 
        /// </summary>
        /// <param name="e"></param>
        public override void BeforeUpdateValue(BeforeUpdateValueEventArgs e)
        {
            string _key = e.Key.ToUpperInvariant();
            if (Lock_ChangeVal != "")
            {
                return;
            }
            switch (_key)
            {
                case "FRPTPRICE":   //行 FRptPrice 报价
                    Act_BUV_FRptPrice(e);
                    //Act_DC_RndAmount();
                    break;
                case "FBDATAGROUP": //BPR行 FBDataGroup 分组号
                    Act_BUV_FBDataGroup(e);
                    break;
                default:
                    break;
            }

            base.BeforeUpdateValue(e);
        }

        /// <summary>
        /// 按钮事件
        /// <param name="e"></param>
        public override void AfterButtonClick(AfterButtonClickEventArgs e)
        {
            string _key = e.Key.ToUpperInvariant();
            switch (_key)
            {
                case "FBTNTEST":
                    //FBtnTest
                    //Act_EntityFilter();
                    //this.View.Refresh();
                    Act_EntityFilterEmpty();
                    Act_Control_Visable();
                    break;
                case "FBTNANZRPT":
                    //FBtnAnzRpt-拆分报价
                    Act_BC_AnzFRptPrice();
                    break;
                default:
                    break;
            }
            base.AfterButtonClick(e);
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
                case "TBNEWLIST":
                    //tbNewList         下拉按钮-新增行
                    Act_EntityFilter();
                    break;
                case "TBNEWENTRY":
                    //tbNewEntry        按钮-新增行
                    Act_EntityFilter();
                    break;
                case "TBINSERtENTRY":
                    //tbInsertEntry     按钮-插入行
                    Act_EntityFilter();
                    break;
                case "TBDELETEENTRY":
                    //tbDeleteEntry     按钮-删除行
                    Act_AfterEBIC_tbDeleteEntry();
                    Act_EntityFilter();
                    break;
                case "TBNEWENTRYB":
                    //tbNewEntryB       BPR-按钮-新增行
                    Act_EntityFilter_GetBPR();
                    Act_EntityFilter_BPR();
                    break;
                case "TBINSERTENTRYB":
                    //tbInsertEntryB    BPR-按钮-插入行
                    Act_EntityFilter_GetBPR();
                    Act_EntityFilter_BPR();
                    break;
                case "TBDELETEENTRYB":
                    //tbDeleteEntryB    BPR-按钮-删除行
                    Act_EntityFilter_GetBPR();
                    Act_EntityFilter_BPR();
                    Act_AfterEBIC_tbDeleteEntryB();
                    break;
                case "TBRUSH2MAIN":
                    //tbRush2Main       BPR-按钮-更新明细基价
                    Act_AEBIC_TbRush2Main(e);
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// 在根据编码检索数据之前调用；
        /// 通过重载本事件，可以设置必要的过滤条件，以限定检索范围；
        /// 还可以控制当前过滤是否启用组织隔离，数据状态隔离
        /// </summary>
        /// <param name="e"></param>
        public override void BeforeSetItemValueByNumber(BeforeSetItemValueByNumberArgs e)
        {
            string _crmFilter;

            switch (e.BaseDataField.Key.ToUpperInvariant())
            {
                //case "FXXX":通过字段的Key[大写]来区分不同的基础资料
                //e.Filter = "FXXX= AND fxxy=";过滤的字段使用对应基础资料的字段的Key，支持ksql语法
                //break;
                case "FMTLGROUP":
                    _crmFilter = this.CZ_GetValue("FLocalFilter");
                    if (_crmFilter != "")
                    {
                        e.Filter = " FID in(" + _crmFilter + ")";
                    }
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// 显示基础资料列表之前调用
        /// 通过重载本事件，可以设置必要的过滤条件，以限定检索范围；
        /// </summary>
        /// <param name="e"></param>
        public override void BeforeF7Select(BeforeF7SelectEventArgs e)
        {
            string _crmFilter;

            switch (e.FieldKey.ToUpperInvariant())
            {
                //case "FXXX":通过字段的Key[大写]来区分不同的基础资料
                //    e.ListFilterParameter.Filter = "FXXX= AND fxxy=";过滤的字段使用对应基础资料的字段的Key，支持ksql语法
                //break;
                case "FMTLGROUP":
                    _crmFilter = this.CZ_GetValue("FLocalFilter");
                    if (_crmFilter != "")
                    {
                        e.ListFilterParameter.Filter = " FID in(" + _crmFilter + ")";
                    }
                    break;
                default:
                    break;
            }

            base.BeforeF7Select(e);
        }
        #endregion

        #region Actions
        /// <summary>
        /// 计算行过滤 需要过滤时 标记行
        /// 跟据Crm产品类别确认明细信息是否可显示行
        /// 提取【明细信息】可显示的分组号限定【基价计算单】的分组号    用于限定显示及使用
        /// </summary>
        private void Act_CtrlRow_SetHideMkt()
        {
            string _crmFilter = this.CZ_GetValue("FLocalFilter");
            if (_crmFilter == "")
            {
                return;
            }

            ArrayList _alCrmMtlGroup = new ArrayList();
            _alCrmMtlGroup.AddRange(_crmFilter.Split(','));
            //AL_EntryBPRFilter = new ArrayList();
            int _rowCnt = this.View.Model.GetEntryRowCount("FEntity");
            string _rowFMtlGroup = "0";
            //string _rowFDataGroup = "0";
            string _rowFIS2W = "0";
            for (int i = 0; i < _rowCnt; i++)
            {
                _rowFMtlGroup = this.CZ_GetRowValue_DF("FMtlGroup", "Id", i, "0");
                if (_rowFMtlGroup == "0")
                {
                    _rowFIS2W = "0";
                }
                else
                {
                    _rowFIS2W = _alCrmMtlGroup.Contains(_rowFMtlGroup) ? "0" : "1";
                }
                this.View.Model.SetValue("FIS2W", _rowFIS2W, i);
            }
        }

        /// <summary>
        /// 计算行过滤 需要过滤时 标记行 BPR
        /// 跟据明细表体可显示分组号 设置基价计算单表体行
        /// </summary>
        private void Act_CtrlRow_SetHideMkt_BPR()
        {
            if (!AL_EntryBPRFilter.Contains("0"))
            {
                AL_EntryBPRFilter.Add("0");
            }

            int _rowCntBPR = this.View.Model.GetEntryRowCount(Str_EntryKey_BPR);
            string _rowFBDataGroup = "";
            string _rowFBIS2W = "";
            for (int i = 0; i < _rowCntBPR; i++)
            {
                _rowFBDataGroup = this.CZ_GetRowValue("FBDataGroup", i);
                _rowFBIS2W = AL_EntryBPRFilter.Contains(_rowFBDataGroup) ? "0" : "1";
                this.View.Model.SetValue("FBIS2W", _rowFBIS2W, i);
            }
        }

        /// <summary>
        /// FEntity表体 报价员 按授权 CRM-产品分类 过滤 明细信息表体
        /// </summary>
        private void Act_EntityFilter()
        {
            string _crmFilter = this.CZ_GetValue("FLocalFilter");
            if (_crmFilter == "")
            {
                return;
            }

            //string _userID = this.Context.UserId.ToString();    //登录用户
            //string _docStatus = this.CZ_GetFormStatus();

            //Kingdee.BOS.Core.Metadata.EntityElement.Entity objEE = this.View.Model.BillBusinessInfo.GetEntity("FEntity");

            EntryGrid grid = this.View.GetControl<EntryGrid>("FEntity");
            //String filter = string.Format("FDataGroup=1 or FDataGroup=2");
            //String filter = string.Format("FMtlGroup=123324 or FMtlGroup=123325 or FMtlGroup=123323");    //基础资料无效不能作为判定条件
            String filter = string.Format("FIS2W=0 or FIS2W=''");
            grid.SetFilterString(filter);
            grid.SetCustomPropertyValue("AllowSorting", false);
            //grid.SetSort("", EntryGridFieldSortOrder.None);
            this.View.UpdateView("FEntity");
        }

        /// <summary>
        /// FEntityBPR表体
        /// </summary>
        private void Act_EntityFilter_BPR()
        {
            EntryGrid grid = this.View.GetControl<EntryGrid>(Str_EntryKey_BPR);
            String filter = string.Format("FBIS2W=0 or FBIS2W=''");
            grid.SetFilterString(filter);
            grid.SetCustomPropertyValue("AllowSorting", false);
            //grid.SetSort("", EntryGridFieldSortOrder.None);
            this.View.UpdateView(Str_EntryKey_BPR);
        }

        /// <summary>
        /// 遍历 明细信息 表体 获取当前可显示 分组号 集合
        /// </summary>
        private void Act_EntityFilter_GetBPR()
        {
            AL_EntryBPRFilter = new ArrayList();
            int _rowCntMain = this.View.Model.GetEntryRowCount(Str_EntryKey_Main);
            int _rowCntBPR = this.View.Model.GetEntryRowCount(Str_EntryKey_BPR);
            if (_rowCntMain == 0 || _rowCntMain == 0)
            {
                return;
            }

            string _rowFDataGroup = "0";
            string _rowFIS2W = "0";
            for (int i = 0; i < _rowCntMain; i++)
            {
                _rowFDataGroup = this.CZ_GetRowValue("FDataGroup", i);
                _rowFIS2W = this.CZ_GetRowValue("FIS2W", i);
                if ((_rowFIS2W == "0" || _rowFIS2W == "") && !AL_EntryBPRFilter.Contains(_rowFDataGroup))
                {
                    AL_EntryBPRFilter.Add(_rowFDataGroup);
                }
            }
        }

        /// <summary>
        /// 根据 (ArrayList)AL_EntryBPRFilter 设置 基价计算单 隐藏 判定逻辑变更 此方法已停用 
        /// </summary>
        private void Act_EntityFilter_SetBPR()
        {
            if (!AL_EntryBPRFilter.Contains("0"))
            {
                AL_EntryBPRFilter.Add("0");
            }

            string _crmFilterBPR = "";
            this.CZ_Rnd_AL2Str(AL_EntryBPRFilter, ref _crmFilterBPR);

            if (_crmFilterBPR == "")
            {
                return;
            }

            EntryGrid grid = this.View.GetControl<EntryGrid>(Str_EntryKey_BPR);
            String filter = string.Format("FBDataGroup in(" + _crmFilterBPR + ")");
            grid.SetFilterString(filter);
            grid.SetCustomPropertyValue("AllowSorting", false);
            this.View.UpdateView(Str_EntryKey_BPR);
        }

        /// <summary>
        /// FEntity表体  撤消隐藏 测试用
        /// </summary>
        private void Act_EntityFilterEmpty()
        {
            EntryGrid grid = this.View.GetControl<EntryGrid>("FEntity");
            grid.SetFilterString("");
            this.View.UpdateView("FEntity");

            EntryGrid gridBPR = this.View.GetControl<EntryGrid>(Str_EntryKey_BPR);
            gridBPR.SetFilterString("");
            this.View.UpdateView(Str_EntryKey_BPR);
        }

        /// <summary>
        /// 目标不显示 测试用
        /// </summary>
        private void Act_Control_Visable()
        {
            this.View.StyleManager.SetVisible("F_ora_Tab1_P2", null, false);
            this.View.StyleManager.SetEnabled("FCustName", "", false);
        }

        /// <summary>
        /// FCrmHdDept 持有部门 测试用
        /// </summary>
        /// <param name="e"></param>
        private void Act_DC_FCrmHdDept_T(DataChangedEventArgs e)
        {
            string _newID = e.NewValue.ToString();
            DynamicObject _objDept = this.View.Model.GetValue("FCrmHdDept") as DynamicObject;   //取得更新后的值
            _newID = _objDept["Id"].ToString();
        }

        /// <summary>
        /// 添加行后
        /// </summary>
        /// <param name="e"></param>
        private void Act_AfterCNER(CreateNewEntryEventArgs e)
        {
            string _EntryName = e.Entity.DynamicObjectType.ToString();      //引发事件的表体名 FEntity-明细信息 ｜ FEntityBPR-基价计算表体

            if (_EntryName == Str_EntryKey_Main)
            {
                if (this.View.Model.GetEntryRowCount("FEntity") == 1)
                {
                    this.View.Model.SetValue("FDataGroup", "1", e.Row);
                }
                return;
            }

            if (_EntryName == Str_EntryKey_BPR && e.Row > 0)
            {
                string _curBDG = this.CZ_GetRowValue_DF("FBDataGroup", e.Row - 1, "0");
                this.View.Model.SetValue("FBDataGroup", _curBDG, e.Row);
                return;
            }
            ////string _FMtlItem=this.CZ_GetValue("FMtlItem", "Id", e.Row);
            //string _prvFDataGroup = "";
            //string _prvFMtlGroup = "";

            //_prvFDataGroup = this.View.Model.GetValue("FDataGroup", e.Row - 1).ToString();
            //_prvFMtlGroup = CZ_GetValue("FMtlGroup", "Id", e.Row - 1);    //this.View.Model.GetValue("FMtlGroup", e.Row - 1).ToString();
            //this.View.Model.SetValue("FDataGroup", _prvFDataGroup, e.Row);
            //this.View.Model.SetValue("FMtlGroup", _prvFMtlGroup, e.Row);

            //Act_EntityFilter();
        }

        /// <summary>
        /// 菜单按钮事件处理 删除行 逻辑承接 行删除前(BeforeDeleteRow)处理
        /// </summary>
        private void Act_AfterEBIC_tbDeleteEntry()
        {
            if (Al_EntryDelGroup.Count == 0)
            {
                return;
            }

            int _rowCnt = this.View.Model.GetEntryRowCount("FEntity");

            string _rowFDataGroup = "";
            //循环Al_EntryDelGroup 删除已移除本体的附件项
            for (int i = _rowCnt - 1; i >= 0; i--)
            {
                _rowFDataGroup = this.CZ_GetRowValue_DF("FDATAGROUP", i, "0");
                if (Al_EntryDelGroup.Contains(_rowFDataGroup))
                {
                    this.View.Model.DeleteEntryRow("FEntity", i);
                }
            }
            Act_DC_RndAmount();
            Al_EntryDelGroup = new ArrayList();
        }

        /// <summary>
        /// 菜单按钮事件处理 删除行 重算合计价
        /// </summary>
        private void Act_AfterEBIC_tbDeleteEntryB()
        {
            string _FBDataGroup = "";
            int _maxCnt = this.View.Model.GetEntryRowCount(Str_EntryKey_BPR);
            int _groupTopIdx = -1;      //当前组号第一行 索引号
            string _rowFBDG = "";
            string _rowFBAmt = "0";     //行总价
            double _FBGpAmtB = 0;       //分组总价
            for (int i = 0; i < _maxCnt; i++)
            {
                _rowFBDG = this.CZ_GetRowValue_DF("FBDataGroup", i, "0");
                if (i == 0)
                {
                    _FBDataGroup = _rowFBDG;
                    _groupTopIdx = i;
                    _rowFBAmt = this.CZ_GetRowValue_DF("FBAmt", i, "0");
                    _FBGpAmtB = Double.Parse(_rowFBAmt);
                    continue;
                }

                //同分组
                if (_FBDataGroup == _rowFBDG)
                {
                    _rowFBAmt = this.CZ_GetRowValue_DF("FBAmt", i, "0");
                    _FBGpAmtB = _FBGpAmtB + Double.Parse(_rowFBAmt);
                    continue;
                }
                else
                {
                    //不同分组
                    this.View.Model.SetValue("FBGpAmtB", _FBGpAmtB.ToString(), _groupTopIdx);
                    _FBDataGroup = _rowFBDG;
                    _groupTopIdx = i;
                    _rowFBAmt = this.CZ_GetRowValue_DF("FBAmt", i, "0");
                    _FBGpAmtB = Double.Parse(_rowFBAmt);
                }
            }
            this.View.Model.SetValue("FBGpAmtB", _FBGpAmtB.ToString(), _groupTopIdx);
        }

        #region DataChanged => 行 CRM物料分类 | CRM组件 | 分组号[项次] 变更控制方法组
        /// <summary>
        /// DataChanged - FMTLGROUP 值更新 CRM物料分组
        /// </summary>
        /// <param name="e"></param>
        private void Act_DC_FMtlGroup(DataChangedEventArgs e)
        {
            /*CRM物料组发生变更
             * !=：【本体】——到下一本体结束 
             */
            string _newVal = e.NewValue == null ? "0" : e.NewValue.ToString();
            string _oldVal = e.OldValue == null ? "0" : e.OldValue.ToString();
            string _eFMtlItem = this.CZ_GetRowValue_DF("FMtlItem", "Id", e.Row, "0");
            string _eFDataGroup = this.CZ_GetRowValue_DF("FDataGroup", e.Row, "0");

            //如果本行 附件!=（0｜主体）
            if (_eFMtlItem != "0" && _eFMtlItem != Val_FMtlItem_Base)
            {
                return;
            }

            //如果行 分组号[项次]=0 取一个新分组号[项次]替代
            if (_eFDataGroup == "0")
            {
                _eFDataGroup = (Act_GetMaxDataGroup(new ArrayList()) + 1).ToString();
                this.View.Model.SetValue("FDataGroup", _eFDataGroup, e.Row);
            }

            int _maxCnt = this.View.Model.GetEntryRowCount("FEntity");
            string _rowFMtlGroup = "";
            string _rowFMtlItem = "";
            for (int i = e.Row + 1; i < _maxCnt; i++)
            {
                _rowFMtlGroup = this.CZ_GetRowValue_DF("FMtlGroup", "Id", i, "0");
                _rowFMtlItem = this.CZ_GetRowValue_DF("FMtlItem", "Id", i, "0");
                //循环行 类别=独立附件 or 附件=主体 返回
                if (_rowFMtlGroup == Val_FMtlGroup_XX || _rowFMtlItem == Val_FMtlItem_Base)
                {
                    return;
                }
                //循环行 类别=空 or e原值
                if (_rowFMtlGroup == _oldVal || _rowFMtlGroup == "0")
                {
                    this.View.Model.SetValue("FMtlGroup", _newVal, i);
                    this.View.Model.SetValue("FDataGroup", _eFDataGroup, i);
                }
            }
        }

        /// <summary>
        /// DataChanged - FMtlItem 值更新 CRM附件
        /// </summary>
        /// <param name="e"></param>
        private void Act_DC_FMtlItem(DataChangedEventArgs e)
        {
            string _newVal = e.NewValue == null ? "0" : e.NewValue.ToString();
            string _oldVal = e.OldValue == null ? "0" : e.OldValue.ToString();
            string _eFMtlGroup = this.CZ_GetRowValue_DF("FMtlGroup", "Id", e.Row, "0");
            string _eFDataGroup = this.CZ_GetRowValue_DF("FDataGroup", e.Row, "0");

            // CRM物料分组= 独立附件
            if (_eFMtlGroup == Val_FMtlGroup_XX)
            {
                return;
            }

            //如果新值=主体 行无分组号[项次] 补分组号[项次]
            if (_newVal == Val_FMtlItem_Base && _eFDataGroup == "0")
            {
                _eFDataGroup = (Act_GetMaxDataGroup(new ArrayList()) + 1).ToString();
                this.View.Model.SetValue("FDataGroup", _eFDataGroup, e.Row);
                return;
            }

            if (e.Row == 0 || _newVal == Val_FMtlItem_Base)
            {
                return;
            }

            string _prvFDataGroup = this.View.Model.GetValue("FDataGroup", e.Row - 1).ToString();
            string _prvFMtlGroup = this.CZ_GetRowValue("FMtlGroup", "Id", e.Row - 1);    //this.View.Model.GetValue("FMtlGroup", e.Row - 1).ToString();
            this.View.Model.SetValue("FDataGroup", _prvFDataGroup, e.Row);
            this.View.Model.SetValue("FMtlGroup", _prvFMtlGroup, e.Row);
        }

        /// <summary>
        /// DataChanged - FDATAGROUP 值更新 CRM分组号[项次]
        /// </summary>
        /// <param name="e"></param>
        private void Act_DC_FDataGroup(DataChangedEventArgs e)
        {
            string _newVal = e.NewValue == null ? "0" : e.NewValue.ToString();
            string _oldVal = e.OldValue == null ? "0" : e.OldValue.ToString();
            string _eFMtlGroup = this.CZ_GetRowValue_DF("FMtlGroup", "Id", e.Row, "0");
            string _eFMtlItem = this.CZ_GetRowValue_DF("FMtlItem", "Id", e.Row, "0");

            //本行 CRM物料分组=独立组件|0                             ｜ CRM组件！=主体
            if (_eFMtlGroup == Val_FMtlGroup_XX || _eFMtlGroup == "0" || _eFMtlItem != Val_FMtlItem_Base)
            {
                return;
            }

            int _maxCnt = this.View.Model.GetEntryRowCount("FEntity");
            string _rowFMtlGroup = "";
            string _rowFMtlItem = "";
            //循环行 检测到 行物料分组变化 或 附件=主体 退出
            for (int i = e.Row + 1; i < _maxCnt; i++)
            {
                _rowFMtlGroup = this.CZ_GetRowValue_DF("FMtlGroup", "Id", i, "0");
                _rowFMtlItem = this.CZ_GetRowValue_DF("FMtlItem", "Id", i, "0");
                if (_rowFMtlGroup != _eFMtlGroup || _rowFMtlItem == Val_FMtlItem_Base)
                {
                    return;
                }
                this.View.Model.SetValue("FDataGroup", _newVal, i);
            }
        }
        #endregion

        #region DataChanged => 行 基本单价｜数量 ｜报价  =>（本体行+独立附件行）合计基价 ｜总基价｜总报价 方法组
        /// <summary>
        /// DataChanged 行 FBasePrice 基本单价 + 行 FQty 数量
        /// </summary>
        /// <param name="_eRow"></param>
        private void Act_DC_BP8Qty(int _eRow)
        {
            //string _eFMtlGroup = this.CZ_GetValue_DF("FMtlGroup", "Id", _eRow, "0");              //
            //string _eFMtlItem = this.CZ_GetValue_DF("FMtlItem", "Id", _eRow, "0");                //
            //string _eFDataGroup = this.CZ_GetValue_DF("FDataGroup", _eRow, "0");                  //
            //double _eFBPAmt = Double.Parse(this.CZ_GetValue_DF("FBPAmt", _eRow, "0"));            //行基本金额
            //double _eFBPAmtGroup = Double.Parse(this.CZ_GetValue_DF("FBPAmtGroup", _eRow, "0"));  //组基本金额  

            double _eFBasePrice = Double.Parse(this.CZ_GetRowValue_DF("FBasePrice", _eRow, "0"));      //基本单价
            double _eFQty = Double.Parse(this.CZ_GetRowValue_DF("FQty", _eRow, "0"));                  //FQty
            this.View.Model.SetValue("FBPAmt", _eFBasePrice * _eFQty, _eRow);
            //this.View.UpdateView("FEntity");
        }

        /// <summary>
        /// DataChanged 行 FBasePrice 基本单价 + 行 FQty 数量
        /// </summary>
        /// <param name="e"></param>
        private void Act_DC_BP8Qty(DataChangedEventArgs e)
        {
            //string _eFMtlGroup = this.CZ_GetValue_DF("FMtlGroup", "Id", _eRow, "0");              //
            //string _eFMtlItem = this.CZ_GetValue_DF("FMtlItem", "Id", _eRow, "0");                //
            //string _eFDataGroup = this.CZ_GetValue_DF("FDataGroup", _eRow, "0");                  //
            //double _eFBPAmt = Double.Parse(this.CZ_GetValue_DF("FBPAmt", _eRow, "0"));            //行基本金额
            //double _eFBPAmtGroup = Double.Parse(this.CZ_GetValue_DF("FBPAmtGroup", _eRow, "0"));  //组基本金额  
            int _eRow = e.Row;
            double _eFBasePrice = Double.Parse(this.CZ_GetRowValue_DF("FBasePrice", _eRow, "0"));      //基本单价
            double _eFQty = Double.Parse(this.CZ_GetRowValue_DF("FQty", _eRow, "0"));                  //FQty
            this.View.Model.SetValue("FBPAmt", _eFBasePrice * _eFQty, _eRow);
        }

        /// <summary>
        /// 统合计算 行基本金额》组基本金额 ｜ 总基价     行报价》总报价
        /// 组基本金额=组汇总 行金额          总基价=表汇总 行金额
        /// 总报价=表汇总行报价+表头扣款
        /// </summary>
        private void Act_DC_RndAmount()
        {
            //string _eFMtlGroup = this.CZ_GetValue_DF("FMtlGroup", "Id", _eRow, "0");              //
            //string _eFMtlItem = this.CZ_GetValue_DF("FMtlItem", "Id", _eRow, "0");                //
            //string _eFDataGroup = this.CZ_GetValue_DF("FDataGroup", _eRow, "0");                  //
            //double _eFBPAmt = Double.Parse(this.CZ_GetValue_DF("FBPAmt", _eRow, "0"));            //行基本金额
            //double _eFBPAmtGroup = Double.Parse(this.CZ_GetValue_DF("FBPAmtGroup", _eRow, "0"));  //组基本金额  

            double _FRate = Double.Parse(this.CZ_GetValue_DF("FRate", "0"));
            string _rowFMtlGroup = "";
            string _rowFMtlItem = "";
            string _rowFDataGroup = "";
            double _rowFBPAmt = 0;                                                                  //行基本金额
            double _rowFRptPrice = 0;                                                               //行（组）报价
            double _rowFBPAmtGroup = 0;                                                             //组基本金额  
            int _curGroupIdx = 0;
            int _maxCnt = this.View.Model.GetEntryRowCount("FEntity");
            string _prvFMtlGroup = "";
            string _prvFMtlItem = "";
            string _prvFDataGroup = "";
            double _FAmount = 0;
            double _FAmountRpt = 0;
            double _FRangeAmt = 0;

            for (int i = 0; i < _maxCnt; i++)
            {
                _rowFMtlGroup = this.CZ_GetRowValue_DF("FMtlGroup", "Id", i, "0");
                _rowFMtlItem = this.CZ_GetRowValue_DF("FMtlItem", "Id", i, "0");
                _rowFDataGroup = this.CZ_GetRowValue_DF("FDataGroup", i, "0");
                _rowFBPAmt = Double.Parse(this.CZ_GetRowValue_DF("FBPAmt", i, "0"));

                if (i == 0)
                {
                    _curGroupIdx = i;
                    _rowFBPAmtGroup = _rowFBPAmt;
                    _rowFRptPrice = Double.Parse(this.CZ_GetRowValue_DF("FRptPrice", i, "0"));
                    _prvFMtlGroup = _rowFMtlGroup;
                    _prvFMtlItem = _rowFMtlItem;
                    _prvFDataGroup = _rowFDataGroup;
                    continue;
                }
                else if (_rowFMtlGroup != _prvFMtlGroup || _rowFDataGroup != _prvFDataGroup || _rowFMtlGroup == Val_FMtlGroup_XX)
                {
                    this.View.Model.SetValue("FBPAmtGroup", _rowFBPAmtGroup, _curGroupIdx);
                    this.View.Model.SetValue("FBPAmtCN", _rowFBPAmtGroup * _FRate, _curGroupIdx);
                    _FAmount += _rowFBPAmtGroup;
                    _FAmountRpt += _rowFRptPrice;
                    Act_DC_RndFDownPoints(_curGroupIdx);

                    _curGroupIdx = i;
                    _rowFBPAmtGroup = _rowFBPAmt;
                    _rowFRptPrice = Double.Parse(this.CZ_GetRowValue_DF("FRptPrice", i, "0"));
                    _prvFMtlGroup = _rowFMtlGroup;
                    _prvFMtlItem = _rowFMtlItem;
                    _prvFDataGroup = _rowFDataGroup;
                    continue;
                }
                else
                {
                    _rowFBPAmtGroup += _rowFBPAmt;
                }
            }
            this.View.Model.SetValue("FBPAmtGroup", _rowFBPAmtGroup, _curGroupIdx);
            this.View.Model.SetValue("FBPAmtCN", _rowFBPAmtGroup * _FRate, _curGroupIdx);
            Act_DC_RndFDownPoints(_curGroupIdx);
            _FAmount += _rowFBPAmtGroup;
            _FAmountRpt += _rowFRptPrice;
            _FRangeAmt = Double.Parse(this.CZ_GetValue_DF("FRangeAmt", "0"));
            this.View.Model.SetValue("FAmount", _FAmount);
            this.View.Model.SetValue("FAmtCN", _FAmount * _FRate);
            this.View.Model.SetValue("FAmountRpt", _FAmountRpt + _FRangeAmt);
            this.View.Model.SetValue("FAmtRptCN", (_FAmountRpt + _FRangeAmt) * _FRate);
        }

        /// <summary>
        /// 扣款金额 
        /// </summary>
        /// <param name="e"></param>
        private void Act_DC_FRangeAmt(DataChangedEventArgs e)
        {
            double _FRate = Double.Parse(this.CZ_GetValue_DF("FRate", "0"));
            double _oldVal = Double.Parse(e.OldValue.ToString());
            double _newVal = Double.Parse(e.NewValue.ToString());
            double _FAmountRpt = Double.Parse(this.CZ_GetValue_DF("FAmountRpt", "0"));
            double _FAmountRpt2 = _FAmountRpt + _newVal - _oldVal;
            this.View.Model.SetValue("FAmountRpt", _FAmountRpt2);
            this.View.Model.SetValue("FAmtRptCN", _FAmountRpt2 * _FRate);
        }

        /// <summary>
        /// FBtnAnzRpt-拆分报价     本体行｜独立行 报价=（总报价-扣款）*权重     权重=组基本金额/总基价
        /// </summary>
        private void Act_BC_AnzFRptPrice()
        {
            double _FRate = Double.Parse(this.CZ_GetValue_DF("FRate", "0"));
            int _maxCnt = this.View.Model.GetEntryRowCount("FEntity");
            if (_maxCnt == 0)
            {
                return;
            }

            Lock_ChangeVal = "Act_BC_AnzFRptPrice";

            double _FAmount = Double.Parse(this.CZ_GetValue_DF("FAmount", "0"));
            double _FAmountRpt = Double.Parse(this.CZ_GetValue_DF("FAmountRpt", "0"));
            double _FRangeAmt = Double.Parse(this.CZ_GetValue_DF("FRangeAmt", "0"));
            double _RndAmountRpt = _FAmountRpt - _FRangeAmt;

            double _RndBaseRate = _FAmount == 0 ? 0 : _RndAmountRpt / _FAmount;
            string _rowFMtlGroup = "";
            string _rowFMtlItem = "";
            string _rowFDataGroup = "";
            double _rowFBPAmtGroup = 0;
            double _rowFRptPrice = 0;

            for (int i = 0; i < _maxCnt; i++)
            {
                _rowFMtlGroup = this.CZ_GetRowValue_DF("FMtlGroup", "Id", i, "0");
                _rowFMtlItem = this.CZ_GetRowValue_DF("FMtlItem", "Id", i, "0");
                _rowFDataGroup = this.CZ_GetRowValue_DF("FDataGroup", i, "0");
                _rowFBPAmtGroup = Double.Parse(this.CZ_GetRowValue_DF("FBPAmtGroup", i, "0"));
                //_rowFRptPrice = Double.Parse(this.CZ_GetRowValue_DF("FRptPrice", i, "0"));
                if (_rowFMtlGroup == Val_FMtlGroup_XX || _rowFMtlItem == Val_FMtlItem_Base)
                {
                    _rowFRptPrice = _rowFBPAmtGroup * _RndBaseRate;
                    this.View.Model.SetValue("FRptPrice", _rowFRptPrice, i);
                    this.View.Model.SetValue("FRptPrcCN", _rowFRptPrice * _FRate, i);
                    Act_DC_RndFDownPoints(i);
                }
            }

            Lock_ChangeVal = "";
        }

        /// <summary>
        /// DataChanged 行 FRptPrice 报价
        /// </summary>
        /// <param name="e"></param>
        private void Act_DC_FRptPrice(DataChangedEventArgs e)
        {
            double _FRate = Double.Parse(this.CZ_GetValue_DF("FRate", "0"));
            //判定 是否本体行｜独立行
            string _eFMtlGroup = this.CZ_GetRowValue_DF("FMtlGroup", "Id", e.Row, "0");              //
            string _eFMtlItem = this.CZ_GetRowValue_DF("FMtlItem", "Id", e.Row, "0");                //
            string _eFDataGroup = this.CZ_GetRowValue_DF("FDataGroup", e.Row, "0");
            double _oldVal = Double.Parse(e.OldValue.ToString());
            double _newVal = Double.Parse(e.NewValue.ToString());

            if (_eFMtlGroup == Val_FMtlGroup_XX || _eFMtlItem == Val_FMtlItem_Base)
            {
                double _FAmountRpt = Double.Parse(this.CZ_GetValue_DF("FAmountRpt", "0"));
                double _FAmountRpt2 = _FAmountRpt + _newVal - _oldVal;
                this.View.Model.SetValue("FRptPrcCN", _newVal * _FRate, e.Row);
                this.View.Model.SetValue("FAmountRpt", _FAmountRpt2);
                this.View.Model.SetValue("FAmtRptCN", _FAmountRpt2 * _FRate);
                Act_DC_RndFDownPoints(e.Row);
            }
            else
            {
                //e.NewValue = 0;
                this.View.Model.SetValue("FRptPrice", 0, e.Row);
            }
        }

        /// <summary>
        /// 值更新前验证 表体行 报价
        /// </summary>
        /// <param name="e"></param>
        private void Act_BUV_FRptPrice(BeforeUpdateValueEventArgs e)
        {
            //判定 是否本体行｜独立行
            string _eFMtlGroup = this.CZ_GetRowValue_DF("FMtlGroup", "Id", e.Row, "0");              //
            string _eFMtlItem = this.CZ_GetRowValue_DF("FMtlItem", "Id", e.Row, "0");                //
            string _eFDataGroup = this.CZ_GetRowValue_DF("FDataGroup", e.Row, "0");

            if (_eFMtlGroup != Val_FMtlGroup_XX && _eFMtlItem != Val_FMtlItem_Base)
            {
                e.Cancel = true;
            }
        }

        /// <summary>
        /// 下浮点数 计算 指定行 =(组基本金额-报价)*100/组基本金额，只算组1行
        /// /报价?组基本金额?
        /// </summary>
        /// <param name="_eRow"></param>
        private void Act_DC_RndFDownPoints(int _eRow)
        {
            double _rowFBPAmtGroup = Double.Parse(this.CZ_GetRowValue_DF("FBPAmtGroup", _eRow, "0"));
            double _rowFRptPrice = Double.Parse(this.CZ_GetRowValue_DF("FRptPrice", _eRow, "0"));
            //double _FDownPoints = _rowFRptPrice == 0 ? 0 : (_rowFBPAmtGroup - _rowFRptPrice) * 100 / _rowFRptPrice;
            double _FDownPoints = _rowFBPAmtGroup == 0 ? 0 : (_rowFBPAmtGroup - _rowFRptPrice) * 100 / _rowFBPAmtGroup;
            this.View.Model.SetValue("FDownPoints", _FDownPoints, _eRow);
        }

        /// <summary>
        /// 本体行｜独立行 组基本金额｜报价 变更后 的计算 NULL 
        /// </summary>
        private void Act_Rnd_RowInfo()
        {

        }
        #endregion

        /// <summary>
        /// 值更新前验证 BPR表体行 分组号
        /// </summary>
        /// <param name="e"></param>
        private void Act_BUV_FBDataGroup(BeforeUpdateValueEventArgs e)
        {
            string _eVal = e.Value.ToString();
            int _eValInt = 0;
            Int32.TryParse(_eVal.Split('.')[0].ToString(), out _eValInt);
            string _eOldVal = this.CZ_GetRowValue_DF(e.Key, e.Row, "0");
            Act_EntityFilter_GetBPR();
            if (!AL_EntryBPRFilter.Contains("0"))
            {
                AL_EntryBPRFilter.Add("0");
            }

            if (!AL_EntryBPRFilter.Contains(_eValInt.ToString()))
            {
                this.View.ShowErrMessage("基价计算 行" + (e.Row + 1).ToString() + " 无效的分组号");
                e.Cancel = true;
            }
        }

        #region DataChanged => BPR基价计算 行 DataChangedEvent
        /// <summary>
        /// 基价计算-数量
        /// </summary>
        /// <param name="e"></param>
        private void Act_DC_BPR_FBQty(DataChangedEventArgs e)
        {
            Act_DC_BPR_RndFBAmt(e.Row);
        }

        /// <summary>
        /// 基价计算-单价
        /// </summary>
        /// <param name="e"></param>
        private void Act_DC_BPR_FBPrice(DataChangedEventArgs e)
        {
            Act_DC_BPR_RndFBAmt(e.Row);
        }

        /// <summary>
        /// 基价计算 总价=数量*单价
        /// </summary>
        /// <param name="_row"></param>
        private void Act_DC_BPR_RndFBAmt(int _row)
        {
            string _FBQty = this.CZ_GetRowValue_DF("FBQty", _row, "0");
            string _FBPrice = this.CZ_GetRowValue_DF("FBPrice", _row, "0");
            double _FBAmt = Double.Parse(_FBQty) * Double.Parse(_FBPrice);
            this.View.Model.SetValue("FBAmt", _FBAmt.ToString(), _row);
        }

        /// <summary>
        /// 基价计算-总价
        /// </summary>
        /// <param name="e"></param>
        private void Act_DC_BPR_FBAmt(DataChangedEventArgs e)
        {
            string _FBDataGroup = this.CZ_GetRowValue_DF("FBDataGroup", e.Row, "0");
            int _maxCnt = this.View.Model.GetEntryRowCount(Str_EntryKey_BPR);
            int _groupTopIdx = -1;       //指定组号第一行 索引号
            string _rowFBDG = "";
            string _rowFBAmt = "0";     //行总价
            double _FBGpAmtB = 0;       //分组总价
            for (int i = 0; i < _maxCnt; i++)
            {
                _rowFBDG = this.CZ_GetRowValue_DF("FBDataGroup", i, "0");
                if (_rowFBDG != _FBDataGroup)
                {
                    continue;
                }

                if (_rowFBDG == _FBDataGroup && _groupTopIdx == -1)
                {
                    _groupTopIdx = i;
                }

                _rowFBAmt = this.CZ_GetRowValue_DF("FBAmt", i, "0");
                _FBGpAmtB = _FBGpAmtB + Double.Parse(_rowFBAmt);
            }

            this.View.Model.SetValue("FBGpAmtB", _FBGpAmtB.ToString(), _groupTopIdx);
        }

        /// <summary>
        /// 组 总价合计
        /// </summary>
        /// <param name="e"></param>
        private void Act_DC_BPR_FBGpAmtB(DataChangedEventArgs e)
        {
            //string _FBGpAmtB = this.CZ_GetRowValue_DF("FBGpAmtB", e.Row, "0");
            //string _FBCostRate = this.CZ_GetRowValue_DF("FBCostRate", e.Row, "0");
            //double _FBCost = Double.Parse(_FBGpAmtB) * Double.Parse(_FBCostRate) / 100;
            //this.View.Model.SetValue("FBCost", _FBCost.ToString(), e.Row);
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
            double _FBGpAmtB = Double.Parse(this.CZ_GetRowValue_DF("FBGpAmtB", _row, "0"));
            double _FBCostRate = Double.Parse(this.CZ_GetRowValue_DF("FBCostRate", _row, "0")) / 100;
            double _FBCost = _FBGpAmtB * _FBCostRate;
            this.View.Model.SetValue("FBCost", _FBCost.ToString(), _row);
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
            //double _FBGpAmtB = Double.Parse(this.CZ_GetRowValue_DF("FBGpAmtB", e.Row, "0"));
            //double _FBCost = Double.Parse(this.CZ_GetRowValue_DF("FBCost", e.Row, "0"));
            //double _FBGPRate = Double.Parse(this.CZ_GetRowValue_DF("FBGPRate", e.Row, "0")) / 100;
            ////double _FBGP = (Double.Parse(_FBGpAmtB) + _FBCost) / (1 - _FBGPRate/ 100);
            //double _FBGP = (_FBGpAmtB + _FBCost) * _FBGPRate / (1 - _FBGPRate);
            //this.View.Model.SetValue("FBGP", _FBGP.ToString(), e.Row);
            Act_DC_BPR_RndFBGP(e.Row);
        }

        /// <summary>
        /// 基价计算-计算毛利
        /// </summary>
        /// <param name="_row"></param>
        private void Act_DC_BPR_RndFBGP(int _row)
        {
            double _FBGpAmtB = Double.Parse(this.CZ_GetRowValue_DF("FBGpAmtB", _row, "0"));
            double _FBCost = Double.Parse(this.CZ_GetRowValue_DF("FBCost", _row, "0"));
            double _FBGPRate = Double.Parse(this.CZ_GetRowValue_DF("FBGPRate", _row, "0")) / 100;

            //double _FBGP = (Double.Parse(_FBGpAmtB) + _FBCost) / (1 - _FBGPRate/ 100);
            double _FBGP = (_FBGpAmtB + _FBCost) * _FBGPRate / (1 - _FBGPRate);
            this.View.Model.SetValue("FBGP", _FBGP.ToString(), _row);
        }

        /// <summary>
        /// 基价计算-毛利
        /// </summary>
        /// <param name="e"></param>
        private void Act_DC_BPR_FBGP(DataChangedEventArgs e)
        {
            double _FBGpAmtB = Double.Parse(this.CZ_GetRowValue_DF("FBGpAmtB", e.Row, "0"));
            double _FBCost = Double.Parse(this.CZ_GetRowValue_DF("FBCost", e.Row, "0"));
            double _FBGP = Double.Parse(this.CZ_GetRowValue_DF("FBGP", e.Row, "0"));
            double _FBGpAmt = _FBGpAmtB + _FBCost + _FBGP;
            this.View.Model.SetValue("FBGpAmt", _FBGpAmt.ToString(), e.Row);

            double _FRate = Double.Parse(this.CZ_GetValue_DF("FRate", "0"));
            double _FBGpAmtCN = _FBGpAmt * _FRate;
            this.View.Model.SetValue("FBGpAmtCN", _FBGpAmtCN.ToString(), e.Row);
        }

        /// <summary>
        /// BPR 分组号 DC代码实测没有实际效果 已注销使用
        /// </summary>
        /// <param name="e"></param>
        private void Act_DC_FBDataGroup(DataChangedEventArgs e)
        {
            string _newVal = e.NewValue == null ? "0" : e.NewValue.ToString();
            string _oldVal = e.OldValue == null ? "0" : e.OldValue.ToString();

            Act_EntityFilter_GetBPR();
            if (!AL_EntryBPRFilter.Contains("0"))
            {
                AL_EntryBPRFilter.Add("0");
            }

            if (!AL_EntryBPRFilter.Contains(_newVal))
            {
                this.View.Model.SetValue("FBDataGroup", 0, e.Row);
                //this.View.ShowErrMessage("基价计算单 行" + (e.Row + 1).ToString() + " 无效的分组号");
            }

            string _chVal = this.CZ_GetRowValue("FBDataGroup", e.Row).ToString();
        }
        #endregion

        /// <summary>
        /// BPR-按钮-更新明细基价 tbRush2Main AfterBarItemClick
        /// </summary>
        /// <param name="e"></param>
        private void Act_AEBIC_TbRush2Main(AfterBarItemClickEventArgs e)
        {
            Hashtable _htGpMtlBPrc = new Hashtable();
            //double _FRate = Double.Parse(this.CZ_GetValue_DF("FRate", "0"));
            int _maxCntB = this.View.Model.GetEntryRowCount(Str_EntryKey_BPR);
            string _FBIS2W = "1";
            string _FBDataGroup = "-1", _rowFBDG = "";
            double _FBGpAmt = 0;
            for (int i = 0; i < _maxCntB; i++)
            {
                _FBIS2W = this.CZ_GetRowValue_DF("FBIS2W", i, "1");
                if (_FBIS2W == "1")
                {
                    continue;
                }
                _rowFBDG = this.CZ_GetRowValue_DF("FBDataGroup", i, "0");
                if (_rowFBDG != _FBDataGroup)
                {
                    _FBDataGroup = _rowFBDG;
                    _FBGpAmt = Double.Parse(this.CZ_GetRowValue_DF("FBGpAmt", i, "0"));
                    if (!_htGpMtlBPrc.ContainsKey(_rowFBDG))
                    {
                        _htGpMtlBPrc.Add(_rowFBDG, _FBGpAmt);
                    }
                }
            }

            int _maxCntM = this.View.Model.GetEntryRowCount(Str_EntryKey_Main);
            string _FDataGroup = "", _FMtlGroup = "", _FMtlItem = "";
            double _FBasePrice = 0;
            for (int k = 0; k < _maxCntM; k++)
            {
                _FDataGroup = this.CZ_GetRowValue_DF("FDataGroup", k, "-1");
                _FMtlGroup = this.CZ_GetRowValue_DF("FMtlGroup", "Id", k, "0");
                _FMtlItem = this.CZ_GetRowValue_DF("FMtlItem", "Id", k, "0");
                if (_FMtlGroup != Val_FMtlGroup_XX && _FMtlItem == Val_FMtlItem_Base && _htGpMtlBPrc.ContainsKey(_FDataGroup))
                {
                    _FBasePrice = Double.Parse(_htGpMtlBPrc[_FDataGroup].ToString());
                    this.View.Model.SetValue("FBasePrice", _FBasePrice, k);
                }
            }

            this.View.ShowMessage("明细信息-基本单价 已更新");
        }

        /// <summary>
        /// 获取单据所有权（持有）数据 从登录用户 返回主任岗位的 数据链
        /// </summary>
        /// <param name="_FHolder">单据持有人 maybe Holder｜Creator</param>
        private void Act_Hold_GetHoldInfo(string _FHolder)
        {
            //string _FHolder = this.Context.UserId.ToString();
            string _FHoldOrgID = "";
            string _FHoldDeptID = "";
            string _FCrmSN = "";

            string _sql = "exec proc_cztyCrm_GetCrmSN @FUserID='" + _FHolder + "'";
            DataTable _dt = this.CZDB_SearchBase(_sql);
            if (_dt.Rows.Count == 0)
            {
                return;
            }
            //FUserID	FEmpID	FEmpName	FDeptID	FDeptOrg	FLevelCode	FPostID	FLeaderPost
            //157191	157131	郭俊	    156178	156139	    .156178.	157121	1
            _FHoldOrgID = _dt.Rows[0]["FDeptOrg"].ToString();
            _FHoldDeptID = _dt.Rows[0]["FDeptID"].ToString();
            _FCrmSN = _dt.Rows[0]["FLevelCode"].ToString();

            this.View.Model.SetValue("FCrmHdOrgID", _FHoldOrgID);
            this.View.Model.SetValue("FCrmHdDept", _FHoldDeptID);
            this.View.Model.SetValue("FCrmHolder", _FHolder);
            this.View.Model.SetValue("FCrmSN", _FCrmSN);

            int _EHdRowCnt = this.View.Model.GetEntryRowCount("FEntityHD");
            if (_EHdRowCnt == 1)
            {
                this.View.Model.SetValue("FHHdOrgID", _FHoldOrgID, 0);
                this.View.Model.SetValue("FHHdDept", _FHoldDeptID, 0);
                this.View.Model.SetValue("FHHolder", _FHolder, 0);
                this.View.Model.SetValue("FHSN", _FCrmSN, 0);
                this.View.Model.SetValue("FHBegDate", DateTime.Now.ToString("yyyy-MM-dd"), 0);
                this.View.Model.SetValue("FHEndDate", "9999-12-31", 0);
            }
        }

        /// <summary>
        /// 获取单据所有权（持有）数据 取上源单据
        /// </summary>
        private void Act_Hold_GetHold2SrcBill()
        {

        }

        /// <summary>
        /// 获取用户任职岗位所属部门
        /// </summary>
        private void Act_Hold_GetHolderDept()
        {

        }

        /// <summary>
        /// null
        /// </summary>
        private void Act_DataChanged_Entry()
        {

        }

        /// <summary>
        /// 获取最大分组号 
        /// </summary>
        /// <param name="_alGroupLst">已有分组号[项次]</param>
        /// <returns></returns>
        private int Act_GetMaxDataGroup(ArrayList _alGroupLst)
        {
            int _rowGroupNum = 0;   //当前组数值
            int _maxGroupNum = 0;   //最大组数值
            _alGroupLst = new ArrayList();
            int _rowCnt = this.View.Model.GetEntryRowCount("FEntity");

            //string _EntryName = "FEntity";
            //string _FieldName = "FDataGroup";
            //DynamicObjectCollection _doc = (this.View.Model.DataObject[_EntryName] as DynamicObjectCollection);
            //DynamicObject _obj;
            //int _entryRowCnt = 0;   //表体行数
            //_entryRowCnt = _doc.Count;

            for (int i = 0; i < _rowCnt; i++)
            {
                _rowGroupNum = Int32.Parse(this.CZ_GetRowValue_DF("FDataGroup", i, "0"));
                _maxGroupNum = _maxGroupNum > _rowGroupNum ? _maxGroupNum : _rowGroupNum;
                if (!_alGroupLst.Contains(_rowGroupNum))
                {
                    _alGroupLst.Add(_rowGroupNum);
                }
            }
            return _maxGroupNum;
        }

        /// <summary>
        /// queryservice取数方案，通过业务对象来获取数据，推荐使用
        /// </summary>
        /// <returns></returns>
        public DynamicObjectCollection GetQueryDatas()
        {
            QueryBuilderParemeter paramCatalog = new QueryBuilderParemeter()
            {
                FormId = "",//取数的业务对象
                FilterClauseWihtKey = "",//过滤条件，通过业务对象的字段Key拼装过滤条件
                SelectItems = SelectorItemInfo.CreateItems("", "", ""),//要筛选的字段【业务对象的字段Key】，可以多个，如果要取主键，使用主键名
            };

            DynamicObjectCollection dyDatas = Kingdee.BOS.ServiceHelper.QueryServiceHelper.GetDynamicObjectCollection(this.Context, paramCatalog);
            return dyDatas;
        }
        #endregion

        #region Actions CnyRate
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

            //表头 总基价[FAmount]-本币总基价[FAmtCN]  
            double _FAmount = Double.Parse(this.CZ_GetValue_DF("FAmount", "0"));
            this.View.Model.SetValue("FAmtCN", _FAmount * _FRate);
            //表头 总报价[FAmountRpt]-本币总报价[FAmtRptCN]
            double _FAmountRpt = Double.Parse(this.CZ_GetValue_DF("FAmountRpt", "0"));
            this.View.Model.SetValue("FAmtRptCN", _FAmountRpt * _FRate);

            //明细信息 
            int _maxCntM = this.View.Model.GetEntryRowCount(Str_EntryKey_Main);
            string _FMtlGroup = "", _FMtlItem = "";
            double _FBPAmtGroup = 0, _FRptPrice = 0;
            for (int i = 0; i < _maxCntM; i++)
            {
                _FMtlGroup = this.CZ_GetRowValue_DF("FMtlGroup", "Id", i, "0");
                _FMtlItem = this.CZ_GetRowValue_DF("FMtlItem", "Id", i, "0");
                if (_FMtlGroup == Val_FMtlGroup_XX || _FMtlItem == Val_FMtlItem_Base)
                {
                    //组基本金额[FBPAmtGroup]-合计基价CN[FBPAmtCN]
                    _FBPAmtGroup = Double.Parse(this.CZ_GetRowValue_DF("FBPAmtGroup", i, "0"));
                    this.View.Model.SetValue("FBPAmtCN", _FBPAmtGroup * _FRate, i);
                    //报价[FRptPrice]-报价CN[FRptPrcCN]
                    _FRptPrice = Double.Parse(this.CZ_GetRowValue_DF("FRptPrice", i, "0"));
                    this.View.Model.SetValue("FRptPrcCN", _FRptPrice * _FRate, i);
                }
            }

            //基价计算
            int _maxCntB = this.View.Model.GetEntryRowCount(Str_EntryKey_BPR);
            double _FBGpAmt = 0;
            string _FBDataGroup = "-1", _rowFBDG = "";
            for (int k = 0; k < _maxCntB; k++)
            {
                _rowFBDG = this.CZ_GetRowValue_DF("FBDataGroup", k, "0");
                if (_rowFBDG != _FBDataGroup)
                {
                    _FBDataGroup = _rowFBDG;
                    //分组汇总[FBGpAmt]-本币分组汇总[FBGpAmtCN]
                    _FBGpAmt = Double.Parse(this.CZ_GetRowValue_DF("FBGpAmt", k, "0"));
                    this.View.Model.SetValue("FBGpAmtCN", _FBGpAmt * _FRate, k);
                }
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
        #endregion
    }
}
