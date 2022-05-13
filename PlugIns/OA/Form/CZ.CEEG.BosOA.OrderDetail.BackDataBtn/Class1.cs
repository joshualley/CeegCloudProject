using Kingdee.BOS.Core.DynamicForm.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Core.Metadata.EntityElement;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.Util;
using System.ComponentModel;

namespace CZ.CEEG.BosOA.OrderDetail.BackDataBtn
{
    [Description("[订单详情]返回订单详情数据"), HotUpdate]
    public class Class1 : AbstractDynamicFormPlugIn
    {
        int count = 0;
        public override void BarItemClick(BarItemClickEventArgs e)
        {
            base.BarItemClick(e);
            if (e.BarItemKey.Equals("tbReturnData"))
            {
                Entity entity = this.View.BillBusinessInfo.GetEntity("FEntity");
                DynamicObjectCollection entityObject = this.View.Model.GetEntityDataObject(entity);
                DynamicObjectCollection dymat = new DynamicObjectCollection(entity.DynamicObjectType);
                foreach (DynamicObject current in entityObject)
                {
                    if (current["F_ora_CheckBox"].ToString().ToUpper() == "TRUE")
                    {
                        dymat.Add(current);
                        count++;
                    }
                }
                if (count == 0)
                {
                    this.View.ShowMessage("请选择数据。");
                }
                else
                {
                    this.View.ReturnToParentWindow(dymat);
                    this.View.Close();
                }
            }
        }
    }
}
