/*------------ 输入用户ID 取得绑定职员ID ------------*/  
ALTER proc [dbo].[proc_czty_GetLoginUser2Emp]  
@FUserID  int=-1,
@FEmpID int=-1, 
@FIsFirstPost varchar(10)='1',
@FOrgID int=-1
as  
BEGIN  

create table #temp(
	FEmpID int, 
    FEmpName varchar(255),
	FDeptID int, 
    FDeptName varchar(255),
    FLevelCode varchar(255),
	FNumber  varchar(255),
	FRankID varchar(255), 
	FJobLevelId varchar(255),
	FPostID int, 
    FPostName varchar(255),
	FMobile varchar(255),
	FSuperiorPost int, 
    FSPostName varchar(255),
	FGManager int,
	FORGID INT,
	FORGName Varchar(255),
	FContractType INT,
	FWorkAddress INT
)
  
if @FEmpID=-1
begin
	insert into #temp
	select --u.FUserID,  
		e.FID FEmpID, el.FNAME,
		es.FDeptID, dl.FFULLNAME,d.FLevelCode,
		e.FNUMBER FNumber, 
		e.F_HR_RANK FRankID,spi.FJobLevelId,
		es.FPostID, pl.FNAME,
		e.FMOBILE FMobile, 
		isnull(pr.FSuperiorPost,0) FSuperiorPost, pl2.FNAME,
		0,es.FWORKORGID,ol.FNAME,
		p.FCONTRACTTYPE, p.FWORKADDRESS
	from(select * from T_SEC_USER where FUSERID=@FUserID)u  
	inner join V_bd_ContactObject c on u.FLinkObject=c.FID  
	inner join V_bd_ContactObject_L cl on u.FLinkObject=cl.FID  
	inner join T_HR_EMPINFO e on c.FNumber=e.FNumber  
	inner join T_HR_EMPINFO_L el on e.FID=el.FID   
	inner join T_BD_STAFFTEMP es on e.FID=es.FID and es.FIsFirstPost='1' --convert(varchar,es.FIsFirstPost)like(@FIsFirstPost)     
	inner join T_BD_STAFF s on es.FSTAFFID=s.FSTAFFID and s.FDOCUMENTSTATUS='C' and s.FFORBIDSTATUS='A'   
	left join T_BD_STAFFPOSTINFO spi on s.FSTAFFID=spi.FSTAFFID
	inner join T_BD_Department d on es.FDeptID=d.FDeptID  
	inner join T_BD_Department_L dl on es.FDeptID=dl.FDeptID
	inner join T_ORG_ORGANIZATIONS_L ol on es.FWORKORGID=ol.FORGID and ol.FLOCALEID=2052 
	left join( 
		SELECT ae.FENTRYID, ael.FDATAVALUE 
		FROM T_BAS_ASSISTANTDATA_L al
		INNER JOIN T_BAS_ASSISTANTDATAENTRY ae ON ae.FID=al.FID
		INNER JOIN T_BAS_ASSISTANTDATAENTRY_L ael ON ael.FENTRYID=ae.FENTRYID
		WHERE al.FNAME='职级'
	)a on e.F_HR_RANK=a.FENTRYID   
	left join T_ORG_POST_L pl on es.FPostID=pl.FPOSTID   
	left join T_ORG_POSTREPORTLINE pr on es.FPOSTID=pr.FPOSTID and pr.FISVALID=1 
										and pr.FREPORTTYPE='aa4d1d1b3b184a888ee835387604e955' 
	left join T_ORG_POST_L pl2 on pr.FSUPERIORPOST=pl2.FPOSTID and pl2.FLOCALEID=2052
	inner join T_BD_PERSON p on p.FID=e.FID
	--left join t_BD_EmpinfoBank eb on e.FID=eb.FID and eb.FISDEFAULT=1   
	--select FID,FISDEFAULT,FCOUNTRY,FBANKCODE from t_BD_EmpinfoBank
end 
else if @FUserID=-1
begin
	insert into #temp
	SELECT 
		e.FID FEmpID, el.FNAME,
		es.FDeptID, dl.FFULLNAME,d.FLevelCode,
		e.FNUMBER FNumber, 
		e.F_HR_RANK FRankID,spi.FJobLevelId,
		es.FPostID, pl.FNAME,
		e.FMOBILE FMobile, 
		isnull(pr.FSuperiorPost,0) FSuperiorPost, pl2.FNAME,
		0,es.FWORKORGID,ol.FNAME,
		p.FCONTRACTTYPE, p.FWORKADDRESS
	FROM (select * from T_HR_EMPINFO WHERE FID=@FEmpID) e
	inner join T_HR_EMPINFO_L el on e.FID=el.FID   
	inner join T_BD_STAFFTEMP es on e.FID=es.FID and es.FIsFirstPost='1' --convert(varchar,es.FIsFirstPost)like(@FIsFirstPost)    
	inner join T_BD_STAFF s on es.FSTAFFID=s.FSTAFFID and s.FDOCUMENTSTATUS='C' and s.FFORBIDSTATUS='A'   
	left join T_BD_STAFFPOSTINFO spi on s.FSTAFFID=spi.FSTAFFID
	inner join T_BD_Department d on es.FDeptID=d.FDeptID  
	inner join T_BD_Department_L dl on es.FDeptID=dl.FDeptID
	inner join T_ORG_ORGANIZATIONS_L ol on es.FWORKORGID=ol.FORGID and ol.FLOCALEID=2052 
	left join( SELECT ae.FENTRYID, ael.FDATAVALUE 
		FROM T_BAS_ASSISTANTDATA_L al
		INNER JOIN T_BAS_ASSISTANTDATAENTRY ae ON ae.FID=al.FID
		INNER JOIN T_BAS_ASSISTANTDATAENTRY_L ael ON ael.FENTRYID=ae.FENTRYID
		WHERE al.FNAME='职级'
	)a on e.F_HR_RANK=a.FENTRYID
	left join T_ORG_POST_L pl on es.FPostID=pl.FPOSTID   
	left join T_ORG_POSTREPORTLINE pr on es.FPOSTID=pr.FPOSTID and pr.FISVALID=1 
										and pr.FREPORTTYPE='aa4d1d1b3b184a888ee835387604e955' 
	left join T_ORG_POST_L pl2 on pr.FSUPERIORPOST=pl2.FPOSTID and pl2.FLOCALEID=2052
	inner join T_BD_PERSON p on p.FID=e.FID
end

update t set FGManager=(
	select top 1 FGMANAGERID from ora_OA_GManager where FORGID=t.FORGID
) from #temp t

select * from #temp
drop table #temp

END  
-----------------  
-- exec proc_czty_GetLoginUser2Emp @FUserID=270315
-- exec proc_czty_GetLoginUser2Emp @FUserID=173439,@FIsFirstPost='%'  
-- exec proc_czty_GetLoginUser2Emp @FEmpID=287146,@FOrgID='156143'  
-- exec proc_czty_GetLoginUser2Emp @FEmpID='268549'
