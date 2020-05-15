using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Data;
using System.Collections;

using Kingdee.BOS.Contracts;
using Kingdee.BOS.Core.Bill.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Core.Metadata;
using Kingdee.BOS.Core.SqlBuilder;
using Kingdee.BOS.Orm.DataEntity;

using Kingdee.BOS.App.Data;
using Kingdee.BOS.Core.DynamicForm;
using Kingdee.BOS.Core.Bill;
using Kingdee.BOS.Util;
using Kingdee.BOS.Msg;
using Kingdee.BOS.ServiceHelper.Messages;

using Kingdee.BOS;


namespace CZ.CEEG.OABos.EmpinfoCalScyYear
{
    [Description("入职前社会工作年")]
    [HotUpdate]
    public class CZ_CEEG_OABos_EmpinfoCalScyYear : AbstractBillPlugIn
    {

        public override void DataChanged(DataChangedEventArgs e)
        {
            base.DataChanged(e);
            if(this.Context.ClientType.ToString() != "Mobile")
            {
                switch (e.Field.Key.ToString())
                {
                    case "FJoinDate": //参加工作日期
                        CalSYear();
                        break;
                    case "F_HR_BobDate": //HR入职日期
                        CalSYear();
                        break;
                }
            }
            
        }

        private void CalSYear()
        {
            var FJoinDate = this.View.Model.GetValue("FJoinDate") == null ? "" : this.View.Model.GetValue("FJoinDate").ToString();
            var F_HR_BobDate = this.View.Model.GetValue("F_HR_BobDate") == null ? "" : this.View.Model.GetValue("F_HR_BobDate").ToString();

            if(FJoinDate != "" && F_HR_BobDate != "")
            {
                var sDate = DateTime.Parse(FJoinDate);
                var eDate = DateTime.Parse(F_HR_BobDate);
                int sYear = sDate.Month > 2 ? sDate.Year : sDate.Month == 2 && sDate.Day == 29 ? sDate.Year : sDate.Year + 1;
                int eYear = eDate.Month > 2 ? eDate.Year : eDate.Month == 2 && eDate.Day == 29 ? eDate.Year : eDate.Year - 1;
                int extraDay = IncludeLeapNum(sYear, eYear);
                double SYear = (eDate - sDate).Days <= 0 ? 0 : Math.Floor(((eDate - sDate).Days - extraDay) / 365.0);
                this.View.Model.SetValue("FSocietyYear", SYear);
            }
        }

        /// <summary>
        /// 计算包含多少闰年
        /// </summary>
        /// <param name="sYear"></param>
        /// <param name="eYear"></param>
        /// <returns></returns>
        private int IncludeLeapNum(int sYear, int eYear)
        {
            if (eYear <= sYear) return 0;
            int num = 0;
            for(int i = sYear; i <= eYear; i++)
            {
                if(i % 4 == 0 && i % 100 != 0)
                {
                    num++;
                }
            }
            return num;
        }

    }
}
