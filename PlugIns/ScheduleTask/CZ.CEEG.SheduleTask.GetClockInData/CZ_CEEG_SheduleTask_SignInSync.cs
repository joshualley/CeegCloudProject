using CZ.CEEG.SheduleTask.GetClockInData.Utils;
using Kingdee.BOS.Core.DynamicForm;
using Kingdee.BOS.Core.DynamicForm.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Util;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace CZ.CEEG.SheduleTask.SignInSync
{
    [Description("考勤同步")]
    [HotUpdate]
    public class CZ_CEEG_SheduleTask_SignInSync : AbstractDynamicFormPlugIn
    {
        private bool mBtnLock = false;

        public override void AfterBindData(EventArgs e)
        {
            base.AfterBindData(e);
            DateTime now = DateTime.Now;
            string from = string.Format("{0}-{1}-01", now.Year, now.Month);
            string to = now.ToString();
            this.Model.SetValue("FFromDt", from);
            this.Model.SetValue("FToDt", to);
            this.View.UpdateView("FFromDt");
            this.View.UpdateView("FToDt");
        }


        public override void AfterButtonClick(AfterButtonClickEventArgs e)
        {
            base.AfterButtonClick(e);
            string key = e.Key.ToUpperInvariant();
            switch(key)
            {
                case "FSYNCBTN": //FSyncBtn
                    if (mBtnLock)
                    {
                        return;
                    }
                    this.View.ShowMessage("确定进行数据的同步吗？", MessageBoxOptions.YesNo, (result) =>
                    {
                        if(result == MessageBoxResult.Yes)
                        {
                            mBtnLock = true;
                            string from = this.Model.GetValue("FFromDt").ToString();
                            string to = this.Model.GetValue("FToDt").ToString();
                            if (DateTime.Parse(to).CompareTo(DateTime.Parse(from)) >= 0)
                            {
                                this.View.ShowMessage("截止时间需要大于结束时间！");
                            }
                            SignInSyncUtils req = new SignInSyncUtils(this.Context);
                            this.View.ShowMessage("此操作比较耗时，请勿频繁操作！");
                            req.InsertDataWithinDate(from, to, (msg) => {
                                this.View.ShowMessage(msg);
                            });
                            mBtnLock = false;
                        }
                    });
                    
                    break;
            }
            
        }
    }
}
