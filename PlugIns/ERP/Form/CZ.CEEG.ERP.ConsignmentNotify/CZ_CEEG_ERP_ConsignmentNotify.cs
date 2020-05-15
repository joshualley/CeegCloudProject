using Kingdee.BOS.App.Data;
using Kingdee.BOS.Core.Bill.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.Util;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace CZ.CEEG.ERP.ConsignmentNotify
{
    [Description("ERP发货通知")]
    [HotUpdate]
    public class CZ_CEEG_ERP_ConsignmentNotify : AbstractBillPlugIn
    {
        public override void DataChanged(DataChangedEventArgs e)
        {
            base.DataChanged(e);
            string key = e.Field.Key.ToString();
            switch (key)
            {
                case "FSalesManID":
                    SetSm();
                    break;
            }
        }

        public override void AfterBindData(EventArgs e)
        {
            base.AfterBindData(e);
            SetSm();
        }

        /// <summary>
        /// 设置销售员组织部门
        /// </summary>
        private void SetSm()
        {
            if(this.Context.ClientType.ToString() == "Mobile")
            {
                return;
            }
            string FDocumentStatus = this.View.Model.GetValue("FDocumentStatus").ToString();
            if (FDocumentStatus == "Z" || FDocumentStatus == "A")
            {
                string FSalesManID = this.View.Model.GetValue("FSalesManID") == null ? "0" : (this.View.Model.GetValue("FSalesManID") as DynamicObject)["Id"].ToString();
                if (FSalesManID != "0")
                {
                    string sql = "exec proc_czly_GetOrgDeptBySalemanId @SmId='" + FSalesManID + "'";
                    var objs = DBUtils.ExecuteDynamicObject(this.Context, sql);
                    if (objs.Count > 0)
                    {
                        this.View.Model.SetValue("FSmOrgId", objs[0]["FOrgID"].ToString());
                        this.View.Model.SetValue("FSmDeptID", objs[0]["FDeptID"].ToString());
                        this.View.Model.SetValue("F_Salemolie", objs[0]["FMobile"].ToString());
                    }
                }
            }
        }
    }
}
