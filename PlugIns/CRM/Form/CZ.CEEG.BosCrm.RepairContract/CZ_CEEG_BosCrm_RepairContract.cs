using Kingdee.BOS.Core.Bill.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Util;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace CZ.CEEG.BosCrm.RepairContract
{
    [Description("维修合同")]
    [HotUpdate]
    public class CZ_CEEG_BosCrm_RepairContract : AbstractBillPlugIn
    {
        public override void DataChanged(DataChangedEventArgs e)
        {
            base.DataChanged(e);
            string key = e.Field.Key.ToString();
            switch(key)
            {
                case "FBRptPrice": //报价
                    CalAmt(e.Row);
                    break;
                case "FTaxRate": //税率
                    CalAmt(e.Row);
                    break;
            }
        }

        /// <summary>
        /// 计算金额
        /// </summary>
        private void CalAmt(int row)
        {
            double FBQty = double.Parse(this.View.Model.GetValue("FQty", row).ToString()); //数量
            double FBRptPrice = double.Parse(this.View.Model.GetValue("FBRptPrice", row).ToString());//报价
            double FBTaxRate = double.Parse(this.View.Model.GetValue("FTaxRate", row).ToString());//税率

            //FBTaxRate = FBTaxRate == 0 ? 100 : FBTaxRate;

            this.View.Model.SetValue("FUtPriceTax", FBRptPrice / FBQty, row); //含税单价
            this.View.Model.SetValue("FUtPrice", FBRptPrice / FBQty / (FBTaxRate / 100 + 1), row);  //不含税单价
            this.View.Model.SetValue("FBTaxAmt", FBRptPrice - (FBRptPrice / (FBTaxRate / 100 + 1)), row);   //税额
            this.View.Model.SetValue("FBNTAmt", FBRptPrice / (FBTaxRate / 100 + 1), row);    //不含税金额
        }

    }
}
