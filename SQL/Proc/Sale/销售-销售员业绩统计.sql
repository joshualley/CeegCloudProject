/*
销售员业绩统计
*/
CREATE PROC [dbo].[proc_czly_SellerPerform](
    @QDate DATETIME='',
    @QSaleNo VARCHAR(55)=''
)
AS
BEGIN

SET NOCOUNT ON

-- DECLARE @QDate DATETIME=''

IF @QDate='' SET @QDate=GETDATE()


DECLARE @year INT=YEAR(@QDate)
       ,@lyear INT=YEAR(@QDate)-1
       ,@month INT=MONTH(@QDate)
       ,@day INT=DAY(@QDate)


-- 全部订单
SELECT o.FDate, sm.FNumber FSaleNo, ofi.FBILLALLAMOUNT FOrderAmt, 
    -- oef.FALLAMOUNT FOrderAmt,
    ofi.FRecConditionId
INTO #t_order
FROM T_SAL_ORDER o
INNER JOIN T_SAL_ORDERFIN ofi ON o.FID=ofi.FID
-- INNER JOIN T_SAL_ORDERENTRY oe ON o.FID=oe.FID
-- INNER JOIN T_SAL_ORDERENTRY_F oef ON oef.FENTRYID=oe.FENTRYID
INNER JOIN V_BD_SALESMAN sm ON sm.FID=o.FSalerID
WHERE sm.FNUMBER LIKE '%'+ @QSaleNo +'%'
-- AND oe.F_ORA_JJYY=''

-- 全款订单
SELECT * 
INTO #t_fullpay
FROM #t_order WHERE dbo.Fun_IsFullPay(FRecConditionId)=1

-- 出库(销售)
SELECT os.FDate, sm.FNumber FSaleNo, ose.FRealQty*oef.FTaxPrice FDelvAmt
INTO #t_sale
FROM T_SAL_OUTSTOCK os
INNER JOIN T_SAL_OUTSTOCKENTRY_R osr ON os.FID=osr.FID
INNER JOIN T_SAL_OUTSTOCKENTRY ose ON osr.FEntryId=ose.FEntryId
INNER JOIN T_SAL_ORDERENTRY_F oef ON oef.FEntryID=osr.FSOENTRYID
INNER JOIN T_SAL_ORDERENTRY oe ON oe.FENTRYID=oef.FENTRYID
INNER JOIN T_SAL_ORDER o ON o.FID=oe.FID
INNER JOIN V_BD_SALESMAN sm ON sm.FID=o.FSalerID
WHERE sm.FNUMBER LIKE '%'+ @QSaleNo +'%'



SELECT FSaleNo,
    SUM(FOrderAmtD) FOrderAmtD, SUM(FOrderAmtM) FOrderAmtM, SUM(FOrderAmtY) FOrderAmtY, 
    SUM(FOrderAmtLD) FOrderAmtLD, SUM(FOrderAmtLM) FOrderAmtLM, 
    SUM(FFullPayAmtD) FFullPayAmtD, SUM(FFullPayAmtM) FFullPayAmtM, SUM(FFullPayAmtY) FFullPayAmtY, 
    SUM(FFullPayAmtLD) FFullPayAmtLD, SUM(FFullPayAmtLM) FFullPayAmtLM, 
    SUM(FSaleAmtD) FSaleAmtD, SUM(FSaleAmtM) FSaleAmtM, SUM(FSaleAmtY) FSaleAmtY,
    SUM(FSaleAmtLD) FSaleAmtLD, SUM(FSaleAmtLM) FSaleAmtLM
INTO #t_result
FROM (
    -- ------>>>>>>>>>>>>>>>--------当年订单----------<<<<<<<<<<<<<<<<<------
    -- 当年 订单当日
    SELECT FSaleNo, 
        FOrderAmt FOrderAmtD, 0 FOrderAmtM, 0 FOrderAmtY, 
        0 FOrderAmtLD, 0 FOrderAmtLM, 
        0 FFullPayAmtD, 0 FFullPayAmtM, 0 FFullPayAmtY, 
        0 FFullPayAmtLD, 0 FFullPayAmtLM, 
        0 FSaleAmtD, 0 FSaleAmtM, 0 FSaleAmtY,
        0 FSaleAmtLD, 0 FSaleAmtLM
    FROM #t_order WHERE YEAR(FDate)=@year AND MONTH(FDate)=@month AND DAY(FDate)=@day
    UNION ALL
    -- 当年 订单当月
    SELECT FSaleNo, 
        0 FOrderAmtD, FOrderAmt FOrderAmtM, 0 FOrderAmtY, 
        0 FOrderAmtLD, 0 FOrderAmtLM, 
        0 FFullPayAmtD, 0 FFullPayAmtM, 0 FFullPayAmtY, 
        0 FFullPayAmtLD, 0 FFullPayAmtLM, 
        0 FSaleAmtD, 0 FSaleAmtM, 0 FSaleAmtY,
        0 FSaleAmtLD, 0 FSaleAmtLM
    FROM #t_order WHERE YEAR(FDate)=@year AND MONTH(FDate)=@month
    UNION ALL
    -- 当年 订单当年
    SELECT FSaleNo, 
        0 FOrderAmtD, 0 FOrderAmtM, FOrderAmt FOrderAmtY, 
        0 FOrderAmtLD, 0 FOrderAmtLM, 
        0 FFullPayAmtD, 0 FFullPayAmtM, 0 FFullPayAmtY, 
        0 FFullPayAmtLD, 0 FFullPayAmtLM, 
        0 FSaleAmtD, 0 FSaleAmtM, 0 FSaleAmtY,
        0 FSaleAmtLD, 0 FSaleAmtLM
    FROM #t_order WHERE YEAR(FDate)=@year
    UNION ALL
    -- ------>>>>>>>>>>>>>>>--------上年订单----------<<<<<<<<<<<<<<<<<------
    -- 上年 订单当日
    SELECT FSaleNo, 
        0 FOrderAmtD, 0 FOrderAmtM, 0 FOrderAmtY, 
        FOrderAmt FOrderAmtLD, 0 FOrderAmtLM, 
        0 FFullPayAmtD, 0 FFullPayAmtM, 0 FFullPayAmtY, 
        0 FFullPayAmtLD, 0 FFullPayAmtLM, 
        0 FSaleAmtD, 0 FSaleAmtM, 0 FSaleAmtY,
        0 FSaleAmtLD, 0 FSaleAmtLM
    FROM #t_order WHERE YEAR(FDate)=@lyear AND MONTH(FDate)=@month AND DAY(FDate)=@day
    UNION ALL
    -- 上年 订单当月
    SELECT FSaleNo, 
        0 FOrderAmtD, 0 FOrderAmtM, 0 FOrderAmtY, 
        0 FOrderAmtLD, FOrderAmt FOrderAmtLM, 
        0 FFullPayAmtD, 0 FFullPayAmtM, 0 FFullPayAmtY, 
        0 FFullPayAmtLD, 0 FFullPayAmtLM, 
        0 FSaleAmtD, 0 FSaleAmtM, 0 FSaleAmtY,
        0 FSaleAmtLD, 0 FSaleAmtLM
    FROM #t_order WHERE YEAR(FDate)=@lyear AND MONTH(FDate)=@month
    UNION ALL
    -- ------>>>>>>>>>>>>>>>--------当年全款订单----------<<<<<<<<<<<<<<<<<------
    -- 当年 全款当日
    SELECT FSaleNo, 
        0 FOrderAmtD, 0 FOrderAmtM, 0 FOrderAmtY, 
        0 FOrderAmtLD, 0 FOrderAmtLM, 
        FOrderAmt FFullPayAmtD, 0 FFullPayAmtM, 0 FFullPayAmtY, 
        0 FFullPayAmtLD, 0 FFullPayAmtLM, 
        0 FSaleAmtD, 0 FSaleAmtM, 0 FSaleAmtY,
        0 FSaleAmtLD, 0 FSaleAmtLM 
    FROM #t_fullpay WHERE YEAR(FDate)=@year AND MONTH(FDate)=@month AND DAY(FDate)=@day
    UNION ALL
    -- 当年 全款当月
    SELECT FSaleNo, 
        0 FOrderAmtD, 0 FOrderAmtM, 0 FOrderAmtY, 
        0 FOrderAmtLD, 0 FOrderAmtLM, 
        0 FFullPayAmtD, FOrderAmt FFullPayAmtM, 0 FFullPayAmtY, 
        0 FFullPayAmtLD, 0 FFullPayAmtLM, 
        0 FSaleAmtD, 0 FSaleAmtM, 0 FSaleAmtY,
        0 FSaleAmtLD, 0 FSaleAmtLM  
    FROM #t_fullpay WHERE YEAR(FDate)=@year AND MONTH(FDate)=@month
    UNION ALL
    -- 当年 全款当年
    SELECT FSaleNo, 
        0 FOrderAmtD, 0 FOrderAmtM, 0 FOrderAmtY, 
        0 FOrderAmtLD, 0 FOrderAmtLM, 
        0 FFullPayAmtD, 0 FFullPayAmtM, FOrderAmt FFullPayAmtY, 
        0 FFullPayAmtLD, 0 FFullPayAmtLM, 
        0 FSaleAmtD, 0 FSaleAmtM, 0 FSaleAmtY,
        0 FSaleAmtLD, 0 FSaleAmtLM  
    FROM #t_fullpay WHERE YEAR(FDate)=@year
    UNION ALL
    -- ------>>>>>>>>>>>>>>>--------上年全款订单----------<<<<<<<<<<<<<<<<<------
    -- 上年 全款当日
    SELECT FSaleNo, 
        0 FOrderAmtD, 0 FOrderAmtM, 0 FOrderAmtY, 
        0 FOrderAmtLD, 0 FOrderAmtLM, 
        0 FFullPayAmtD, 0 FFullPayAmtM, 0 FFullPayAmtY, 
        FOrderAmt FFullPayAmtLD, 0 FFullPayAmtLM, 
        0 FSaleAmtD, 0 FSaleAmtM, 0 FSaleAmtY,
        0 FSaleAmtLD, 0 FSaleAmtLM 
    FROM #t_fullpay WHERE YEAR(FDate)=@lyear AND MONTH(FDate)=@month AND DAY(FDate)=@day
    UNION ALL
    -- 上年 全款当月
    SELECT FSaleNo, 
        0 FOrderAmtD, 0 FOrderAmtM, 0 FOrderAmtY, 
        0 FOrderAmtLD, 0 FOrderAmtLM, 
        0 FFullPayAmtD, 0 FFullPayAmtM, 0 FFullPayAmtY, 
        0 FFullPayAmtLD, FOrderAmt FFullPayAmtLM, 
        0 FSaleAmtD, 0 FSaleAmtM, 0 FSaleAmtY,
        0 FSaleAmtLD, 0 FSaleAmtLM  
    FROM #t_fullpay WHERE YEAR(FDate)=@lyear AND MONTH(FDate)=@month
    UNION ALL
    -- ------>>>>>>>>>>>>>>>--------当年销售----------<<<<<<<<<<<<<<<<<------
    -- 当年 销售当日
    SELECT FSaleNo, 
        0 FOrderAmtD, 0 FOrderAmtM, 0 FOrderAmtY, 
        0 FOrderAmtLD, 0 FOrderAmtLM, 
        0 FFullPayAmtD, 0 FFullPayAmtM, 0 FFullPayAmtY, 
        0 FFullPayAmtLD, 0 FFullPayAmtLM, 
        FDelvAmt FSaleAmtD, 0 FSaleAmtM, 0 FSaleAmtY,
        0 FSaleAmtLD, 0 FSaleAmtLM 
    FROM #t_sale WHERE YEAR(FDate)=@year AND MONTH(FDate)=@month AND DAY(FDate)=@day
    UNION ALL
    -- 当年 销售当月
    SELECT FSaleNo, 
        0 FOrderAmtD, 0 FOrderAmtM, 0 FOrderAmtY, 
        0 FOrderAmtLD, 0 FOrderAmtLM, 
        0 FFullPayAmtD, 0 FFullPayAmtM, 0 FFullPayAmtY, 
        0 FFullPayAmtLD, 0 FFullPayAmtLM, 
        0 FSaleAmtD, FDelvAmt FSaleAmtM, 0 FSaleAmtY,
        0 FSaleAmtLD, 0 FSaleAmtLM 
    FROM #t_sale WHERE YEAR(FDate)=@year AND MONTH(FDate)=@month
    UNION ALL
    -- 当年 销售当年
    SELECT FSaleNo, 
        0 FOrderAmtD, 0 FOrderAmtM, 0 FOrderAmtY, 
        0 FOrderAmtLD, 0 FOrderAmtLM, 
        0 FFullPayAmtD, 0 FFullPayAmtM, 0 FFullPayAmtY, 
        0 FFullPayAmtLD, 0 FFullPayAmtLM, 
        0 FSaleAmtD, 0 FSaleAmtM, FDelvAmt FSaleAmtY,
        0 FSaleAmtLD, 0 FSaleAmtLM 
    FROM #t_sale WHERE YEAR(FDate)=@year
    UNION ALL
    -- ------>>>>>>>>>>>>>>>--------上年销售----------<<<<<<<<<<<<<<<<<------
    -- 上年 销售当日
    SELECT FSaleNo, 
        0 FOrderAmtD, 0 FOrderAmtM, 0 FOrderAmtY, 
        0 FOrderAmtLD, 0 FOrderAmtLM, 
        0 FFullPayAmtD, 0 FFullPayAmtM, 0 FFullPayAmtY, 
        0 FFullPayAmtLD, 0 FFullPayAmtLM, 
        0 FSaleAmtD, 0 FSaleAmtM, 0 FSaleAmtY,
        FDelvAmt FSaleAmtLD, 0 FSaleAmtLM 
    FROM #t_sale WHERE YEAR(FDate)=@lyear AND MONTH(FDate)=@month AND DAY(FDate)=@day
    UNION ALL
    -- 上年 销售当月
    SELECT FSaleNo, 
        0 FOrderAmtD, 0 FOrderAmtM, 0 FOrderAmtY, 
        0 FOrderAmtLD, 0 FOrderAmtLM, 
        0 FFullPayAmtD, 0 FFullPayAmtM, 0 FFullPayAmtY, 
        0 FFullPayAmtLD, 0 FFullPayAmtLM, 
        0 FSaleAmtD, 0 FSaleAmtM, 0 FSaleAmtY,
        0 FSaleAmtLD, FDelvAmt FSaleAmtLM 
    FROM #t_sale WHERE YEAR(FDate)=@lyear AND MONTH(FDate)=@month
) t
GROUP BY FSaleNo


SELECT el.FName FSaleName, 
    FOrderAmtD, FOrderAmtM, FOrderAmtY, 
    FOrderAmtLD, FOrderAmtLM, 
    CASE WHEN FOrderAmtLM=0 THEN 0 ELSE 100*FOrderAmtM/FOrderAmtLM END FOrderRate,
    FFullPayAmtD, FFullPayAmtM, FFullPayAmtY, 
    FFullPayAmtLD, FFullPayAmtLM, 
    CASE WHEN FFullPayAmtLM=0 THEN 0 ELSE 100*FFullPayAmtM/FFullPayAmtLM END FFullPayRate,
    FSaleAmtD, FSaleAmtM, FSaleAmtY,
    FSaleAmtLD, FSaleAmtLM,
    CASE WHEN FSaleAmtLM=0 THEN 0 ELSE 100*FSaleAmtM/FSaleAmtLM END FSaleRate
FROM #t_result r
INNER JOIN T_HR_EMPINFO e ON e.FNUMBER=r.FSaleNo
INNER JOIN T_HR_EMPINFO_L el ON el.FID=e.FID


DROP TABLE #t_sale
DROP TABLE #t_fullpay
DROP TABLE #t_order
DROP TABLE #t_result


END

/*
EXEC proc_czly_SellerPerform @QDate='', @QSaleNo=''

EXEC proc_czly_SellerPerform @QDate='#FDate#', @QSaleNo='#FSellerNo#'
*/