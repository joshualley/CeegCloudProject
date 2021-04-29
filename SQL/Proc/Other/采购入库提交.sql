-- 采购入库提交
CREATE proc [dbo].[proc_czty_PrdInStock2Sbmt]
@FID	bigint
as
begin
set nocount on
declare @FBackM varchar(10)='',@FBackMsg varchar(2000)=''
select distinct i.FID,ie.FMoID,ie.FMoBillNo,ie.FMOEntryID 
into #t
from(select FID from T_PRD_INSTOCK where FID=@FID)i
inner join T_PRD_INSTOCKENTRY ie on i.FID=ie.FID 

--insert into #t select 1,1,'uk999999',1
--insert into #t select 2,2,'uk888888',2

select t.*,isnull(p.FCnt,0)FCnt 
into #r
from #t t 
left join(select t.FMOEntryID,COUNT(pd.FEntryID)FCnt from #t t 
	inner join T_PRD_PICKMTRLData pd on t.FMoEntryID=pd.FMoEntryID 
	inner join T_PRD_PICKMTRL p on pd.FID=p.FID and p.FDocumentStatus='C' 
group by t.FMOEntryID)p on t.FMOENTRYID=p.FMOENTRYID 
where isnull(p.FCnt,0)=0 

if not exists(select * from #r where FCnt=0)
begin
	select @FBackM='OK',@FBackMsg=''
	goto OutPut 
end

SELECT  @FBackM='Err',@FBackMsg=case @FBackMsg when '' then FMoBillNo else  (@FBackMsg + ',' + FMoBillNo) end FROM #r where FCnt=0

OutPut:
select @FBackM FBackM,@FBackMsg FBackMsg

end
-----------
-- exec proc_czty_PrdInStock2Sbmt @FID='100581'
