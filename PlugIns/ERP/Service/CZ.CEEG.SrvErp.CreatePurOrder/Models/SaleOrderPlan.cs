using System;


namespace CZ.CEEG.SrvErp.CreatePurOrder.Models
{
    public class SaleOrderPlan
    {
        public bool FNeedRecAdvance { get; set; }
        public BaseData FReceiveType { get; set; }
        public decimal FRecAdvanceRate { get; set; }
        public decimal FRecAdvanceAmount { get; set; }
        public DateTime FMustDate { get; set; }
        public string FRelBillNo { get; set; }
        public decimal FRecAmount { get; set; }
        public string FControlSend { get; set; }
        public bool FIsOutStockByRecamount { get; set; }
        public string FReMark { get; set; }
        public BaseData FPlanMaterialId { get; set; }
        public int FMaterialSeq { get; set; }
        public string FMaterialRowID { get; set; }
        public int FOrderEntryId { get; set; }
        public bool F_CZ_SpecChk { get; set; }
        public decimal F_ora_SplitAmount { get; set; }
        public decimal F_ora_SplitAmountFor { get; set; }
    }

}
