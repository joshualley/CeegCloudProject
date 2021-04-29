/*
营销单位订单销售汇总
*/
ALTER PROC [dbo].[proc_czly_OrgOrderSaleSumm](
    @QDate DATETIME='',
    @QOrgNo VARCHAR(55)=''
)
AS
BEGIN

SET NOCOUNT ON

IF @QDate='' SET @QDate=GETDATE()

-- 全部订单
SELECT o.FDate, d.FUseOrgId FOrgId, oe.FOrderAmt, ofi.FRecConditionId
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

-- 全款订单
SELECT * 
INTO #t_fullpay
FROM #t_order WHERE dbo.Fun_IsFullPay(FRecConditionId)=1

-- 出库(销售)
SELECT os.FDate, d.FUseOrgId FOrgId, ose.FRealQty*oef.FTaxPrice FDelvAmt
INTO #t_sale
FROM T_SAL_OUTSTOCK os
INNER JOIN T_SAL_OUTSTOCKENTRY_R osr ON os.FID=osr.FID
INNER JOIN T_SAL_OUTSTOCKENTRY ose ON osr.FEntryId=ose.FEntryId
INNER JOIN T_SAL_ORDERENTRY_F oef ON oef.FEntryID=osr.FSOENTRYID
INNER JOIN T_SAL_ORDERENTRY oe ON oe.FENTRYID=oef.FENTRYID
INNER JOIN T_SAL_ORDER o ON o.FID=oe.FID
INNER JOIN V_BD_SALESMAN sm ON sm.FID=o.FSalerID
INNER JOIN T_BD_DEPARTMENT d ON dbo.fun_czty_GetWorkDeptID(sm.FDEPTID)=d.FDEPTID
INNER JOIN T_ORG_ORGANIZATIONS org ON org.FOrgID=d.FUseOrgId
WHERE org.FNUMBER LIKE '%'+ @QOrgNo +'%'
AND oe.F_ORA_JJYY=''


SELECT FOrgId,
    SUM(FOrderAmtD) FOrderAmtD, SUM(FOrderAmtM) FOrderAmtM, SUM(FOrderAmtY) FOrderAmtY, 
    SUM(FFullPayAmtD) FFullPayAmtD, SUM(FFullPayAmtM) FFullPayAmtM, SUM(FFullPayAmtY) FFullPayAmtY, 
    SUM(FSaleAmtD) FSaleAmtD, SUM(FSaleAmtM) FSaleAmtM, SUM(FSaleAmtY) FSaleAmtY
INTO #t_result
FROM (
    -- 订单当日
    SELECT FOrgId, 
        FOrderAmt FOrderAmtD, 0 FOrderAmtM, 0 FOrderAmtY, 
        0 FFullPayAmtD, 0 FFullPayAmtM, 0 FFullPayAmtY, 
        0 FSaleAmtD, 0 FSaleAmtM, 0 FSaleAmtY 
    FROM #t_order WHERE YEAR(FDate)=YEAR(@QDate) AND MONTH(FDate)=MONTH(@QDate) AND DAY(FDate)=DAY(@QDate)
    UNION ALL
    -- 订单当月
    SELECT FOrgId, 
        0 FOrderAmtD, FOrderAmt FOrderAmtM, 0 FOrderAmtY, 
        0 FFullPayAmtD, 0 FFullPayAmtM, 0 FFullPayAmtY, 
        0 FSaleAmtD, 0 FSaleAmtM, 0 FSaleAmtY 
    FROM #t_order WHERE YEAR(FDate)=YEAR(@QDate) AND MONTH(FDate)=MONTH(@QDate)
    UNION ALL
    -- 订单当年
    SELECT FOrgId, 
        0 FOrderAmtD, 0 FOrderAmtM, FOrderAmt FOrderAmtY, 
        0 FFullPayAmtD, 0 FFullPayAmtM, 0 FFullPayAmtY, 
        0 FSaleAmtD, 0 FSaleAmtM, 0 FSaleAmtY 
    FROM #t_order WHERE YEAR(FDate)=YEAR(@QDate)
    UNION ALL
    -- 全款当日
    SELECT FOrgId, 
        0 FOrderAmtD, 0 FOrderAmtM, 0 FOrderAmtY, 
        FOrderAmt FFullPayAmtD, 0 FFullPayAmtM, 0 FFullPayAmtY, 
        0 FSaleAmtD, 0 FSaleAmtM, 0 FSaleAmtY 
    FROM #t_fullpay WHERE YEAR(FDate)=YEAR(@QDate) AND MONTH(FDate)=MONTH(@QDate) AND DAY(FDate)=DAY(@QDate)
    UNION ALL
    -- 全款当月
    SELECT FOrgId, 
        0 FOrderAmtD, 0 FOrderAmtM, 0 FOrderAmtY, 
        0 FFullPayAmtD, FOrderAmt FFullPayAmtM, 0 FFullPayAmtY, 
        0 FSaleAmtD, 0 FSaleAmtM, 0 FSaleAmtY 
    FROM #t_fullpay WHERE YEAR(FDate)=YEAR(@QDate) AND MONTH(FDate)=MONTH(@QDate)
    UNION ALL
    -- 全款当年
    SELECT FOrgId, 
        0 FOrderAmtD, 0 FOrderAmtM, 0 FOrderAmtY, 
        0 FFullPayAmtD, 0 FFullPayAmtM, FOrderAmt FFullPayAmtY, 
        0 FSaleAmtD, 0 FSaleAmtM, 0 FSaleAmtY 
    FROM #t_fullpay WHERE YEAR(FDate)=YEAR(@QDate)
    UNION ALL
    -- 销售当日
    SELECT FOrgId, 
        0 FOrderAmtD, 0 FOrderAmtM, 0 FOrderAmtY, 
        0 FFullPayAmtD, 0 FFullPayAmtM, 0 FFullPayAmtY, 
        FDelvAmt FSaleAmtD, 0 FSaleAmtM, 0 FSaleAmtY 
    FROM #t_sale WHERE YEAR(FDate)=YEAR(@QDate) AND MONTH(FDate)=MONTH(@QDate) AND DAY(FDate)=DAY(@QDate)
    UNION ALL
    -- 销售当日
    SELECT FOrgId, 
        0 FOrderAmtD, 0 FOrderAmtM, 0 FOrderAmtY, 
        0 FFullPayAmtD, 0 FFullPayAmtM, 0 FFullPayAmtY, 
        0 FSaleAmtD, FDelvAmt FSaleAmtM, 0 FSaleAmtY 
    FROM #t_sale WHERE YEAR(FDate)=YEAR(@QDate) AND MONTH(FDate)=MONTH(@QDate)
    UNION ALL
    -- 销售当日
    SELECT FOrgId, 
        0 FOrderAmtD, 0 FOrderAmtM, 0 FOrderAmtY, 
        0 FFullPayAmtD, 0 FFullPayAmtM, 0 FFullPayAmtY, 
        0 FSaleAmtD, 0 FSaleAmtM, FDelvAmt FSaleAmtY 
    FROM #t_sale WHERE YEAR(FDate)=YEAR(@QDate)
) t
GROUP BY FOrgId

SELECT o.FName FOrgName, r.* FROM #t_result r
INNER JOIN T_ORG_ORGANIZATIONS_L o ON o.FLOCALEID=2052 AND o.FORGID=r.FOrgId

DROP TABLE #t_sale
DROP TABLE #t_fullpay
DROP TABLE #t_order
DROP TABLE #t_result

END


/*
EXEC proc_czly_OrgOrderSaleSumm @QDate='2020-11-16', @QOrgNo=''

EXEC proc_czly_OrgOrderSaleSumm @QDate='#FDate#', @QOrgNo='#FOrgNo#'
*/
