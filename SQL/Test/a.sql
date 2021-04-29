


select *
from T_HR_EMPINFO e
inner join T_HR_EMPINFO_L el on e.FID=el.FID and el.FLOCALEID=2052 
--任岗信息
inner join T_BD_STAFFTEMP es on e.FID=es.FID and convert(varchar,es.FIsFirstPost)like('1')	 --es.FIsFirstPost=1 
inner join T_BD_STAFF s on es.FSTAFFID=s.FSTAFFID and s.FDOCUMENTSTATUS='C' and s.FFORBIDSTATUS='A' --and es.FDEPTID=@FDeptId
inner join T_BD_DEPARTMENT d on dbo.fun_czty_GetWorkDeptID(es.FDEPTID)=d.FDEPTID --and d.FNUMBER like @FDeptNumber
inner join T_ORG_POST_L pl on es.FPOSTID=pl.FPOSTID and pl.FLOCALEID=2052 
--inner join T_ORG_HRPOST ph on es.FPOSTID=ph.FPOSTID 
inner join V_bd_ContactObject c on e.FNumber=c.FNumber --联系对象
inner join T_SEC_USER u on u.FLINKOBJECT=c.FID AND u.FFORBIDSTATUS='A' --用户
inner join ora_Task_PersonalReport pr on pr.FCreatorId=u.FUSERID and pr.FDOCUMENTSTATUS='C' 