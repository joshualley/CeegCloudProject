using Kingdee.BOS.Core.Bill.PlugIn;
using Kingdee.BOS.Core.DynamicForm;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Util;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace CZ.CEEG.SheduleTask.EmpWkDt
{
    [Description("考勤表-同步按钮")]
    [HotUpdate]
    public class CZ_CEEG_SheduleTask_EmpWkDt : AbstractBillPlugIn
    {
        public override void AfterBarItemClick(AfterBarItemClickEventArgs e)
        {
            base.AfterBarItemClick(e);
            string key = e.BarItemKey.ToUpperInvariant();
            switch(key)
            {
                case "ORA_TBGETYUNZHIJIADATA": //ora_tbGetYunZhiJiaData
                    var param = new DynamicFormShowParameter();
                    param.FormId = "ora_HR_SignInSync";
                    param.OpenStyle.ShowType = ShowType.Modal; //打开方式
                    param.ParentPageId = this.View.PageId;
                    this.View.ShowForm(param);
                    break;
            }
        }
    }
}
