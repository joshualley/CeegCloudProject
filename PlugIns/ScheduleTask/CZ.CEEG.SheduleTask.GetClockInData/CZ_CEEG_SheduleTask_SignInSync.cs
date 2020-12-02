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
using System.Threading;

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
                        this.View.ShowMessage("[数据获取中...]此操作比较耗时，请勿频繁操作！");
                        return;
                    }
                    this.View.ShowMessage("确定进行数据的获取吗？\n提醒：此过程可能较为耗时，需要耐心等待！", MessageBoxOptions.YesNo, (result) =>
                    {
                        if(result == MessageBoxResult.Yes)
                        {
                            mBtnLock = true;
                            string from = this.Model.GetValue("FFromDt").ToString();
                            string to = this.Model.GetValue("FToDt").ToString();
                            if (DateTime.Parse(to).CompareTo(DateTime.Parse(from)) <= 0)
                            {
                                this.View.ShowMessage("截止时间需要大于开始时间！");
                                return;
                            }
                            string FLog = "";
                            this.Model.SetValue("FLog", FLog);
                            SignInSyncUtils req = new SignInSyncUtils(this.Context);
                            //var that = this;
                            req.InsertDataWithinDate(from, to, (msg) => {
                                FLog += msg + "\n";
                            });
                            this.Model.SetValue("FLog", FLog);
                            mBtnLock = false;
                            this.View.ShowMessage("数据获取完成");
                        }
                    });
                    
                    break;
            }
            
        }
    }
}
