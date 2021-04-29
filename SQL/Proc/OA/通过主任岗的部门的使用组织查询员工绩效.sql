--通过主任岗的部门的使用组织查询员工绩效
ALTER PROC [dbo].[proc_czly_GetPerformanceInfo](
	@FOrgId int,
	@FDeptId int,
	@Date datetime
)
AS
BEGIN
	declare @Year int = YEAR(@Date)
	declare @Month int = MONTH(@Date)
	declare @FNumber varchar(20)=(select FNUMBER from T_BD_DEPARTMENT where FDEPTID=@FDeptId)
	select @FDeptId=db.FDEPTID from T_BD_DEPARTMENT da
	inner join T_BD_DEPARTMENT db on da.FNUMBER=db.FNUMBER and db.FUSEORGID=1
	where da.FDEPTID=@FDeptId
	declare @FDeptNumber varchar(30)=@FNumber+'%'

	if @FDeptId = 0
	begin
		select d.FUSEORGID FOrgId, d.FDeptId, e.FID FEmpId,el.FName FEmpName,
			   pl.FPostId, pl.FNAME FPostName, pr.FScore, pr.FID FPrPk
		from T_HR_EMPINFO e 
		inner join T_HR_EMPINFO_L el on e.FID=el.FID and el.FLOCALEID=2052 
		--任岗信息
		inner join T_BD_STAFFTEMP es on e.FID=es.FID and convert(varchar,es.FIsFirstPost)like('1')	 --es.FIsFirstPost=1
		inner join T_BD_DEPARTMENT d on dbo.fun_czty_GetWorkDeptID(es.FDEPTID)=d.FDEPTID
		inner join T_BD_STAFF s on es.FSTAFFID=s.FSTAFFID and s.FDOCUMENTSTATUS='C' and s.FFORBIDSTATUS='A'
		--inner join T_BD_DEPARTMENT d on es.FDEPTID=d.FDEPTID
		inner join T_ORG_POST_L pl on es.FPOSTID=pl.FPOSTID and pl.FLOCALEID=2052 
		--inner join T_ORG_HRPOST ph on es.FPOSTID=ph.FPOSTID 
		inner join V_bd_ContactObject c on e.FNumber=c.FNumber --联系对象
		inner join T_SEC_USER u on u.FLINKOBJECT=c.FID AND u.FFORBIDSTATUS='A'--用户
		inner join ora_Task_PersonalReport pr on pr.FCreatorId=u.FUSERID and pr.FDOCUMENTSTATUS='C' --个人汇报单
		where d.FUSEORGID=@FOrgId and YEAR(pr.FCREATEDATE)=@Year and MONTH(pr.FCREATEDATE)=@Month and e.FFORBIDSTATUS='A'
		order by e.FNUMBER
	end
	else
		select d.FUSEORGID FOrgId, d.FDeptId, e.FID FEmpId,el.FName FEmpName,
			   pl.FPostId, pl.FNAME FPostName, pr.FScore, pr.FID FPrPk
		from T_HR_EMPINFO e
		inner join T_HR_EMPINFO_L el on e.FID=el.FID and el.FLOCALEID=2052 
		--任岗信息
		inner join T_BD_STAFFTEMP es on e.FID=es.FID and convert(varchar,es.FIsFirstPost)like('1')	 --es.FIsFirstPost=1 
		inner join T_BD_STAFF s on es.FSTAFFID=s.FSTAFFID and s.FDOCUMENTSTATUS='C' and s.FFORBIDSTATUS='A' --and es.FDEPTID=@FDeptId
		inner join T_BD_DEPARTMENT d on dbo.fun_czty_GetWorkDeptID(es.FDEPTID)=d.FDEPTID and d.FNUMBER like @FDeptNumber
		inner join T_ORG_POST_L pl on es.FPOSTID=pl.FPOSTID and pl.FLOCALEID=2052 
		--inner join T_ORG_HRPOST ph on es.FPOSTID=ph.FPOSTID 
		inner join V_bd_ContactObject c on e.FNumber=c.FNumber --联系对象
		inner join T_SEC_USER u on u.FLINKOBJECT=c.FID AND u.FFORBIDSTATUS='A' --用户
		inner join ora_Task_PersonalReport pr on pr.FCreatorId=u.FUSERID and pr.FDOCUMENTSTATUS='C' --个人汇报单
		where d.FUSEORGID=@FOrgId and YEAR(pr.FCREATEDATE)=@Year and MONTH(pr.FCREATEDATE)=@Month and e.FFORBIDSTATUS='A'
		order by e.FNUMBER
END

--select * from T_BD_DEPARTMENT_L where FNAME like '%人力%'
--select * from T_ORG_ORGANIZATIONS_L where FNAME like '%变压器%'

-- exec proc_czly_GetPerformanceInfo @FOrgId='156139',@FDeptId='295125', @Date='2020-04-01'
-- exec proc_czly_GetPerformanceInfo @FOrgId='100680',@FDeptId='0', @Date='2021-04-01'



-- select * from T_ORG_ORGANIZATIONS_L
