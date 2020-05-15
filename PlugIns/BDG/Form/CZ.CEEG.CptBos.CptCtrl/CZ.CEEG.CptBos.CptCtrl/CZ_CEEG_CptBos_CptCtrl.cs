using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Data;
using System.Collections;

using Kingdee.BOS.Core;
using Kingdee.BOS.Contracts;
using Kingdee.BOS.Core.Bill.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Core.Metadata;
using Kingdee.BOS.Core.SqlBuilder;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.App.Data;
using Kingdee.BOS.Core.DynamicForm;
using Kingdee.BOS.Util;

namespace CZ.CEEG.CptBos.CptCtrl
{
    [Description("资金占用控制")]
    [HotUpdate]
    public class CZ_CEEG_CptBos_CptCtrl : AbstractBillPlugIn
    {
        #region override
        public override void DataChanged(DataChangedEventArgs e)
        {
            base.DataChanged(e);
            if (this.View.Context.ClientType.ToString() != "Mobile" && IsUsingBdgSys())
            {
                if (e.Field.Key == "SETTLETYPEID") //结算方式
                {
                    // string FBraOffice = CZ_GetBaseData("FPAYORGID", "Id");
                    // string FSettleType = this.View.Model.GetValue("FSETTLETYPEID", e.Row) == null ? "" : (this.View.Model.GetValue("FSETTLETYPEID", e.Row) as DynamicObject)["Id"].ToString();
                    var obj = GetCapitalBalance();
                    if (obj.Count > 0)
                    {
                        string CptBalance = float.Parse(obj[0]["FOccBal"].ToString()).ToString("f2");
                        //string sql = "select FNAME from T_BD_SETTLETYPE_L where FID='" + FSettleType + "'";
                        //string FCostPrjName = CZDB_GetData(sql)[0]["FNAME"].ToString();
                        string msg = string.Format("本月资金占用余额为：{0}元。", CptBalance);
                        this.View.ShowMessage(msg);
                    }
                }
            }
        }

        public override void BeforeDoOperation(BeforeDoOperationEventArgs e)
        {
            base.BeforeDoOperation(e);
            if (IsUsingBdgSys())
            {
                switch (e.Operation.FormOperation.Operation.ToUpperInvariant())
                {
                    case "SAVE":
                        if (Check())
                        {
                            e.Cancel = true;
                        }
                        break;
                    case "SUBMIT":
                        if (Check())
                        {
                            e.Cancel = true;
                        }
                        break;
                }
            }
            
        }

        #endregion

        #region 业务函数
        /// <summary>
        /// 获取资金余额
        /// </summary>
        /// <returns></returns>
        private DynamicObjectCollection GetCapitalBalance()
        {
            string org = CZ_GetBaseData("FPAYORGID", "Id"); //付款组织
            string year = DateTime.Now.Year.ToString();
            string month = DateTime.Now.Month.ToString();
            string sql = string.Format(@"select FOccBal from ora_BDG_CapitalMD 
                                        where FYear='{0}' and FMonth='{1}' and FBraOffice='{2}';",
                                        year, month,org);
            
            return CZDB_GetData(sql);
        }

        private bool Check()
        {
            var obj = GetCapitalBalance();
            if (obj.Count == 0)
            {
                this.View.ShowMessage("本月资金明细单不存在！");
                return false;
            }
            float CptBalance = float.Parse(obj[0]["FOccBal"].ToString());
            float PayTotalMount = float.Parse(CZ_GetValue("FPAYTOTALAMOUNTFOR_H")); //应付金额
            float RealPayMount = float.Parse(CZ_GetValue("FREALPAYAMOUNTFOR_H"));   //实付金额

            if(CptBalance < RealPayMount)
            {
                string msg = "资金占用余额不足！\n目前资金占用余额为：" + CptBalance.ToString("0.00") + "元。";
                this.View.ShowMessage(msg);
                return true;
            }
            
            return false;
        }

        #endregion

        #region 基本取数方法
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
            return this.View.Model.DataObject[sign] == null ? "0" : (this.View.Model.DataObject[sign] as DynamicObject)[property].ToString();
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
    }
}
