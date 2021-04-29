--获取销售订单源单及金额信息
CREATE PROC [dbo].[proc_czly_GetSaleOrderSrcInfo]
    @FID INT
AS
BEGIN
    SELECT 
        FSaleOrderBillNo,FNicheBillNo, MAX(FContractBillNo) FContractBillNo,
        FCustOrgID,FCustID,FSaleOrgId,FPrjName,
        FBillAllAmount FOrderAmt, --订单金额
        FRecAmount FRecAmt, --到款金额
        SUM(FARAMOUNT) FSendPdtAmt, --发货金额
        SUM(FInvoiceAmount) FInvAmt--开票金额
    FROM (
        SELECT o.FID,
            o.FBILLNO FSaleOrderBillNo,o.FNicheBillNo,oer.FSRCBILLNO FContractBillNo,
            c.FUSEORGID FCustOrgID,o.FCustID,o.FSaleOrgId,o.F_CZ_PrjName FPrjName,
            orf.FBillAllAmount,   --价税合计
            ISNULL(FRecAmount, 0) FRecAmount,   --收款金额
            oef.FTaxPrice*oer.FBaseStockOutQty FARAMOUNT,  --含税单价*累计出库数量=发货金额
            oer.FInvoiceAmount    --累计开票金额
        FROM (SELECT * FROM T_SAL_ORDER WHERE FID=@FID) o
        INNER JOIN T_SAL_ORDERENTRY_R oer ON o.FID=oer.FID --执行表
        INNER JOIN T_SAL_ORDERENTRY_F oef ON oer.FENTRYID=oef.FENTRYID
        INNER JOIN T_SAL_ORDERFIN orf ON o.FID=orf.FID
        INNER JOIN T_BD_CUSTOMER c ON c.FCustID=o.FCUSTID
        LEFT JOIN (
            SELECT op.FID, SUM(rse.FSplitAmount) FRecAmount FROM T_SAL_ORDERPLAN  op
            INNER JOIN T_CZ_ReceiptSplitOrderPlan rse ON rse.FORDERPLANEID=op.FENTRYID
            INNER JOIN T_CZ_ReceiptSplit rs ON rs.FID=rse.FID AND rs.FDocumentStatus='C'
            GROUP BY op.FID
        ) op ON op.FID=o.FID
    ) t
    GROUP BY FID,FSaleOrderBillNo,FNicheBillNo,--FContractBillNo,
    FCustOrgID,FCustID,FPrjName,FSaleOrgId,FBillAllAmount,FRecAmount

END


-- SELECT * from T_Sal_Order where FBILLNO='XSDD20072792'

-- EXEC proc_czly_GetSaleOrderSrcInfo @FID='112033'
-- select * from T_SAL_ORDERENTRY_R
