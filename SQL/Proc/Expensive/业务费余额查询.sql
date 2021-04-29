-- 业务费余额查询
CREATE PROC [dbo].[proc_czly_QueryExpBalance](
    @FYear INT,
    @FMonth INT,
    @FSellerNo VARCHAR(100)=''
)
AS
BEGIN

SET NOCOUNT ON

DECLARE @FLastYear INT, @FLastMonth INT

IF @FMonth = 1
BEGIN
    SET @FLastMonth=12
    SET @FLastYear=@FYear-1
END
ELSE
BEGIN
    SET @FLastMonth=@FMonth-1
    SET @FLastYear=@FYear
END
-- 期初余额
SELECT ISNULL(FCurrPeriodBalance, 0) FInitBalance, sm.FNumber FSellerNumber
INTO #balance
FROM ora_Exp_Balance b
INNER JOIN ora_Exp_BalanceEntry be ON b.FID=be.FID
INNER JOIN V_BD_SALESMAN sm ON be.FSellerID=sm.FID
WHERE b.FYear=@FLastYear AND b.FMonth=@FLastMonth AND b.FDOCUMENTSTATUS='C'

-- 结算金额
SELECT SUM(se.FRealSettleAmt) FAmt, sm.FNumber FSellerNumber
INTO #settle
FROM ora_OptExp_SettleEntry se
INNER JOIN ora_OptExp_Settle s ON s.FID=se.FID AND s.FDOCUMENTSTATUS='C'
INNER JOIN V_BD_SALESMAN sm ON se.FSellerID=sm.FID 
WHERE YEAR(s.FDate)=@FYear AND MONTH(s.FDATE)=@FMonth AND FDate >= '2020-09-01'
GROUP BY sm.FNumber

-- 调整金额
SELECT SUM(FAmount) FAmt, sm.FNumber FSellerNumber
INTO #adjust
FROM T_CZ_JSTZDEntry je
INNER JOIN T_CZ_JSTZD j ON je.FID=j.FID AND j.FDOCUMENTSTATUS='C'
INNER JOIN V_BD_SALESMAN sm ON je.FEmpID=sm.FID 
WHERE YEAR(F_ora_Date)=@FYear AND MONTH(F_ora_Date)=@FMonth
GROUP BY sm.FNumber

-- 新系统结算金额
SELECT (ISNULL(s.FAmt, 0)+ISNULL(a.FAmt, 0)) FNewSysAmt, s.FSellerNumber
INTO #newsys
FROM #settle s
LEFT JOIN #adjust a ON s.FSellerNumber=a.FSellerNumber

-- 老系统结算金额
SELECT SUM(FSettleAmt) FOldSysAmt,  sm.FNumber FSellerNumber
INTO #oldsys
FROM ora_Exp_OSRegisterEntry ose
INNER JOIN ora_Exp_OldSysRegister os ON ose.FID=os.FID AND os.FDOCUMENTSTATUS='C'
INNER JOIN V_BD_SALESMAN sm ON ose.FSellerID=sm.FID 
WHERE --YEAR(FRegisterDate)=@FYear AND MONTH(FRegisterDate)=@FMonth
os.FYear=@FYear AND os.FMonth=@FMonth
GROUP BY sm.FNumber

-- 已入账金额
SELECT SUM(FAmountBase) FRecordedAmt, e.FNumber FSellerNumber
INTO #record
FROM ora_t_AccountEntry ae
INNER JOIN ora_t_Account a ON ae.FID=a.FID AND a.FDOCUMENTSTATUS='C'
INNER JOIN T_HR_EMPINFO e ON e.FID=ae.FSellerEmpID
WHERE YEAR(FDate)=@FYear AND MONTH(FDate)=@FMonth AND FDate >= '2020-11-01'
AND FAccountActual=828919 --业务费
GROUP BY e.FNumber

-- SELECT  FInitBalance,
--     ISNULL(FNewSysAmt, 0) FNewSysAmt, 
--     ISNULL(FOldSysAmt, 0) FOldSysAmt, 
--     ISNULL(FRecordedAmt, 0) FRecordedAmt,
--     FInitBalance+ISNULL(FNewSysAmt, 0)+ISNULL(FOldSysAmt, 0)-ISNULL(FRecordedAmt, 0) FCurrPeriodBalance
-- FROM #balance b
-- LEFT JOIN #newsys n ON b.FSellerNumber=n.FSellerNumber
-- LEFT JOIN #oldsys o ON b.FSellerNumber=o.FSellerNumber
-- LEFT JOIN #record r ON b.FSellerNumber=r.FSellerNumber

SELECT FSellerNumber,
    SUM(FInitBalance) FInitBalance, SUM(FNewSysAmt) FNewSysAmt, 
    SUM(FOldSysAmt) FOldSysAmt, SUM(FRecordedAmt) FRecordedAmt,
    SUM(FInitBalance)+SUM(FNewSysAmt)+SUM(FOldSysAmt)-SUM(FRecordedAmt) FCurrPeriodBalance
INTO #result
FROM (
    SELECT FSellerNumber, FInitBalance, 0 FNewSysAmt, 0 FOldSysAmt, 0 FRecordedAmt FROM #balance
    UNION ALL
    SELECT FSellerNumber, 0 FInitBalance, FNewSysAmt, 0 FOldSysAmt, 0 FRecordedAmt FROM #newsys
    UNION ALL
    SELECT FSellerNumber, 0 FInitBalance, 0 FNewSysAmt, FOldSysAmt, 0 FRecordedAmt FROM #oldsys
    UNION ALL
    SELECT FSellerNumber, 0 FInitBalance, 0 FNewSysAmt, 0 FOldSysAmt, FRecordedAmt FROM #record
) t
GROUP BY FSellerNumber--,FInitBalance,FNewSysAmt,FOldSysAmt,FRecordedAmt

SELECT sm.FID FSellerId, r.* FROM #result r
INNER JOIN V_BD_SALESMAN sm ON sm.FNUMBER=r.FSellerNumber AND sm.FBizOrgID=1
WHERE FSellerNumber LIKE '%'+ @FSellerNo +'%'

DROP TABLE #balance
DROP TABLE #settle
DROP TABLE #adjust
DROP TABLE #oldsys
DROP TABLE #newsys
DROP TABLE #record

END

-- exec proc_czly_QueryExpBalance 2020, 9
