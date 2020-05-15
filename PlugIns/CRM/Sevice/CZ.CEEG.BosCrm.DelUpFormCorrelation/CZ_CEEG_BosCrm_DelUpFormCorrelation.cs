using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using Kingdee.BOS.Core.DynamicForm.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.App.Data;
using Kingdee.BOS.Util;



namespace CZ.CEEG.BosCrm.DelUpFormCorrelation
{
    [Description("删除与上游单据的关联")]
    [HotUpdate]
    public class CZ_CEEG_BosCrm_DelUpFormCorrelation : AbstractOperationServicePlugIn
    {

        public override void EndOperationTransaction(EndOperationTransactionArgs e)
        {
            base.EndOperationTransaction(e);
            var dict = GetTbName();
            if (dict.Count <= 0)
                return;
            string sql = "";
            foreach(var d in e.DataEntitys)
            {
                string FID = d["Id"].ToString();
                sql = string.Format("delete from T_BF_INSTANCEENTRY where FTTABLENAME='{0}' and FTID='{1}'", dict["tgt"], FID);
                CZDB_GetData(sql);
            }
        }

        private Dictionary<string, string> GetTbName()
        {
            string srcFormId = CZ_GetFormType();
            var dict = new Dictionary<string, string>();
            switch (srcFormId)
            {
                case "ora_CRM_Niche":
                    dict.Add("src", "ora_CRM_Clue");
                    dict.Add("tgt", "ora_CRM_Niche");
                    break;
                case "ora_CRM_SaleOffer":
                    dict.Add("src", "ora_CRM_Niche");
                    dict.Add("tgt", "ora_CRM_SaleOffer");
                    break;
                case "ora_CRM_Contract":
                    dict.Add("src", "ora_CRM_SaleOffer");
                    dict.Add("tgt", "ora_CRM_Contract");
                    break;
                case "ora_Cust_SaleOrder":
                    dict.Add("src", "ora_CRM_Contract");
                    dict.Add("tgt", "T_SAL_ORDER");
                    break;
                case "ora_CRM_CCRP":
                    dict.Add("src", "T_SAL_ORDER");
                    dict.Add("tgt", "ora_CRM_CCRP");
                    break;
                case "ora_CRM_SaleInvoice":
                    dict.Add("src", "T_SAL_ORDER");
                    dict.Add("tgt", "ora_CRM_SaleInvoice");
                    break;
                case "ora_CRM_SpcBussCost":
                    dict.Add("src", "T_SAL_ORDER");
                    dict.Add("tgt", "ora_CRM_SpcBussCost");
                    break;
                case "ora_CRM_AfterSaleSrv":
                    dict.Add("src", "ora_CRM_CCRP");
                    dict.Add("tgt", "ora_CRM_AfterSaleSrv");
                    break;
            }
            return dict;
        }

        /// <summary>
        /// 获取单据标识 FormType | FormID
        /// </summary>
        /// <returns></returns>
        private string CZ_GetFormType()
        {
            //string _BI_DTONS = this.BusinessInfo.DTONS;     //"Kingdee.BOS.ServiceInterface.Temp.ora_test_Table002"
            string[] _BI_DTONS_C = this.BusinessInfo.DTONS.Split('.');
            return _BI_DTONS_C[_BI_DTONS_C.Length - 1].ToString();
        }

        /// <summary>
        /// 基本方法 
        /// </summary>
        /// <param name="_sql"></param>
        /// <returns></returns>
        public DynamicObjectCollection CZDB_GetData(string _sql)
        {
            try
            {
                var obj = DBUtils.ExecuteDynamicObject(this.Context, _sql);
                return obj;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}
