--月度预算结转
alter proc [dbo].[proc_czly_BugetCarryForward](
	@FIDB int,
	@FCreatorId int,
	@FCreateOrgId int --创建组织
)
AS
BEGIN

--declare @FIDB int = 100051 --上月明细内码
--declare @FCreatorId int = 100621 --创建人ID
--declare @FCreateOrgId int=249319 --创建组织
IF OBJECT_ID('tempdb.dbo.#BudgetMDEntry_temp','U') IS NOT NULL DROP TABLE dbo.#BudgetMDEntry_temp;

declare @FYear int --年度
declare @FMonth int --月份
declare @FBraOffice int --分公司
declare @FBudgetMon decimal(18,2) --当月计划预算


--计算创建人组织
--select @FCreateOrgId=FCREATEORG from T_SEC_USER where FUSERID=@FCreatorId

--获取基本数据
select @FYear=FYEAR, @FMonth=FMONTH+1, @FBraOffice=FBRAOFFICE
from ora_BDG_BudgetMD where FID=@FIDB

--获取当月计划预算
select @FBudgetMon=spe.FBudgetMon from ora_BDG_SalePlan sp
inner join ora_BDG_SalePlanEntry spe on sp.FID=spe.FID
where spe.FMonth=@FMonth and sp.FYEAR=@FYear and sp.FBRAOFFICE=@FBraOffice

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

begin tran --开启事务
begin try

--单据头
insert into ora_BDG_BudgetMD(FID,FBILLNO,FDOCUMENTSTATUS,FCREATORID,FCreateDate,FCREATEORGID,FYEAR,FMonth,FBraOffice,FCurrencyCN,
	FMonBdg, --月计划预算
	FBegOcc, --上月结转占用
	FBegBdg, --上月结转余额
	FOccMon, --已占用预算
	FOccBal, --预算占用余额
	FUseBal  --预算实际余额
)
select @FID_BudgetMD,@FBILLNO_BudgetMD,'C',@FCreatorId,GETDATE(),@FCreateOrgId,@FYear,@FMonth,@FBraOffice,bmd.FCurrencyCN,
	@FBudgetMon,
	bmd.FOccMon, --(上月)已占用预算-->上月结转占用
	bmd.FUseBal, --(上月)预算实际余额-->上月结转余额
	bmd.FOccMon, --(上月)已占用预算-->已占用预算
	bmd.FOccBal+@FBudgetMon, --(上月)预算占用余额+(本月)月计划预算-->预算占用余额
	bmd.FUseBal+@FBudgetMon  --(上月)预算实际余额+(本月)月计划预算-->预算实际余额
from ora_BDG_BudgetMD bmd where FID=@FIDB

DBCC CHECKIDENT (Z_ora_BDG_BudgetMD,RESEED,@FID_BudgetMD)

--单据体
select @FID_BudgetMD as FID, 0 as FEntryID, bmde.FSEQ as FSEQ,
	@FYear as FEYEAR, @FMonth as FEMONTH, @FBraOffice as FEBRAOFFICE, bmde.FCOSTPRJ as FCOSTPRJ,
	spa.FAPrjBudget as FEMonBdg,  --本月计划预算
	bmde.FEOccMon as FEBegOcc, --上月结转占用
	bmde.FEUseBal as FEBegBdg, --上月结转余额
	bmde.FEOccMon as FEOccMon, --已占用预算
	bmde.FEOccBal+spa.FAPrjBudget-bmde.FEOccMon as FEOccBal, --预算占用余额
	bmde.FEUseBal+spa.FAPrjBudget as FEUseBal  --预算实际余额
into #BudgetMDEntry_temp --临时存放预算明细表
from ora_BDG_BudgetMDEntry bmde
inner join ora_BDG_SalePlanAnz spa on bmde.FCOSTPRJ=spa.FACOSTPRJ
inner join ora_BDG_SalePlan sp on sp.FID=spa.FID
where bmde.FID=@FIDB and spa.FAMONTH=@FMonth and sp.FYEAR=@FYear and sp.FDocumentStatus = 'C'


declare @FEntryID int --表体FEntryID
set @FEntryID = ident_current('Z_ora_BDG_BudgetMDEntry') + 1
update #BudgetMDEntry_temp set FEntryID=@FEntryID+FSEQ
select @FEntryID=isnull(MAX(FEntryID),@FEntryID) from #BudgetMDEntry_temp

--select * from #BudgetMDEntry_temp


--临时表插入正式表
insert into ora_BDG_BudgetMDEntry(
	FID,FEntryID,FSEQ,FEYEAR,FEMONTH,FEBRAOFFICE,FCOSTPRJ,
	FEMonBdg,FEBegOcc,FEBegBdg,FEOccMon,FEOccBal,FEUseBal
)
select FID,FEntryID,FSEQ,FEYEAR,FEMONTH,FEBRAOFFICE,FCOSTPRJ,
	   FEMonBdg,FEBegOcc,FEBegBdg,FEOccMon,FEOccBal,FEUseBal
from #BudgetMDEntry_temp

DBCC CHECKIDENT (Z_ora_BDG_BudgetMDEntry,RESEED,@FEntryID)

--更新月预算明细复选框，已结算
update ora_BDG_BudgetMD set FIsTurn=1 where FID=@FIDB

commit tran

end try
begin catch
	if (@@trancount>0) rollback tran --事务回滚
end catch

END

--exec proc_czly_BugetCarryForward @FIDB=100003,@FCreatorId=100037,@FCreateOrgId=1

--update ora_BDG_BudgetMD set FIsTurn=0 where FID=100003
--select * from ora_BDG_BudgetMD
