/*
销售员签订产品价格分析
*/
ALTER PROC [dbo].[proc_czly_SellerProdSign](
    @QDate DATETIME='', 
    @QSaleNo VARCHAR(100)=''
)
AS
BEGIN

SET NOCOUNT ON

-- DECLARE @QSaleNo VARCHAR(100)=''
-- DECLARE @QDate DATETIME=''

IF @QDate='' SET @QDate=GETDATE()

DECLARE @year INT=YEAR(@QDate)
       ,@month INT=MONTH(@QDate)
       ,@day INT=DAY(@QDate)


-- 全部订单
SELECT o.FDate, sm.FNumber FSaleNo, oef.FALLAMOUNT FOrderRowAmt, CONVERT(DECIMAL(18,6),oe.FBDownPoints) FBDownPoints,
    CASE WHEN CONVERT(DECIMAL(18,6),oe.FBDownPoints)>10 THEN '基价90%以下'
         WHEN CONVERT(DECIMAL(18,6),oe.FBDownPoints)>5 AND CONVERT(DECIMAL(18,6),oe.FBDownPoints)<=10 THEN '基价90%-95%'
         WHEN CONVERT(DECIMAL(18,6),oe.FBDownPoints)>0 AND CONVERT(DECIMAL(18,6),oe.FBDownPoints)<=5 THEN '基价95%-100%'
         WHEN CONVERT(DECIMAL(18,6),oe.FBDownPoints)=0 THEN '基价100%'
         WHEN CONVERT(DECIMAL(18,6),oe.FBDownPoints)>=-10 AND CONVERT(DECIMAL(18,6),oe.FBDownPoints)<0 THEN '基价100%-110%'
         WHEN CONVERT(DECIMAL(18,6),oe.FBDownPoints)<-10 THEN '基价110%以上'
    END FPriceRange
INTO #t_order
FROM T_SAL_ORDER o
INNER JOIN T_SAL_ORDERENTRY oe ON o.FID=oe.FID
INNER JOIN T_SAL_ORDERENTRY_F oef ON oef.FENTRYID=oe.FENTRYID
INNER JOIN V_BD_SALESMAN sm ON sm.FID=o.FSalerID
INNER JOIN T_BD_DEPARTMENT d ON dbo.fun_czty_GetWorkDeptID(sm.FDEPTID)=d.FDEPTID
WHERE sm.FNUMBER LIKE '%'+ @QSaleNo +'%'
AND oe.F_ORA_JJYY=''


SELECT FSaleNo, 
    SUM(F110U_D) F110U_D, SUM(F110U_M) F110U_M, SUM(F110U_Y) F110U_Y,
    SUM(F100U_D) F100U_D, SUM(F100U_M) F100U_M, SUM(F100U_Y) F100U_Y,
    SUM(F100_D) F100_D, SUM(F100_M) F100_M, SUM(F100_Y) F100_Y,
    SUM(F95U_D) F95U_D, SUM(F95U_M) F95U_M, SUM(F95U_Y) F95U_Y,
    SUM(F90U_D) F90U_D, SUM(F90U_M) F90U_M, SUM(F90U_Y) F90U_Y,
    SUM(F90D_D) F90D_D, SUM(F90D_M) F90D_M, SUM(F90D_Y) F90D_Y,
    SUM(FTotal) FTotal
INTO #result
FROM (
    ---------------------------------- 基价110%以上 -------------------------------------
    -- 基价110%以上 当日
    SELECT FSaleNo, 
        FOrderRowAmt F110U_D, 0 F110U_M, 0 F110U_Y,
        0 F100U_D, 0 F100U_M, 0 F100U_Y,
        0 F100_D, 0 F100_M, 0 F100_Y,
        0 F95U_D, 0 F95U_M, 0 F95U_Y,
        0 F90U_D, 0 F90U_M, 0 F90U_Y,
        0 F90D_D, 0 F90D_M, 0 F90D_Y, 0 FTotal
    FROM #t_order WHERE FPriceRange='基价110%以上'
    AND YEAR(FDate)=@year AND MONTH(FDate)=@month AND DAY(FDate)=@day
    UNION ALL
    -- 基价110%以上 当月
    SELECT FSaleNo, 
        0 F110U_D, FOrderRowAmt F110U_M, 0 F110U_Y,
        0 F100U_D, 0 F100U_M, 0 F100U_Y,
        0 F100_D, 0 F100_M, 0 F100_Y,
        0 F95U_D, 0 F95U_M, 0 F95U_Y,
        0 F90U_D, 0 F90U_M, 0 F90U_Y,
        0 F90D_D, 0 F90D_M, 0 F90D_Y, 0 FTotal
    FROM #t_order WHERE FPriceRange='基价110%以上'
    AND YEAR(FDate)=@year AND MONTH(FDate)=@month
    UNION ALL
    -- 基价110%以上 当年
    SELECT FSaleNo, 
        0 F110U_D, 0 F110U_M, FOrderRowAmt F110U_Y,
        0 F100U_D, 0 F100U_M, 0 F100U_Y,
        0 F100_D, 0 F100_M, 0 F100_Y,
        0 F95U_D, 0 F95U_M, 0 F95U_Y,
        0 F90U_D, 0 F90U_M, 0 F90U_Y,
        0 F90D_D, 0 F90D_M, 0 F90D_Y, FOrderRowAmt FTotal
    FROM #t_order WHERE FPriceRange='基价110%以上'
    AND YEAR(FDate)=@year
    UNION ALL
    ---------------------------------- 基价100%-110% -------------------------------------
    -- 基价100%以上 当日
    SELECT FSaleNo, 
        0 F110U_D, 0 F110U_M, 0 F110U_Y,
        FOrderRowAmt F100U_D, 0 F100U_M, 0 F100U_Y,
        0 F100_D, 0 F100_M, 0 F100_Y,
        0 F95U_D, 0 F95U_M, 0 F95U_Y,
        0 F90U_D, 0 F90U_M, 0 F90U_Y,
        0 F90D_D, 0 F90D_M, 0 F90D_Y, 0 FTotal
    FROM #t_order WHERE FPriceRange='基价100%-110%' 
    AND YEAR(FDate)=@year AND MONTH(FDate)=@month AND DAY(FDate)=@day
    UNION ALL
    -- 基价100%以上 当月
    SELECT FSaleNo, 
        0 F110U_D, 0 F110U_M, 0 F110U_Y,
        0 F100U_D, FOrderRowAmt F100U_M, 0 F100U_Y,
        0 F100_D, 0 F100_M, 0 F100_Y,
        0 F95U_D, 0 F95U_M, 0 F95U_Y,
        0 F90U_D, 0 F90U_M, 0 F90U_Y,
        0 F90D_D, 0 F90D_M, 0 F90D_Y, 0 FTotal
    FROM #t_order WHERE FPriceRange='基价100%-110%' 
    AND YEAR(FDate)=@year AND MONTH(FDate)=@month
    UNION ALL
    -- 基价100%以上 当年
    SELECT FSaleNo, 
        0 F110U_D, 0 F110U_M, 0 F110U_Y,
        0 F100U_D, 0 F100U_M, FOrderRowAmt F100U_Y,
        0 F100_D, 0 F100_M, 0 F100_Y,
        0 F95U_D, 0 F95U_M, 0 F95U_Y,
        0 F90U_D, 0 F90U_M, 0 F90U_Y,
        0 F90D_D, 0 F90D_M, 0 F90D_Y, FOrderRowAmt FTotal
    FROM #t_order WHERE FPriceRange='基价100%-110%' 
    AND YEAR(FDate)=@year
    UNION ALL
    ---------------------------------- 基价100% -------------------------------------
    -- 基价100% 当日
    SELECT FSaleNo, 
        0 F110U_D, 0 F110U_M, 0 F110U_Y,
        0 F100U_D, 0 F100U_M, 0 F100U_Y,
        FOrderRowAmt F100_D, 0 F100_M, 0 F100_Y,
        0 F95U_D, 0 F95U_M, 0 F95U_Y,
        0 F90U_D, 0 F90U_M, 0 F90U_Y,
        0 F90D_D, 0 F90D_M, 0 F90D_Y, 0 FTotal
    FROM #t_order WHERE FPriceRange='基价100%'
    AND YEAR(FDate)=@year AND MONTH(FDate)=@month AND DAY(FDate)=@day
    UNION ALL
    -- 基价100% 当月
    SELECT FSaleNo, 
        0 F110U_D, 0 F110U_M, 0 F110U_Y,
        0 F100U_D, 0 F100U_M, 0 F100U_Y,
        0 F100_D, FOrderRowAmt F100_M, 0 F100_Y,
        0 F95U_D, 0 F95U_M, 0 F95U_Y,
        0 F90U_D, 0 F90U_M, 0 F90U_Y,
        0 F90D_D, 0 F90D_M, 0 F90D_Y, 0 FTotal
    FROM #t_order WHERE FPriceRange='基价100%'
    AND YEAR(FDate)=@year AND MONTH(FDate)=@month
    UNION ALL
    -- 基价100% 当年
    SELECT FSaleNo, 
        0 F110U_D, 0 F110U_M, 0 F110U_Y,
        0 F100U_D, 0 F100U_M, 0 F100U_Y,
        0 F100_D, 0 F100_M, FOrderRowAmt F100_Y,
        0 F95U_D, 0 F95U_M, 0 F95U_Y,
        0 F90U_D, 0 F90U_M, 0 F90U_Y,
        0 F90D_D, 0 F90D_M, 0 F90D_Y, FOrderRowAmt FTotal
    FROM #t_order WHERE FPriceRange='基价100%'
    AND YEAR(FDate)=@year
    UNION ALL
    ---------------------------------- 基价95%-100% -------------------------------------
    -- 基价95%-100% 当日
    SELECT FSaleNo, 
        0 F110U_D, 0 F110U_M, 0 F110U_Y,
        0 F100U_D, 0 F100U_M, 0 F100U_Y,
        0 F100_D, 0 F100_M, 0 F100_Y,
        FOrderRowAmt F95U_D, 0 F95U_M, 0 F95U_Y,
        0 F90U_D, 0 F90U_M, 0 F90U_Y,
        0 F90D_D, 0 F90D_M, 0 F90D_Y, 0 FTotal
    FROM #t_order WHERE FPriceRange='基价95%-100%'
    AND YEAR(FDate)=@year AND MONTH(FDate)=@month AND DAY(FDate)=@day
    UNION ALL
    -- 基价95%-100% 当月
    SELECT FSaleNo, 
        0 F110U_D, 0 F110U_M, 0 F110U_Y,
        0 F100U_D, 0 F100U_M, 0 F100U_Y,
        0 F100_D, 0 F100_M, 0 F100_Y,
        0 F95U_D, FOrderRowAmt F95U_M, 0 F95U_Y,
        0 F90U_D, 0 F90U_M, 0 F90U_Y,
        0 F90D_D, 0 F90D_M, 0 F90D_Y, 0 FTotal
    FROM #t_order WHERE FPriceRange='基价95%-100%'
    AND YEAR(FDate)=@year AND MONTH(FDate)=@month
    UNION ALL
    -- 基价95%-100% 当年
    SELECT FSaleNo, 
        0 F110U_D, 0 F110U_M, 0 F110U_Y,
        0 F100U_D, 0 F100U_M, 0 F100U_Y,
        0 F100_D, 0 F100_M, 0 F100_Y,
        0 F95U_D, 0 F95U_M, FOrderRowAmt F95U_Y,
        0 F90U_D, 0 F90U_M, 0 F90U_Y,
        0 F90D_D, 0 F90D_M, 0 F90D_Y, FOrderRowAmt FTotal
    FROM #t_order WHERE FPriceRange='基价95%-100%'
    AND YEAR(FDate)=@year
    UNION ALL
    ---------------------------------- 基价90%-95% -------------------------------------
    -- 基价90%-95% 当日
    SELECT FSaleNo, 
        0 F110U_D, 0 F110U_M, 0 F110U_Y,
        0 F100U_D, 0 F100U_M, 0 F100U_Y,
        0 F100_D, 0 F100_M, 0 F100_Y,
        0 F95U_D, 0 F95U_M, 0 F95U_Y,
        FOrderRowAmt F90U_D, 0 F90U_M, 0 F90U_Y,
        0 F90D_D, 0 F90D_M, 0 F90D_Y, 0 FTotal
    FROM #t_order WHERE FPriceRange='基价90%-95%'
    AND YEAR(FDate)=@year AND MONTH(FDate)=@month AND DAY(FDate)=@day
    UNION ALL
    -- 基价90%-95% 当月
    SELECT FSaleNo, 
        0 F110U_D, 0 F110U_M, 0 F110U_Y,
        0 F100U_D, 0 F100U_M, 0 F100U_Y,
        0 F100_D, 0 F100_M, 0 F100_Y,
        0 F95U_D, 0 F95U_M, 0 F95U_Y,
        0 F90U_D, FOrderRowAmt F90U_M, 0 F90U_Y,
        0 F90D_D, 0 F90D_M, 0 F90D_Y, 0 FTotal
    FROM #t_order WHERE FPriceRange='基价90%-95%'
    AND YEAR(FDate)=@year AND MONTH(FDate)=@month
    UNION ALL
    -- 基价90%-95% 当年
    SELECT FSaleNo, 
        0 F110U_D, 0 F110U_M, 0 F110U_Y,
        0 F100U_D, 0 F100U_M, 0 F100U_Y,
        0 F100_D, 0 F100_M, 0 F100_Y,
        0 F95U_D, 0 F95U_M, 0 F95U_Y,
        0 F90U_D, 0 F90U_M, FOrderRowAmt F90U_Y,
        0 F90D_D, 0 F90D_M, 0 F90D_Y, FOrderRowAmt FTotal
    FROM #t_order WHERE FPriceRange='基价90%-95%'
    AND YEAR(FDate)=@year
    UNION ALL
    ---------------------------------- 基价90%以下 -------------------------------------
    -- 基价90%以下 当日
    SELECT FSaleNo, 
        0 F110U_D, 0 F110U_M, 0 F110U_Y,
        0 F100U_D, 0 F100U_M, 0 F100U_Y,
        0 F100_D, 0 F100_M, 0 F100_Y,
        0 F95U_D, 0 F95U_M, 0 F95U_Y,
        0 F90U_D, 0 F90U_M, 0 F90U_Y,
        FOrderRowAmt F90D_D, 0 F90D_M, 0 F90D_Y, 0 FTotal
    FROM #t_order WHERE FPriceRange='基价90%以下'
    AND YEAR(FDate)=@year AND MONTH(FDate)=@month AND DAY(FDate)=@day
    UNION ALL
    -- 基价90%以下 当月
    SELECT FSaleNo, 
        0 F110U_D, 0 F110U_M, 0 F110U_Y,
        0 F100U_D, 0 F100U_M, 0 F100U_Y,
        0 F100_D, 0 F100_M, 0 F100_Y,
        0 F95U_D, 0 F95U_M, 0 F95U_Y,
        0 F90U_D, 0 F90U_M, 0 F90U_Y,
        0 F90D_D, FOrderRowAmt F90D_M, 0 F90D_Y, 0 FTotal
    FROM #t_order WHERE FPriceRange='基价90%以下'
    AND YEAR(FDate)=@year AND MONTH(FDate)=@month
    UNION ALL
    -- 基价90%以下 当年
    SELECT FSaleNo, 
        0 F110U_D, 0 F110U_M, 0 F110U_Y,
        0 F100U_D, 0 F100U_M, 0 F100U_Y,
        0 F100_D, 0 F100_M, 0 F100_Y,
        0 F95U_D, 0 F95U_M, 0 F95U_Y,
        0 F90U_D, 0 F90U_M, 0 F90U_Y,
        0 F90D_D, 0 F90D_M, FOrderRowAmt F90D_Y, FOrderRowAmt FTotal
    FROM #t_order WHERE FPriceRange='基价90%以下'
    AND YEAR(FDate)=@year

) t GROUP BY FSaleNo


SELECT el.FName FSaleName, r.* 
FROM #result r
INNER JOIN T_HR_EMPINFO e ON r.FSaleNo=e.FNUMBER
INNER JOIN T_HR_EMPINFO_L el ON e.FID=el.FID AND el.FLOCALEID=2052


DROP TABLE #t_order
DROP TABLE #result


END

/*
EXEC proc_czly_SellerProdSign @QDate='#FDate#', @QSaleNo='#FSaleNo#'
EXEC proc_czly_SellerProdSign @QDate='2020-12-31'
*/