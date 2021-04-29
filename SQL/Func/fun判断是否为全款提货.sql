/*
判断是否为全款提货
*/
CREATE FUNCTION [dbo].[Fun_IsFullPay](
    @recCondId BIGINT
)
RETURNS INT
AS
BEGIN
    DECLARE @isFull INT

    SELECT @isFull = CASE WHEN SUM(FRate)=100 THEN 1 ELSE 0 END
    FROM T_BD_RecConditionEntry
    WHERE (FDueCalMethodID=20011 OR FISPREPAID=1) AND FID=@recCondId

    RETURN @isFull
END
