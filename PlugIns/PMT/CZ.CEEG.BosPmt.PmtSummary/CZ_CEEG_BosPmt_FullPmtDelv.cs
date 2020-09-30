using Kingdee.BOS.App.Data;
using Kingdee.BOS.Core;
using Kingdee.BOS.Core.Bill;
using Kingdee.BOS.Core.Bill.PlugIn;
using Kingdee.BOS.Core.DynamicForm;
using Kingdee.BOS.Core.DynamicForm.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Core.Metadata;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.Util;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CZ.CEEG.BosPmt.OuterPmt
{
    [HotUpdate]
    [Description("全款提货报表")]
    public class CZ_CEEG_BosPmt_FullPmtDelv : AbstractDynamicFormPlugIn
    {
        #region Overrides
        public override void AfterBindData(EventArgs e)
        {
            base.AfterBindData(e);
            DateTime currDt = DateTime.Now;
            string sDt = currDt.Year.ToString() + "-" + currDt.Month.ToString() + "-01";
            string eDt = currDt.ToString();
            string sql = "SELECT TOP 1 FDate FROM T_SAL_ORDER ORDER BY FDate ASC";
            var obj = DBUtils.ExecuteDynamicObject(this.Context, sql);
            if (obj.Count > 0)
            {
                sDt = obj[0]["FDate"].ToString();
            }
            this.View.Model.SetValue("FSDate", sDt);
            this.View.UpdateView("FSDate");
            this.View.Model.SetValue("FEDate", eDt);
            this.View.UpdateView("FEDate");
            Act_QueryDeliverData();
            Act_QueryContractData();
        }

        public override void AfterButtonClick(AfterButtonClickEventArgs e)
        {
            base.AfterButtonClick(e);
            string key = e.Key.ToUpperInvariant();
            switch (key)
            {
                case "FQUERYBTN":
                    Act_QueryDeliverData();
                    Act_QueryContractData();
                    break;
            }
        }

        #endregion

        #region Actions

        /// <summary>
        /// 查询发货明细数据
        /// </summary>
        private void Act_QueryDeliverData()
        {
            string formid = this.View.GetFormId() + "_Deliver";
            string FSDate = this.View.Model.GetValue("FSDate") == null ? "" : this.View.Model.GetValue("FSDate").ToString();
            string FEDate = this.View.Model.GetValue("FEDate") == null ? "" : this.View.Model.GetValue("FEDate").ToString();
            string FQDeptId = this.View.Model.GetValue("FQDeptId") == null ? "0" : (this.View.Model.GetValue("FQDeptId") as DynamicObject)["Id"].ToString();
            string FQSalerId = this.View.Model.GetValue("FQSalerId") == null ? "0" : (this.View.Model.GetValue("FQSalerId") as DynamicObject)["Id"].ToString();
            string FQCustId = this.View.Model.GetValue("FQCustId") == null ? "0" : (this.View.Model.GetValue("FQCustId") as DynamicObject)["Id"].ToString();
            string FQFactoryId = this.View.Model.GetValue("FQFactoryId") == null ? "0" : (this.View.Model.GetValue("FQFactoryId") as DynamicObject)["Id"].ToString();
            string FQOrderNo = this.View.Model.GetValue("FQOrderNo") == null ? "" : this.View.Model.GetValue("FQOrderNo").ToString().Trim();

            string sql = string.Format(@"exec proc_czly_GetPmt @FormId='{0}', @sDt='{1}', @eDt='{2}',
@FQDeptId={3}, @FQSalerId={4}, @FQCustId={5}, @FQFactoryId='{6}', @FQOrderNo='{7}'",
            formid, FSDate, FEDate, FQDeptId, FQSalerId, FQCustId, FQFactoryId, FQOrderNo);
            var objs = DBUtils.ExecuteDynamicObject(this.Context, sql);
            
            this.View.Model.DeleteEntryData("FDeliverEntity");
            if (objs.Count <= 0)
            {
                return;
            }
            this.View.Model.BatchCreateNewEntryRow("FDeliverEntity", objs.Count);
            Parallel.For(0, objs.Count, (i) =>
            {
                this.View.Model.SetValue("FOrderNo", objs[i]["FOrderNo"].ToString(), i);
                this.View.Model.SetValue("FSerialNum", objs[i]["FSerialNum"].ToString(), i);
                this.View.Model.SetValue("FDeliverNo", objs[i]["FDeliverNo"].ToString(), i);
                this.View.Model.SetValue("FDeptID", objs[i]["FDeptID"].ToString(), i);
                string[] FStrDirectors = objs[i]["FDirectors"].ToString().Split(',');
                List<long> FDirectors = new List<long>();
                foreach (var d in FStrDirectors)
                {
                    FDirectors.Add(int.Parse(d));
                }
                this.View.Model.SetValue("FDirectors", FDirectors, i);

                this.View.Model.SetValue("FCustID", objs[i]["FCustID"].ToString(), i);
                this.View.Model.SetValue("FSellerID", objs[i]["FSellerID"].ToString(), i);
                this.View.Model.SetValue("FOrderAmt", objs[i]["FOrderAmt"].ToString(), i);
                this.View.Model.SetValue("FPayWay", objs[i]["FPayWay"].ToString(), i);
                this.View.Model.SetValue("FCurrencyID", objs[i]["FCurrencyID"].ToString(), i);
                this.View.Model.SetValue("FDelvGoodsDt", objs[i]["FDelvGoodsDt"].ToString(), i);
                this.View.Model.SetValue("FOrderSeq", objs[i]["FOrderSeq"].ToString(), i);
                this.View.Model.SetValue("FMaterialID", objs[i]["FMaterialID"].ToString(), i);
                this.View.Model.SetValue("FFactoryID", objs[i]["FFactoryID"].ToString(), i);
                this.View.Model.SetValue("FProdCapacity", objs[i]["FProdCapacity"].ToString(), i);
                this.View.Model.SetValue("FDelvQty", objs[i]["FDelvQty"].ToString(), i);
                this.View.Model.SetValue("FDelvCapacity", objs[i]["FDelvCapacity"].ToString(), i);
            });
            this.View.UpdateView("FDeliverEntity");
        }

        /// <summary>
        /// 查询合同明细数据
        /// </summary>
        private void Act_QueryContractData()
        {
            string formid = this.View.GetFormId() + "_Contract";
            string FSDate = this.View.Model.GetValue("FSDate") == null ? "" : this.View.Model.GetValue("FSDate").ToString();
            string FEDate = this.View.Model.GetValue("FEDate") == null ? "" : this.View.Model.GetValue("FEDate").ToString();
            string FQDeptId = this.View.Model.GetValue("FQDeptId") == null ? "0" : (this.View.Model.GetValue("FQDeptId") as DynamicObject)["Id"].ToString();
            string FQSalerId = this.View.Model.GetValue("FQSalerId") == null ? "0" : (this.View.Model.GetValue("FQSalerId") as DynamicObject)["Id"].ToString();
            string FQCustId = this.View.Model.GetValue("FQCustId") == null ? "0" : (this.View.Model.GetValue("FQCustId") as DynamicObject)["Id"].ToString();
            string FQFactoryId = this.View.Model.GetValue("FQFactoryId") == null ? "0" : (this.View.Model.GetValue("FQFactoryId") as DynamicObject)["Id"].ToString();
            string FQOrderNo = this.View.Model.GetValue("FQOrderNo") == null ? "" : this.View.Model.GetValue("FQOrderNo").ToString().Trim();

            string sql = string.Format(@"exec proc_czly_GetPmt @FormId='{0}', @sDt='{1}', @eDt='{2}',
@FQDeptId={3}, @FQSalerId={4}, @FQCustId={5}, @FQFactoryId='{6}', @FQOrderNo='{7}'",
                formid, FSDate, FEDate, FQDeptId, FQSalerId, FQCustId, FQFactoryId, FQOrderNo);
            var objs = DBUtils.ExecuteDynamicObject(this.Context, sql);

            this.View.Model.DeleteEntryData("FContractEntity");
            for (int i = 0; i < objs.Count; i++)
            {
                this.View.Model.CreateNewEntryRow("FContractEntity");
                this.View.Model.SetValue("FCOrderNo", objs[i]["FOrderNo"].ToString(), i);
                this.View.Model.SetValue("FCBillDt", objs[i]["FBillDt"].ToString(), i);
                this.View.Model.SetValue("FCSerialNum", objs[i]["FSerialNum"].ToString(), i);
                this.View.Model.SetValue("FCDeptID", objs[i]["FDeptID"].ToString(), i);
                string[] FStrDirectors = objs[i]["FDirectors"].ToString().Split(',');
                List<long> FDirectors = new List<long>();
                foreach (var d in FStrDirectors)
                {
                    FDirectors.Add(int.Parse(d));
                }
                this.View.Model.SetValue("FCDirectors", FDirectors, i);

                this.View.Model.SetValue("FCCustID", objs[i]["FCustID"].ToString(), i);
                this.View.Model.SetValue("FCSellerID", objs[i]["FSellerID"].ToString(), i);
                this.View.Model.SetValue("FCOrderAmt", objs[i]["FOrderAmt"].ToString(), i);
                this.View.Model.SetValue("FCPayWay", objs[i]["FPayWay"].ToString(), i);
                this.View.Model.SetValue("FCCurrencyID", objs[i]["FCurrencyID"].ToString(), i);
            }
            this.View.UpdateView("FContractEntity");
        }

        #endregion
    }
}
