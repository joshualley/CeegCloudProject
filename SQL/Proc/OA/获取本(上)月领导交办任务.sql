--获取本(上)月领导交办任务
CREATE PROC [dbo].[GetAssignTask](
	@FUserId INT,
	@IsLastMonth INT=0,
	@FormDate DATETIME=''
)
AS
BEGIN

--DECLARE @FUserId int=100621
--DECLARE @IsLastMonth INT=0
IF @FormDate = '' SET @FormDate=GETDATE()

DECLARE @FYear INT=YEAR(@FormDate)
DECLARE @FMonth INT=MONTH(@FormDate)

--获取上月
IF @IsLastMonth=1
BEGIN
	IF (@FMonth=1)
	BEGIN
		SET @FYear=@FYear-1
		SET @FMonth=12
	END
	ELSE 
		SET @FMonth=@FMonth-1
END

--获取负责人
SELECT
	FID AS FSrcID,0 AS FSrcEntryID,FTask,FTarget,FPlanDt,1 AS FIsResp,
	FWeight,FPerformance,FResult,FSelfGrade,FDirectorGrade,FDirectorIdea,FGManagerGrade,FGManagerIdea
INTO #T_AssignTemp
FROM ora_Task_Dispatch
WHERE FRespID=@FUserId AND YEAR(FCREATEDATE)=@FYear AND MONTH(FCREATEDATE)=@FMonth AND FDOCUMENTSTATUS='C'
--获取参与人
INSERT INTO #T_AssignTemp
SELECT
	d.FID,FEntryID,FTask,FTarget,FPlanDt,0,
	FEWeight,FEPerformance,FEResult,FESelfGrade,FEDirectorGrade,FEDirectorIdea,FEGManagerGrade,FEGManagerIdea
FROM ora_Task_Dispatch d INNER JOIN ora_Task_DispatchEntry de ON d.FID=de.FID
WHERE FParticipant=@FUserId AND YEAR(FCREATEDATE)=@FYear AND MONTH(FCREATEDATE)=@FMonth AND FDOCUMENTSTATUS='C'

SELECT * FROM #T_AssignTemp

END

--EXEC GetAssignTask @FUserId=100621,@IsLastMonth=0

