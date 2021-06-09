using System;


namespace CZ.CEEG.SrvErp.CreatePurOrder.Models
{
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
        /// <summary>
        /// 交货方式
        /// </summary>
        public BaseData FHeadDeliveryWay { get; set; }
        /// <summary>
        /// 交货地点
        /// </summary>
        public BaseData FHEADLOCID { get; set; }
        /// <summary>
        /// 对应组织
        /// </summary>
        public BaseData FCorrespondOrgId { get; set; }
        /// <summary>
        /// 办事处
        /// </summary>
        public BaseData FSaleDeptId { get; set; }
        /// <summary>
        /// 销售组
        /// </summary>
        public BaseData FSaleGroupId { get; set; }
        /// <summary>
        /// 销售员
        /// </summary>
        public BaseData FSalerId { get; set; }
        /// <summary>
        /// 收货方地址
        /// </summary>
        public string FReceiveAddress { get; set; }
        /// <summary>
        /// 收货方联系人
        /// </summary>
        public BaseDataName FReceiveContact { get; set; }
        /// <summary>
        /// 付款方
        /// </summary>
        public BaseData FChargeId { get; set; }
        /// <summary>
        /// 总基价
        /// </summary>
        public decimal FAmount2 { get; set; }
        /// <summary>
        /// 是否期初单据
        /// </summary>
        public bool FISINIT { get; set; }
        /// <summary>
        /// 备注
        /// </summary>
        public string FNote { get; set; }
        /// <summary>
        /// 是否上传原件
        /// </summary>
        public bool F_ora_SCYJ { get; set; }
        /// <summary>
        /// 大项目
        /// </summary>
        public bool F_CZ_IsBig { get; set; }
        /// <summary>
        /// 订单拒绝
        /// </summary>
        public bool F_CZ_IsR { get; set; }
        /// <summary>
        /// 保函
        /// </summary>
        public bool F_ora_BH { get; set; }
        /// <summary>
        /// 质保金
        /// </summary>
        public bool F_ora_ZBJ { get; set; }
        /// <summary>
        /// 订单类型
        /// </summary>
        public string F_CZ_BillType { get; set; }
        /// <summary>
        /// 订单来源
        /// </summary>
        public string FSOFrom { get; set; }
        /// <summary>
        /// 项目名称
        /// </summary>
        public string F_CZ_PrjName { get; set; }
        /// <summary>
        /// 商机编号
        /// </summary>
        public string FNicheBillNo { get; set; }
        /// <summary>
        /// 期初收款计划
        /// </summary>
        public string F_CZ_Prepay { get; set; }
        /// <summary>
        /// 项目地址
        /// </summary>
        public string F_ora_Adress { get; set; }
        /// <summary>
        /// 采购订单编号
        /// </summary>
        public string F_ora_poorderno { get; set; }
        /// <summary>
        /// 老系统销售员
        /// </summary>
        public string F_CZ_OldSaler { get; set; }
        /// <summary>
        /// 是否有违约风险
        /// </summary>
        public bool F_ora_breach { get; set; }
        /// <summary>
        /// 原销售订单号
        /// </summary>
        public string F_ora_Sonum { get; set; }
        /// <summary>
        /// 签约公司
        /// </summary>
        public BaseData F_ora_SignOrgId { get; set; }
        public SaleOrderFinance FSaleOrderFinance { get; set; }
        public SaleOrderEntry[] FSaleOrderEntry { get; set; }
        
        // public SaleOrderClause[] FSaleOrderClause { get; set; }
        // public SaleOrderPlan[] FSaleOrderPlan { get; set; }
    }
}