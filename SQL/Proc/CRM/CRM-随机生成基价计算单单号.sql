-- CRM-随机生成基价计算单单号
create proc [dbo].[proc_cztyCrm_GetBPRndNo]
@FTag varchar(50)
as
begin
set nocount on
declare @newNo varchar(50)
select @newNo=dbo.fun_BosRnd_addSpace(convert(varchar,isnull(convert(int, replace(max(FBILLNO),@FTag,'')),0)+1),@FTag,'',3)  
from ora_CRM_BPRnd where FBILLNO like(@FTag+'%')

select @newNo FBillNo

end
----------
-- exec proc_cztyCrm_GetBPRndNo @FTag='CX000001000006'
