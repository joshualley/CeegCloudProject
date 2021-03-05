using CZ.CEEG.OABos.LeaveApply.LeaveType;
using Kingdee.BOS.App.Data;
using Kingdee.BOS.Core.Bill;
using Kingdee.BOS.Core.DynamicForm;
using Kingdee.BOS.Core.DynamicForm.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Core.Metadata;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.Util;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace CZ.CEEG.OABos.LeaveQueryNew
{
    [Description("请假查询")]
    [HotUpdate]
    public class CZ_CEEG_OABos_LeaveQueryNew : AbstractDynamicFormPlugIn
    {

        public override void AfterBindData(EventArgs e)
        {
            base.AfterBindData(e);
            string year = DateTime.Now.Year.ToString();
            this.View.Model.SetValue("FYear", year);
            this.View.UpdateView("FYear");
        }


        public override void ButtonClick(ButtonClickEventArgs e)
        {
            base.ButtonClick(e);
            switch (e.Key)
            {
                case "FSEARCH":
                    ShowQueryResult();
                    break;
                case "FCARRYDOWN": //FCarryDown
                    CarryDown();
                    break;
            }
        }


        /// <summary>
        /// 年休假结转
        /// </summary>
        private void CarryDown()
        {
            var currTime = DateTime.Now;
            string year = currTime.Year.ToString();
            string sql = "select FID from ora_t_LeaveHead where FIsOrigin=1 and YEAR(FCreateDate)='" + year + "';";
            var objs = CZDB_GetData(sql);
            if (objs.Count > 0)
            {
                string FID = objs[0]["FID"].ToString();
                //this.View.ShowMessage("去年的年休假已经结转到了今年，请勿重复操作！");
                this.View.ShowMessage("去年的年休假已经结转到了今年，是否要查看年假结转单？", MessageBoxOptions.YesNo,
                    new Action<MessageBoxResult>((result) =>
                    {
                        if (result == MessageBoxResult.Yes)
                        {
                            //跳转到初始年假页面
                            OpenInitLeave(FID);
                        }
                    }));
            }
            else
            {
                sql = "exec proc_czly_CreateInitLeave";
                CZDB_GetData(sql);
                sql = "select FID from ora_t_LeaveHead where FIsOrigin=1 and YEAR(FCreateDate)='" + year + "';";
                string FID = CZDB_GetData(sql)[0]["FID"].ToString();
                OpenInitLeave(FID);
            }
        }

        /// <summary>
        /// 打开请假年假单
        /// </summary>
        /// <param name="FID"></param>
        private void OpenInitLeave(string FID)
        {
            var para = new BillShowParameter();
            para.FormId = "kbea624189d8e4d829b68340507eda196"; //请假申请的标识
            para.OpenStyle.ShowType = ShowType.MainNewTabPage; //打开方式
            para.ParentPageId = this.View.PageId;
            para.PKey = FID;
            para.Status = OperationStatus.VIEW;
            this.View.ShowForm(para);
        }

        /// <summary>
        /// 显示请假查询结果
        /// </summary>
        private void ShowQueryResult()
        {
            long empId = long.Parse(CZ_GetBaseData("FName", "Id"));
            if(empId > 0)
            {
                CreateEntryRow(empId, 0);
                return;
            }
            string sql = @"select FID from T_HR_EMPINFO 
where FUSEORGID=100221 and FID not in (112568,113060) and FFORBIDSTATUS='A'
order by FNUMBER ";
            var empIds = DBUtils.ExecuteDynamicObject(Context, sql);

            this.View.Model.DeleteEntryData("FEntity");
            
            for (int i = 0; i < empIds.Count; i++)
            {
                CreateEntryRow(Convert.ToInt64(empIds[i]["FID"]), i);
            }
            this.View.UpdateView("FEntity");
        }


        private void CreateEntryRow(long empId, int rowNum)
        {
            this.Model.CreateNewEntryRow("FEntity");
            string type = CZ_GetValue("FLeaveType");
            string year = CZ_GetValue("FYear");
            var leave = new LeaveFactory().MakeLeave(Context, int.Parse(type), empId, 0);
            var data = leave.GetReportData();
            if(data != null)
            {
                this.Model.SetValue("FEYear", year, rowNum);
                this.Model.SetValue("FELeaveType", data.LeaveType, rowNum);
                this.Model.SetValue("FEName", data.Name, rowNum);
                this.Model.SetValue("FEAllowDays", data.AllowDays, rowNum);
                this.Model.SetValue("FCurrDays", data.CurrDays, rowNum);
                this.Model.SetValue("FLastYearLeft", data.LastYearLeft, rowNum);
                this.Model.SetValue("FLeftDays", data.LeftDays, rowNum);
                this.Model.SetValue("FSurplusDays", data.SurplusDays, rowNum);
            }

            
        }

        private DynamicObjectCollection GetLeaveData()
        {
            string name = CZ_GetBaseData("FName", "Id");
            string type = CZ_GetValue("FLeaveType");
            string year = CZ_GetValue("FYear");
            string sql = string.Format("exec proc_czly_LeaveQuery @FNameId='{0}',@FLeaveType='{1}',@FYear='{2}';", name, type, year);
            return CZDB_GetData(sql);
        }

        #region 基本取数方法
        public string CZ_GetValue(string sign)
        {
            return this.View.Model.GetValue(sign) == null ? "" : this.View.Model.GetValue(sign).ToString();
        }
        /// <summary>
        /// 获取基础资料
        /// </summary>
        /// <param name="sign">标识</param>
        /// <param name="property">属性</param>
        /// <returns></returns>
        private string CZ_GetBaseData(string sign, string property)
        {
            return this.View.Model.DataObject[sign] == null ? "0" : (this.View.Model.DataObject[sign] as DynamicObject)[property].ToString();
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

        #region 查询
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
