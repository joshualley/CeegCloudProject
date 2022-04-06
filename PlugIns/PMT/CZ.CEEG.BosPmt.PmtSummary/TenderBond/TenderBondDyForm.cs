using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Kingdee.BOS.App.Data;
using Kingdee.BOS.Core.Bill;
using Kingdee.BOS.Core.DynamicForm;
using Kingdee.BOS.Core.DynamicForm.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Msg;
using Kingdee.BOS.ServiceHelper.Messages;
using Kingdee.BOS.Util;

namespace CZ.CEEG.BosPmt.PmtSummary.TenderBond
{
    [Description("投标保证金报表")]
    [HotUpdate]
    public class TenderBondDyForm: AbstractDynamicFormPlugIn
    {
        public override void AfterBindData(EventArgs e)
        {
            base.AfterBindData(e);
            Act_Query();
        }

        public override void AfterButtonClick(AfterButtonClickEventArgs e)
        {
            base.AfterButtonClick(e);
            switch (e.Key.ToUpperInvariant())
            {
                case "FQUERY": // FQuery 查询
                    Act_Query();
                    break;
            }
        }

        public override void AfterEntryBarItemClick(AfterBarItemClickEventArgs e)
        {
            base.AfterEntryBarItemClick(e);
            switch (e.BarItemKey.ToUpperInvariant())
            {
                case "ORA_TBBIDDING": // ora_tbBidding 中标登记
                    Act_Bidding();
                    break;
                case "ORA_TBTRANS": // ora_tbTrans 转移保证金
                    Act_Trans();
                    break;
                case "ORA_SEND_MSG2":
                    SendMsg();
                    break;
            }
        }

        private void Act_Query()
        {
            this.Model.DeleteEntryData("FEntity");
            string FBeginDt = this.Model.GetValue("FBeginDt")?.ToString() ?? "";
            string FEndDt = this.Model.GetValue("FEndDt")?.ToString() ?? "";

            string sql = string.Format(@"/*dialect*/
            select
                pf.FCreateDate FDate, pf.FBillNo FPubFundNo, pf.FID FPubFundID, 
                pf.FPrjName, pf.FCostType FExpType, pf.FRealMoney FMargin, 
                isnull(bd.FInvoiceAmt, 0) FInvoiceAmt, isnull(bd.FBidSrvFee, 0) FBidSrvFee,
                isnull(bd.FReturnAmt, 0) FReturnAmt,
                case when isnull(tp.FCAPTION, '')='' and isnull(bd.FID, 0)<>0 then '中标'
                     else isnull(tp.FCAPTION, '') end FTransNote,u.fname,x.FOPENID,pf.FBACKDATE,pf.FTENDERTIME,pf.FCREATEDATE,cus2.FNAME as FCONTRACTPARTY 
            from ora_t_Cust100011 pf
            left join ora_PMT_Bidding bd on bd.FPubFundID=pf.FID and bd.FDocumentStatus in ('B', 'C')
            left join ora_PMT_MarginTrans mt on mt.FPubFundID=pf.FID and mt.FDocumentStatus in ('B', 'C')
                and mt.FBillStatus='A'
            left join (
                SELECT feil.FCAPTION, fei.FVALUE
                FROM T_META_FORMENUM_L fel
                INNER JOIN T_META_FORMENUMITEM fei ON fel.FID=fei.FID
                INNER JOIN T_META_FORMENUMITEM_L feil ON feil.FENUMID=fei.FENUMID
                WHERE FNAME='PMT转移类型'
            ) tp on tp.FVALUE=mt.FTransType 
             join T_HR_EMPINFO emp on emp.fid = pf.fapplyid
			 join V_bd_ContactObject obj on emp.FNUMBER = obj.FNUMBER
			 join T_SEC_USER u on u.FLINKOBJECT=obj.FID 
			 join T_SEC_XTUSERMAP x on u.FUSERID = x.FUSERID 
             join T_BD_CUSTOMER cus on cus.FCUSTID = pf.FCONTRACTPARTY 
			 join T_BD_CUSTOMER_L cus2 on cus2.FCUSTID = cus.FCUSTID
            where pf.FCostType in ('3', '10') and pf.FDocumentStatus='C'
            and pf.FCreateDate between '{0}' and '{1}'
            order by pf.FCreateDate desc", FBeginDt, FEndDt);
            var items = DBUtils.ExecuteDynamicObject(Context, sql);
            if (items.Count > 0)
            {
                this.Model.BatchCreateNewEntryRow("FEntity", items.Count);
                for (int i = 0; i < items.Count; i++)
                {
                    var item = items[i];
                    this.Model.SetValue("FDate", item["FDate"], i);
                    this.Model.SetValue("FPubFundNo", item["FPubFundNo"], i);
                    this.Model.SetValue("FPubFundID", item["FPubFundID"], i);
                    this.Model.SetValue("FPrjName", item["FPrjName"], i);
                    this.Model.SetValue("FExpType", item["FExpType"], i);
                    this.Model.SetValue("FMargin", item["FMargin"], i);
                    this.Model.SetValue("FInvoiceAmt", item["FInvoiceAmt"], i);
                    this.Model.SetValue("FBidSrvFee", item["FBidSrvFee"], i);
                    this.Model.SetValue("FReturnAmt", item["FReturnAmt"], i);
                    this.Model.SetValue("FTransNote", item["FTransNote"], i);
                    this.Model.SetValue("FSeller", item["fname"], i);
                    this.Model.SetValue("FOpenid", item["FOPENID"], i);
                    this.Model.SetValue("FBackDate", item["FBACKDATE"], i);
                    this.Model.SetValue("FTENDERTIME", item["FTENDERTIME"], i);
                    this.Model.SetValue("FCREATEDATE", item["FCREATEDATE"], i);
                    this.Model.SetValue("FCONTRACTPARTY", item["FCONTRACTPARTY"], i);
                }
            }
            this.View.UpdateView("FEntity");
        }

        private void SendMsg()
        {
            int rowNum = this.View.Model.GetEntryRowCount("FEntity");

            List<Dictionary<string, string>> sendInfos = new List<Dictionary<string, string>>();

            for (int i = 0; i < rowNum; i++)
            {
                bool check = (bool)this.Model.GetValue("FSendCheck", i);
                if (check)
                {
                    Dictionary<string, string> info = new Dictionary<string, string>
                        {
                            { "FOPENID", this.Model.GetValue("FOpenid", i)==null?"":this.Model.GetValue("FOpenid", i).ToString() },
                            { "FBackDate", this.Model.GetValue("FBackDate", i)==null?"":this.Model.GetValue("FBackDate", i).ToString() },
                            { "FTENDERTIME", this.Model.GetValue("FTENDERTIME", i)==null?"":this.Model.GetValue("FTENDERTIME", i).ToString() },
                            { "FCREATEDATE", this.Model.GetValue("FCREATEDATE", i)==null?"":this.Model.GetValue("FCREATEDATE", i).ToString() },
                            { "FCONTRACTPARTY", this.Model.GetValue("FCONTRACTPARTY", i)==null?"":this.Model.GetValue("FCONTRACTPARTY", i).ToString() },
                            { "FSeller", this.Model.GetValue("FSeller", i)==null?"":this.Model.GetValue("FSeller", i).ToString() },
                            { "FPrjName", this.Model.GetValue("FPrjName", i)==null?"":this.Model.GetValue("FPrjName", i).ToString() },
                            { "FMargin", this.Model.GetValue("FMargin", i)==null?"":this.Model.GetValue("FMargin", i).ToString() },
                            { "FPubFundNo", this.Model.GetValue("FPubFundNo", i)==null?"":this.Model.GetValue("FPubFundNo", i).ToString() }
                        };
                    sendInfos.Add(info);
                }
            }

            if (sendInfos.Count==0)
            {
                this.View.ShowErrMessage("请选择!");
            }
            else
            {
                String now = DateTime.Now.ToString("yyyy-MM-dd");
                String appId = "PmtSummary";
                String title = "title";
                String PubAcctCode = "XT-4249be9f-5181-4a2a-a01f-7a9be93f1b3a";
                String PubAcctKey = "4839223c64dff23b04bf4e3376b7a082";

                List<Dictionary<string, string>> sendLogs = new List<Dictionary<string, string>>();

                sendInfos.ForEach(info =>
                {
                    string text = string.Format(@"亲爱的中电家人[{0}],您在[{1}]所申请的[{2}]的[{3}]开标时间为[{4}]的投标保证金,金额为[{5}],该笔投标保证金预计退还日期为[{6}],请及时跟踪并反馈具体情况.谢谢!",
                                            info["FSeller"], info["FCREATEDATE"], info["FCONTRACTPARTY"], info["FPrjName"],
                                            info["FTENDERTIME"], info["FMargin"], info["FBackDate"]);

                    Kingdee.BOS.Msg.XunTongMessage message = new Kingdee.BOS.Msg.XunTongMessage
                    {
                        AppId = appId,
                        Title = title,
                        Date = DateTime.Now.ToString("yyyy-MM-dd"),
                        Text = text,
                        PubAcctCode = PubAcctCode,//公共号帐号，移动BOS
                        PubAcctKey = PubAcctKey,//公共号密钥
                        Users = new List<string> { info["FOPENID"] }
                    };

                    XTSendResult res = XunTongServiceHelper.SendMessage(this.Context, message);

                    Dictionary<string, string> log = new Dictionary<string, string>
                    {
                        { "order", info["FPubFundNo"] }
                    };

                    if (res.IsSuccess)
                    {
                        log.Add("res", "success");
                    }
                    else
                    {
                        log.Add("res", "error");
                        log.Add("message", res.Msg);
                    }

                    sendLogs.Add(log);

                });

                string errorMessages = "";

                sendLogs.Where(log => log["res"].Equals("error")).ToList().ForEach(log =>
                {
                    errorMessages +=  log["order"] + "发送失败:"+ log["message"]+"\n\r";
                });

                if (!errorMessages.Equals(""))
                {
                    this.View.ShowErrMessage(errorMessages.Substring(1));
                }
                else
                {
                    this.View.ShowMessage("消息已发送");
                }
            }
        }

        private void Act_Trans()
        {
            int row = this.Model.GetEntryCurrentRowIndex("FEntity");
            string FTransNote = this.Model.GetValue("FTransNote", row)?.ToString() ?? "";
            string FPrjName = this.Model.GetValue("FPrjName", row)?.ToString() ?? "";
            if (!FTransNote.Equals(""))
            {
                this.View.ShowWarnningMessage($"项目《{FPrjName}》，已转为：{FTransNote}，请勿重复操作！");
                return;
            }

            string FPubFundID = this.Model.GetValue("FPubFundID", row)?.ToString() ?? "0";
            string FPubFundNo = this.Model.GetValue("FPubFundNo", row)?.ToString() ?? "";
            string FMargin = this.Model.GetValue("FMargin", row)?.ToString() ?? "0";
            

            BillShowParameter param = new BillShowParameter();
            param.FormId = "ora_PMT_MarginTrans";
            param.OpenStyle.ShowType = ShowType.Modal;
            param.ParentPageId = this.View.PageId;
            param.CustomParams.Add("FPubFundID", FPubFundID);
            param.CustomParams.Add("FPubFundNo", FPubFundNo);
            param.CustomParams.Add("FPrjName", FPrjName);
            param.CustomParams.Add("FMargin", FMargin);
            this.View.ShowForm(param, (r) => {
                Act_Query();
            });
        }

        private void Act_Bidding()
        {
            int row = this.Model.GetEntryCurrentRowIndex("FEntity");
            string FTransNote = this.Model.GetValue("FTransNote", row)?.ToString() ?? "";
            string FPrjName = this.Model.GetValue("FPrjName", row)?.ToString() ?? "";
            if (!FTransNote.Equals(""))
            {
                this.View.ShowWarnningMessage($"项目《{FPrjName}》，已转为：{FTransNote}，请勿重复操作！");
                return;
            }

            string FPubFundID = this.Model.GetValue("FPubFundID", row)?.ToString() ?? "0";
            string FPubFundNo = this.Model.GetValue("FPubFundNo", row)?.ToString() ?? "";
            string FMargin = this.Model.GetValue("FMargin", row)?.ToString() ?? "0";

            BillShowParameter param = new BillShowParameter();
            param.FormId = "ora_PMT_Bidding";
            param.OpenStyle.ShowType = ShowType.Modal;
            param.ParentPageId = this.View.PageId;
            param.CustomParams.Add("FPubFundID", FPubFundID);
            param.CustomParams.Add("FPubFundNo", FPubFundNo);
            param.CustomParams.Add("FPrjName", FPrjName);
            param.CustomParams.Add("FMargin", FMargin);
            this.View.ShowForm(param, (r) => {
                Act_Query();
            });
        }
    }
}

