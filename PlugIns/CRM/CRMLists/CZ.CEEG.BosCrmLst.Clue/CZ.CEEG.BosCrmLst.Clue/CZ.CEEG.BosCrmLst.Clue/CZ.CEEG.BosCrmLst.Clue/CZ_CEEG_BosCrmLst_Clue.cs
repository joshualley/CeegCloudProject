using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Data;

using Kingdee.BOS.Core.Bill.PlugIn;
using Kingdee.BOS.Core.List.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Core.List.PlugIn.Args;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.Core.SqlBuilder;
using Kingdee.BOS.Core.Metadata;

//using Kingdee.BOS.Core.DynamicForm.PlugIn;
//using Kingdee.BOS.Core.DynamicForm.PlugIn.ControlModel;
using Kingdee.BOS.App.Data;
using Kingdee.BOS.Core;

namespace CZ.CEEG.BosCrmLst.Clue
{
    /// <summary>
    /// BOS_CRM_List 线索列表
    /// </summary>
    [Description("BOS_CRM_List 线索列表")]
    [Kingdee.BOS.Util.HotUpdate]
    public class CZ_CEEG_BosCrmLst_Clue: AbstractListPlugIn
    {
        string Str_Filter = "";

        #region override
        /// <summary>
        /// 菜单点击事件，表单插件同样适用
        /// </summary>
        /// <param name="e"></param>
        public override void BarItemClick(BarItemClickEventArgs e)
        {
            switch (e.BarItemKey.ToUpperInvariant())
            {
                //case "TBDELETE": 列表工具栏按钮事件，通过按钮Key[大写]来区分那个按钮事件
                //break;
                case "":
                    break;
                default:
                    break;
            }
        }
        /// <summary>
        /// 菜单点击后处理事件，表单插件同样适用
        /// </summary>
        /// <param name="e"></param>
        public override void AfterBarItemClick(AfterBarItemClickEventArgs e)
        {
            switch (e.BarItemKey.ToUpperInvariant())
            {
                case "TBBMYALL":
                    //tbbMyAll      全部线索
                    Act_ABIC_tbbMyAll();
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

        /// <summary>
        /// 对列表数据追加过滤或是排序，推荐通过过滤方案进行处理，如果是特殊的强制过滤，可以在这个位置进行处理
        /// </summary>
        /// <param name="e"></param>
        public override void PrepareFilterParameter(FilterArgs e)
        {
            //string _userID = this.Context.UserId.ToString();
            //string _FCrmR = this.Act_GetSchRoleFilter(_userID, "or");
            //string _FilterStr = " FCreatorID='" + _userID + "' or FCrmHolder='" + _userID + "'" + _FCrmR;

            if (Str_Filter == "")
            {
                Act_ABIC_tbbMyAll();
            }
            addTimeOutFilter();
            e.AppendQueryFilter(Str_Filter);
            e.AppendQueryOrderby("");
            
        }

        /// <summary>
        /// queryservice取数方案，通过业务对象来获取数据，推荐使用
        /// </summary>
        /// <returns></returns>
        public DynamicObjectCollection GetQueryDatas()
        {
            QueryBuilderParemeter paramCatalog = new QueryBuilderParemeter()
            {
                FormId = "",//取数的业务对象
                FilterClauseWihtKey = "",//过滤条件，通过业务对象的字段Key拼装过滤条件
                SelectItems = SelectorItemInfo.CreateItems("", "", ""),//要筛选的字段【业务对象的字段Key】，可以多个，如果要取主键，使用主键名
            };

            DynamicObjectCollection dyDatas = Kingdee.BOS.ServiceHelper.QueryServiceHelper.GetDynamicObjectCollection(this.Context, paramCatalog);
            return dyDatas;
        }
        #endregion

        #region Action
        /// <summary>
        /// tbbMyAll      全部线索
        /// </summary>
        private void Act_ABIC_tbbMyAll()
        {
            string _userID = this.Context.UserId.ToString();
            string _FCrmR = this.Act_GetSchRoleFilter(_userID, "or");
            string _FilterStr = " FCreatorId='" + _userID + "' or FCrmHolder='" + _userID + "'" + _FCrmR;
            Str_Filter = _FilterStr;
            addTimeOutFilter();
        }

        /// <summary>
        /// tbbMyCreate   我创建的 
        /// </summary>
        private void Act_ABIC_tbbMyCreate()
        {
            string _userID = this.Context.UserId.ToString();
            Str_Filter = " FCreatorId='" + _userID + "'";
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
            if (formId == "ora_CRM_Clue")
            {
                string _currDate = DateTime.Now.ToString("yyyy-MM-dd");
                string _userID = this.Context.UserId.ToString();
                //Str_Filter = "(" + Str_Filter + ")";
                //Str_Filter += string.Format(@" and DATEDIFF(d, FClueEndDt, (case when FCrmHolder='{0}' and FCreatorId<>'{1}' then '{2}' else '1900-01-01' end))<=0 ", _userID, _userID, _currDate);
            }
        }
        #endregion

        #region CZTY Action Base
        /// <summary>
        /// Crm授权组织-部门  查询权
        /// </summary>
        /// <param name="_userID">User ID  EXP=this.Context.UserId.ToString();</param>
        /// <param name="_BefWhile">追加前导 or|and</param>
        /// <returns>授权CrmSN过滤串</returns>
        private string Act_GetSchRoleFilter(string _userID,string _BefWhile)
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
    }
}
