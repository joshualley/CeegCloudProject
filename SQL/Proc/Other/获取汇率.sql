/*------------------------ 获取汇率 ------------------------*/
create proc [dbo].[proc_cztyBD_GetRate]
@FRateTypeID	int=1,					--取汇率类型
@FGetDate		datetime,				--即取日期
@FCyForID		int,	--原币			--原币ID
@FFCyToID		int		--目标币		--目标币ID
as
begin
set nocount on 
declare @FRate	decimal(18,6)

--原币=目标币
if(@FCyForID=@FFCyToID)
begin
	set @FRate=1
	GOTO EndPoint
end

--原币 兑换 目标币 取直接汇率
select @FRate=FExchangeRate --FRATEID,FCyForID,FCyToID,FRateTypeID,FExchangeRate,FReverseExRate,FBEGDATE,FENDDATE 
from T_BD_Rate 
where FRateTypeID=@FRateTypeID and @FGetDate between FBEGDATE and FENDDATE 
and FCyForID=@FCyForID and FCyToID=@FFCyToID and FDOCUMENTSTATUS='C' and FFORBIDSTATUS='A' 
if(@FRate>0)
begin
	GOTO EndPoint
end

--原币》目标币 不存在 反向拟查询 间接汇率
select @FRate=FReverseExRate --FRATEID,FCyForID,FCyToID,FRateTypeID,FExchangeRate,FReverseExRate,FBEGDATE,FENDDATE 
from T_BD_Rate 
where FRateTypeID=@FRateTypeID and @FGetDate between FBEGDATE and FENDDATE 
and FCyForID=@FFCyToID and FCyToID=@FCyForID and FDOCUMENTSTATUS='C' and FFORBIDSTATUS='A' 

EndPoint:
select isnull(@FRate,0) FRate
end
--------------
/*
 exec proc_cztyBD_GetRate @FGetDate='2019-08-17',@FCyForID='7',@FFCyToID='1'
 exec proc_cztyBD_GetRate @FGetDate='2019-09-17',@FCyForID='1',@FFCyToID='7'
*/
