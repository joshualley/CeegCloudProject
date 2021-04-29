--判断预算占用是否释放
CREATE PROC [dbo].[proc_czly_IsFreeBdgOcc](
	@FID INT = 0,
	@FEntryID INT = 0,
	@FFormID VARCHAR(100)
)
AS

--DECLARE @FID INT = 0
--DECLARE @FEntryID INT = 0
--DECLARE @FFormID VARCHAR(100)

SELECT FDSrcAction INTO #t
FROM ora_BDG_BudgetMDDtl 
WHERE FDSrcType=@FFormID AND FDSrcFID=@FID AND FDSrcEntryID=@FEntryID

DECLARE @OCC_ROW INT=0
DECLARE @FREE_ROW INT=0
SELECT @FREE_ROW=COUNT(*) FROM #t WHERE FDSrcAction <> '提交'
SELECT @OCC_ROW=COUNT(*) FROM #t WHERE FDSrcAction = '提交'
DROP TABLE #t

DECLARE @FResult INT=0
--占用比释放多一次时，允许释放
IF(@OCC_ROW-@FREE_ROW)=1
BEGIN
	SET @FResult=1
END

SELECT @FResult AS FResult

--EXEC proc_czly_IsFreeBdgOcc @FID=0, @FEntryID=0, @FFormID='k0c30c431418e4cf4a60d241a18cb241c'
