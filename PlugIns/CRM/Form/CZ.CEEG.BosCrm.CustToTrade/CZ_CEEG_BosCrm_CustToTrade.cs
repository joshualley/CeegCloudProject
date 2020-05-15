using Kingdee.BOS.Core.Bill;
using Kingdee.BOS.Core.DynamicForm;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Core.List.PlugIn;
using Kingdee.BOS.Core.List.PlugIn.Args;
using Kingdee.BOS.Core.Metadata;
using Kingdee.BOS.Util;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace CZ.CEEG.BosCrm.CustToTrade
{
    [Description("客户转正式")]
    [HotUpdate]
    public class CZ_CEEG_BosCrm_CustToTrade : AbstractListPlugIn
    {
        /// <summary>
        /// 菜单点击事件，表单插件同样适用
        /// </summary>
        /// <param name="e"></param>
        public override void BarItemClick(BarItemClickEventArgs e)
        {
            switch (e.BarItemKey.ToUpperInvariant())
            {
                case "TBVIEW": //tbView 打开
                    Act_OpenCust();
                    break;
                default:
                    break;
            }
        }

        public override void ListRowDoubleClick(ListRowDoubleClickArgs e)
        {
            base.ListRowDoubleClick(e);
            //Act_OpenCust();
        }

        /// <summary>
        /// 以编辑状态打开现存潜在客户表单
        /// </summary>
        private void Act_OpenCust()
        {
            //IDynamicFormView view = this.View.GetView("BD_Customer_All");
            //if (view != null)
            //{
            //    view.Close();
            //    this.View.SendDynamicFormAction(view);
            //}

            var fid = this.ListView.CurrentSelectedRowInfo.PrimaryKeyValue.ToString();
            
            if(fid != "")
            {
                string pageId = Guid.NewGuid().ToString();
                var para = new BillShowParameter();
                para.FormId = "BD_Customer_All";//58bf6037-cea8-4934-8c74-b7f6bf9a19db
                para.OpenStyle.ShowType = ShowType.Modal;
                para.ParentPageId = this.View.PageId;
                para.PageId = pageId;
                para.Status = OperationStatus.EDIT;
                para.PKey = fid;
                //para.Status = OperationStatus.VIEW;

                this.View.ShowForm(para);
            }
        }
    }
}
