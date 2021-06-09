using CZ.CEEG.SrvErp.CreatePurOrder.Models;
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
    /// 小中电审核后，在新特变或变压器的数据中心下生成一条销售订单
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
            string fid = dataEntity["Id"].ToString();
            string sql = string.Format(@"
select * 
from T_SAL_SaleOrder o
where o.FID={0}", fid);
            var objs = DBUtils.ExecuteDynamicObject(Context, sql);
            SaleOrder order = new SaleOrder 
            {
                FBillTypeID = new BaseData{ FNumber = "" },
                FBillNo = "",
                FDate = DateTime.Today,
                FSaleOrgId = new BaseData { FNumber = "" },
                FCustId = new BaseData { FNumber = "" },
                FHeadDeliveryWay = new BaseData { FNumber = "" },
                FHEADLOCID = new BaseData { FNumber = "" },
                FCorrespondOrgId = new BaseData { FNumber = "" },
            };
            
        }
    }
}
