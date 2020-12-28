using Kingdee.BOS.Core.DynamicForm;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Core.List.PlugIn;
using Kingdee.BOS.Core.Metadata;
using Kingdee.BOS.Util;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace CZ.CEEG.OABos.LeaveApplyList
{
    [Description("请假列表")]
    [HotUpdate]
    public class CZ_CEEG_OABos_LeaveApplyList : AbstractListPlugIn
    {
        public override void AfterBarItemClick(AfterBarItemClickEventArgs e)
        {
            base.AfterBarItemClick(e);
            string key = e.BarItemKey.ToUpperInvariant();
            switch(key)
            {
                case "TBALLLEAVE":  //tbAllLeave 集体请假
                    Act_ABIC_AllLeave();
                    break;
            }
        }

        /// <summary>
        /// 集体请假
        /// </summary>
        private void Act_ABIC_AllLeave()
        {
            var para = new DynamicFormShowParameter();
            para.FormId = "ora_OA_AllLeaveSetting";
            para.OpenStyle.ShowType = ShowType.MainNewTabPage;
            para.ParentPageId = this.View.PageId;

            this.View.ShowForm(para);
        }
    }
}
