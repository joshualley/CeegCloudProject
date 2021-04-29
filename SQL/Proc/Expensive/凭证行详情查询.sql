/*
Title:    凭证行详情查询
Author:   刘跃
Created:  2020-08-13
Modified: 2020-08-24
Info:
	由部门名称查询
*/

CREATE PROC [dbo].[proc_czly_AccountVocunter](
    @SDt DATETIME,
    @EDt DATETIME,
    @FOrgId BIGINT=0,
    @FAccountId BIGINT=0, --科目
    @FDeptName VARCHAR(100)='',
    @FCostItemId BIGINT=0 --费用项目
)
AS
BEGIN

SET NOCOUNT ON

IF @SDt = '' SET @SDt = (SELECT TOP 1 FDate FROM T_GL_VOUCHER ORDER BY FDate ASC)
IF @EDt = '' SET @EDt = GETDATE()

DECLARE @FNumber VARCHAR(20)=(
    SELECT DISTINCT FNUMBER FROM T_BD_DEPARTMENT d
    INNER JOIN T_BD_DEPARTMENT_L dl ON d.FDEPTID=dl.FDEPTID AND FLocalEID=2052
    WHERE dl.FNAME=@FDeptName
)

--DECLARE @FDeptNumber VARCHAR(30)=@FNumber+'%'

SELECT  
    v.FDATE, v.FBILLNO, v.FVOUCHERGROUPNO, ve.*,
    fi.FFLEX9 FOrignCostItem, ISNULL(ae.FAccountActual, 0) FCostItem
INTO #temp
FROM T_GL_VOUCHERENTRY ve
INNER JOIN T_GL_VOUCHER v ON v.FVOUCHERID=ve.FVOUCHERID
INNER JOIN T_BD_FLEXITEMDETAILV fi ON fi.FID=ve.FDETAILID
INNER JOIN T_BD_DEPARTMENT d ON fi.FFLEX5=d.FDEPTID
LEFT JOIN ora_t_AccountEntry_LK aek ON ve.FENTRYID=aek.FSId
LEFT JOIN ora_t_AccountEntry ae ON ae.FEntryID=aek.FEntryID
LEFT JOIN ora_t_Account a ON a.FID=ae.FID AND a.FDOCUMENTSTATUS='C'
WHERE v.FDate BETWEEN @SDt AND @EDt --AND v.FDOCUMENTSTATUS='C' 
AND ve.FCREDIT=0
AND ve.FACCOUNTID IN (4083, 4084)
AND (@FAccountId=0 OR ve.FACCOUNTID=@FAccountId)
AND (@FOrgId=0 OR d.FUSEORGID=@FOrgId)
AND (@FDeptName='' OR d.FNumber LIKE @FNumber)

UPDATE #temp SET FCostItem=FOrignCostItem WHERE FCostItem=NULL OR FCostItem=0

SELECT * FROM #temp WHERE @FCostItemId=0 OR FCostItem=@FCostItemId

END


/*
EXEC proc_czly_AccountVocunter @SDt='2020-04-01', @EDt='2020-04-30', 
@FAccountId='0', @FDeptName='财务中心_仓储部'
*/
