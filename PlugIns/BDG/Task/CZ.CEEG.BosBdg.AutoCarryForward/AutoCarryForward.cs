using Kingdee.BOS;
using Kingdee.BOS.App.Data;
using Kingdee.BOS.Contracts;
using Kingdee.BOS.Core;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

// CZ.CEEG.BosBdg.AutoCarryForward.AutoCarryForward,CZ.CEEG.BosBdg.AutoCarryForward

namespace CZ.CEEG.BosBdg.AutoCarryForward
{
    [Description("每月自动结转")]
    public class AutoCarryForward : IScheduleService
    {
        private Context mContext;
        public void Run(Context ctx, Schedule schedule)
        {
            mContext = ctx;
            CarryForward_EveryMonthStart();
        }


        private void CarryForward_EveryMonthStart()
        {
            var today = DateTime.Today;
            // 1月份不进行结转
            if (today.Month == 1) return;
            if (today.Day == 1)
            {
                // 结转预算
                string sql = $"select FID, FBraOffice from ora_BDG_BudgetMD where FYear={today.Year} and FMonth={today.Month-1}";
                var items = DBUtils.ExecuteDynamicObject(mContext, sql);
                foreach(var item in items)
                {
                    sql = string.Format("exec proc_czly_BugetCarryForward @FIDB='{0}',@FCreatorId='{1}',@FCreateOrgId='{2}'",
                        item["FID"], mContext.UserId, item["FBraOffice"]);
                    DBUtils.Execute(mContext, sql);
                }

                // 结转资金
                sql = $"select FID, FBraOffice from ora_BDG_CapitalMD where FYear={today.Year} and FMonth={today.Month - 1}";
                items = DBUtils.ExecuteDynamicObject(mContext, sql);
                foreach(var item in items)
                {
                    sql = string.Format("exec proc_czly_CapitalCarryForward @FID='{0}',@FCreatorId='{1}',@FCreateOrgId='{2}'",
                        item["FID"], mContext.UserId, item["FBraOffice"]);
                    DBUtils.Execute(mContext, sql);
                }
            }
        }
    }
}
