-- 判断收款计划的行是否为质保金
CREATE FUNCTION [dbo].[Fun_IsWarranty](
    @paywayId BIGINT,
    @planSeq INT
)
RETURNS INT
AS
BEGIN
    DECLARE @isWrt INT = 0
    SELECT @isWrt=F_ora_IsWrt FROM T_BD_RecConditionEntry
    WHERE FID=@paywayId AND FSeq=@planSeq

    RETURN @isWrt
END
