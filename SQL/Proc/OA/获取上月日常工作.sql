--获取上月日常工作
CREATE PROC [dbo].[GetLastMonthDailyTask](
	@FUserId INT,
	@FormDate DATETIME=''
)
AS
BEGIN

IF @FormDate = '' SET @FormDate=GETDATE()

DECLARE @FYear INT=YEAR(@FormDate)
DECLARE @FMonth INT=MONTH(@FormDate)

IF (@FMonth=1)
BEGIN
	SET @FYear=@FYear-1
	SET @FMonth=12
END
ELSE 
	SET @FMonth=@FMonth-1

DECLARE @FLID INT

SELECT @FLID=FID FROM ora_Task_PersonalReport WHERE YEAR(FCREATEDATE)=@FYear AND MONTH(FCREATEDATE)=@FMonth AND FCREATORID=@FUserId AND FDOCUMENTSTATUS='C'

SELECT 
	FSEQ,FID FSrcID,FEntryID FSrcEntryID,FEvaContent,FEvaDetail,FWeight,FNote,
	FPerformance,FResult,FSelfGrade,FDirectorGrade,FDirectorIdea,FGManagerGrade,FGManagerIdea
FROM ora_Task_DailyEntry
WHERE FID=@FLID

END

--EXEC GetLastMonthDailyTask @FUserId='100621'

