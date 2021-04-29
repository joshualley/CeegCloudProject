/*
Title:    销售费用报表
Author:   刘跃
Created:  2020-07-29
Modified: 2020-07-30
Info:
	对于凭证每行的费用类别汇总
*/
CREATE PROC [dbo].[proc_czly_SaleCostReport](
    @SDt DATETIME,
    @EDt DATETIME
)
AS
BEGIN

--DECLARE @SDt DATETIME='2020-01-01'
--DECLARE @EDt DATETIME='2020-07-30'

SET NOCOUNT ON

IF @SDt = '' SET @SDt = GETDATE()
IF @EDt = '' SET @EDt = GETDATE()

SELECT FID, FNAME INTO #ctype FROM ora_t_CostType_L ORDER BY FID
DECLARE @sql NVARCHAR(MAX)='CREATE TABLE #results('  + CHAR(10)
SET @sql += '成本中心 VARCHAR(255), ' + CHAR(10)
SET @sql += '合计 DECIMAL(18, 2), ' + CHAR(10)
-- 表的字段名
DECLARE @fields VARCHAR(1000)='成本中心, 合计'

DECLARE @typeid BIGINT
DECLARE @typename VARCHAR(255)=''

DECLARE @count INT=0
SELECT @count=COUNT(*) FROM #ctype
IF @count > 0 SET @fields+=', '
-- 动态生成表
DECLARE @iter INT=1
WHILE @iter <= @count
BEGIN
    SELECT TOP 1 @typeid=FID,@typename=FNAME FROM #ctype
    
	SET @fields+=@typename + ', '
    SET @sql+=@typename + ' DECIMAL(18, 2), ' + CHAR(10)
    
    DELETE FROM #ctype WHERE FID=@typeid
    SET @iter+=1
END
SET @sql += '未明 DECIMAL(18, 2) ' + CHAR(10) + ');' + CHAR(10)
SET @fields+='未明'
-- 建表结束 --
DROP TABLE #ctype
-- 遍历组织，即成本中心
SELECT FACCTORGID FID, ol.FNAME INTO #org
FROM T_GL_VOUCHER v
INNER JOIN T_ORG_ORGANIZATIONS_L ol ON v.FACCTORGID=ol.FORGID
WHERE FLOCALEID=2052

DECLARE @orgid BIGINT
DECLARE @orgname VARCHAR(255)=''
WHILE EXISTS(SELECT FID FROM #org)
BEGIN
    SELECT TOP 1 @orgid=FID,@orgname=FNAME FROM #org
    --给成本中心、合计赋值
    DECLARE @sum DECIMAL(18, 2)=0
    SELECT @sum=ISNULL(SUM(FAMOUNTFOR),0) FROM T_GL_VOUCHERENTRY ve
    INNER JOIN T_GL_VOUCHER v ON v.FVOUCHERID=ve.FVOUCHERID AND FACCTORGID=@orgid
    WHERE v.FBUSDATE BETWEEN @SDt AND @EDt --日期区间
    SET @sql += 'INSERT INTO #results(' + @fields + ') VALUES(''' + @orgname + ''', '''
    SET @sql +=  CONVERT(VARCHAR(55), @sum) + ''', '
    --遍历费用类型
    SELECT FID, FNAME INTO #ctype1 FROM ora_t_CostType_L ORDER BY FID
    SET @iter=1
    WHILE @iter <= @count
    BEGIN
        SELECT TOP 1 @typeid=FID,@typename=FNAME FROM #ctype1
        --汇总每一个类别的值
        DECLARE @type_sum DECIMAL(18, 2)=0
        SELECT @type_sum=ISNULL(SUM(FAMOUNTFOR),0) FROM T_GL_VOUCHERENTRY ve
        INNER JOIN T_GL_VOUCHER v ON v.FVOUCHERID=ve.FVOUCHERID
        WHERE v.FACCTORGID=@orgid AND ve.F_ORA_COSTTYPE=@typeid
            AND v.FBUSDATE BETWEEN @SDt AND @EDt --日期区间
		
		SET @sql+='''' + CONVERT(VARCHAR(55), @type_sum) + ''', '

        SET @iter+=1
        DELETE FROM #ctype1 WHERE FID=@typeid
    END
	-- 未明分类
	DECLARE @unsure_sum DECIMAL(18, 2)=0
    SELECT @unsure_sum=ISNULL(SUM(FAMOUNTFOR),0) FROM T_GL_VOUCHERENTRY ve
    INNER JOIN T_GL_VOUCHER v ON v.FVOUCHERID=ve.FVOUCHERID
    WHERE v.FACCTORGID=@orgid AND ve.F_ORA_COSTTYPE=0
        AND v.FBUSDATE BETWEEN @SDt AND @EDt --日期区间
	SET @sql+='''' + CONVERT(VARCHAR(55), @unsure_sum) + ''');' + CHAR(10)

    DROP TABLE #ctype1
    DELETE FROM #org WHERE FID=@orgid
END
DROP TABLE #org
SET @sql += 'SELECT * FROM #results' + CHAR(10)
SET @sql += 'DROP TABLE #results' + CHAR(10)

--print(@sql)
EXECUTE (@sql);

END

-- EXEC proc_czly_SaleCostReport @SDt='2020-01-01', @EDt='2020-07-30'
