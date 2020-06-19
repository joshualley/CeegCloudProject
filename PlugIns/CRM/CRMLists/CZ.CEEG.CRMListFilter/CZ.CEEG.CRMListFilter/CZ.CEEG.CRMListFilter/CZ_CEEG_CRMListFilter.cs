using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Kingdee.BOS.Core;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Core.Metadata;
using Kingdee.BOS.FileServer.Core;
using Kingdee.BOS.FileServer.Core.Object;
using Kingdee.BOS.FileServer.ProxyService;
using Kingdee.BOS.KDThread;
using Kingdee.BOS.Mobile.Metadata.ControlDataEntity;
using Kingdee.BOS.Mobile.PlugIn;
using Kingdee.BOS.Mobile.PlugIn.ControlModel;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.ServiceHelper;
using Kingdee.BOS.Util;
using Kingdee.BOS.App.Data;


namespace CZ.CEEG.CRMListFilter
{
    [Description("CRM移动端单据列表进行列表过滤")]
    [HotUpdate]
    public class CZ_CEEG_CRMListFilte :AbstractMobileListPlugin
    {


        string Str_Filter = "";

        public override void ButtonClick(ButtonClickEventArgs e)
        {
            base.ButtonClick(e);
            
            switch (e.Key.ToString().ToUpperInvariant())
            {
                case "TBBMYALL":
                    //tbbMyAll      全部
                    Act_ABIC_tbbMyAll();
                    this.View.RefreshByFilter();
                    break;
                case "TBBSOBID":
                    //tbbSoBid      全部招投标
                    Act_ABIC_tbbSoBid();
                    this.View.RefreshByFilter();
                    break;
                case "TBBSOOFFER":
                    //tbbSoOffer      全部报价
                    Act_ABIC_tbbSoOffer();
                    this.View.RefreshByFilter();
                    break;
                case "TBBMYCREATE":
                    //tbbMyCreate   我创建的 
                    Act_ABIC_tbbMyCreate();
                    this.View.RefreshByFilter();
                    break;
                case "TBBMYHOLD":
                    //tbbMyHold     我持有的
                    Act_ABIC_tbbMyHold();
                    this.View.RefreshByFilter();
                    break;
                case "TBBMYCTRL":
                    //tbbMyCtrl     我管理的
                    Act_ABIC_tbbMyCtrl();
                    this.View.RefreshByFilter();
                    break;
                default:
                    break;
            }
        }

        public override void AfterBarItemClick(AfterBarItemClickEventArgs e)
        {
            base.AfterBarItemClick(e);
            
        }

        public override void PrepareFilterParameter(Kingdee.BOS.Core.List.PlugIn.Args.FilterArgs e)
        {
            base.PrepareFilterParameter(e);
            if (Str_Filter == ""||Str_Filter==null)
            {
                //Act_ABIC_tbbMyAll();
                Act_ABIC_tbbMyHold();
            }
            addTimeOutFilter();
            e.AppendQueryFilter(Str_Filter);
            e.AppendQueryOrderby(" FCreateDate DESC");
        }

        #region Action
        /// <summary>
        /// tbbSoBid 报价招投标     
        /// </summary>
        private void Act_ABIC_tbbSoBid()
        {
            string _userID = this.Context.UserId.ToString();
            string _FCrmR = this.Act_GetSchRoleFilter(_userID, "or");
            string _FilterStr = " (FCreatorID='" + _userID + "' or FCrmHolder='" + _userID + "'" + _FCrmR + ") and FIsBid=1";
            Str_Filter = _FilterStr;
            addTimeOutFilter();
        }

        /// <summary>
        /// tbbSoOffer 报价     
        /// </summary>
        private void Act_ABIC_tbbSoOffer()
        {
            string _userID = this.Context.UserId.ToString();
            string _FCrmR = this.Act_GetSchRoleFilter(_userID, "or");
            string _FilterStr = " (FCreatorID='" + _userID + "' or FCrmHolder='" + _userID + "'" + _FCrmR + ") and FIsBid=0";
            Str_Filter = _FilterStr;
            addTimeOutFilter();
        }


        /// <summary>
        /// tbbMyAll      全部
        /// </summary>
        private void Act_ABIC_tbbMyAll()
        {
            string _userID = this.Context.UserId.ToString();
            string _FCrmR = this.Act_GetSchRoleFilter(_userID, "or");
            string _FilterStr = " FCreatorID='" + _userID + "' or FCrmHolder='" + _userID + "'" + _FCrmR;
            Str_Filter = _FilterStr;
            addTimeOutFilter();
        }

        /// <summary>
        /// tbbMyCreate   我创建的 
        /// </summary>
        private void Act_ABIC_tbbMyCreate()
        {
            string _userID = this.Context.UserId.ToString();
            Str_Filter = " FCreatorID='" + _userID + "'";
            addTimeOutFilter();
        }

        /// <summary>
        /// tbbMyHold     我持有的
        /// </summary>
        private void Act_ABIC_tbbMyHold()
        {
            string _userID = this.Context.UserId.ToString();
            Str_Filter = " FCrmHolder='" + _userID + "'";
            addTimeOutFilter();
        }

        /// <summary>
        /// tbbMyCtrl     我管理的
        /// </summary>
        private void Act_ABIC_tbbMyCtrl()
        {
            string _userID = this.Context.UserId.ToString();
            string _FCrmR = this.Act_GetSchRoleFilter(_userID, "");
            Str_Filter = _FCrmR;
            addTimeOutFilter();
        }
        #endregion



        #region 到期后不再显示
        /// <summary>
        /// 添加超时过滤，线索超过有效期后，创建人可见，持有人不可见
        /// </summary>
        private void addTimeOutFilter()
        {
            string formId = this.View.GetFormId();
            if(formId == "ora_CRM_Clue")
            {
                string _currDate = DateTime.Now.ToString("yyyy-MM-dd");
                string _userID = this.Context.UserId.ToString();
                Str_Filter = "(" + Str_Filter + ")";
                Str_Filter += string.Format(@" and DATEDIFF(d, FClueEndDt, (case when FCrmHolder='{0}' and FCreatorID!='{1}' then '{2}' else '1900-01-01' end))<=0 ", _userID, _userID, _currDate);
            }
        }
        #endregion


        #region Base Functions
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
                //return null;
                throw _ex;
            }
        }
       
        /// <summary>
        /// Crm授权组织-部门  查询权
        /// </summary>
        /// <param name="_userID">User ID  EXP=this.Context.UserId.ToString();</param>
        /// <param name="_BefWhile">追加前导 or|and</param>
        /// <returns>授权CrmSN过滤串</returns>
        private string Act_GetSchRoleFilter(string _userID, string _BefWhile)
        {
            string _Filter = "";    //(charIndex('.Z1.',FCrmSN)>0 
            string _rowCrmSN = "";
            string _sql = "exec proc_cztyCrm_GetCrmSN4U @FUserID='" + _userID + "'";
            DataTable _dt = this.CZDB_SearchBase(_sql);
            if (_dt.Rows.Count == 0)
            {
                return " " + _BefWhile + " 1<>1";
            }

            foreach (DataRow _dr in _dt.Rows)
            {
                _rowCrmSN = _dr["FCrmSN"].ToString();
                _rowCrmSN = "charIndex('" + _rowCrmSN + "',FCrmSN)>0";
                if (_Filter == "")
                {
                    _Filter = _rowCrmSN;
                }
                else
                {
                    _Filter = _Filter + " or " + _rowCrmSN;
                }
            }

            _Filter = " (" + _Filter + ")";
            if (_BefWhile != "")
            {
                _Filter = " " + _BefWhile + " " + _Filter;
            }
            return _Filter;
        }

        #endregion

    }
}
