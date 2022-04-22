using Kingdee.BOS.App.Data;
using Kingdee.BOS.Core.Bill.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Util;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;


namespace CZ.CEEG.BosOA.ForPubFundFormCtrl
{
    [Description("对公资金页面控制")]
    [HotUpdate]
    public class CZ_CEEG_BosOA_ForPubFundFormCtrl : AbstractBillPlugIn
    {
        public override void AfterBindData(EventArgs e)
        {
            SetVis();
        }

        public override void DataChanged(DataChangedEventArgs e)
        {
            if (e.Field.Key.ToString().Equals("FCostType")) {
                SetVis();
            }
        }

        public void SetVis() {
            string FCostType = this.View.Model.GetValue("FCostType") == null ? "" : this.View.Model.GetValue("FCostType").ToString();
            if (FCostType.Equals("3"))
            {
                this.View.GetControl("FBackDate").Visible = true;
            }
            else
            {
                this.View.GetControl("FBackDate").Visible = false;
            }
        }

        public override void BeforeDoOperation(BeforeDoOperationEventArgs e)
        {
            string key = e.Operation.FormOperation.Operation;
            if (key.Equals("Submit") || key.Equals("Save")) {
                string FCostType = this.View.Model.GetValue("FCostType") == null ? "" : this.View.Model.GetValue("FCostType").ToString();
                if (FCostType.Equals("3"))
                {
                    if (this.View.Model.GetValue("FBackDate") == null) {
                        this.View.ShowErrMessage("请选择预计归还时间");
                        e.Cancel = true;
                    }
                }
            }
        }
    }
}
