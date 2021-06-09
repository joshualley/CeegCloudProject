using System;


namespace CZ.CEEG.SrvErp.CreatePurOrder.Models
{
        /// <summary>
    /// 财务信息
    /// </summary>
    public class SaleOrderFinance
    {
        /// <summary>
        /// 结算币别
        /// </summary>
        public BaseData FSettleCurrId { get; set; }
        /// <summary>
        /// 收款条件
        /// </summary>
        public BaseData FRecConditionId { get; set; }
        /// <summary>
        /// 价外税
        /// </summary>
        public bool FIsPriceExcludeTax { get; set; }
        /// <summary>
        /// 结算方式
        /// </summary>
        public BaseData FSettleModeId { get; set; }
        /// <summary>
        /// 是否含税
        /// </summary>
        public bool FIsIncludedTax { get; set; }
        /// <summary>
        /// 价目表
        /// </summary>
        public BaseData FPriceListId { get; set; }
        /// <summary>
        /// 折扣表
        /// </summary>
        public BaseData FDiscountListId { get; set; }
        /// <summary>
        /// 汇率类型
        /// </summary>
        public BaseData FExchangeTypeId { get; set; }
        /// <summary>
        /// 保证金比例（%）
        /// </summary>
        public decimal FMarginLevel { get; set; }
        /// <summary>
        /// 保证金
        /// </summary>
        public decimal FMargin { get; set; }
        /// <summary>
        /// 寄售生成跨组织调拨
        /// </summary>
        public bool FOverOrgTransDirect { get; set; }
        /// <summary>
        /// 收款二维码链接
        /// </summary>
        public string FRecBarcodeLink { get; set; }
    }

}