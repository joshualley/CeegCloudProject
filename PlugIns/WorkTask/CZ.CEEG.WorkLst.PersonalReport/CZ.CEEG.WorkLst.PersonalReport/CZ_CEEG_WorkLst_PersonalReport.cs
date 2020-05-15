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

namespace CZ.CEEG.WorkLst.PersonalReport
{
    [Description("汇报列表过滤")]
    [HotUpdate]
    public class CZ_CEEG_WorkLst_PersonalReport : AbstractListPlugIn
    {
        string Str_Filter = "";
        /// <summary>
        /// 对列表数据追加过滤或是排序，推荐通过过滤方案进行处理，如果是特殊的强制过滤，可以在这个位置进行处理
        /// </summary>
        /// <param name="e"></param>
        public override void PrepareFilterParameter(FilterArgs e)
        {
            if (Str_Filter == "")
            {
                Act_Task_tbbMyCreate();
            }

            e.AppendQueryFilter(Str_Filter);
            e.AppendQueryOrderby("");
        }

        /// <summary>
        /// tbbMyCreate     我创建的
        /// </summary>
        private void Act_Task_tbbMyCreate()
        {
            string _userID = this.Context.UserId.ToString();
            Str_Filter = " FCreatorID='" + _userID + "'";
        }
    }
}
