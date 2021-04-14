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
using Kingdee.BOS.Core.Bill;
using Kingdee.BOS.Core.DynamicForm;

namespace CZ.CEEG.BosCrm.BPRnd
{
    /// <summary>
    /// BOS_Crm 基价计算单
    /// </summary>
    [Description("BOS_Crm 基价计算单")]
    [Kingdee.BOS.Util.HotUpdate]
    public class CZ_CEEG_BosCrm_BPRnd : AbstractBillPlugIn 
    {
        /// <summary>
        /// CRM组件（附件）-主体=123328
        /// </summary>
        public string Val_FMtlItem_Base { get; set; } = "本体";
        //public string Val_FMtlItem_Base { get; set; } = "123328";

        string Val_OpenPrm = "";

        /// <summary>
        /// 设置是否更新父单据 Is Return To Parent Window
        /// </summary>
        bool Val_IsReturn2PW = false;

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
        public override void AfterButtonClick(AfterButtonClickEventArgs e)
        {
            base.AfterButtonClick(e);
            string key = e.Key.ToUpperInvariant();
            switch(key)
            {
                case "FSAVEASSCHEME": //FSaveAsScheme -另存为方案
                    Act_SaveAsScheme();
                    break;
            }
        }

        /// <summary>
        /// 初始化，对其他界面传来的参数进行处理，对控件某些属性进行处理
        /// 这里不宜对数据DataModel进行处理
        /// </summary>
        /// <param name="e"></param>
        public override void OnInitialize(InitializeEventArgs e)
        {
            if (this.View.OpenParameter.GetCustomParameter("SaleOfferPrm") != null)
            {
                Val_OpenPrm = this.View.OpenParameter.GetCustomParameter("SaleOfferPrm").ToString();
                //this.View.ShowMessage(_prm);
            }
        }

        /// <summary>
        /// 数据加载完毕
        /// </summary>
        /// <param name="e"></param>
        public override void AfterBindData(EventArgs e)
        {
            this.View.Model.SetValue("FSaleOfferPrm", Val_OpenPrm);
            Act_ABD_IsSaleOfferOpen(Val_OpenPrm);

            Act_Rate_GetOrgCny();
            Act_Grid_NoSort("FEntity");
            Act_Grid_NoSort("FEntityB");
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
        /// 行删除事件
        /// </summary>
        /// <param name="e"></param>
        public override void AfterDeleteRow(AfterDeleteRowEventArgs e)
        {
            base.AfterDeleteRow(e);
            string _eEntryKey = e.EntityKey;
            if (_eEntryKey == "FEntityB")
            {
                Act_DC_BPR_FBAmt(null);
            }
        }

        /// <summary>
        /// DataChanged
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
                //表单控件 材料组成
                case "FBQTY":
                    //FBQty 数量
                    Act_DC_BPR_FBQty(e);
                    break;
                case "FBPRICE":
                    //FBPrice 单价
                    Act_DC_BPR_FBPrice(e);
                    break;
                case "FBAMT":
                    //FBAmt     基价计算-总价
                    Act_DC_BPR_FBAmt(e);
                    break;
                case "FBGPAMTB":
                    //FBGpAmtB  基价计算-材料总金额
                    Act_DC_BPR_FBGpAmtB(e);
                    Act_DcDo_RndFGpAmt(e.Row);
                    break;
                case "FBCOSTRATE":
                    //FBCostRate    基价计算-费用率%
                    Act_DC_BPR_FBCostRate(e);
                    break;
                case "FBCOST":
                    //FBCost        基价计算-费用
                    Act_DC_BPR_FBCost(e);
                    Act_DcDo_RndFGpAmt(e.Row);
                    break;
                case "FBGPRATE":
                    //FBGPRate      基价计算-毛利率%
                    Act_DC_BPR_FBGPRate(e);
                    break;
                case "FBGP":
                    //FBGP  毛利
                    Act_DC_BPR_FBGP(e);
                    break;
                //产品组成
                //case "FQTY":
                //    //FQty  数量  在BOS设置
                //    break;
                //case "FBASEPRICE":
                //    //FBasePrice    基本单价 在BOS设置
                //    break;
                //表头 基本信息 
                case "FSUMAMT":
                    //FSumAmt   合计基价
                    Act_DC_FSumAmt(e);
                    break;
                case "FSCHEME":
                    //FScheme   基础资料-基价方案库
                    Act_DC_FScheme(e);
                    break;
                case "FMTLITEM": //产品组成
                    Act_LockBasePriceByMtlItem(e);
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

            switch (_key)
            {
                case "FBCOSTRATE":  //材料组成 FBCostRate   费用率%
                    Act_BUV_ChkEnBIs1st(e);
                    break;
                case "FBGPRATE":    //材料组成 FBGPRate     毛利率%
                    Act_BUV_ChkEnBIs1st(e);
                    break;
                default:
                    break;
            }
            base.BeforeUpdateValue(e);
        }

        public override void BeforeF7Select(BeforeF7SelectEventArgs e)
        {
            string _key = e.FieldKey.ToUpperInvariant();
            switch(_key)
            {
                case "FSCHEME":     //基价方案
                    Act_FilterScheme(e);
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
                case "TBUPPRICE":
                    //tbUpPrice       BPR-按钮-更新本体单价
                    Act_AEBIC_TbUpPrice(e);
                    break;
                default:
                    break;
            }
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
                case "TBRLSCHEME":
                    //tbRLScheme    更新材料组成
                    Act_AEBIC_TbRLScheme(e);
                    break;
                case "TBBACKBPRND":
                    //tbBackBPRnd   返回数据
                    Act_AEBIC_TbBackBPRnd(e);
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

        #region Actions
        /// <summary>
        /// 另存为基价方案
        /// </summary>
        private void Act_SaveAsScheme()
        {
            string FDocumentStatus = CZ_GetValue("FDocumentStatus");
            if(FDocumentStatus == "Z")
            {
                this.View.ShowMessage("请保存后再进行操作！");
                return;
            }
            string FID = CZ_GetFormID();
            var para = new BillShowParameter();
            para.FormId = "ora_CrmBD_BPScheme";
            para.OpenStyle.ShowType = ShowType.Modal;
            para.ParentPageId = this.View.PageId;
            para.PageId = Guid.NewGuid().ToString();
            para.Status = OperationStatus.ADDNEW;
            para.CustomParams.Add("FSrcID", FID);

            this.View.ShowForm(para);

        }

        /// <summary>
        /// 产品组成为本体时，锁定对应行的基价
        /// </summary>
        private void Act_LockBasePriceByMtlItem(DataChangedEventArgs e)
        {
            string FMtlItem = this.View.Model.GetValue("FMtlItem", e.Row) == null ? "0" : 
                (this.View.Model.GetValue("FMtlItem", e.Row) as DynamicObject)["Name"].ToString();
            if(FMtlItem == "本体")
            {
                this.View.Model.SetValue("FBasePrice", 0, e.Row);
                this.View.GetFieldEditor("FBasePrice", e.Row).SetEnabled("", false);
            }
            else
            {
                this.View.GetFieldEditor("FBasePrice", e.Row).SetEnabled("", true);
            }
        }

        /// <summary>
        /// 过滤基价方案
        /// </summary>
        private void Act_FilterScheme(BeforeF7SelectEventArgs e)
        {
            string _UserId = this.Context.UserId.ToString();
            string _FMtlGroup = this.View.Model.GetValue("FMtlGroup") == null ? "0" : (this.View.Model.GetValue("FMtlGroup") as DynamicObject)["Id"].ToString();
            
            //string filter = " FID in (";
            //if(_FMtlGroup != "0")
            //{
            //    string sql = string.Format("select FID from ora_CrmBD_BPScheme where FCreatorId='{0}' and FMtlGroup='{1}'", _UserId, _FMtlGroup);
            //    var objs = CZDB_GetData(sql);

            //    for (int i = 0; i < objs.Count; i++)
            //    {
            //        filter += "'" + objs[i]["FID"].ToString() + "'";
            //        if (i < objs.Count - 1) filter += ",";
            //    }
            //    if (objs.Count <= 0)
            //    {
            //        filter += "'0'";
            //    }
            //}
            //else
            //{
            //    filter += "'0'";
            //}
            
            //filter += ")";

            string filter = string.Format(" FCreatorId='{0}' and FMtlGroup='{1}'", _UserId, _FMtlGroup);
            e.ListFilterParameter.Filter = filter;
        }

        /// <summary>
        /// 判定是报价单传入 格式正确 且为新增时 解析单号
        /// </summary>
        /// <param name="_Val_OpenPrm"></param>
        private void Act_ABD_IsSaleOfferOpen(string _Val_OpenPrm)
        {
            if (_Val_OpenPrm == null || _Val_OpenPrm == "")
            {
                return;
            }
            //Flag=AddNew;FSOBillNo=CX000001;FMtlGroup=156199;FMtlGroupNo=000001
            Hashtable _htObj = Cz_Rnd_Str2Ht(_Val_OpenPrm, ';', '=');
            if (_htObj["Flag"] == null || _htObj["FSOBillNo"] == null || _htObj["FMtlGroupNo"] == null || _htObj["Flag"].ToString() != "AddNew")
            {
                return;
            }

            string _newTag = _htObj["FSOBillNo"].ToString() + _htObj["FMtlGroupNo"].ToString();
            /*
            select dbo.fun_BosRnd_addSpace(convert(varchar,convert(int, 
            replace(max(FBILLNO),'CX000001000001',''))+1),'CX000001000001','',3) FBillNo 
            from ora_CRM_BPRnd where FBILLNO like('CX000001000001%')
            exec proc_cztyCrm_GetBPRndNo @FTag=CX000001000001
            */

            string _sql = "exec proc_cztyCrm_GetBPRndNo @FTag='" + _newTag + "'";
            //this.View.ShowMessage(_sql);
            try
            {
                DataTable _dt = DBUtils.ExecuteDataSet(this.Context, _sql).Tables[0];
                if (_dt.Rows.Count > 0)
                {
                    this.View.Model.SetValue("FBillNo", _dt.Rows[0]["FBillNo"].ToString());
                }
            }
            catch (Exception _ex)
            {
                this.View.ShowMessage(_ex.Message);
            }
            this.View.Model.SetValue("FMtlGroup", _htObj["FMtlGroup"].ToString());
            this.View.Model.SetValue("FSOBillNo", _htObj["FSOBillNo"].ToString());
        }

        /// <summary>
        /// BPR-按钮-更新本体单价
        /// </summary>
        /// <param name="e"></param>
        private void Act_AEBIC_TbUpPrice(AfterBarItemClickEventArgs e)
        {
            string _BillStatus = this.CZ_GetFormStatus();
            if (_BillStatus == "C" || _BillStatus == "B")
            {
                this.View.ShowMessage("单据已提交或审核 不能执行");
                return;
            }
            int _maxRow = this.View.Model.GetEntryRowCount("FEntity");
            int _maxRowB = this.View.Model.GetEntryRowCount("FEntityB");
            if (_maxRow == 0 || _maxRowB == 0)
            {
                this.View.ShowErrMessage("产品组成 或 材料组成 没有行", "错误：产品组成 或 材料组成 没有行");
                return;
            }

            double _FBGpAmt = Double.Parse(this.CZ_GetRowValue_DF("FBGpAmt", 0, "0"));
            string _rowFMtlItem = "";
            bool _isUpdateErr = true;
            for (int i = 0; i < _maxRow; i++)
            {
                _rowFMtlItem = this.CZ_GetRowValue_DF("FMtlItem", "Name", i, "");
                if (_rowFMtlItem == Val_FMtlItem_Base)
                {
                    this.View.Model.SetValue("FBasePrice", _FBGpAmt, i);
                    this.View.InvokeFieldUpdateService("FBasePrice", i);
                    _isUpdateErr = false;
                    break;
                }
            }

            if (_isUpdateErr)
            {
                this.View.ShowErrMessage("产品组成 可能没有本体行", "错误：未成功更新产品组成 本体 基本单价");
            }
            else
            {
                this.View.ShowMessage("产品组成 本体 基本单价 已更新");
            }
        }

        /// <summary>
        /// 值更新前验证 是否第一行 不是则中断
        /// </summary>
        /// <param name="e"></param>
        private void Act_BUV_ChkEnBIs1st(BeforeUpdateValueEventArgs e)
        {
            if (e.Row > 0)
            {
                e.Cancel = true;
            }
        }

        /// <summary>
        /// 添加行后
        /// </summary>
        /// <param name="e"></param>
        private void Act_AfterCNER(CreateNewEntryEventArgs e)
        {
            string _EntryName = e.Entity.DynamicObjectType.ToString();      //引发事件的表体名 FEntity-明细信息 ｜ FEntityB-基价计算表体

            if (_EntryName == "FEntityB")
            {
                if (e.Row > 0)
                {
                    string _curFClass = this.CZ_GetRowValue_DF("FBClass", e.Row - 1, "0");
                    this.View.Model.SetValue("FBClass", _curFClass, e.Row);
                    return;
                }
            }
        }

        /// <summary>
        /// FScheme   基础资料-基价方案库
        /// </summary>
        /// <param name="e"></param>
        private void Act_DC_FScheme(DataChangedEventArgs e)
        {
            string _FSchemeNID = e.NewValue == null ? "0" : e.NewValue.ToString();
            Act_DcDo_FillEntityB(_FSchemeNID);
        }

        /// <summary>
        /// 表单菜单按钮 更新材料组成	tbRLScheme	tb ReadLoad Scheme
        /// </summary>
        /// <param name="e"></param>
        private void Act_AEBIC_TbRLScheme(AfterBarItemClickEventArgs e)
        {
            string _BillStatus = this.CZ_GetFormStatus();
            if (_BillStatus == "C" || _BillStatus == "B")
            {
                this.View.ShowMessage("单据已提交或审核 不能执行");
                return;
            }
            string _FSchemeNID = this.CZ_GetValue_DF("FScheme", "Id", "0");
            Act_DcDo_FillEntityB(_FSchemeNID);
        }

        /// <summary>
        /// 填充 材料组成 
        /// </summary>
        /// <param name="_FSchemeID"></param>
        private void Act_DcDo_FillEntityB(string _FSchemeID)
        {
            if (_FSchemeID == "0")
            {
                return;
            }

            //循环 清除表体行
            int _rowCount = this.View.Model.GetEntryRowCount("FEntityB");
            for (int i = _rowCount - 1; i >= 0; i--)
            {
                this.View.Model.DeleteEntryRow("FEntityB", i);
            }

            //重新加载行  //加载数据 Table
            StringBuilder _sb = new StringBuilder();
            _sb.Append("select b.FID,be.FEntryID,be.FSEQ,be.FClass,be.FMtl,be.FModel,be.FQty,be.FUnit,be.FPrice,be.FAmt, ");
            _sb.Append("b.FGpAmtB,b.FCostRate,b.FCost,b.FGPRate,b.FGP,b.FGpAmt,b.FGPAmtLc ");
            _sb.Append("from (select * from ora_CrmBD_BPScheme where FID='" + _FSchemeID + "') b ");
            _sb.Append("inner join ora_CrmBD_BPSchemeEntry be on b.FID=be.FID order by be.FSEQ ");
            string _sql = _sb.ToString();
            DataTable _objDT = this.CZDB_SearchBase(_sql);
            if(_objDT.Rows.Count==0)
            {
                return;
            }

            try
            {
                for (int i = 0; i < _objDT.Rows.Count; i++)
                {
                    this.View.Model.CreateNewEntryRow("FEntityB");   //新增一行

                    this.View.Model.SetValue("FBClass", _objDT.Rows[i]["FClass"].ToString(), i);
                    this.View.Model.SetValue("FBMtl", _objDT.Rows[i]["FMtl"].ToString(), i);
                    this.View.Model.SetValue("FBModel", _objDT.Rows[i]["FModel"].ToString(), i);
                    this.View.Model.SetValue("FBQty", _objDT.Rows[i]["FQty"].ToString(), i);
                    this.View.Model.SetValue("FBUnit", _objDT.Rows[i]["FUnit"].ToString(), i);
                    this.View.Model.SetValue("FBPrice", _objDT.Rows[i]["FPrice"].ToString(), i);
                    this.View.Model.SetValue("FBAmt", _objDT.Rows[i]["FAmt"].ToString(), i);

                    if (i == 0)
                    {
                        this.View.Model.SetValue("FBGpAmtB", _objDT.Rows[i]["FGpAmtB"].ToString(), i);
                        this.View.Model.SetValue("FBCostRate", _objDT.Rows[i]["FCostRate"].ToString(), i);
                        this.View.Model.SetValue("FBCost", _objDT.Rows[i]["FCost"].ToString(), i);
                        this.View.Model.SetValue("FBGPRate", _objDT.Rows[i]["FGPRate"].ToString(), i);
                        this.View.Model.SetValue("FBGP", _objDT.Rows[i]["FGP"].ToString(), i);
                        this.View.Model.SetValue("FBGpAmt", _objDT.Rows[i]["FGpAmt"].ToString(), i);
                        this.View.Model.SetValue("FBGpAmtLc", _objDT.Rows[i]["FGPAmtLc"].ToString(), i);
                    }
                }
            }
            catch (Exception _ex)
            {
                this.View.ShowErrMessage(_ex.Message, _ex.TargetSite.ToString());
            }
            this.View.ShowMessage("材料组成 已重新加载");
        }

        /// <summary>
        /// tbBackBPRnd   返回数据
        /// </summary>
        /// <param name="e"></param>
        private void Act_AEBIC_TbBackBPRnd(AfterBarItemClickEventArgs e)
        {
            if (Val_OpenPrm == "")
            {
                return;
            }
            string _billStatus = this.CZ_GetFormStatus();
            if (_billStatus != "C")
            {
                this.View.ShowMessage("请确保单据已保存并审核");
                return;
            }
            string _FBillID = this.CZ_GetFormID();
            //返回值
            Val_IsReturn2PW = true;
            this.View.ReturnToParentWindow(new Kingdee.BOS.Core.DynamicForm.FormResult(_FBillID));
            this.View.Close();
        }

        #region DataChanged => 材料组成 行 DataChangedEvent
        /// <summary>
        /// 材料组成 数量
        /// </summary>
        /// <param name="e"></param>
        private void Act_DC_BPR_FBQty(DataChangedEventArgs e)
        {
            Act_DC_BPR_RndFBAmt(e.Row);
        }

        /// <summary>
        /// 材料组成 单价
        /// </summary>
        /// <param name="e"></param>
        private void Act_DC_BPR_FBPrice(DataChangedEventArgs e)
        {
            Act_DC_BPR_RndFBAmt(e.Row);
        }

        /// <summary>
        /// 材料组成 运算行 行金额=数量*单价
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
        /// 材料组成 行金额    ：方法内 汇总全部行金额 写入0行【材料总金额】
        /// </summary>
        /// <param name="e"></param>
        private void Act_DC_BPR_FBAmt(DataChangedEventArgs e)
        {
            int _maxCnt = this.View.Model.GetEntryRowCount("FEntityB");
            string _rowFBAmt = "0";     //行总价
            double _FBGpAmtB = 0;       //分组总价
            for (int i = 0; i < _maxCnt; i++)
            {
                _rowFBAmt = this.CZ_GetRowValue_DF("FBAmt", i, "0");
                _FBGpAmtB = _FBGpAmtB + Double.Parse(_rowFBAmt);
            }

            this.View.Model.SetValue("FBGpAmtB", _FBGpAmtB.ToString(), 0);
        }

        /// <summary>
        /// 材料组成 材料总金额
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
        /// 材料组成 费用率%
        /// </summary>
        /// <param name="e"></param>
        private void Act_DC_BPR_FBCostRate(DataChangedEventArgs e)
        {
            Act_DC_BPR_RndFBCost(e.Row);
        }

        /// <summary>
        /// 材料组成 运算 费用=材料总金额/(1-费用率%)-材料总金额
        /// </summary>
        /// <param name="_row"></param>
        private void Act_DC_BPR_RndFBCost(int _row)
        {
            double _FBGpAmtB = Double.Parse(this.CZ_GetRowValue_DF("FBGpAmtB", _row, "0"));
            double _FBCostRate = Double.Parse(this.CZ_GetRowValue_DF("FBCostRate", _row, "0")) / 100;
            double _FBCost = _FBGpAmtB / (1 - _FBCostRate) - _FBGpAmtB;
            this.View.Model.SetValue("FBCost", _FBCost.ToString(), _row);
        }

        /// <summary>
        /// 材料组成 费用
        /// </summary>
        /// <param name="e"></param>
        private void Act_DC_BPR_FBCost(DataChangedEventArgs e)
        {
            Act_DC_BPR_RndFBGP(e.Row);
        }

        /// <summary>
        /// 材料组成 毛利率%
        /// </summary>
        /// <param name="e"></param>
        private void Act_DC_BPR_FBGPRate(DataChangedEventArgs e)
        {
            Act_DC_BPR_RndFBGP(e.Row);
        }

        /// <summary>
        /// 材料组成 运算 计算毛利
        /// </summary>
        /// <param name="_row"></param>
        private void Act_DC_BPR_RndFBGP(int _row)
        {
            double _FBGpAmtB = Double.Parse(this.CZ_GetRowValue_DF("FBGpAmtB", _row, "0"));
            double _FBCost = Double.Parse(this.CZ_GetRowValue_DF("FBCost", _row, "0"));
            double _FBGPRate = Double.Parse(this.CZ_GetRowValue_DF("FBGPRate", _row, "0")) / 100;

            //double _FBGP = (Double.Parse(_FBGpAmtB) + _FBCost) / (1 - _FBGPRate/ 100);
            //double _FBGP = (_FBGpAmtB + _FBCost) * _FBGPRate / (1 - _FBGPRate);
            double _FBGP = (_FBGpAmtB + _FBCost) /(1 - _FBGPRate) - (_FBGpAmtB + _FBCost);
            this.View.Model.SetValue("FBGP", _FBGP.ToString(), _row);
        }

        /// <summary>
        /// 材料组成 毛利
        /// </summary>
        /// <param name="e"></param>
        private void Act_DC_BPR_FBGP(DataChangedEventArgs e)
        {
            //double _FBGpAmtB = Double.Parse(this.CZ_GetRowValue_DF("FBGpAmtB", e.Row, "0"));
            //double _FBCost = Double.Parse(this.CZ_GetRowValue_DF("FBCost", e.Row, "0"));
            //double _FBGP = Double.Parse(this.CZ_GetRowValue_DF("FBGP", e.Row, "0"));
            //double _FBGpAmt = _FBGpAmtB + _FBCost + _FBGP;
            //this.View.Model.SetValue("FBGpAmt", _FBGpAmt.ToString(), e.Row);

            //double _FRate = Double.Parse(this.CZ_GetValue_DF("FRate", "0"));
            //double _FBGpAmtLc = _FBGpAmt * _FRate;
            //this.View.Model.SetValue("FBGpAmtLc", _FBGpAmtLc.ToString(), e.Row);

            Act_DcDo_RndFGpAmt(e.Row);
        }

        /// <summary>
        /// 材料组成 运算 计算  合计基价金额=材料总金额+费用+毛利
        /// </summary>
        /// <param name="_row"></param>
        private void Act_DcDo_RndFGpAmt(int _row)
        {
            double _FBGpAmtB = Double.Parse(this.CZ_GetRowValue_DF("FBGpAmtB", _row, "0"));
            double _FBCost = Double.Parse(this.CZ_GetRowValue_DF("FBCost", _row, "0"));
            double _FBGP = Double.Parse(this.CZ_GetRowValue_DF("FBGP", _row, "0"));
            double _FBGPRate = Double.Parse(this.CZ_GetRowValue_DF("FBGPRate", _row, "0")) / 100;
            //double _FBGpAmt = _FBGpAmtB + _FBCost + _FBGP;
            double _FBGpAmt = (_FBGpAmtB + _FBCost) / (1 - _FBGPRate);
            this.View.Model.SetValue("FBGpAmt", _FBGpAmt.ToString(), _row);

            double _FRate = Double.Parse(this.CZ_GetValue_DF("FRate", "0"));
            double _FBGpAmtLc = _FBGpAmt * _FRate;
            this.View.Model.SetValue("FBGpAmtLc", _FBGpAmtLc.ToString(), _row);
        }

        #endregion

        /// <summary>
        /// FSumAmt 合计基价
        /// </summary>
        /// <param name="e"></param>
        private void Act_DC_FSumAmt(DataChangedEventArgs e)
        {
            double _FSumAmt = Double.Parse(this.CZ_GetValue_DF("FSumAmt", "0"));
            double _FRate = Double.Parse(this.CZ_GetValue_DF(Mkt_FRate, "0"));
            this.View.Model.SetValue("FSumAmtLc", _FSumAmt * _FRate);
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
            double _FSumAmt = Double.Parse(this.CZ_GetValue_DF("FSumAmt", "0"));
            this.View.Model.SetValue("FSumAmtLc", _FSumAmt * _FRate);

            //明细信息 材料组成 只算第一行
            int _maxRowB = this.View.Model.GetEntryRowCount("FEntityB");
            double _FBGpAmt = 0;
            if (_maxRowB > 0)
            {
                _FBGpAmt = Double.Parse(this.CZ_GetRowValue_DF("FSumAmt", 0, "0"));
                this.View.Model.SetValue("FBGpAmtLc", _FBGpAmt * _FRate, 0);
            }
            //for (int i = 0; i < _maxRowB; i++)
            //{
            //    _FBGpAmt = Double.Parse(this.CZ_GetRowValue_DF("FSumAmt", i, "0"));
            //    this.View.Model.SetValue("FBGpAmtLc", _FBGpAmt * _FRate, i);
            //}
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
            string _item="";
            foreach(object o in _alObj)
            {
                _item=o.ToString();
                _htObj.Add(_item.Split(_itemKey)[0], _item.Split(_itemKey)[1]);
            }
            return _htObj;
        }
        #endregion
    }
}
