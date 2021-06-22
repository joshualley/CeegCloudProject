/*
日订单销售汇总
Created: 2020-10-04
Author: 刘跃
*/
ALTER PROC [dbo].[proc_czly_DailyOrderSale](
    @QDate DATETIME='',
    @QBeginDate DATETIME='',
    @QEndDate DATETIME='',
    @QProdType VARCHAR(100)='',
    @QVoltageLevel VARCHAR(100)=''
)
AS
BEGIN

SET NOCOUNT ON

-- DECLARE @QDate DATETIME='',
--     @QProdType VARCHAR(100)='',
--     @QVoltageLevel VARCHAR(100)=''

DECLARE @QYear INT=0,
        @QMonth INT=0,
        @QDay INT=0

IF @QDate=''
BEGIN
    SET @QDate=GETDATE()
END

SET @QYear=YEAR(@QDate)
SET @QMonth=MONTH(@QDate)
SET @QDay=DAY(@QDate)

SELECT ROW_NUMBER() OVER(ORDER BY ae.FENTRYID) FSeq, ae.FENTRYID, ael.FDATAVALUE 
INTO #product_type
FROM T_BAS_ASSISTANTDATA_L al
INNER JOIN T_BAS_ASSISTANTDATAENTRY ae ON ae.FID=al.FID
INNER JOIN T_BAS_ASSISTANTDATAENTRY_L ael ON ael.FENTRYID=ae.FENTRYID
WHERE al.FNAME='产品大类'
AND ael.FDATAVALUE LIKE '%'+ @QProdType +'%'



SELECT ae.FENTRYID, ael.FDATAVALUE 
INTO #capacity
FROM T_BAS_ASSISTANTDATA_L al
INNER JOIN T_BAS_ASSISTANTDATAENTRY ae ON ae.FID=al.FID
INNER JOIN T_BAS_ASSISTANTDATAENTRY_L ael ON ael.FENTRYID=ae.FENTRYID
WHERE al.FNAME='容量'
AND ael.FDATAVALUE LIKE '%'+ @QVoltageLevel +'%'

-- 订单金额
SELECT ISNULL(mpt.FENTRYID, '') FProdType, ISNULL(mct.FENTRYID, '') FVoltageLevel, oef.FAllAmount FOrderRowAmt, o.FDate
INTO #order_amt
FROM T_SAL_ORDER o
INNER JOIN T_SAL_ORDERENTRY oe ON o.FID=oe.FID
INNER JOIN T_SAL_ORDERENTRY_F oef ON oe.FENTRYID=oef.FENTRYID
INNER JOIN T_BD_Material m ON oe.FMATERIALID=m.FMATERIALID
LEFT JOIN #product_type mpt ON m.F_ora_Assistant=mpt.FENTRYID
LEFT JOIN #capacity mct ON m.F_ora_Assistant1=mct.FENTRYID
WHERE ISNULL(mpt.FDATAVALUE, '') LIKE '%'+ @QProdType +'%'
AND ISNULL(mct.FDATAVALUE, '') LIKE '%'+ @QVoltageLevel +'%'
AND oe.F_ORA_JJYY=''

-- select SUM(FOrderRowAmt) from #order_amt where FDate BETWEEN '2020-1-1' AND '2020-12-31'
-- drop table #order_amt

--发货（销售）金额
SELECT ISNULL(mpt.FENTRYID, '') FProdType, ISNULL(mct.FENTRYID, '') FVoltageLevel, ose.FRealQty*oef.FTaxPrice FDelvAmt, os.FDate
INTO #sale_amt
FROM T_SAL_OUTSTOCK os
INNER JOIN T_SAL_OUTSTOCKENTRY_R osr ON os.FID=osr.FID
INNER JOIN T_SAL_OUTSTOCKENTRY ose ON osr.FEntryId=ose.FEntryId
INNER JOIN T_SAL_ORDERENTRY_F oef ON oef.FEntryID=osr.FSOENTRYID
INNER JOIN T_SAL_ORDERENTRY oe ON oe.FENTRYID=oef.FENTRYID
INNER JOIN T_BD_Material m ON oe.FMATERIALID=m.FMATERIALID
LEFT JOIN #product_type mpt ON m.F_ora_Assistant=mpt.FENTRYID
LEFT JOIN #capacity mct ON m.F_ora_Assistant1=mct.FENTRYID
WHERE ISNULL(mpt.FDATAVALUE, '') LIKE '%'+ @QProdType +'%'
AND ISNULL(mct.FDATAVALUE, '') LIKE '%'+ @QVoltageLevel +'%'
AND oe.F_ORA_JJYY=''

-- 返回结果
CREATE TABLE #daily_order_data(
    FProdType VARCHAR(100),
    FVoltageLevel VARCHAR(100),
    FOrderToday DECIMAL(18,2),
    FOrderMonth DECIMAL(18,2),
    FOrderYear DECIMAL(18,2),
    FSaleToday DECIMAL(18,2),
    FSaleMonth DECIMAL(18,2),
    FSaleYear DECIMAL(18,2)
)


DECLARE @i INT=0, 
        @count INT=(SELECT COUNT(*) FROM #product_type),
        @FProdType VARCHAR(100),
        @FProdTypeName VARCHAR(100),
        @FVoltageLevel VARCHAR(100),
        @FVoltageLevelName VARCHAR(100),
        @FOrderToday DECIMAL(18,2),
        @FOrderMonth DECIMAL(18,2),
        @FOrderYear DECIMAL(18,2),
        @FSaleToday DECIMAL(18,2),
        @FSaleMonth DECIMAL(18,2),
        @FSaleYear DECIMAL(18,2)
WHILE @i <= @count
BEGIN 
    SET @i += 1
    SELECT @FProdType=FENTRYID, @FProdTypeName=FDATAVALUE FROM #product_type WHERE FSeq=@i
    -- 获取此大类下的电压等级
    SELECT DISTINCT F_ora_Assistant1 FVoltageLevel INTO #lv
    FROM T_BD_Material WHERE F_ora_Assistant=@FProdType
    AND F_ora_Assistant1 IN (SELECT FENTRYID FROM #capacity)
    ORDER BY F_ora_Assistant1

    WHILE EXISTS(SELECT * FROM #lv)
    BEGIN
        SELECT TOP 1 @FVoltageLevel=FVoltageLevel FROM #lv
        DELETE FROM #lv WHERE FVoltageLevel=@FVoltageLevel
        -- 计算订单情况
        SELECT @FVoltageLevelName=FDATAVALUE FROM #capacity WHERE FENTRYID=@FVoltageLevel
        -- 今日订单
        SELECT @FOrderToday=ISNULL(SUM(FOrderRowAmt),0) FROM #order_amt 
        WHERE YEAR(FDate)=@QYear AND MONTH(FDate)=@QMonth AND DAY(FDate)=@QDay AND FProdType=@FProdType AND FVoltageLevel=@FVoltageLevel
        -- 本月订单
        SELECT @FOrderMonth=ISNULL(SUM(FOrderRowAmt),0) FROM #order_amt 
        WHERE YEAR(FDate)=@QYear AND MONTH(FDate)=@QMonth AND FProdType=@FProdType AND FVoltageLevel=@FVoltageLevel
        -- 本年订单
        SELECT @FOrderYear=ISNULL(SUM(FOrderRowAmt),0) FROM #order_amt 
        WHERE FDate BETWEEN @QBeginDate AND @QEndDate AND FProdType=@FProdType AND FVoltageLevel=@FVoltageLevel
        
        -- 计算销售情况
        -- 今日销售
        SELECT @FSaleToday=ISNULL(SUM(FDelvAmt),0) FROM #sale_amt 
        WHERE YEAR(FDate)=@QYear AND MONTH(FDate)=@QMonth AND DAY(FDate)=@QDay AND FProdType=@FProdType AND FVoltageLevel=@FVoltageLevel
        -- 本月销售
        SELECT @FSaleMonth=ISNULL(SUM(FDelvAmt),0) FROM #sale_amt 
        WHERE YEAR(FDate)=@QYear AND MONTH(FDate)=@QMonth AND FProdType=@FProdType AND FVoltageLevel=@FVoltageLevel
        -- 本年销售
        SELECT @FSaleYear=ISNULL(SUM(FDelvAmt),0) FROM #sale_amt 
        WHERE FDate BETWEEN @QBeginDate AND @QEndDate AND FProdType=@FProdType AND FVoltageLevel=@FVoltageLevel
        --插入一行数据
        INSERT INTO #daily_order_data 
        VALUES(@FProdTypeName, @FVoltageLevelName, @FOrderToday, @FOrderMonth, @FOrderYear, @FSaleToday, @FSaleMonth, @FSaleYear)
        
    END
    DROP TABLE #lv
    IF @QVoltageLevel='' --OR '其他分类' LIKE '%'+ @QVoltageLevel + '%'
    BEGIN
        -- 计算此大类中未分电压等级的
        -- 今日订单
        SELECT @FOrderToday=ISNULL(SUM(FOrderRowAmt),0) FROM #order_amt 
        WHERE YEAR(FDate)=@QYear AND MONTH(FDate)=@QMonth AND DAY(FDate)=@QDay AND FProdType=@FProdType AND FVoltageLevel=''
        -- 本月订单
        SELECT @FOrderMonth=ISNULL(SUM(FOrderRowAmt),0) FROM #order_amt 
        WHERE YEAR(FDate)=@QYear AND MONTH(FDate)=@QMonth AND FProdType=@FProdType AND FVoltageLevel=''
        -- 本年订单
        SELECT @FOrderYear=ISNULL(SUM(FOrderRowAmt),0) FROM #order_amt 
        WHERE FDate BETWEEN @QBeginDate AND @QEndDate AND FProdType=@FProdType AND FVoltageLevel=''
        
        -- 计算销售情况
        -- 今日销售
        SELECT @FSaleToday=ISNULL(SUM(FDelvAmt),0) FROM #sale_amt 
        WHERE YEAR(FDate)=@QYear AND MONTH(FDate)=@QMonth AND DAY(FDate)=@QDay AND FProdType=@FProdType AND FVoltageLevel=''
        -- 本月销售
        SELECT @FSaleMonth=ISNULL(SUM(FDelvAmt),0) FROM #sale_amt 
        WHERE YEAR(FDate)=@QYear AND MONTH(FDate)=@QMonth AND FProdType=@FProdType AND FVoltageLevel=''
        -- 本年销售
        SELECT @FSaleYear=ISNULL(SUM(FDelvAmt),0) FROM #sale_amt 
        WHERE FDate BETWEEN @QBeginDate AND @QEndDate AND FProdType=@FProdType AND FVoltageLevel=''
        --插入一行数据
        INSERT INTO #daily_order_data 
        VALUES(@FProdTypeName, '其他分类', @FOrderToday, @FOrderMonth, @FOrderYear, @FSaleToday, @FSaleMonth, @FSaleYear)
    END
END

IF @QVoltageLevel='' --OR '其他大类' LIKE '%'+ @QProdType + '%'
BEGIN
    -- 计算此大类及电压等级皆为空的
    SELECT @FOrderToday=ISNULL(SUM(FOrderRowAmt),0) FROM #order_amt 
    WHERE YEAR(FDate)=@QYear AND MONTH(FDate)=@QMonth AND DAY(FDate)=@QDay AND FProdType='' AND FVoltageLevel=''
    -- 本月订单
    SELECT @FOrderMonth=ISNULL(SUM(FOrderRowAmt),0) FROM #order_amt 
    WHERE YEAR(FDate)=@QYear AND MONTH(FDate)=@QMonth AND FProdType='' AND FVoltageLevel=''
    -- 本年订单
    SELECT @FOrderYear=ISNULL(SUM(FOrderRowAmt),0) FROM #order_amt 
    WHERE FDate BETWEEN @QBeginDate AND @QEndDate AND FProdType='' AND FVoltageLevel=''

    -- 计算销售情况
    -- 今日销售
    SELECT @FSaleToday=ISNULL(SUM(FDelvAmt),0) FROM #sale_amt 
    WHERE YEAR(FDate)=@QYear AND MONTH(FDate)=@QMonth AND DAY(FDate)=@QDay AND FProdType='' AND FVoltageLevel=''
    -- 本月销售
    SELECT @FSaleMonth=ISNULL(SUM(FDelvAmt),0) FROM #sale_amt 
    WHERE YEAR(FDate)=@QYear AND MONTH(FDate)=@QMonth AND FProdType='' AND FVoltageLevel=''
    -- 本年销售
    SELECT @FSaleYear=ISNULL(SUM(FDelvAmt),0) FROM #sale_amt 
    WHERE FDate BETWEEN @QBeginDate AND @QEndDate AND FProdType='' AND FVoltageLevel=''
    --插入一行数据
    INSERT INTO #daily_order_data 
    VALUES('其他大类', '其他分类', @FOrderToday, @FOrderMonth, @FOrderYear, @FSaleToday, @FSaleMonth, @FSaleYear)
END


SELECT * FROM #daily_order_data

DROP TABLE #product_type
DROP TABLE #capacity
DROP TABLE #order_amt
DROP TABLE #sale_amt
DROP TABLE #daily_order_data

END

/*
EXEC proc_czly_DailyOrderSale


EXEC proc_czly_DailyOrderSale @QDate='#FDate#',
    @QBeginDate='#FBeginDate#', @QEndDate='#FEndDate#',
    @QProdType='#FProdType#',@QVoltageLevel='#FVoltageLevel#'
*/