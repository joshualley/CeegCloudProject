
create function [dbo].[fn_GetWorkYear]
(
@beginday datetime, --开始日期
@endday datetime --结束日期
)
returns int
as
begin
    if @beginday > @endday
    begin
        return 0;
    end
    declare @workyear int
    select @workyear = datediff(year, @beginday, @endday)-1--年份差值
    if datepart(month, @endday) > datepart(month, @beginday)--月份超过
    begin
        select @workyear = @workyear + 1
    end
    if datepart(month, @endday) = datepart(month, @beginday)--月份一样
    begin
        if datepart(day, @endday) >= datepart(day, @beginday)--日超过
        begin
            select @workyear = @workyear + 1
        end
    end
    return @workyear ;
End

