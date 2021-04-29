-- 各类别货款
CREATE PROC [dbo].[proc_czly_GetPmt](
    @FormId VARCHAR(55),
    @FSellerID BIGINT=0,
    @sDt DATETIME='',
    @eDt DATETIME='',
    @FQDeptId BIGINT=0,
    @FQSalerId BIGINT=0,
    @FQCustId BIGINT=0,
    @FQFactoryId BIGINT=0,
    @FQOrderNo VARCHAR(55)=''
)
AS
BEGIN

SET NOCOUNT ON

CREATE TABLE #pmt(
    FID BIGINT,
    FEntryID BIGINT,
    FBeforeRcvAmt DECIMAL(18, 2),
    FOrderNo VARCHAR(100),
    FOrderSeq INT,
    FFactoryID BIGINT,
    FDirectors VARCHAR(255),
    FSerialNum VARCHAR(255),
    FSaleOrgID BIGINT,
    FSignOrgID BIGINT,
    FSellerID BIGINT, 
    FDeptID BIGINT, 
    FOrgID BIGINT, 
    FCustID BIGINT,
    FEarlyDelvGoodsDt DATETIME,
    FLaterDelvGoodsDt DATETIME,
    FIntervalDay INT,
    FIntervalMonth INT,
    FPayWay BIGINT, 
    FRemark VARCHAR(1000),
    FOrderAmt DECIMAL(18, 2), 
    FTOrderAmt DECIMAL(18, 2), 
    FDeliverAmt DECIMAL(18, 2), 
    FReceiverAmt DECIMAL(18, 2), 
    FInvoiceAmt DECIMAL(18, 2),
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

INSERT INTO #pmt EXEC proc_czly_GetPmtDetail2 @SDt=@sDt, @EDt=@eDt, 
@FQDeptId=@FQDeptId, @FQSalerId=@FQSalerId, @FQCustId=@FQCustId, 
@FQFactoryId=@FQFactoryId, @FQOrderNo=@FQOrderNo

-- 货款总额
DECLARE @FTOuterPmt DECIMAL(18, 2) = (SELECT SUM(FOuterPmt) FROM #pmt)
SET @FTOuterPmt=NULLIF(@FTOuterPmt, 0)


IF @FormId = 'ora_PMT_Summary'
BEGIN
    SELECT 
        FOrderNo,FSignOrgID,FSerialNum,FDirectors,FSaleOrgID,FSellerID,FDeptID,FOrgID,FCustID,FPayWay,FLaterDelvGoodsDt,
        FTOrderAmt,FDeliverAmt FTDeliverAmt,FReceiverAmt FTReceiverAmt,FInvoiceAmt FTInvoiceAmt,
        FOuterPmt,FNormOverduePmt,FNormUnoverduePmt,FOverduePmt,FTOverduePmt,FTUnoverduePmt,
        FTExceedePmt,FOverdueWarranty,FUnoverdueWarranty,FTWarranty,
        FIntervalMonth,FIntervalDay,FRemark
    FROM #pmt p
END
IF @FormId = 'ora_PMT_OfficePmt'
BEGIN
    SELECT 
        d.FMASTERID FDeptID, SUM(FOuterPmt) FOuterPmt, 
        ISNULL(SUM(FOuterPmt)/@FTOuterPmt*100, 0) FRatioForTAmt,
        SUM(FTOverduePmt) FTOverduePmt, 
        ISNULL(SUM(FTOverduePmt)/NULLIF(SUM(FOuterPmt), 0)*100, 0) FRatioForTPmt,
        ISNULL(SUM(FTOverduePmt)/NULLIF(SUM(FTExceedePmt), 0)*100, 0) FRatioForTExceedePmt,
        SUM(FTUnoverduePmt) FTUnoverduePmt, SUM(FTWarranty) FWarranty
    FROM #pmt p
    LEFT JOIN T_BD_DEPARTMENT d ON p.FDeptID=d.FDEPTID
    WHERE p.FDeptID <> 0
    GROUP BY d.FMASTERID
END
ELSE IF @FormId='ora_PMT_CustomerPmt'
BEGIN
    SELECT 
        d.FMASTERID FDeptID, c.FMASTERID FCustID,
        SUM(FOuterPmt) FOuterPmt, 
        ISNULL(SUM(FOuterPmt)/@FTOuterPmt*100, 0) FRatioForTAmt,
        SUM(FTOverduePmt) FTOverduePmt, 
        ISNULL(SUM(FTOverduePmt)/NULLIF(SUM(FOuterPmt), 0)*100, 0) FRatioForTPmt,
        ISNULL(SUM(FTOverduePmt)/NULLIF(SUM(FTExceedePmt), 0)*100, 0) FRatioForTExceedePmt
    FROM #pmt p
    LEFT JOIN T_BD_DEPARTMENT d ON p.FDeptID=d.FDEPTID
    LEFT JOIN T_BD_CUSTOMER c ON p.FCustID=c.FCUSTID
    WHERE d.FMASTERID<>0 AND c.FMASTERID <> 0
    GROUP BY d.FMASTERID, c.FMASTERID
END
ELSE IF @FormId='ora_PMT_SalesmanPmt'
BEGIN
    SELECT 
        p.FSellerID,
        SUM(FOuterPmt) FOuterPmt, 
        SUM(FNormOverduePmt) FNormOverduePmt,
        SUM(FNormUnoverduePmt) FNormUnoverduePmt,
        SUM(FTExceedePmt) FTExceedePmt,
        SUM(FTOverduePmt) FTOverduePmt,
        SUM(FTUnoverduePmt) FTUnoverduePmt, 
        SUM(FOverdueWarranty) FOverdueWarranty,
        SUM(FUnoverdueWarranty) FUnoverdueWarranty,
        SUM(FTWarranty) FWarranty
    FROM #pmt p
    LEFT JOIN V_BD_SALESMAN s1 ON p.FSellerID=s1.FID
    WHERE p.FSellerID <> 0
    GROUP BY s1.FNUMBER, p.FSellerID
END
ELSE IF @FormId='ora_PMT_SalesmanItemPmt'
BEGIN
    SELECT 
        p.FSellerID,
        FOrderNo,
        FOuterPmt, 
        FNormOverduePmt,
        FNormUnoverduePmt,
        FTExceedePmt FExceedePmt,
        FTOverduePmt FOverduePmt,
        FTUnoverduePmt FUnoverduePmt, 
        FOverdueWarranty,
        FUnoverdueWarranty,
        FTWarranty FWarranty
    FROM #pmt p
    LEFT JOIN V_BD_SALESMAN s1 ON p.FSellerID=s1.FID
    LEFT JOIN V_BD_SALESMAN s2 ON s1.FNUMBER=s2.FNUMBER
    WHERE (@FSellerID=0 OR s2.FID = @FSellerID)
END
ELSE IF @FormId='ora_PMT_FactoryPmt'
BEGIN
    SELECT 
        FFactoryID,
        SUM(FOuterPmt) FOuterPmt, 
        SUM(FNormOverduePmt) FNormOverduePmt,
        SUM(FNormUnoverduePmt) FNormUnoverduePmt,
        SUM(FTExceedePmt) FTExceedePmt,
        SUM(FTOverduePmt) FTOverduePmt,
        SUM(FTUnoverduePmt) FTUnoverduePmt, 
        SUM(FOverdueWarranty) FOverdueWarranty,
        SUM(FUnoverdueWarranty) FUnoverdueWarranty,
        SUM(FTWarranty) FWarranty
    FROM #pmt p 
    WHERE FFactoryID <> 0
    GROUP BY FFactoryID
END
ELSE IF @FormId='ora_PMT_FactoryCustPmt'
BEGIN
    SELECT 
        FFactoryID, c.FMASTERID FCustID,
        SUM(FOuterPmt) FOuterPmt, 
        SUM(FNormOverduePmt) FNormOverduePmt,
        SUM(FNormUnoverduePmt) FNormUnoverduePmt,
        SUM(FTExceedePmt) FTExceedePmt,
        SUM(FTOverduePmt) FTOverduePmt,
        SUM(FTUnoverduePmt) FTUnoverduePmt, 
        SUM(FOverdueWarranty) FOverdueWarranty,
        SUM(FUnoverdueWarranty) FUnoverdueWarranty,
        SUM(FTWarranty) FWarranty
    FROM #pmt p
    LEFT JOIN T_BD_CUSTOMER c ON p.FCustID=c.FCUSTID
    WHERE FFactoryID <> 0
    GROUP BY FFactoryID, c.FMASTERID
END
ELSE IF @FormId='ora_PMT_Outer'
BEGIN
    SELECT FSignOrgID,
        FOrderNo, 
        p.FSerialNum,
        p.FCustID,
        p.FSellerID,
        p.FPayWay,
        FLaterDelvGoodsDt, FIntervalDay, 
        FTOrderAmt,
        FDeliverAmt FTDeliverAmt,
        FReceiverAmt FTReceiverAmt,
        FInvoiceAmt FTInvoiceAmt,
        FOuterPmt,
        FRemark
    FROM #pmt p
END
ELSE IF @FormId='ora_PMT_FullPayment_Deliver'
BEGIN
    SELECT 
        FOrderNo, FSerialNum, FDirectors, 
        FSellerID, FDeptID, o.FOrgID, FCustID, o.FSaleOrgID, FPayWay,
        FOrderAmt, ofi.FLocalCurrId FCurrencyID,
        os.FBILLNO FDeliverNo, os.FDATE FDelvGoodsDt,
        oe.FSEQ FOrderSeq, oe.FMaterialID, oe.FSTOCKORGID FFactoryID,
        --oe.FInventoryQty FProdCapacity, --当前库存
        --oe.FAwaitQty FDelvQty, --待发数量
        --oe.FAvailableQty FDelvCapacity --可发库存
        0 FProdCapacity,
        0 FDelvQty,
        0 FDelvCapacity
    FROM (
        SELECT FID, FBeforeRcvAmt, SUM(FDeliverAmt) FTDeliverAmt,
            FOrderNo, FSerialNum, FDirectors, FSellerID, FDeptID, FOrgID,
            FCustID, FSaleOrgID, FPayWay, FTOrderAmt FOrderAmt
        FROM #pmt
        GROUP BY FID, FBeforeRcvAmt, FOrderNo, FSerialNum, FDirectors, FSellerID, FDeptID, FOrgID,
        FCustID, FSaleOrgID, FPayWay, FTOrderAmt
    ) o
    INNER JOIN T_SAL_ORDERFIN ofi ON o.FID=ofi.FID
    INNER JOIN T_SAL_ORDERENTRY oe ON o.FID=oe.FID
    --INNER JOIN T_SAL_ORDERENTRY_F oef ON oe.FENTRYID=oef.FENTRYID --财务信息拆分表
    --INNER JOIN T_SAL_ORDERENTRY_R oer ON oe.FENTRYID=oer.FENTRYID
    INNER JOIN T_SAL_OUTSTOCKENTRY_R osr ON oe.FENTRYID=osr.FSOENTRYID
    INNER JOIN T_SAL_OUTSTOCK os ON os.FID=osr.FID AND os.FDocumentStatus='C'
    --INNER JOIN T_SAL_OUTSTOCKENTRY_F osef ON os.FID=osef.FID
    WHERE ofi.FRecConditionId=306036 OR o.FBeforeRcvAmt>=o.FTDeliverAmt
    
END
ELSE IF @FormId='ora_PMT_FullPayment_Contract'
BEGIN
    SELECT 
        o.FOrderNo, o.FSerialNum,
        o.FDirectors, 
        o.FSellerID, o.FDeptID,
        o.FCustID,
        o.FPayWay,
        ofi.FLocalCurrId FCurrencyID,
        o.FOrderAmt,
        od.FDate FBillDt
    FROM (
        SELECT FID, FBeforeRcvAmt, SUM(FDeliverAmt) FTDeliverAmt,
            FOrderNo, FSerialNum, FDirectors, FSellerID, FDeptID, FOrgID,
            FCustID, FSaleOrgID, FPayWay, FTOrderAmt FOrderAmt
        FROM #pmt
        GROUP BY FID, FBeforeRcvAmt, FOrderNo, FSerialNum, FDirectors, FSellerID, FDeptID, FOrgID,
        FCustID, FSaleOrgID, FPayWay, FTOrderAmt
    ) o
    INNER JOIN T_SAL_ORDER od ON o.FID=od.FID
    INNER JOIN T_SAL_ORDERFIN ofi ON o.FID=ofi.FID
    WHERE ofi.FRecConditionId=306036 OR o.FBeforeRcvAmt>=o.FTDeliverAmt
END

--SELECT * FROM #pmt
DROP TABLE #pmt

END

-- exec proc_czly_GetPmt @FormId='ora_PMT_SalesmanPmt', @sDt='2020-05-01', @eDt='2020-06-01'