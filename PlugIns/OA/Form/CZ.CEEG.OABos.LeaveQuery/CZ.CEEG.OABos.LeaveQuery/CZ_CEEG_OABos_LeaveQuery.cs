using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Data;
using System.Collections;

using Kingdee.BOS.Contracts;
using Kingdee.BOS.Core.Bill;
using Kingdee.BOS.Core.Bill.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Core.Metadata;
using Kingdee.BOS.Core.SqlBuilder;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.Util;
using Kingdee.BOS.App.Data;
using Kingdee.BOS.Core.DynamicForm;

namespace CZ.CEEG.OABos.LeaveQuery
{
    [Description("请假查询")]
    [HotUpdate]
    public class CZ_CEEG_OABos_LeaveQuery : AbstractDynamicFormPlugIn
    {
        private bool isFirst = true;
        public override void AfterBindData(EventArgs e)
        {
            base.AfterBindData(e);
            if (isFirst)
            {
                isFirst = false;
                string year = DateTime.Now.Year.ToString();
                this.View.Model.SetValue("FYear", year);
                this.View.UpdateView("FYear");
            }
            
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
            string sql = "select FID from ora_t_LeaveHead where FIsOrigin=1 and YEAR(FCreateDate)='"+ year +"';";
            var objs = CZDB_GetData(sql);
            if(objs.Count > 0)
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
            para.OpenStyle.ShowType = ShowType.InContainer; //打开方式
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
            var data = GetLeaveData();
            this.View.Model.DeleteEntryData("FEntity");
            for (int i = 0; i < data.Count; i++)
            {
                CreateEntryRow(data[i], i);
            }
            this.View.UpdateView();
        }


        private void CreateEntryRow(DynamicObject row, int rowNum)
        {
            this.Model.CreateNewEntryRow("FEntity");
            this.Model.SetValue("FEYear", CZ_GetValue("FYear"), rowNum);
            this.Model.SetValue("FELeaveType", row["类别值"].ToString(), rowNum);
            this.View.Model.SetItemValueByID("FEName", row["员工内码"].ToString(), rowNum);
            this.Model.SetValue("FEAllowDays", row["本年可请"].ToString(), rowNum);
            this.Model.SetValue("FCurrDays", row["目前可请"].ToString(), rowNum);
            this.Model.SetValue("FLastYearLeft", row["上年结转"].ToString(), rowNum);
            this.Model.SetValue("FLeftDays", row["已请天数"].ToString(), rowNum);
            this.Model.SetValue("FSurplusDays", row["剩余可请"].ToString(), rowNum);
        }

        private DynamicObjectCollection GetLeaveData()
        {
            string name = CZ_GetBaseData("FName", "Id");
            string type = CZ_GetValue("FLeaveType");
            string year = CZ_GetValue("FYear");
            string sql = string.Format("exec proc_czly_LeaveQuery @FNameId='{0}',@FLeaveType='{1}',@FYear='{2}';", name,type,year);
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
