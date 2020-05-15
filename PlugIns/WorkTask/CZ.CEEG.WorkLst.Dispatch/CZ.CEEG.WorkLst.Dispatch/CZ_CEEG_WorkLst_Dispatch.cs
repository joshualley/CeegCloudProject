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
using Kingdee.BOS.Util;

namespace CZ.CEEG.WorkLst.Dispatch
{
    [Description("任务列表过滤")]
    [HotUpdate]
    public class CZ_CEEG_WorkLst_Dispatch : AbstractListPlugIn
    {
        string Str_Filter = "";

        #region override
        
        /// <summary>
        /// 菜单点击后处理事件，表单插件同样适用
        /// </summary>
        /// <param name="e"></param>
        public override void AfterBarItemClick(AfterBarItemClickEventArgs e)
        {
            switch (e.BarItemKey.ToUpperInvariant())
            {
                case "TBBMYALL":
                    //tbbMyAll      全部任务
                    Act_Task_tbbMyAll();
                    this.View.RefreshByFilter();
                    break;
                case "TBBMYRESP":
                    //tbbMyResp     我负责的 
                    Act_Task_tbbMyResp();
                    this.View.RefreshByFilter();
                    break;
                case "TBBMYPART":
                    //tbbMyPart     我执行的
                    Act_Task_tbbMyPart();
                    this.View.RefreshByFilter();
                    break;
                case "TBBMYEXEC":
                    //tbbMyExec     待执行的
                    Act_Task_tbbMyExec();
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
            if (Str_Filter == "")
            {
                Act_Task_tbbMyExec();
            }

            e.AppendQueryFilter(Str_Filter);
            e.AppendQueryOrderby("");
        }

        #endregion

        #region Action
        /// <summary>
        /// tbbMyAll      全部任务
        /// </summary>
        private void Act_Task_tbbMyAll()
        {
            string _userID = this.Context.UserId.ToString();
            string _FilterStr = " FRespID='" + _userID + "' or FUserIds like '%" + _userID + "%'";
            Str_Filter = _FilterStr;
        }

        /// <summary>
        /// tbbMyResp     我负责的 
        /// </summary>
        private void Act_Task_tbbMyResp()
        {
            string _userID = this.Context.UserId.ToString();
            Str_Filter = " FRespID='" + _userID + "'";
        }

        /// <summary>
        /// tbbMyPart     我执行的
        /// </summary>
        private void Act_Task_tbbMyPart()
        {
            string _userID = this.Context.UserId.ToString();
            Str_Filter = " FUserIds like '%" + _userID + "%'";
        }

        /// <summary>
        /// tbbMyExec     待执行的（默认）
        /// </summary>
        private void Act_Task_tbbMyExec()
        {
            string _userID = this.Context.UserId.ToString();
            string currDt = DateTime.Now.ToString("yyyy-MM-dd");
            Str_Filter = " (FRespID='" + _userID + "' or FUserIds like '%" + _userID + "%') and FPlanDt >= '" + currDt + "'";
        }

        #endregion

    }
}
