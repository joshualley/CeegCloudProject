using System;


namespace CZ.CEEG.SrvErp.CreatePurOrder.Models
{
    public class SaleOrderEntry
    {
        /// <summary>
        /// 退补类型
        /// </summary>
        public string FReturnType { get; set; }
        /// <summary>
        /// 产品类型
        /// </summary>
        public string FRowType { get; set; }
        /// <summary>
        /// 客户物料名称
        /// </summary>
        public string F_CZ_CustItemName { get; set; }
        /// <summary>
        /// 客户物料编码
        /// </summary>
        public BaseData FMapId { get; set; }
        /// <summary>
        /// 物料编码
        /// </summary>
        public BaseData FMaterialId { get; set; }
        /// <summary>
        /// 产品大类
        /// </summary>
        public BaseData FBMtlGroup { get; set; }
        /// <summary>
        /// 项次
        /// </summary>
        public int FBSrcSEQ { get; set; }
        /// <summary>
        /// ?? 辅助属性ID
        /// </summary>
        public BaseData FAuxPropId { get; set; }
        /// <summary>
        /// 父项产品
        /// </summary>
        public BaseData FParentMatId { get; set; }
        /// <summary>
        /// 销售单位
        /// </summary>
        public BaseData FUnitID { get; set; }
        /// <summary>
        /// 当前库存
        /// </summary>
        public decimal FInventoryQty { get; set; }
        /// <summary>
        /// 可用库存
        /// </summary>
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

}
