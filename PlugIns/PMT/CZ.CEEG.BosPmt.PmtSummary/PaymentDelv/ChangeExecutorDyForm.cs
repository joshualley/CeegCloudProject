using Kingdee.BOS;
using Kingdee.BOS.App.Data;
using Kingdee.BOS.Core.Bill;
using Kingdee.BOS.Core.Bill.PlugIn;
using Kingdee.BOS.Core.DynamicForm;
using Kingdee.BOS.Core.DynamicForm.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Msg;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.ServiceHelper;
using Kingdee.BOS.ServiceHelper.Messages;
using Kingdee.BOS.Util;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace CZ.CEEG.BosPmt.PmtSummary.PaymentDelv
{
    [HotUpdate]
    [Description("调整承办人-动态表单")]
    public class ChangeExecutorDyForm: AbstractDynamicFormPlugIn
    {
        private bool isReturnData = false;
        public override void BeforeClosed(BeforeClosedEventArgs e)
        {
            base.BeforeClosed(e);
            if (!isReturnData)
            {
                this.View.ReturnToParentWindow(null);
            }
        }

        public override void AfterBindData(EventArgs e)
        {
            base.AfterBindData(e);
            string FExecutorId = this.View.OpenParameter.GetCustomParameter("FExecutorId")?.ToString() ?? "0";
            this.Model.SetValue("FExecutorId", FExecutorId);
            this.View.UpdateView("FExecutorId");
        }

        public override void AfterBarItemClick(AfterBarItemClickEventArgs e)
        {
            base.AfterBarItemClick(e);
            switch (e.BarItemKey.ToUpperInvariant())
            {
                case "ORA_TBCONFIRM":
                    Act_Confirm();
                    break;
            }
        }

        private void Act_Confirm()
        {
            
            string FExecutorId = (this.Model.GetValue("FExecutorId") as DynamicObject)?["Id"].ToString() ?? "0";
            if (FExecutorId.Equals("0")) 
            {
                this.View.ShowMessage("无效的承办人！");
                return;
            }

            isReturnData = true;
            this.View.ReturnToParentWindow(FExecutorId);
            this.View.Close();
        }
    }
}