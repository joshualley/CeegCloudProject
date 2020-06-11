using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Data;
using System.Collections;

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
using Kingdee.BOS.Core.DynamicForm.PlugIn.ControlModel;
using Kingdee.BOS.Core.Metadata;
using Kingdee.BOS.JSON;

namespace CZ.CEEG.MblCrm.Niche
{
    [Description("移动商机")]
    [HotUpdate]
    public class CZ_CEEG_MblCrm_Niche : AbstractMobileBillPlugin
    {
        #region CZTY

        #region 页面属性组 CRM算SN号
        /// <summary>
        /// CLOUD 持有记录表体ID
        /// </summary>
        string VAL_HoldEntity = "FEntity";
        /// <summary>
        /// 持有人 任职 组织
        /// </summary>
        ArrayList AL_HoldOrg = new ArrayList();
        /// <summary>
        /// Key:任职组织ID  Val:对应任职部门ID集合
        /// </summary>
        Hashtable HT_HoldOrg2ALDept = new Hashtable();
        /// <summary>
        /// 任职组织字符串 org1,org2,...
        /// </summary>
        string STR_HoldOrg = "";
        /// <summary>
        /// 选中组织下 部门集合
        /// </summary>
        ArrayList AL_HoldDept = new ArrayList();
        /// <summary>
        /// 选中组织下 部门字符串 dept1,dept2,...
        /// </summary>
        string STR_HoldDept = "";
        /// <summary>
        /// Key:部门ID Val:部门递归码  ,L1-DeptID,L2-DeptID,...,Local-DeptID,
        /// </summary>
        Hashtable HT_HoldDeptCrmSN = new Hashtable();
        /// <summary>
        /// 事务锁 DataChange-Lock-Key 事务锁不为''时，其他DC-Act排斥
        /// </summary>
        //string LockTran_DC_ActKey = "";
        /// <summary>
        /// Kingdee.BOS.ClientType this.Context.ClientType.ToString()	[]	
        /// WPF     PC端 云之家客户端
        /// Html    PC端 浏览器
        /// Mobile  移动端｜摸拟打开移动端
        /// </summary>
        string ClientType = "";
        #endregion

        #region Actions
        /// <summary>
        /// 值更新-持有人
        /// </summary>
        /// <param name="e">DataChangedEventArgs</param>
        public void Act_DC_FCrmHolder(DataChangedEventArgs e)
        {
            string _oldVal = e.OldValue == null ? "0" : e.OldValue.ToString();
            string _newVal = e.NewValue == null ? "0" : e.NewValue.ToString();
            if (_newVal == "" || _newVal == "0")
            {
                this.View.BillModel.SetValue("FCrmHolder", _oldVal);
                //this.View.UpdateView("FCrmHolder");
                return;
            }

            Act_Hold_GetHoldInfo2(_newVal);
        }

        /// <summary>
        /// 值更新-持有组织
        /// </summary>
        /// <param name="e">DataChangedEventArgs</param>
        public void Act_DC_FCrmHdOrgID(DataChangedEventArgs e)
        {
            string _oldVal = e.OldValue == null ? "0" : e.OldValue.ToString();
            string _newVal = e.NewValue == null ? "0" : e.NewValue.ToString();
            if (_newVal == "" || _newVal == "0")
            {
                this.View.BillModel.SetValue("FCrmHdOrgID", _oldVal);
                //this.View.UpdateView("FCrmHdOrgID");
                return;
            }

            AL_HoldDept = HT_HoldOrg2ALDept[_newVal] as ArrayList;
            CZ_Rnd_AL2Str(AL_HoldDept, ref STR_HoldDept);
            string _localDeptID = AL_HoldDept[0].ToString();
            string _localCrmSN = HT_HoldDeptCrmSN[_localDeptID].ToString();

            this.View.BillModel.SetValue("FCrmHdDept", _localDeptID);
            this.View.BillModel.SetValue("FCrmSN", _localCrmSN);

            int _EHdRowCnt = this.View.BillModel.GetEntryRowCount(VAL_HoldEntity);
            if (_EHdRowCnt == 1)
            {
                this.View.BillModel.SetValue("FHHdOrgID", _newVal, 0);
                this.View.BillModel.SetValue("FHHdDept", _localDeptID, 0);
                this.View.BillModel.SetValue("FHSN", _localCrmSN, 0);
                this.View.BillModel.SetValue("FHBegDate", DateTime.Now.ToString("yyyy-MM-dd"), 0);
                this.View.BillModel.SetValue("FHEndDate", "9999-12-31", 0);
            }
        }

        /// <summary>
        /// 值更新-持有部门
        /// </summary>
        /// <param name="e"></param>
        public void Act_DC_FCrmHdDept(DataChangedEventArgs e)
        {
            string _oldVal = e.OldValue == null ? "0" : e.OldValue.ToString();
            string _newVal = e.NewValue == null ? "0" : e.NewValue.ToString();
            if (_newVal == "" || _newVal == "0")
            {
                this.View.BillModel.SetValue("FCrmHdDept", _oldVal);
                //this.View.UpdateView("FCrmHdDept");
                return;
            }

            string _localCrmSN = HT_HoldDeptCrmSN[_newVal].ToString();
            this.View.BillModel.SetValue("FCrmSN", _localCrmSN);

            int _EHdRowCnt = this.View.BillModel.GetEntryRowCount(VAL_HoldEntity);
            if (_EHdRowCnt == 1)
            {
                this.View.BillModel.SetValue("FHHdDept", _newVal, 0);
                this.View.BillModel.SetValue("FHSN", _localCrmSN, 0);
                this.View.BillModel.SetValue("FHBegDate", DateTime.Now.ToString("yyyy-MM-dd"), 0);
                this.View.BillModel.SetValue("FHEndDate", "9999-12-31", 0);
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

            this.View.BillModel.SetValue("FCrmHdOrgID", _FHoldOrgID);
            this.View.BillModel.SetValue("FCrmHdDept", _FHoldDeptID);
            this.View.BillModel.SetValue("FCrmHolder", _FHolder);
            this.View.BillModel.SetValue("FCrmSN", _FCrmSN);

            int _EHdRowCnt = this.View.BillModel.GetEntryRowCount("FEntityHD");
            if (_EHdRowCnt == 1)
            {
                this.View.BillModel.SetValue("FHHdOrgID", _FHoldOrgID, 0);
                this.View.BillModel.SetValue("FHHdDept", _FHoldDeptID, 0);
                this.View.BillModel.SetValue("FHHolder", _FHolder, 0);
                this.View.BillModel.SetValue("FHSN", _FCrmSN, 0);
                this.View.BillModel.SetValue("FHBegDate", DateTime.Now.ToString("yyyy-MM-dd"), 0);
                this.View.BillModel.SetValue("FHEndDate", "9999-12-31", 0);
            }
        }

        /// <summary>
        /// 根据传入用户ID 获取持有数据链 
        /// </summary>
        /// <param name="_FHolder">用户ID</param>
        private void Act_Hold_GetHoldInfo2(string _FHolder)
        {
            //_FHolder = this.Context.UserId.ToString();
            //_FHolder = this.CZ_GetValue_DF("FCrmHolder", "Id", "0");

            //exec proc_cztyCrm_GetCrmSN @FUserID='157202',@FIsFirstPost='%'
            string _sql = "exec proc_cztyCrm_GetCrmSN @FUserID='" + _FHolder + "',@FIsFirstPost='%'";
            DataTable _dt = this.CZDB_SearchBase(_sql);
            if (_dt.Rows.Count == 0)
            {
                this.View.BillModel.SetValue("FCrmHdOrgID", "0");
                this.View.BillModel.SetValue("FCrmHdDept", "0");
                this.View.BillModel.SetValue("FCrmHolder", "0");
                this.View.BillModel.SetValue("FCrmSN", "");
                return;
            }

            //string VAL_HoldEntity = "FEntity";
            AL_HoldOrg = new ArrayList();
            HT_HoldOrg2ALDept = new Hashtable();
            STR_HoldOrg = "";
            AL_HoldDept = new ArrayList();
            STR_HoldDept = "";
            HT_HoldDeptCrmSN = new Hashtable();
            //主任岗组织
            string _LocalOrgID = "";

            string _FHoldOrgID = "";    //组织
            string _FHoldDeptID = "";   //部门
            string _FCrmSN = "";        //CRMSN 部门层级码
            string _FIsFirstPost = "";  //主任岗标记 1=主任岗

            foreach (DataRow _dr in _dt.Rows)
            {
                _FHoldOrgID = _dr["FDeptOrg"].ToString();
                _FHoldDeptID = _dr["FDeptID"].ToString();
                _FCrmSN = _dr["FLevelCode"].ToString();
                _FIsFirstPost = _dr["FIsFirstPost"].ToString();

                //判定组织序列是否存在
                if (AL_HoldOrg.Contains(_FHoldOrgID))
                {
                    //已存在 取出
                    AL_HoldDept = HT_HoldOrg2ALDept[_FHoldOrgID] as ArrayList;
                }
                else
                {
                    //不存在 加入 声明
                    AL_HoldOrg.Add(_FHoldOrgID);
                    AL_HoldDept = new ArrayList();
                    HT_HoldOrg2ALDept.Add(_FHoldOrgID, AL_HoldDept);
                }

                if (!AL_HoldDept.Contains(_FHoldDeptID))
                {
                    AL_HoldDept.Add(_FHoldDeptID);
                }

                if (!HT_HoldDeptCrmSN.ContainsKey(_FHoldDeptID))
                {
                    HT_HoldDeptCrmSN.Add(_FHoldDeptID, _FCrmSN);
                }

                HT_HoldOrg2ALDept[_FHoldOrgID] = AL_HoldDept;

                if (_FIsFirstPost == "1")
                {
                    this.View.BillModel.SetValue("FCrmHdOrgID", _FHoldOrgID);
                    this.View.BillModel.SetValue("FCrmHdDept", _FHoldDeptID);
                    this.View.BillModel.SetValue("FCrmHolder", _FHolder);
                    this.View.BillModel.SetValue("FCrmSN", _FCrmSN);

                    _LocalOrgID = _FHoldOrgID;

                    int _EHdRowCnt = this.View.BillModel.GetEntryRowCount(VAL_HoldEntity);
                    if (_EHdRowCnt == 1)
                    {
                        this.View.BillModel.SetValue("FHHdOrgID", _FHoldOrgID, 0);
                        this.View.BillModel.SetValue("FHHdDept", _FHoldDeptID, 0);
                        this.View.BillModel.SetValue("FHHolder", _FHolder, 0);
                        this.View.BillModel.SetValue("FHSN", _FCrmSN, 0);
                        this.View.BillModel.SetValue("FHBegDate", DateTime.Now.ToString("yyyy-MM-dd"), 0);
                        this.View.BillModel.SetValue("FHEndDate", "9999-12-31", 0);
                    }
                }
            }

            CZ_Rnd_AL2Str(AL_HoldOrg, ref STR_HoldOrg);
            AL_HoldDept = HT_HoldOrg2ALDept[_LocalOrgID] as ArrayList;
            CZ_Rnd_AL2Str(AL_HoldDept, ref STR_HoldDept);
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

        #region CZTY Action Base
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
        /// 获取变量值 一般值
        /// </summary>
        /// <param name="_prm">普通控件名称</param>
        /// <param name="_dfVal">Default Val</param>
        /// <returns></returns>
        public string CZ_GetValue_DF(string _prm, string _dfVal)
        {
            string _backVal = this.View.BillModel.GetValue(_prm) == null ? "" : this.View.BillModel.GetValue(_prm).ToString();
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
            return (this.View.BillModel.GetValue(_obj) as DynamicObject) == null ? "" : (this.View.BillModel.GetValue(_obj) as DynamicObject)[_prm].ToString();
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
            string _backVal = (this.View.BillModel.GetValue(_obj) as DynamicObject) == null ? "" : (this.View.BillModel.GetValue(_obj) as DynamicObject)[_prm].ToString();
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
            return this.View.BillModel.GetValue(_prm, _rIdx) == null ? "" : this.View.BillModel.GetValue(_prm, _rIdx).ToString();
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
            _backVal = this.View.BillModel.GetValue(_prm, _rIdx) == null ? "" : this.View.BillModel.GetValue(_prm, _rIdx).ToString();
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
            return (this.View.BillModel.GetValue(_obj, _rIdx) as DynamicObject) == null ? "" : (this.View.BillModel.GetValue(_obj, _rIdx) as DynamicObject)[_prm].ToString();
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
            _backVal = (this.View.BillModel.GetValue(_obj, _rIdx) as DynamicObject) == null ? "" : (this.View.BillModel.GetValue(_obj, _rIdx) as DynamicObject)[_prm].ToString();
            return _backVal == "" ? _dfVal : _backVal;
        }

        /// <summary>
        /// 获取当前单据ID
        /// </summary>
        /// <returns></returns>
        public string CZ_GetFormID()
        {
            return (this.View.BillModel.DataObject as DynamicObject)["Id"] == null ? "0" : (this.View.BillModel.DataObject as DynamicObject)["Id"].ToString();
        }

        /// <summary>
        /// 获取当前单据状态    新增时为 Z  审核：C
        /// </summary>
        /// <returns></returns>
        public string CZ_GetFormStatus()
        {
            return this.View.BillModel.GetValue("FDocumentStatus").ToString();
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

        #endregion

        #region CZLY

        #region K3 Override
        /// <summary>
        /// 数据加载完毕
        /// </summary>
        /// <param name="e"></param>
        public override void AfterBindData(EventArgs e)
        {
            initView();
            Act_ABD_CopyNiche();
            this.View.BillModel.SetValue("FCustOrgId", 1);
        }

        /// <summary>
        /// 值更新
        /// </summary>
        /// <param name="e"></param>
        public override void DataChanged(DataChangedEventArgs e)
        {
            string _key = e.Field.Key.ToUpperInvariant();
            switch (_key)
            {
                case "FCUSTID":
                    //点击客户时刷新基础资料属性
                    this.View.UpdateView();
                    break;
                default:
                    break;
            }

            base.DataChanged(e);

        }

        public override void AfterDoOperation(AfterDoOperationEventArgs e)
        {
            base.AfterDoOperation(e);
            string op = e.Operation.Operation.ToUpperInvariant();
            switch (op)
            {
                case "SUBMIT":
                    string Flag = this.View.OpenParameter.GetCustomParameter("Flag") == null ? "" : this.View.OpenParameter.GetCustomParameter("Flag").ToString();
                    if (Flag == "ADD")
                    {
                        //如果是下推生成的单据，则建立起它们的关联关系
                        CreateBillRelation();
                    }
                    break;
            }
        }

        public override void AfterButtonClick(AfterButtonClickEventArgs e)
        {
            base.AfterButtonClick(e);
            string key = e.Key.ToString().ToUpperInvariant();
            switch (key)
            {
                case "FNEWROW":
                    AddNewEntryRow();
                    break;
                case "FPUSH":
                    if (CZ_GetValue("FDocumentStatus") != "C")
                    {
                        this.View.ShowMessage("商机还未通过审核！");
                    }
                    else
                    {
                        Act_Push();
                    }
                    break;
                case "FCOPY":
                    Act_ABC_CopyNiche();
                    break;
            }

        }

        public override void BeforeF7Select(BeforeF7SelectEventArgs e)
        {
            switch (e.FieldKey.ToUpperInvariant())
            {
                case "FCUSTID": //客户
                    //string filter = FilterCustomerBySeller();
                    //e.ListFilterParameter.Filter = filter;
                    break;
                default:
                    break;
            }

            base.BeforeF7Select(e);
        }

        #endregion

        #region 过滤客户
        /// <summary>
        /// 通过销售员过滤客户，销售员仅可选择自己创建的客户
        /// </summary>
        /// <returns></returns>
        private string FilterCustomerBySeller()
        {
            string filter = " FCustID in (";
            string userId = this.Context.UserId.ToString();
            //string orgId = this.View.BillModel.GetValue("FOrgID") == null ? "0" : (this.View.BillModel.GetValue("FOrgID") as DynamicObject)["Id"].ToString();
            string sql = string.Format("EXEC proc_czly_GetSalesmanIdByUserId @FUserId='{0}'", userId);
            var objs = CZDB_GetData(sql);
            if (objs.Count > 0)
            {
                string FSalesmanIds = "";
                for (int i = 0; i < objs.Count; i++)
                {
                    if (i == objs.Count - 1)
                    {
                        FSalesmanIds += string.Format("'{0}'", objs[i]["FSalesmanId"].ToString());
                    }
                    else
                    {
                        FSalesmanIds += string.Format("'{0}',", objs[i]["FSalesmanId"].ToString());
                    }
                }
                if (objs.Count <= 0)
                {
                    FSalesmanIds += "'0'";
                }

                sql = "select FCustID from T_BD_CUSTOMER where FSELLER in (" + FSalesmanIds + ")";
                objs = CZDB_GetData(sql);

                for (int i = 0; i < objs.Count; i++)
                {
                    filter += "'" + objs[i]["FCustID"].ToString() + "'";
                    if (i < objs.Count - 1) filter += ",";
                }
                if (objs.Count <= 0)
                {
                    filter += "'0'";
                }
            }
            else
            {
                filter += "'0'";
            }

            filter += ')';

            return filter;
        }

        #endregion

        #region Actions
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

        /// <summary>
        /// 商机复制
        /// </summary>
        private void Act_ABC_CopyNiche()
        {
            var para = new MobileShowParameter();
            para.FormId = "ora_CRM_MBL_SJ"; //商机的标识
            para.OpenStyle.ShowType = ShowType.Modal; //打开方式
            para.ParentPageId = this.View.PageId;
            para.CustomParams.Add("Flag", "COPY");
            para.CustomParams.Add("FID", this.View.BillModel.DataObject["Id"].ToString());
            this.View.ShowForm(para);
        }
        /// <summary>
        /// 设置复制的新商机数据
        /// </summary>
        private void Act_ABD_CopyNiche()
        {
            string FDocumentStatus = this.View.BillModel.GetValue("FDocumentStatus").ToString();
            if(FDocumentStatus != "Z")
            {
                return;
            }
            string Flag = this.View.OpenParameter.GetCustomParameter("Flag") == null ? "" : this.View.OpenParameter.GetCustomParameter("Flag").ToString();
            if(Flag == "COPY")
            {
                string FSrcID = this.View.OpenParameter.GetCustomParameter("FID").ToString();
                //this.View.ShowMessage(FSrcID);
                //表头
                string sql = "select * from ora_CRM_Niche where FID='" + FSrcID + "'";
                var billHead = CZDB_GetData(sql);
                this.View.BillModel.SetValue("FNicheSrc", billHead[0]["FNicheSrc"].ToString());
                this.View.BillModel.SetValue("FCustID", billHead[0]["FCustID"].ToString());
                this.View.BillModel.SetValue("FExpectAmt", billHead[0]["FExpectAmt"].ToString());
                this.View.BillModel.SetValue("FPhone", billHead[0]["FPhone"].ToString());
                this.View.BillModel.SetValue("FPrjName", billHead[0]["FPrjName"].ToString());
                this.View.BillModel.SetValue("FPrjAddress", billHead[0]["FPrjAddress"].ToString());
                this.View.BillModel.SetValue("FRemarks", billHead[0]["FRemarks"].ToString());
                this.View.BillModel.SetValue("FExpectAmtCN", billHead[0]["FExpectAmtCN"].ToString());
                this.View.BillModel.SetValue("FRate", billHead[0]["FRate"].ToString());
                this.View.BillModel.SetValue("FCrmSN", billHead[0]["FCrmSN"].ToString());
                this.View.BillModel.SetValue("FNicheSrc", billHead[0]["FNicheSrc"].ToString());
                this.View.BillModel.SetValue("FCrmHdOrgID", billHead[0]["FCrmHdOrgID"].ToString());
                this.View.BillModel.SetValue("FCrmHdDept", billHead[0]["FCrmHdDept"].ToString());
                this.View.BillModel.SetValue("FCrmHolder", billHead[0]["FCrmHolder"].ToString());
                //表体
                sql = "select * from ora_CRM_NicheEntry where FID='" + FSrcID + "'";
                var entry = CZDB_GetData(sql);
                for (int i = 0; i < entry.Count; i++)
                {
                    int rowIndex = this.View.BillModel.GetEntryRowCount("FEntity") - 1;
                    this.View.BillModel.SetValue("FMtlGroup", entry[rowIndex]["FMtlGroup"].ToString());
                    this.View.BillModel.SetValue("FQty", entry[rowIndex]["FQty"].ToString());
                    this.View.BillModel.SetValue("FPdtText", entry[rowIndex]["FPdtText"].ToString());
                    if(i != entry.Count-1)
                    {
                        this.View.BillModel.CreateNewEntryRow("FEntity");
                    }
                }
                this.View.UpdateView("FEntity");
                //创建结转信息
                int _EHdRowCnt = this.View.BillModel.GetEntryRowCount(VAL_HoldEntity);
                if (_EHdRowCnt == 1)
                {

                    this.View.BillModel.SetValue("FHHolder", billHead[0]["FCrmHolder"].ToString(), 0);
                    this.View.BillModel.SetValue("FHHdOrgID", billHead[0]["FCrmHdOrgID"].ToString(), 0);
                    this.View.BillModel.SetValue("FHHdDept", billHead[0]["FCrmHdDept"].ToString(), 0);
                    this.View.BillModel.SetValue("FHSN", billHead[0]["FCrmSN"].ToString(), 0);
                    this.View.BillModel.SetValue("FHBegDate", DateTime.Now.ToString("yyyy-MM-dd"), 0);
                    this.View.BillModel.SetValue("FHEndDate", "9999-12-31", 0);
                }

            }
        }

        #endregion

        #region 初始化，默认功能设置
        /// <summary>
        /// 根据流程显示隐藏按钮
        /// </summary>
        private void HideBtn()
        {
            string _FDocumentStatus = this.CZ_GetValue("FDocumentStatus");
            if (_FDocumentStatus == "Z" || _FDocumentStatus == "A" || _FDocumentStatus == "D")
            {
                var submitBtn = this.View.GetControl("FSubmitBtn");
                var pushBtn = this.View.GetControl("FPUSH");
                var copyBtn = this.View.GetControl("FCOPY");
                copyBtn.Visible = false;
                pushBtn.Visible = false;
                submitBtn.SetCustomPropertyValue("width", 310);
            }
            else if (_FDocumentStatus == "B")
            {
                var submitBtn = this.View.GetControl("FSubmitBtn");
                //var saveBtn = this.View.GetControl("FSaveBtn");
                var pushBtn = this.View.GetControl("FPUSH");
                var copyBtn = this.View.GetControl("FCOPY");
                copyBtn.Visible = false;
                submitBtn.Enabled = false;
                pushBtn.Visible = false;
                submitBtn.SetCustomPropertyValue("width", 310);
            }
            else if (_FDocumentStatus == "C")
            {
                string FBillNo = this.View.BillModel.GetValue("FBillNo").ToString();
                string sql = "select FID from ora_CRM_SaleOffer where FDocumentStatus not in ('Z', 'A', 'D') and FNicheID='" + FBillNo + "'";
                var objs = CZDB_GetData(sql);
                if (objs.Count > 0)
                {
                    this.View.GetControl("FSubmitBtn").Visible = false;
                    this.View.GetControl("FPUSH").Visible = false;
                    var copyBtn = this.View.GetControl("FCOPY");
                    copyBtn.SetCustomPropertyValue("width", 310);
                }
                else
                {
                    var submitBtn = this.View.GetControl("FSubmitBtn");
                    var pushBtn = this.View.GetControl("FPUSH");
                    submitBtn.Visible = false;
                    pushBtn.SetCustomPropertyValue("width", 310);
                }
            }

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

        /// <summary>
        /// 新增表体行
        /// </summary>
        private void AddNewEntryRow()
        {
            this.View.BillModel.BeginIniti();
            this.View.BillModel.CreateNewEntryRow("FEntity");
            this.View.BillModel.EndIniti();
            this.View.UpdateView("F_ora_MobileProxyEntryEntity");
        }

        /// <summary>
        /// 这里进行视图初始化操作
        /// </summary>
        private void initView()
        {
            this.View.BillModel.SetValue("FSrcBillType", "ora_CRM_Clue");
            EntryEditEnable();
            HideBtn();
            

            //锁定持有信息
            this.View.GetControl("FCrmHdOrgID").SetCustomPropertyValue("disabled", true);
            this.View.GetControl("FCrmHdDept").SetCustomPropertyValue("disabled", true);
            this.View.GetControl("FCrmHolder").SetCustomPropertyValue("disabled", true);

            ClientType = this.Context.ClientType.ToString();
            string Flag = this.View.OpenParameter.GetCustomParameter("Flag") == null ? "" : this.View.OpenParameter.GetCustomParameter("Flag").ToString();
            if (Flag == "ADD")
            {
                FromClueGetData();
            }
            else if (Flag == "")
            {
                if (this.CZ_GetFormStatus() == "Z" && ClientType == "Mobile")
                {
                    //如果创建人具有任岗信息，则根据其用户id初始化持有信息
                    Act_Hold_GetHoldInfo2(this.Context.UserId.ToString());
                }
            }
        }
        #endregion

        #region 接收线索下推信息

        private void FromClueGetData()
        {
            string _FCustID = this.View.OpenParameter.GetCustomParameter("FCustID").ToString();
            string _FPrjName = this.View.OpenParameter.GetCustomParameter("FPrjName").ToString();
            string _FPrjAddress = this.View.OpenParameter.GetCustomParameter("FPrjAddress").ToString();
            string _FRemarks = this.View.OpenParameter.GetCustomParameter("FRemarks").ToString();

            string _FClueID = this.View.OpenParameter.GetCustomParameter("FClueID").ToString();
            string _FClueNo = this.View.OpenParameter.GetCustomParameter("FClueNo").ToString();

            string _FCrmHdOrgID = this.View.OpenParameter.GetCustomParameter("FCrmHdOrgID").ToString();
            string _FCrmHdDept = this.View.OpenParameter.GetCustomParameter("FCrmHdDept").ToString();
            string _FCrmHolder = this.View.OpenParameter.GetCustomParameter("FCrmHolder").ToString();
            string _FCrmSN = this.View.OpenParameter.GetCustomParameter("FCrmSN").ToString();
            //客户及项目
            this.View.BillModel.SetValue("FCustID", _FCustID);
            this.View.BillModel.SetValue("FPrjName", _FPrjName);
            this.View.BillModel.SetValue("FPrjAddress", _FPrjAddress);
            this.View.BillModel.SetValue("FRemarks", _FRemarks);
            //源单编号
            this.View.BillModel.SetValue("FClueID", _FClueID);
            this.View.BillModel.SetValue("FClueNo", _FClueNo);
            //持有信息
            this.View.BillModel.SetValue("FCrmHdOrgID", _FCrmHdOrgID);
            this.View.BillModel.SetValue("FCrmHdDept", _FCrmHdDept);
            this.View.BillModel.SetValue("FCrmHolder", _FCrmHolder);
            this.View.BillModel.SetValue("FCrmSN", _FCrmSN);

            //创建结转信息
            int _EHdRowCnt = this.View.BillModel.GetEntryRowCount(VAL_HoldEntity);
            if (_EHdRowCnt == 1)
            {

                this.View.BillModel.SetValue("FHHolder", _FCrmHolder, 0);
                this.View.BillModel.SetValue("FHHdOrgID", _FCrmHdOrgID, 0);
                this.View.BillModel.SetValue("FHHdDept", _FCrmHdDept, 0);
                this.View.BillModel.SetValue("FHSN", _FCrmSN, 0);
                this.View.BillModel.SetValue("FHBegDate", DateTime.Now.ToString("yyyy-MM-dd"), 0);
                this.View.BillModel.SetValue("FHEndDate", "9999-12-31", 0);
            }
        }

        /// <summary>
        /// 接收下推并保存时创建单据关联，需要在保存完成后执行
        /// </summary>
        private void CreateBillRelation()
        {
            /*
                @lktable varchar(30),--下游单据关联表
                @targetfid int,--下游单据头内码
                @targettable varchar(30),--下游单据头表名
                @targetformid varchar(36),--下游单据标识
                @sourcefid int,--上游单据头内码
                @sourcetable varchar(30),--上游单据头表名
                @sourceformid varchar(36),--上游单据标识
                @sourcefentryid int = 0, --上游单据体内码
                @sourcefentrytable varchar(30) = '' -- 上游单据体表名
             */
            string _FClueNo = this.View.OpenParameter.GetCustomParameter("FClueNo").ToString();
            string _sql = "select FID from ora_CRM_Clue where FBILLNO='" + _FClueNo + "'";

            string lktable = "ora_CRM_Niche_LK";
            string targetfid = this.View.BillModel.DataObject["Id"].ToString();
            string targettable = "ora_CRM_Niche";
            string targetformid = "ora_CRM_Niche";
            string sourcefid = CZDB_GetData(_sql)[0]["FID"].ToString();
            string sourcetable = "ora_CRM_Clue";
            string sourceformid = "ora_CRM_Clue";
            string sourcefentryid = "0";
            string sourcefentrytable = "";
            string sql = String.Format(@"exec proc_czly_CreateBillRelation 
                   @lktable='{0}',@targetfid='{1}',@targettable='{2}',
                   @targetformid='{3}',@sourcefid='{4}',@sourcetable='{5}',
                   @sourceformid='{6}',@sourcefentryid='{7}',@sourcefentrytable='{8}'",
                   lktable, targetfid, targettable, targetformid, sourcefid, sourcetable, sourceformid, sourcefentryid, sourcefentrytable);
            var obj = CZDB_GetData(sql);
            //修改线索状态为已转化
            sql = "update ora_CRM_Clue set FClueStatus='3', FBillStatus=1 where FBillNo='" + _FClueNo + "'";
            CZDB_GetData(sql);

        }

        #endregion

        #region 下推
        /// <summary>
        /// 下推选择单据类型
        /// </summary>
        private void Act_Push()
        {
            //打开选择界面
            var para1 = new MobileShowParameter();
            para1.FormId = "ora_CRM_MBL_SJXT"; 
            para1.OpenStyle.ShowType = ShowType.Modal;
            para1.Height = 143;
            para1.Width = 240;
            para1.ParentPageId = this.View.PageId;
            para1.Status = OperationStatus.EDIT;
            string pushFormId = "";
            this.View.ShowForm(para1, (formResult) =>
            {
                if (formResult.ReturnData == null)
                {
                    return;
                }
                pushFormId = formResult.ReturnData.ToString();
                if (pushFormId == "ora_CRM_MBL_BJ")
                {
                    Act_PushSaleOffer();
                }
                else if (pushFormId == "ora_CRM_MBL_LawEntrust")
                {
                    Act_PushLawEntrust();
                }
            });

            
            
        }

        /// <summary>
        /// 下推开具法委
        /// </summary>
        private void Act_PushLawEntrust()
        {
            var para = new MobileShowParameter();
            para.FormId = "ora_CRM_MBL_LawEntrust"; //标识
            para.OpenStyle.ShowType = ShowType.Modal; //打开方式
            para.ParentPageId = this.View.PageId;
            //查询是否已经存在下推商机单据
            string _BillNo = (this.View.BillModel.DataObject["BillNo"] == null) ? "" : this.View.BillModel.DataObject["BillNo"].ToString();
            string sql = "select FID from ora_CRM_LawEntrust where FNicheID='" + _BillNo + "'";
            var data = CZDB_GetData(sql);
            string strTitle;
            LocaleValue formTitle;
            if (data.Count > 0)
            {
                this.View.ShowMessage("已经存在下推的开具发委单，确定打开吗？", MessageBoxOptions.YesNo,
                    new Action<MessageBoxResult>((result) =>
                    {
                        if(result == MessageBoxResult.Yes)
                        {
                            //para.Status = OperationStatus.VIEW;
                            para.PKey = data[0]["FID"].ToString();//已有单据内码
                            para.CustomParams.Add("Flag", "EDIT");
                            para.CustomParams.Add("FID", data[0]["FID"].ToString());
                            //设置表单Title
                            strTitle = "开具法委";
                            formTitle = new LocaleValue();
                            formTitle.Add(new KeyValuePair<int, string>(this.Context.UserLocale.LCID, strTitle));
                            this.View.SetFormTitle(formTitle);
                            this.View.ShowForm(para);
                        }
                    }));
            }
            else
            {
                this.View.ShowMessage("是否下推生成开具法委？", MessageBoxOptions.YesNo, (result) =>
                {
                    if(result == MessageBoxResult.Yes)
                    {
                        //para.PKey = objs[0]["FID"].ToString();//已有单据内码
                        string FCustID = this.View.BillModel.GetValue("FCustID") == null ? "0" :
                            (this.View.BillModel.GetValue("FCustID") as DynamicObject)["Id"].ToString();
                        string FPrjName = CZ_GetValue("FPrjName");
                        string FCrmSN = CZ_GetValue("FCrmSN");
                        para.CustomParams.Add("Flag", "ADD");
                        para.CustomParams.Add("FBillNo", _BillNo);
                        para.CustomParams.Add("FCustID", FCustID);
                        para.CustomParams.Add("FPrjName", FPrjName);
                        para.CustomParams.Add("FCrmSN", FCrmSN);

                        //设置表单Title
                        strTitle = "开具法委";
                        formTitle = new LocaleValue();
                        formTitle.Add(new KeyValuePair<int, string>(this.Context.UserLocale.LCID, strTitle));
                        this.View.SetFormTitle(formTitle);
                        this.View.ShowForm(para);
                    }
                });
                
            }
            

        }

        /// <summary>
        /// 下推报价
        /// </summary>
        private void Act_PushSaleOffer()
        {
            var para = new MobileShowParameter();
            para.FormId = "ora_CRM_MBL_BJ"; //报价的标识
            para.OpenStyle.ShowType = ShowType.Modal; //打开方式
            para.ParentPageId = this.View.PageId;
            //查询是否已经存在下推商机单据
            string _BillNo = (this.View.BillModel.DataObject["BillNo"] == null) ? "" : this.View.BillModel.DataObject["BillNo"].ToString();
            string sql = "select FID from ora_CRM_SaleOffer where FNicheID='" + _BillNo + "'";
            var data = CZDB_GetData(sql);
            string strTitle;
            LocaleValue formTitle;
            if (data.Count > 0)
            {
                this.View.ShowMessage("已经存在下推的报价单，确定打开吗？", MessageBoxOptions.YesNo,
                    new Action<MessageBoxResult>((result) =>
                    {
                        //para.Status = OperationStatus.VIEW;
                        para.PKey = data[0]["FID"].ToString();//已有单据内码
                        para.CustomParams.Add("Flag", "EDIT");
                        para.CustomParams.Add("FID", data[0]["FID"].ToString());
                        this.View.ShowForm(para);
                        //设置表单Title
                        strTitle = "销售报价";
                        formTitle = new LocaleValue();
                        formTitle.Add(new KeyValuePair<int, string>(this.Context.UserLocale.LCID, strTitle));
                        this.View.SetFormTitle(formTitle);
                    }));
            }
            else
            {
                this.View.ShowMessage("是否下推生成销售报价？", MessageBoxOptions.YesNo, (result) =>
                {
                    if (result == MessageBoxResult.Yes)
                    {
                        string _FID = this.View.BillModel.DataObject["Id"] == null ? "" : this.View.BillModel.DataObject["Id"].ToString();
                        sql = string.Format("exec proc_czly_GeneSaleOffer @NFID='{0}'", _FID);
                        var objs = CZDB_GetData(sql);

                        para.PKey = objs[0]["FID"].ToString();//已有单据内码
                        para.CustomParams.Add("Flag", "ADD");
                        para.CustomParams.Add("FID", objs[0]["FID"].ToString());
                        this.View.ShowForm(para);
                        //设置表单Title
                        strTitle = "销售报价";
                        formTitle = new LocaleValue();
                        formTitle.Add(new KeyValuePair<int, string>(this.Context.UserLocale.LCID, strTitle));
                        this.View.SetFormTitle(formTitle);
                    }
                });
                        
            }
            
        }

        /// <summary>
        /// 添加自定义页面传输参数
        /// </summary>
        /// <param name="para"></param>
        private void addCustParas(MobileShowParameter para)
        {
            //客户信息
            string _FCustID = CZ_GetValue("FCustID", "Id");
            //项目信息
            string _FPrjName = CZ_GetValue("FPrjName");
            string _FPrjAddress = CZ_GetValue("FPrjAddress");
            string _FRemarks = CZ_GetValue("FRemarks");
            //单号
            string _FBillNo = CZ_GetValue("FBillNo");
            //持有信息
            string _FCrmHdOrgID = CZ_GetValue("FCrmHdOrgID", "Id");
            string _FCrmHdDept = CZ_GetValue("FCrmHdDept", "Id");
            string _FCrmHolder = CZ_GetValue("FCrmHolder", "Id");
            //CRM标识码
            string _FCrmSN = CZ_GetValue("FCrmSN");

            para.CustomParams.Add("FCustName", _FCustID);
            para.CustomParams.Add("FPrjName", _FPrjName);
            para.CustomParams.Add("FPrjAddress", _FPrjAddress);
            para.CustomParams.Add("FRemarks", _FRemarks);

            para.CustomParams.Add("FNicheID", _FBillNo);
            //para.CustomParams.Add("FClueNo", _FBillNo);

            para.CustomParams.Add("FCrmHdOrgID", _FCrmHdOrgID);
            para.CustomParams.Add("FCrmHdDept", _FCrmHdDept);
            para.CustomParams.Add("FCrmHolder", _FCrmHolder);
            para.CustomParams.Add("FCrmSN", _FCrmSN);
            //表体无法传输，直接在目标单中用SQL查询源单
        }
        #endregion

        #region 数据库查询方法
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

        #endregion
    }
}
