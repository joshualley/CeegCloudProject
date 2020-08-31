using Kingdee.BOS;
using Kingdee.BOS.App.Data;
using Kingdee.BOS.Core.DynamicForm;
using Kingdee.BOS.Core.DynamicForm.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Core.DynamicForm.PlugIn.ControlModel;
using Kingdee.BOS.Core.Metadata;
using Kingdee.BOS.Core.Metadata.ControlElement;
using Kingdee.BOS.Core.Metadata.EntityElement;
using Kingdee.BOS.Core.Metadata.FieldElement;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.Util;
using System;
using System.ComponentModel;
using System.Data;

namespace CZ.CEEG.Report.AccountQueryCond
{
    [Description("费用台账报表查询条件")]
    [HotUpdate]
    public class CZ_CEEG_Report_AccountQueryCond : AbstractDynamicFormPlugIn
    {
        public override void AfterBindData(EventArgs e)
        {
            base.AfterBindData(e);
            DateTime now = DateTime.Now;
            string sDt = string.Format("{0}-{1}-01", now.Year, now.Month);
            this.View.Model.SetValue("FSDate", sDt);
            this.View.Model.SetValue("FEDate", now.ToString());
            this.View.UpdateView("FSDate");
            this.View.UpdateView("FEDate");
        }

        public override void BeforeF7Select(BeforeF7SelectEventArgs e)
        {
            base.BeforeF7Select(e);
            string key = e.FieldKey.ToUpperInvariant();
            switch(key)
            {
                case "FDEPTID":
                    break;
            }
        }

        public override void AfterButtonClick(AfterButtonClickEventArgs e)
        {
            base.AfterButtonClick(e);
            string key = e.Key.ToUpperInvariant();
            switch(key)
            {
                case "FQUERY":
                    string FSDate = this.View.Model.GetValue("FSDate") == null ? "" : this.View.Model.GetValue("FSDate").ToString();
                    string FEDate = this.View.Model.GetValue("FEDate") == null ? "" : this.View.Model.GetValue("FEDate").ToString();
                    string FOrgId = this.View.Model.GetValue("FOrgId") == null ? "0" :
                        (this.View.Model.GetValue("FOrgId") as DynamicObject)["Id"].ToString();
                    string FDeptID = this.View.Model.GetValue("FDeptID") == null ? "0" :
                        (this.View.Model.GetValue("FDeptID") as DynamicObject)["Id"].ToString();
                    string FAccountId = this.View.Model.GetValue("FAccountId") == null ? "0" :
                        (this.View.Model.GetValue("FAccountId") as DynamicObject)["Id"].ToString();


                    DynamicFormShowParameter param = new DynamicFormShowParameter();
                    param.ParentPageId = this.View.PageId;
                    param.FormId = "ora_CZ_CostAmount";
                    param.OpenStyle.ShowType = ShowType.MainNewTabPage;

                    param.CustomParams.Add("FSDate", FSDate);
                    param.CustomParams.Add("FEDate", FEDate);
                    param.CustomParams.Add("FOrgId", FOrgId);
                    param.CustomParams.Add("FDeptID", FDeptID);
                    param.CustomParams.Add("FAccountId", FAccountId);

                    this.View.ShowForm(param);
                    break;
            }
        }
    }
}
