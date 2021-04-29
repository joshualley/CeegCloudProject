-- CRM-报价单选择报价员
/*---------------------- Crm报价单 跟据表体Crm产品分类 选择报价员 ----------------------*/
CREATE proc [dbo].[proc_cztyCrm_RndOffer]
@FCrmSOID	int	--ora_CRM_SaleOffer[FID]
as
begin
set nocount on
----清除单据原报价员
update ora_CRM_SaleOffer set 
	FChecker1=0,FChecker2=0,FChecker3=0,FChecker4=0,FChecker5=0,
	FChecker6=0,FChecker7=0,FChecker8=0,FChecker9=0,FChecker10=0
where FID=@FCrmSOID

----获取报价员授权
--select p.FPostID,st.FID FEmpID,el.FNAME FEmpNA,convert(varchar(15),isnull(ec.F_ora_CrmMtlGp,-1))FCrmMtlGP,isnull(e.F_ora_CrmTime,'1900-01-01')FCrmTime
--into #e
--from(select FPOSTID from T_ORG_POST_L where FNAME='报价员' and FLOCALEID=2052)p
--inner join T_BD_STAFFTEMP st on p.FPOSTID=st.FPOSTID and st.FIsFirstPost=1 
--inner join T_HR_EmpInfo e on st.FID=e.FID 
--inner join T_HR_EMPINFO_L el on e.FID=el.FID and el.FLOCALEID=2052 
--left join T_HR_EmpInfoCrmMG ec on e.FID=ec.FID 
--where e.FDOCUMENTSTATUS='C' and FForbidStatus='A' 

select e.FID FEmpID,el.FNAME FEmpNA,convert(varchar(15),isnull(ec.F_ora_CrmMtlGp,-1))FCrmMtlGP,isnull(e.F_ora_CrmTime,'1900-01-01')FCrmTime 
into #e
from(select * from T_HR_EmpInfo where F_ORA_ISCRMRPT=1)e 
inner join T_HR_EMPINFO_L el on e.FID=el.FID and el.FLOCALEID=2052 
left join T_HR_EmpInfoCrmMG ec on e.FID=ec.FID 
where e.FDOCUMENTSTATUS='C' and FForbidStatus='A' 

--select * from #e

----员工合并授权 （字符串合并）
select e.*,u.FUserID 
into #em
from(select FEmpID,FEmpNA,convert(int,0)FRndSeq,convert(int,0)FRndGp,min(FCrmTime)FCrmTime,
	FCrmMtlGP=','+(select [FCrmMtlGP] +',' from #e as b where b.FEmpID = a.FEmpID for xml path('')) 
	from #e as a group by FEmpID,FEmpNA)e
inner join T_HR_EMPINFO ei on e.FEmpID=ei.FID
inner join V_bd_ContactObject co on ei.FNUMBER=co.FNUMBER 
inner join T_SEC_USER u on co.FID=u.FLinkObject 

--select * from #em

----提取授权
select 
row_number()over(order by FCrmMtlGP)FIdxGp,FCrmMtlGP 
into #mg
from #em 
group by FCrmMtlGP 

--select * from #mg

----分组 分组按分派日期排序
--select * 
update em set em.FRndSeq=t.FIdxItem,em.FRndGp=t.FIdxGp
from #em em 
inner join(
	select em.*,mg.FIdxGp,ROW_NUMBER() over(partition by mg.FIdxGp order by em.FCrmTime)FIdxItem
	from #em em inner join #mg mg on em.FCrmMtlGP=mg.FCrmMtlGP)t on em.FEmpID=t.FEmpID


--select * from #em order by FRndGP,FRndSEQ
--select * from #em order by FRndGP,FCrmTime

----报价单关系分组
select FIdxGp 
into #sb
from(
--select sb.FBMtlGroup,mg.FCrmMtlGP,mg.FIdxGp 
--from(select distinct FBMtlGroup from ora_CRM_SaleOfferBPR where FID=@FCrmSOID)sb 
--inner join #mg mg on charindex(','+convert(varchar,sb.FBMtlGroup)+',',mg.FCrmMtlGP)>0 
select sb.FMtlGroup,mg.FCrmMtlGP,mg.FIdxGp 
from(select distinct FMtlGroup from ora_CRM_SaleOfferEntry where FID=@FCrmSOID)sb 
inner join #mg mg on charindex(','+convert(varchar,sb.FMtlGroup)+',',mg.FCrmMtlGP)>0 

)t
group by FIdxGp 

----查找报价员 分组下最早分派任务日期,联表用户 
select ROW_NUMBER() over(order by em.FEmpID)FSEQ,em.*
into #u
from #sb sb 
inner join #em em on sb.FIdxGp=em.FRndGp 
where FRndSeq=1 

----写入报价单 报价员1-10，变更选定报价员报价分派时间
--select * from #u
--select * from ora_CRM_SaleOffer where FID=110170

if exists(select * from #u where FSEQ=1)
update ora_CRM_SaleOffer set FChecker1=(select FUserID from #u where FSEQ=1) where FID=@FCrmSOID
if exists(select * from #u where FSEQ=2)
update ora_CRM_SaleOffer set FChecker2=(select FUserID from #u where FSEQ=2) where FID=@FCrmSOID
if exists(select * from #u where FSEQ=3)
update ora_CRM_SaleOffer set FChecker3=(select FUserID from #u where FSEQ=3) where FID=@FCrmSOID
if exists(select * from #u where FSEQ=4)
update ora_CRM_SaleOffer set FChecker4=(select FUserID from #u where FSEQ=4) where FID=@FCrmSOID
if exists(select * from #u where FSEQ=5)
update ora_CRM_SaleOffer set FChecker5=(select FUserID from #u where FSEQ=5) where FID=@FCrmSOID
if exists(select * from #u where FSEQ=6)
update ora_CRM_SaleOffer set FChecker6=(select FUserID from #u where FSEQ=6) where FID=@FCrmSOID
if exists(select * from #u where FSEQ=7)
update ora_CRM_SaleOffer set FChecker7=(select FUserID from #u where FSEQ=7) where FID=@FCrmSOID
if exists(select * from #u where FSEQ=8)
update ora_CRM_SaleOffer set FChecker8=(select FUserID from #u where FSEQ=8) where FID=@FCrmSOID
if exists(select * from #u where FSEQ=9)
update ora_CRM_SaleOffer set FChecker9=(select FUserID from #u where FSEQ=9) where FID=@FCrmSOID
if exists(select * from #u where FSEQ=10)
update ora_CRM_SaleOffer set FChecker10=(select FUserID from #u where FSEQ=10) where FID=@FCrmSOID

--select e.FID,e.F_ora_CrmTime 
update e set e.F_ora_CrmTime=getdate()
from t_hr_EmpInfo e 
inner join #u u on e.FID=u.FEmpID 

select 'OK' FBackM,'' FBackMsg

--select * from #e	--员工+CRM大类授权
--select * from #em	--员工+CRM大类授权集合串
--select * from #mg	--大类集合串排序
--select * from #sb	--指定报价单对应的授权分组
--select * from #u	--报价单 筛选指派报价员

--drop table #e
--drop table #em
--drop table #mg
--drop table #sb
--drop table #u
end
-------------------
-- exec proc_cztyCrm_RndOffer @FCrmSOID=110170
