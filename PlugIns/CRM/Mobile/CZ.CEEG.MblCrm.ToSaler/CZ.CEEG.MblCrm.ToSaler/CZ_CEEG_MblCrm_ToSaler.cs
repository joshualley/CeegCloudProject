using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Data;
using System.Collections;
using System.Web;

using Kingdee.BOS;
using Kingdee.BOS.Core.DynamicForm;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.Util;
using Kingdee.BOS.App.Data;

using Kingdee.BOS.Mobile;
using Kingdee.BOS.Mobile.Metadata;
using Kingdee.BOS.Mobile.Metadata.ControlDataEntity;
using Kingdee.BOS.Mobile.PlugIn;
using Kingdee.BOS.Mobile.PlugIn.ControlModel;
using Kingdee.BOS.FileServer.Core;
using Kingdee.BOS.FileServer.Core.Object;
using Kingdee.BOS.FileServer.ProxyService;
using Kingdee.BOS.ServiceHelper;
using Kingdee.BOS.Core.Metadata;
using Kingdee.BOS.Core;

using Kingdee.BOS.Core.DynamicForm.PlugIn.ControlModel;
using Kingdee.BOS.JSON;
using Kingdee.BOS.Mobile.PlugIn.Args;
using System.Threading;
using Kingdee.BOS.Workflow.ServiceHelper;
using Kingdee.BOS.Workflow.Models.Chart;

namespace CZ.CEEG.MblCrm.ToSaler
{
    [HotUpdate]
    [Description("移动报价")]
    public class CZ_CEEG_MblCrm_ToSaler : AbstractMobileBillPlugin
    {
        #region 页面属性组 CRM算SN号
        /// <summary>
        /// 持有人 任职 组织
        /// </summary>
        ArrayList AL_HoldOrg = new ArrayList();
        /// <summary>
        /// Key:任职组织ID  Val:对应任职部门ID集合
        /// </summary>
        Hashtable HT_HoldOrg2ALDept = new Hashtable();
        /// <summary>
        /// 选中组织下 部门集合
        /// </summary>
        ArrayList AL_HoldDept = new ArrayList();
        /// <summary>
        /// Key:部门ID Val:部门递归码  ,L1-DeptID,L2-DeptID,...,Local-DeptID,
        /// </summary>
        Hashtable HT_HoldDeptCrmSN = new Hashtable();
        /// <summary>
        /// Kingdee.BOS.ClientType this.Context.ClientType.ToString()	[]	
        /// WPF     PC端 云之家客户端
        /// Html    PC端 浏览器
        /// Mobile  移动端｜摸拟打开移动端
        /// </summary>
        string ClientType = "";
        #endregion

        #region override 方法
        public override void AfterBindData(EventArgs e)
        {
            base.AfterBindData(e);
            //bool _FIsBid = (bool)this.View.BillModel.GetValue("FIsBid");
            EntryEditEnable();
            initView();
            //暂时隐藏行详情按钮
            this.View.GetControl("FRowBtn").Visible = false;
            if(this.CZ_GetValue("FDocumentStatus") != "Z")
            {
                //利用保存生成组号
                //IOperationResult saveResult = BusinessDataServiceHelper.Save(
                //    this.Context,
                //    this.View.BillView.BillBusinessInfo,
                //    this.View.BillModel.DataObject
                //);
            }
            HideBtn();
        }

        public override void BeforeDoOperation(BeforeDoOperationEventArgs e)
        {
            base.BeforeDoOperation(e);
            string op = e.Operation.FormOperation.Operation.ToUpperInvariant();
            switch (op)
            {
                case "SAVE":
                    if (!Act_Validate())
                    {
                        e.Cancel = true;
                    }
                    break;
                case "SUBMIT":
                    if (!Act_Validate())
                    {
                        e.Cancel = true;
                    }
                    break;
            }
        }


        public override void AfterDoOperation(AfterDoOperationEventArgs e)
        {
            base.AfterDoOperation(e);
            string op = e.Operation.Operation.ToUpperInvariant();
            switch (op)
            {
                case "SAVE":
                    //如果是下推生成的单据，则建立起它们的关联关系
                    CreateBillRelation();
                    break;
                case "SUBMIT":
                    //如果是下推生成的单据，则建立起它们的关联关系
                    CreateBillRelation();
                    //Jump2Audit();
                    break;
            }
        }

        public override void AfterButtonClick(AfterButtonClickEventArgs e)
        {
            base.AfterButtonClick(e);
            string key = e.Key.ToUpperInvariant();
            switch (key)
            {
                case "FPUSH":
                    if(CZ_GetValue("FDocumentStatus") != "C")
                    {
                        this.View.ShowMessage("报价未通过审核！");
                        return;
                    }
                    PushContact();
                    break;
            }
        }

        public override void DataChanged(DataChangedEventArgs e)
        {
            base.DataChanged(e);
            string key = e.Field.Key.ToUpperInvariant();
            switch (key)
            {
                case "FRPTPRICEMB": //报价
                    if (!IsMainBodyRptPrice(e.Row))
                    {
                        this.View.BillModel.SetValue("FBDownPoints", 0, e.Row);
                    }
                    Act_CalRowDownPoints(e.Row);
                    Act_CalDownPoints1();
                    break;
                case "FISBID": //FIsBid
                    Act_IsHideBidDate();
                    break;
                case "FBEANGEAMTONE": //FBRangeAmtOne  单台扣款
                    break;
            }
        }
        /// <summary>
        /// 报价明细当前选中行
        /// </summary>
        private int currSelectedRow = 0;

        public override void EntityRowDoubleClick(EntityRowClickEventArgs e)
        {
            base.EntityRowDoubleClick(e);
            currSelectedRow = e.Row - 1;
            OpenDetailedPage();
            //this.View.GetControl("FBJRow").SetCustomPropertyValue("forecolor", "#ffff99");
        }

        public override void ButtonClick(ButtonClickEventArgs e)
        {
            base.ButtonClick(e);
            string key = e.Key.ToUpperInvariant();
            switch (key)
            {
                case "FDETAILED":
                    //OpenDetailedPage()
                    break;
            }
        }

        #endregion

        #region Actions
        /// <summary>
        /// 根据是否招投标，显示隐藏开标时间
        /// </summary>
        private void Act_IsHideBidDate()
        {
            string FIsBid = this.View.BillModel.GetValue("FIsBid").ToString();
            if (FIsBid == "True")
            {
                this.View.GetControl("FTenderDateFL").Visible = true;
                this.View.GetControl("FBidMakerFL").Visible = true;
            }
            else
            {
                this.View.GetControl("FTenderDateFL").Visible = false;
                this.View.GetControl("FBidMakerFL").Visible = false;
            }
        }
        /// <summary>
        /// 必录校验
        /// </summary>
        /// <returns></returns>
        private bool Act_Validate()
        {
            string FIsBid = this.View.BillModel.GetValue("FIsBid").ToString();
            if(FIsBid == "True")
            {
                string FTenderDate = this.View.BillModel.GetValue("FTenderDate") == null ? "" : this.View.BillModel.GetValue("FTenderDate").ToString();
                var stdate = DateTime.Parse("1900-01-01");
                if(FTenderDate == "" || DateTime.Parse(FTenderDate).CompareTo(stdate) <= 0)
                {
                    this.View.ShowMessage("开标时间为必录字段！");
                    return false;
                }
            }
            string FPayCond = CZ_GetValue("FPayCond");
            if (FPayCond.IsNullOrEmptyOrWhiteSpace())
            {
                this.View.ShowMessage("“付款条件”是必录字段！");
                return false;
            }

            return true;
        }

        /// <summary>
        /// 根据流程显示隐藏按钮
        /// </summary>
        private void HideBtn()
        {
            Act_IsHideBidDate();
            string _FDocumentStatus = this.CZ_GetValue("FDocumentStatus");
            if (_FDocumentStatus == "Z" || _FDocumentStatus == "A")
            {

                var submitBtn = this.View.GetControl("FSubmitBtn");
                var saveBtn = this.View.GetControl("FSaveBtn");
                var pushBtn = this.View.GetControl("FPUSH");
                saveBtn.Visible = false;
                pushBtn.Visible = false;
                submitBtn.SetCustomPropertyValue("width", 310);
                //初始隐藏标书制作员
                this.View.GetControl("FBidMakerFL").Visible = false;

            }
            else if (_FDocumentStatus == "B")
            {
                var submitBtn = this.View.GetControl("FSubmitBtn");
                var saveBtn = this.View.GetControl("FSaveBtn");
                var pushBtn = this.View.GetControl("FPUSH");
                submitBtn.Visible = false;
                pushBtn.Visible = false;
                saveBtn.SetCustomPropertyValue("width", 310);
                this.View.GetControl("FBidMakerFL").Visible = true;
            }
            else if (_FDocumentStatus == "C")
            {
                var submitBtn = this.View.GetControl("FSubmitBtn");
                var saveBtn = this.View.GetControl("FSaveBtn");
                var pushBtn = this.View.GetControl("FPUSH");
                submitBtn.Visible = false;
                saveBtn.Visible = false;
                pushBtn.SetCustomPropertyValue("width", 310);
            }
            else if (_FDocumentStatus == "D")
            {
                
                var submitBtn = this.View.GetControl("FSubmitBtn");
                var saveBtn = this.View.GetControl("FSaveBtn");
                var pushBtn = this.View.GetControl("FPUSH");
                
                //初始隐藏标书制作员
                this.View.GetControl("FBidMakerFL").Visible = false;
                string FID = this.View.BillModel.DataObject["Id"].ToString();
                string procInstId = WorkflowChartServiceHelper.GetProcInstIdByBillInst(this.Context, this.View.GetFormId(), FID);
                string sql = "select FSTATUS from t_WF_ProcInst where FPROCINSTID='" + procInstId + "' order by FCREATETIME desc";
                var data = CZDB_GetData(sql);
                // FSTATUS=1 --> 流程终止或撤销
                if (data.Count > 0 && (data[0]["FSTATUS"].ToString() == "1"))
                {
                    saveBtn.Visible = false;
                    pushBtn.Visible = false;
                    submitBtn.SetCustomPropertyValue("width", 310);
                }
                // FSTATUS=2、4 --> 打回发起人
                else
                {
                    submitBtn.Visible = false;
                    pushBtn.Visible = false;
                    saveBtn.SetCustomPropertyValue("width", 310);
                }
            }

        }

        /// <summary>
        /// 是否主体报价
        /// </summary>
        private bool IsMainBodyRptPrice(int i)
        {
            string _FBMtlItem = this.View.BillModel.GetValue("FBMtlItem", i) == null ? "" : (this.View.BillModel.GetValue("FBMtlItem", i) as DynamicObject)["Id"].ToString();
            if (_FBMtlItem == "123328")
                return true;

            return false;
        }

        /// <summary>
        /// 计算表头总下浮点数（平均），行最大下浮点数，总下浮
        /// </summary>
        private void Act_CalDownPoints()
        {
            int count = this.View.BillModel.GetEntryRowCount("FEntityBPR");
            //string _FBMtlItem;
            float _FBDownPoints;
            float max = float.MinValue;
            float avg = 0;
            int cnt = 0;
            for (int i = 0; i < count; i++)
            {
                if(IsMainBodyRptPrice(i)) //本体
                {
                    _FBDownPoints = float.Parse(this.View.BillModel.GetValue("FBDownPoints", i).ToString());
                    if (cnt == 0) max = _FBDownPoints;
                    cnt++;
                    avg += _FBDownPoints;
                    
                    if (_FBDownPoints > max)
                    {
                        max = _FBDownPoints;
                    }
                }
            }
            avg = avg / cnt;
            this.View.BillModel.SetValue("FMaxDnPts", max);
            this.View.BillModel.SetValue("FAvgDnPts", avg);

        }

        private void Act_CalDownPoints1()
        {
            int count = this.View.BillModel.GetEntryRowCount("FEntity");
            float _FBDownPoints;
            float max = float.MinValue;
            float avg = 0;
            int cnt = 0;
            for (int i = 0; i < count; i++)
            {
                string guid = this.View.BillModel.GetValue("FGUID", i).ToString();
                int row = GetMainBodyRowInGroup(guid);
                if(row != -1)
                {
                    _FBDownPoints = float.Parse(this.View.BillModel.GetValue("FBDownPoints", row).ToString());
                    if (cnt == 0) max = _FBDownPoints;
                    cnt++;
                    avg += _FBDownPoints;

                    if (_FBDownPoints > max)
                    {
                        max = _FBDownPoints;
                    }
                }
            }
            avg = avg / cnt;
            this.View.BillModel.SetValue("FMaxDnPts", max);
            this.View.BillModel.SetValue("FAvgDnPts", avg);
        }


        /// <summary>
        /// 计算组报价汇总后的行下浮点数
        /// </summary>
        private void Act_CalRowDownPoints(int Row)
        {
            int count = this.View.BillModel.GetEntryRowCount("FEntityBPR");
            //string _FBMtlItem;
            double _FBRptPrice = 0;
            string _FBGUID = this.View.BillModel.GetValue("FBGUID", Row).ToString();
            string guid = "";
           
            for (int i = 0; i < count; i++)
            {
                guid = this.View.BillModel.GetValue("FBGUID", i).ToString();
                if (_FBGUID == guid)
                {
                    _FBRptPrice += double.Parse(this.View.BillModel.GetValue("FBRptPrice", i).ToString());
                }
            }

            int mbRow = GetMainBodyRowInGroup(_FBGUID);
            double _FBPAmtGroup = double.Parse(this.View.BillModel.GetValue("FBPAmtGroup", mbRow).ToString());   //组基价
            double _FBRangeAmtGP = double.Parse(this.View.BillModel.GetValue("FBRangeAmtGP", mbRow).ToString()); //组扣款
            double _FBDownPoints = _FBRptPrice == 0 ? 0 : (_FBPAmtGroup - _FBRptPrice + _FBRangeAmtGP) * 100 / _FBPAmtGroup;
            this.View.BillModel.SetValue("FBDownPoints", _FBDownPoints, mbRow);
        }

        /// <summary>
        /// 组内是否存在本体，存在返回本体行号，否则返回组内第一行行号，表体数据不存在返回-1
        /// </summary>
        /// <param name="guid">组ID标识</param>
        /// <returns></returns>
        private int GetMainBodyRowInGroup(string guid)
        {
            var entity = this.View.BillModel.DataObject["ora_CRM_SaleOfferBPR"] as DynamicObjectCollection;
            if(entity.Count <= 0)
            {
                return -1;
            }
            string _FBMtlItem;
            string _FBGUID;
            List<int> rowNums = new List<int>();
            for (int i = 0; i < entity.Count; i++)
            {
                _FBGUID = entity[i]["FBGUID"] == null ? "" : entity[i]["FBGUID"].ToString();
                if(_FBGUID == guid)
                {
                    _FBMtlItem = entity[i]["FBMtlItem"] == null ? "0" : (entity[i]["FBMtlItem"] as DynamicObject)["Id"].ToString();
                    if (_FBMtlItem == "123328") //存在本体
                    {
                        return i;
                    }
                    rowNums.Add(i);
                }
            }
            return rowNums.Min();
        }

        /// <summary>
        /// 打开报价详情页面
        /// </summary>
        private void OpenDetailedPage()
        {
            var entity = this.View.BillModel.DataObject["ora_CRM_SaleOfferBPR"] as DynamicObjectCollection;

            string FMtlGroup = (entity[currSelectedRow]["FBMtlGroup"] as DynamicObject)["Id"].ToString();
            string FMtlItem = (entity[currSelectedRow]["FBMtlItem"] as DynamicObject)["Id"].ToString();
            string FQty = entity[currSelectedRow]["FBQty"].ToString();
            string FPAmt = entity[currSelectedRow]["FBPAmt"].ToString();

            string FBGUID = entity[currSelectedRow]["FBGUID"].ToString();
            int mbRow = GetMainBodyRowInGroup(FBGUID);
            string FPAmtGroup = entity[mbRow]["FBPAmtGroup"].ToString(); //获取组基价汇总，位于本体行上，单独附件位于组内第一行
            string FBRangeAmtGP = entity[mbRow]["FBRangeAmtGP"].ToString(); //组扣款
            double FRptPriceGroup = - double.Parse(entity[currSelectedRow]["FBRptPrice"].ToString()); //累加组内减去本行的报价
            foreach (var row in entity)
            {
                if (row["FBGUID"].ToString() == FBGUID)
                {
                    FRptPriceGroup += double.Parse(row["FBRptPrice"].ToString());
                }
            }

            //string FPAmtGroup = entity[currSelectedRow]["FBPAmtGroup"].ToString();
            string FDescribe = entity[currSelectedRow]["FBDescribe"].ToString();
            string FModel = entity[currSelectedRow]["FBModel"].ToString();
            string FRptPrice = entity[currSelectedRow]["FBRptPrice"].ToString();
            string FDownPoints = entity[currSelectedRow]["FBDownPoints"].ToString();

            var para = new MobileShowParameter();
            para.FormId = "ora_CRM_MBL_BJD"; //报价详情的标识
            para.OpenStyle.ShowType = ShowType.Modal; //打开方式
            para.Width = 240;
            para.Height = 257;
            para.ParentPageId = this.View.PageId;
            para.CustomParams.Add("FMtlGroup", FMtlGroup);
            para.CustomParams.Add("FMtlItem", FMtlItem);
            para.CustomParams.Add("FQty", FQty);
            para.CustomParams.Add("FPAmt", FPAmt);
            para.CustomParams.Add("FPAmtGroup", FPAmtGroup);
            para.CustomParams.Add("FRangeAmtGP", FBRangeAmtGP);
            para.CustomParams.Add("FDescribe", FDescribe);
            para.CustomParams.Add("FModel", FModel);
            para.CustomParams.Add("FRptPrice", FRptPrice);
            para.CustomParams.Add("FRptPriceGroup", FRptPriceGroup.ToString());
            para.CustomParams.Add("FDownPoints", FDownPoints);

            if(CZ_GetValue("FDocumentStatus") == "C")
            {
                para.Status = OperationStatus.VIEW;
            }

            this.View.ShowForm(para, formResult =>
            {
                if (formResult.ReturnData == null)
                {
                    return;
                }
                FRptPrice = formResult.ReturnData.ToString();
                double FBUnitRPTPrice = double.Parse(FRptPrice) / double.Parse(FQty);
                this.View.BillModel.SetValue("FBRptPrice", FRptPrice, currSelectedRow);
                this.View.InvokeFieldUpdateService("FBRptPrice", currSelectedRow);
                this.View.BillModel.SetValue("FBUnitRPTPrice", FBUnitRPTPrice, currSelectedRow);

            });
        }

        #endregion

        #region 初始化操作

        /// <summary>
        /// 这里进行视图初始化操作
        /// </summary>
        private void initView()
        {
            /*
            var entryCrt = this.View.GetControl("F_ora_MobileProxyEntryEntity");
            //表体可编辑
            if (this.View.OpenParameter.Status.ToString() != "VIEW")
            {

                entryCrt.SetCustomPropertyValue("listEditable", true);
            }
            else
            {
                entryCrt.SetCustomPropertyValue("listEditable", false);
            }
             * */

            //锁定持有信息
            this.View.GetControl("FCrmHdOrgID").SetCustomPropertyValue("disabled", true);
            this.View.GetControl("FCrmHdDept").SetCustomPropertyValue("disabled", true);
            this.View.GetControl("FCrmHolder").SetCustomPropertyValue("disabled", true);

            ClientType = this.Context.ClientType.ToString();
        }

        /// <summary>
        /// 表体可编辑
        /// </summary>
        private void EntryEditEnable()
        {
            Control entryCtl = null;
            try
            {
                entryCtl = this.View.GetControl("F_ora_MobileProxyEntryEntity");
            }
            catch (Exception) { }

            if (entryCtl != null)
            {
                if (this.View.OpenParameter.Status.ToString() != "VIEW")
                {
                    entryCtl.SetCustomPropertyValue("listEditable", true);
                }
                else
                {
                    entryCtl.SetCustomPropertyValue("listEditable", false);
                }
            }
        }

        #endregion

        #region 提交后提醒退出
        /// <summary>
        /// 提醒跳转到业务审批，附件上传后执行
        /// </summary>
        private void Jump2Audit()
        {
            this.View.ShowMessage("提交成功，是否退出页面？",
                MessageBoxOptions.YesNo,
                new Action<MessageBoxResult>((result) =>
                {
                    if (result == MessageBoxResult.Yes)
                    {
                        //JSONArray paras = new JSONArray();
                        //JSONObject obj = new JSONObject();
                        //obj["url"] = "http://erp.ceegpower.com/K3Cloud/mobile/k3cloud.html?entryrole=XT&appid=10037&formId=MOB_WFTodoList&formType=mobilelist&acctid=5d3ea17f85b053";
                        //obj["title"] = "业务审批";
                        //paras.Add(obj);
                        //this.View.AddAction("openUrlWindow", paras);
                        this.View.Close();
                    }
                })
            );

        }
        #endregion

        #region 接收下推并保存时创建单据关联，需要在保存完成后执行
        /// <summary>
        /// 接收下推并保存时创建单据关联，需要在保存完成后执行
        /// </summary>
        private void CreateBillRelation()
        {
            /*
                @lktable varchar(30),--下游单据关联表
                @targetfid int,--下游单据头内码
                @targetformid varchar(36),--下游单据标识
                @targettable varchar(30),--下游单据头表名
                @sourcefid int,--上游单据头内码
                @sourcetable varchar(30),--上游单据头表名
                @sourceformid varchar(36),--上游单据标识
                @sourcefentryid int = 0, --上游单据体内码
                @sourcefentrytable varchar(30) = '' -- 上游单据体表名
             */

            string FTgtID = this.View.BillModel.DataObject["Id"].ToString();
            string sql = string.Format("SELECT * from T_BF_INSTANCEENTRY where FTTABLENAME = 'ora_CRM_SaleOffer' and FTID = '{0}'", FTgtID);
            var objs = CZDB_GetData(sql);
            if (objs.Count > 0)
            {
                return;
            }
            //string _FID = this.View.OpenParameter.GetCustomParameter("FID").ToString();
            string _FNicheBillNo = CZ_GetValue("FNicheID");
            string _sql = "select FID from ora_CRM_Niche where FBILLNO='" + _FNicheBillNo + "'";
            var data = CZDB_GetData(_sql);

            string lktable = "ora_CRM_SaleOffer_LK";
            string targetfid = FTgtID;
            string targettable = "ora_CRM_SaleOffer";
            string targetformid = "ora_CRM_SaleOffer";
            string sourcefid = data[0]["FID"].ToString();
            string sourcetable = "ora_CRM_Niche";
            string sourceformid = "ora_CRM_Niche";
            string sourcefentryid = "0";
            string sourcefentrytable = "";
            sql = String.Format(@"exec proc_czly_CreateBillRelation 
                   @lktable='{0}',@targetfid='{1}',@targettable='{2}',
                   @targetformid='{3}',@sourcefid='{4}',@sourcetable='{5}',
                   @sourceformid='{6}',@sourcefentryid='{7}',@sourcefentrytable='{8}'",
                   lktable, targetfid, targettable, targetformid, sourcefid, sourcetable, sourceformid, sourcefentryid, sourcefentrytable);
            var obj = CZDB_GetData(sql);
            //判断是否招投标
            string _FIsBid = this.View.BillModel.GetValue("FIsBid").ToString();
            if (_FIsBid == "True")
            {
                //修改商机销售状态为招投标阶段
                sql = "update ora_CRM_Niche set FSaleStatus='4',FBillStatus=1 where FBILLNO='" + _FNicheBillNo + "'";
            }
            else
            {
                //修改商机销售状态为报价阶段
                sql = "update ora_CRM_Niche set FSaleStatus='2',FBillStatus=1 where FBILLNO='" + _FNicheBillNo + "'";
            }
            CZDB_GetData(sql);
            

        }

        #endregion

        #region 下推生成销售合同评审
        /// <summary>
        /// 下推合同评审
        /// </summary>
        private void PushContact()
        {
            string _FID = this.View.BillModel.DataObject["Id"] == null ? "" : this.View.BillModel.DataObject["Id"].ToString();
            if (_FID == "")
            {
                this.View.ShowMessage("单据还未提交！");
                return;
            }
            var para = new MobileShowParameter();
            para.FormId = "ora_CRM_MBL_HTPS"; //报价的标识
            para.OpenStyle.ShowType = ShowType.Modal; //打开方式
            para.ParentPageId = this.View.PageId;
            //查询是否已经存在下推商机单据
            string _BillNo = (this.View.BillModel.DataObject["BillNo"] == null) ? "" : this.View.BillModel.DataObject["BillNo"].ToString();
            string sql = "select FID from ora_CRM_Contract where FNicheID='" + _BillNo + "'";
            var data = CZDB_GetData(sql);
            if (data.Count > 0)
            {
                this.View.ShowMessage("已经存在下推的合同评审，确定打开吗？", MessageBoxOptions.YesNo, 
                    new Action<MessageBoxResult>((result) =>
                    {
                        para.Status = OperationStatus.EDIT;
                        para.PKey = data[0]["FID"].ToString();//已有单据内码
                        para.CustomParams.Add("Flag", "EDIT");
                        this.View.ShowForm(para);
                    }));
            }
            else
            {
                //生成下推销售合同单据
                sql = string.Format("EXEC proc_czly_CRMGeneContact @FUserId='{0}', @FID='{1}'", this.Context.UserId, _FID);
                var datas = CZDB_GetData(sql);

                //para.CustomParams.Add("FID", _FID);
                //para.CustomParams.Add("Flag", "ADD");
                para.Status = OperationStatus.EDIT;
                para.PKey = datas[0]["FID"].ToString();//已有单据内码
                para.CustomParams.Add("Flag", "ADD");
                this.View.ShowForm(para);
            }
            //设置表单Title
            string strTitle = "销售合同评审";
            LocaleValue formTitle = new LocaleValue();
            formTitle.Add(new KeyValuePair<int, string>(this.Context.UserLocale.LCID, strTitle));
            this.View.SetFormTitle(formTitle);
        }
        #endregion

        #region 数据库查询方法
        /// <summary>
        /// 获取变量值 一般值
        /// </summary>
        /// <param name="_prm">普通控件名称</param>
        /// <returns></returns>
        public string CZ_GetValue(string _prm)
        {
            return this.View.BillModel.GetValue(_prm) == null ? "" : this.View.BillModel.GetValue(_prm).ToString();
        }
        /// <summary>
        /// 获取变量值 基础资料关联
        /// </summary>
        /// <param name="_obj">基础资料控件名称</param>
        /// <param name="_prm">值序列名称</param>
        /// <returns></returns>
        public string CZ_GetValue(string _obj, string _prm)
        {
            return (this.View.BillModel.GetValue(_obj) as DynamicObject) == null ? "" : (this.View.BillModel.GetValue(_obj) as DynamicObject)[_prm].ToString();
        }

        /// <summary>
        /// 获取用户
        /// </summary>
        /// <param name="userID"></param>
        /// <returns></returns>
        DynamicObject GetUser(string userID)
        {
            OQLFilter filter = OQLFilter.CreateHeadEntityFilter(string.Format("FUSERID={0}", userID));
            return BusinessDataServiceHelper.Load(this.View.Context, FormIdConst.SEC_User, null, filter).FirstOrDefault();
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
        #endregion

    }
}
