/*
办事处业绩
*/
ALTER PROC [dbo].[proc_czly_DeptPerform] (
    @QDeptNo VARCHAR(100)='',
    @QDate DATETIME='',
    @QBeginDate DATETIME='',
    @QEndDate DATETIME=''
) AS
BEGIN

SET NOCOUNT ON

-- DECLARE @QDeptNo VARCHAR(100)=''
-- DECLARE @QDate DATETIME=''

IF @QDate='' SET @QDate=GETDATE()


DECLARE @year INT=YEAR(@QDate)
       ,@lyear INT=YEAR(@QDate)-1
       ,@month INT=MONTH(@QDate)
       ,@day INT=DAY(@QDate)


-- 全部订单
SELECT o.FDate, d.FNumber FDeptNo, oe.FOrderAmt, ofi.FRecConditionId
INTO #t_order
FROM T_SAL_ORDER o
INNER JOIN (
    SELECT oe.FID, SUM(FALLAMOUNT) FOrderAmt FROM T_SAL_ORDERENTRY oe
    INNER JOIN T_SAL_ORDERENTRY_F oef ON oef.FENTRYID=oe.FENTRYID
    WHERE oe.F_ORA_JJYY=''
    GROUP BY oe.FID
) oe ON oe.FID=o.FID
INNER JOIN T_SAL_ORDERFIN ofi ON o.FID=ofi.FID
INNER JOIN T_BD_DEPARTMENT d ON d.FDEPTID=o.FSALEDEPTID
WHERE d.FNUMBER LIKE '%'+ @QDeptNo +'%'

-- 全款订单
SELECT * 
INTO #t_fullpay
FROM #t_order WHERE dbo.Fun_IsFullPay(FRecConditionId)=1

-- 出库(销售)
SELECT os.FDate, d.FNumber FDeptNo, ose.FRealQty*oef.FTaxPrice FDelvAmt
INTO #t_sale
FROM T_SAL_OUTSTOCK os
INNER JOIN T_SAL_OUTSTOCKENTRY_R osr ON os.FID=osr.FID
INNER JOIN T_SAL_OUTSTOCKENTRY ose ON osr.FEntryId=ose.FEntryId
INNER JOIN T_SAL_ORDERENTRY_F oef ON oef.FEntryID=osr.FSOENTRYID
INNER JOIN T_SAL_ORDERENTRY oe ON oe.FENTRYID=oef.FENTRYID
INNER JOIN T_SAL_ORDER o ON o.FID=oe.FID
INNER JOIN T_BD_DEPARTMENT d ON d.FDEPTID=o.FSALEDEPTID
WHERE d.FNUMBER LIKE '%'+ @QDeptNo +'%'
AND oe.F_ORA_JJYY=''


SELECT FDeptNo,
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
    SELECT FDeptNo, 
        FOrderAmt FOrderAmtD, 0 FOrderAmtM, 0 FOrderAmtY, 
        0 FOrderAmtLD, 0 FOrderAmtLM, 
        0 FFullPayAmtD, 0 FFullPayAmtM, 0 FFullPayAmtY, 
        0 FFullPayAmtLD, 0 FFullPayAmtLM, 
        0 FSaleAmtD, 0 FSaleAmtM, 0 FSaleAmtY,
        0 FSaleAmtLD, 0 FSaleAmtLM
    FROM #t_order WHERE YEAR(FDate)=@year AND MONTH(FDate)=@month AND DAY(FDate)=@day
    UNION ALL
    -- 当年 订单当月
    SELECT FDeptNo, 
        0 FOrderAmtD, FOrderAmt FOrderAmtM, 0 FOrderAmtY, 
        0 FOrderAmtLD, 0 FOrderAmtLM, 
        0 FFullPayAmtD, 0 FFullPayAmtM, 0 FFullPayAmtY, 
        0 FFullPayAmtLD, 0 FFullPayAmtLM, 
        0 FSaleAmtD, 0 FSaleAmtM, 0 FSaleAmtY,
        0 FSaleAmtLD, 0 FSaleAmtLM
    FROM #t_order WHERE YEAR(FDate)=@year AND MONTH(FDate)=@month
    UNION ALL
    -- 当年 订单当年
    SELECT FDeptNo, 
        0 FOrderAmtD, 0 FOrderAmtM, FOrderAmt FOrderAmtY, 
        0 FOrderAmtLD, 0 FOrderAmtLM, 
        0 FFullPayAmtD, 0 FFullPayAmtM, 0 FFullPayAmtY, 
        0 FFullPayAmtLD, 0 FFullPayAmtLM, 
        0 FSaleAmtD, 0 FSaleAmtM, 0 FSaleAmtY,
        0 FSaleAmtLD, 0 FSaleAmtLM
    FROM #t_order WHERE 
        (@QBeginDate='' AND @QEndDate='' AND YEAR(FDate)=@year)
        OR (FDate BETWEEN @QBeginDate AND @QEndDate)
    UNION ALL
    -- ------>>>>>>>>>>>>>>>--------上年订单----------<<<<<<<<<<<<<<<<<------
    -- 上年 订单当日
    SELECT FDeptNo, 
        0 FOrderAmtD, 0 FOrderAmtM, 0 FOrderAmtY, 
        FOrderAmt FOrderAmtLD, 0 FOrderAmtLM, 
        0 FFullPayAmtD, 0 FFullPayAmtM, 0 FFullPayAmtY, 
        0 FFullPayAmtLD, 0 FFullPayAmtLM, 
        0 FSaleAmtD, 0 FSaleAmtM, 0 FSaleAmtY,
        0 FSaleAmtLD, 0 FSaleAmtLM
    FROM #t_order WHERE YEAR(FDate)=@lyear AND MONTH(FDate)=@month AND DAY(FDate)=@day
    UNION ALL
    -- 上年 订单当月
    SELECT FDeptNo, 
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
    SELECT FDeptNo, 
        0 FOrderAmtD, 0 FOrderAmtM, 0 FOrderAmtY, 
        0 FOrderAmtLD, 0 FOrderAmtLM, 
        FOrderAmt FFullPayAmtD, 0 FFullPayAmtM, 0 FFullPayAmtY, 
        0 FFullPayAmtLD, 0 FFullPayAmtLM, 
        0 FSaleAmtD, 0 FSaleAmtM, 0 FSaleAmtY,
        0 FSaleAmtLD, 0 FSaleAmtLM 
    FROM #t_fullpay WHERE YEAR(FDate)=@year AND MONTH(FDate)=@month AND DAY(FDate)=@day
    UNION ALL
    -- 当年 全款当月
    SELECT FDeptNo, 
        0 FOrderAmtD, 0 FOrderAmtM, 0 FOrderAmtY, 
        0 FOrderAmtLD, 0 FOrderAmtLM, 
        0 FFullPayAmtD, FOrderAmt FFullPayAmtM, 0 FFullPayAmtY, 
        0 FFullPayAmtLD, 0 FFullPayAmtLM, 
        0 FSaleAmtD, 0 FSaleAmtM, 0 FSaleAmtY,
        0 FSaleAmtLD, 0 FSaleAmtLM  
    FROM #t_fullpay WHERE YEAR(FDate)=@year AND MONTH(FDate)=@month
    UNION ALL
    -- 当年 全款当年
    SELECT FDeptNo, 
        0 FOrderAmtD, 0 FOrderAmtM, 0 FOrderAmtY, 
        0 FOrderAmtLD, 0 FOrderAmtLM, 
        0 FFullPayAmtD, 0 FFullPayAmtM, FOrderAmt FFullPayAmtY, 
        0 FFullPayAmtLD, 0 FFullPayAmtLM, 
        0 FSaleAmtD, 0 FSaleAmtM, 0 FSaleAmtY,
        0 FSaleAmtLD, 0 FSaleAmtLM  
    FROM #t_fullpay WHERE 
        (@QBeginDate='' AND @QEndDate='' AND YEAR(FDate)=@year)
        OR (FDate BETWEEN @QBeginDate AND @QEndDate)
    UNION ALL
    -- ------>>>>>>>>>>>>>>>--------上年全款订单----------<<<<<<<<<<<<<<<<<------
    -- 上年 全款当日
    SELECT FDeptNo, 
        0 FOrderAmtD, 0 FOrderAmtM, 0 FOrderAmtY, 
        0 FOrderAmtLD, 0 FOrderAmtLM, 
        0 FFullPayAmtD, 0 FFullPayAmtM, 0 FFullPayAmtY, 
        FOrderAmt FFullPayAmtLD, 0 FFullPayAmtLM, 
        0 FSaleAmtD, 0 FSaleAmtM, 0 FSaleAmtY,
        0 FSaleAmtLD, 0 FSaleAmtLM 
    FROM #t_fullpay WHERE YEAR(FDate)=@lyear AND MONTH(FDate)=@month AND DAY(FDate)=@day
    UNION ALL
    -- 上年 全款当月
    SELECT FDeptNo, 
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
    SELECT FDeptNo, 
        0 FOrderAmtD, 0 FOrderAmtM, 0 FOrderAmtY, 
        0 FOrderAmtLD, 0 FOrderAmtLM, 
        0 FFullPayAmtD, 0 FFullPayAmtM, 0 FFullPayAmtY, 
        0 FFullPayAmtLD, 0 FFullPayAmtLM, 
        FDelvAmt FSaleAmtD, 0 FSaleAmtM, 0 FSaleAmtY,
        0 FSaleAmtLD, 0 FSaleAmtLM 
    FROM #t_sale WHERE YEAR(FDate)=@year AND MONTH(FDate)=@month AND DAY(FDate)=@day
    UNION ALL
    -- 当年 销售当月
    SELECT FDeptNo, 
        0 FOrderAmtD, 0 FOrderAmtM, 0 FOrderAmtY, 
        0 FOrderAmtLD, 0 FOrderAmtLM, 
        0 FFullPayAmtD, 0 FFullPayAmtM, 0 FFullPayAmtY, 
        0 FFullPayAmtLD, 0 FFullPayAmtLM, 
        0 FSaleAmtD, FDelvAmt FSaleAmtM, 0 FSaleAmtY,
        0 FSaleAmtLD, 0 FSaleAmtLM 
    FROM #t_sale WHERE YEAR(FDate)=@year AND MONTH(FDate)=@month
    UNION ALL
    -- 当年 销售当年
    SELECT FDeptNo, 
        0 FOrderAmtD, 0 FOrderAmtM, 0 FOrderAmtY, 
        0 FOrderAmtLD, 0 FOrderAmtLM, 
        0 FFullPayAmtD, 0 FFullPayAmtM, 0 FFullPayAmtY, 
        0 FFullPayAmtLD, 0 FFullPayAmtLM, 
        0 FSaleAmtD, 0 FSaleAmtM, FDelvAmt FSaleAmtY,
        0 FSaleAmtLD, 0 FSaleAmtLM 
    FROM #t_sale WHERE 
        (@QBeginDate='' AND @QEndDate='' AND YEAR(FDate)=@year)
        OR (FDate BETWEEN @QBeginDate AND @QEndDate)
    UNION ALL
    -- ------>>>>>>>>>>>>>>>--------上年销售----------<<<<<<<<<<<<<<<<<------
    -- 上年 销售当日
    SELECT FDeptNo, 
        0 FOrderAmtD, 0 FOrderAmtM, 0 FOrderAmtY, 
        0 FOrderAmtLD, 0 FOrderAmtLM, 
        0 FFullPayAmtD, 0 FFullPayAmtM, 0 FFullPayAmtY, 
        0 FFullPayAmtLD, 0 FFullPayAmtLM, 
        0 FSaleAmtD, 0 FSaleAmtM, 0 FSaleAmtY,
        FDelvAmt FSaleAmtLD, 0 FSaleAmtLM 
    FROM #t_sale WHERE YEAR(FDate)=@lyear AND MONTH(FDate)=@month AND DAY(FDate)=@day
    UNION ALL
    -- 上年 销售当月
    SELECT FDeptNo, 
        0 FOrderAmtD, 0 FOrderAmtM, 0 FOrderAmtY, 
        0 FOrderAmtLD, 0 FOrderAmtLM, 
        0 FFullPayAmtD, 0 FFullPayAmtM, 0 FFullPayAmtY, 
        0 FFullPayAmtLD, 0 FFullPayAmtLM, 
        0 FSaleAmtD, 0 FSaleAmtM, 0 FSaleAmtY,
        0 FSaleAmtLD, FDelvAmt FSaleAmtLM 
    FROM #t_sale WHERE YEAR(FDate)=@lyear AND MONTH(FDate)=@month
) t
GROUP BY FDeptNo


SELECT FDeptNo, dl.FName FDeptName, 
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
INNER JOIN T_BD_DEPARTMENT d ON d.FNUMBER=r.FDeptNo
INNER JOIN T_BD_DEPARTMENT_L dl ON dl.FDEPTID=d.FDEPTID


DROP TABLE #t_sale
DROP TABLE #t_fullpay
DROP TABLE #t_order
DROP TABLE #t_result


END
/*
EXEC proc_czly_DeptPerform @QDate='#FDate#', 
    @QBeginDate='#FBeginDate#', @QEndDate='#FEndDate#', 
    @QDeptNo='#FDeptNo#'
*/