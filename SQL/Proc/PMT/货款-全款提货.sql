--全款提货
/*
Title: 全款提货报表
Author: 刘跃
Modified: 2020-07-29, 
          2020-08-28
Info: 
    1.汇总全款提货的销售订单
    2.全款提货包含两类：1)销售订单上收款; 2)发货前，收款金额>=发货金额
*/
ALTER PROC [dbo].[proc_czly_PmtFullDelv](
    @Type VARCHAR(55),
    @sDt DATETIME,
    @eDt DATETIME,
    @FQDeptId BIGINT=0,
    @FQSalerId BIGINT=0,
    @FQCustId BIGINT=0,
    @FQOrderNo VARCHAR(55)=''
)
AS
BEGIN

SET NOCOUNT ON

CREATE TABLE #pmt(
    FID BIGINT, 
    FBeforeRcvAmt DECIMAL(18, 2), 
    FOrderNo VARCHAR(100), 
    FSaleOrgID BIGINT, 
    FSellerID BIGINT, 
    FDeptID BIGINT, 
    FOrgID BIGINT, 
    FCustID BIGINT,
    FDirectors VARCHAR(255),
    FRemark VARCHAR(1000), 
    FSerialNum VARCHAR(255),
    FIntervalDay INT, 
    FIntervalMonth INT,
    FTOrderAmt DECIMAL(18, 2), 
    FPayWay BIGINT, 
    FLaterDelvGoodsDt DATETIME, 
    FTDeliverAmt DECIMAL(18, 2), 
    FTReceiverAmt DECIMAL(18, 2), 
    FTInvoiceAmt DECIMAL(18, 2),
    FOuterPmt DECIMAL(18, 2), 
    FNormOverduePmt DECIMAL(18, 2), 
    FNormUnoverduePmt DECIMAL(18, 2),
    FOverduePmt DECIMAL(18, 2),
    FTOverduePmt DECIMAL(18, 2),
    FTUnoverduePmt DECIMAL(18, 2),
    FTExceedePmt DECIMAL(18, 2),
    FTWarranty DECIMAL(18, 2),
    FOverdueWarranty DECIMAL(18, 2),
    FUnoverdueWarranty DECIMAL(18, 2)
)

INSERT INTO #pmt EXEC proc_czly_GetPmtSummary @SDt=@sDt, @EDt=@eDt,
@FQDeptId=@FQDeptId, @FQSalerId=@FQSalerId, @FQCustId=@FQCustId, @FQOrderNo=@FQOrderNo

IF @Type='Deliver' --发货
BEGIN
    SELECT 
        o.FOrderNo, o.FSerialNum,
        o.FDirectors, 
        o.FSellerID, o.FDeptID, o.FOrgID,
        o.FCustID, o.FSaleOrgID,
        o.FPayWay,
        ofi.FLocalCurrId FCurrencyID,
        o.FTOrderAmt FOrderAmt,
        os.FBILLNO FDeliverNo, os.FDATE FDelvGoodsDt,
        oe.FSEQ FOrderSeq, oe.FMaterialID, oe.FSTOCKORGID FFactoryID,
        --oe.FInventoryQty FProdCapacity, --当前库存
        --oe.FAwaitQty FDelvQty, --待发数量
        --oe.FAvailableQty FDelvCapacity --可发库存
        0 FProdCapacity,
        0 FDelvQty,
        0 FDelvCapacity
    FROM #pmt o
    INNER JOIN T_SAL_ORDERFIN ofi ON o.FID=ofi.FID
    INNER JOIN T_SAL_ORDERENTRY oe ON o.FID=oe.FID AND oe.F_ORA_JJYY=''
    --INNER JOIN T_SAL_ORDERENTRY_F oef ON oe.FENTRYID=oef.FENTRYID --财务信息拆分表
    --INNER JOIN T_SAL_ORDERENTRY_R oer ON oe.FENTRYID=oer.FENTRYID
    INNER JOIN T_SAL_OUTSTOCKENTRY_R osr ON oe.FENTRYID=osr.FSOENTRYID
    INNER JOIN T_SAL_OUTSTOCK os ON os.FID=osr.FID AND os.FDocumentStatus='C'
    --INNER JOIN T_SAL_OUTSTOCKENTRY_F osef ON os.FID=osef.FID
    WHERE ofi.FRecConditionId=306036 OR o.FBeforeRcvAmt>=o.FTDeliverAmt

END
ELSE IF @Type='Contract' --合同
BEGIN
    SELECT 
        o.FOrderNo, o.FSerialNum,
        o.FDirectors, 
        o.FSellerID, o.FDeptID,
        o.FCustID,
        o.FPayWay,
        ofi.FLocalCurrId FCurrencyID,
        o.FTOrderAmt FOrderAmt,
        od.FDate FBillDt
    FROM #pmt o
    INNER JOIN T_SAL_ORDER od ON o.FID=od.FID
    INNER JOIN T_SAL_ORDERFIN ofi ON o.FID=ofi.FID
    WHERE ofi.FRecConditionId=306036 --全款提货的
    OR o.FBeforeRcvAmt>=o.FTDeliverAmt
    
END

END

-- exec proc_czly_PmtFullDelv @Type='Contract', @sDt='2020-01-01', @eDt='2020-08-28'