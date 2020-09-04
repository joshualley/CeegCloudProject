using System.Collections.Generic;
using System.ComponentModel;
using System;

using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Core.Bill.PlugIn;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.Util;
using Kingdee.BOS.App.Data;
using CZ.CEEG.OABos.LeaveApply.LeaveType;
using Kingdee.BOS.ServiceHelper;
using Kingdee.BOS.Core.DynamicForm;

namespace CZ.CEEG.OABos.LeaveApplyNew
{
    [HotUpdate]
    [Description("BOS请假控制New")]
    public class CZ_CEEG_OABos_LeaveApplyNew : AbstractBillPlugIn
    {

        #region 带出表体的请假人,及请假人部门、岗位
        /// <summary>
        /// 新增时设置由表头带出表体的请假人（暂定）
        /// </summary>
        private void addEntryLeaver()
        {
            if (CZ_GetCommonField("DocumentStatus") == "Z")
            {
                string _FApplyID = this.View.Model.DataObject["FApplyID"] == null ? "" : (this.View.Model.DataObject["FApplyID"] as DynamicObject)["Id"].ToString();
                string _F_ora_Post = this.View.Model.DataObject["F_ora_Post"] == null ? "" : (this.View.Model.DataObject["F_ora_Post"] as DynamicObject)["Id"].ToString();
                string _FDeptID = this.View.Model.DataObject["FDeptID"] == null ? "" : (this.View.Model.DataObject["FDeptID"] as DynamicObject)["Id"].ToString();

                int Row = this.View.Model.GetEntryRowCount("FEntity") - 1;
                //this.View.ShowMessage(Row.ToString());
                if (_FApplyID != "" && _F_ora_Post != "" && _FDeptID != "")
                {
                    this.View.Model.SetItemValueByID("FName", _FApplyID, Row);
                    this.View.Model.SetItemValueByID("FPost", _F_ora_Post, Row);
                    this.View.Model.SetItemValueByID("FDept", _FDeptID, Row);
                }
            }
        }

        /// <summary>
        /// 携带表体请假人部门，岗位
        /// </summary>
        /// <param name="Row"></param>
        private void setEmpInfo(int Row)
        {
            string FApplyID = this.View.Model.GetValue("FName", Row) == null ? "" : (this.View.Model.GetValue("FName", Row) as DynamicObject)["Id"].ToString();
            string sql = String.Format(@"exec proc_czty_GetLoginUser2Emp @FEmpID='{0}'", FApplyID);
            var obj = CZDB_GetData(sql);
            //string _FEmpID = "";
            string _FDeptID = obj[0]["FDeptID"].ToString();
            string _FPostID = obj[0]["FPostID"].ToString();
            this.View.Model.SetItemValueByID("FPost", _FPostID, Row);
            this.View.Model.SetItemValueByID("FDept", _FDeptID, Row);
        }

        #endregion

        #region 计算请假天数
        //上午上班
        private string AM_S = DateTime.Parse("8:30:00").ToString();
        //上午下班
        private string AM_E = DateTime.Parse("12:00:00").ToString();
        //下午上班
        private string PM_S = DateTime.Parse("13:00:00").ToString();
        //下午下班
        private string PM_E = DateTime.Parse("17:30:00").ToString();

        /// <summary>
        /// 请假天数计算，调用存储过程实现
        /// </summary>
        /// <param name="_FOrgID"></param>
        /// <param name="_FBegDt"></param>
        /// <param name="_FBegDtAP"></param>
        /// <param name="_FEndDt"></param>
        /// <param name="_FEndDtAP"></param>
        /// <returns></returns>
        private string CZDB_GetLeaveWorkDaysAP(string _FOrgID, string _FBegDt, string _FBegDtAP, string _FEndDt, string _FEndDtAP)
        {
            string _LeaveWDDays = "0";

            string sql = String.Format(@"exec proc_czty_LeaveWorkDaysAP 
                                        @FOrgID='{0}',@FBD='{1}',@FBD_AP='{2}',@FED='{3}',@FED_AP='{4}'",
                                        _FOrgID, _FBegDt, _FBegDtAP, _FEndDt, _FEndDtAP);
            var obj = CZDB_GetData(sql);
            if (obj.Count > 0) _LeaveWDDays = obj[0]["lwds"].ToString();

            return _LeaveWDDays;
        }



        /// <summary>
        /// 根据情况设置请假天数值
        /// </summary>
        /// <param name="e"></param>
        private void Act_SetLeaveDays(DataChangedEventArgs e)
        {
            string leaveType = this.View.Model.GetValue("FLeaveType", e.Row).ToString();

            if (e.Field.Key == "FStartDate" ||
                e.Field.Key == "FStartTimeFrame" ||
                e.Field.Key == "FEndDate" ||
                e.Field.Key == "FEndTimeFrame" ||
                e.Field.Key == "FSTime" ||
                e.Field.Key == "FETime" ||
                e.Field.Key == "FLeaveType")
            {
                var _FEntity = this.View.Model.DataObject["FEntity"] as DynamicObjectCollection;

                string orgId = this.View.Model.DataObject["FOrgID"] == null ? "" : (this.View.Model.DataObject["FOrgID"] as DynamicObject)["Id"].ToString();
                if(orgId == "")
                {
                    this.View.ShowMessage("请先选择组织！");
                    return;
                }
                string startDate = _FEntity[e.Row]["FStartDate"] == null ? "" : _FEntity[e.Row]["FStartDate"].ToString();
                string endDate = _FEntity[e.Row]["FEndDate"] == null ? "" : _FEntity[e.Row]["FEndDate"].ToString();
                string startTimeFrame = _FEntity[e.Row]["FStartTimeFrame"] == null ? "" : _FEntity[e.Row]["FStartTimeFrame"].ToString();
                string endTimeFrame = _FEntity[e.Row]["FEndTimeFrame"] == null ? "" : _FEntity[e.Row]["FEndTimeFrame"].ToString();
                string sTime = _FEntity[e.Row]["FSTime"] == null ? "" : _FEntity[e.Row]["FSTime"].ToString();
                string eTime = _FEntity[e.Row]["FETime"] == null ? "" : _FEntity[e.Row]["FETime"].ToString();

                if (startDate != "" && startTimeFrame != "" && endDate != "" && endTimeFrame != "")
                {
                    //同步时间和时段1 : 如果改动的是时段 
                    //根据开始时段上下午设置开始时间
                    if (e.Field.Key == "FStartTimeFrame" && startTimeFrame == "1")
                    {
                        this.View.Model.SetValue("FSTime", AM_S, e.Row);
                    }
                    else if (e.Field.Key == "FStartTimeFrame" && startTimeFrame == "2")
                    {
                        this.View.Model.SetValue("FSTime", PM_S, e.Row);
                    }
                    //根据结束时段上下午设置结束时间
                    if (e.Field.Key == "FEndTimeFrame" && endTimeFrame == "1")
                    {
                        this.View.Model.SetValue("FETime", AM_E, e.Row);
                    }
                    else if (e.Field.Key == "FEndTimeFrame" && endTimeFrame == "2")
                    {
                        this.View.Model.SetValue("FETime", PM_E, e.Row);
                    }
                    //同步时间和时段2 : 如果改动的是时间
                    DateTime md = DateTime.Parse("13:00:00");
                    //根据开始时间设置开始时段上下午
                    if (e.Field.Key == "FSTime" && DateTime.Parse(sTime).CompareTo(md) < 0)
                    {
                        this.View.Model.SetValue("FStartTimeFrame", "1", e.Row);
                        startTimeFrame = "1";
                        this.View.UpdateView("F_ora_MobileProxyEntryEntity");
                    }
                    else if (e.Field.Key == "FSTime" && DateTime.Parse(sTime).CompareTo(md) >= 0)
                    {
                        this.View.Model.SetValue("FStartTimeFrame", "2", e.Row);
                        startTimeFrame = "2";
                        this.View.UpdateView("F_ora_MobileProxyEntryEntity");
                    }
                    //根据结束时间设置结束时段上下午
                    if (e.Field.Key == "FETime" && DateTime.Parse(eTime).CompareTo(md) < 0)
                    {
                        this.View.Model.SetValue("FEndTimeFrame", "1", e.Row);
                        endTimeFrame = "1";
                        this.View.UpdateView("F_ora_MobileProxyEntryEntity");
                    }
                    else if (e.Field.Key == "FETime" && DateTime.Parse(eTime).CompareTo(md) >= 0)
                    {
                        this.View.Model.SetValue("FEndTimeFrame", "2", e.Row);
                        endTimeFrame = "2";
                        this.View.UpdateView("F_ora_MobileProxyEntryEntity");
                    }

                    //计算请假时长（忽略节假日，周末）
                    string day = CZDB_GetLeaveWorkDaysAP(orgId, startDate, startTimeFrame, endDate, endTimeFrame);
                    //如果是哺乳假
                    if (leaveType == "11")
                    {
                        if (int.Parse(day.Split('.')[1]) > 0)
                        {
                            day = ((float.Parse(day.Split('.')[0]) + 1) / 8).ToString();
                        }
                        else
                        {
                            day = (float.Parse(day.Split('.')[0]) / 8).ToString();
                        }
                    }

                    if (leaveType == "9")  //请假类别为调休
                    {
                        startTimeFrame = this.View.Model.GetValue("FStartTimeFrame", e.Row).ToString();
                        endTimeFrame = this.View.Model.GetValue("FEndTimeFrame", e.Row).ToString();
                        sTime = this.View.Model.GetValue("FSTime", e.Row).ToString();
                        eTime = this.View.Model.GetValue("FETime", e.Row).ToString();
                        day = AmendCal_HolidayShift(day, startTimeFrame, endTimeFrame, sTime, eTime);
                    }

                    //设置请假天数
                    this.View.Model.SetValue("FDayNum", day, e.Row);
                }
            }
        }

        /// <summary>
        /// 修正调休时间计算，需要在时间和时段同步后执行
        /// </summary>
        /// <param name="day"></param>
        /// <param name="startTimeFrame"></param>
        /// <param name="endTimeFrame"></param>
        private string AmendCal_HolidayShift(string day, string startTimeFrame, string endTimeFrame, string sTime, string eTime)
        {
            if (day != "0")
            {
                double _day = 0;
                //皆为上午
                if (startTimeFrame == "1" && endTimeFrame == "1")
                {
                    _day = double.Parse(day) - 0.5;
                }
                //上午、下午
                if (startTimeFrame == "1" && endTimeFrame == "2")
                {
                    _day = double.Parse(day) - 1;
                }
                //下午、上午
                if (startTimeFrame == "2" && endTimeFrame == "1")
                {
                    _day = double.Parse(day) - 1;
                }
                //下午、下午
                if (startTimeFrame == "2" && endTimeFrame == "2")
                {
                    _day = double.Parse(day) - 0.5;
                }
                day = (_day + CalHours(DateTime.Parse(sTime), DateTime.Parse(eTime)) / 8.0).ToString("#.000000");
            }
            return day;
        }

        /// <summary>
        /// 调休时计算开始、结束时间
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <returns></returns>
        private double CalHours(DateTime start, DateTime end)
        {
            //结束时间 大于 开始时间
            //假设取得的时间就是短时间
            DateTime am = DateTime.Parse(AM_S);
            DateTime md1 = DateTime.Parse(AM_E);
            DateTime md2 = DateTime.Parse(PM_S);
            DateTime pm = DateTime.Parse(PM_E);

            double totalHour = 0;
            //如果开始时间在中午
            if (isInTimeRange(start, md1, md2))
            {
                totalHour = (end.Ticks - md2.Ticks) / 10000000.0 / 3600.0;
            }
            //如果结束时间在中午
            else if (isInTimeRange(end, md1, md2))
            {
                totalHour = (md1.Ticks - start.Ticks) / 10000000.0 / 3600.0;
            }
            //同时在中午
            else if (isInTimeRange(start, md1, md2) && isInTimeRange(end, md1, md2))
            {
                totalHour = 0;
            }
            //开始在上午，结束在下午
            else if (isInTimeRange(start, am, md1) && isInTimeRange(end, md2, pm))
            {
                totalHour = (end.Ticks - start.Ticks) / 10000000.0 / 3600.0 - 1;
            }
            //开始在下午，结束在上午
            else if (isInTimeRange(end, am, md1) && isInTimeRange(start, md2, pm))
            {
                totalHour = (pm.Ticks - start.Ticks + end.Ticks - am.Ticks) / 10000000.0 / 3600.0;
            }
            //同在上午，或同在下午
            else if ((isInTimeRange(start, am, md1) && isInTimeRange(end, am, md1)) || (isInTimeRange(start, md2, pm) && isInTimeRange(end, md2, pm)))
            {
                totalHour = (end.Ticks - start.Ticks) / 10000000.0 / 3600.0;
            }
            //不在上班区间
            else
            {
                totalHour = 0;
                this.View.ShowMessage("你所选择的时间不在上班的时间范围！");
            }
            return totalHour;
        }

        /// <summary>
        /// 是否在一段时间内
        /// </summary>
        /// <param name="time"></param>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <returns></returns>
        private bool isInTimeRange(DateTime time, DateTime start, DateTime end)
        {
            if (time.CompareTo(start) >= 0 && time.CompareTo(end) <= 0)
                return true;
            return false;
        }

        #endregion

        #region override方法
        private bool isFirstOpen = true;

        /// <summary>
        /// 初始化时显示默认剩余天数
        /// </summary>
        /// <param name="e"></param>
        public override void AfterBindData(EventArgs e)
        {
            base.AfterBindData(e);
            addEntryLeaver();
            if (isFirstOpen)
            {
                isFirstOpen = false;
                string type = this.View.Model.GetValue("FLeaveType", 0).ToString();
                string msg = "";
                msg = QueryLeftDays(type, 0, true);
                this.View.Model.SetValue("FDispLeaveDay", msg);
            }
        }



        /// <summary>
        /// 请假类型变动时，返回可请假天数。
        /// </summary>
        /// <param name="e"></param>
        public override void DataChanged(DataChangedEventArgs e)
        {
            base.DataChanged(e);
            Act_SetLeaveDays(e);
            if (e.Field.Key == "FLeaveType" || e.Field.Key == "FName")
            {
                if (e.Field.Key == "FName")
                {
                    setEmpInfo(e.Row);
                }
                string type = this.View.Model.GetValue("FLeaveType", e.Row).ToString();
                string msg = "";
                msg = QueryLeftDays(type, e.Row, false);
                this.View.Model.SetValue("FDispLeaveDay", msg); //代理字段使用BillModel，单据字段使用Model
            }
        }

        /// <summary>
        /// 提交前验证输入是否合法
        /// </summary>
        /// <param name="e"></param>
        public override void BeforeDoOperation(BeforeDoOperationEventArgs e)
        {
            base.BeforeDoOperation(e);
            string _opKey = e.Operation.FormOperation.Operation.ToUpperInvariant();
            if (_opKey == "SUBMIT")
            {
                if (!IsPass())
                {
                    e.Cancel = true;
                }
                SetMaxLeaveDay();
            }
            else if (_opKey == "SAVE")
            {
                SetMaxLeaveDay();
            }
        }
        #endregion

        #region 设置最大请假天数
        private void SetMaxLeaveDay()
        {
            var entity = this.View.Model.DataObject["FEntity"] as DynamicObjectCollection;
            float MaxDay = 0;
            float temDay = 0;
            foreach (var row in entity)
            {
                temDay = float.Parse(row["FDayNum"].ToString());
                if (temDay > MaxDay) MaxDay = temDay;
            }

            this.View.Model.SetValue("FLTDays", MaxDay);
        }

        #endregion

        #region 获取可请假天数，提交前验证

        /// <summary>
        /// 提交前验证请假天数是否合理
        /// </summary>
        /// <returns></returns>
        private bool IsPass()
        {
            var _FEntity = this.View.Model.DataObject["FEntity"] as DynamicObjectCollection;
            //验证请假天数不为0
            foreach (var row in _FEntity)
            {
                if ("0".Equals(row["FDayNum"].ToString()))
                {
                    this.View.ShowMessage("请假提交失败！\n原因：请假天数需要大于0！");
                    return false;
                }
            }
            int _FLeaveType = 0;
            double _FDayNum = 0;
            long _FName = 0;

            LeaveFactory factory = new LeaveFactory();
            foreach (var _Row in _FEntity)
            {
                _FName = long.Parse((_Row["FName"] as DynamicObject)["Id"].ToString());
                _FLeaveType = int.Parse(_Row["FLeaveType"].ToString());
                _FDayNum = double.Parse(_Row["FDayNum"].ToString());
                if(_FName == 0)
                {
                    this.View.ShowMessage("请假提交失败！\n原因：请假人姓名不能为空！");
                    return false;
                }
                factory.AppendLeave(Context, _FLeaveType, _FName, _FDayNum);
            }
            string msg = "";
            if(!factory.VadidateLeave(ref msg))
            {
                this.View.ShowMessage(msg);
                return false;
            }

            return true;
        }

        /// <summary>
        /// 请假类型变动时，进行查询，提示
        /// </summary>
        /// <param name="_FLeaveType"></param>
        /// <param name="Row"></param>
        /// <returns></returns>
        private string QueryLeftDays(string _FLeaveType, int Row, bool isInit)
        {
            string _FNAME = this.View.Model.GetValue("FNAME", Row) == null ? "0" : (this.View.Model.GetValue("FNAME", Row) as DynamicObject)["Id"].ToString();
            
            if (_FNAME == "0")
            {
                if (!isInit)
                {
                    this.View.ShowMessage("姓名为空，请先选择请假人姓名！");
                }
                return "";
            }
            LeaveFactory factory = new LeaveFactory();
            var leave = factory.MakeLeave(Context, int.Parse(_FLeaveType), long.Parse(_FNAME), 0);
            return leave.GetLeftLeaveMessage();
        }
        #endregion

        #region 基本取数方法
        /// <summary>
        /// 获取当前单据FID
        /// </summary>
        /// <returns></returns>
        public string CZ_GetFID()
        {
            return (this.View.Model.DataObject as DynamicObject)["Id"] == null ? "0" : (this.View.Model.DataObject as DynamicObject)["Id"].ToString();
        }
        /// <summary>
        /// 获取基础资料
        /// </summary>
        /// <param name="sign">标识</param>
        /// <param name="property">属性</param>
        /// <returns></returns>
        private string CZ_GetBaseData(string sign, string property)
        {
            return this.View.Model.DataObject[sign] == null ? "" : (this.View.Model.DataObject[sign] as DynamicObject)[property].ToString();
        }
        /// <summary>
        /// 获取一般字段
        /// </summary>
        /// <param name="sign">标识</param>
        /// <returns></returns>
        private string CZ_GetCommonField(string sign)
        {
            return this.View.Model.DataObject[sign] == null ? "" : this.View.Model.DataObject[sign].ToString();
        }
        #endregion

        #region 数据库查询
        /// <summary>
        /// 基本方法 
        /// </summary>
        /// <param name="_sql"></param>
        /// <returns></returns>
        public DynamicObjectCollection CZDB_GetData(string _sql)
        {
            try
            {
                var obj = DBUtils.ExecuteDynamicObject(this.Context, _sql);
                return obj;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        #endregion
    }
}
