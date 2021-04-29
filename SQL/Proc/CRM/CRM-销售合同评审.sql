-- CRM-销售合同评审
CREATE proc [dbo].[proc_cztyCrm_Contract]
@FCrmSOID	int	--ora_CRM_Contract[FID]
as
begin
set nocount on
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

----验证是否已做授权
select t.*,ml.FName FMGName 
into #t
from(select distinct s.FBillNO,se.FMTLGROUP 
	--se.FEntryID,se.FSEQ,se.FMTLGROUP,e.FEmpID,u.FUserID
	from(select FID,FBillNo from ora_CRM_Contract where FID=@FCrmSOID)s 
	inner join ora_CRM_ContractEntry se on s.FID=se.FID
	left join #e e on se.FMTLGROUP=e.FCrmMtlGP 
	left join T_HR_EMPINFO ei on e.FEmpID=ei.FID
	left join V_bd_ContactObject co on ei.FNUMBER=co.FNUMBER 
	left join T_SEC_USER u on co.FID=u.FLinkObject 
	where isnull(u.FUserID,-1)=-1 
)t 
inner join ora_CrmBD_MtlGroup_L ml on t.FMTLGROUP=ml.FID and ml.FLocaleID=2052

DECLARE @val NVARCHAR(MAX)= ''
SELECT @val = case @val when '' then FBillNo+'未被授权分类:'+FMGName else  (@val + ',' + FMGName) end FROM #t 
--where id=2 

select case @val when '' then 'OK' else 'ERR' end as FBackM, @val FBackMsg

end
--------------
--	update T_BD_STAFFTEMP set FPOSTID=157167 where fid=157140		--设为报价员 
--	update T_BD_STAFFTEMP set FPOSTID=157126 where fid=157140		--设为原岗位职务 
-- exec proc_cztyCrm_Contract @FCrmSOID=100032 