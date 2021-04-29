--字符串分割
CREATE FUNCTION [dbo].[Fun_Split](
	@str VARCHAR(MAX),
	@tag VARCHAR       --分隔符
)
RETURNS @result TABLE(item VARCHAR(100))
AS
BEGIN
	DECLARE @idx INT = CHARINDEX(@tag, @str)

	WHILE @idx >= 1
	BEGIN
		IF LEFT(@str, @idx-1) <> '' INSERT INTO @result VALUES(LEFT(@str, @idx-1))
		SET @str = SUBSTRING(@str, @idx+1, LEN(@str))
		SET @idx = CHARINDEX(@tag, @str)
	END
	IF @str <> '' INSERT INTO @result VALUES(@str)
		

	RETURN
END

--select * from [dbo].[Fun_Split]('.1231233.1231231.123123.', '.')
