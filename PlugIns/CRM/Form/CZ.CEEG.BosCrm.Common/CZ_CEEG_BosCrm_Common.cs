using Kingdee.BOS.App.Data;
using Kingdee.BOS.Core;
using Kingdee.BOS.Core.Bill.PlugIn;
using Kingdee.BOS.Util;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace CZ.CEEG.BosCrm.Common
{
    [Description("CrmBos通用")]
    [HotUpdate]
    public class CZ_CEEG_BosCrm_Common : AbstractBillPlugIn
    {
        public override void AfterBindData(EventArgs e)
        {
            base.AfterBindData(e);
            SetDefualtField();

        }

        /// <summary>
        /// 设置默认组织部门
        /// </summary>
        private void SetDefualtField()
        {

            string FDocumentStatus = this.View.Model.GetValue("FDocumentStatus").ToString();
            if (FDocumentStatus != "Z")
            {
                return;
            }
            string userId = this.Context.UserId.ToString();
            string sql = "EXEC proc_czty_GetLoginUser2Emp @FUserID='" + userId + "'";
            var objs = DBUtils.ExecuteDynamicObject(this.Context, sql);

            if (objs.Count > 0)
            {
                string FOrgID = objs[0]["FORGID"].ToString();
                string FDeptID = objs[0]["FDeptID"].ToString();
                string FManager = objs[0]["FGManager"].ToString();
                string FPostNameSup = objs[0]["FSuperiorPost"].ToString();
                string formId = this.View.GetFormId();
                if(formId != "STK_MISCELLANEOUS" && formId != "STK_MisDelivery")
                {
                    this.View.Model.SetValue("FOrgID", FOrgID);
                    this.View.Model.SetValue("FDeptID", FDeptID);
                    
                }
                
                this.View.Model.SetValue("FManager", FManager);
                this.View.Model.SetValue("FPostNameSup", FPostNameSup);
            }

            sql = "exec proc_czly_GetSalesmanIdByUserId @FUserId='" + userId + "'";

            objs = DBUtils.ExecuteDynamicObject(this.Context, sql);
            if(objs.Count > 0)
            {
                this.View.Model.SetValue("FSalerID", objs[0]["FSalesmanId"].ToString());
                this.View.Model.SetValue("FDept", objs[0]["FDeptID"].ToString());
            }
        }
    }
}
