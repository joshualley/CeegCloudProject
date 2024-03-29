/*
Title:    费用台账获取费用项目
Author:   刘跃
Created:  2020-08-13
Modified: 2020-08-13
Info:
	费用台账获取费用项目
*/

CREATE PROC [dbo].[proc_czly_AccountOrg](
    @SDt DATETIME,
    @EDt DATETIME,
    @FOrgId BIGINT=0, --冗余字段，提供根据子公司查询
    @FDeptId BIGINT=0,
    @FAccountId BIGINT=0 --科目
)
AS
BEGIN

SET NOCOUNT ON

--DECLARE @SDt DATETIME='2020-01-01'
--DECLARE @EDt DATETIME='2020-07-30'

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


SELECT FEXPID,FNAME FROM T_BD_Expense_L e
INNER JOIN #temp t ON e.FEXPID=t.FCostItem
GROUP BY FEXPID,FNAME ORDER BY FEXPID

END

-- EXEC proc_czly_AccountOrg @SDt='2020-01-01', @EDt='2020-08-13'
