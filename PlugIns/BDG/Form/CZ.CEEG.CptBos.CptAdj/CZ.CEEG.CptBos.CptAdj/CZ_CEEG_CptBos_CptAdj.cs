using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System;

using Kingdee.BOS.Core;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Core.Metadata;
using Kingdee.BOS.Core.Bill.PlugIn;
using Kingdee.BOS.FileServer.Core;
using Kingdee.BOS.FileServer.Core.Object;
using Kingdee.BOS.FileServer.ProxyService;
using Kingdee.BOS.KDThread;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.ServiceHelper;
using Kingdee.BOS.Util;
using Kingdee.BOS.App.Data;

namespace CZ.CEEG.CptBos.CptAdj
{
    [Description("资金调拨")]
    [HotUpdate]
    public class CZ_CEEG_CptBos_CptAdj : AbstractBillPlugIn
    {
        #region override
        public override void AfterBindData(EventArgs e)
        {
            base.AfterBindData(e);
            string FDocumentStatus = CZ_GetCommonField("DocumentStatus");
            if (FDocumentStatus == "Z")
            {
                DateTime currTime = DateTime.Now;
                this.View.Model.SetValue("FYear", currTime.Year.ToString());
                this.View.Model.SetValue("FMonth", currTime.Month.ToString());
            }
        }

        public override void AfterBarItemClick(AfterBarItemClickEventArgs e)
        {
            base.AfterBarItemClick(e);
            switch (e.BarItemKey.ToUpperInvariant())
            {
                case "TBUPDATECPT": //tbUpdateCpt
                    Act_CptTrans();
                    break;
            }
        }

        public override void BeforeDoOperation(BeforeDoOperationEventArgs e)
        {
            base.BeforeDoOperation(e);
            switch (e.Operation.FormOperation.Operation.ToUpperInvariant())
            {
                case "SUBMIT":
                    if (Check())
                    {
                        e.Cancel = true;
                    }
                    break;
            }
        }
        #endregion

        #region Actions
        private void Act_CptTrans()
        {
            string DocumentStatus = CZ_GetCommonField("DocumentStatus");
            string FIsDoBG = CZ_GetCommonField("FIsDoBG");
            if (DocumentStatus == "C")
            {
                if (FIsDoBG == "True")
                {
                    this.View.ShowMessage("资金已经通过此单进行了调拨！");
                    return;
                }
                string FBraOffice = CZ_GetBaseData("FBraOffice", "Id");
                //string FYear = CZ_GetCommonField("FYear");
                //string FMonth = CZ_GetCommonField("FMonth");
                string FDSrcType = "调拨";
                string FDSrcAction = "调整";
                string FDSrcBillID = "ora_BDG_BudgetAdj";
                string FDSrcFID = CZ_GetFID();
                string FDSrcBNo = CZ_GetValue("FBillNo");

                string FDSrcEntryID = "0";
                string FDSrcSEQ = "0";
                string FDCptType = "";
                string FPreCost = "0";
                string FNote = "";
                var FEntity = this.View.Model.DataObject["FEntity"] as DynamicObjectCollection;
                string FDirection = CZ_GetCommonField("FDirection");
                string sql = "";
                for (int i = 0; i < FEntity.Count; i++)
                {
                    FDSrcEntryID = FEntity[i]["Id"].ToString(); 
                    FDSrcSEQ = FEntity[i]["Seq"].ToString();
                    FDCptType = FEntity[i]["FECptType"] == null ? "0" : FEntity[i]["FECptType"].ToString();
                    FPreCost = FDirection == "1" ? FEntity[i]["FETransAmt"].ToString() : "-" + FEntity[i]["FETransAmt"].ToString();
                    
                    FNote = FEntity[i]["FEText"] == null ? "" : FEntity[i]["FEText"].ToString();
                    sql += String.Format(@"exec proc_czly_InsertCapitalFlowS
	                                        @FBraOffice='{0}',@FDSrcType='{1}',
	                                        @FDSrcAction='{2}',@FDSrcBillID='{3}',
	                                        @FDSrcFID='{4}',@FDSrcBNo='{5}',
	                                        @FDSrcEntryID='{6}',@FDSrcSEQ='{7}',
	                                        @FDCptType='{8}',@FPreCost='{9}',@FNote='{10} ';
                                          ", FBraOffice, FDSrcType, FDSrcAction, FDSrcBillID,
                                            FDSrcFID, FDSrcBNo, FDSrcEntryID, FDSrcSEQ,
                                            FDCptType, FPreCost, FNote);
                }
                sql += "update ora_BDG_CptTrans set FIsDoBG=1 where FID='" + FDSrcFID + "'";
                CZDB_GetData(sql);
                this.View.ShowMessage("已更新资金！");
                this.View.Refresh();
            }
            else
            {
                this.View.ShowMessage("单据审核后才能进行调拨！");
            }
        }

        private bool Check()
        {
            var FEntity = this.View.Model.DataObject["FEntity"] as DynamicObjectCollection;
            string FBraOffice = CZ_GetBaseData("FBraOffice", "Id");
            foreach (var row in FEntity)
            {
                string FCptType = row["FECptType"].ToString();
                var obj = GetCapitalBalance(FBraOffice, FCptType);
                string sql = string.Format("/*dialect*/select dbo.GetName('ENUM','{0}','结算方式类别') FName", FCptType);
                string FCptTypeName = CZDB_GetData(sql)[0]["FName"].ToString();
                if (obj.Count > 0)
                {
                    float FEUseBal = float.Parse(obj[0]["FEUseBal"].ToString());  //资金使用余额
                    float FETransAmt = float.Parse(row["FETransAmt"].ToString()); //调拨金额
                    string FDirection = CZ_GetCommonField("FDirection");
                    FETransAmt = FDirection == "1" ? FETransAmt : -FETransAmt;

                    if (FEUseBal + FETransAmt < 0)
                    {
                        string msg = string.Format("调拨后资金不能小于0!\n资金类型：{0}，本月资金实际余额为：{1}元。", FCptTypeName, FEUseBal.ToString("f2"));
                        this.View.ShowErrMessage(msg);
                        return true;
                    }

                }
                else
                {
                    this.View.ShowErrMessage("当前月份资金明细单中不存在此资金类型: " + FCptTypeName + "!");
                    return true;
                }
            }
            return false;
        }


        private DynamicObjectCollection GetCapitalBalance(string FBraOffice, string FCptType="")
        {
            //var currTime = DateTime.Now;
            string FYear = CZ_GetCommonField("FYear");
            string FMonth = CZ_GetCommonField("FMonth");
            string sql = "";
            if (FCptType == "")
            {
                sql = string.Format(@"select FOccBal,FUseBal from ora_BDG_CapitalMD 
                                    where FBraOffice='{0}' and FYear='{1}' and FMonth='{2}'",
                                    FBraOffice, FYear, FMonth);
            }
            else
            {
                sql = string.Format(@"select FEOccBal,FEUseBal from ora_BDG_CapitalMDEntry 
                                where FEBraOffice='{0}' and FEYear='{1}' and FEMonth='{2}' and FECptType='{3}'",
                                FBraOffice, FYear, FMonth, FCptType);
            }

            var obj = CZDB_GetData(sql);
            return obj;
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

        #region 数据库查询
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
