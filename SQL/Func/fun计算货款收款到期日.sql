-- 计算货款收款到期日
CREATE FUNCTION [dbo].[Fun_CalDeadline](
    @paywayId BIGINT,
    @planSeq INT,
    @dvtDt DATETIME
)
    RETURNS DATETIME
AS
BEGIN
    DECLARE @deadline DATETIME
    SELECT
        @deadline=DATEADD(DAY, FODMonth*30+FODMonth/2+FODDay, @dvtDt)
    --FID,FRate,FIsPrePaid,FODConfirmMethod
    --dbo.GetName('ENUM', FODConfirmMethod, '到期日确定方式的值') FCombName,
    --FODMonth, FODDay
    FROM T_BD_RecConditionEntry
    WHERE FID=@paywayId AND FSeq=@planSeq

    RETURN @deadline
END
