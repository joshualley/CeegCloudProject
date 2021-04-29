using System;


namespace CZ.CEEG.SrvErp.CreatePurOrder.Models
{
    public class BaseData 
    {
        public string FNumber { get; set; }
    }

    public class BaseDataName
    {
        public string FName { get; set; }
    }

    /// <summary>
    /// 财务信息
    /// </summary>
    public class SaleOrderFinance
    {
        public BaseData FSettleCurrId { get; set; }
        public BaseData FRecConditionId { get; set; }
        public bool FIsPriceExcludeTax { get; set; }
        public BaseData FSettleModeId { get; set; }
        public bool FIsIncludedTax { get; set; }
        public BaseData FPriceListId { get; set; }
        public BaseData FDiscountListId { get; set; }
        public BaseData FExchangeTypeId { get; set; }
        public decimal FMarginLevel { get; set; }
        public decimal FMargin { get; set; }
        public bool FOverOrgTransDirect { get; set; }
        public string FRecBarcodeLink { get; set; }
    }

    /// <summary>
    /// 订单条款
    /// </summary>
    public class SaleOrderClause
    {
        public BaseData FClauseId { get; set; }
        public string FClauseDesc { get; set; }
    }

    public class SaleOrderEntry
    {
        public string FReturnType { get; set; }
        public string FRowType { get; set; }
        public string F_CZ_CustItemName { get; set; }
        public BaseData FMapId { get; set; }
        public BaseData FMaterialId { get; set; }
        public BaseData FBMtlGroup { get; set; }
        public int FBSrcSEQ { get; set; }
        /// <summary>
        /// ?? 辅助属性ID
        /// </summary>
        /// <value></value>
        public BaseData FAuxPropId { get; set; }
        public BaseData FParentMatId { get; set; }
        public BaseData FUnitID { get; set; }
        public decimal FInventoryQty { get; set; }
        public decimal FCurrentInventory { get; set; }
        public decimal FAwaitQty { get; set; }
        public decimal FAvailableQty { get; set; }
        public decimal FQty { get; set; }
        public decimal FOldQty { get; set; }
        public BaseData FPurPriceUnitId { get; set; }
        public decimal FPrice { get; set; }
        public decimal FTaxPrice { get; set; }
        public bool FIsFree { get; set; }
        public BaseData FTaxCombination { get; set; }
        public decimal FEntryTaxRate { get; set; }
        public decimal F_CZ_BRangeAmtGP { get; set; }
        public decimal F_CZ_FBRangeAmtGP { get; set; }
        public string F_CZ_FBrangeAmtReason { get; set; }
        public string F_ora_CutpaySmtRmk { get; set; }
        public decimal F_CZ_FBPAmt { get; set; }
        public decimal F_ora_ProdGroupAmt { get; set; }
        public decimal FBDownPoints { get; set; }
        public bool F_CZ_IsSettle { get; set; }
        public DateTime FProduceDate { get; set; }
        public int FExpPeriod { get; set; }
        public string FExpUnit { get; set; }
        public DateTime FExpiryDate { get; set; }
        public BaseData FLot { get; set; }
        public decimal FPriceDiscount { get; set; }
        public decimal FInStockPrice { get; set; }
        public decimal FDiscountRate { get; set; }
        public DateTime FDeliveryDate { get; set; }
        public BaseData FStockOrgId { get; set; }
        public BaseData FSettleOrgIds { get; set; }
        public BaseData FSupplyOrgId { get; set; }
        public string FOwnerTypeId { get; set; }
        public BaseData FOwnerId { get; set; }
        public string F_ora_Jjyy { get; set; }
        public string FEntryNote { get; set; }
        public string FReserveType { get; set; }
        public int FPriority { get; set; }
        public string FMtoNo { get; set; }
        public string FPromotionMatchType { get; set; }
        public int FNetOrderEntryId { get; set; }
        public decimal FPriceBaseQty { get; set; }
        public BaseData FStockUnitID { get; set; }
        public decimal FStockQty { get; set; }
        public decimal FStockBaseQty { get; set; }
        public string FServiceContext { get; set; }
        public BaseData FOutLmtUnitID { get; set; }
        public string FOUTLMTUNIT { get; set; }
        public BaseData FSOStockId { get; set; }
        public bool FISMRP { get; set; }
    }

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

    /// <summary>
    /// 销售订单的数据模型
    /// </summary>
    public class SaleOrder
    {
        public BaseData FBillTypeID { get; set; }
        public string FBillNo { get; set; }
        public DateTime FDate { get; set; }
        public BaseData FSaleOrgId { get; set; }
        public BaseData FCustId { get; set; }
        public BaseData FHeadDeliveryWay { get; set; }
        public BaseData FHEADLOCID { get; set; }
        public BaseData FCorrespondOrgId { get; set; }
        public BaseData FSaleDeptId { get; set; }
        public BaseData FSaleGroupId { get; set; }
        public BaseData FSalerId { get; set; }
        public string FReceiveAddress { get; set; }
        public BaseDataName FReceiveContact { get; set; }
        public BaseData FChargeId { get; set; }
        public decimal FAmount2 { get; set; }
        public string FNetOrderBillNo { get; set; }
        public int FNetOrderBillId { get; set; }
        public int FOppID { get; set; }
        public BaseData FSalePhaseID { get; set; }
        public bool FISINIT { get; set; }
        public string FNote { get; set; }
        public bool FIsMobile { get; set; }
        public bool F_ora_SCYJ { get; set; }
        public bool F_CZ_IsBig { get; set; }
        public bool F_CZ_IsR { get; set; }
        public bool F_ora_BH { get; set; }
        public bool F_ora_ZBJ { get; set; }
        public string F_CZ_BillType { get; set; }
        public string FSOFrom { get; set; }
        public string F_CZ_PrjName { get; set; }
        public string FNicheBillNo { get; set; }
        public string F_CZ_Prepay { get; set; }
        public string F_ora_Adress { get; set; }
        public string F_ora_poorderno { get; set; }
        public string F_CZ_OldSaler { get; set; }
        public bool F_ora_breach { get; set; }
        public string F_ora_Sonum { get; set; }
        public BaseData F_ora_SignOrgId { get; set; }
        public SaleOrderFinance FSaleOrderFinance { get; set; }
        public SaleOrderClause[] FSaleOrderClause { get; set; }
        public SaleOrderEntry[] FSaleOrderEntry { get; set; }
        public SaleOrderPlan[] FSaleOrderPlan { get; set; }
    }
}