/*
各产品订单价格分析
*/
ALTER PROC [dbo].[proc_czly_ProdOrderPrice](
    @QDate DATETIME=''
    ,@QOrgNo VARCHAR(100)=''
    ,@QProdType VARCHAR(100)=''
    ,@QVoltageLevel VARCHAR(100)=''
) AS
BEGIN

SET NOCOUNT ON

-- DECLARE @QDate DATETIME=''
--        ,@QOrgNo VARCHAR(100)=''
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

-- 订单金额
SELECT org.FOrgID, orgl.FName FOrgName, ISNULL(mpt.FENTRYID, '') FProdType, ISNULL(mct.FENTRYID, '') FVoltageLevel, oef.FAllAmount FOrderRowAmt, o.FDate,
    CASE WHEN CONVERT(DECIMAL(18,6),oe.FBDownPoints)>10 THEN '基价90%以下'
         WHEN CONVERT(DECIMAL(18,6),oe.FBDownPoints)>5 AND CONVERT(DECIMAL(18,6),oe.FBDownPoints)<=10 THEN '基价90%-95%'
         WHEN CONVERT(DECIMAL(18,6),oe.FBDownPoints)>0 AND CONVERT(DECIMAL(18,6),oe.FBDownPoints)<=5 THEN '基价95%-100%'
         WHEN CONVERT(DECIMAL(18,6),oe.FBDownPoints)=0 THEN '基价100%'
         WHEN CONVERT(DECIMAL(18,6),oe.FBDownPoints)>-10 AND CONVERT(DECIMAL(18,6),oe.FBDownPoints)<0 THEN '基价100%-110%'
         WHEN CONVERT(DECIMAL(18,6),oe.FBDownPoints)<=-10 THEN '基价110%以上'
    END FPriceRange
INTO #t_order
FROM T_SAL_ORDER o
INNER JOIN T_SAL_ORDERENTRY oe ON o.FID=oe.FID
INNER JOIN T_SAL_ORDERENTRY_F oef ON oe.FENTRYID=oef.FENTRYID
INNER JOIN V_BD_SALESMAN sm ON sm.FID=o.FSalerID
INNER JOIN T_BD_DEPARTMENT d ON dbo.fun_czty_GetWorkDeptID(sm.FDEPTID)=d.FDEPTID
INNER JOIN T_ORG_ORGANIZATIONS org ON org.FOrgID=d.FUseOrgId
INNER JOIN T_ORG_ORGANIZATIONS_L orgl ON orgl.FOrgID=org.FOrgID AND orgl.FLOCALEID=2052
INNER JOIN T_BD_Material m ON oe.FMATERIALID=m.FMATERIALID
LEFT JOIN #product_type mpt ON m.F_ora_Assistant=mpt.FENTRYID
LEFT JOIN #capacity mct ON m.F_ora_Assistant1=mct.FENTRYID
WHERE ISNULL(mpt.FDATAVALUE, '') LIKE '%'+ @QProdType +'%'
AND ISNULL(mct.FDATAVALUE, '') LIKE '%'+ @QVoltageLevel +'%'
AND org.FNUMBER LIKE '%'+ @QOrgNo +'%'
AND oe.F_ORA_JJYY=''

-- 营销单位集合
SELECT DISTINCT FOrgID, FOrgName INTO #org FROM #t_order

-- 返回结果
CREATE TABLE #t_result(
    FOrgName VARCHAR(100),
    FProdType VARCHAR(100),
    FVoltageLevel VARCHAR(100),
    F110U_D DECIMAL(18, 6),
    F110U_M DECIMAL(18, 6),
    F110U_Y DECIMAL(18, 6),
    F100_110_D DECIMAL(18, 6),
    F100_110_M DECIMAL(18, 6),
    F100_110_Y DECIMAL(18, 6),
    F100_D DECIMAL(18, 6),
    F100_M DECIMAL(18, 6),
    F100_Y DECIMAL(18, 6),
    F95_100_D DECIMAL(18, 6),
    F95_100_M DECIMAL(18, 6),
    F95_100_Y DECIMAL(18, 6),
    F90_95_D DECIMAL(18, 6),
    F90_95_M DECIMAL(18, 6),
    F90_95_Y DECIMAL(18, 6),
    F90D_D DECIMAL(18, 6),
    F90D_M DECIMAL(18, 6),
    F90D_Y DECIMAL(18, 6),
)


DECLARE @i INT=0, 
        @count INT=(SELECT COUNT(*) FROM #product_type),
        @FOrgId INT,
        @FOrgName VARCHAR(100),
        @FProdType VARCHAR(100),
        @FProdTypeName VARCHAR(100),
        @FVoltageLevel VARCHAR(100),
        @FVoltageLevelName VARCHAR(100)

WHILE EXISTS(SELECT * FROM #org)
BEGIN
    -- 取出组织
    SELECT TOP 1 @FOrgId=FOrgId, @FOrgName=FOrgName FROM #org
    DELETE FROM #org WHERE FOrgId=@FOrgId
    -- 遍历产品大类
    SET @i=0
    WHILE @i <= @count
    BEGIN 
        SET @i += 1
        SELECT @FProdType=FENTRYID, @FProdTypeName=FDATAVALUE FROM #product_type WHERE FSeq=@i
        -- 获取此大类下的电压等级
        SELECT DISTINCT F_ora_Assistant1 FVoltageLevel INTO #lv
        FROM T_BD_Material WHERE F_ora_Assistant=@FProdType
        AND F_ora_Assistant1 IN (SELECT FENTRYID FROM #capacity)
        ORDER BY F_ora_Assistant1
        
        -- 遍历电压等级
        WHILE EXISTS(SELECT * FROM #lv)
        BEGIN
            SELECT TOP 1 @FVoltageLevel=FVoltageLevel FROM #lv
            DELETE FROM #lv WHERE FVoltageLevel=@FVoltageLevel
            -- 计算订单情况
            SELECT @FVoltageLevelName=FDATAVALUE FROM #capacity WHERE FENTRYID=@FVoltageLevel
            INSERT INTO #t_result
            SELECT @FOrgName, @FProdTypeName, @FVoltageLevelName,
                    SUM(F110U_D), SUM(F110U_M), SUM(F110U_Y),
                    SUM(F100_110_D), SUM(F100_110_M), SUM(F100_110_Y),
                    SUM(F100_D), SUM(F100_M), SUM(F100_Y),
                    SUM(F95_100_D), SUM(F95_100_M), SUM(F95_100_Y),
                    SUM(F90_95_D), SUM(F90_95_M), SUM(F90_95_Y),
                    SUM(F90D_D), SUM(F90D_M), SUM(F90D_Y)
            FROM (
                -- 当日
                SELECT @FVoltageLevel FVoltageLevel, 
                    FOrderRowAmt F110U_D, 0 F110U_M, 0 F110U_Y,
                    0 F100_110_D, 0 F100_110_M, 0 F100_110_Y,
                    0 F100_D, 0 F100_M, 0 F100_Y,
                    0 F95_100_D, 0 F95_100_M, 0 F95_100_Y,
                    0 F90_95_D, 0 F90_95_M, 0 F90_95_Y,
                    0 F90D_D, 0 F90D_M, 0 F90D_Y
                FROM #t_order
                WHERE FProdType=@FProdType AND FVoltageLevel=@FVoltageLevel AND FOrgId=@FOrgId
                AND FPriceRange='基价110%以上'
                AND YEAR(FDate)=@year AND MONTH(FDate)=@month AND DAY(FDate)=@day
                UNION ALL
                -- 当月
                SELECT @FVoltageLevel FVoltageLevel, 
                    0 F110U_D, FOrderRowAmt F110U_M, 0 F110U_Y,
                    0 F100_110_D, 0 F100_110_M, 0 F100_110_Y,
                    0 F100_D, 0 F100_M, 0 F100_Y,
                    0 F95_100_D, 0 F95_100_M, 0 F95_100_Y,
                    0 F90_95_D, 0 F90_95_M, 0 F90_95_Y,
                    0 F90D_D, 0 F90D_M, 0 F90D_Y
                FROM #t_order
                WHERE FProdType=@FProdType AND FVoltageLevel=@FVoltageLevel AND FOrgId=@FOrgId
                AND FPriceRange='基价110%以上'
                AND YEAR(FDate)=@year AND MONTH(FDate)=@month 
                UNION ALL
                -- 当年
                SELECT @FVoltageLevel FVoltageLevel, 
                    0 F110U_D, 0 F110U_M, FOrderRowAmt F110U_Y,
                    0 F100_110_D, 0 F100_110_M, 0 F100_110_Y,
                    0 F100_D, 0 F100_M, 0 F100_Y,
                    0 F95_100_D, 0 F95_100_M, 0 F95_100_Y,
                    0 F90_95_D, 0 F90_95_M, 0 F90_95_Y,
                    0 F90D_D, 0 F90D_M, 0 F90D_Y
                FROM #t_order
                WHERE FProdType=@FProdType AND FVoltageLevel=@FVoltageLevel AND FOrgId=@FOrgId
                AND FPriceRange='基价110%以上'
                AND YEAR(FDate)=@year
                UNION ALL
                ---------------------------------------- 基价100%-110% ---------------------------------------
                -- 当日
                SELECT @FVoltageLevel FVoltageLevel, 
                    0 F110U_D, 0 F110U_M, 0 F110U_Y,
                    FOrderRowAmt F100_110_D, 0 F100_110_M, 0 F100_110_Y,
                    0 F100_D, 0 F100_M, 0 F100_Y,
                    0 F95_100_D, 0 F95_100_M, 0 F95_100_Y,
                    0 F90_95_D, 0 F90_95_M, 0 F90_95_Y,
                    0 F90D_D, 0 F90D_M, 0 F90D_Y
                FROM #t_order
                WHERE FProdType=@FProdType AND FVoltageLevel=@FVoltageLevel AND FOrgId=@FOrgId
                AND FPriceRange='基价100%-110%'
                AND YEAR(FDate)=@year AND MONTH(FDate)=@month AND DAY(FDate)=@day
                UNION ALL
                -- 当月
                SELECT @FVoltageLevel FVoltageLevel, 
                    0 F110U_D, 0 F110U_M, 0 F110U_Y,
                    0 F100_110_D, FOrderRowAmt F100_110_M, 0 F100_110_Y,
                    0 F100_D, 0 F100_M, 0 F100_Y,
                    0 F95_100_D, 0 F95_100_M, 0 F95_100_Y,
                    0 F90_95_D, 0 F90_95_M, 0 F90_95_Y,
                    0 F90D_D, 0 F90D_M, 0 F90D_Y
                FROM #t_order
                WHERE FProdType=@FProdType AND FVoltageLevel=@FVoltageLevel AND FOrgId=@FOrgId
                AND FPriceRange='基价100%-110%'
                AND YEAR(FDate)=@year AND MONTH(FDate)=@month 
                UNION ALL
                -- 当年
                SELECT @FVoltageLevel FVoltageLevel, 
                    0 F110U_D, 0 F110U_M, 0 F110U_Y,
                    0 F100_110_D, 0 F100_110_M, FOrderRowAmt F100_110_Y,
                    0 F100_D, 0 F100_M, 0 F100_Y,
                    0 F95_100_D, 0 F95_100_M, 0 F95_100_Y,
                    0 F90_95_D, 0 F90_95_M, 0 F90_95_Y,
                    0 F90D_D, 0 F90D_M, 0 F90D_Y
                FROM #t_order
                WHERE FProdType=@FProdType AND FVoltageLevel=@FVoltageLevel AND FOrgId=@FOrgId
                AND FPriceRange='基价100%-110%'
                AND YEAR(FDate)=@year
                UNION ALL
                ---------------------------------------- 基价100% ---------------------------------------
                -- 当日
                SELECT @FVoltageLevel FVoltageLevel, 
                    0 F110U_D, 0 F110U_M, 0 F110U_Y,
                    0 F100_110_D, 0 F100_110_M, 0 F100_110_Y,
                    FOrderRowAmt F100_D, 0 F100_M, 0 F100_Y,
                    0 F95_100_D, 0 F95_100_M, 0 F95_100_Y,
                    0 F90_95_D, 0 F90_95_M, 0 F90_95_Y,
                    0 F90D_D, 0 F90D_M, 0 F90D_Y
                FROM #t_order
                WHERE FProdType=@FProdType AND FVoltageLevel=@FVoltageLevel AND FOrgId=@FOrgId
                AND FPriceRange='基价100%'
                AND YEAR(FDate)=@year AND MONTH(FDate)=@month AND DAY(FDate)=@day
                UNION ALL
                -- 当月
                SELECT @FVoltageLevel FVoltageLevel, 
                    0 F110U_D, 0 F110U_M, 0 F110U_Y,
                    0 F100_110_D, 0 F100_110_M, 0 F100_110_Y,
                    0 F100_D, FOrderRowAmt F100_M, 0 F100_Y,
                    0 F95_100_D, 0 F95_100_M, 0 F95_100_Y,
                    0 F90_95_D, 0 F90_95_M, 0 F90_95_Y,
                    0 F90D_D, 0 F90D_M, 0 F90D_Y
                FROM #t_order
                WHERE FProdType=@FProdType AND FVoltageLevel=@FVoltageLevel AND FOrgId=@FOrgId
                AND FPriceRange='基价100%'
                AND YEAR(FDate)=@year AND MONTH(FDate)=@month 
                UNION ALL
                -- 当年
                SELECT @FVoltageLevel FVoltageLevel, 
                    0 F110U_D, 0 F110U_M, 0 F110U_Y,
                    0 F100_110_D, 0 F100_110_M, 0 F100_110_Y,
                    0 F100_D, 0 F100_M, FOrderRowAmt F100_Y,
                    0 F95_100_D, 0 F95_100_M, 0 F95_100_Y,
                    0 F90_95_D, 0 F90_95_M, 0 F90_95_Y,
                    0 F90D_D, 0 F90D_M, 0 F90D_Y
                FROM #t_order
                WHERE FProdType=@FProdType AND FVoltageLevel=@FVoltageLevel AND FOrgId=@FOrgId
                AND FPriceRange='基价100%'
                AND YEAR(FDate)=@year
                UNION ALL
                ---------------------------------------- 基价95%-100% ---------------------------------------
                -- 当日
                SELECT @FVoltageLevel FVoltageLevel, 
                    0 F110U_D, 0 F110U_M, 0 F110U_Y,
                    0 F100_110_D, 0 F100_110_M, 0 F100_110_Y,
                    0 F100_D, 0 F100_M, 0 F100_Y,
                    FOrderRowAmt F95_100_D, 0 F95_100_M, 0 F95_100_Y,
                    0 F90_95_D, 0 F90_95_M, 0 F90_95_Y,
                    0 F90D_D, 0 F90D_M, 0 F90D_Y
                FROM #t_order
                WHERE FProdType=@FProdType AND FVoltageLevel=@FVoltageLevel AND FOrgId=@FOrgId
                AND FPriceRange='基价95%-100%'
                AND YEAR(FDate)=@year AND MONTH(FDate)=@month AND DAY(FDate)=@day
                UNION ALL
                -- 当月
                SELECT @FVoltageLevel FVoltageLevel, 
                    0 F110U_D, 0 F110U_M, 0 F110U_Y,
                    0 F100_110_D, 0 F100_110_M, 0 F100_110_Y,
                    0 F100_D, 0 F100_M, 0 F100_Y,
                    0 F95_100_D, FOrderRowAmt F95_100_M, 0 F95_100_Y,
                    0 F90_95_D, 0 F90_95_M, 0 F90_95_Y,
                    0 F90D_D, 0 F90D_M, 0 F90D_Y
                FROM #t_order
                WHERE FProdType=@FProdType AND FVoltageLevel=@FVoltageLevel AND FOrgId=@FOrgId
                AND FPriceRange='基价95%-100%'
                AND YEAR(FDate)=@year AND MONTH(FDate)=@month 
                UNION ALL
                -- 当年
                SELECT @FVoltageLevel FVoltageLevel, 
                    0 F110U_D, 0 F110U_M, 0 F110U_Y,
                    0 F100_110_D, 0 F100_110_M, 0 F100_110_Y,
                    0 F100_D, 0 F100_M, 0 F100_Y,
                    0 F95_100_D, 0 F95_100_M, FOrderRowAmt F95_100_Y,
                    0 F90_95_D, 0 F90_95_M, 0 F90_95_Y,
                    0 F90D_D, 0 F90D_M, 0 F90D_Y
                FROM #t_order
                WHERE FProdType=@FProdType AND FVoltageLevel=@FVoltageLevel AND FOrgId=@FOrgId
                AND FPriceRange='基价95%-100%'
                AND YEAR(FDate)=@year
                UNION ALL
                ---------------------------------------- 基价90%-95% ---------------------------------------
                -- 当日
                SELECT @FVoltageLevel FVoltageLevel, 
                    0 F110U_D, 0 F110U_M, 0 F110U_Y,
                    0 F100_110_D, 0 F100_110_M, 0 F100_110_Y,
                    0 F100_D, 0 F100_M, 0 F100_Y,
                    0 F95_100_D, 0 F95_100_M, 0 F95_100_Y,
                    FOrderRowAmt F90_95_D, 0 F90_95_M, 0 F90_95_Y,
                    0 F90D_D, 0 F90D_M, 0 F90D_Y
                FROM #t_order
                WHERE FProdType=@FProdType AND FVoltageLevel=@FVoltageLevel AND FOrgId=@FOrgId
                AND FPriceRange='基价90%-95%'
                AND YEAR(FDate)=@year AND MONTH(FDate)=@month AND DAY(FDate)=@day
                UNION ALL
                -- 当月
                SELECT @FVoltageLevel FVoltageLevel, 
                    0 F110U_D, 0 F110U_M, 0 F110U_Y,
                    0 F100_110_D, 0 F100_110_M, 0 F100_110_Y,
                    0 F100_D, 0 F100_M, 0 F100_Y,
                    0 F95_100_D, 0 F95_100_M, 0 F95_100_Y,
                    0 F90_95_D, FOrderRowAmt F90_95_M, 0 F90_95_Y,
                    0 F90D_D, 0 F90D_M, 0 F90D_Y
                FROM #t_order
                WHERE FProdType=@FProdType AND FVoltageLevel=@FVoltageLevel AND FOrgId=@FOrgId
                AND FPriceRange='基价90%-95%'
                AND YEAR(FDate)=@year AND MONTH(FDate)=@month 
                UNION ALL
                -- 当年
                SELECT @FVoltageLevel FVoltageLevel, 
                    0 F110U_D, 0 F110U_M, 0 F110U_Y,
                    0 F100_110_D, 0 F100_110_M, 0 F100_110_Y,
                    0 F100_D, 0 F100_M, 0 F100_Y,
                    0 F95_100_D, 0 F95_100_M, 0 F95_100_Y,
                    0 F90_95_D, 0 F90_95_M, FOrderRowAmt F90_95_Y,
                    0 F90D_D, 0 F90D_M, 0 F90D_Y
                FROM #t_order
                WHERE FProdType=@FProdType AND FVoltageLevel=@FVoltageLevel AND FOrgId=@FOrgId
                AND FPriceRange='基价90%-95%'
                AND YEAR(FDate)=@year
                UNION ALL
                ---------------------------------------- 基价90%以下 ---------------------------------------
                -- 当日
                SELECT @FVoltageLevel FVoltageLevel, 
                    0 F110U_D, 0 F110U_M, 0 F110U_Y,
                    0 F100_110_D, 0 F100_110_M, 0 F100_110_Y,
                    0 F100_D, 0 F100_M, 0 F100_Y,
                    0 F95_100_D, 0 F95_100_M, 0 F95_100_Y,
                    0 F90_95_D, 0 F90_95_M, 0 F90_95_Y,
                    FOrderRowAmt F90D_D, 0 F90D_M, 0 F90D_Y
                FROM #t_order
                WHERE FProdType=@FProdType AND FVoltageLevel=@FVoltageLevel AND FOrgId=@FOrgId
                AND FPriceRange='基价90%以下'
                AND YEAR(FDate)=@year AND MONTH(FDate)=@month AND DAY(FDate)=@day
                UNION ALL
                -- 当月
                SELECT @FVoltageLevel FVoltageLevel, 
                    0 F110U_D, 0 F110U_M, 0 F110U_Y,
                    0 F100_110_D, 0 F100_110_M, 0 F100_110_Y,
                    0 F100_D, 0 F100_M, 0 F100_Y,
                    0 F95_100_D, 0 F95_100_M, 0 F95_100_Y,
                    0 F90_95_D, 0 F90_95_M, 0 F90_95_Y,
                    0 F90D_D, FOrderRowAmt F90D_M, 0 F90D_Y
                FROM #t_order
                WHERE FProdType=@FProdType AND FVoltageLevel=@FVoltageLevel AND FOrgId=@FOrgId
                AND FPriceRange='基价90%以下'
                AND YEAR(FDate)=@year AND MONTH(FDate)=@month 
                UNION ALL
                -- 当年
                SELECT @FVoltageLevel FVoltageLevel, 
                    0 F110U_D, 0 F110U_M, 0 F110U_Y,
                    0 F100_110_D, 0 F100_110_M, 0 F100_110_Y,
                    0 F100_D, 0 F100_M, 0 F100_Y,
                    0 F95_100_D, 0 F95_100_M, 0 F95_100_Y,
                    0 F90_95_D, 0 F90_95_M, 0 F90_95_Y,
                    0 F90D_D, 0 F90D_M, FOrderRowAmt F90D_Y
                FROM #t_order
                WHERE FProdType=@FProdType AND FVoltageLevel=@FVoltageLevel AND FOrgId=@FOrgId
                AND FPriceRange='基价90%以下'
                AND YEAR(FDate)=@year
            ) t GROUP BY FVoltageLevel
        END
        DROP TABLE #lv
    END
END


SELECT * FROM #t_result

DROP TABLE #product_type
DROP TABLE #capacity
DROP TABLE #t_result
DROP TABLE #t_order
DROP TABLE #org

END

/*
EXEC proc_czly_ProdOrderPrice @QDate='#FDate#',@QOrgNo='#FOrgNo#',
       @QProdType='#FProdType#',@QVoltageLevel='#FVoltageLevel#'
*/