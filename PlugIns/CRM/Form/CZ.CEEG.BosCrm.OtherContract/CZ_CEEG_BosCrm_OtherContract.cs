using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.ComponentModel;

using Kingdee.BOS;
using Kingdee.BOS.Core.DynamicForm;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.Util;
using Kingdee.BOS.App.Data;
using Kingdee.BOS.Core.Bill.PlugIn;
using Kingdee.BOS.Core.SqlBuilder;
using Kingdee.BOS.Core.Metadata;
using Kingdee.BOS.Core.Bill;
using System.Collections;
using Kingdee.BOS.Core.DynamicForm.PlugIn.ControlModel;

namespace CZ.CEEG.BosCrm.OtherContract
{
    [Description("其他合同报价计算")]
    [HotUpdate]
    public class CZ_CEEG_BosCrm_OtherContract : AbstractBillPlugIn
    {

        #region 明细表体 CRM物料相关基础资料 窗体变量组
        /// <summary>
        /// 删除Entity行的，保留GUID，供关联删除BPR和Mtl
        /// </summary>
        ArrayList Al_EntryDelGuID = new ArrayList();
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
        /// 当前登录用户 岗位名称  例：报价员(用于行隐藏设置)
        /// </summary>
        string Val_FLocalPost = "";
        /// <summary>
        /// 标签页 材料明细 是否可见 0-不可见 1-可见
        /// </summary>
        string Val_SeeCrmMtl = "0";
        #endregion

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

        #region 其他窗体变量 及 预置值
        /// <summary>
        /// 表体 产品大类列表  ID     =FEntity
        /// </summary>
        string Str_EntryKey_Main = "FEntity";
        /// <summary>
        /// 表体 报价明细 ID  =FBEntryID   (BasePriceRound)
        /// </summary>
        string Str_EntryKey_BPR = "FEntityBPR";
        /// <summary>
        /// 表体 报价明细 ID  =FEntryIDM
        /// </summary>
        string Str_EntryKey_Mtl = "FEntityM";
        #endregion

        #region K3 Override
        /// <summary>
        /// 下推数据创建完成
        /// </summary>
        /// <param name="e"></param>
        public override void AfterCreateNewData(EventArgs e)
        {
            base.AfterCreateNewData(e);
            //Act_AfterTransform_GeneSecondaryEntry();
        }

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
            Act_Rate_GetOrgCny();

            if (this.CZ_GetFormStatus() == "Z")
            {
                //当持有为空时，添加持有人为创建人；不为空时，添加销售员
                if (this.CZ_GetValue_DF("FCrmHolder", "Id", "0") == "0")
                {
                    //Act_Hold_GetHoldInfo("157191");
                    Act_Hold_GetHoldInfo(this.Context.UserId.ToString());
                }
                else if (this.CZ_GetValue_DF("FSaler", "Id", "0") == "0")
                {
                    Act_Hold_SetSaler(this.CZ_GetValue_DF("FCrmHolder", "Id", "0"));
                }
                //汇率
                Act_Rate_GetRate(this.CZ_GetValue_DF("FCreateDate", ""));
            }

            //取用户授权 明细信息列表授权 物料分类
            string _FUserID = this.View.Context.UserId.ToString();
            string _sql = "exec proc_cztyCrm_OfferGetMtlGroup @FUserID='" + _FUserID + "'";
            string _crmFilter = "";
            try
            {
                DataTable _dt = this.CZDB_SearchBase(_sql);
                _crmFilter = _dt.Rows[0]["FIDFilter"].ToString();
                Val_FLocalPost = _dt.Rows[0]["FPostName"].ToString();
                Val_SeeCrmMtl = _dt.Rows[0]["F_Ora_SeeCrmMtl"].ToString();
                this.View.Model.SetValue("FLocalFilter", _crmFilter);
            }
            catch (Exception _ex)
            {
                return;
            }

            if (Val_SeeCrmMtl == "0")
            {
                this.View.StyleManager.SetVisible("F_ora_Tab1_P4", null, false);
            }

            if (Val_FLocalPost == "报价员")
            {
                Act_CtrlRow_SetHideMkt(Str_EntryKey_Main, "FMtlGroup", "FIS2W");
                Act_EntityFilter(Str_EntryKey_Main, "FIS2W");
                Act_CtrlRow_SetHideMkt(Str_EntryKey_BPR, "FBMtlGroup", "FBIS2W");
                Act_EntityFilter(Str_EntryKey_BPR, "FBIS2W");
                Act_CtrlRow_SetHideMkt(Str_EntryKey_Mtl, "FMMtlGroup", "FMIS2W");
                Act_EntityFilter(Str_EntryKey_Mtl, "FMIS2W");
            }
            else
            {
                //隐藏 计价计算单 表体菜单按钮 (GetBarItem(EntryKey,BarItemKey)-表体菜单  GetMainBarItem(BarItemKey)-主菜单)
                this.View.GetBarItem(Str_EntryKey_Main, "tbBPRnd").Visible = false;
            }

            Act_BD_LockBPRRowByBomer();
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
        /// 删除行前 记录 删除行的信息 留后续处理（删除BRP表、Mtl表关联行）
        /// </summary>
        /// <param name="e"></param>
        public override void BeforeDeleteRow(BeforeDeleteRowEventArgs e)
        {
            base.BeforeDeleteRow(e);
            if (e.EntityKey == Str_EntryKey_Main)
            {
                //Al_EntryDelGuID = new ArrayList();
                string _rowFGUID = this.CZ_GetRowValue_DF("FGUID", "Id", e.Row, "");
                if (_rowFGUID != "" && !Al_EntryDelGuID.Contains(_rowFGUID))
                {
                    Al_EntryDelGuID.Add(_rowFGUID);
                }
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
                case "FRANGEAMT":       //表头    FRangeAmt 扣款金额
                    //Act_DC_FRangeAmt(e);
                    break;
                case "FAMTRNDRPT":      //表头    FAmtRndRpt E表报价和 
                    Act_DC_FRangeAmt(e);
                    break;
                case "FAMOUNTRPT":      //表头    FAmountRpt 总报价 
                    Act_DC_FAmountRpt(e);
                    break;
                case "FAMOUNT":      //表头    FAmount 总基价 
                    Act_DC_FAmount(e);
                    break;
                case "FCRMHDDEPT":  //FCrmHdDept
                    break;
                //报价明细
                case "FQTY":        //行 FQty 数量
                    //Act_DC_BP8Qty(e);
                    //Act_DC_RndAmount();
                    break;
                case "FBRPTPRICE":   //行 FBRptPrice 报价
                    Act_DC_FBRptPrice(e);
                    Act_DC_FBTaxRate(e.Row);
                    break;
                case "FBRANGEAMTONE":   //单台扣款	FBRangeAmtOne
                    Act_DC_FBRangeAmtOne(e);
                    break;
                case "FBRANGEAMTGP":    //汇总扣款	FBRangeAmtGP
                    Act_DC_FBRangeAmtGP(e);
                    break;
                case "FMATERIALID":    //FMaterialID 物料代码
                    Act_DC_FMaterialID(e);
                    break;
                case "FBTAXRATEID":    //FBTaxRateID 税率ID
                    break;
                case "FBTAXRATE":            //FBTaxRate 税率%
                    Act_DC_FBTaxRate(e.Row);
                    break;
                case "FSALER": //销售员
                    Act_DC_SetSalerInfo(e);
                    break;
                case "FSETTLEORGID": //签单组织 FSettleOrgId
                    //Act_DC_SetSalerInfo(e);
                    Act_DC_AlterCustSellerByOrg(e);
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
                case "FBRANGEAMTONE":    //单台扣款	FBRangeAmtOne
                    string _FBPRndSEQ = this.CZ_GetRowValue_DF("FBPRndSEQ", e.Row, "0");
                    if (_FBPRndSEQ != "1")
                    {
                        e.Cancel = true;
                    }
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
                    //Act_Control_Visable();
                    break;
                case "FBTNANZRPT":
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
                    //Act_EntityFilter();
                    Act_AEBIC_ReSetFilter();
                    break;
                case "TBNEWENTRY":
                    //tbNewEntry        按钮-新增行
                    //Act_EntityFilter(Str_EntryKey_Main, "FIS2W");
                    Act_AEBIC_ReSetFilter();
                    break;
                case "TBINSERtENTRY":
                    //tbInsertEntry     按钮-插入行
                    //Act_EntityFilter(Str_EntryKey_Main, "FIS2W");
                    Act_AEBIC_ReSetFilter();
                    break;
                case "TBDELETEENTRY":
                    //tbDeleteEntry     按钮-删除行
                    Act_AfterEBIC_tbDeleteEntry();
                    //Act_EntityFilter(Str_EntryKey_Main, "FIS2W");
                    Act_AEBIC_ReSetFilter();
                    break;
                case "TBBPRND":
                    Act_AfterEBIC_tbBPRnd(e);
                    //tbBPRnd 打开基价计算单
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
                case "FCUSTNAME":
                    //e.ListFilterParameter.Filter = Act_FilterCustomerBySeller();
                    break;
                default:
                    break;
            }
            base.BeforeF7Select(e);
        }

        /// <summary>
        /// 单据持有事件变更
        /// </summary>
        /// <param name="e"></param>
        public override void AfterDoOperation(AfterDoOperationEventArgs e)
        {
            base.AfterDoOperation(e);
            string _eOpActName = e.Operation.Operation.ToUpperInvariant();

            switch (_eOpActName)
            {
                //case "SAVE": 表单定义的事件都可以在这里执行，需要通过事件的代码[大写]区分不同事件
                //break;
                case "SAVE":
                    //Act_AfterDO_Save();
                    //this.View.Refresh();
                    break;
                default:
                    break;
            }
        }
        #endregion

        #region Actions

        /// <summary>
        /// 由销售员带出其组织及部门
        /// </summary>
        private void Act_DC_SetSalerInfo(DataChangedEventArgs e)
        {
            string _FSaler = e.NewValue == null ? "0" : e.NewValue.ToString();
            string _sql = "EXEC proc_czly_GetOrgDeptBySalemanId @SmId='" + _FSaler + "'";
            var objs = CZDB_GetData(_sql);
            if (objs.Count > 0)
            {
                //string FOrgID = objs[0]["FOrgID"].ToString();
                string FDeptID = objs[0]["FBizDeptID"].ToString();
                this.View.Model.SetValue("FDept", FDeptID);
            }
        }

        /// <summary>
        /// 根据签单组织刷新客户，销售员内码
        /// </summary>
        /// <param name="e"></param>
        private void Act_DC_AlterCustSellerByOrg(DataChangedEventArgs e)
        {
            if (this.Context.ClientType.ToString() == "Mobile")
            {
                return;
            }
            string orgId = e.NewValue.ToString();
            string FSaler = this.View.Model.GetValue("FSaler") == null ? "0" : (this.View.Model.GetValue("FSaler") as DynamicObject)["Id"].ToString();
            string FCustName = this.View.Model.GetValue("FCustName") == null ? "0" : (this.View.Model.GetValue("FCustName") as DynamicObject)["Id"].ToString();
            string sql = string.Format(@"SELECT c.FCUSTID,cl.FNAME FROM T_BD_CUSTOMER c
            INNER JOIN T_BD_CUSTOMER_L cl ON c.FCUSTID=cl.FCUSTID
            WHERE FUSEORGID='{0}' AND cl.FNAME=(SELECT FNAME FROM T_BD_CUSTOMER_L WHERE FCUSTID='{1}')",
            orgId, FCustName);
            var objs = CZDB_GetData(sql);
            //string xx = "";
            if (objs.Count > 0)
            {
                this.View.Model.SetValue("FCustName", objs[0]["FCUSTID"].ToString());
                //xx += objs[0]["FCUSTID"].ToString();
            }
            else
            {
                this.View.Model.SetValue("FCustName", "0");
            }
            sql = string.Format(@"SELECT s.FID,sl.FNAME FROM V_BD_SALESMAN s
            INNER JOIN V_BD_SALESMAN_L sl ON s.FID=sl.FID
            WHERE FBIZORGID='{0}' AND 
            sl.FNAME=(SELECT FNAME FROM V_BD_SALESMAN_L WHERE FID='{1}')",
            orgId, FSaler);
            objs = CZDB_GetData(sql);
            if (objs.Count > 0)
            {
                this.View.Model.SetValue("FSaler", objs[0]["FID"].ToString());
                //xx += "-" + objs[0]["FID"].ToString();
            }
            else
            {
                this.View.Model.SetValue("FSaler", "0");
            }
            //this.View.ShowMessage(xx);
        }

        /// <summary>
        /// 根据Bom员锁定表体行
        /// </summary>
        private void Act_BD_LockBPRRowByBomer()
        {
            if (this.Context.ClientType.ToString() == "Mobile")
            {
                return;
            }
            string userId = this.Context.UserId.ToString();
            string sql = string.Format(@"SELECT emg.F_ORA_CRMMTLGP FROM (SELECT * FROM T_SEC_USER WHERE FUSERID='{0}') u
            INNER JOIN V_bd_ContactObject c ON u.FLINKOBJECT=c.FID
            INNER JOIN T_HR_EMPINFO e ON c.FNUMBER=e.FNUMBER AND F_ORA_ISCRMBOM=1
            INNER JOIN T_HR_EMPINFOCrmMG emg ON emg.FID=e.FID", userId);
            var objs = CZDB_GetData(sql);
            if (objs.Count <= 0)
            {
                return;
            }
            string mtlGroups = "";
            foreach (var obj in objs)
            {
                mtlGroups += obj["F_ORA_CRMMTLGP"].ToString() + ",";
            }
            var entity = this.View.Model.DataObject["FEntityBPR"] as DynamicObjectCollection;
            if (entity.Count > 0)
            {
                string mtlGp;
                for (int i = 0; i < entity.Count; i++)
                {
                    mtlGp = entity[i]["FBMtlGroup"] == null ? "0" : (entity[i]["FBMtlGroup"] as DynamicObject)["Id"].ToString();
                    if (mtlGp != "0" && !mtlGroups.Contains(mtlGp))
                    {
                        this.View.GetFieldEditor("FMaterialID", i).SetEnabled("", false);
                    }
                }
            }
        }

        /// <summary>
        /// 单据下推生成时，根据报价单生成次表体
        /// </summary>
        private void Act_AfterTransform_GeneSecondaryEntry()
        {
            if (this.Context.ClientType.ToString() == "Mobile")
            {
                return;
            }
            string FNicheNo = CZ_GetValue("FNicheID");
            string sql = string.Format("select FID,FCrmHolder from ora_CRM_SaleOffer where FBillNo='{0}'", FNicheNo);
            var objs = CZDB_GetData(sql);
            if (objs.Count > 0)
            {
                string FCrmHolder = objs[0]["FCrmHolder"].ToString();
                sql = string.Format("EXEC proc_czly_GetSalesmanIdByUserId @FUserId='{0}'", FCrmHolder);
                var obj = CZDB_GetData(sql);
                if (obj.Count > 0)
                {
                    this.View.Model.SetValue("FSaler", obj[0]["FSalesmanId"].ToString());
                    this.View.Model.SetValue("FDept", obj[0]["FDeptID"].ToString());
                }
                string fid = objs[0]["FID"].ToString();
                //从报价单获取报价明细表体
                sql = string.Format("select * from ora_CRM_SaleOfferBPR where FID='{0}'", fid);
                objs = CZDB_GetData(sql);
                //生成报价明细表体
                this.View.Model.BatchCreateNewEntryRow("FEntityBPR", objs.Count);
                for (int i = 0; i < objs.Count; i++)
                {
                    this.View.Model.SetValue("FBGUID", GetObjValue(objs[i], "FBGUID"), i);//E表GUID
                    //this.View.Model.SetValue("FBSrcEID", GetObjValue(objs[i], "FBSrcEID"), i);//E表EntryID
                    //this.View.Model.SetValue("FBSrcSEQ", GetObjValue(objs[i], "FBSrcSEQ"), i);//E表SEQ
                    this.View.Model.SetValue("FBPRndSEQ", GetObjValue(objs[i], "FBPRndSEQ"), i);//计算单SEQ
                    this.View.Model.SetItemValueByID("FBMtlGroup", GetObjValue(objs[i], "FBMtlGroup"), i);//产品大类
                    this.View.Model.SetItemValueByID("FBMtlItem", GetObjValue(objs[i], "FBMtlItem"), i);//产品组成
                    this.View.Model.SetValue("FBDescribe", GetObjValue(objs[i], "FBDescribe"), i);//描述
                    this.View.Model.SetValue("FBQty", GetObjValue(objs[i], "FBQty"), i);//数量
                    this.View.Model.SetValue("FBModel", GetObjValue(objs[i], "FBModel"), i);//型号
                    this.View.Model.SetValue("FBIsStandard", GetObjValue(objs[i], "FBIsStandard"), i);//标准
                    this.View.Model.SetValue("FBasePrice", GetObjValue(objs[i], "FBasePrice"), i);//基本单价
                    this.View.Model.SetValue("FBPAmt", GetObjValue(objs[i], "FBPAmt"), i);//行基价金额
                    this.View.Model.SetValue("FBPAmtGroup", GetObjValue(objs[i], "FBPAmtGroup"), i);//产品基价合计
                    this.View.Model.SetValue("FBRptPrice", GetObjValue(objs[i], "FBRptPrice"), i);//报价
                    this.View.Model.SetValue("FBAbaComm", GetObjValue(objs[i], "FBAbaComm"), i);//放弃提成%
                    this.View.Model.SetValue("FBDownPoints", GetObjValue(objs[i], "FBDownPoints"), i);//下浮点数%
                    this.View.Model.SetValue("FBWorkDay", GetObjValue(objs[i], "FBWorkDay"), i);//下浮点数%
                    this.View.Model.SetValue("FBCostAdj", GetObjValue(objs[i], "FBCostAdj"), i);//成本调整
                    this.View.Model.SetValue("FBCAReason", GetObjValue(objs[i], "FBCAReason"), i);//调整原因
                    if (GetObjValue(objs[i], "FBDelivery") != "0001-01-01 00:00:00")
                    {
                        this.View.Model.SetValue("FBDelivery", GetObjValue(objs[i], "FBDelivery"), i);//交期
                    }
                    this.View.Model.SetValue("FBPAmtLc", GetObjValue(objs[i], "FBPAmtLc"), i);//合计基价Lc
                    this.View.Model.SetValue("FBRptPrcLc", GetObjValue(objs[i], "FBRptPrcLc"), i);//报价Lc
                    this.View.Model.SetValue("FBTaxRate", 13, i);//税率
                    double FBRptPrice = double.Parse(GetObjValue(objs[i], "FBRptPrice"));
                    double FBQty = double.Parse(GetObjValue(objs[i], "FBQty"));
                    this.View.Model.SetValue("FBTaxPrice", FBRptPrice / FBQty, i);//含税单价
                    this.View.Model.SetValue("FBNTPrice", FBRptPrice / FBQty / 1.13, i);//单价
                    this.View.Model.SetValue("FBTaxAmt", FBRptPrice - FBRptPrice / 1.13, i);//税额
                    this.View.Model.SetValue("FBNTAmt", FBRptPrice / 1.13, i);//不含税金额
                    this.View.Model.SetValue("FBRangeAmtOne", GetObjValue(objs[i], "FBRangeAmtOne"), i);//单台扣款
                    this.View.Model.SetValue("FBRangeAmtGP", GetObjValue(objs[i], "FBRangeAmtGP"), i);//汇总扣款
                    this.View.Model.SetValue("FBRangeAmtReason", GetObjValue(objs[i], "FBRangeAmtReason"), i);//扣款原因

                }
                //从报价单获取材料明细表体
                sql = string.Format("select * from ora_CRM_SaleOfferMtl where FID='{0}'", fid);
                objs = CZDB_GetData(sql);
                //创建材料明细表体
                this.View.Model.BatchCreateNewEntryRow("FEntityM", objs.Count);
                for (int i = 0; i < objs.Count; i++)
                {
                    this.View.Model.SetValue("FMGUID", GetObjValue(objs[i], "FMGUID"), i);//E表GUID
                    //this.View.Model.SetValue("FMSrcEID", GetObjValue(objs[i], "FMSrcEID"), i);//E表EntryID
                    //this.View.Model.SetValue("FMSrcSEQ", GetObjValue(objs[i], "FMSrcSEQ"), i);//E表SEQ
                    this.View.Model.SetItemValueByID("FMMtlGroup", GetObjValue(objs[i], "FMMtlGroup"), i);//产品大类
                    this.View.Model.SetItemValueByID("FMMtlItem", GetObjValue(objs[i], "FMMtlItem"), i);//产品组成
                    this.View.Model.SetItemValueByID("FMClass", GetObjValue(objs[i], "FMClass"), i);//材料类别
                    this.View.Model.SetItemValueByID("FMMtl", GetObjValue(objs[i], "FMMtl"), i);//材料
                    this.View.Model.SetValue("FMModel", GetObjValue(objs[i], "FMModel"), i);//规格型号
                    this.View.Model.SetItemValueByID("FMQty", GetObjValue(objs[i], "FMQty"), i);//数量
                    this.View.Model.SetValue("FMPrice", GetObjValue(objs[i], "FMPrice"), i);//单价
                    this.View.Model.SetValue("FMGpAmtB", GetObjValue(objs[i], "FMGpAmtB"), i);//材料总金额
                    this.View.Model.SetValue("FMCostRate", GetObjValue(objs[i], "FMCostRate"), i);//费用率%
                    this.View.Model.SetValue("FMCost", GetObjValue(objs[i], "FMCost"), i);//费用
                    this.View.Model.SetValue("FMGPRate", GetObjValue(objs[i], "FMGPRate"), i);//毛利率%
                    this.View.Model.SetValue("FMGP", GetObjValue(objs[i], "FMGP"), i);//毛利
                    this.View.Model.SetValue("FMGpAmt", GetObjValue(objs[i], "FMGpAmt"), i);//合计基价金额
                    this.View.Model.SetValue("FMGpAmtLc", GetObjValue(objs[i], "FMGpAmtLc"), i);//合计基价金额Lc
                    this.View.Model.SetValue("FMIS2W", GetObjValue(objs[i], "FMIS2W"), i);//显示隐藏
                }
                //获取持有记录
                sql = string.Format("select FCrmHdOrgID,FCrmHdDept,FCrmHolder,FCrmSN from ora_CRM_SaleOffer where FID='{0}'", fid);
                objs = CZDB_GetData(sql);
                //创建持有记录表体
                int cnt = this.View.Model.GetEntryRowCount("FEntityHD");
                if (cnt <= 0) this.View.Model.CreateNewEntryRow("FEntityHD");
                if (cnt == 1)
                {
                    this.View.Model.SetItemValueByID("FHHdOrgID", GetObjValue(objs[0], "FCrmHdOrgID"), 0);//持有组织
                    this.View.Model.SetItemValueByID("FHHdDept", GetObjValue(objs[0], "FCrmHdDept"), 0);//持有部门
                    this.View.Model.SetItemValueByID("FHHolder", GetObjValue(objs[0], "FCrmHolder"), 0);//持有人
                    this.View.Model.SetValue("FHSN", GetObjValue(objs[0], "FCrmSN"), 0);//CRM标识码
                    this.View.Model.SetValue("FHBegDate", DateTime.Now.ToString("yyyy-MM-dd"), 0);//生效日期
                    this.View.Model.SetValue("FHEndDate", "9999-12-31", 0);//失效日期
                }
            }
        }

        /// <summary>
        /// 保存后调用
        /// </summary>
        private void Act_AfterDO_Save()
        {
            string _billStatus = this.CZ_GetFormStatus();
            string _FID = this.CZ_GetFormID();
            if (_billStatus == "Z")
            {
                return;
            }

            string _sql = "exec proc_cztyCrm_Contract_AfterSave @FID='" + _FID + "'";
            DataTable _dt = this.CZDB_SearchBase(_sql);
            if (_dt.Rows.Count > 0)
            {
                this.View.Refresh();
            }
        }

        #region Action - 报价相关方法  包含扣款方法

        /// <summary>
        /// 报价表体 值更新事件 单台扣款 FBRangeAmtOne
        /// </summary>
        /// <param name="e"></param>
        private void Act_DC_FBRangeAmtOne(DataChangedEventArgs e)
        {
            double _FBQty = double.Parse(this.CZ_GetRowValue_DF("FBQty", e.Row, "0"));
            double _FBRangeAmtOne = double.Parse(this.CZ_GetRowValue_DF("FBRangeAmtOne", e.Row, "0"));
            this.View.Model.SetValue("FBRangeAmtGP", _FBQty * _FBRangeAmtOne, e.Row);
        }

        /// <summary>
        /// 报价表体 值更新事件 汇总扣款 FBRangeAmtGP
        /// </summary>
        /// <param name="e"></param>
        private void Act_DC_FBRangeAmtGP(DataChangedEventArgs e)
        {
            //double _eValOld = e.OldValue == null ? 0 : double.Parse(e.OldValue.ToString());
            //double _eValNew = e.NewValue == null ? 0 : double.Parse(e.NewValue.ToString());
            //double _FRangeAmt = double.Parse(this.CZ_GetValue_DF("FRangeAmt", "0"));
            //this.View.Model.SetValue("FRangeAmt", _FRangeAmt - _eValOld + _eValNew);
            //Act_DC_RndFBDownPoints(e.Row);

            double _FRangeAmt = 0;
            int _maxIdx = this.View.Model.GetEntryRowCount(Str_EntryKey_BPR);
            for (int i = 0; i < _maxIdx; i++)
            {
                string _FBPRndSEQ = this.CZ_GetRowValue_DF("FBPRndSEQ", i, "0");
                if (_FBPRndSEQ != "1")
                {
                    continue;
                }
                double _FBRangeAmtGP = double.Parse(this.CZ_GetRowValue_DF("FBRangeAmtGP", i, "0").ToString());
                _FRangeAmt += _FBRangeAmtGP;
            }
            this.View.Model.SetValue("FRangeAmt", _FRangeAmt);
            Act_DC_RndFBDownPoints(e.Row, true);
        }

        /// <summary>
        /// 总报价改变
        /// </summary>
        /// <param name="e"></param>
        private void Act_DC_FAmountRpt(DataChangedEventArgs e)
        {
            double _FRate = Double.Parse(this.CZ_GetValue_DF("FRate", "0"));
            double _newValue = e.NewValue == null ? 0 : Double.Parse(e.NewValue.ToString());
            this.View.Model.SetValue("FAmtRptCN", _FRate * _newValue);
        }

        /// <summary>
        /// 总基价改变
        /// </summary>
        /// <param name="e"></param>
        private void Act_DC_FAmount(DataChangedEventArgs e)
        {
            double _FRate = Double.Parse(this.CZ_GetValue_DF("FRate", "0"));
            double _newValue = e.NewValue == null ? 0 : Double.Parse(e.NewValue.ToString());
            this.View.Model.SetValue("FAmtCN", _FRate * _newValue);
        }

        /// <summary>
        /// 扣款金额 报价行合计发生改变
        /// </summary>
        /// <param name="e"></param>
        private void Act_DC_FRangeAmt(DataChangedEventArgs e)
        {
            //double _FRate = Double.Parse(this.CZ_GetValue_DF("FRate", "0"));
            //double _FRangeAmt = Double.Parse(this.CZ_GetValue_DF("FRangeAmt", "0"));
            double _FAmtRndRpt = Double.Parse(this.CZ_GetValue_DF("FAmtRndRpt", "0"));
            //this.View.Model.SetValue("FAmountRpt", _FRangeAmt + _FAmtRndRpt);
            this.View.Model.SetValue("FAmountRpt", _FAmtRndRpt);
            this.View.InvokeFieldUpdateService("FAmountRpt", 0);
        }

        /// <summary>
        /// 行报价变更 更新表头报价行合计 重算下浮
        /// </summary>
        /// <param name="e"></param>
        private void Act_DC_FBRptPrice(DataChangedEventArgs e)
        {
            double _FRate = Double.Parse(this.CZ_GetValue_DF("FRate", "0"));
            double _newValue = e.NewValue == null ? 0 : Double.Parse(e.NewValue.ToString());
            double _oldValue = e.OldValue == null ? 0 : Double.Parse(e.OldValue.ToString());
            double _FAmtRndRpt = Double.Parse(this.CZ_GetValue_DF("FAmtRndRpt", "0"));
            this.View.Model.SetValue("FAmtRndRpt", _FAmtRndRpt - _oldValue + _newValue);

            this.View.Model.SetValue("FBRptPrcLc", _newValue * _FRate, e.Row);
            Act_DC_RndFBDownPoints(e.Row, true);
        }

        /// <summary>
        /// 报价 or 税率发生变化时，计算行的物料价格｜金额信息
        /// </summary>
        /// <param name="_rowIdx">int row</param>
        private void Act_DC_FBTaxRate(int _rowIdx)
        {
            double _rptPrice = Double.Parse(this.CZ_GetRowValue_DF("FBRptPrice", _rowIdx, "0"));        //报价 (含税金额)
            double _taxRate = Double.Parse(this.CZ_GetRowValue_DF("FBTaxRate", _rowIdx, "0")) / 100;    //税率
            double _qty = Double.Parse(this.CZ_GetRowValue_DF("FQty", _rowIdx, "0"));                   //数量
            double _FBTaxPrice = _rptPrice / _qty;              //小数	含税单价=报价/数量
            double _FBNTPrice = _FBTaxPrice / (1 + _taxRate);   //小数	不含税单价=含税单价/(1+税率)
            double _FBTaxAmt = _rptPrice - _rptPrice / (1 + _taxRate);      //小数	税额=报价-报价/(1+税率)
            double _FBNTAmt = _rptPrice - _FBTaxAmt;            //小数	不含税金额=报价-税额

            this.View.Model.SetValue("FBTaxPrice", _FBTaxPrice, _rowIdx);
            this.View.Model.SetValue("FBNTPrice", _FBNTPrice, _rowIdx);
            this.View.Model.SetValue("FBTaxAmt", _FBTaxAmt, _rowIdx);
            this.View.Model.SetValue("FBNTAmt", _FBNTAmt, _rowIdx);
            //this.View.Model.SetValue("FBLkQty", _qty, _rowIdx);     //
        }

        /// <summary>
        /// 分配报价
        /// </summary>
        private void Act_BC_AnzFRptPrice()
        {
            double _FRate = Double.Parse(this.CZ_GetValue_DF("FRate", "0"));
            int _maxCnt = this.View.Model.GetEntryRowCount(Str_EntryKey_BPR);
            if (_maxCnt == 0)
            {
                return;
            }

            Lock_ChangeVal = "Act_BC_AnzFRptPrice";

            double _FAmount = Double.Parse(this.CZ_GetValue_DF("FAmount", "0"));        //总基价
            double _FAmountRpt = Double.Parse(this.CZ_GetValue_DF("FAmountRpt", "0"));  //总报价
            double _FRangeAmt = Double.Parse(this.CZ_GetValue_DF("FRangeAmt", "0"));    //扣款金额
            //double _FAmtRndRpt = _FAmountRpt - _FRangeAmt;                              //分配到行的报价和
            double _FAmtRndRpt = _FAmountRpt;
            this.View.Model.SetValue("FAmtRndRpt", _FAmtRndRpt);

            //计算基数
            string _rowFBMtlItem = "";  //行物料
            double _rowFBRptPrice = 0;  //行报价
            double _rowFBPAmt = 0;      //行基价金额
            double _rowFBPAmtGroup = 0;	//产品基价合计
            double _rndSumPBA = 0;      //用于计算的基价分母合计

            _rndSumPBA = _FAmount;      //基价分母合计=总基价

            #region 已注销代码段
            //for (int i = 0; i < _maxCnt; i++)
            //{
            //    _rowFBMtlItem = this.CZ_GetRowValue_DF("FBMtlItem", "Id", i, "0");
            //    _rowFBRptPrice = Double.Parse(this.CZ_GetRowValue_DF("FBRptPrice", i, "0"));
            //    _rowFBPAmt = Double.Parse(this.CZ_GetRowValue_DF("FBPAmt", i, "0"));
            //    _rowFBPAmtGroup = Double.Parse(this.CZ_GetRowValue_DF("FBPAmtGroup", i, "0"));

            //    if (_rowFBMtlItem != Val_FMtlItem_Base && _rowFBRptPrice == 0)
            //    {
            //        continue;
            //    }
            //    if (_rowFBMtlItem == Val_FMtlItem_Base)
            //    {
            //        _rndSumPBA += _rowFBPAmtGroup;
            //        continue;
            //    }
            //    if (_rowFBMtlItem != Val_FMtlItem_Base && _rowFBRptPrice != 0)
            //    {
            //        _rndSumPBA += _rowFBPAmt;
            //        continue;
            //    }
            //}
            #endregion

            //分配报价  分配至有产品基价合计的行 其他行报价置空
            //double _rndAmtBP = 0;       //用于计算分配的基价分子
            for (int i = 0; i < _maxCnt; i++)
            {
                _rowFBMtlItem = this.CZ_GetRowValue_DF("FBMtlItem", "Id", i, "0");
                _rowFBRptPrice = Double.Parse(this.CZ_GetRowValue_DF("FBRptPrice", i, "0"));
                _rowFBPAmt = Double.Parse(this.CZ_GetRowValue_DF("FBPAmt", i, "0"));
                _rowFBPAmtGroup = Double.Parse(this.CZ_GetRowValue_DF("FBPAmtGroup", i, "0"));

                #region 已注销代码段
                //if (_rowFBMtlItem == Val_FMtlItem_Base || _rowFBRptPrice != 0)
                //{
                //    _rndAmtBP = _rowFBPAmtGroup == 0 ? _rowFBPAmt : _rowFBPAmtGroup;
                //    _rowFBRptPrice = _rndAmtBP * _FAmtRndRpt / _rndSumPBA;
                //    this.View.Model.SetValue("FBRptPrice", _rowFBRptPrice, i);
                //    this.View.Model.SetValue("FBRptPrcLc", _rowFBRptPrice * _FRate, i);

                //    Act_DC_RndFBDownPoints(i);
                //}
                #endregion
                _rowFBRptPrice = _rowFBPAmtGroup > 0 ? (_rowFBPAmtGroup * _FAmtRndRpt / _rndSumPBA) : 0;
                this.View.Model.SetValue("FBRptPrice", _rowFBRptPrice, i);
                this.View.Model.SetValue("FBRptPrcLc", _rowFBRptPrice * _FRate, i);
                Act_DC_RndFBDownPoints(i, false);
                Act_DC_FBTaxRate(i);
            }
            Act_DC_RndFBDownPoints();
            Lock_ChangeVal = "";
        }

        /// <summary>
        /// 计算行 下浮点数%
        /// </summary>
        /// <param name="_rowIdx"></param>
        /// <param name="_doBillRnd">是否启动表单计算</param>
        private void Act_DC_RndFBDownPoints(int _rowIdx, bool _doBillRnd)
        {
            #region 注销的代码
            /* 
            double _FRptPrice = Double.Parse(this.CZ_GetRowValue_DF("FBRptPrice", _rowIdx, "0"));       //报价
            double _FBPAmtGroup = Double.Parse(this.CZ_GetRowValue_DF("FBPAmtGroup", _rowIdx, "0"));    //产品基价合计
            double _FBPAmt = Double.Parse(this.CZ_GetRowValue_DF("FBPAmt", _rowIdx, "0"));              //行基价金额
            double _FBRangeAmtGP = Double.Parse(this.CZ_GetRowValue_DF("FBRangeAmtGP", _rowIdx, "0"));  //行汇总扣款
            double _FRndRowBP = _FBPAmtGroup == 0 ? _FBPAmt : _FBPAmtGroup;
            double _FBDownPoints = 0;
            //下浮点数% (基价-报价)*100/报价，只算组1行	 
            //2020-04 修改公式下浮比例%=（基价-行报价+扣款）*100/基价
            //this.View.Model.SetValue("FBDownPoints", _FRptPrice == 0 ? 0 : (_FRndRowBP - _FRptPrice) * 100 / _FRptPrice, _rowIdx);
            if (_FRptPrice != 0 && _FRndRowBP != 0) //当用于计算的基价 或 报价 存在=0时，设下浮点数=0
            {
                _FBDownPoints = (_FRndRowBP - _FRptPrice + _FBRangeAmtGP) * 100 / _FRndRowBP;
            }
            this.View.Model.SetValue("FBDownPoints", _FBDownPoints, _rowIdx);
            */
            #endregion

            string _eFBGUID = this.CZ_GetRowValue_DF("FBGUID", _rowIdx, "0");
            string _eFBPRndSEQ = this.CZ_GetRowValue_DF("FBPRndSEQ", _rowIdx, "0");
            double _rowFRptPrice = 0;
            int _mainIdx = 0;
            double _FRptPrice = 0;
            int _maxIdx = this.View.Model.GetEntryRowCount(Str_EntryKey_BPR);
            for (int i = 0; i < _maxIdx; i++)
            {
                string _FBGUID = this.CZ_GetRowValue_DF("FBGUID", i, "0");
                if (_eFBGUID != _FBGUID)
                {
                    continue;   //不同GUID的行 跳出
                }
                string _FBPRndSEQ = this.CZ_GetRowValue_DF("FBPRndSEQ", i, "0");
                if (_FBPRndSEQ == "1")
                {
                    _mainIdx = i;   //计算单行号=1 记录主行
                }
                _rowFRptPrice = Double.Parse(this.CZ_GetRowValue_DF("FBRptPrice", i, "0"));
                _FRptPrice += _rowFRptPrice;
            }
            _rowIdx = _mainIdx;
            //double _FRptPrice = Double.Parse(this.CZ_GetRowValue_DF("FBRptPrice", _rowIdx, "0"));     //报价
            double _FBPAmtGroup = Double.Parse(this.CZ_GetRowValue_DF("FBPAmtGroup", _rowIdx, "0"));    //产品基价合计
            double _FBPAmt = Double.Parse(this.CZ_GetRowValue_DF("FBPAmt", _rowIdx, "0"));              //行基价金额
            double _FBRangeAmtGP = Double.Parse(this.CZ_GetRowValue_DF("FBRangeAmtGP", _rowIdx, "0"));  //行汇总扣款
            double _FRndRowBP = _FBPAmtGroup;  //_FBPAmtGroup == 0 ? _FBPAmt : _FBPAmtGroup;
            double _FBDownPoints = 0;
            //下浮点数% (基价-报价)*100/报价，只算组1行	 
            //2020-04 修改公式下浮比例%=（基价-行报价+扣款）*100/基价
            //this.View.Model.SetValue("FBDownPoints", _FRptPrice == 0 ? 0 : (_FRndRowBP - _FRptPrice) * 100 / _FRptPrice, _rowIdx);
            if (_FRptPrice != 0 && _FRndRowBP != 0) //当用于计算的基价 或 报价 存在=0时，设下浮点数=0
            {
                _FBDownPoints = (_FRndRowBP - _FRptPrice + _FBRangeAmtGP) * 100 / _FRndRowBP;
            }
            this.View.Model.SetValue("FBDownPoints", _FBDownPoints, _rowIdx);

            if (_doBillRnd)
            {
                Act_DC_RndFBDownPoints();
            }
        }

        /// <summary>
        /// 计算表头 下浮点数%
        /// </summary>
        private void Act_DC_RndFBDownPoints()
        {
            double _FMaxDnPts = 0;  //最大下浮比
            double _rowDownPoints = 0;  //行下浮比
            int _rowCont = this.View.Model.GetEntryRowCount(Str_EntryKey_BPR);
            for (int i = 0; i < _rowCont; i++)
            {
                _rowDownPoints = double.Parse(this.CZ_GetRowValue_DF("FBDownPoints", i, "0"));
                if (_rowDownPoints == 0)
                {
                    continue;
                }
                _FMaxDnPts = _FMaxDnPts > _rowDownPoints ? _FMaxDnPts : _rowDownPoints;
            }

            double _FAvgDnPts = 0;      //平均下浮
            double _FAmountRpt = double.Parse(this.CZ_GetValue_DF("FAmountRpt", "0"));      //总报价
            double _FAmount = double.Parse(this.CZ_GetValue_DF("FAmount", "0"));            //总基价
            double _FRangeAmt = double.Parse(this.CZ_GetValue_DF("FRangeAmt", "0"));        //扣款金额
            if (_FAmount != 0)
            {
                _FAvgDnPts = (_FAmount - _FAmountRpt + _FRangeAmt) * 100 / _FAmount;
            }

            this.View.Model.SetValue("FMaxDnPts", _FMaxDnPts);
            this.View.Model.SetValue("FAvgDnPts", _FAvgDnPts);
        }

        /// <summary>
        /// 重新合计报价
        /// </summary>
        private void Act_Do_RndSumFRptPrice()
        {
            int _maxCnt = this.View.Model.GetEntryRowCount(Str_EntryKey_BPR);
            double _FAmtRndRpt = 0;		//小数2	累计报价
            double _rowFBRptPrice = 0;
            for (int i = 0; i < _maxCnt; i++)
            {
                _rowFBRptPrice = Double.Parse(this.CZ_GetRowValue_DF("FBRptPrice", i, "0"));
                _FAmtRndRpt += _rowFBRptPrice;
            }
            this.View.Model.SetValue("FAmtRndRpt", _FAmtRndRpt);
        }
        #endregion

        /// <summary>
        /// 报价明细 FMaterialID 物料代码 变更税率ID，税率%,单位
        /// </summary>
        /// <param name="e"></param>
        private void Act_DC_FMaterialID(DataChangedEventArgs e)
        {
            string _FMtlID = e.NewValue == null ? "0" : e.NewValue.ToString();
            if (_FMtlID == "0" || _FMtlID == "")
            {
                return;
            }
            StringBuilder _sb = new StringBuilder();
            _sb.Append("select mb.FMaterialID,isnull(mb.FTaxRateID,0)FTaxRateID,isnull(r.FTaxRate,0)FTaxRate,FBaseUnitID,isnull(b.FID,0)FBomID ");
            _sb.Append("from (select FMaterialID,FTaxRateID,FBaseUnitID from t_BD_MaterialBase where FMaterialID='" + _FMtlID + "')mb left join T_BD_TaxRate r on mb.FTaxRateID=r.FID ");
            _sb.Append("left join(select top 1 FID,FMaterialID from t_Eng_BOM where FMaterialID='165059' and FDocumentStatus='C' and FForbidStatus='A' order by FNumber desc)b on mb.FMaterialID=b.FMaterialID");
            string _sql = _sb.ToString();
            DataTable _dt = this.CZDB_SearchBase(_sql);
            if (_dt != null && _dt.Rows.Count > 0)
            {
                this.View.Model.SetValue("FBTaxRateID", _dt.Rows[0]["FTaxRateID"].ToString(), e.Row);
                this.View.Model.SetValue("FBTaxRate", _dt.Rows[0]["FTaxRate"].ToString(), e.Row);
                this.View.Model.SetValue("FBUnitID", _dt.Rows[0]["FBaseUnitID"].ToString(), e.Row);
                this.View.Model.SetValue("FBBomVsn", _dt.Rows[0]["FBomID"].ToString(), e.Row);
            }
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
            string _FEmpID = "";

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
            _FEmpID = _dt.Rows[0]["FEmpID"].ToString();

            this.View.Model.SetValue("FCrmHdOrgID", _FHoldOrgID);
            this.View.Model.SetValue("FCrmHdDept", _FHoldDeptID);
            this.View.Model.SetValue("FCrmHolder", _FHolder);
            this.View.Model.SetValue("FCrmSN", _FCrmSN);
            this.View.Model.SetValue("FSaler", _FEmpID);

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
        /// 从单据持有人获取销售员 -职员
        /// </summary>
        /// <param name="_FHolder"></param>
        private void Act_Hold_SetSaler(string _FHolder)
        {
            string _FEmpID = "";

            string _sql = "exec proc_cztyCrm_GetCrmSN @FUserID='" + _FHolder + "'";
            DataTable _dt = this.CZDB_SearchBase(_sql);
            if (_dt.Rows.Count == 0)
            {
                return;
            }
            //FUserID	FEmpID	FEmpName	FDeptID	FDeptOrg	FLevelCode	FPostID	FLeaderPost
            //157191	157131	郭俊	    156178	156139	    .156178.	157121	1
            _FEmpID = _dt.Rows[0]["FEmpID"].ToString();
            this.View.Model.SetValue("FSaler", _FEmpID);
            this.View.InvokeFieldUpdateService("FSaler", 0);
        }

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
        /// 设置隐藏
        /// </summary>
        /// <param name="_entity">子表体ID</param>
        /// <param name="_FMtlG_Col">列名 CRM产品大类</param>
        /// <param name="_FHide_Col">列名 判定隐藏列</param>
        private void Act_CtrlRow_SetHideMkt(string _entity, string _FMtlG_Col, string _FHide_Col)
        {
            string _crmFilter = this.CZ_GetValue("FLocalFilter");
            if (_crmFilter == "")
            {
                return;
            }

            ArrayList _alCrmMtlGroup = new ArrayList();
            _alCrmMtlGroup.AddRange(_crmFilter.Split(','));
            //AL_EntryBPRFilter = new ArrayList();
            int _rowCnt = this.View.Model.GetEntryRowCount(_entity);

            string _rowFMtlGroup = "0"; //行 物料分类
            string _rowFIS2W = "0";     //行是否隐藏
            for (int i = 0; i < _rowCnt; i++)
            {
                _rowFMtlGroup = this.CZ_GetRowValue_DF(_FMtlG_Col, "Id", i, "0");
                if (_rowFMtlGroup == "0")
                {
                    _rowFIS2W = "0";
                }
                else
                {
                    _rowFIS2W = _alCrmMtlGroup.Contains(_rowFMtlGroup) ? "0" : "1";
                }
                this.View.Model.SetValue(_FHide_Col, _rowFIS2W, i);
            }
        }

        /// <summary>
        /// FEntity表体 报价员 按授权 CRM-产品分类 过滤 明细信息表体
        /// </summary>
        private void Act_EntityFilter()
        {
            if (Val_FLocalPost != "报价员")
            {
                return;
            }

            string _crmFilter = this.CZ_GetValue("FLocalFilter");
            if (_crmFilter == "")
            {
                return;
            }

            EntryGrid grid = this.View.GetControl<EntryGrid>("FEntity");
            String filter = string.Format("FIS2W=0 or FIS2W=''");
            grid.SetFilterString(filter);
            grid.SetCustomPropertyValue("AllowSorting", false);
            this.View.UpdateView("FEntity");
        }

        /// <summary>
        /// 过滤 明细信息表体
        /// </summary>
        /// <param name="_entity">子表体ID</param>
        /// <param name="_FHide_Col">列名 判定隐藏列</param>
        private void Act_EntityFilter(string _entity, string _FHide_Col)
        {
            if (Val_FLocalPost != "报价员")
            {
                return;
            }

            string _crmFilter = this.CZ_GetValue("FLocalFilter");
            if (_crmFilter == "")
            {
                return;
            }

            EntryGrid grid = this.View.GetControl<EntryGrid>(_entity);
            //String filter = string.Format(_FHide_Col + "=0 or " + _FHide_Col + "=''");
            String filter = "";
            if (this.Context.ClientType.ToString() == "WPF")
            {
                filter = string.Format(" {0}=0 or {1}='' ", _FHide_Col, _FHide_Col);
            }
            else
            {
                filter = _FHide_Col + "='' ";
            }
            grid.SetFilterString(filter);
            grid.SetCustomPropertyValue("AllowSorting", false);
            this.View.UpdateView(_entity);
        }

        /// <summary>
        /// 单据体 刷新后 根据客户端类型 启动隐藏行重置
        /// </summary>
        private void Act_AEBIC_ReSetFilter()
        {
            string ClientType = this.Context.ClientType.ToString();
            if (ClientType == "WPF")
            {
                Act_EntityFilter(Str_EntryKey_Main, "FIS2W");
            }
        }

        /// <summary>
        /// 添加行后 Entity主表体 生成GUID
        /// </summary>
        /// <param name="e"></param>
        private void Act_AfterCNER(CreateNewEntryEventArgs e)
        {
            string _EntryName = e.Entity.DynamicObjectType.ToString();      //引发事件的表体名 FEntity-明细信息 ｜ FEntityBPR-基价计算表体
            string _FEGuID = "";
            if (_EntryName == Str_EntryKey_Main)
            {
                _FEGuID = this.CZ_GetRowValue_DF("FGUID", e.Row, "");
                if (_FEGuID == "")
                {
                    //_FEGuID = new System.Guid().ToString();
                    this.View.Model.SetValue("FGUID", System.Guid.NewGuid().ToString(), e.Row);
                    this.View.Model.SetValue("FIS2W", "0", e.Row);
                }
            }
        }

        /// <summary>
        /// Entity 表体删除行后 检测删除 BRP｜Mtl关联行
        /// </summary>
        private void Act_AfterEBIC_tbDeleteEntry()
        {
            if (Al_EntryDelGuID.Count == 0)
            {
                return;
            }

            //Str_EntryKey_BPR
            //删除 匹配GUID 已删产品明细的报价明细
            int _maxRowB = this.View.Model.GetEntryRowCount(Str_EntryKey_BPR);
            string _FBGUID = "";
            for (int i = _maxRowB - 1; i >= 0; i--)
            {
                _FBGUID = this.CZ_GetRowValue_DF("FBGUID", i, "");
                if (Al_EntryDelGuID.Contains(_FBGUID))
                {
                    this.View.Model.DeleteEntryRow(Str_EntryKey_BPR, i);
                }
            }

            //Str_EntryKey_Mtl
            //删除 匹配GUID 已删产品明细的材料明细
            int _maxRowM = this.View.Model.GetEntryRowCount(Str_EntryKey_Mtl);
            string _FMGUID = "";
            for (int i = _maxRowM - 1; i >= 0; i--)
            {
                _FMGUID = this.CZ_GetRowValue_DF("FMGUID", i, "");
                if (Al_EntryDelGuID.Contains(_FMGUID))
                {
                    this.View.Model.DeleteEntryRow(Str_EntryKey_Mtl, i);
                }
            }

            //重算表体报价汇总
            Act_Do_RndSumFRptPrice();

            //清除待处理GUID集合
            Al_EntryDelGuID = new ArrayList();
        }

        /// <summary>
        /// 打开基价计算单
        /// </summary>
        private void Act_AfterEBIC_tbBPRnd(AfterBarItemClickEventArgs e)
        {
            int _curRow = this.View.Model.GetEntryCurrentRowIndex(this.Str_EntryKey_Main);

            if (_curRow < 0)
            {
                return;
            }

            string _FMtlGroup = this.CZ_GetRowValue_DF("FMtlGroup", "Id", _curRow, "0");
            string _FMtlGroupNo = this.CZ_GetRowValue_DF("FMtlGroup", "Number", _curRow, "0");
            if (_FMtlGroup == "0")
            {
                this.View.ShowMessage("产品大类未指定");
                return;
            }

            //
            string _FBPRndID = this.CZ_GetRowValue_DF("FBPRndID", _curRow, "0");
            string _FBRndNo = this.CZ_GetRowValue_DF("FBRndNo", _curRow, "0");
            string _FSOBillNo = this.CZ_GetValue("FBillNo");
            string _FGUID = this.CZ_GetRowValue_DF("FGUID", _curRow, "");

            //show Form new 
            BillShowParameter _bsp = new BillShowParameter();
            _bsp.FormId = "ora_CRM_BPRnd";
            //_bsp.SyncCallBackAction = true;
            _bsp.OpenStyle.ShowType = Kingdee.BOS.Core.DynamicForm.ShowType.MainNewTabPage;
            if (_FBPRndID == "0")
            {
                _bsp.Status = Kingdee.BOS.Core.Metadata.OperationStatus.ADDNEW;
                _bsp.PKey = "0";
                _bsp.CustomParams.Add("SaleOfferPrm", "Flag=AddNew;FSOBillNo=" + _FSOBillNo + ";FMtlGroup=" + _FMtlGroup + ";FMtlGroupNo=" + _FMtlGroupNo);
            }
            else
            {
                _bsp.Status = Kingdee.BOS.Core.Metadata.OperationStatus.EDIT;
                _bsp.PKey = _FBPRndID;
                _bsp.CustomParams.Add("SaleOfferPrm", "Flag=Edit;FSOBillNo=" + _FSOBillNo + ";FMtlGroup=" + _FMtlGroup + ";FMtlGroupNo=" + _FMtlGroupNo);
            }
            //this.View.LockBill();
            this.View.StyleManager.SetEnabled("F_ora_SpliteContainer1", null, false);
            //打开基价计算单 有返回值则更新 无返回值则返回
            this.View.ShowForm(_bsp, (Kingdee.BOS.Core.DynamicForm.FormResult frt) =>
            {
                this.View.StyleManager.SetEnabled("F_ora_SpliteContainer1", null, true);
                if (frt.ReturnData == null)
                {
                    return;
                }
                string _value = frt.ReturnData.ToString();
                //this.View.ShowMessage(_value);
                this.Act_RushBPRndInfo(_FGUID, _value, _curRow);
                this.Act_Do_RndSumFRptPrice();
            });

        }

        /// <summary>
        /// 弹出基价计算单 且获得返回值 BPRnd - FID 后续处理
        /// </summary>
        /// <param name="_FGUID">当前行FGUID</param>
        /// <param name="_FBPRndID">从基价计算单返回的单据FID</param>
        /// <param name="_curRow">当前行的RowIndex</param>
        private void Act_RushBPRndInfo(string _FGUID, string _FBPRndID, int _curRow)
        {
            //this.View.ShowMessage("FGUID:" + _FGUID + " FBackBPRndID:" + _FBPRndID);
            //重新加载 报价明细
            int _maxRowB = this.View.Model.GetEntryRowCount(Str_EntryKey_BPR);
            string _FBGUID = "";
            for (int i = _maxRowB - 1; i >= 0; i--)
            {
                _FBGUID = this.CZ_GetRowValue_DF("FBGUID", i, "");
                if (_FGUID == _FBGUID)
                {
                    this.View.Model.DeleteEntryRow(Str_EntryKey_BPR, i);
                }
            }
            string _sqlLoadBPR = Act_SQL_GetEntryBPR(_FBPRndID);
            DataTable _dtB = this.CZDB_SearchBase(_sqlLoadBPR);
            if (_dtB.Rows.Count > 0)
            {
                _maxRowB = this.View.Model.GetEntryRowCount(Str_EntryKey_BPR);
                for (int i = 0; i < _dtB.Rows.Count; i++)
                {
                    this.View.Model.CreateNewEntryRow(Str_EntryKey_BPR);   //新增一行
                    this.View.Model.SetValue("FBGUID", _FGUID, _maxRowB + i);
                    this.View.Model.SetValue("FBSrcEID", "0", _maxRowB + i);
                    this.View.Model.SetValue("FBSrcSEQ", "0", _maxRowB + i);
                    this.View.Model.SetValue("FBPRndSEQ", _dtB.Rows[i]["FBPRndSEQ"].ToString(), _maxRowB + i);
                    this.View.Model.SetValue("FBMtlGroup", _dtB.Rows[i]["FBMtlGroup"].ToString(), _maxRowB + i);
                    this.View.Model.SetValue("FBMtlItem", _dtB.Rows[i]["FBMtlItem"].ToString(), _maxRowB + i);
                    this.View.Model.SetValue("FBDescribe", _dtB.Rows[i]["FBDescribe"].ToString(), _maxRowB + i);
                    this.View.Model.SetValue("FBQty", _dtB.Rows[i]["FBQty"].ToString(), _maxRowB + i);
                    this.View.Model.SetValue("FBModel", _dtB.Rows[i]["FBModel"].ToString(), _maxRowB + i);
                    this.View.Model.SetValue("FBIsStandard", _dtB.Rows[i]["FBIsStandard"].ToString(), _maxRowB + i);
                    this.View.Model.SetValue("FBasePrice", _dtB.Rows[i]["FBasePrice"].ToString(), _maxRowB + i);
                    this.View.Model.SetValue("FBPAmt", _dtB.Rows[i]["FBPAmt"].ToString(), _maxRowB + i);
                    //this.View.Model.SetValue("FBPAmtGroup", _dtB.Rows[i]["FBPAmtGroup"].ToString(), _maxRowB + i);
                    this.View.Model.SetValue("FBRptPrice", "0", _maxRowB + i);
                    this.View.Model.SetValue("FBAbaComm", "0", _maxRowB + i);
                    this.View.Model.SetValue("FBDownPoints", "0", _maxRowB + i);
                    this.View.Model.SetValue("FBWorkDay	0", "0", _maxRowB + i);
                    this.View.Model.SetValue("FBCostAdj	0", "0", _maxRowB + i);
                    this.View.Model.SetValue("FBCAReason", "", _maxRowB + i);
                    //this.View.Model.SetValue("FBDelivery", null, _maxRowB + i);
                    //this.View.Model.SetValue("FBPAmtLc", _dtB.Rows[i]["FBPAmtLc"].ToString(), _maxRowB + i);
                    this.View.Model.SetValue("FBRptPrcLc", "0", _maxRowB + i);
                    this.View.Model.SetValue("FBIS2W", "0", _maxRowB + i);
                    if (i == 0)
                    {
                        this.View.Model.SetValue("FBPRndID", _dtB.Rows[i]["FID"].ToString(), _curRow);
                        this.View.Model.SetValue("FBRndNo", _dtB.Rows[i]["FBILLNO"].ToString(), _curRow);
                        this.View.Model.SetValue("FBPAmtGroup", _dtB.Rows[i]["FBPAmtGroup"].ToString(), _maxRowB + i);
                        this.View.Model.SetValue("FBPAmtLc", _dtB.Rows[i]["FBPAmtLc"].ToString(), _maxRowB + i);
                    }
                }
            }

            //重新加载 材料明细
            int _maxRowM = this.View.Model.GetEntryRowCount(Str_EntryKey_Mtl);
            string _FMGUID = "";
            for (int i = _maxRowM - 1; i >= 0; i--)
            {
                _FMGUID = this.CZ_GetRowValue_DF("FMGUID", i, "");
                if (_FGUID == _FMGUID)
                {
                    this.View.Model.DeleteEntryRow(Str_EntryKey_Mtl, i);
                }
            }
            string _sqlLoadMtl = Act_SQL_GetEntryMtl(_FBPRndID);
            DataTable _dtM = this.CZDB_SearchBase(_sqlLoadMtl);
            if (_dtM.Rows.Count > 0)
            {
                _maxRowM = this.View.Model.GetEntryRowCount(Str_EntryKey_Mtl);
                for (int i = 0; i < _dtM.Rows.Count; i++)
                {
                    this.View.Model.CreateNewEntryRow(Str_EntryKey_Mtl);   //新增一行
                    this.View.Model.SetValue("FMGUID", _FGUID, _maxRowM + i);
                    this.View.Model.SetValue("FMSrcEID", "0", _maxRowM + i);
                    this.View.Model.SetValue("FMSrcSEQ", "0", _maxRowM + i);
                    this.View.Model.SetValue("FMMtlGroup", _dtM.Rows[i]["FMMtlGroup"].ToString(), _maxRowM + i);
                    this.View.Model.SetValue("FMMtlItem", _dtM.Rows[i]["FMMtlItem"].ToString(), _maxRowM + i);
                    this.View.Model.SetValue("FMClass", _dtM.Rows[i]["FMClass"].ToString(), _maxRowM + i);
                    this.View.Model.SetValue("FMMtl", _dtM.Rows[i]["FMMtl"].ToString(), _maxRowM + i);
                    this.View.Model.SetValue("FMModel", _dtM.Rows[i]["FMModel"].ToString(), _maxRowM + i);
                    this.View.Model.SetValue("FMQty", _dtM.Rows[i]["FMQty"].ToString(), _maxRowM + i);
                    this.View.Model.SetValue("FMUnit", _dtM.Rows[i]["FMUnit"].ToString(), _maxRowM + i);
                    this.View.Model.SetValue("FMPrice", _dtM.Rows[i]["FMPrice"].ToString(), _maxRowM + i);
                    this.View.Model.SetValue("FMAmt", _dtM.Rows[i]["FMAmt"].ToString(), _maxRowM + i);
                    this.View.Model.SetValue("FMGpAmtB", _dtM.Rows[i]["FMGpAmtB"].ToString(), _maxRowM + i);
                    this.View.Model.SetValue("FMCostRate", _dtM.Rows[i]["FMCostRate"].ToString(), _maxRowM + i);
                    this.View.Model.SetValue("FMCost", _dtM.Rows[i]["FMCost"].ToString(), _maxRowM + i);
                    this.View.Model.SetValue("FMGPRate", _dtM.Rows[i]["FMGPRate"].ToString(), _maxRowM + i);
                    this.View.Model.SetValue("FMGP", _dtM.Rows[i]["FMGP"].ToString(), _maxRowM + i);
                    this.View.Model.SetValue("FMGpAmt", _dtM.Rows[i]["FMGpAmt"].ToString(), _maxRowM + i);
                    this.View.Model.SetValue("FMGpAmtLc", _dtM.Rows[i]["FMGpAmtLc"].ToString(), _maxRowM + i);
                    this.View.Model.SetValue("FMIS2W", "0", _maxRowM + i);
                }
            }
        }

        /// <summary>
        /// 从基价计算单获取基价 用于填充报价表体（产品构成）
        /// </summary>
        /// <param name="_FBPRndID">基价计算单ID</param>
        /// <returns></returns>
        private string Act_SQL_GetEntryBPR(string _FBPRndID)
        {
            StringBuilder _sb = new StringBuilder();
            _sb.Append("select b.FID,b.FBILLNO,be.FSEQ FBPRndSEQ,be.FSEQ FBPRndSEQ,b.FMtlGroup FBMtlGroup,be.FMtlItem FBMtlItem,");
            _sb.Append("be.FDescribe FBDescribe,be.FQty FBQty,be.FModel FBModel,be.FIsStandard FBIsStandard,be.FBasePrice FBasePrice,");
            _sb.Append("be.FRowAmt FBPAmt,b.FSumAmt FBPAmtGroup,b.FSumAmtLc FBPAmtLc ");
            _sb.Append("from(select * from ora_CRM_BPRnd where FID='" + _FBPRndID + "')b inner join ora_CRM_BPRndEntry be on b.FID=be.FID");
            return _sb.ToString();
        }

        /// <summary>
        /// 从基价计算单获取 本体 材料构成
        /// </summary>
        /// <param name="_FBPRndID">基价计算单ID</param>
        /// <returns></returns>
        private string Act_SQL_GetEntryMtl(string _FBPRndID)
        {
            StringBuilder _sb = new StringBuilder();
            _sb.Append("select b.FMtlGroup FMMtlGroup,be.FMtlItem FMMtlItem,beb.FBClass FMClass,beb.FBMtl FMMtl,beb.FBModel FMModel,");
            _sb.Append("beb.FBQty FMQty,beb.FBUnit FMUnit,beb.FBPrice FMPrice,beb.FBAmt FMAmt,beb.FBGpAmtB FMGpAmtB,beb.FBCostRate FMCostRate,");
            _sb.Append("beb.FBCost FMCost,beb.FBGPRate FMGPRate,beb.FBGP FMGP,beb.FBGpAmt FMGpAmt,beb.FBGpAmtLc FMGpAmtLc ");
            _sb.Append("from(select * from ora_CRM_BPRnd where FID='" + _FBPRndID + "')b ");
            _sb.Append("inner join ora_CRM_BPRndEntry be on b.FID=be.FID and be.FMTLITEM=" + Val_FMtlItem_Base + " ");
            _sb.Append("inner join ora_CRM_BPRndEntryB beb on b.FID=beb.FID");
            return _sb.ToString();
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

            //表头 总基价[FAmount]-本币总基价[FAmtCN]  
            double _FAmount = Double.Parse(this.CZ_GetValue_DF("FAmount", "0"));
            this.View.Model.SetValue("FAmtCN", _FAmount * _FRate);
            //表头 总报价[FAmountRpt]-本币总报价[FAmtRptCN]
            double _FAmountRpt = Double.Parse(this.CZ_GetValue_DF("FAmountRpt", "0"));
            this.View.Model.SetValue("FAmtRptCN", _FAmountRpt * _FRate);

            //报价表体
            int _maxCntB = this.View.Model.GetEntryRowCount(Str_EntryKey_BPR);
            double _FBPAmt = 0;
            double _FBRptPrice = 0;
            for (int k = 0; k < _maxCntB; k++)
            {
                _FBPAmt = Double.Parse(this.CZ_GetRowValue_DF("FBPAmt", k, "0"));
                _FBRptPrice = Double.Parse(this.CZ_GetRowValue_DF("FBRptPrice", k, "0"));
                this.View.Model.SetValue("FBPAmtLc", _FBPAmt * _FRate, k);
                this.View.Model.SetValue("FBRptPrcLc", _FBRptPrice * _FRate, k);
            }

            //材料表体
            int _maxCntM = this.View.Model.GetEntryRowCount(Str_EntryKey_Mtl);
            double _FMGpAmt = 0;
            for (int k = 0; k < _maxCntM; k++)
            {
                _FMGpAmt = Double.Parse(this.CZ_GetRowValue_DF("FMGpAmt", k, "0"));
                this.View.Model.SetValue("FMGpAmtLc", _FMGpAmt * _FRate, k);
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
        /// 数据库查询基本方法
        /// </summary>
        /// <param name="sql"></param>
        /// <returns></returns>
        private DynamicObjectCollection CZDB_GetData(string sql)
        {
            DynamicObjectCollection objs;
            try
            {
                objs = DBUtils.ExecuteDynamicObject(this.Context, sql);
            }
            catch (Exception _ex)
            {
                return null;
                throw _ex;
            }
            return objs;
        }
        /// <summary>
        /// 获取动态对象的值（字符串）
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="sign"></param>
        /// <returns></returns>
        private string GetObjValue(DynamicObject obj, string sign)
        {
            return obj[sign] == null ? "" : obj[sign].ToString();
        }

        /// <summary>
        /// 获取动态对象的ID（基础资料）
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="sign"></param>
        /// <returns></returns>
        private string GetObjID(DynamicObject obj, string sign)
        {
            return obj[sign] == null ? "0" : (obj[sign] as DynamicObject)["Id"].ToString();
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
