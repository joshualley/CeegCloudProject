using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using System.ComponentModel;

using Kingdee.BOS.Core;
using Kingdee.BOS.Core.Bill.PlugIn;
using Kingdee.BOS.Core.List.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Core.List.PlugIn.Args;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.Core.SqlBuilder;
using Kingdee.BOS.Core.Metadata;
using Kingdee.BOS.ServiceHelper;
using Kingdee.BOS.Core.DynamicForm.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.ControlModel;
using Kingdee.BOS.App.Data;
using Kingdee.BOS.Util;
using System.Threading.Tasks;

namespace CZ.CEEG.BosLst.FormClose
{
    [Description("单据关闭，释放预算")]
    [HotUpdate]
    public class CZ_CEEG_BosLst_FormClose : AbstractListPlugIn
    {
        public override void AfterBarItemClick(AfterBarItemClickEventArgs e)
        {
            base.AfterBarItemClick(e);
            switch (e.BarItemKey.ToUpperInvariant())
            {
                case "TBCLOSEONE": //tbCloseOne
                    Act_CloseForm();
                    break;
                case "TBCLOSEALL": //tbCloseAll
                    Act_CloseFormAll();
                    break;
            }
            
        }

        #region Functions
        /// <summary>
        /// 预算一键释放
        /// </summary>
        private void Act_CloseFormAll()
        {
            if (!IsUsingBdgSys())
            {
                this.View.ShowWarnningMessage("系统未开启预算控制！");
                return;
            }
            string FBraOffice = "0";
            string FDSrcType = "立项";
            string FDSrcAction = "关闭";
            string FDSrcBillID = this.View.GetFormId();
            string FDSrcFID = "";
            string FDSrcBNo = "";

            string FDSrcEntryID = "0";
            string FDSrcSEQ = "0";
            string FDCostPrj = "";
            string FPreCost = "0";
            string FReCost = "0";
            string FNote = "";

            string sql = "";

            //限定范围：立项单据
            //获取单据是否存在表体，及表名
            string[] tbName = GetTbNameByFormId()[this.View.GetFormId()];
            bool hasEntry = tbName[1] == "" ? false : true;
            //获取单据状态（根据转换字典获取不同单据的单据状态字段名）
            Dictionary<string, string> Trans = Transform();
            string FSourceStatus = "";
            var IDs = GetAllIDs(tbName, Trans["FSourceStatus"]);
            foreach (var ID in IDs)
            {
                var data = DB_GetFormData(ID);

                FSourceStatus = data[Trans["FSourceStatus"]].ToString();
                FDSrcBNo = data["FBillNo"].ToString();
                FDSrcFID = hasEntry ? data["FID"].ToString() : ID;
                FDSrcEntryID = hasEntry ? ID : "0";
                FBraOffice = data[Trans["FBraOffice"]].ToString();
                FDSrcSEQ = hasEntry ? data["FSEQ"].ToString() : "0";
                FDCostPrj = data[Trans["FDCostPrj"]].ToString();
                FPreCost = data[Trans["FPreCost"]].ToString();
                FReCost = data[Trans["FReCost"]].ToString();
                sql += String.Format(@"exec proc_czly_InsertBudgetFlowS
	                            @FBraOffice='{0}',@FDSrcType='{1}',@FDSrcAction='{2}',@FDSrcBillID='{3}',
	                            @FDSrcFID='{4}',@FDSrcBNo='{5}',@FDSrcEntryID='{6}',@FDSrcSEQ='{7}',
	                            @FDCostPrj='{8}',@FPreCost='{9}',@FReCost='{10}',@FNote='{11}';
                            ", FBraOffice, FDSrcType, FDSrcAction, FDSrcBillID,
                                FDSrcFID, FDSrcBNo, FDSrcEntryID, FDSrcSEQ,
                                FDCostPrj, FPreCost, FReCost, FNote);
            }
            if(IDs.Count > 0)
            {
                CZDB_GetData(sql);
                this.View.ShowMessage("预算已释放！");
            }
            else
            {
                this.View.ShowMessage("没有可释放预算的单据！");
            }
            
        }

        /// <summary>
        /// 并发获取本单据所有未释放预算的主键及单据体主键
        /// </summary>
        /// <param name="tbName"></param>
        /// <param name="FSourceStatusSign"></param>
        /// <returns></returns>
        private List<string> GetAllIDs(string[] tbName, string FSourceStatusSign)
        {
            string sql = "";
            bool hasEntry = tbName[1] == "" ? false : true;
            if (!hasEntry)
            {
                sql = "select *, 0 as FEntryID from " + tbName[0] + " where " + FSourceStatusSign + "=1";
            }
            else
            {
                sql = "select FID, FEntryID from " + tbName[1] + " where " + FSourceStatusSign + "=1";
            }
            var data = CZDB_GetData(sql);
            //并行过滤出可以释放预算的ID
            List<string[]> ids = new List<string[]>();
            for(int i = 0; i < data.Count; i++)
            {
                ids.Add(new string[] { data[i]["FID"].ToString(), data[i]["FEntryID"].ToString() });
            }
            List<string> IDs = new List<string>();
            Parallel.ForEach(ids, idArr =>
            {
                if(IsFreeBdg(idArr[0], idArr[1]))
                {
                    if (hasEntry)
                    {
                        IDs.Add(idArr[1]);
                    }
                    else
                    {
                        IDs.Add(idArr[0]);
                    }
                }
            });
   
            return IDs;
        }

        /// <summary>
        /// 预算选择释放
        /// </summary>
        private void Act_CloseForm()
        {
            if (!IsUsingBdgSys())
            {
                this.View.ShowWarnningMessage("系统未开启预算控制！");
                return;
            }
            string FBraOffice = "0";
            //string FYear = "0";
            //string FMonth = "0";
            string FDSrcType = "立项";
            string FDSrcAction = "关闭";
            string FDSrcBillID = this.View.GetFormId();
            string FDSrcFID = "";
            string FDSrcBNo = "";

            string FDSrcEntryID = "0";
            string FDSrcSEQ = "0";
            string FDCostPrj = "";
            string FPreCost = "0";
            string FReCost = "0";
            string FNote = "释放预算";

            string sql = "";

            //限定范围：立项单据
            //获取单据是否存在表体，及表名
            string[] tbName = GetTbNameByFormId()[this.View.GetFormId()];
            bool hasEntry = tbName[1] == "" ? false : true;
            //获取单据状态（根据转换字典获取不同单据的单据状态字段名）
            Dictionary<string, string> Trans = Transform();
            string FSourceStatus = "";
            string[] IDs = hasEntry ? CZ_GetSelectedRowsFEntryID() : CZ_GetSelectedRowsFID();
            if(IDs.Length <= 0)
            {
                this.View.ShowMessage("请选择释放预算的单据！");
                return;
            }
            List<string> BillNos = new List<string>();

            foreach (var ID in IDs)
            {
                var data = DB_GetFormData(ID);
                
                FSourceStatus = data[Trans["FSourceStatus"]].ToString();
                FDSrcBNo = data["FBillNo"].ToString();
                //判断单据（行）是否已经下推
                FDSrcFID = hasEntry ? data["FID"].ToString() : ID;
                FDSrcEntryID = hasEntry ? ID : "0";
                if (FSourceStatus == "1" && IsFreeBdg(FDSrcFID, FDSrcEntryID))
                {
                    FBraOffice = data[Trans["FBraOffice"]].ToString();
                    FDSrcSEQ = hasEntry ? data["FSEQ"].ToString() : "0";
                    FDCostPrj = data[Trans["FDCostPrj"]].ToString();
                    FPreCost = data[Trans["FPreCost"]].ToString();
                    FReCost = data[Trans["FReCost"]].ToString();
                    sql += String.Format(@"exec proc_czly_InsertBudgetFlowS
	                             @FBraOffice='{0}',@FDSrcType='{1}',@FDSrcAction='{2}',@FDSrcBillID='{3}',
	                             @FDSrcFID='{4}',@FDSrcBNo='{5}',@FDSrcEntryID='{6}',@FDSrcSEQ='{7}',
	                             @FDCostPrj='{8}',@FPreCost='{9}',@FReCost='{10}',@FNote='{11}';
                                ", FBraOffice, FDSrcType, FDSrcAction, FDSrcBillID,
                                    FDSrcFID, FDSrcBNo, FDSrcEntryID, FDSrcSEQ,
                                    FDCostPrj, FPreCost, FReCost, FNote);
                }
                else
                {
                    BillNos.Add(FDSrcBNo);
                }
            }
            if(sql != "")
            {
                CZDB_GetData(sql);
            }
            if (BillNos.Count > 0)
            {
                string bs = "";
                foreach (var b in BillNos)
                {
                    bs += b + "、";
                }
                bs.TrimEnd('、');
                this.View.ShowErrMessage("单据" + bs + ",预算占用不能释放或已经释放！");
            }
        }
        /// <summary>
        /// 根据行查流水判断占用是否释放
        /// </summary>
        /// <returns></returns>
        private bool IsFreeBdg(string FID, string FEntryID)
        {
            string FFormID = this.View.GetFormId();
            string sql = string.Format("EXEC proc_czly_IsFreeBdgOcc @FID='{0}', @FEntryID='{1}', @FFormID='{2}';",
                                        FID, FEntryID, FFormID);
            return bool.Parse(CZDB_GetData(sql)[0]["FResult"].ToString());
        }

        /// <summary>
        /// 根据formID获取表名
        /// </summary>
        /// <returns></returns>
        private Dictionary<string, string[]> GetTbNameByFormId()
        {
            var dict = new Dictionary<string, string[]>();
            //对公费用立项
            dict.Add("kaa55d0cac0c5447bbc6700cfbdf0b11e", new string[] { "ora_t_PublicApply", "" });
            //对公费用报销
            dict.Add("k5c88e2dc1ac14349935d452e74e152c8", new string[] { "ora_t_PublicSubmit", "ora_t_PublicSubmitEntry" });
            //对公资金申请
            dict.Add("k191b3057af6c4252bcea813ff644cd3a", new string[] { "ora_t_Cust100011", "" });
            //出差申请
            dict.Add("k0c30c431418e4cf4a60d241a18cb241c", new string[] { "ora_t_TravelApply", "ora_t_TravelApplyEntry" });
            //出差报销
            dict.Add("k6575db4ed77c449f88dd20cceef75a73", new string[] { "ora_t_TravelSubmit", "ora_t_TravelSubmitEntry" });
            //个人费用立项
            dict.Add("ke6d80dfd260e4ef88d75f69f4c7ef0a1", new string[] { "ora_t_PeronCostApplyHead", "" });
            //个人费用报销
            dict.Add("k767a317ad28e40f1b25e95b92e218fea", new string[] { "ora_t_PersonalReimburse", "ora_t_PCostReimburse" });
            //个人资金借支
            dict.Add("k0c6b452fa8154c4f8e8e5f55f96bcfac", new string[] { "ora_t_PersonMoney", "" });
            //招待费用申请
            dict.Add("k1ae2591790044d95b9966ad0dff1d987", new string[] { "ora_t_ServeFee", "" });
            //招待费用报销
            dict.Add("kdcdde6ac18cb4d419a6924b49a593460", new string[] { "ora_t_Server", "ora_t_Server_Entry" });
            //采购合同评审
            dict.Add("k3972241808034802b04c3d18d4107afd", new string[] { "ora_t_PCReview", "ora_t_PCReviewEntry" });
            //销售合同评审
            dict.Add("kdb6ae742543a4f6da09dfed7ba4e02dd", new string[] { "ora_t_SellContractHead", "ora_t_SellContractEntry" });

            return dict;
        }

        /// <summary>
        /// 获取单据所有相关的表名
        /// </summary>
        /// <returns></returns>
        private List<string> GetTbNames()
        {
            string formId = this.View.GetFormId();
            string sql = "select FKERNELXML.query('//TableName') TbName from T_META_OBJECTTYPE where FID='" + formId + "'";
            string TbNameXML = CZDB_GetData(sql)[0]["TbName"].ToString();
            XElement xe = XElement.Parse(TbNameXML);
            var tbNames = xe.Elements("TableName").Cast<string>().ToList();
            return tbNames;
        }
        /// <summary>
        /// 获取本单据的表头或某一行数据
        /// </summary>
        /// <param name="isAll">为false只返回表头数据</param>
        /// <returns></returns>
        private DynamicObject DB_GetFormData(string ID, bool isAll = true)
        {

            //string FID = CZ_GetFID();
            string[] tb = GetTbNameByFormId()[this.View.GetFormId()];
            string t_head = tb[0];
            string t_entry = tb[1];
            string sql = "";
            if (!isAll)
            {
                sql = string.Format("select * from {0} where FID='{1}'", t_head, ID);
            }
            else
            {
                if (t_entry == "")
                {
                    sql = string.Format("select * from {0} where FID='{1}'", t_head, ID);
                }
                else
                {
                    sql = string.Format(@"select * from {0} h 
                                    inner join {1} e on h.FID=e.FID
                                    where e.FEntryID='{2}'", t_head, t_entry, ID);
                }
            }

            var objs = CZDB_GetData(sql);
            return objs[0];
        }

        /// <summary>
        /// 使用字典统一单据字段
        /// </summary>
        /// <returns></returns>
        private Dictionary<string, string> Transform()
        {
            string FormId = this.View.GetFormId();
            var dict = new Dictionary<string, string>();
            switch (FormId)
            {
                case "kaa55d0cac0c5447bbc6700cfbdf0b11e"://对公费用立项
                    dict.Add("FBraOffice", "FOrgId");
                    dict.Add("FTPreCost", "FExpectCost1");
                    dict.Add("FTReCost", "FCommitAmount");
                    dict.Add("FCostPrj", "FCostType1");
                    dict.Add("FPreCost", "FExpectCost1");
                    dict.Add("FReCost", "FCommitAmount");
                    dict.Add("FSourceStatus", "FCommitStatus");
                    break;
                case "k5c88e2dc1ac14349935d452e74e152c8"://对公费用报销
                    dict.Add("FBraOffice", "FOrgId");
                    dict.Add("FTPreCost", "FTotalAmount");
                    dict.Add("FTReCost", "FTCkAmount");
                    dict.Add("FCostPrj", "FCostType");
                    dict.Add("FPreCost", "FRealAmount");
                    dict.Add("FReCost", "FCheckAmount");
                    break;
                case "k191b3057af6c4252bcea813ff644cd3a"://对公资金申请
                    break;
                case "k0c30c431418e4cf4a60d241a18cb241c"://出差申请
                    dict.Add("FBraOffice", "FOrgId");
                    dict.Add("FTPreCost", "F_ora_Amount");
                    dict.Add("FTReCost", "FActualAmount");
                    dict.Add("FCostPrj", "FCostType");
                    dict.Add("FPreCost", "FExpectCost");
                    dict.Add("FReCost", "FActualCost");
                    dict.Add("FSourceStatus", "FSourceStatus");
                    break;
                case "k6575db4ed77c449f88dd20cceef75a73"://出差报销
                    dict.Add("FBraOffice", "FOrgId");
                    dict.Add("FTPreCost", "FTotalAmount");
                    dict.Add("FTReCost", "FTCkAmount");
                    dict.Add("FCostPrj", "FCostType");
                    dict.Add("FPreCost", "FCarAmount");
                    dict.Add("FReCost", "FCheckAmount");
                    break;
                case "ke6d80dfd260e4ef88d75f69f4c7ef0a1"://个人费用立项
                    dict.Add("FBraOffice", "FOrgId");
                    dict.Add("FTPreCost", "FPREMONEY1");
                    dict.Add("FTReCost", "FCommitAmount");
                    dict.Add("FCostPrj", "FCostType1");
                    dict.Add("FPreCost", "FPREMONEY1");
                    dict.Add("FReCost", "FCommitAmount");
                    dict.Add("FSourceStatus", "FSourcelStatus");
                    break;
                case "k767a317ad28e40f1b25e95b92e218fea"://个人费用报销
                    dict.Add("FBraOffice", "FOrgId");
                    dict.Add("FTPreCost", "FTotalAmount");
                    dict.Add("FTReCost", "FTCkAmount");
                    dict.Add("FCostPrj", "FCostItem");
                    dict.Add("FPreCost", "FAmount");
                    dict.Add("FReCost", "FCheckAmount");
                    break;
                case "k0c6b452fa8154c4f8e8e5f55f96bcfac"://个人资金借支
                    dict.Add("FBraOffice", "FOrgId");
                    dict.Add("FTPreCost", "FExpectCost1");
                    dict.Add("FTReCost", "FCommitAmount");
                    dict.Add("FCostPrj", "FCostType1");
                    dict.Add("FPreCost", "FExpectCost1");
                    dict.Add("FReCost", "FCommitAmount");
                    break;
                case "k1ae2591790044d95b9966ad0dff1d987"://招待费用申请
                    dict.Add("FBraOffice", "F_ora_OrgId");
                    dict.Add("FTPreCost", "F_ora_Amount");
                    dict.Add("FTReCost", "FACTUALCOST");
                    dict.Add("FCostPrj", "FCostType");
                    dict.Add("FPreCost", "F_ora_Amount");
                    dict.Add("FReCost", "FACTUALCOST");
                    dict.Add("FSourceStatus", "FSourceStatus");
                    break;
                case "kdcdde6ac18cb4d419a6924b49a593460"://招待费用报销
                    dict.Add("FBraOffice", "F_ora_OrgId");
                    dict.Add("FTPreCost", "FTotalAmount");
                    dict.Add("FTReCost", "FTCkAmount");
                    dict.Add("FCostPrj", "FCostType1");
                    dict.Add("FPreCost", "F_ora_Money");
                    dict.Add("FReCost", "FCheckAmount");
                    break;
                case "k3972241808034802b04c3d18d4107afd"://采购合同评审
                    break;
                case "kdb6ae742543a4f6da09dfed7ba4e02dd"://销售合同评审
                    break;

            }
            return dict;
        }
        #endregion

        #region 基本取数方法
        /// <summary>
        /// 获取当前单据FID
        /// </summary>
        /// <returns></returns>
        public string CZ_GetFID()
        {
            return (this.View.Model.DataObject as DynamicObject)["Id"] == null ? "0" : (this.View.Model.DataObject as DynamicObject)["Id"].ToString();
        }
        public string CZ_GetValue(string sign)
        {
            return this.View.Model.GetValue(sign) == null ? "" : this.View.Model.GetValue(sign).ToString();
        }
        /// <summary>
        /// 获取基础资料
        /// </summary>
        /// <param name="sign">标识</param>
        /// <param name="property">属性</param>
        /// <returns></returns>
        private string CZ_GetBaseData(string sign, string property)
        {
            return this.View.Model.DataObject[sign] == null ? "" : (this.View.Model.DataObject[sign] as DynamicObject)[property].ToString();
        }
        /// <summary>
        /// 获取一般字段
        /// </summary>
        /// <param name="sign">标识</param>
        /// <returns></returns>
        private string CZ_GetCommonField(string sign)
        {
            return this.View.Model.DataObject[sign] == null ? "" : this.View.Model.DataObject[sign].ToString();
        }
        #endregion

        #region 获取选中行信息

        /// <summary>
        /// 获取当前选中行内码
        /// </summary>
        /// <returns></returns>
        private string CZ_GetCurrentRowFID()
        {
            return this.ListView.CurrentSelectedRowInfo.PrimaryKeyValue;
        }
        /// <summary>
        /// 获取当前选中行FEntryID
        /// </summary>
        /// <returns></returns>
        private string CZ_GetCurrentRowFEntryID()
        {
            return this.ListView.CurrentSelectedRowInfo.EntryPrimaryKeyValue;
        }

        /// <summary>
        /// 获取所有勾选行内码
        /// </summary>
        /// <returns></returns>
        private string[] CZ_GetSelectedRowsFID()
        {
            return this.ListView.SelectedRowsInfo.GetPrimaryKeyValues();
        }

        /// <summary>
        /// 获取所有勾选行FEntryID
        /// </summary>
        /// <returns></returns>
        private string[] CZ_GetSelectedRowsFEntryID()
        {
            return this.ListView.SelectedRowsInfo.GetEntryPrimaryKeyValues();
        }

        /// <summary>
        /// 获取所有勾选行单据编号
        /// </summary>
        /// <returns></returns>
        private DynamicObjectCollection CZ_GetSelectedRowsBillNo()
        {
            string filter = "FID in (";
            string[] fids = CZ_GetSelectedRowsFID();
            foreach (var fid in fids)
            {
                filter += fid + ",";
            }
            filter = filter.TrimEnd(',') + ")";
            QueryBuilderParemeter param = new QueryBuilderParemeter()
            {
                FormId = this.View.GetFormId(),
                FilterClauseWihtKey = filter,
                SelectItems = SelectorItemInfo.CreateItems("FBILLNO"),
            };

            return QueryServiceHelper.GetDynamicObjectCollection(this.Context, param);
        }
        #endregion

        #region CZTY Action Base
        /// <summary>
        /// 是否使用预算系统
        /// </summary>
        /// <returns></returns>
        private bool IsUsingBdgSys()
        {
            string sql = "EXEC proc_cz_ly_IsUsingBdgSys";
            return CZDB_GetData(sql)[0]["FSwitch"].ToString() == "1" ? true : false;
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
