using Kingdee.BOS.App.Data;
using Kingdee.BOS.Core.Bill;
using Kingdee.BOS.Core.DynamicForm;
using Kingdee.BOS.Core.DynamicForm.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Core.Metadata;
using Kingdee.BOS.JSON;
using Kingdee.BOS.Util;
using Kingdee.BOS.Workflow.ServiceHelper;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace CZ.CEEG.BosSys.WorkflowChart
{
    [HotUpdate]
    [Description("流程图中打开单据")]
    public class CZ_CEEG_BosSys_WorkflowChart : AbstractDynamicFormPlugIn
    {
        public override void OnInitialize(InitializeEventArgs e)
        {
            base.OnInitialize(e);
            
        }


        public override void AfterBarItemClick(AfterBarItemClickEventArgs e)
        {
            base.AfterBarItemClick(e);
            string key = e.BarItemKey.ToUpperInvariant();
            
            switch(key)
            {
                case "ORA_TBVIEW": //ora_tbView
                    string ProcKeys = this.View.OpenParameter.GetCustomParameter("ProcKeys") == null ? ""
                        : this.View.OpenParameter.GetCustomParameter("ProcKeys").ToString();

                    if (ProcKeys == "")
                    {
                        return;
                    }
                    var obj = KDObjectConverter.DeserializeObject<JSONObject>(ProcKeys);
                    string sql = "select * from t_WF_PiBiMap where FPROCINSTID='" + obj["ProcInstanceId"].ToString() + "'";
                    var res = DBUtils.ExecuteDynamicObject(this.Context, sql);
                    if(res.Count <= 0)
                    {
                        return;
                    }
                    string FID = res[0]["FKEYVALUE"].ToString();
                    string pageId = Guid.NewGuid().ToString();
                    var para = new BillShowParameter();
                    para.FormId = res[0]["FOBJECTTYPEID"].ToString();
                    para.OpenStyle.ShowType = ShowType.Modal;
                    para.ParentPageId = this.View.PageId;
                    para.PageId = pageId;
                    para.Status = OperationStatus.EDIT;
                    para.PKey = FID;

                    this.View.ShowForm(para);
                    break;
            }
        }
    }
}
