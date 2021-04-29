--生成预算
alter proc [dbo].[proc_czly_GeneBuget]( 
	@FID_SalePlan int, --年销售计划内码
	@FCreatorId int, --创建人ID
	@FCreateOrgId int --创建组织
)
AS
begin
--declare @FID_SalePlan int =100024 --年销售计划内码,传入
declare @FBudgetMon decimal(18,2) --月预算额度
declare @FYear int --年度
declare @FBraOffice int --分公司
declare @FBegMon int --起始月份
declare @FEndMon int --截止月份
declare @FAllRate decimal(18,2) --总费率
declare @FSalePlanYear decimal(18,2) --年度销售计划
declare @FBudgetYear decimal(18,2) --年度预算金额
declare @FSeq int --表体序号
declare @FCurrencyID int --本位币

IF OBJECT_ID('tempdb.dbo.#SalePlanEntry_temp','U') IS NOT NULL DROP TABLE dbo.#SalePlanEntry_temp;
IF OBJECT_ID('tempdb.dbo.#CostRate_temp','U') IS NOT NULL DROP TABLE dbo.#CostRate_temp;
IF OBJECT_ID('tempdb.dbo.#SalePlanAnz_temp','U') IS NOT NULL DROP TABLE dbo.#SalePlanAnz_temp;
IF OBJECT_ID('tempdb.dbo.#BudgetMDEntry_temp','U') IS NOT NULL DROP TABLE dbo.#BudgetMDEntry_temp;
IF OBJECT_ID('tempdb.dbo.#CapitalMDEntry_temp','U') IS NOT NULL DROP TABLE dbo.#CapitalMDEntry_temp;

--获取起止月份、年度、分公司、年度销售计划
select @FBegMon=sp.FBEGMON,@FEndMon=sp.FENDMON,@FYear=FYEAR,
@FBraOffice=sp.FBRAOFFICE,@FSalePlanYear=sp.FSALEPLANYEAR,@FCurrencyID=sp.FCURRENCYCN
from ora_BDG_SalePlan sp where sp.FID=@FID_SalePlan
--计算年度预算金额
select @FAllRate=FAllRate from ora_BDG_CostRate cr where cr.FYEAR=@FYear and cr.FBRAOFFICE=@FBraOffice
set @FBudgetYear=@FSalePlanYear*@FAllRate/100.0
--刷新表单年度预算金额数据
update ora_BDG_SalePlan set FBudgetYear=@FBudgetYear where FID=@FID_SalePlan

--0--生成年度销售计划明细信息
create table #SalePlanEntry_temp( --年度销售计划明细临时表
	FID int,
	FEntryID int,
	FSEQ int,
	FMonth int,
	FSalePlanMon decimal(18,2),
	FBudgetMon decimal(18,2),
)
declare @month int = @FBegMon
declare @FEntryID_SPE int --表体主键
set @FSeq = 0
while (@month <= @FEndMon)
begin
	--生成entryid
	set @FSeq = @FSeq + 1
	set @FEntryID_SPE = ident_current('Z_ora_BDG_SalePlanEntry') + 1
	insert into #SalePlanEntry_temp(FID,FEntryID,FSEQ,FMonth,FSalePlanMon,FBudgetMon)
	values(@FID_SalePlan,@FEntryID_SPE,@FSeq,@month,@FSalePlanYear/(@FEndMon-@FBegMon+1),@FBudgetYear/(@FEndMon-@FBegMon+1))
	DBCC CHECKIDENT (Z_ora_BDG_SalePlanEntry,RESEED,@FEntryID_SPE)
	set @month = @month + 1
end
--select * from #SalePlanEntry_temp
--临时表插入正式表
delete from ora_BDG_SalePlanEntry where FID=@FID_SalePlan
insert into ora_BDG_SalePlanEntry(FID,FEntryID,FSEQ,FMonth,FSalePlanMon,FBudgetMon)
select FID,FEntryID,FSEQ,FMonth,FSalePlanMon,FBudgetMon
from #SalePlanEntry_temp
--1--首先生成预算拆分表
--查询费用系数表
create table #CostRate_temp(
	cost_prj int,
	cost_rate decimal(18,6)
)

insert into #CostRate_temp(cost_prj,cost_rate)
select FCOSTPRJ, FCOSTRATE
from ora_BDG_CostRateEntry cre
inner join ora_BDG_CostRate cr on cre.FID=cr.FID
where cr.FYEAR=@FYear and cr.FBRAOFFICE=@FBraOffice
order by FCOSTPRJ

select tmp.FID, 0 as FAEntryID, ROW_NUMBER() over (order by tmp.FMonth,cre.FCOSTPRJ) FASEQ,
	tmp.FEntryID as FASrcEID, tmp.FMonth as FAMonth,
	cre.FCOSTPRJ as FACostPrj, cre.FCOSTRATE as FACostRate, 
	tmp.FSalePlanMon*cre.FCOSTRATE/100.0 as FAPrjBudget --月销售计划*费率
into #SalePlanAnz_temp --临时存放预算拆分表信息
from #SalePlanEntry_temp tmp, ora_BDG_CostRateEntry cre
inner join ora_BDG_CostRate cr on cre.FID=cr.FID
where cr.FYEAR=@FYear and cr.FBRAOFFICE=@FBraOffice
order by FAMonth,FACostPrj
--获取FEntryID,并批量插入
declare @FAEntryID int --ora_BDG_SalePlanAnz的FAEntryID
set @FAEntryID = ident_current('Z_ora_BDG_SalePlanAnz')
update #SalePlanAnz_temp set FAEntryID = @FAEntryID+FASEQ
--计算最大FEntryID，回插入Z表
select @FAEntryID=MAX(FAEntryID)+1 from #SalePlanAnz_temp
DBCC CHECKIDENT (Z_ora_BDG_SalePlanAnz,RESEED,@FAEntryID)
--临时表插入正式表
delete from ora_BDG_SalePlanAnz where FID=@FID_SalePlan
insert into ora_BDG_SalePlanAnz(FID,FAEntryID,FASEQ,FASrcEID,FAMonth,FACostPrj,FACostRate,FAPrjBudget)
select FID,FAEntryID,FASEQ,FASrcEID,FAMonth,FACostPrj,FACostRate,FAPrjBudget
from #SalePlanAnz_temp

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
--select @prefix,@serial_num,@FBILLNO_BudgetMD
--select dbo.fun_BosRnd_addSpace(1,'20191031','',4)
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
	select FAMonth, FACostPrj, FAPrjBudget from #SalePlanAnz_temp where FAMonth=@FBegMon
	union all
	-- 这里并上未选择的费用项目
	select @FBegMon FAMonth, FEXPID FACostPrj, 0 FAPrjBudget from T_BD_EXPENSE 
	where FEXPID NOT IN (select distinct FACostPrj from #SalePlanAnz_temp)
)t

declare @FEntryID int --表体FEntryID
set @FEntryID = ident_current('Z_ora_BDG_BudgetMDEntry') + 1
update #BudgetMDEntry_temp set FEntryID=@FEntryID+FSEQ
select @FEntryID=MAX(FEntryID) from #BudgetMDEntry_temp
DBCC CHECKIDENT (Z_ora_BDG_BudgetMDEntry,RESEED,@FEntryID)
--临时表插入正式表
insert into ora_BDG_BudgetMDEntry(FID,FEntryID,FSEQ,FEYEAR,FEMONTH,FEBRAOFFICE,FCOSTPRJ,FEOccBal,FEUseBal,FEMonBdg)
select FID,FEntryID,FSEQ,FEYEAR,FEMONTH,FEBRAOFFICE,FCOSTPRJ,FEOccBal,FEUseBal,FEMonBdg from #BudgetMDEntry_temp

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

--更新年销售计划复选框，已生成预算
update ora_BDG_SalePlan set FIsDoBG=1 where FID=@FID_SalePlan

end

--exec proc_czly_GeneBuget @FID_SalePlan=100031,@FCreatorId=100621,@FCreateOrgId=249319
