using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System;

using Kingdee.BOS.Core;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Core.Metadata;
using Kingdee.BOS.Core.Bill.PlugIn;
using Kingdee.BOS.FileServer.Core;
using Kingdee.BOS.FileServer.Core.Object;
using Kingdee.BOS.FileServer.ProxyService;
using Kingdee.BOS.KDThread;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.ServiceHelper;
using Kingdee.BOS.Util;
using Kingdee.BOS.App.Data;
using Kingdee.BOS.Core.DynamicForm.PlugIn.ControlModel;

namespace CZ.CEEG.OABos.LeaveApply
{
    [HotUpdate]
    [Description("BOS请假控制Old")]
    public class CZ_CEEG_OABos_LeaveApply : AbstractBillPlugIn
    {
        //需要实现的功能：
        //1.请假日期计算
        //2.调休日期修正
        //3.时间、时段同步
        //4.员工假期提示：总、已用、剩余
        //5.保存、提交时进行可请天数校验

        #region Actions
        /// <summary>
        /// 新增时设置由表头带出表体的请假人（暂定）
        /// </summary>
        private void addEntryLeaver()
        {
            if(CZ_GetCommonField("DocumentStatus") == "Z")
            {
                string sql = String.Format(@"exec proc_czty_GetLoginUser2Emp @FUserID='{0}'", this.Context.UserId.ToString());
                var obj = CZDB_GetData(sql);
                int Row = this.View.Model.GetEntryRowCount("FEntity") - 1;
                //this.View.ShowMessage(Row.ToString());
                if (obj.Count > 0)
                {
                    this.View.Model.SetItemValueByID("FName", obj[0]["FEmpID"].ToString(), Row);
                    this.View.Model.SetItemValueByID("FPost", obj[0]["FPostID"].ToString(), Row);
                    this.View.Model.SetItemValueByID("FDept", obj[0]["FDeptID"].ToString(), Row);
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

        private void dyLoadLeaveType()
        {
            string FDocumentStatus = CZ_GetCommonField("DocumentStatus");
            if(FDocumentStatus == "Z" || FDocumentStatus == "A" || FDocumentStatus == "D")
            {
                //获取请假类别
                List<EnumItem> list = new List<EnumItem>();
                string sql = @"select FCAPTION,fi.FENUMID,FVALUE from T_META_FORMENUMITEM fi
                inner join T_META_FORMENUMITEM_L fil on fil.FENUMID=fi.FENUMID
                inner join T_META_FORMENUM_L fl on fl.FID=fi.FID
                where fl.FNAME='OA请假类别'";
                var obj = CZDB_GetData(sql);

                sql = "EXEC proc_czly_GetSalesmanIdByUserId @FUserId='" + this.Context.UserId.ToString() + "'";
                var Smans = CZDB_GetData(sql);
                if (Smans.Count > 0)
                {
                    foreach(var row in obj)
                    {
                        if(row["FVALUE"].ToString() == "3")
                        {
                            continue;
                        }
                        EnumItem item = new EnumItem();
                        item.Caption = new Kingdee.BOS.LocaleValue(row["FCAPTION"].ToString());
                        item.EnumId = row["FENUMID"].ToString();
                        item.Value = row["FVALUE"].ToString();
                        list.Add(item);
                    }
                }
                else
                {
                    foreach (var row in obj)
                    {
                        if (row["FVALUE"].ToString() == "20")
                        {
                            continue;
                        }
                        EnumItem item = new EnumItem();
                        item.Caption = new Kingdee.BOS.LocaleValue(row["FCAPTION"].ToString());
                        item.EnumId = row["FENUMID"].ToString();
                        item.Value = row["FVALUE"].ToString();
                        list.Add(item);
                    }
                }
                //this.View.ShowMessage(list.Count.ToString());
                this.View.GetControl<ComboFieldEditor>("FLeaveType").SetComboItems(list);
            }
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
                string startDate = _FEntity[e.Row]["FStartDate"] == null ? "" : _FEntity[e.Row]["FStartDate"].ToString();
                string endDate = _FEntity[e.Row]["FEndDate"] == null ? "" : _FEntity[e.Row]["FEndDate"].ToString();
                string startTimeFrame = _FEntity[e.Row]["FStartTimeFrame"] == null ? "" : _FEntity[e.Row]["FStartTimeFrame"].ToString();
                string endTimeFrame = _FEntity[e.Row]["FEndTimeFrame"] == null ? "" : _FEntity[e.Row]["FEndTimeFrame"].ToString();
                string sTime = _FEntity[e.Row]["FSTime"] == null ? "" : _FEntity[e.Row]["FSTime"].ToString();
                string eTime = _FEntity[e.Row]["FETime"] == null ? "" : _FEntity[e.Row]["FETime"].ToString();

                if (startDate != "" && startTimeFrame != "" && endDate != "" && endTimeFrame != "" && orgId != "")
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
            dyLoadLeaveType();
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
                //this.View.ShowMessage(type);
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
                if (Check())
                {
                    e.Cancel = true;
                }
                SetMaxLeaveDay();
            }
            else if(_opKey == "SAVE")
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
        private bool Check()
        {
            var _FEntity = this.View.Model.DataObject["FEntity"] as DynamicObjectCollection;
            string _FLeaveType = "";
            string _FStartDate = "";
            string _FStartTimeFrame = "";
            string _FEndDate = "";
            string _FEndTimeFrame = "";
            string _FDayNum = "";
            string _FName = "";
            string text = "";
            //获取请假人
            List<string> leavers = new List<string>();
            foreach (var _Row in _FEntity)
            {
                _FName = (_Row["FName"] as DynamicObject)["Id"].ToString();
                if (!leavers.Contains(_FName))
                {
                    leavers.Add(_FName);
                }
            }
            //对每个请假人的请假进行汇总，并分别验证
            foreach (string leaver in leavers)
            {
                int row = 0;
                for (int i = 0; i < _FEntity.Count; i++)
                {
                    row = i + 1;
                    _FName = (_FEntity[i]["FName"] as DynamicObject)["Id"].ToString();
                    _FLeaveType = _FEntity[i]["FLeaveType"].ToString();
                    if (leaver == _FName && _FLeaveType != "9") //不为调休
                    {
                        
                        if (_FLeaveType == "20") //销售员探亲假，不能超过6次
                        {
                            string sql = "select s.FID from V_BD_SALESMAN s inner join T_HR_EMPINFO e on e.FNUMBER=s.FNUMBER where e.FID='" + _FName + "'";
                            var Smans = CZDB_GetData(sql);
                            if(Smans.Count <= 0)
                            {
                                this.View.ShowMessage("请选择探亲假！");
                                return true;
                            }
                            DateTime currTime = DateTime.Now;
                            sql = string.Format(@"select FNAME from ora_t_Leave 
                            where YEAR(FStartDate)={0} and FLEAVETYPE=20 and FNAME={1}", currTime.Year, _FName);
                            var objs = CZDB_GetData(sql);
                            if(objs.Count > 6)
                            {
                                sql = String.Format("select * from T_HR_EMPINFO_L where FID='{0}'", _FName);
                                string name = CZDB_GetData(sql)[0]["FNAME"].ToString();
                                this.View.ShowErrMessage(name + "的探亲假提交失败，\n原因：\n超出了本年可请次数！");
                                return true;
                            }
                        }
                        _FStartDate = _FEntity[i]["FStartDate"].ToString().Split(' ')[0];
                        _FStartTimeFrame = _FEntity[i]["FStartTimeFrame"].ToString();
                        _FEndDate = _FEntity[i]["FEndDate"].ToString().Split(' ')[0];
                        _FEndTimeFrame = _FEntity[i]["FEndTimeFrame"].ToString();
                        _FDayNum = _FEntity[i]["FDayNum"].ToString();
                        text += row.ToString() + "#" + _FLeaveType + "#" + _FStartDate + "#" + _FStartTimeFrame + "#" + _FEndDate + "#" + _FEndTimeFrame + "#" + _FDayNum + "#,";
                    }
                    else if (leaver.Equals(_FName) && _FLeaveType == "9")
                    {
                        //验证调休
                        string sql = String.Format(@"exec proc_czly_GetHolidayShiftSituation @EmpID='{0}'", _FName);
                        var obj = CZDB_GetData(sql);
                        string _FOverHours = obj[0]["FOverHours"].ToString();
                        string _FRestHours = obj[0]["FRestHours"].ToString();
                        string _FLeftHours = obj[0]["FLeftHours"].ToString();
                        _FDayNum = _FEntity[i]["FDayNum"].ToString();
                        if (float.Parse(_FDayNum) * 8.0 > float.Parse(_FLeftHours))
                        {
                            sql = String.Format("select * from T_HR_EMPINFO_L where FID='{0}'", _FName);
                            string name = CZDB_GetData(sql)[0]["FNAME"].ToString();
                            this.View.ShowErrMessage(name + "的调休提交失败！\n原因：\n超出了可调休的时长！");
                            return true;
                        }
                    }
                    
                }

                //验证其他请假
                if (_FEntity.Count > 0 && text != "")
                {

                    string sql = String.Format(@"exec proc_cztyBos_ChkLeaveDaysT
                        @FEmpID = '{0}', 
                        @text = '{1}',
                        @tax = ',', 
                        @taxi = '#'", leaver, text);
                    var obj = CZDB_GetData(sql);
                    if (obj.Count > 0 && obj[0]["FChkMkt"].ToString() == "1")
                    {
                        //this.View.ShowErrMessage(obj[0]["FChkRs"].ToString());
                        sql = String.Format("select * from T_HR_EMPINFO_L where FID='{0}'", leaver);
                        string name = CZDB_GetData(sql)[0]["FNAME"].ToString();
                        this.View.ShowMessage(name + "的请假提交失败！\n原因：\n" + obj[0]["FChkRs"].ToString());
                        return true;
                    }
                    else
                    {
                        //obj第0行是判定信息
                        for (int i = 1; i < obj.Count; i++)
                        {
                            if(CheckByType(obj[i], leaver))
                                return true;
                        }
                    }
                }
            }
            //验证请假天数不为0
            foreach(var row in _FEntity)
            {
                if ("0".Equals(row["FDayNum"].ToString()))
                {
                    this.View.ShowMessage("请假提交失败！\n原因：\n请假天数需要大于0！");
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 根据类别返回不同的校验结果
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        private bool CheckByType(DynamicObject obj_i, string _FNAME)
        {
            float _FAllowDays = float.Parse(obj_i["FAllowDays"].ToString());
            float _FHisDays = float.Parse(obj_i["FHisDays"].ToString());
            float applyDays = float.Parse(obj_i["FDays"].ToString());
            float tempHisDays = 0;
            string type = obj_i["FTypeVal"].ToString();
            string row = obj_i["FInRow"].ToString();
            string sql = String.Format("select * from T_HR_EMPINFO_L where FID='{0}'", _FNAME);
            string name = CZDB_GetData(sql)[0]["FNAME"].ToString();
            
            switch (type)
            {
                case "1":
                    if (applyDays > (_FAllowDays - _FHisDays))
                    {
                        this.View.ShowMessage(name + "第" + row + "行的产检假提交失败！\n原因：\n超出了剩余可请假天数！");
                        return true;
                    }
                    break;
                case "2":
                    if (applyDays > (15 - _FHisDays))
                    {
                        this.View.ShowMessage(name + "第" + row + "行的事假提交失败！\n原因：\n超出了剩余可请假天数！");
                        return true;
                    }
                    else if (applyDays > 5)
                    {
                        this.View.ShowMessage(name + "第" + row + "行的事假提交失败！\n原因：\n超出了本次可请假天数！");
                        return true;
                    }
                    break;
                case "3":
                    tempHisDays = float.Parse(GetQuery("6", _FNAME, 0)[1]["FHisDays"].ToString());
                    if (applyDays > (_FAllowDays - _FHisDays - tempHisDays))
                    {
                        this.View.ShowMessage(name + "第" + row + "行的探亲假提交失败！\n原因：\n超出了剩余可请假天数！");
                        return true;
                    }
                    break;
                case "4":
                    if (applyDays > (_FAllowDays - _FHisDays))
                    {
                        this.View.ShowMessage(name + "第" + row + "行的病假提交失败！\n原因：\n超出了剩余可请假天数！");
                        return true;
                    }
                    break;
                case "5":
                    if (applyDays > (_FAllowDays - _FHisDays))
                    {
                        this.View.ShowMessage(name + "第" + row + "行的护理假提交失败！\n原因：\n超出了剩余可请假天数！");
                        return true;
                    }
                    break;
                case "6":
                    tempHisDays = float.Parse(GetQuery("3", _FNAME, 0)[1]["FHisDays"].ToString());
                    if (applyDays > (_FAllowDays - _FHisDays - tempHisDays))
                    {
                        this.View.ShowMessage(name + "第" + row + "行的年休假提交失败！\n原因：\n超出了可请假天数！");
                        return true;
                    }
                    break;
                case "7":
                    if (applyDays > _FAllowDays)
                    {
                        this.View.ShowMessage(name + "第" + row + "行的丧假提交失败！\n原因：\n超出了本次可请假天数！");
                        return true;
                    }
                    break;
                case "8":
                    //婚假一次性休完
                    if (applyDays > _FAllowDays)
                    {
                        this.View.ShowMessage(name + "第" + row + "行的婚假提交失败！\n原因：\n超出了本次可请假天数！");
                        return true;
                    }
                    break;
                case "9":
                    //调休,额外计算
                    break;
                case "10":
                    //msg = "工伤，不限请假天数。";
                    break;
                case "11":
                    //msg = "哺乳假，每次可请1小时";
                    break;
                case "12":
                    if (applyDays > _FAllowDays - _FHisDays)
                    {
                        this.View.ShowMessage(name + "第" + row + "行的顺产假提交失败！\n原因：\n超出了剩余可请假天数！");
                        return true;
                    }
                    break;
                case "13":
                    if (applyDays > _FAllowDays - _FHisDays)
                    {
                        this.View.ShowMessage(name + "第" + row + "行的剖腹产假提交失败！\n原因：\n超出了剩余可请假天数！");
                        return true;
                    }
                    break;
                case "14":
                    if (applyDays > _FAllowDays - _FHisDays)
                    {
                        this.View.ShowMessage(name + "第" + row + "行的流产假(90天内)提交失败！\n原因：\n超出了剩余可请假天数！");
                        return true;
                    }
                    break;
                case "15":
                    if (applyDays > _FAllowDays - _FHisDays)
                    {
                        this.View.ShowMessage(name + "第" + row + "行的流产假(210天内)提交失败！\n原因：\n超出了剩余可请假天数！");
                        return true;
                    }
                    break;
                case "16":
                    if (applyDays > _FAllowDays - _FHisDays)
                    {
                        this.View.ShowMessage(name + "第" + row + "行的流产假(210天以上)提交失败！\n原因：\n超出了剩余可请假天数！");
                        return true;
                    }
                    break;
                case "17":
                    //msg = "参军，不限请假天数。";
                    break;
                case "18":
                    if (applyDays > _FAllowDays)
                    {
                        this.View.ShowMessage(name + "第" + row + "行的拆迁假提交失败！\n原因：\n超出了本次可请假天数！");
                        return true;
                    }
                    break;
                case "19":
                    if (applyDays > _FAllowDays)
                    {
                        this.View.ShowMessage(name + "第" + row + "行的献血假提交失败！\n原因：\n超出了本次可请假天数！");
                        return true;
                    }
                    break;
                case "20": //销售员探亲假
                    break;
                default:
                    break;
            }
            return false;
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
            //string _FApplyID = this.View.BillModel.DataObject["FApplyID"] == null ? "0" : (this.View.BillModel.DataObject["FApplyID"] as DynamicObject)["Id"].ToString();
            if (_FNAME == "0")
            {
                if (!isInit)
                {
                    this.View.ShowMessage("姓名为空，请先选择请假人姓名！");
                }
                return "";
            }
            var obj = GetQuery(_FLeaveType, _FNAME, Row);
            if (obj.Count > 0 && obj[1]["FErrMkt"].ToString() == "1")
            {
                this.View.ShowMessage("HR信息缺失，请联系相关人员补录HR信息！");
                return "";
            }

            float _FAllowDays = float.Parse(obj[1]["FAllowDays"].ToString());
            float _FHisDays = float.Parse(obj[1]["FHisDays"].ToString());
            float tempHisDays = 0;
            string msg = "";
            /*
            1   产检假  2   事假 3	探亲假 4	病假 5	护理假 6	年休假 7	丧假 8	婚假
            9	调休    10	工伤 11	哺乳假 12	产假-顺产 13	产假-剖腹产 14	产假-流产-90天内
            15	产假-流产-210天内 16	产假-流产-210天以上 17	其他-参军 18	其他-拆迁 19	其他-献血
            * */

            string year = DateTime.Now.Year.ToString();
            string sql = "";
            DynamicObjectCollection data = null;
            float lastYearLeftDays = 0;

            switch (_FLeaveType)
            {
                case "1":
                    msg = String.Format("产检假，总共可请{0}天，已请{1}天，剩余{2}天。",
                                        _FAllowDays, _FHisDays, (_FAllowDays - _FHisDays).ToString("f2"));
                    break;
                case "2":
                    
                    msg = String.Format("事假，本年可请15天，本次可请{0}天，已请{1}天，剩余{2}天。",
                                       _FAllowDays, _FHisDays, (15 - _FHisDays).ToString("f2"));
                    break;
                case "3":
                    sql = string.Format(@"select FDayNum from ora_t_Leave e
                                                inner join ora_t_LeaveHead h on e.FID=h.FID
                                                where FIsOrigin=1 and Year(h.FCreateDate)='{0}' and FName='{1}'",
                                                year, _FNAME);
                    data =  CZDB_GetData(sql);
                    
                    if (data.Count > 0) lastYearLeftDays = -float.Parse(data[0]["FDayNum"].ToString());
                    tempHisDays = float.Parse(GetQuery("6", _FNAME, 0)[1]["FHisDays"].ToString());
                    sql = "select s.FID from V_BD_SALESMAN s inner join T_HR_EMPINFO e on e.FNUMBER=s.FNUMBER where e.FID='" + _FNAME + "'";
                    var Smans = CZDB_GetData(sql);
                    if (Smans.Count > 0)
                    {
                        msg = "请选择销售员探亲假！";
                        this.View.ShowMessage(msg);
                        return msg;
                    }
                    msg = String.Format("探亲假，本年目前可请{0}天，已请{1}天，年休假已请{2}天，剩余(可请){3}天。",
                          _FAllowDays, _FHisDays, tempHisDays + lastYearLeftDays, (_FAllowDays - _FHisDays - tempHisDays).ToString("f2"));
                    
                    break;
                case "4":
                    msg = String.Format("病假，本年可请{0}天，已请{1}天，剩余{2}天。",
                                       _FAllowDays, _FHisDays, (_FAllowDays - _FHisDays).ToString("f2"));
                    break;
                case "5":
                    msg = String.Format("护理假，本年可请{0}天，已请{1}天，剩余{2}天。",
                                       _FAllowDays, _FHisDays, (_FAllowDays - _FHisDays).ToString("f2"));
                    break;
                case "6":
                    
                    sql = string.Format(@"select FDayNum from ora_t_Leave e
                                                inner join ora_t_LeaveHead h on e.FID=h.FID
                                                where FIsOrigin=1 and Year(h.FCreateDate)='{0}' and FName='{1}'",
                                                year, _FNAME);
                    data = CZDB_GetData(sql);
                    if (data.Count > 0) lastYearLeftDays = -float.Parse(data[0]["FDayNum"].ToString());
                    tempHisDays = float.Parse(GetQuery("3", _FNAME, 0)[1]["FHisDays"].ToString());
                    msg = String.Format("年休假，本年目前可请{0}天，已请{1}天，探亲假已请{2}天，剩余(可请){3}天。",
                                       _FAllowDays, _FHisDays + lastYearLeftDays, tempHisDays, (_FAllowDays - _FHisDays - tempHisDays).ToString("f2"));
                    break;
                case "7":
                    msg = String.Format("丧假，本次可请{0}天。",
                                       _FAllowDays);
                    break;
                case "8":
                    //婚假一次性休完
                    msg = String.Format("婚假，本次可请{0}天。",
                                       _FAllowDays);
                    break;
                case "9":
                    //调休,额外计算
                    msg = HolidayShiftTip(Row);
                    break;
                case "10":
                    msg = "工伤，不限请假天数。";
                    break;
                case "11":
                    msg = "哺乳假，每次可请1小时";
                    break;
                case "12":
                    msg = String.Format("顺产假，可请{0}天，已请{1}天，剩余{2}天。",
                                       _FAllowDays, _FHisDays, (_FAllowDays - _FHisDays).ToString("f2"));
                    break;
                case "13":
                    msg = String.Format("剖腹产假，可请{0}天，已请{1}天，剩余{2}天。",
                                       _FAllowDays, _FHisDays, (_FAllowDays - _FHisDays).ToString("f2"));
                    break;
                case "14":
                    msg = String.Format("流产(90天内)，可请{0}天，已请{1}天，剩余{2}天。",
                                       _FAllowDays, _FHisDays, (_FAllowDays - _FHisDays).ToString("f2"));
                    break;
                case "15":
                    msg = String.Format("流产(210天内)，可请{0}天，已请{1}天，剩余{2}天。",
                                       _FAllowDays, _FHisDays, (_FAllowDays - _FHisDays).ToString("f2"));
                    break;
                case "16":
                    msg = String.Format("流产(210天以上)，可请{0}天，已请{1}天，剩余{2}天。",
                                       _FAllowDays, _FHisDays, (_FAllowDays - _FHisDays).ToString("f2"));
                    break;
                case "17":
                    msg = "参军，不限请假天数。";
                    break;
                case "18":
                    msg = String.Format("拆迁，本次可请{0}天，已请{1}天。",
                                       _FAllowDays, _FHisDays);
                    break;
                case "19":
                    msg = String.Format("献血，本次可请{0}天，已请{1}天。",
                                       _FAllowDays, _FHisDays);
                    break;
                case "20": //销售员探亲假
                    sql = "select s.FID from V_BD_SALESMAN s inner join T_HR_EMPINFO e on e.FNUMBER=s.FNUMBER where e.FID='" + _FNAME + "'";
                    var Sman = CZDB_GetData(sql);
                    if (Sman.Count <= 0)
                    {
                        msg = "请选择探亲假！";
                        this.View.ShowMessage(msg);
                        return msg;
                    }
                    sql = string.Format(@"select * from ora_t_Leave 
                            where YEAR(FStartDate)={0} and FLEAVETYPE=20  and FNAME={1}", DateTime.Now.Year, _FNAME);
                    var objs = CZDB_GetData(sql);
                    msg = String.Format("探亲假，本年可请6次, 已请{0}次。", objs.Count);
                    break;
                default:
                    break;
            }
            return msg;
        }

        /// <summary>
        /// 获取已请假天数
        /// </summary>
        /// <returns></returns>
        private DynamicObjectCollection GetQuery(string _FLeaveType, string _FNAME, int Row)
        {
            //-- @text='行号#请假类型#开始日期#AP#结束日期#AP#天数,……'	 AP:上[1]下[2]午
            //--exec proc_cztyBos_ChkLeaveDays @FEmpID = '113044', @text = '0#1#2019-10-01#1#2019-10-02#1#2.5#,1#13#2019-09-22#1#2019-09-30#2#17#', @tax = ',', @taxi = '#'
            string sql = String.Format(@"exec proc_cztyBos_ChkLeaveDaysT
                        @FEmpID = '{0}', 
                        @text = '{1}#{2}#2019-10-01#1#2019-10-02#1#2.5#',
                        @tax = ',', 
                        @taxi = '#'", _FNAME, Row.ToString(), _FLeaveType);
            return CZDB_GetData(sql);
        }

        /// <summary>
        /// 请假类型转为调休时，进行查询，提示
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        private string HolidayShiftTip(int Row)
        {
            string _FName = this.View.Model.GetValue("FName", Row) == null ? "0" : (this.View.Model.GetValue("FName", Row) as DynamicObject)["Id"].ToString();
            if (_FName == "0")
            {
                this.View.ShowMessage("姓名为空，请先选择请假人姓名！");
                return "";
            }
            string sql = String.Format(@"exec proc_czly_GetHolidayShiftSituation @EmpID='{0}'", _FName);
            var obj = CZDB_GetData(sql);
            string _FOverHours = obj[0]["FOverHours"].ToString();
            string _FRestHours = obj[0]["FRestHours"].ToString();
            string _FLeftHours = obj[0]["FLeftHours"].ToString();
            string day = (float.Parse(_FLeftHours) / 8.0).ToString();
            return "共加班" + _FOverHours + "小时，已调休" + _FRestHours + "小时，剩余" + _FLeftHours + "小时，折合" + day + "天。";
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
