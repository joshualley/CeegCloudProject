/*
营销单位单笔订单分布
*/
ALTER PROC [dbo].[proc_czly_OrgSingleOrder](
    @QOrgNo VARCHAR(100)='',
    @QBeginDate DATETIME='',
    @QEndDate DATETIME='',
    @QDate DATETIME=''
) AS
BEGIN

SET NOCOUNT ON
-- DECLARE @QOrgNo VARCHAR(100)=''
-- DECLARE @QDate DATETIME=''

IF @QDate='' SET @QDate=GETDATE()


DECLARE @year INT=YEAR(@QDate)
       ,@month INT=MONTH(@QDate)
       ,@day INT=DAY(@QDate)


SELECT o.FDate, org.FOrgID, oe.FOrderAmt, 
    CASE WHEN ofi.FBILLALLAMOUNT<500000 THEN '50万以下'
         WHEN ofi.FBILLALLAMOUNT>=500000 AND ofi.FBILLALLAMOUNT<1000000 THEN '50-100万'
         WHEN ofi.FBILLALLAMOUNT>=1000000 AND ofi.FBILLALLAMOUNT<2000000 THEN '100-200万'
         WHEN ofi.FBILLALLAMOUNT>2000000 THEN '200万以上'
    END FDistribution
INTO #t_order
FROM T_SAL_ORDER o
INNER JOIN (
    SELECT oe.FID, SUM(FALLAMOUNT) FOrderAmt FROM T_SAL_ORDERENTRY oe
    INNER JOIN T_SAL_ORDERENTRY_F oef ON oef.FENTRYID=oe.FENTRYID
    WHERE oe.F_ORA_JJYY=''
    GROUP BY oe.FID
) oe ON oe.FID=o.FID
INNER JOIN T_SAL_ORDERFIN ofi ON o.FID=ofi.FID
INNER JOIN V_BD_SALESMAN sm ON sm.FID=o.FSalerID
INNER JOIN T_BD_DEPARTMENT d ON dbo.fun_czty_GetWorkDeptID(sm.FDEPTID)=d.FDEPTID
INNER JOIN T_ORG_ORGANIZATIONS org ON org.FOrgID=d.FUseOrgId
WHERE org.FNUMBER LIKE '%'+ @QOrgNo +'%'

SELECT FOrgID,
    SUM(F50W_M) F50W_M, SUM(F50_100W_M) F50_100W_M, SUM(F100_200W_M) F100_200W_M, SUM(F200W_M) F200W_M, SUM(FSum_M) FSum_M,
    SUM(F50W_Y) F50W_Y, SUM(F50_100W_Y) F50_100W_Y, SUM(F100_200W_Y) F100_200W_Y, SUM(F200W_Y) F200W_Y, SUM(FSum_Y) FSum_Y
INTO #t_result
FROM (
    -- 本月
    SELECT FOrgID, 
        FOrderAmt F50W_M, 0 F50_100W_M, 0 F100_200W_M, 0 F200W_M, 0 FSum_M,
        0 F50W_Y, 0 F50_100W_Y, 0 F100_200W_Y, 0 F200W_Y, 0 FSum_Y
    FROM #t_order WHERE FDistribution='50万以下'
    AND YEAR(FDate)=@year AND MONTH(FDate)=@month
    UNION ALL
    SELECT FOrgID, 
        0 F50W_M, FOrderAmt F50_100W_M, 0 F100_200W_M, 0 F200W_M, 0 FSum_M,
        0 F50W_Y, 0 F50_100W_Y, 0 F100_200W_Y, 0 F200W_Y, 0 FSum_Y
    FROM #t_order WHERE FDistribution='50-100万'
    AND YEAR(FDate)=@year AND MONTH(FDate)=@month
    UNION ALL
    SELECT FOrgID, 
        0 F50W_M, 0 F50_100W_M, FOrderAmt F100_200W_M, 0 F200W_M, 0 FSum_M,
        0 F50W_Y, 0 F50_100W_Y, 0 F100_200W_Y, 0 F200W_Y, 0 FSum_Y
    FROM #t_order WHERE FDistribution='100-200万'
    AND YEAR(FDate)=@year AND MONTH(FDate)=@month
    UNION ALL
    SELECT FOrgID, 
        0 F50W_M, 0 F50_100W_M, 0 F100_200W_M, FOrderAmt F200W_M, 0 FSum_M,
        0 F50W_Y, 0 F50_100W_Y, 0 F100_200W_Y, 0 F200W_Y, 0 FSum_Y
    FROM #t_order WHERE FDistribution='200万以上'
    AND YEAR(FDate)=@year AND MONTH(FDate)=@month
    UNION ALL
    SELECT FOrgID, 
        0 F50W_M, 0 F50_100W_M, 0 F100_200W_M, 0 F200W_M, FOrderAmt FSum_M,
        0 F50W_Y, 0 F50_100W_Y, 0 F100_200W_Y, 0 F200W_Y, 0 FSum_Y
    FROM #t_order WHERE YEAR(FDate)=@year AND MONTH(FDate)=@month
    UNION ALL
    -- 本年
    SELECT FOrgID, 
        0 F50W_M, 0 F50_100W_M, 0 F100_200W_M, 0 F200W_M, 0 FSum_M,
        FOrderAmt F50W_Y, 0 F50_100W_Y, 0 F100_200W_Y, 0 F200W_Y, 0 FSum_Y
    FROM #t_order WHERE FDistribution='50万以下'
    AND (@QBeginDate='' AND @QEndDate='' AND YEAR(FDate)=YEAR(@QDate))
        OR (FDate BETWEEN @QBeginDate AND @QEndDate)
    UNION ALL
    SELECT FOrgID, 
        0 F50W_M, 0 F50_100W_M, 0 F100_200W_M, 0 F200W_M, 0 FSum_M,
        0 F50W_Y, FOrderAmt F50_100W_Y, 0 F100_200W_Y, 0 F200W_Y, 0 FSum_Y
    FROM #t_order WHERE FDistribution='50-100万'
    AND (@QBeginDate='' AND @QEndDate='' AND YEAR(FDate)=YEAR(@QDate))
        OR (FDate BETWEEN @QBeginDate AND @QEndDate)
    UNION ALL
    SELECT FOrgID, 
        0 F50W_M, 0 F50_100W_M, 0 F100_200W_M, 0 F200W_M, 0 FSum_M,
        0 F50W_Y, 0 F50_100W_Y, FOrderAmt F100_200W_Y, 0 F200W_Y, 0 FSum_Y
    FROM #t_order WHERE FDistribution='100-200万'
    AND (@QBeginDate='' AND @QEndDate='' AND YEAR(FDate)=YEAR(@QDate))
        OR (FDate BETWEEN @QBeginDate AND @QEndDate)
    UNION ALL
    SELECT FOrgID, 
        0 F50W_M, 0 F50_100W_M, 0 F100_200W_M, 0 F200W_M, 0 FSum_M,
        0 F50W_Y, 0 F50_100W_Y, 0 F100_200W_Y, FOrderAmt F200W_Y, 0 FSum_Y
    FROM #t_order WHERE FDistribution='200万以上'
    AND (@QBeginDate='' AND @QEndDate='' AND YEAR(FDate)=YEAR(@QDate))
        OR (FDate BETWEEN @QBeginDate AND @QEndDate)
    UNION ALL
    SELECT FOrgID, 
        0 F50W_M, 0 F50_100W_M, 0 F100_200W_M, 0 F200W_M, 0 FSum_M,
        0 F50W_Y, 0 F50_100W_Y, 0 F100_200W_Y, 0 F200W_Y, FOrderAmt FSum_Y
    FROM #t_order WHERE 
        (@QBeginDate='' AND @QEndDate='' AND YEAR(FDate)=YEAR(@QDate))
        OR (FDate BETWEEN @QBeginDate AND @QEndDate)
) t GROUP BY FOrgID


SELECT o.FNUMBER FOrgNo, ol.FNAME FOrgName,
    r.*
FROM #t_result r
INNER JOIN T_ORG_ORGANIZATIONS o ON o.FORGID=r.FORGID
INNER JOIN T_ORG_ORGANIZATIONS_L ol ON o.FORGID=ol.FORGID AND ol.FLocalEID=2052

DROP TABLE #t_order
DROP TABLE #t_result

END

/*
EXEC proc_czly_OrgSingleOrder  @QDate='#FDate#', 
    @QBeginDate='#FBeginDate#', @QEndDate='#FEndDate#', 
    @QOrgNo='#FOrgNo#'
*/