using System;
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

using Kingdee.BOS.App.Data;
using Kingdee.BOS.Core.DynamicForm;

namespace CZ.CEEG.BosCrm.Niche
{
    [Description("BOS商机")]
    [Kingdee.BOS.Util.HotUpdate]
    public class CZ_CEEG_BosCrm_Niche : AbstractDynamicFormPlugIn
    {
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
        string LockTran_DC_ActKey = "";
        /// <summary>
        /// Kingdee.BOS.ClientType this.Context.ClientType.ToString()	[]	
        /// WPF     PC端 云之家客户端
        /// Html    PC端 浏览器
        /// Mobile  移动端｜摸拟打开移动端
        /// </summary>
        string ClientType = "";
        #endregion

        #region K3 Override

        private bool isInit = false;
        /// <summary>
        /// 这里进行视图初始化操作 by: ly
        /// </summary>
        private void initView()
        {
            //锁定持有信息
            this.View.StyleManager.SetEnabled("FCrmHdOrgID", "", false);
            this.View.StyleManager.SetEnabled("FCrmHdDept", "", false);
            this.View.StyleManager.SetEnabled("FCrmHolder", "", false);
        }

        /// <summary>
        /// 数据加载完毕 by: ly
        /// </summary>
        /// <param name="e"></param>
        public override void AfterBindData(EventArgs e)
        {
            base.AfterBindData(e);
            if (this.View.ClientType.ToString() != "Mobile" && !isInit)
            {
                initView();
                isInit = true;
            }
            ClientType = this.Context.ClientType.ToString();
            if (this.CZ_GetFormStatus() == "Z" && ClientType != "Mobile")
            {
                Act_Hold_GetHoldInfo2(this.Context.UserId.ToString());
                this.View.Model.SetValue("FCustOrgId", 1); //设置客户组织
            }
        }

        /// <summary>
        /// 保存后的一些操作
        /// </summary>
        /// <param name="e"></param>
        public override void AfterDoOperation(AfterDoOperationEventArgs e)
        {
            base.AfterDoOperation(e);
            if(this.View.ClientType.ToString() != "Mobile")
            {
                string opKey = e.Operation.Operation.ToUpperInvariant();
                string FClueNo = "";
                switch (opKey)
                {
                    case "SAVE":
                        FClueNo = CZ_GetValue("FClueID");
                        if (FClueNo != "")
                        {
                            //反写线索状态为已转化商机
                            string sql = String.Format("update ora_CRM_Clue set FClueStatus=3 where FBillNo='{0}'", FClueNo);
                            CZDB_GetData(sql);
                        }
                        break;
                    case "SUBMIT":
                        FClueNo = CZ_GetValue("FClueID");
                        if (FClueNo != "")
                        {
                            //反写线索状态为已转化商机
                            string sql = String.Format("update ora_CRM_Clue set FClueStatus=3 where FBillNo='{0}'", FClueNo);
                            CZDB_GetData(sql);
                        }
                        break;
                }
            }
        }

        /// <summary>
        /// 值更新
        /// </summary>
        /// <param name="e"></param>
        public override void DataChanged(DataChangedEventArgs e)
        {
            base.DataChanged(e);
            string _FCrmHolder = (this.View.Model.DataObject["FCrmHolder"] == null) ? "" : (this.View.Model.DataObject["FCrmHolder"] as DynamicObject)["Id"].ToString();
            string _FCrmHdOrgID = (this.View.Model.DataObject["FCrmHdOrgID"] == null) ? "" : (this.View.Model.DataObject["FCrmHdOrgID"] as DynamicObject)["Id"].ToString();
            string _FCrmHdDept = (this.View.Model.DataObject["FCrmHdDept"] == null) ? "" : (this.View.Model.DataObject["FCrmHdDept"] as DynamicObject)["Id"].ToString();
            string _key = e.Field.FieldName.ToUpperInvariant();
            switch (_key)
            {
                case "FCRMHOLDER":
                    //FCrmHolder 持有人
                    if (ClientType == "Mobile" || LockTran_DC_ActKey != "")
                    {
                        break;
                    }

                    LockTran_DC_ActKey = "FCrmHolder";
                    Act_DC_FCrmHolder(e);
                    LockTran_DC_ActKey = "";
                    //ConfirmMsg(_FCrmHolder, _FCrmHdOrgID, _FCrmHdDept);
                    break;
                case "FCRMHDORGID":
                    //FCrmHdOrgID 持有组织
                    if (ClientType == "Mobile" || LockTran_DC_ActKey != "")
                    {
                        break;
                    }

                    if (_FCrmHolder == "")
                    {
                        //this.View.ShowMessage("请先选择持有人！");
                        break;
                    }

                    LockTran_DC_ActKey = "FCrmHdOrgID";
                    Act_DC_FCrmHdOrgID(e);
                    LockTran_DC_ActKey = "";
                    //ConfirmMsg(_FCrmHolder, _FCrmHdOrgID, _FCrmHdDept);
                    break;
                case "FCRMHDDEPT":
                    //FCrmHdDept 持有部门
                    if (ClientType == "Mobile" || LockTran_DC_ActKey != "")
                    {
                        break;
                    }
                    if (_FCrmHolder == "")
                    {
                        //this.View.ShowMessage("请先选择持有人！");
                        break;
                    }
                    else if (_FCrmHdOrgID == "")
                    {
                        //this.View.ShowMessage("请先选择持有组织！");
                        break;
                    }

                    LockTran_DC_ActKey = "FCrmHdDept";
                    Act_DC_FCrmHdDept(e);
                    LockTran_DC_ActKey = "";
                    //ConfirmMsg(_FCrmHolder, _FCrmHdOrgID, _FCrmHdDept);
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// 提示是否分配 by: ly
        /// </summary>
        /// <param name="_FCrmHolder"></param>
        /// <param name="_FCrmHdOrgID"></param>
        /// <param name="_FCrmHdDept"></param>
        private void ConfirmMsg(string _FCrmHolder, string _FCrmHdOrgID, string _FCrmHdDept)
        {
            if (_FCrmHolder != "" && _FCrmHdOrgID != "" && _FCrmHdDept != "")
            {
                //存在BUG，不能在选择否时，使选择的持有人无效
                string FCrmHolderName = (this.View.Model.DataObject["FCrmHolder"] == null) ? "" : (this.View.Model.DataObject["FCrmHolder"] as DynamicObject)["Name"].ToString();
                this.View.ShowMessage("是否确定将本条商机转给" + FCrmHolderName + "？", MessageBoxOptions.YesNo,
                        new Action<MessageBoxResult>((result) =>
                        {
                            if (result == MessageBoxResult.Yes)
                            {
                                this.View.Model.SetValue("FClueStatus", "2");
                                //锁定持有信息
                                this.View.StyleManager.SetEnabled("FCrmHdOrgID", "", false);
                                this.View.StyleManager.SetEnabled("FCrmHdDept", "", false);
                                this.View.StyleManager.SetEnabled("FCrmHolder", "", false);
                            }
                        }));
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
                case "FCRMHDORGID":
                    _crmFilter = STR_HoldOrg;
                    if (_crmFilter != "")
                    {
                        e.Filter = " FOrgID in(" + _crmFilter + ")";
                    }
                    break;
                case "FCRMHDDEPT":
                    _crmFilter = STR_HoldDept;
                    if (_crmFilter != "")
                    {
                        e.Filter = " FDEPTID in(" + _crmFilter + ")";
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
                case "FCRMHDORGID":
                    _crmFilter = STR_HoldOrg;
                    if (_crmFilter != "")
                    {
                        e.ListFilterParameter.Filter = " FOrgID in(" + _crmFilter + ")";
                    }
                    break;
                case "FCRMHDDEPT":
                    _crmFilter = STR_HoldDept;
                    if (_crmFilter != "")
                    {
                        e.ListFilterParameter.Filter = " FDEPTID in(" + _crmFilter + ")";
                    }
                    break;
                case "FCUSTID":
                    //e.ListFilterParameter.Filter = FilterCustomerBySeller();
                    break;
                default:
                    break;
            }

            base.BeforeF7Select(e);
        }

        #endregion

        #region Actions
        /// <summary>
        /// 通过销售员过滤客户，销售员仅可选择自己创建的客户
        /// </summary>
        /// <returns></returns>
        private string FilterCustomerBySeller()
        {
            string filter = " FSELLER in (";
            
            string userId = this.Context.UserId.ToString();
            string sql = string.Format("EXEC proc_czly_GetSalesmanIdByUserId @FUserId='{0}'", userId);
            var objs = CZDB_GetData(sql);

            for (int i = 0; i < objs.Count; i++)
            {
                if (i == objs.Count - 1) 
                    filter += "'" + objs[i]["FSalesmanId"].ToString() + "'";
                else
                    filter += "'" + objs[i]["FSalesmanId"].ToString() + "',";

            }
            if(objs.Count <= 0)
            {
                filter += "0";
            }
            filter += ")";
            return filter;
        }

        /// <summary>
        /// 值更新-持有人
        /// </summary>
        /// <param name="e">DataChangedEventArgs</param>
        public void Act_DC_FCrmHolder(DataChangedEventArgs e)
        {
            string _oldVal = e.OldValue == null ? "0" : e.OldValue.ToString();
            string _newVal = e.NewValue.ToString();
            if (_newVal == "" || _newVal == "0")
            {
                this.View.Model.SetValue("FCrmHolder2", _oldVal);
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
            string _newVal = e.NewValue.ToString();
            if (_newVal == "" || _newVal == "0")
            {
                this.View.Model.SetValue("FCrmHdOrgID", _oldVal);
                return;
            }

            AL_HoldDept = HT_HoldOrg2ALDept[_newVal] as ArrayList;
            CZ_Rnd_AL2Str(AL_HoldDept, ref STR_HoldDept);
            string _localDeptID = AL_HoldDept[0].ToString();
            string _localCrmSN = HT_HoldDeptCrmSN[_localDeptID].ToString();

            this.View.Model.SetValue("FCrmHdDept", _localDeptID);
            this.View.Model.SetValue("FCrmSN", _localCrmSN);

            int _EHdRowCnt = this.View.Model.GetEntryRowCount(VAL_HoldEntity);
            if (_EHdRowCnt == 1)
            {
                this.View.Model.SetValue("FHHdOrgID", _newVal, 0);
                this.View.Model.SetValue("FHHdDept", _localDeptID, 0);
                this.View.Model.SetValue("FHSN", _localCrmSN, 0);
                this.View.Model.SetValue("FHBegDate", DateTime.Now.ToString("yyyy-MM-dd"), 0);
                this.View.Model.SetValue("FHEndDate", "9999-12-31", 0);
            }
        }

        /// <summary>
        /// 值更新-持有部门
        /// </summary>
        /// <param name="e"></param>
        public void Act_DC_FCrmHdDept(DataChangedEventArgs e)
        {
            string _oldVal = e.OldValue == null ? "0" : e.OldValue.ToString();
            string _newVal = e.NewValue.ToString();
            if (_newVal == "" || _newVal == "0")
            {
                this.View.Model.SetValue("FCrmHdDept", _oldVal);
                return;
            }

            string _localCrmSN = HT_HoldDeptCrmSN[_newVal].ToString();
            this.View.Model.SetValue("FCrmSN", _localCrmSN);

            int _EHdRowCnt = this.View.Model.GetEntryRowCount(VAL_HoldEntity);
            if (_EHdRowCnt == 1)
            {
                this.View.Model.SetValue("FHHdDept", _newVal, 0);
                this.View.Model.SetValue("FHSN", _localCrmSN, 0);
                this.View.Model.SetValue("FHBegDate", DateTime.Now.ToString("yyyy-MM-dd"), 0);
                this.View.Model.SetValue("FHEndDate", "9999-12-31", 0);
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
                this.View.Model.SetValue("FCrmHdOrgID", "0");
                this.View.Model.SetValue("FCrmHdDept", "0");
                this.View.Model.SetValue("FCrmHolder", "0");
                this.View.Model.SetValue("FCrmSN", "");
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
                    this.View.Model.SetValue("FCrmHdOrgID", _FHoldOrgID);
                    this.View.Model.SetValue("FCrmHdDept", _FHoldDeptID);
                    this.View.Model.SetValue("FCrmHolder", _FHolder);
                    this.View.Model.SetValue("FCrmSN", _FCrmSN);

                    _LocalOrgID = _FHoldOrgID;

                    int _EHdRowCnt = this.View.Model.GetEntryRowCount(VAL_HoldEntity);
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
        /// 基本方法 
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
