-- 按比例计算收款金额
CREATE FUNCTION [dbo].[Fun_CalRateAmt](
    @paywayId BIGINT,
    @planSeq INT,
    @orderAmt DECIMAL(18,2)
)
RETURNS DECIMAL(18,2)
AS
BEGIN
    DECLARE @amt DECIMAL(18,2) = @orderAmt
    SELECT @amt=@orderAmt*FRate/100 FROM T_BD_RecConditionEntry
    WHERE FID=@paywayId AND FSeq=@planSeq

    RETURN @amt
END

