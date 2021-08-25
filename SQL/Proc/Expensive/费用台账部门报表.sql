/*
Title:    费用台账部门报表
Author:   刘跃
Created:  2020-08-13
Modified: 2020-08-24
Info:
	对于费用台账每行的费用项目按部门(包含其下的子部门)汇总
*/

ALTER PROC [dbo].[proc_czly_AccountDept](
    @SDt DATETIME,
    @EDt DATETIME,
    @FOrgId BIGINT=0, --冗余字段，提供根据子公司查询
    @FDeptId BIGINT=0,
    @FAccountId BIGINT=0 --科目
)
AS
BEGIN

SET NOCOUNT ON


-- DECLARE @SDt DATETIME='2020-01-01'
-- DECLARE @EDt DATETIME='2020-07-30'
-- DECLARE @FOrgId BIGINT=0
-- DECLARE @FDeptId BIGINT=0
-- DECLARE @FAccountId BIGINT=0

IF @SDt = '' SET @SDt = (SELECT TOP 1 FDate FROM T_GL_VOUCHER ORDER BY FDate ASC)
IF @EDt = '' SET @EDt = GETDATE()


DECLARE @FNumber VARCHAR(20)=(SELECT FNUMBER FROM T_BD_DEPARTMENT WHERE FDEPTID=@FDeptId)
DECLARE @FDeptNumber VARCHAR(30)=@FNumber+'%'

SELECT  v.FDate, ve.FAmountFor FAmt, d.FUSEORGID FOrgID,
fi.FFLEX5 FDeptID, fi.FFLEX9 FOrignCostItem, 
ISNULL(ae.FAccountActual, 0) FCostItem
INTO #temp
FROM T_GL_VOUCHERENTRY ve
inner join T_BD_ACCOUNT acc on acc.FACCTID=ve.FACCOUNTID
INNER JOIN T_GL_VOUCHER v ON v.FVOUCHERID=ve.FVOUCHERID
INNER JOIN T_BD_FLEXITEMDETAILV fi ON fi.FID=ve.FDETAILID
INNER JOIN T_BD_DEPARTMENT d ON fi.FFLEX5=d.FDEPTID
LEFT JOIN ora_t_AccountEntry_LK aek ON ve.FENTRYID=aek.FSId
LEFT JOIN ora_t_AccountEntry ae ON ae.FEntryID=aek.FEntryID
LEFT JOIN ora_t_Account a ON a.FID=ae.FID AND a.FDOCUMENTSTATUS='C'
WHERE v.FDate BETWEEN @SDt AND @EDt --AND v.FDOCUMENTSTATUS='C' 
AND ve.FCREDIT=0 --贷方金额
AND acc.FNUMBER IN ('6601', '6602')
AND (@FAccountId=0 OR ve.FACCOUNTID=@FAccountId)
AND (@FOrgId=0 OR d.FUSEORGID=@FOrgId)
AND (@FDeptId=0 OR d.FNumber LIKE @FDeptNumber)

--SELECT * FROM T_BD_ACCOUNT_L WHERE FNAME IN ('销售费用', '管理费用')
--SELECT * FROM T_BD_ACCOUNT WHERE FACCTID IN (4083, 4084)

-- 未在费用台账中选择实际费用项目的凭证，其费用项目通过核算维度获取
UPDATE #temp SET FCostItem=FOrignCostItem WHERE FCostItem=NULL OR FCostItem=0


SELECT FEXPID,FNAME INTO #T_CostItems FROM T_BD_Expense_L e
INNER JOIN #temp t ON e.FEXPID=t.FCostItem
GROUP BY FEXPID,FNAME ORDER BY FEXPID

DECLARE @sql NVARCHAR(MAX)='CREATE TABLE #results('  + CHAR(10)
--SET @sql += '子公司 VARCHAR(255), ' + CHAR(10)
SET @sql += '部门 VARCHAR(255), ' + CHAR(10)
SET @sql += '合计 DECIMAL(18, 2), ' + CHAR(10)
-- 表的字段名
DECLARE @fields NVARCHAR(MAX)=''
--SET @fields += '子公司, '
SET @fields += '部门, 合计'

DECLARE @typeid BIGINT
DECLARE @typename VARCHAR(255)=''

DECLARE @count INT=0
SELECT @count=COUNT(*) FROM #T_CostItems
IF @count > 0 SET @fields+=', '
-- 动态生成表
DECLARE @iter INT=1
WHILE @iter <= @count
BEGIN
    SELECT TOP 1 @typeid=FEXPID,@typename=FNAME FROM #T_CostItems
    SET @typename=REPLACE(@typename, '-', '')
    SET @typename=REPLACE(@typename, '、', '')
    SET @typename=REPLACE(@typename, '(', '')
    SET @typename=REPLACE(@typename, ')', '')
    
	SET @fields += @typename
    SET @sql += @typename + ' DECIMAL(18, 2)'
    IF @iter < @count
    BEGIN
        SET @fields += ', '
        SET @sql += ', '
    END
    SET @sql += CHAR(10)
    
    DELETE FROM #T_CostItems WHERE FEXPID=@typeid
    SET @iter+=1
END
--SET @sql += '未明 DECIMAL(18, 2) ' + CHAR(10)
SET @sql += ');' + CHAR(10)
--SET @fields += '未明'

DROP TABLE #T_CostItems

--PRINT(@sql)

-- 查询的子公司
--DECLARE @orgname VARCHAR(255)=(SELECT FNAME FROM T_ORG_ORGANIZATIONS_L WHERE FOrgID=@FOrgId AND FLOCALEID=2052)

-- 遍历部门
SELECT DISTINCT t.FDeptID, dl.FNAME INTO #T_Dept FROM #temp t
INNER JOIN T_BD_DEPARTMENT_L dl ON t.FDeptID=dl.FDEPTID AND FLOCALEID=2052

DECLARE @deptid BIGINT
DECLARE @deptname VARCHAR(255)=''
WHILE EXISTS(SELECT FDeptID FROM #T_Dept)
BEGIN
    SELECT TOP 1 @deptid=FDeptID,@deptname=FNAME FROM #T_Dept
    --给子公司、合计赋值
    DECLARE @sum DECIMAL(18, 2)=0
    SELECT @sum=ISNULL(SUM(FAmt),0) FROM #temp WHERE FDeptID=@deptid
    
    SET @sql += 'INSERT INTO #results(' + @fields + ') VALUES('''
    --SET @sql += @orgname + ''', '''
    SET @sql +=  CONVERT(VARCHAR(55), @deptname) + ''', '''
    SET @sql +=  CONVERT(VARCHAR(55), @sum) + ''', '
    --遍历费用项目
    SELECT FEXPID,FNAME INTO #T_CostItems1 FROM T_BD_Expense_L e
    INNER JOIN #temp t ON e.FEXPID=t.FCostItem
    GROUP BY FEXPID,FNAME ORDER BY FEXPID

    SET @iter=1
    WHILE @iter <= @count
    BEGIN
        SELECT TOP 1 @typeid=FEXPID,@typename=FNAME FROM #T_CostItems1
        --汇总每一个类别的值
        DECLARE @type_sum DECIMAL(18, 2)=0
        SELECT @type_sum=ISNULL(SUM(FAmt),0) FROM #temp
        WHERE FDeptID=@deptid AND FCostItem=@typeid
		
		SET @sql += '''' + CONVERT(VARCHAR(55), @type_sum)
        IF @iter < @count SET @sql += ''', '

        SET @iter+=1
        DELETE FROM #T_CostItems1 WHERE FEXPID=@typeid
    END
	-- 未明分类
	-- DECLARE @unsure_sum DECIMAL(18, 2)=0
    -- SELECT @unsure_sum=ISNULL(SUM(FAmt),0) FROM #temp
    -- WHERE FDeptID=@deptid AND FCostItem=0

	--SET @sql += '''' + CONVERT(VARCHAR(55), @unsure_sum)
	SET @sql += ''');' + CHAR(10)

    DROP TABLE #T_CostItems1
    DELETE FROM #T_Dept WHERE FDeptID=@deptid
END

DROP TABLE #T_Dept
DROP TABLE #temp


SET @sql += 'SELECT * FROM #results' + CHAR(10)
SET @sql += 'DROP TABLE #results' + CHAR(10)

--PRINT(@sql)
EXEC(@sql)

END


/*
EXEC proc_czly_AccountDept @SDt='2020-01-01', @EDt='2020-08-13', 
@FOrgId='0', @FDeptId='0'


SELECT d.FDEPTID,d.FLEVELCODE,d.FNUMBER,FNAME FROM T_BD_DEPARTMENT_L dl
INNER JOIN T_BD_DEPARTMENT d ON dl.FDEPTID=d.FDEPTID
*/
-- SELECT * FROM T_ORG_ORGANIZATIONS_L
