--ͨ通过用户获取销售员
CREATE PROC [dbo].[proc_czly_GetSalesmanIdByUserId](
	@FUserId int,
	@FOrgId int=-1
)
AS
BEGIN
if @FOrgId <> -1
begin
	select s.FID FSalesmanId, u.FUserId, u.FName, d.FDeptID, se.FWORKORGID FOrgID, s.FBIZORGID, s.FDEPTID FBizDeptID
	from (select FUserId,FName,FLinkObject from T_SEC_USER where FUSERID=@FUserId) u
	inner join V_bd_ContactObject c on u.FLINKOBJECT=c.FID
	--inner join T_HR_EMPINFO e on c.FNumber=e.FNumber
	inner join V_BD_SALESMAN s on c.FNumber=s.FNumber
	inner join T_HR_EMPINFO e on s.FNUMBER=e.FNUMBER
	inner join T_BD_STAFFTEMP se on e.FID=se.FID and se.FIsFirstPost='1'
	inner join T_BD_DEPARTMENT d on d.FDEPTID=se.FDEPTID
	where s.FBIZORGID=@FOrgId
end
else
	select s.FID FSalesmanId, u.FUserId, u.FName, d.FDeptID, se.FWORKORGID FOrgID, s.FBIZORGID, s.FDEPTID FBizDeptID
	from (select FUserId,FName,FLinkObject from T_SEC_USER where FUSERID=@FUserId) u
	inner join V_bd_ContactObject c on u.FLINKOBJECT=c.FID
	--inner join T_HR_EMPINFO e on c.FNumber=e.FNumber
	inner join V_BD_SALESMAN s on c.FNumber=s.FNumber
	inner join T_HR_EMPINFO e on s.FNUMBER=e.FNUMBER
	inner join T_BD_STAFFTEMP se on e.FID=se.FID and se.FIsFirstPost='1'
	inner join T_BD_DEPARTMENT d on d.FDEPTID=se.FDEPTID
END

-- EXEC proc_czly_GetSalesmanIdByUserId @FUserId=100559
-- EXEC proc_czly_GetSalesmanIdByUserId @FUserId=100560

/*
select distinct u.FUserId, u.FName --,s.FID FSalesmanId, s.FFORBIDSTATUS
from (select FUserId,FName,FLinkObject from T_SEC_USER) u
inner join V_bd_ContactObject c on u.FLINKOBJECT=c.FID
--inner join T_HR_EMPINFO e on c.FNumber=e.FNumber
inner join V_BD_SALESMAN s on c.FNUMBER=s.FNUMBER 
where u.FUserId not in (
select distinct u.FUserId--, u.FName ,s.FID FSalesmanId, s.FFORBIDSTATUS
from (select FUserId,FName,FLinkObject from T_SEC_USER) u
inner join V_bd_ContactObject c on u.FLINKOBJECT=c.FID
--inner join T_HR_EMPINFO e on c.FNumber=e.FNumber
inner join V_BD_SALESMAN s on c.FNUMBER=s.FNUMBER 
where s.FBIZORGID=1
)
*/
