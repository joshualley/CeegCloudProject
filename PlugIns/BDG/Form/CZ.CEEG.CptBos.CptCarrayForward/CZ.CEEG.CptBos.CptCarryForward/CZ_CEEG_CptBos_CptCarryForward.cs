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

namespace CZ.CEEG.CptBos.CptCarryForward
{
    [Description("资金月结转")]
    [HotUpdate]
    public class CZ_CEEG_CptBos_CptCarryForward : AbstractBillPlugIn
    {
        #region override
        public override void AfterBarItemClick(AfterBarItemClickEventArgs e)
        {
            base.AfterBarItemClick(e);
            if (this.Context.ClientType.ToString() != "Mobile")
            {
                switch (e.BarItemKey.ToUpperInvariant())
                {
                    case "TBCRYFWD": //tbCryFwd --资金结转
                        Act_CarryForward();
                        break;
                }
            }
        }

        #endregion

        private void Act_CarryForward()
        {
            string _FMonth = CZ_GetCommonField("FMonth");
            //if(DateTime.Now.Month == int.Parse(_FMonth) + 1)
            if (DateTime.Now.Month != 12)
            {
                string _FIsTurn = CZ_GetCommonField("FIsTurn");
                if (_FIsTurn == "False")
                {
                    string _FID = CZ_GetFID();
                    string _FCreatorId = this.Context.UserId.ToString();
                    string _FCreateOrgId = CZ_GetBaseData("FCreateOrgId", "Id");
                    string sql = string.Format("exec proc_czly_CapitalCarryForward @FID='{0}',@FCreatorId='{1}',@FCreateOrgId='{2}'",
                        _FID, _FCreatorId, _FCreateOrgId);
                    CZDB_GetData(sql);
                    this.View.Refresh();
                }
                else
                {
                    this.View.ShowMessage("本月资金已经结转到下月！");
                }
            }
            else
            {
                this.View.ShowMessage("当前月份不允许结转！");
            }
        }

        #region 基本取数方法
        /// <summary>
        /// 获取当前单据FID
        /// </summary>
        /// <returns></returns>
        public string CZ_GetFID()
        {
            return (this.View.Model.DataObject as DynamicObject)["Id"] == null ? "0" : (this.View.Model.DataObject as DynamicObject)["Id"].ToString();
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
