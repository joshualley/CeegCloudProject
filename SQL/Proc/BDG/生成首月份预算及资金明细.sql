--生成首月份预算及资金明细
ALTER PROC [dbo].[proc_czly_GeneFirstMon_BDG_CPT](
	@FID_SalePlan int, --年销售计划内码
	@FCreatorId int, --创建人ID
	@IsGeneCpt int=1 --是否生成资金,默认生成
)
AS
BEGIN

declare @FCreateOrgId int --创建组织
declare @FBudgetMon decimal(18,2) --月预算额度
declare @FYear int --年度
declare @FBraOffice int --分公司
declare @FBegMon int --起始月份
declare @FCurrencyID int --本位币

--1--获取起止月份、年度、分公司、年度销售计划
select @FBegMon=sp.FBEGMON,--@FEndMon=sp.FENDMON,
@FYear=FYEAR,@FBraOffice=sp.FBRAOFFICE,@FCurrencyID=sp.FCURRENCYCN,@FCreateOrgId=sp.FCreateOrgId
from ora_BDG_SalePlan sp where sp.FID=@FID_SalePlan

--2--然后生成起始月份预算明细表单
--获取首月月度预算金额
select @FBudgetMon=spe.FBudgetMon from ora_BDG_SalePlan sp
inner join ora_BDG_SalePlanEntry spe on sp.FID=spe.FID
where spe.FMonth=@FBegMon and sp.FID=@FID_SalePlan

--插入起始月份数据  FMonBdg 本月计划预算,FOccBal 预算占用余额,FUseBal 预算实际余额
--生成FID
declare @FID_BudgetMD int = ident_current('Z_ora_BDG_BudgetMD') + 1
--生成单据编号
declare @prefix varchar(50) --前缀
declare @serial_num int --流水号
declare @FBILLNO_BudgetMD varchar(30) --单据编号

select @prefix=CONVERT(VARCHAR(10),YEAR(GETDATE())) + dbo.fun_BosRnd_addSpace(MONTH(GETDATE()),'','',2) + dbo.fun_BosRnd_addSpace(DAY(GETDATE()),'','',2)

select @serial_num=MAX(CONVERT(INT,RIGHT(FBILLNO, 4)))+1 from ora_BDG_BudgetMD where LEFT(FBILLNO, 4) = LEFT(@prefix, 4)
select @serial_num=isnull(@serial_num, 1)

select @FBILLNO_BudgetMD=dbo.fun_BosRnd_addSpace(@serial_num, @prefix, '', 4)
--表头
insert into ora_BDG_BudgetMD(FID,FBILLNO,FDOCUMENTSTATUS,FCREATORID,FCreateDate,FCREATEORGID,
FYEAR,FMonth,FBraOffice,FMonBdg,FOccBal,FUseBal,FCURRENCYCN)
values(@FID_BudgetMD, @FBILLNO_BudgetMD, 'C', @FCreatorId, GETDATE(), @FCreateOrgId,
@FYear, @FBegMon, @FBraOffice, @FBudgetMon, @FBudgetMon, @FBudgetMon, @FCurrencyID)

DBCC CHECKIDENT (Z_ora_BDG_BudgetMD,RESEED,@FID_BudgetMD)
--表体
select @FID_BudgetMD as FID, 0 as FEntryID, ROW_NUMBER() over (order by FACostPrj) FSEQ,
@FYear as FEYEAR, FAMonth as FEMONTH, @FBraOffice as FEBRAOFFICE, FACostPrj as FCOSTPRJ,
FAPrjBudget as FEOccBal, FAPrjBudget as FEUseBal, FAPrjBudget as FEMonBdg
into #BudgetMDEntry_temp --临时存放预算明细表
from (
	select FAMonth, FACostPrj, FAPrjBudget from ora_BDG_SalePlanAnz 
	where FID=@FID_SalePlan and FAMonth=@FBegMon
	union all
	-- 这里并上未选择的费用项目
	select @FBegMon FAMonth, FEXPID FACostPrj, 0 FAPrjBudget from T_BD_EXPENSE 
	where FEXPID NOT IN (
		select distinct FACostPrj from ora_BDG_SalePlanAnz 
		where FID=@FID_SalePlan and FAMonth=@FBegMon
	)
)t

declare @FEntryID int --表体FEntryID
set @FEntryID = ident_current('Z_ora_BDG_BudgetMDEntry') + 1
update #BudgetMDEntry_temp set FEntryID=@FEntryID+FSEQ
select @FEntryID=MAX(FEntryID) from #BudgetMDEntry_temp
DBCC CHECKIDENT (Z_ora_BDG_BudgetMDEntry,RESEED,@FEntryID)
--临时表插入正式表
insert into ora_BDG_BudgetMDEntry(FID,FEntryID,FSEQ,FEYEAR,FEMONTH,FEBRAOFFICE,FCOSTPRJ,FEOccBal,FEUseBal,FEMonBdg)
select FID,FEntryID,FSEQ,FEYEAR,FEMONTH,FEBRAOFFICE,FCOSTPRJ,FEOccBal,FEUseBal,FEMonBdg
from #BudgetMDEntry_temp

if(@IsGeneCpt=1)
begin
	--3--生成首月月度资金明细
	--生成FID
	declare @FID_CapitalMD int = ident_current('Z_ora_BDG_CapitalMD') + 1
	--生成单据编号
	declare @FBILLNO_CapitalMD varchar(30) --单据编号
	select @serial_num=MAX(CONVERT(INT,RIGHT(FBILLNO, 4)))+1 from ora_BDG_CapitalMD where LEFT(FBILLNO, 8) = @prefix
	select @serial_num=isnull(@serial_num, 1)
	select @FBILLNO_CapitalMD=dbo.fun_BosRnd_addSpace(@serial_num, @prefix, '', 4)
	--表头
	insert into ora_BDG_CapitalMD(FID,FBILLNO,FDOCUMENTSTATUS,FCREATORID,FCreateDate,FCREATEORGID,
	FYEAR,FMonth,FBraOffice,FCURRENCYCN)
	values(@FID_CapitalMD, @FBILLNO_CapitalMD, 'C', @FCreatorId, GETDATE(), @FCreateOrgId,
	@FYear, @FBegMon, @FBraOffice, @FCurrencyID)

	DBCC CHECKIDENT (Z_ora_BDG_CapitalMD,RESEED,@FID_CapitalMD)

	--表体
	select @FID_CapitalMD as FID, 0 as FEntryID, ROW_NUMBER() over(order by CONVERT(int, st.FID)) FSEQ,
	@FYear as FEYEAR, @FBegMon as FEMONTH, @FBraOffice as FEBRAOFFICE, st.FID as FECptType
	into #CapitalMDEntry_temp --临时存放资金明细表
	from T_BD_SETTLETYPE st
	order by st.FID

	set @FEntryID = ident_current('Z_ora_BDG_CapitalMDEntry') + 1
	update #CapitalMDEntry_temp set FEntryID=@FEntryID+FSEQ
	select @FEntryID=MAX(FEntryID) from #CapitalMDEntry_temp

	insert into ora_BDG_CapitalMDEntry(
		FID,FEntryID,FSEQ,FEYEAR,FEMONTH,FEBRAOFFICE,FECptType
	)
	select FID,FEntryID,FSEQ,FEYEAR,FEMONTH,FEBRAOFFICE,FECptType
	from #CapitalMDEntry_temp

	DBCC CHECKIDENT (Z_ora_BDG_CapitalMDEntry,RESEED,@FEntryID)
end

--更新年销售计划复选框，已生成预算
update ora_BDG_SalePlan set FIsDoBG=1 where FID=@FID_SalePlan

END

--exec proc_czly_GeneFirstMon_BDG_CPT @FID_SalePlan='', @FCreatorId='', @IsGeneCpt=0
