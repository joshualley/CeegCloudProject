-- 转换部门的内码至正确的使用组织
alter function [dbo].[fun_czty_GetWorkDeptID](@FDeptID int)
returns int 
as
begin

if( select FName from T_ORG_ORGANIZATIONS_L ol
	inner join T_ORG_ORGANIZATIONS o on o.FORGID=ol.FORGID
	where FLOCALEID=2052 and FNUMBER='100')<>'变压器产业'
begin
	return @FDeptID
end

declare @FWorkDeptID int=0
----多阶
select @FWorkDeptID=d2.FDeptID
--d.FDeptID,d.FMasterID,dl.FName,dl.FFULLNAME,d2.FDEPTID,d2.FUSEORGID,o2l.FName
from(select * from T_BD_Department where FDEPTID=@FDeptID)d	
inner join T_BD_Department_L dl on d.FDeptID=dl.FDeptID and dl.FLOCALEID=2052 
inner join T_BD_Department d2 on d.FMasterID=d2.FMasterID and d2.FDEPTID<>d2.FMasterID
inner join T_ORG_ORGANIZATIONS_L o2l on d2.FUseOrgID=o2l.FOrgID and o2l.FLOCALEID=2052 and CHARINDEX(o2l.FNAME,dl.FFULLNAME)>0 
														and CHARINDEX(o2l.FNAME,dl.FName)>0
if(@FWorkDeptID>0)
begin
	goto output
end

select top 1 @FWorkDeptID=d2.FDeptID
--d.FDeptID,d.FMasterID,dl.FName,dl.FFULLNAME,d2.FDEPTID,d2.FUSEORGID,o2l.FName
from(select * from T_BD_Department where FDEPTID=@FDeptID)d	
inner join T_BD_Department_L dl on d.FDeptID=dl.FDeptID and dl.FLOCALEID=2052 
inner join T_BD_Department d2 on d.FMasterID=d2.FMasterID and d2.FDEPTID<>d2.FMasterID
inner join T_ORG_ORGANIZATIONS_L o2l on d2.FUseOrgID=o2l.FOrgID and o2l.FLOCALEID=2052 and CHARINDEX(o2l.FNAME,dl.FFULLNAME)>0 
order by CHARINDEX(o2l.FNAME,dl.FFULLNAME) desc 

output:
return @FWorkDeptID
end
-------------
/*
select dbo.fun_czty_GetWorkDeptID(295115)
select dbo.fun_czty_GetWorkDeptID(295164)
select dbo.fun_czty_GetWorkDeptID(295169)
*/
