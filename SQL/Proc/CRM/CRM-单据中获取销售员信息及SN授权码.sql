-- CRM-单据中获取销售员信息及SN授权码
/*---------- CRM 跟据传入用户获取 部门、层级码、岗位信息 ----------*/
CREATE proc [dbo].[proc_cztyCrm_GetCrmSN]
@FUserID		int, 
@FIsFirstPost	varchar(10)='1'
as
begin
set nocount on

--set @FUserID=157198
--select u.FUserID,u.FNAME FUserName,e.FID FEmpID,el.FName FEmpName,	--u.FName,u.FLinkObject,c.FNumber,cl.FName FName_CL,
--es.FDeptID,es.FWORKORGID FDeptOrg,d.FLevelCode FDeptLC,'.Z'+convert(varchar,es.FWORKORGID)+d.FLevelCode FLevelCode,
--es.FIsFirstPost,es.FPostID,pl.FNAME FPostName,ph.FLeaderPost--,s.FDOCUMENTSTATUS,s.FFORBIDSTATUS
--from(select * from T_SEC_USER where FUSERID=@FUserID )u 
--inner join V_bd_ContactObject c on u.FLinkObject=c.FID 
--inner join V_bd_ContactObject_L cl on u.FLinkObject=cl.FID 
--inner join T_HR_EMPINFO e on c.FNumber=e.FNumber 
--inner join T_HR_EMPINFO_L el on e.FID=el.FID and el.FLOCALEID=2052 
--inner join T_BD_STAFFTEMP es on e.FID=es.FID and convert(varchar,es.FIsFirstPost)like(@FIsFirstPost)	 --es.FIsFirstPost=1 
--inner join T_BD_STAFF s on es.FSTAFFID=s.FSTAFFID and s.FDOCUMENTSTATUS='C' and s.FFORBIDSTATUS='A'
--inner join T_BD_DEPARTMENT d on es.FDEPTID=d.FDEPTID 
----inner join T_ORG_POST p on es.FPOSTID=p.FPOSTID 
--inner join T_ORG_POST_L pl on es.FPOSTID=pl.FPOSTID and pl.FLOCALEID=2052 
--inner join T_ORG_HRPOST ph on es.FPOSTID=ph.FPOSTID 
--order by es.FISFIRSTPOST desc,es.FWORKORGID,es.FDeptID 

select u.FUserID,u.FNAME FUserName,e.FID FEmpID,el.FName FEmpName,	--u.FName,u.FLinkObject,c.FNumber,cl.FName FName_CL,
ISNULL(d2.FDeptID, es.FDeptID) FDeptID,ISNULL(d2.FUseOrgID, es.FWorkOrgID) FDeptOrg,
d.FLevelCode FDeptLC,'.Z'+convert(varchar,d2.FUseOrgID)+d2.FLevelCode FLevelCode,
es.FIsFirstPost,es.FPostID,pl.FNAME FPostName,ph.FLeaderPost--,s.FDOCUMENTSTATUS,s.FFORBIDSTATUS
from(select * from T_SEC_USER where FUSERID=@FUserID )u 
inner join V_bd_ContactObject c on u.FLinkObject=c.FID 
inner join V_bd_ContactObject_L cl on u.FLinkObject=cl.FID 
inner join T_HR_EMPINFO e on c.FNumber=e.FNumber 
inner join T_HR_EMPINFO_L el on e.FID=el.FID and el.FLOCALEID=2052 
inner join T_BD_STAFFTEMP es on e.FID=es.FID and convert(varchar,es.FIsFirstPost)like(@FIsFirstPost)	 --es.FIsFirstPost=1 
inner join T_BD_STAFF s on es.FSTAFFID=s.FSTAFFID and s.FDOCUMENTSTATUS='C' and s.FFORBIDSTATUS='A'
inner join T_BD_DEPARTMENT d on es.FDEPTID=d.FDEPTID 
left join T_BD_DEPARTMENT d2 on dbo.fun_czty_GetWorkDeptID(d.FDeptID)=d2.FDeptID
--inner join T_ORG_POST p on es.FPOSTID=p.FPOSTID 
inner join T_ORG_POST_L pl on es.FPOSTID=pl.FPOSTID and pl.FLOCALEID=2052 
inner join T_ORG_HRPOST ph on es.FPOSTID=ph.FPOSTID 
order by es.FISFIRSTPOST desc,es.FWORKORGID,es.FDeptID 


end 
