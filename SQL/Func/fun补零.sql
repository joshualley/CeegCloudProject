/*---------------------------- 函数 路径编码补0 ----------------------------*/
create function [dbo].[fun_BosRnd_addSpace](
@itemID varchar(10),	--待处理值
@strB varchar(50),		--前缀
@strE varchar(50),		--后缀
@maxLen int				--补足长度
)  
returns varchar(30) 
as  
begin  

declare @itemNo varchar(80)
--计算补0  
while(len(@itemID)<@maxLen)  
begin  
 set @itemID='0'+@itemID  
end  

set @itemNo=@strB+@itemID+@strE

return  @itemNo 
  
end  
----------------
--select dbo.fun_BosRnd_addSpace(345,'XX','',3)
--select dbo.fun_BosRnd_addSpace(1,'','',3)
