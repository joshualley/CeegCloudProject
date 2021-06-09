/*
营销单位订单分价格段分析
*/
ALTER PROC [dbo].[proc_czly_OrgOrderPrice](
    @QOrgNo VARCHAR(100)='',
    @QBeginDate DATETIME='',
    @QEndDate DATETIME='',
    @QDate DATETIME=''
) AS
BEGIN

SET NOCOUNT ON

IF @QDate='' SET @QDate=GETDATE()


DECLARE @year INT=YEAR(@QDate)
       ,@month INT=MONTH(@QDate)
       ,@day INT=DAY(@QDate)


-- 全款订单
SELECT o.FDate, org.FOrgID, oef.FALLAMOUNT FOrderRowAmt,
    CASE WHEN CONVERT(DECIMAL(18,6),oe.FBDownPoints)>0 THEN '特价'
         WHEN CONVERT(DECIMAL(18,6),oe.FBDownPoints)=0 THEN '基价'
         WHEN CONVERT(DECIMAL(18,6),oe.FBDownPoints)<0 THEN '超价'
    END FPriceRange
INTO #t_order
FROM T_SAL_ORDER o
INNER JOIN T_SAL_ORDERENTRY oe ON oe.FID=o.FID
INNER JOIN T_SAL_ORDERENTRY_F oef ON oef.FENTRYID=oe.FENTRYID
INNER JOIN V_BD_SALESMAN sm ON sm.FID=o.FSalerID
INNER JOIN T_BD_DEPARTMENT d ON dbo.fun_czty_GetWorkDeptID(sm.FDEPTID)=d.FDEPTID
INNER JOIN T_ORG_ORGANIZATIONS org ON org.FOrgID=d.FUseOrgId
WHERE org.FNUMBER LIKE '%'+ @QOrgNo +'%'
AND oe.F_ORA_JJYY=''


SELECT FOrgId,
    SUM(FUpperAmtD) FUpperAmtD, SUM(FUpperAmtM) FUpperAmtM, SUM(FUpperAmtY) FUpperAmtY,
    SUM(FBaseAmtD) FBaseAmtD, SUM(FBaseAmtM) FBaseAmtM, SUM(FBaseAmtY) FBaseAmtY,
    SUM(FLowerAmtD) FLowerAmtD, SUM(FLowerAmtM) FLowerAmtM, SUM(FLowerAmtY) FLowerAmtY,
    SUM(FTotal) FTotal
INTO #t_result
FROM (
    --------------------------超价-------------------------------
    -- 当日
    SELECT FOrgId, 
        FOrderRowAmt FUpperAmtD, 0 FUpperAmtM, 0 FUpperAmtY,
        0 FBaseAmtD, 0 FBaseAmtM, 0 FBaseAmtY,
        0 FLowerAmtD, 0 FLowerAmtM, 0 FLowerAmtY, 0 FTotal
    FROM #t_order WHERE YEAR(FDate)=YEAR(@QDate) AND MONTH(FDate)=MONTH(@QDate) AND DAY(FDate)=DAY(@QDate)
    AND FPriceRange='超价'
    UNION ALL
    -- 当月
    SELECT FOrgId, 
        0 FUpperAmtD, FOrderRowAmt FUpperAmtM, 0 FUpperAmtY,
        0 FBaseAmtD, 0 FBaseAmtM, 0 FBaseAmtY,
        0 FLowerAmtD, 0 FLowerAmtM, 0 FLowerAmtY, 0 FTotal
    FROM #t_order WHERE YEAR(FDate)=YEAR(@QDate) AND MONTH(FDate)=MONTH(@QDate)
    AND FPriceRange='超价'
    UNION ALL
    -- 当年
    SELECT FOrgId, 
        0 FUpperAmtD, 0 FUpperAmtM, FOrderRowAmt FUpperAmtY,
        0 FBaseAmtD, 0 FBaseAmtM, 0 FBaseAmtY,
        0 FLowerAmtD, 0 FLowerAmtM, 0 FLowerAmtY, FOrderRowAmt FTotal
    FROM #t_order WHERE 
        (@QBeginDate='' AND @QEndDate='' AND YEAR(FDate)=YEAR(@QDate))
        OR (FDate BETWEEN @QBeginDate AND @QEndDate)
    AND FPriceRange='超价'
    UNION ALL
    --------------------------基价-------------------------------
    -- 当日
    SELECT FOrgId, 
        0 FUpperAmtD, 0 FUpperAmtM, 0 FUpperAmtY,
        FOrderRowAmt FBaseAmtD, 0 FBaseAmtM, 0 FBaseAmtY,
        0 FLowerAmtD, 0 FLowerAmtM, 0 FLowerAmtY, 0 FTotal
    FROM #t_order WHERE YEAR(FDate)=YEAR(@QDate) AND MONTH(FDate)=MONTH(@QDate) AND DAY(FDate)=DAY(@QDate)
    AND FPriceRange='基价'
    UNION ALL
    -- 当月
    SELECT FOrgId, 
        0 FUpperAmtD, 0 FUpperAmtM, 0 FUpperAmtY,
        0 FBaseAmtD, FOrderRowAmt FBaseAmtM, 0 FBaseAmtY,
        0 FLowerAmtD, 0 FLowerAmtM, 0 FLowerAmtY, 0 FTotal
    FROM #t_order WHERE YEAR(FDate)=YEAR(@QDate) AND MONTH(FDate)=MONTH(@QDate)
    AND FPriceRange='基价'
    UNION ALL
    -- 当年
    SELECT FOrgId, 
        0 FUpperAmtD, 0 FUpperAmtM, 0 FUpperAmtY,
        0 FBaseAmtD, 0 FBaseAmtM, FOrderRowAmt FBaseAmtY,
        0 FLowerAmtD, 0 FLowerAmtM, 0 FLowerAmtY, FOrderRowAmt FTotal
    FROM #t_order WHERE 
        (@QBeginDate='' AND @QEndDate='' AND YEAR(FDate)=YEAR(@QDate))
        OR (FDate BETWEEN @QBeginDate AND @QEndDate)
    AND FPriceRange='基价'
    UNION ALL
    --------------------------特价-------------------------------
    -- 当日
    SELECT FOrgId, 
        0 FUpperAmtD, 0 FUpperAmtM, 0 FUpperAmtY,
        0 FBaseAmtD, 0 FBaseAmtM, 0 FBaseAmtY,
        FOrderRowAmt FLowerAmtD, 0 FLowerAmtM, 0 FLowerAmtY, 0 FTotal
    FROM #t_order WHERE YEAR(FDate)=YEAR(@QDate) AND MONTH(FDate)=MONTH(@QDate) AND DAY(FDate)=DAY(@QDate)
    AND FPriceRange='特价'
    UNION ALL
    -- 当月
    SELECT FOrgId, 
        0 FUpperAmtD, 0 FUpperAmtM, 0 FUpperAmtY,
        0 FBaseAmtD, 0 FBaseAmtM, 0 FBaseAmtY,
        0 FLowerAmtD, FOrderRowAmt FLowerAmtM, 0 FLowerAmtY, 0 FTotal
    FROM #t_order WHERE YEAR(FDate)=YEAR(@QDate) AND MONTH(FDate)=MONTH(@QDate)
    AND FPriceRange='特价'
    UNION ALL
    -- 当年
    SELECT FOrgId, 
        0 FUpperAmtD, 0 FUpperAmtM, 0 FUpperAmtY,
        0 FBaseAmtD, 0 FBaseAmtM, 0 FBaseAmtY,
        0 FLowerAmtD, 0 FLowerAmtM, FOrderRowAmt FLowerAmtY, FOrderRowAmt FTotal
    FROM #t_order WHERE 
        (@QBeginDate='' AND @QEndDate='' AND YEAR(FDate)=YEAR(@QDate))
        OR (FDate BETWEEN @QBeginDate AND @QEndDate)
    AND FPriceRange='特价'
) t GROUP BY FOrgId


SELECT o.FNUMBER FOrgNo, ol.FNAME FOrgName, r.*
FROM #t_result r
INNER JOIN T_ORG_ORGANIZATIONS o ON o.FORGID=r.FORGID
INNER JOIN T_ORG_ORGANIZATIONS_L ol ON o.FORGID=ol.FORGID AND ol.FLocalEID=2052

DROP TABLE #t_order
DROP TABLE #t_result


END
/*
EXEC proc_czly_OrgOrderPrice @QDate='#FDate#', 
    @QBeginDate='#FBeginDate#', @QEndDate='#FEndDate#', 
    @QOrgNo='#FOrgNo#'
*/