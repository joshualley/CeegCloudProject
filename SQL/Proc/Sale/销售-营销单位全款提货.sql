/*
营销单位全款提货
*/
ALTER PROC [dbo].[proc_czly_OrgFullPay](
    @QOrgNo VARCHAR(100)='',
    @QBeginDate DATETIME='',
    @QEndDate DATETIME='',
    @QDate DATETIME=''
) AS
BEGIN
-- DECLARE @QOrgNo VARCHAR(100)=''
-- DECLARE @QDate DATETIME=''

SET NOCOUNT ON

IF @QDate='' SET @QDate=GETDATE()


DECLARE @year INT=YEAR(@QDate)
       ,@month INT=MONTH(@QDate)
       ,@day INT=DAY(@QDate)


-- 全款订单
SELECT o.FDate, org.FOrgID, 
    -- ofi.FBILLALLAMOUNT FOrderAmt, 
    oe.FOrderAmt, 
    ofi.FRecConditionId
INTO #t_fullpay
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
AND dbo.Fun_IsFullPay(ofi.FRecConditionId)=1


SELECT FOrgId,
    SUM(FFullPayAmtD) FFullPayAmtD, SUM(FFullPayAmtM) FFullPayAmtM, SUM(FFullPayAmtY) FFullPayAmtY
INTO #t_result
FROM (
    -- 全款当日
    SELECT FOrgId, 
        FOrderAmt FFullPayAmtD, 0 FFullPayAmtM, 0 FFullPayAmtY
    FROM #t_fullpay WHERE YEAR(FDate)=YEAR(@QDate) AND MONTH(FDate)=MONTH(@QDate) AND DAY(FDate)=DAY(@QDate)
    UNION ALL
    -- 全款当月
    SELECT FOrgId, 
        0 FFullPayAmtD, FOrderAmt FFullPayAmtM, 0 FFullPayAmtY
    FROM #t_fullpay WHERE YEAR(FDate)=YEAR(@QDate) AND MONTH(FDate)=MONTH(@QDate)
    UNION ALL
    -- 全款当年
    SELECT FOrgId, 
        0 FFullPayAmtD, 0 FFullPayAmtM, FOrderAmt FFullPayAmtY
    FROM #t_fullpay WHERE 
        (@QBeginDate='' AND @QEndDate='' AND YEAR(FDate)=YEAR(@QDate))
        OR (FDate BETWEEN @QBeginDate AND @QEndDate)
) t GROUP BY FOrgId

SELECT o.FOrgId, o.FNUMBER FOrgNo, ol.FNAME FOrgName,
    r.FFullPayAmtD, r.FFullPayAmtM, r.FFullPayAmtY
FROM #t_result r
INNER JOIN T_ORG_ORGANIZATIONS o ON o.FORGID=r.FORGID
INNER JOIN T_ORG_ORGANIZATIONS_L ol ON o.FORGID=ol.FORGID AND ol.FLocalEID=2052

DROP TABLE #t_fullpay
DROP TABLE #t_result

END
/*
EXEC proc_czly_OrgFullPay @QDate='#FDate#', 
    @QBeginDate='#FBeginDate#', @QEndDate='#FEndDate#', 
    @QOrgNo='#FOrgNo#'
*/