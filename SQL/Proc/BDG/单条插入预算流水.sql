--单条插入预算流水，并更新月度预算明细
alter proc [dbo].[proc_czly_InsertBudgetFlowS](
	@FBraOffice int, --分公司
	@FYear int = 0,
	@FMonth int = 0,
	@FDSrcType varchar(30),   --立项｜报销｜应付｜调整
	@FDSrcAction varchar(30), --提交｜审核｜撤回｜反审核 | 追加
	@FDSrcBillID varchar(50), --源单标识
	@FDSrcFID int,            --源单据FID
	@FDSrcBNo varchar(30),    --源单单号
	@FDSrcEntryID int=0,      --源单据FEntryID，无表体为0
	@FDSrcSEQ int=0,          --源单单号，无表体为0
	@FDCostPrj int,           --费用项目
	@FPreCost decimal(18,2),  --费用变动(预计)
	@FReCost decimal(18,2)=0, --费用变动(实际)
	@FNote varchar(2000)=''
)
AS
BEGIN

IF OBJECT_ID('tempdb.dbo.#BudgetFlow_temp','U') IS NOT NULL DROP TABLE dbo.#BudgetFlow_temp;
IF OBJECT_ID('tempdb.dbo.#BudgetFlow_temp2','U') IS NOT NULL DROP TABLE dbo.#BudgetFlow_temp2;
/*
declare @FNote varchar(2000)=''
declare @FBraOffice int=249319
declare @FYear int=0
declare @FMonth int=0
--ID列
declare @FDSrcType varchar(30)='立项' --立项｜报销｜应付｜调整
declare @FDSrcAction varchar(30)='审核' --提交｜审核｜撤回｜反审核 | 关闭 | 追加
declare @FDSrcBillID varchar(50)='k0c30c431418e4cf4a60d241a18cb241c' --源单标识
declare @FDSrcFID int=100418 --源单据FID
declare @FDSrcEntryID int=100413 --源单据FEntryID，无表体为0
declare @FDSrcBNo varchar(30)='1101900177' --源单单号
declare @FDSrcSEQ int=1 --源单行号
--费用列
declare @FDCostPrj int=201926 --费用项目
declare @FPreCost decimal(18,2)=123 --费用变动(预计)
declare @FReCost decimal(18,2)=123 --费用变动(实际)
*/

--流水插入时间
declare @FDActDate datetime = GETDATE() 
--选择最大年度且最大月份的月度明细单
if @FYear=0 set @FYear=YEAR(@FDActDate)
if @FMonth=0 set @FMonth=MONTH(@FDActDate)

--预算明细表FID
declare @FID int=(select FID from ora_BDG_BudgetMD where FYEAR=@FYear and FMONTH=@FMonth and FBRAOFFICE=@FBraOffice)
--月明细表的FEntryID
declare @FDEID int=(select FEntryID from ora_BDG_BudgetMDEntry where FID=@FID and FCostPrj=@FDCostPrj)
--预算流水临时表
create table #BudgetFlow_temp(
	FID int,FDEntryID int,FDSEQ int,FDEID int,
	FDSrcType varchar(30),FDSrcBillID varchar(50),
	FDSrcFID int,FDSrcEntryID int,FDSrcBNo varchar(30),FDSrcSEQ int,
	FDSrcAction varchar(30),FDCostPrj int,
	FDBdgAdj decimal(18,2),FDBdgOcc decimal(18,2),FDBdgUse decimal(18,2),
	FDActDate datetime,FNote varchar(400)
)

declare @FDBdgAdj decimal(18,2)=0 --影响调整
declare @FDBdgOcc decimal(18,2)=0 --影响占用
declare @FDBdgUse decimal(18,2)=0 --影响使用

if @FDSrcType = '调整' 
begin
	set @FDBdgAdj=@FPreCost --调整+D
	set @FNote+=@FDSrcType+@FDSrcAction
end
else	
if @FDSrcType = '立项' and @FDSrcAction='提交'
begin
	set @FDBdgOcc=@FPreCost --占用+A1
	set @FNote+=@FDSrcType+@FDSrcAction
end
else
if @FDSrcType = '立项' and @FDSrcAction='撤销'
begin
	set @FNote+=@FDSrcType+@FDSrcAction
	set @FDBdgOcc=-@FPreCost --程序端不处理正负,占用-A1
end
else
if @FDSrcType = '立项' and @FDSrcAction='审核'
begin
	set @FNote+=@FDSrcType+@FDSrcAction+'，调整占用'
	set @FDBdgOcc=@FReCost --占用，此部分应该传入 实际金额-预计金额
end
else
if @FDSrcType = '立项' and @FDSrcAction='反审核'
begin
	set @FNote+=@FDSrcType+@FDSrcAction
	set @FDBdgOcc=-@FReCost --程序端不处理正负,占用-A2
end
else
if @FDSrcType = '立项' and @FDSrcAction='关闭'
begin
	set @FNote+=@FDSrcType+@FDSrcAction
	set @FDBdgOcc=-@FReCost --程序端不处理正负,占用-A2
end
else
if @FDSrcType = '资金' and @FDSrcAction='审核'
begin
	set @FNote+=@FDSrcType+@FDSrcAction
	set @FDBdgUse=@FReCost --使用+B2
end
else
if @FDSrcType = '资金' and @FDSrcAction='反审核'
begin
	set @FNote+=@FDSrcType+@FDSrcAction
	set @FDBdgUse=-@FReCost --使用-B2
end

--插入流水临时表
insert into #BudgetFlow_temp values
(
	@FID,0,0,@FDEID,@FDSrcType,@FDSrcBillID,@FDSrcFID,@FDSrcEntryID,@FDSrcBNo,
	@FDSrcSEQ,@FDSrcAction,@FDCostPrj,@FDBdgAdj,@FDBdgOcc,@FDBdgUse,@FDActDate,@FNote
)

--生成FEntryID和FDSEQ
declare @MaxSEQ int=(select MAX(FDSEQ) from ora_BDG_BudgetMDDtl where FID=@FID)
set @MaxSEQ=isnull(@MaxSEQ, 0)

--表体FEntryID
declare @FEntryID int = ident_current('Z_ora_BDG_BudgetMDDtl')+1
declare @eid int = (select count(*) from #BudgetFlow_temp)+@FEntryID
DBCC CHECKIDENT (Z_ora_BDG_BudgetMDDtl,RESEED, @eid)

update bf set bf.FDEntryID=t.rowid+@FEntryID, bf.FDSEQ=t.rowid+@MaxSEQ
from (
	select ROW_NUMBER() over(order by FID) as rowid, FID from #BudgetFlow_temp
) t
inner join #BudgetFlow_temp bf on t.FID=bf.FID

-- select * from #BudgetFlow_temp

--临时流水插入正式表
insert into ora_BDG_BudgetMDDtl(
	FID,FDEntryID,FDSEQ,FDEID,FDSrcType,FDSrcBillID,FDSrcFID,FDSrcEntryID,FDSrcBNo,
	FDSrcSEQ,FDSrcAction,FDCostPrj,FDBdgAdj,FDBdgOcc,FDBdgUse,FDActDate,FNote
)
select 
	FID,FDEntryID,FDSEQ,FDEID,FDSrcType,FDSrcBillID,FDSrcFID,FDSrcEntryID,FDSrcBNo,
	FDSrcSEQ,FDSrcAction,FDCostPrj,FDBdgAdj,FDBdgOcc,FDBdgUse,FDActDate,FNote
from #BudgetFlow_temp

--由临时流水表更新表头
select @FDBdgAdj=SUM(FDBdgAdj),@FDBdgOcc=SUM(FDBdgOcc),@FDBdgUse=SUM(FDBdgUse) from #BudgetFlow_temp

update ora_BDG_BudgetMD 
set FAdjBdg=FAdjBdg+@FDBdgAdj,FOccMon=FOccMon+@FDBdgOcc,FUseMon=FUseMon+@FDBdgUse,
	FUseBal=FMonBdg+FBegBdg+FAdjBdg+@FDBdgAdj-FUseMon, --预算实际余额 FEUseBal=FEMonBdg+FEBegBdg+FEAdjBdg-FEUseMon
	FOccBal=FMonBdg+FBegBdg+FAdjBdg+@FDBdgAdj-FUseMon-FOccMon-@FDBdgOcc --预算占用余额 FEOccBal=FEUseBal-FEOccMon
where FID=@FID

--更新表体
select FDCostPrj, SUM(FDBdgAdj) FDBdgAdj, SUM(FDBdgOcc) FDBdgOcc, SUM(FDBdgUse) FDBdgUse 
into #BudgetFlow_temp2
from #BudgetFlow_temp group by FDCostPrj

update ora_BDG_BudgetMDEntry
set FEAdjBdg=FEAdjBdg+t.FDBdgAdj, --本月调整
	FEOccMon=FEOccMon+t.FDBdgOcc, --本月占用
	FEUseMon=FEUseMon+t.FDBdgUse,  --本月使用
	FEUseBal=FEMonBdg+FEBegBdg+FEAdjBdg+t.FDBdgAdj-FEUseMon-t.FDBdgUse, --预算实际余额
	FEOccBal=FEMonBdg+FEBegBdg+FEAdjBdg+t.FDBdgAdj-FEUseMon-t.FDBdgUse-FEOccMon-t.FDBdgOcc --预算占用余额
from ora_BDG_BudgetMDEntry be
inner join #BudgetFlow_temp2 t on be.FCostPrj=t.FDCostPrj


END

/*
EXEC proc_czly_InsertBudgetFlowS 
	@FBraOffice = '100680', @FDSrcType = '立项', 
	@FDSrcAction = '审核', @FDSrcBillID = 'k0c30c431418e4cf4a60d241a18cb241c', 
	@FDSrcFID = '100014', @FDSrcBNo = '1102100005', @FDSrcEntryID = '100014', 
	@FDSrcSEQ = '1', @FDCostPrj = '101806', 
	@FPreCost = '1000.0000000000', @FReCost = '0', 
	@FNote = '出差申请 '

*/  
