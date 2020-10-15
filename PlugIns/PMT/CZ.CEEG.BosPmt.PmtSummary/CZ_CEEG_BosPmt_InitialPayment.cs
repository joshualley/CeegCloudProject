using Kingdee.BOS.App.Data;
using Kingdee.BOS.Core.Bill;
using Kingdee.BOS.Core.Bill.PlugIn;
using Kingdee.BOS.Core.Metadata;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.Core.DynamicForm;
using Kingdee.BOS.Core.DynamicForm.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Util;
using System;
using System.ComponentModel;
using System.Threading.Tasks;
using Kingdee.BOS.Core.List.PlugIn;

namespace CZ.CEEG.BosPmt.PmtDeliver
{
    [HotUpdate]
    [Description("期初货款")]
    public class CZ_CEEG_BosPmt_InitialPayment : AbstractListPlugIn
    {
        public override void BarItemClick(BarItemClickEventArgs e)
        {
            switch (e.BarItemKey.ToUpperInvariant())
            {
                case "ORA_TBRELATION": //ora_tbRelation
                    Act_RelationOrder();
                    break;
            }
        }

        /// <summary>
        /// 关联销售订单
        /// </summary>
        private void Act_RelationOrder()
        {
            string sql = @"/*dialect*/UPDATE ip
set ip.FORDERID=o.FID
FROM ora_Pmt_InitialPayment ip
INNER JOIN T_SAL_ORDER o ON o.FBILLNO=ip.FORDERNO";
            int num = DBUtils.Execute(this.Context, sql);
            this.View.Refresh();
            this.View.ShowMessage("已关联" + num.ToString() + "条销售订单。");
        }
    }
}
