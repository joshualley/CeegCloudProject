using Kingdee.BOS.App.Data;
using Kingdee.BOS.Core.Bill.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Util;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace CZ.CEEG.BosOA.ForPubFund
{
    [Description("Bos对公资金带出供应商信息")]
    [HotUpdate]
    public class CZ_CEEG_BosOA_ForPubFund : AbstractBillPlugIn
    {
        public override void DataChanged(DataChangedEventArgs e)
        {
            base.DataChanged(e);
            string key = e.Field.Key.ToString();
            switch (key)
            {
                case "FContractParty":
                    GetContractPartyInfo(e);
                    break;
            }
        }

        private void GetContractPartyInfo(DataChangedEventArgs e)
        {
            string FContractPartyType = this.View.Model.GetValue("FContractPartyType") == null ? "" : this.View.Model.GetValue("FContractPartyType").ToString();
            //this.View.ShowMessage(FContractPartyType);
            string FContractParty = e.NewValue == null ? "0" : e.NewValue.ToString();
            if (FContractPartyType == "BD_Supplier")
            {
                string sql = string.Format(@"SELECT TOP 1 sbl.FOpenBankName,FOpenAddressRec,FBankCode FROM t_BD_SupplierBank sb
                INNER JOIN t_BD_SupplierBank_L sbl ON sb.FBankId=sbl.FBankId
                WHERE FSUPPLIERID='{0}'", FContractParty);
                var objs = DBUtils.ExecuteDynamicObject(this.Context, sql);
                if (objs.Count > 0)
                {
                    this.View.Model.SetValue("F_ora_Bank", objs[0]["FOpenBankName"].ToString());
                    this.View.Model.SetValue("F_ora_BankInfo", objs[0]["FOpenAddressRec"].ToString());
                    this.View.Model.SetValue("F_ora_Text", objs[0]["FBankCode"].ToString());
                }

            }
            else if (FContractPartyType == "BD_Customer")
            {
                string sql = string.Format(@"SELECT TOP 1 ISNULL(FBANKCODE,'')FBANKCODE,ISNULL(FOPENBANKNAME,'')FOPENBANKNAME,
                ISNULL(FOpenAddressRec,'')FOpenAddressRec FROM T_BD_CUSTOMER c 
                LEFT JOIN T_BD_CUSTBANK cb ON c.FCUSTID=cb.FCUSTID 
                INNER JOIN T_BD_CUSTBANK_L cbl ON cb.FENTRYID=cbl.FENTRYID WHERE c.FCUSTID='{0}'", FContractParty);
                var objs = DBUtils.ExecuteDynamicObject(this.Context, sql);
                if (objs.Count > 0)
                {
                    this.View.Model.SetValue("F_ora_Bank", objs[0]["FOPENBANKNAME"].ToString());
                    this.View.Model.SetValue("F_ora_BankInfo", objs[0]["FOpenAddressRec"].ToString());
                    this.View.Model.SetValue("F_ora_Text", objs[0]["FBANKCODE"].ToString());
                }
            }
            
            
        }
    }
}
