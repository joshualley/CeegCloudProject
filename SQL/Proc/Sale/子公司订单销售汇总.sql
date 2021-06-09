/*
子公司订单销售汇总
*/

ALTER PROC [dbo].[proc_czly_CompanyOrderSale](
    @QDate DATETIME='',
    @QBeginDate DATETIME='',
    @QEndDate DATETIME=''
) AS
BEGIN

SET NOCOUNT ON

-- DECLARE @QDate DATETIME=''

IF @QDate='' SET @QDate=GETDATE()


DECLARE @year INT=YEAR(@QDate)
       ,@month INT=MONTH(@QDate)
       ,@day INT=DAY(@QDate)


-- 全部订单
SELECT o.FDate, oe.FSTOCKORGID, oef.FALLAMOUNT FOrderAmt
INTO #t_order
FROM T_SAL_ORDER o
INNER JOIN T_SAL_ORDERENTRY oe ON o.FID=oe.FID
INNER JOIN T_SAL_ORDERENTRY_F oef ON oe.FENTRYID=oef.FENTRYID
WHERE oe.F_ORA_JJYY='' AND o.FSalerID<>0

-- 出库(销售)
SELECT os.FDate, oe.FSTOCKORGID, ose.FRealQty*oef.FTaxPrice FDelvAmt
INTO #t_sale
FROM T_SAL_OUTSTOCK os
INNER JOIN T_SAL_OUTSTOCKENTRY_R osr ON os.FID=osr.FID
INNER JOIN T_SAL_OUTSTOCKENTRY ose ON osr.FEntryId=ose.FEntryId
INNER JOIN T_SAL_ORDERENTRY_F oef ON oef.FEntryID=osr.FSOENTRYID
INNER JOIN T_SAL_ORDERENTRY oe ON oe.FENTRYID=oef.FENTRYID
WHERE oe.F_ORA_JJYY=''
-- INNER JOIN T_SAL_ORDER o ON o.FID=oe.FID


SELECT FSTOCKORGID, 
    SUM(FOrderAmtD) FOrderAmtD, SUM(FOrderAmtM) FOrderAmtM, SUM(FOrderAmtY) FOrderAmtY, 
    SUM(FSaleAmtD) FSaleAmtD, SUM(FSaleAmtM) FSaleAmtM, SUM(FSaleAmtY) FSaleAmtY
INTO #t_result
FROM (
    -- 订单当日
    SELECT FSTOCKORGID, 
        FOrderAmt FOrderAmtD, 0 FOrderAmtM, 0 FOrderAmtY, 
        0 FSaleAmtD, 0 FSaleAmtM, 0 FSaleAmtY
    FROM #t_order WHERE YEAR(FDate)=@year AND MONTH(FDate)=@month AND DAY(FDate)=@day
    UNION ALL
    -- 订单当月
    SELECT FSTOCKORGID, 
        0 FOrderAmtD, FOrderAmt FOrderAmtM, 0 FOrderAmtY, 
        0 FSaleAmtD, 0 FSaleAmtM, 0 FSaleAmtY
    FROM #t_order WHERE YEAR(FDate)=@year AND MONTH(FDate)=@month
    UNION ALL
    -- 订单当年
    SELECT FSTOCKORGID, 
        0 FOrderAmtD, 0 FOrderAmtM, FOrderAmt FOrderAmtY, 
        0 FSaleAmtD, 0 FSaleAmtM, 0 FSaleAmtY
    FROM #t_order WHERE 
        (@QBeginDate='' AND @QEndDate='' AND YEAR(FDate)=@year)
        OR (FDate BETWEEN @QBeginDate AND @QEndDate)
    UNION ALL
    ------------------------------- 销售 ------------------------------------
    -- 销售当日
    SELECT FSTOCKORGID, 
        0 FOrderAmtD, 0 FOrderAmtM, 0 FOrderAmtY, 
        FDelvAmt FSaleAmtD, 0 FSaleAmtM, 0 FSaleAmtY
    FROM #t_sale WHERE YEAR(FDate)=@year AND MONTH(FDate)=@month AND DAY(FDate)=@day
    UNION ALL
    -- 销售当月
    SELECT FSTOCKORGID, 
        0 FOrderAmtD, 0 FOrderAmtM, 0 FOrderAmtY, 
        0 FSaleAmtD, FDelvAmt FSaleAmtM, 0 FSaleAmtY
    FROM #t_sale WHERE YEAR(FDate)=@year AND MONTH(FDate)=@month
    UNION ALL
    -- 销售当年
    SELECT FSTOCKORGID, 
        0 FOrderAmtD, 0 FOrderAmtM, 0 FOrderAmtY, 
        0 FSaleAmtD, 0 FSaleAmtM, FDelvAmt FSaleAmtY
    FROM #t_sale WHERE 
        (@QBeginDate='' AND @QEndDate='' AND YEAR(FDate)=@year)
        OR (FDate BETWEEN @QBeginDate AND @QEndDate)
) t GROUP BY FSTOCKORGID

SELECT ol.FNAME FOrgName, r.* 
FROM #t_result r
INNER JOIN T_ORG_ORGANIZATIONS_L ol ON ol.FORGID=r.FSTOCKORGID AND ol.FLOCALEID=2052


DROP TABLE #t_sale
DROP TABLE #t_order
DROP TABLE #t_result

END
/*
EXEC proc_czly_CompanyOrderSale @QDate='#FDate#',
    @QBeginDate='#FBeginDate#', @QEndDate='#FEndDate#'
*/