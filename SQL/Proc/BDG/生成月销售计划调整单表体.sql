--生成月销售计划调整单表体
ALTER PROC [dbo].[proc_czly_GeneSalePlanAdjEntry](
	@FID int
)
AS
BEGIN

--declare @FID int=100008

declare @FBraOffice int --=249319
declare @FYear int
declare @FMonth int

declare @FMonPlanAdj decimal(18,2)

select @FMonPlanAdj=FMonPlanAdj,@FBraOffice=FBraOffice,
	   @FYear=FYear,@FMonth=FMonth
from ora_BDG_SalePlanAdj where FID=@FID

select @FID FID, 0 FEntryID, ROW_NUMBER() over (order by cre.FCOSTPRJ) FSEQ,
FCostPrj, FCostRate, @FMonPlanAdj*FCostRate/100.0 FPrjBudget
into #CostRate_temp
from ora_BDG_CostRateEntry cre
inner join ora_BDG_CostRate cr on cre.FID=cr.FID
where cr.FYEAR=@FYear and cr.FBRAOFFICE=@FBraOffice
order by cre.FCOSTPRJ

--select * from ora_BDG_CostRate

declare @FEntryID int=ident_current('Z_ora_BDG_SalePlanAdjEntry')+1
update #CostRate_temp set FEntryID=FSEQ+@FEntryID
--select * from #CostRate_temp

begin tran --开启事务
begin try
	--插入正式表
	delete from ora_BDG_SalePlanAdjEntry where FID=@FID
	insert into ora_BDG_SalePlanAdjEntry select * from #CostRate_temp
	--回插Z表ID
	select @FEntryID=MAX(FEntryID) from #CostRate_temp
	DBCC CHECKIDENT (Z_ora_BDG_SalePlanAdjEntry,RESEED,@FEntryID)

	commit tran
end try
begin catch
	if(@@trancount>0) rollback tran --回滚
end catch

END

--exec proc_czly_GeneSalePlanAdjEntry @FID=100008
