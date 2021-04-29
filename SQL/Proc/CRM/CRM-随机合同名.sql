-- 随机合同名
CREATE proc [dbo].[proc_czty_RndInContractName] 
@FID int 
as
begin 
set nocount on
select distinct 1 id,FName into #t
from(select * from ora_CRM_InnerContractBPR where FID =@FID)t 
inner join ora_CrmBD_MtlGroup_L ml on t.FBMtlGroup=ml.FID and ml.FLocaleID=2052

declare @FName varchar(500)=''

select @FName=(  
select FName +',' from #t as b where b.id = a.id for xml path('')) from #t as a  
group by id  

update ora_CRM_InnerContract set FMtlGroupNames=@FName where FID=@FID 
--insert into czty_tempLog values(@FID,@FName,GETDATE())

end
------------
-- exec proc_czty_RndInContractName @FID=100046
-- select FID,FBILLNO,FMtlGroupNames from ora_CRM_InnerContract where FID=100044