using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Core.DynamicForm;
using Kingdee.BOS.Util;
using Kingdee.BOS.Core.Bill;
using Kingdee.BOS.Core.Metadata;
using Kingdee.BOS.App.Data;
using System.ComponentModel;
using System;
using Kingdee.BOS.Core.DynamicForm.PlugIn;
using System.Text;
using Kingdee.BOS.Orm.DataEntity;

namespace CZ.CEEG.OABos.AllLeaveSetting
{
    [Description("集体请假(年假)设置")]
    [HotUpdate]
    public class CZ_CEEG_OABos_AllLeaveSetting : AbstractDynamicFormPlugIn
    {
        /// <summary>
        /// 生成的请假单的FID
        /// </summary>
        private string mLeaveFID = "-1";

        public override void AfterButtonClick(AfterButtonClickEventArgs e)
        {
            base.AfterButtonClick(e);
            string key = e.Key.ToUpperInvariant();
            switch (key)
            {
                case "FGENE": // FGene 生成
                    Act_ABC_GeneLeaveForm();
                    break;
            }
        }

        public override void DataChanged(DataChangedEventArgs e)
        {
            base.DataChanged(e);
            string key = e.Field.Key.ToUpperInvariant();
            if(key.Equals("FBEGINDT")
                || key.Equals("FENDDT")
                || key.Equals("FBEGINFRAME")
                || key.Equals("FENDFRAME"))
            {
                string beginDt = this.Model.GetValue("FBeginDt") == null ? "" : this.Model.GetValue("FBeginDt").ToString();
                string endDt = this.Model.GetValue("FEndDt") == null ? "" : this.Model.GetValue("FEndDt").ToString();
                string beginFrame = this.Model.GetValue("FBeginFrame") == null ? "" : this.Model.GetValue("FBeginFrame").ToString();
                string endFrame = this.Model.GetValue("FEndFrame") == null ? "" : this.Model.GetValue("FEndFrame").ToString();
                if(beginDt.Equals("")
                    || endDt.Equals("")
                    || beginFrame.Equals("")
                    || endFrame.Equals(""))
                {
                    this.Model.SetValue("FDays", 0);
                    return;
                }
                string days = Act_GetLeaveWorkDaysAP(beginDt, beginFrame, endDt, endFrame);
                this.Model.SetValue("FDays", days);
            }
        }

        public override void AfterBarItemClick(AfterBarItemClickEventArgs e)
        {
            base.AfterBarItemClick(e);
            if (e.BarItemKey.EqualsIgnoreCase("ora_tbSubmit"))
            {
                if(mLeaveFID == "-1")
                {
                    this.View.ShowErrMessage("没有要提交的数据!");
                    return;
                }
                StringBuilder sbSql = new StringBuilder();
                sbSql.Append("/*dialect*/");
                var entity = this.Model.DataObject["FEntity"] as DynamicObjectCollection;
                if (entity.Count <= 0) return;

                foreach (var row in entity)
                {
                    sbSql.Append($"update ora_t_Leave set FDAYNUM={row["FDayNum"]} where FEntryID={row["FEntryID"]};");
                }
                DBUtils.Execute(Context, sbSql.ToString());
                
                // 打开生成的请假单
                var para = new BillShowParameter();
                para.FormId = "kbea624189d8e4d829b68340507eda196";
                para.OpenStyle.ShowType = ShowType.InContainer;
                para.ParentPageId = this.View.PageId;
                para.Status = OperationStatus.VIEW;
                para.PKey = mLeaveFID;
                this.View.ShowForm(para);
            }
        }


        public override void BeforeClosed(BeforeClosedEventArgs e)
        {
            base.BeforeClosed(e);
            if (mLeaveFID != "-1")
            {
                this.View.ShowWarnningMessage(
                    "还有数据未提交，确认退出吗？",
                    "还有数据未提交，确认退出吗？",
                    MessageBoxOptions.YesNo, result =>
                    {
                        if(result == MessageBoxResult.Yes)
                        {
                            string sql = $"delete from ora_t_LeaveHead where FID={mLeaveFID};" +
                                $"delete from ora_t_Leave where FID={mLeaveFID};";
                            DBUtils.Execute(Context, sql);
                            mLeaveFID = "-1";
                        }
                        else
                        {
                            e.Cancel = true;
                        }
                    });
            }
        }

        /// <summary>
        /// 请假天数计算，调用存储过程实现
        /// </summary>
        /// <param name="_FBegDt"></param>
        /// <param name="_FBegDtAP"></param>
        /// <param name="_FEndDt"></param>
        /// <param name="_FEndDtAP"></param>
        /// <returns></returns>
        private string Act_GetLeaveWorkDaysAP(string _FBegDt, string _FBegDtAP, string _FEndDt, string _FEndDtAP)
        {
            string _LeaveWDDays = "0";

            string sql = String.Format(@"exec proc_czty_LeaveWorkDaysAP 
                                        @FOrgID='0',@FBD='{0}',@FBD_AP='{1}',@FED='{2}',@FED_AP='{3}'",
                                        _FBegDt, _FBegDtAP, _FEndDt, _FEndDtAP);
            var obj = DBUtils.ExecuteDynamicObject(Context, sql);
            if (obj.Count > 0) _LeaveWDDays = obj[0]["lwds"].ToString();

            return _LeaveWDDays;
        }


        /// <summary>
        /// 生成一张请假单
        /// </summary>
        private void Act_ABC_GeneLeaveForm()
        {
            // 判读本单是否生成了请假单，但还未进行提交
            if (mLeaveFID != "-1")
            {
                this.View.ShowWarnningMessage("已经生成了请假单，但还未提交，确定放弃上一次的生成，重新进行生成吗？",
                    "已经生成了请假单，但还未提交，确定放弃上一次的生成，重新进行生成吗？", 
                    MessageBoxOptions.YesNo, result =>
                {
                    if(result == MessageBoxResult.Yes)
                    {
                        string sql = $"delete from ora_t_LeaveHead where FID={mLeaveFID};" +
                            $"delete from ora_t_Leave where FID={mLeaveFID};";
                        DBUtils.Execute(Context, sql);
                        mLeaveFID = "-1";
                        GeneLeavForm();
                    }
                });
            }
            GeneLeavForm();
        }


        private void GeneLeavForm()
        {
            // 请假类型
            string leaveType = this.Model.GetValue("FLeaveType") == null ? "" : this.Model.GetValue("FLeaveType").ToString();
            // 开始日期
            string beginDt = this.Model.GetValue("FBeginDt") == null ? "" : this.Model.GetValue("FBeginDt").ToString();
            // 结束日期
            string endDt = this.Model.GetValue("FEndDt") == null ? "" : this.Model.GetValue("FEndDt").ToString();
            // 开始时段
            string beginFrame = this.Model.GetValue("FBeginFrame") == null ? "" : this.Model.GetValue("FBeginFrame").ToString();
            // 结束时段
            string endFrame = this.Model.GetValue("FEndFrame") == null ? "" : this.Model.GetValue("FEndFrame").ToString();
            // 请假天数
            string days = this.Model.GetValue("FDays").ToString();
            // 请假事由
            string remarks = this.Model.GetValue("FRemarks") == null ? "" : this.Model.GetValue("FRemarks").ToString();
            if (leaveType.Equals("")
                || remarks.Equals("")
                || beginDt.Equals("")
                || endDt.Equals("")
                || beginFrame.Equals("")
                || endFrame.Equals(""))
            {
                this.View.ShowErrMessage("所填信息不完整!");
                return;
            }
            if (Convert.ToDecimal(days) <= 0)
            {
                this.View.ShowErrMessage("请假天数必须大于0！");
                return;
            }
            string beginTime = beginFrame == "1" ? DateTime.Parse(beginDt).AddHours(8.5).ToString() :
                DateTime.Parse(beginDt).AddHours(13).ToString();
            string endTime = endFrame == "1" ? DateTime.Parse(endDt).AddHours(12).ToString() :
                DateTime.Parse(endDt).AddHours(17.5).ToString();

            // 生成请假单
            string creatorId = Context.UserId.ToString();
            string sql = "exec proc_czly_AllLeave " +
                $"@FCreatorID='{creatorId}',@FleaveType='{leaveType}',@FBeginDt='{beginDt}',@FEndDt='{endDt}'," +
                $"@FBeginFrame='{beginFrame}',@FEndFrame='{endFrame}',@FBeginTime='{beginTime}',@FEndTime='{endTime}'," +
                $"@FRemarks='{remarks}',@FDays='{days}'";

            var objs = DBUtils.ExecuteDynamicObject(Context, sql);
            mLeaveFID = objs.Count > 0 ? objs[0]["FID"].ToString() : "-1";
            if (objs.Count <= 0)
            {
                this.View.ShowErrMessage("生成失败！");
                return;
            }
            // 创建单据体
            this.Model.DeleteEntryData("FEntity");
            sql = $"select FEntryId, FName, FDayNum from ora_t_Leave where FID={mLeaveFID}";
            var items = DBUtils.ExecuteDynamicObject(Context, sql);
            if (items.Count <= 0) return;
            this.Model.BatchCreateNewEntryRow("FEntity", items.Count);
            for (int i = 0; i < items.Count; i++)
            {
                this.Model.SetValue("FEntryID", items[i]["FEntryId"]);
                this.Model.SetValue("FName", items[i]["FName"]);
                this.Model.SetValue("FDayNum", items[i]["FDayNum"]);
            }
            this.View.UpdateView("FEntity");
        }


        
    }
}
