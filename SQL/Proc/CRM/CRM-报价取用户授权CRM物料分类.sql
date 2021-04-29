-- CRM-报价取用户授权CRM物料分类
/*-------------- proc_cztyCrm_OfferGetMtlGroup CRM报价 取用户授权CRM物料分类 --------------*/
CREATE proc [dbo].[proc_cztyCrm_OfferGetMtlGroup]
@FUserID	int	--登录用户ID
as
begin 
set nocount on

declare @FIDFilter varchar(2000)=''
declare @FPostName varchar(50)

create table #t
(
	FPrjID	int primary key identity(1,1),
	FID		int,
	FIdx	int
)

--用户信息 T_SEC_USER 建立授权表 跟据岗位判定取授权 报价员需要过滤
--select u.FUserID,u.FNAME FUserName,e.FID FEmpID,e.FSTAFFID,es.FDeptID,es.FPostID,pl.FNAME FPostName 
--into #r			--@FPostName=pl.FNAME
--from(select * from T_SEC_USER where FUSERID=@FUserID )u 
--inner join V_bd_ContactObject c on u.FLinkObject=c.FID 
--inner join T_HR_EMPINFO e on c.FNumber=e.FNumber 
--inner join T_BD_STAFFTEMP es on e.FID=es.FID and es.FIsFirstPost=1  -- convert(varchar,es.FIsFirstPost)like(@FIsFirstPost)	 
--inner join T_BD_STAFF s on es.FSTAFFID=s.FSTAFFID 
--inner join T_ORG_POST_L pl on es.FPOSTID=pl.FPOSTID and pl.FLOCALEID=2052 
--where s.FDOCUMENTSTATUS='C' and s.FFORBIDSTATUS='A' 

select u.FUserID,u.FNAME FUserName,e.FID FEmpID,e.FSTAFFID,es.FDeptID,es.FPostID,	--pl.FNAME FPostName 	
case e.F_ORA_IsCrmRpt when 1 then '报价员' else pl.FName end as FPostName,pl.FName FHrPostName
into #r			--@FPostName=pl.FNAME
from(select * from T_SEC_USER where FUSERID=@FUserID )u 
left join V_bd_ContactObject c on u.FLinkObject=c.FID 
left join T_HR_EMPINFO e on c.FNumber=e.FNumber 
left join T_BD_STAFFTEMP es on e.FID=es.FID and es.FIsFirstPost=1  
left join T_BD_STAFF s on es.FSTAFFID=s.FSTAFFID --and s.FDOCUMENTSTATUS='C' and s.FFORBIDSTATUS='A' 
left join T_ORG_POST_L pl on es.FPOSTID=pl.FPOSTID and pl.FLOCALEID=2052 
where isnull(s.FDOCUMENTSTATUS,'C')='C' and isnull(s.FFORBIDSTATUS,'A')='A' 

select @FPostName=FPostName from #r

if(isnull(@FPostName,'')='报价员')
begin
	insert into #t
	select cm.FID,1 FIdx --u.FUserID,u.FNAME FUserName,e.FID FEmpID,e.FSTAFFID,cm.FNUMBER,cm.FID
	from(select * from T_SEC_USER where FUSERID=@FUserID )u 
	inner join V_bd_ContactObject c on u.FLinkObject=c.FID 
	inner join T_HR_EMPINFO e on c.FNumber=e.FNumber 
	inner join T_HR_EMPINFOCrmMG em on e.FID=em.FID
	inner join ora_CrmBD_MtlGroup cm on em.F_ORA_CRMMTLGP=cm.FID 
	order by cm.FNumber 
end
else begin
	insert into #t select FID,1 FIdx from ora_CrmBD_MtlGroup order by FNumber
end

--select * from ora_CrmBD_MtlGroup
--select FID,1 FIdx into #t from ora_CrmBD_MtlGroup where FID in(123324,123325,123327,123323) 

SELECT @FIDFilter = case @FIDFilter when '' then convert(varchar,FID) else (@FIDFilter + ',' + convert(varchar,FID)) end 
FROM #t where FIdx=1 

select r.*,@FIDFilter FIDFilter,isnull(e.F_Ora_SeeCrmMtl,0)F_Ora_SeeCrmMtl
from #r r
left join T_HR_EMPINFO e on r.FEmpID=e.FID

drop table #r
drop table #t
end
-----------
/*
 exec proc_cztyCrm_OfferGetMtlGroup @FUserID='100564'
 exec proc_cztyCrm_OfferGetMtlGroup @FUserID='157194'	--刘双全
 exec proc_cztyCrm_OfferGetMtlGroup @FUserID='157197'	--王鹏
 exec proc_cztyCrm_OfferGetMtlGroup @FUserID='157193'	--钱雪莲
 exec proc_cztyCrm_OfferGetMtlGroup @FUserID='16394'	--Administrator
*/
