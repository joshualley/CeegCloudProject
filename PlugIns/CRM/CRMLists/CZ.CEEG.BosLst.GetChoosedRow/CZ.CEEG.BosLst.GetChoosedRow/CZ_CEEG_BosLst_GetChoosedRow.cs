using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Data;

using Kingdee.BOS.Core;
using Kingdee.BOS.Core.Bill.PlugIn;
using Kingdee.BOS.Core.List.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Core.List.PlugIn.Args;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.Core.SqlBuilder;
using Kingdee.BOS.Core.Metadata;
using Kingdee.BOS.ServiceHelper;

//using Kingdee.BOS.Core.DynamicForm.PlugIn;
//using Kingdee.BOS.Core.DynamicForm.PlugIn.ControlModel;
using Kingdee.BOS.App.Data;

namespace CZ.CEEG.BosLst.GetChoosedRow
{
    [Description("列表获取勾选行")]
    [Kingdee.BOS.Util.HotUpdate]
    public class CZ_CEEG_BosLst_GetChoosedRow : AbstractListPlugIn
    {
       
        #region override
        /// <summary>
        /// 菜单点击后处理事件，表单插件同样适用
        /// </summary>
        /// <param name="e"></param>
        public override void AfterBarItemClick(AfterBarItemClickEventArgs e)
        {
            var x = CZ_GetSelectedRowsBillNo();

            var ee = CZ_GetSelectedRowsFEntryID();
            var zz = CZ_GetCurrentRowFID();
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
            foreach(var fid in fids)
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
        /// queryservice取数方案，通过业务对象来获取数据，推荐使用
        /// </summary>
        /// <returns></returns>
        public DynamicObjectCollection GetQueryDatas(string filter)
        {
            QueryBuilderParemeter paramCatalog = new QueryBuilderParemeter()
            {
                FormId = this.View.GetFormId(),//取数的业务对象
                FilterClauseWihtKey = filter,//过滤条件，通过业务对象的字段Key拼装过滤条件
                SelectItems = SelectorItemInfo.CreateItems("FID", "FBILLNO", "FDOCUMENTSTATUS"),//要筛选的字段【业务对象的字段Key】，可以多个，如果要取主键，使用主键名
            };

            DynamicObjectCollection dyDatas = QueryServiceHelper.GetDynamicObjectCollection(this.Context, paramCatalog);
            return dyDatas;
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
