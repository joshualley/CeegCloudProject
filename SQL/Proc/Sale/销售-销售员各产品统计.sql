/*
销售员各产品统计
*/
ALTER PROC [dbo].[proc_czly_SellerProd](
    @QDate DATETIME='', 
    @QBeginDate DATETIME='',
    @QEndDate DATETIME='',
    @QSaleNo VARCHAR(55)='', 
    @QProdType VARCHAR(55)=''
) AS
BEGIN

SET NOCOUNT ON

 --DECLARE @QSaleNo VARCHAR(55)=''
 --DECLARE @QDate DATETIME=''

IF @QDate='' SET @QDate=GETDATE()

DECLARE @year INT=YEAR(@QDate)
       ,@month INT=MONTH(@QDate)
       ,@day INT=DAY(@QDate)


SELECT ROW_NUMBER() OVER(ORDER BY ae.FENTRYID) FSeq, ae.FENTRYID, 
    REPLACE(REPLACE(ael.FDATAVALUE, '(', ''), ')', '') FDATAVALUE
INTO #product_type
FROM T_BAS_ASSISTANTDATA_L al
INNER JOIN T_BAS_ASSISTANTDATAENTRY ae ON ae.FID=al.FID
INNER JOIN T_BAS_ASSISTANTDATAENTRY_L ael ON ael.FENTRYID=ae.FENTRYID
WHERE al.FNAME='产品大类'
--AND ael.FDATAVALUE LIKE '%'+ @QProdType + '%'



-- 全部订单
SELECT o.FDate, sm.FNumber FSaleNo, mpt.FENTRYID FProdType, oef.FAllAmount FOrderRowAmt
INTO #t_order
FROM T_SAL_ORDER o
INNER JOIN T_SAL_ORDERENTRY oe ON o.FID=oe.FID
INNER JOIN T_SAL_ORDERENTRY_F oef ON oe.FENTRYID=oef.FENTRYID
INNER JOIN V_BD_SALESMAN sm ON sm.FID=o.FSalerID
INNER JOIN T_BD_Material m ON oe.FMATERIALID=m.FMATERIALID
LEFT JOIN #product_type mpt ON m.F_ora_Assistant=mpt.FENTRYID
WHERE sm.FNUMBER LIKE '%'+ @QSaleNo +'%'
AND oe.F_ORA_JJYY=''

-- 出库(销售)
SELECT os.FDate, sm.FNumber FSaleNo, mpt.FENTRYID FProdType, ose.FRealQty*oef.FTaxPrice FDelvAmt
INTO #t_sale
FROM T_SAL_OUTSTOCK os
INNER JOIN T_SAL_OUTSTOCKENTRY_R osr ON os.FID=osr.FID
INNER JOIN T_SAL_OUTSTOCKENTRY ose ON osr.FEntryId=ose.FEntryId
INNER JOIN T_SAL_ORDERENTRY_F oef ON oef.FEntryID=osr.FSOENTRYID
INNER JOIN T_SAL_ORDERENTRY oe ON oe.FENTRYID=oef.FENTRYID
INNER JOIN T_SAL_ORDER o ON o.FID=oe.FID
INNER JOIN V_BD_SALESMAN sm ON sm.FID=o.FSalerID
INNER JOIN T_BD_Material m ON oe.FMATERIALID=m.FMATERIALID
LEFT JOIN #product_type mpt ON m.F_ora_Assistant=mpt.FENTRYID
WHERE sm.FNUMBER LIKE '%'+ @QSaleNo +'%'
AND oe.F_ORA_JJYY=''


-- 定义循环时需要的变量
DECLARE @i INT
       ,@j INT
       ,@len INT = (SELECT COUNT(*) FROM #product_type)+1
       ,@prod_type_id VARCHAR(55)
       ,@prod_type_name VARCHAR(55)

insert into #product_type values(@len, '', '其他')

DECLARE @sql VARCHAR(MAX)=''
       ,@sum_fields VARCHAR(MAX)='FSaleNo,'

SET @j = 1
WHILE @j <= @len
BEGIN 
    SELECT @prod_type_name=FDATAVALUE FROM #product_type WHERE FSeq=@j
    SET @j += 1
    SET @sum_fields += 'SUM(订单_当日_'+@prod_type_name+') 订单_当日_'+@prod_type_name+','
    SET @sum_fields += 'SUM(订单_当月_'+@prod_type_name+') 订单_当月_'+@prod_type_name+','
    SET @sum_fields += 'SUM(订单_当年_'+@prod_type_name+') 订单_当年_'+@prod_type_name+',' + CHAR(10)
END

SET @sum_fields += 'SUM(订单汇总_当日) 订单汇总_当日,SUM(订单汇总_当月) 订单汇总_当月,SUM(订单汇总_当年) 订单汇总_当年,' + CHAR(10)

SET @j = 1
WHILE @j <= @len
BEGIN 
    SELECT @prod_type_name=FDATAVALUE FROM #product_type WHERE FSeq=@j
    SET @j += 1
    SET @sum_fields += 'SUM(销售_当日_'+@prod_type_name+') 销售_当日_'+@prod_type_name+','
    SET @sum_fields += 'SUM(销售_当月_'+@prod_type_name+') 销售_当月_'+@prod_type_name+','
    SET @sum_fields += 'SUM(销售_当年_'+@prod_type_name+') 销售_当年_'+@prod_type_name+',' + CHAR(10)
END

SET @sum_fields += 'SUM(销售汇总_当日) 销售汇总_当日,SUM(销售汇总_当月) 销售汇总_当月,SUM(销售汇总_当年) 销售汇总_当年' + CHAR(10)

SET @sql += 'SELECT ' + @sum_fields + ' INTO #t_result FROM (' + CHAR(10)

--PRINT(@sql)

DECLARE @fields_d VARCHAR(MAX)=''
DECLARE @fields_m VARCHAR(MAX)=''
DECLARE @fields_y VARCHAR(MAX)=''
DECLARE @one_type VARCHAR(MAX)=''

-----------------------------------------------  循环2次产品大类，生成对应的列名 -----------------------------------------------------
SET @i = 0
WHILE @i < @len*2
BEGIN
    SET @i += 1
    -- 获取产品大类ID
    SELECT @prod_type_id=FENTRYID FROM #product_type WHERE FSeq=CASE WHEN @i<=@len THEN @i ELSE @i-@len END
    
    --------------------------------- BEGIN 拼接列名 BEGIN ------------------------------------
    SELECT @fields_d='FSaleNo,',@fields_m='FSaleNo,',@fields_y='FSaleNo,'
    -- 订单
    SET @j = 0
    WHILE @j < @len
    BEGIN 
		SET @j += 1
        SELECT @prod_type_name=FDATAVALUE FROM #product_type WHERE FSeq=@j
        SET @fields_d += CASE WHEN @i = @j THEN 'FOrderRowAmt' ELSE '0' END + ' 订单_当日_' + @prod_type_name 
			+ ',0 订单_当月_' + @prod_type_name 
			+ ',0 订单_当年_' + @prod_type_name + ','
        SET @fields_m += '0 订单_当日_' + @prod_type_name + ',' 
			+ CASE WHEN @i = @j THEN 'FOrderRowAmt' ELSE '0' END + ' 订单_当月_' + @prod_type_name 
			+ ',0 订单_当年_' + @prod_type_name + ','
        SET @fields_y += '0 订单_当日_' + @prod_type_name 
			+ ',0 订单_当月_' + @prod_type_name + ',' 
			+ CASE WHEN @i = @j THEN 'FOrderRowAmt' ELSE '0' END + ' 订单_当年_' + @prod_type_name + ','
    END
    -- 订单汇总
    SET @fields_d += '0 订单汇总_当日,0 订单汇总_当月,0 订单汇总_当年,'
    SET @fields_m += '0 订单汇总_当日,0 订单汇总_当月,0 订单汇总_当年,'
    SET @fields_y += '0 订单汇总_当日,0 订单汇总_当月,0 订单汇总_当年,'
	
    -- 销售
    SET @j = 0
    WHILE @j < @len
    BEGIN 
		SET @j += 1
        SELECT @prod_type_name=FDATAVALUE FROM #product_type WHERE FSeq=@j
        SET @fields_d += CASE WHEN @i = @j+@len THEN 'FDelvAmt' ELSE '0' END + ' 销售_当日_' + @prod_type_name 
			+ ',0 销售_当月_' + @prod_type_name 
			+ ',0 销售_当年_' + @prod_type_name + ','
        SET @fields_m += '0 销售_当日_' + @prod_type_name + ',' 
			+ CASE WHEN @i = @j+@len THEN 'FDelvAmt' ELSE '0' END + ' 销售_当月_' + @prod_type_name 
			+ ',0 销售_当年_' + @prod_type_name + ','
        SET @fields_y += '0 销售_当日_' + @prod_type_name 
			+ ',0 销售_当月_' + @prod_type_name + ',' 
			+ CASE WHEN @i = @j+@len THEN 'FDelvAmt' ELSE '0' END + ' 销售_当年_' + @prod_type_name + ','
    END
    -- 销售汇总
    SET @fields_d += '0 销售汇总_当日,0 销售汇总_当月,0 销售汇总_当年'
    SET @fields_m += '0 销售汇总_当日,0 销售汇总_当月,0 销售汇总_当年'
    SET @fields_y += '0 销售汇总_当日,0 销售汇总_当月,0 销售汇总_当年'
    --------------------------------- END 拼接列名 END ------------------------------------
    
    --------------------------------- BEGIN 一个产品大类(包括当日、当月、当年) BEGIN ------------------------------------
	SET @one_type = ''
    IF @i <= @len -- 第一轮遍历产品大类生成订单相关 SELECT-UNION 块，第二轮遍历产品大类生成销售相关 SELECT-UNION 块
    BEGIN
        ---------------------------  订单  -------------------------------
        -- 当日
        SET @one_type += 'SELECT ' + @fields_d + CHAR(10)
            + ' FROM #t_order WHERE FProdType=''' + @prod_type_id 
            + ''' AND YEAR(FDate)=' + CONVERT(VARCHAR(10), @year) 
            + ' AND MONTH(FDate)=' + CONVERT(VARCHAR(10), @month) + ' AND DAY(FDate)=' + CONVERT(VARCHAR(10), @day) + CHAR(10)
            + 'UNION ALL' + CHAR(10)
        -- 当月
        SET @one_type += 'SELECT ' + @fields_m + CHAR(10)
            + ' FROM #t_order WHERE FProdType=''' + @prod_type_id 
            + ''' AND YEAR(FDate)=' + CONVERT(VARCHAR(10), @year) 
            + ' AND MONTH(FDate)=' + CONVERT(VARCHAR(10), @month) + CHAR(10)
            + 'UNION ALL' + CHAR(10)
        -- 当年
        SET @one_type += 'SELECT ' + @fields_y + CHAR(10)
            + ' FROM #t_order WHERE FProdType=''' + @prod_type_id 
            + ''' AND FDate BETWEEN ''' + CONVERT(VARCHAR(20), @QBeginDate) 
            + ''' AND ''' + CONVERT(VARCHAR(20), @QEndDate) + ''' ' + CHAR(10)
            + 'UNION ALL' + CHAR(10)
    END 
    ELSE
    BEGIN 
        ---------------------------  销售  -------------------------------
        -- 当日
        SET @one_type += 'SELECT ' + @fields_d + CHAR(10)
            + ' FROM #t_sale WHERE FProdType=''' + @prod_type_id 
            + ''' AND YEAR(FDate)=' + CONVERT(VARCHAR(10), @year) 
            + ' AND MONTH(FDate)=' + CONVERT(VARCHAR(10), @month) + ' AND DAY(FDate)=' + CONVERT(VARCHAR(10), @day) + CHAR(10)
            + 'UNION ALL' + CHAR(10)
        -- 当月
        SET @one_type += 'SELECT ' + @fields_m + CHAR(10)
            + ' FROM #t_sale WHERE FProdType=''' + @prod_type_id 
            + ''' AND YEAR(FDate)=' + CONVERT(VARCHAR(10), @year) 
            + ' AND MONTH(FDate)=' + CONVERT(VARCHAR(10), @month) + CHAR(10)
            + 'UNION ALL' + CHAR(10)
        -- 当年
        SET @one_type += 'SELECT ' + @fields_y + CHAR(10)
            + ' FROM #t_sale WHERE FProdType=''' + @prod_type_id 
            + ''' AND FDate BETWEEN ''' + CONVERT(VARCHAR(20), @QBeginDate) 
            + ''' AND ''' + CONVERT(VARCHAR(20), @QEndDate) + ''' ' + CHAR(10)
            + 'UNION ALL' + CHAR(10)
    END
    --------------------------------- END 一个产品大类(包括当日、当月、当年) END ------------------------------------
	SET @sql += @one_type + CHAR(10) 

	--  PRINT(@one_type)
END

DECLARE @fmt VARCHAR(255)=''
------------------------------- 订单汇总 ---------------------------------------
SET @fmt = ''
SELECT @fields_d='FSaleNo,',@fields_m='FSaleNo,',@fields_y='FSaleNo,'
-- 拼接订单相关列名
SET @j = 1
WHILE @j <= @len
BEGIN 
    SELECT @prod_type_name=FDATAVALUE FROM #product_type WHERE FSeq=@j
    SET @j += 1
    SET @fmt = '0 订单_当日_'+@prod_type_name+',0 订单_当月_'+@prod_type_name+',0 订单_当年_'+@prod_type_name+','
    
    SET @fields_d += @fmt
    SET @fields_m += @fmt
    SET @fields_y += @fmt
END

SET @fields_d += 'FOrderRowAmt 订单汇总_当日,0 订单汇总_当月,0 订单汇总_当年,'
SET @fields_m += '0 订单汇总_当日,FOrderRowAmt 订单汇总_当月,0 订单汇总_当年,'
SET @fields_y += '0 订单汇总_当日,0 订单汇总_当月,FOrderRowAmt 订单汇总_当年,'

-- 拼接销售相关列名
SET @j = 1
WHILE @j <= @len
BEGIN 
    SELECT @prod_type_name=FDATAVALUE FROM #product_type WHERE FSeq=@j
    SET @j += 1
    SET @fmt = '0 销售_当日_'+@prod_type_name+',0 销售_当月_'+@prod_type_name+',0 销售_当年_'+@prod_type_name+','
    
    SET @fields_d += @fmt
    SET @fields_m += @fmt
    SET @fields_y += @fmt
END

SET @fields_d += '0 销售汇总_当日,0 销售汇总_当月,0 销售汇总_当年'
SET @fields_m += '0 销售汇总_当日,0 销售汇总_当月,0 销售汇总_当年'
SET @fields_y += '0 销售汇总_当日,0 销售汇总_当月,0 销售汇总_当年'

-- 生成订单汇总 SELECT 块
SET @one_type = ''
-- 当日
SET @one_type += 'SELECT ' + @fields_d + CHAR(10)
    + ' FROM #t_order WHERE YEAR(FDate)=' + CONVERT(VARCHAR(10), @year) 
    + ' AND MONTH(FDate)=' + CONVERT(VARCHAR(10), @month) + ' AND DAY(FDate)=' + CONVERT(VARCHAR(10), @day) + CHAR(10)
    + 'UNION ALL' + CHAR(10)
-- 当月
SET @one_type += 'SELECT ' + @fields_m + CHAR(10)
    + ' FROM #t_order WHERE YEAR(FDate)=' + CONVERT(VARCHAR(10), @year) 
    + ' AND MONTH(FDate)=' + CONVERT(VARCHAR(10), @month) + CHAR(10)
    + 'UNION ALL' + CHAR(10)
-- 当年
SET @one_type += 'SELECT ' + @fields_y + CHAR(10)
    + ' FROM #t_order WHERE FDate BETWEEN ''' + CONVERT(VARCHAR(20), @QBeginDate) 
    + ''' AND ''' + CONVERT(VARCHAR(20), @QEndDate) + ''' ' + CHAR(10)
    + 'UNION ALL' + CHAR(10)
 --PRINT(@one_type)
SET @sql += @one_type
-------------------->>>>>>>>>>>>>>>>>-----销售汇总-----<<<<<<<<<<<<<<<<<---------------
SET @fmt = ''
SELECT @fields_d='FSaleNo,',@fields_m='FSaleNo,',@fields_y='FSaleNo,'
SET @j = 1
WHILE @j <= @len
BEGIN 
    SELECT @prod_type_name=FDATAVALUE FROM #product_type WHERE FSeq=@j
    SET @j += 1
    SET @fmt = '0 订单_当日_'+@prod_type_name+',0 订单_当月_'+@prod_type_name+',0 订单_当年_'+@prod_type_name+','
    
    SET @fields_d += @fmt
    SET @fields_m += @fmt
    SET @fields_y += @fmt
END

SET @fields_d += '0 订单汇总_当日,0 订单汇总_当月,0 订单汇总_当年,'
SET @fields_m += '0 订单汇总_当日,0 订单汇总_当月,0 订单汇总_当年,'
SET @fields_y += '0 订单汇总_当日,0 订单汇总_当月,0 订单汇总_当年,'

SET @j = 1
WHILE @j <= @len
BEGIN 
    SELECT @prod_type_name=FDATAVALUE FROM #product_type WHERE FSeq=@j
    SET @j += 1
    SET @fmt = '0 销售_当日_'+@prod_type_name+',0 销售_当月_'+@prod_type_name+',0 销售_当年_'+@prod_type_name+','
    
    SET @fields_d += @fmt
    SET @fields_m += @fmt
    SET @fields_y += @fmt
END

SET @fields_d += 'FDelvAmt 销售汇总_当日,0 销售汇总_当月,0 销售汇总_当年'
SET @fields_m += '0 销售汇总_当日,FDelvAmt 销售汇总_当月,0 销售汇总_当年'
SET @fields_y += '0 销售汇总_当日,0 销售汇总_当月,FDelvAmt 销售汇总_当年'

-- 生成销售汇总 SELECT 块
SET @one_type = ''
-- 当日
SET @one_type += 'SELECT ' + @fields_d + CHAR(10)
    + ' FROM #t_sale WHERE YEAR(FDate)=' + CONVERT(VARCHAR(10), @year) 
    + ' AND MONTH(FDate)=' + CONVERT(VARCHAR(10), @month) + ' AND DAY(FDate)=' + CONVERT(VARCHAR(10), @day) + CHAR(10)
    + 'UNION ALL' + CHAR(10)
-- 当月
SET @one_type += 'SELECT ' + @fields_m + CHAR(10)
    + ' FROM #t_sale WHERE YEAR(FDate)=' + CONVERT(VARCHAR(10), @year) 
    + ' AND MONTH(FDate)=' + CONVERT(VARCHAR(10), @month) + CHAR(10)
    + 'UNION ALL' + CHAR(10)
-- 当年
SET @one_type += 'SELECT ' + @fields_y + CHAR(10)
    + ' FROM #t_sale WHERE FDate BETWEEN ''' + CONVERT(VARCHAR(20), @QBeginDate) 
    + ''' AND ''' + CONVERT(VARCHAR(20), @QEndDate) + ''' ' + CHAR(10)
    -- + 'UNION ALL' + CHAR(10)
 --PRINT(@one_type)
SET @sql += @one_type
------------------------------------- END -------------------------------------------------

SET @sql += CHAR(10) + ') t GROUP BY FSaleNo' + CHAR(10)
SET @sql += 'SELECT el.FName FSaleName, r.* FROM #t_result r 
INNER JOIN T_HR_EMPINFO e ON e.FNUMBER=r.FSaleNo
INNER JOIN T_HR_EMPINFO_L el ON el.FID=e.FID
DROP TABLE #t_result'

-- SELECT @sql FOR XML PATH('')
EXEC(@sql)



DROP TABLE #t_sale
DROP TABLE #t_order
DROP TABLE #product_type


END
/*
EXEC proc_czly_SellerProd @QDate='#FDate#', 
    @QBeginDate='#FBeginDate#', @QEndDate='#FEndDate#', 
    @QSaleNo='#FSellerNo#', @QProdType=''

EXEC proc_czly_SellerProd @QDate='2020-12-31'

*/