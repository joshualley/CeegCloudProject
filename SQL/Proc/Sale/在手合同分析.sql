/*
在手合同分析
*/
ALTER PROC [dbo].[proc_czly_HoldContractAnaly](
    @QDate DATETIME='',
    @QProdType VARCHAR(100)='',
    @QVoltageLevel VARCHAR(100)=''
) AS
BEGIN

SET NOCOUNT ON

-- DECLARE @QDate DATETIME=''
--        ,@QProdType VARCHAR(100)=''
--        ,@QVoltageLevel VARCHAR(100)=''


IF @QDate='' SET @QDate=GETDATE()

DECLARE @year INT=YEAR(@QDate)
       ,@month INT=MONTH(@QDate)
       ,@day INT=DAY(@QDate)


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


-- 订单
SELECT o.FDate, 
    ISNULL(mpt.FENTRYID, '') FProdType, ISNULL(mct.FENTRYID, '') FVoltageLevel, 
    oef.FAllAmount FOrderRowAmt, oe.FQty
INTO #t_order
FROM T_SAL_ORDER o
INNER JOIN T_SAL_ORDERENTRY oe ON o.FID=oe.FID
INNER JOIN T_SAL_ORDERENTRY_F oef ON oe.FENTRYID=oef.FENTRYID
INNER JOIN T_BD_Material m ON oe.FMATERIALID=m.FMATERIALID
LEFT JOIN #product_type mpt ON m.F_ora_Assistant=mpt.FENTRYID
LEFT JOIN #capacity mct ON m.F_ora_Assistant1=mct.FENTRYID
WHERE o.FDocumentStatus='C'
AND ISNULL(mpt.FDATAVALUE, '') LIKE '%'+ @QProdType +'%'
AND ISNULL(mct.FDATAVALUE, '') LIKE '%'+ @QVoltageLevel +'%'
AND YEAR(o.FDate)=@year AND MONTH(o.FDate)=@month
AND oe.F_ORA_JJYY=''


-- 返回结果
CREATE TABLE #t_result(
    FProdType VARCHAR(100),
    FVoltageLevel VARCHAR(100),
    FQty DECIMAL(18, 2) DEFAULT(0),
    FAmt DECIMAL(18, 2) DEFAULT(0)
)

DECLARE @i INT=0, 
        @count INT=(SELECT COUNT(*) FROM #product_type),
        @FProdType VARCHAR(100),
        @FProdTypeName VARCHAR(100),
        @FVoltageLevel VARCHAR(100),
        @FVoltageLevelName VARCHAR(100)

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
        SELECT @FVoltageLevelName=FDATAVALUE FROM #capacity WHERE FENTRYID=@FVoltageLevel

        INSERT INTO #t_result 
        SELECT @FProdTypeName, @FVoltageLevelName, SUM(FQty), SUM(FOrderRowAmt)
        FROM #t_order
        WHERE FProdType=@FProdType AND FVoltageLevel=@FVoltageLevel
    END
    DROP TABLE #lv
END

SELECT * FROM #t_result

DROP TABLE #product_type
DROP TABLE #capacity
DROP TABLE #t_order
DROP TABLE #t_result

END

/*
EXEC proc_czly_HoldContractAnaly @QDate='#FDate#',
    @QProdType='#FProdType#',@QVoltageLevel='#FVoltageLevel#'
*/