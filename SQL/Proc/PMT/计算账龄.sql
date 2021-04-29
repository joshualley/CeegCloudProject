--计算账龄
CREATE PROC [dbo].[proc_czly_GetAging](
    @Type VARCHAR(100)
)
AS
BEGIN

CREATE TABLE #pmt_entry(
    FID BIGINT,
    FEntryID BIGINT,
    FBeforeRcvAmt DECIMAL(18, 2),
    FOrderNo VARCHAR(100),
    FOrderSeq INT,
    FFactoryID BIGINT,
    FDirectors VARCHAR(255),
    FSerialNum VARCHAR(255),
    FSaleOrgID BIGINT, 
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
DECLARE @SDt DATETIME = (SELECT TOP 1 FDate FROM T_SAL_ORDER ORDER BY FDate ASC)
DECLARE @EDt DATETIME = GETDATE()

INSERT INTO #pmt_entry EXEC proc_czly_GetPmtDetail2 @SDt=@sDt, @EDt=@eDt

IF @Type = 'Dept'
BEGIN
    SELECT FDeptID, MAX(FIntervalMonth) FAging, SUM(FOuterPmt) FOuterPmt
    FROM #pmt_entry WHERE FDeptID <> 0
    GROUP BY FDeptID
END
ELSE 
IF @Type = 'Factory'
BEGIN
    SELECT FFactoryID, MAX(FIntervalMonth) FAging, SUM(FOuterPmt) FOuterPmt
    FROM #pmt_entry WHERE FFactoryID <> 0
    GROUP BY FFactoryID
END

DROP TABLE #pmt_entry

END