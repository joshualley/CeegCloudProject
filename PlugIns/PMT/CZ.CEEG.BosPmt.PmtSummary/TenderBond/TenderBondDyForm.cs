using System;
using System.ComponentModel;
using Kingdee.BOS.App.Data;
using Kingdee.BOS.Core.Bill;
using Kingdee.BOS.Core.DynamicForm;
using Kingdee.BOS.Core.DynamicForm.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
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
                     else isnull(tp.FCAPTION, '') end FTransNote
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
                }
            }
            this.View.UpdateView("FEntity");
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

