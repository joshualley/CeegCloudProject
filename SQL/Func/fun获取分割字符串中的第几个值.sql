--获取分割字符串中的第几个值
CREATE FUNCTION [dbo].[Fun_GetValueAt](
	@str VARCHAR(MAX),
	@tag VARCHAR,       --分隔符
	@index int
)
RETURNS VARCHAR(100)
AS
BEGIN
	DECLARE @result VARCHAR(100)
	DECLARE @idx INT = CHARINDEX(@tag, @str)
	DECLARE @i INT = 0
	WHILE @idx > 0 and @i < @Index
	BEGIN
		SET @i += 1
		SET @result = LEFT(@str, @idx-1)
		SET @str = SUBSTRING(@str, @idx+1, LEN(@str))
		SET @idx = CHARINDEX(@tag, @str)
	END
	
	RETURN @result
END

--select [dbo].[Fun_GetValueAt]('.1231233.1231231.123123.', '.', 1)
