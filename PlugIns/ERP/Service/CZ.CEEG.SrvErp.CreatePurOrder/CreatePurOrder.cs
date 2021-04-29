using Kingdee.BOS.App.Data;
using Kingdee.BOS.Core.DynamicForm.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Core.Validation;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.Util;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace CZ.CEEG.SrvErp.CreatePurOrder
{
    /// <summary>
    /// 小中电审核后，在新特变的数据中心下生成一条销售订单
    /// </summary>
    [HotUpdate]
    [Description("销售订单审核时创建销售订单")]
    public class CreatePurOrder : AbstractOperationServicePlugIn
    {
        public override void EndOperationTransaction(EndOperationTransactionArgs e)
        {
            base.EndOperationTransaction(e);
            string opKey = this.FormOperation.Operation.ToUpperInvariant();
            switch (opKey)
            {
                case "AUDIT":
                    foreach(var dataEntity in e.DataEntitys)
                    {
                        CreateOrder(dataEntity);
                    }
                    
                    break;
            }
        }

        private void CreateOrder(DynamicObject dataEntity)
        {
            
        }
    }
}
