using System;
using System.Collections.Generic;
using System.ComponentModel;
using Kingdee.BOS;
using Kingdee.BOS.App.Data;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Core.List;
using Kingdee.BOS.Core.List.PlugIn;
using Kingdee.BOS.Core.Metadata.EntityElement;
using Kingdee.BOS.Msg;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.ServiceHelper.Messages;
using Kingdee.BOS.Util;

namespace CZ.CEEG.BosOA.DeliveryPlan.List
{
    [Description("[发货计划列表]发货计划列表发送消息按钮"), HotUpdate]
    public class Class1 : AbstractListPlugIn
    {
        public override void AfterBarItemClick(AfterBarItemClickEventArgs e)
        {
            base.AfterBarItemClick(e);
            if (e.BarItemKey.Equals("tbSelectSendMsg"))
            {
                // 获取选中行
                ListSelectedRowCollection list = this.ListView.SelectedRowsInfo;
                for (int i = 0; i < list.Count; i++)
                {
                    DynamicObjectDataRow dynamicObjectDataRow = (DynamicObjectDataRow)list[i].DataRow;
                    DynamicObject dynm = dynamicObjectDataRow.DynamicObject;
                    string sDate = (string)dynm["FSDATE"].ToString();
                    string eDate = (string)dynm["FEDATE"].ToString();
                    SendMsg(sDate, eDate);
                }
                if (list.Count > 0)
                    this.View.ShowMessage("提醒消息发送成功！");
                else
                    this.View.ShowMessage("请选择需要发送提醒的数据。");
            }


        }


        /// <summary>
        /// 发送消息
        /// </summary>
        private void SendMsg(string sDate, string eDate)
        {
            // 发送消息给多人       
            var items = DBUtils.ExecuteDynamicObject(Context, "/*dialect*/ " +
                " select distinct dc.FUSERID,sx.FOPENID,u.FNAME " +
                " from T_Delivery_Context_Control dc " +
                " join T_SEC_XTUSERMAP sx on dc.FUserId = sx.FUserId " +
                " join T_SEC_USER u on u.FUSERID = dc.FUserId " +
                " where dc.FuserId != null or dc.FuserId != '' ");
            List<Dictionary<string, string>> sendInfos = new List<Dictionary<string, string>>();
            for (int i = 0; i < items.Count; i++)
            {
                if (items[i]["FuserId"] != null)
                {
                    Dictionary<string, string> info = new Dictionary<string, string>
                        {
                            { "FUSERID",items[i]["FUSERID"].ToString()},
                            { "FNAME",items[i]["FNAME"].ToString()},
                            { "FOPENID",items[i]["FOPENID"].ToString() },
                            { "FSDATE",sDate },
                            { "FEDATE",eDate }
                        };
                    sendInfos.Add(info);
                }
            }


            String now = DateTime.Now.ToString("yyyy-MM-dd");
            String appId = "DeliveryPlan";
            String title = "title";
            String PubAcctCode = "XT-4249be9f-5181-4a2a-a01f-7a9be93f1b3a";
            String PubAcctKey = "4839223c64dff23b04bf4e3376b7a082";

            sendInfos.ForEach(info =>
            {
                string text = "";
                //判断是否超期,选择不同模板
                text = string.Format(@"亲爱的中电家人，您有新的交货计划【{0}-{1}】待更新，请及时跟踪并反馈采购、生产相关情况。谢谢！", info["FSDATE"], info["FEDATE"]);
                Kingdee.BOS.Msg.XunTongMessage message = new Kingdee.BOS.Msg.XunTongMessage
                {
                    AppId = appId,
                    Title = title,
                    Date = now,
                    Text = text,
                    PubAcctCode = PubAcctCode,//公共号帐号，移动BOS
                    PubAcctKey = PubAcctKey,//公共号密钥
                    Users = new List<string> { info["FOPENID"] }
                };

                XTSendResult res = XunTongServiceHelper.SendMessage(this.Context, message);
            });
        }
    }
}
