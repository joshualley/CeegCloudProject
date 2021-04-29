--资金插流水
ALTER proc [dbo].[proc_czly_InsertCapitalFlowS](
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
	@FDCptType int,           --资金类型
	@FDCostPrj int=0,            --费用项目
	@FPreCost decimal(18,2),  --费用变动(申请)
	@FReCost decimal(18,2)=0, --费用变动(实付)
	@FNote varchar(2000)=''
)
AS
BEGIN


IF OBJECT_ID('tempdb.dbo.#CapitalFlow_temp','U') IS NOT NULL DROP TABLE dbo.#CapitalFlow_temp;

/*
declare @FNote varchar(2000)=''
declare @FBraOffice int=249319
declare @FYear int=0
declare @FMonth int=0
--ID列
declare @FDSrcType varchar(30)='冻结' --付款 | 调拨 | 冻结
declare @FDSrcAction varchar(30)='调整' --提交｜审核｜撤回｜反审核 | 调整
declare @FDSrcBillID varchar(50)='k0c30c431418e4cf4a60d241a18cb241c' --源单标识
declare @FDSrcFID int=100418 --源单据FID
declare @FDSrcEntryID int=100413 --源单据FEntryID，无表体为0
declare @FDSrcBNo varchar(30)='1101900177' --源单单号
declare @FDSrcSEQ int=1 --源单行号
--费用列
declare @FDCptType int=14 --资金类型
declare @FDCostPrj int=201926 --费用项目
declare @FPreCost decimal(18,2)=123 --费用变动(预计)
declare @FReCost decimal(18,2)=123 --费用变动(实际)
*/

declare @FDActDate datetime = GETDATE() --流水插入时间
--declare @FNote varchar(400) = '' --备注

--选择最大年度且最大月份的月度明细单
if @FYear=0 
select @FYear=MAX(FYEAR) from ora_BDG_CapitalMD
if @FMonth=0 
select @FMonth=MAX(FMONTH) from ora_BDG_CapitalMD where FYEAR=@FYear

declare @FID int --资金明细表FID
select @FID=FID from ora_BDG_CapitalMD where FYEAR=@FYear and FMONTH=@FMonth and FBRAOFFICE=@FBraOffice
--月明细表的FEntryID
declare @FDEID int --资金拆分表EntryID
select @FDEID=FEntryID from ora_BDG_CapitalMDEntry where FID=@FID and FECptType=@FDCptType

--select @FID,@FDEID,@FYear,@FMonth

create table #CapitalFlow_temp( --资金流水临时表
	FID int,FDEntryID int,FDSEQ int,FDEID int,
	FDSrcType varchar(30),FDSrcBillID varchar(50),
	FDSrcFID int,FDSrcEntryID int,FDSrcBNo varchar(30),FDSrcSEQ int,
	FDSrcAction varchar(30),FDCptType int,FDCostPrj int,
	FDCptAdj decimal(18,2),FDCptOcc decimal(18,2),FDCptUse decimal(18,2),FDCptFzn decimal(18,2),
	FDActDate datetime,FNote varchar(400)
)

declare @FDCptAdj decimal(18,2)=0 --影响调拨
declare @FDCptFzn decimal(18,2)=0 --影响冻结
declare @FDCptOcc decimal(18,2)=0 --影响占用
declare @FDCptUse decimal(18,2)=0 --影响使用

if @FDSrcType = '调拨' 
begin
	set @FDCptAdj=@FPreCost --调拨+D
	set @FNote+=@FDSrcType+@FDSrcAction
end
else
if @FDSrcType = '冻结' 
begin
	set @FDCptFzn=@FPreCost --冻结+D
	set @FNote+=@FDSrcType+@FDSrcAction
end
else	
if @FDSrcType = '付款' and @FDSrcAction='提交'
begin
	set @FDCptOcc=@FPreCost --占用+C1
	set @FNote+=@FDSrcType+@FDSrcAction
end
else
if @FDSrcType = '付款' and @FDSrcAction='驳回'
begin
	set @FNote+=@FDSrcType+@FDSrcAction
	set @FDCptOcc=-@FPreCost --占用-C1
end
else
if @FDSrcType = '付款' and @FDSrcAction='撤销'
begin
	set @FNote+=@FDSrcType+@FDSrcAction
	set @FDCptOcc=-@FPreCost --占用-C1
end
else		
if @FDSrcType = '付款' and @FDSrcAction='审核'
begin
	set @FNote=@FDSrcType+@FDSrcAction+'，撤回提交时占用'
	set @FDCptOcc=-@FPreCost --占用-C1
	--插入流水临时表
	insert into #CapitalFlow_temp values
	(
		@FID,0,0,@FDEID,@FDSrcType,@FDSrcBillID,@FDSrcFID,@FDSrcEntryID,@FDSrcBNo,
		@FDSrcSEQ,@FDSrcAction,@FDCptType,@FDCostPrj,@FDCptAdj,@FDCptOcc,@FDCptUse,@FDCptFzn,@FDActDate,@FNote
	)
	set @FNote=@FDSrcType+@FDSrcAction+'，写入实际使用'
	--设置本次使用金额
	set @FDCptOcc=0
	set @FDCptUse=@FReCost --使用+C2
end
else
if @FDSrcType = '付款' and @FDSrcAction='反审核'
begin
	set @FNote+=@FDSrcType+@FDSrcAction
	set @FDCptOcc=-@FReCost --占用-C1
end


--插入流水临时表
insert into #CapitalFlow_temp values
(
	@FID,0,0,@FDEID,@FDSrcType,@FDSrcBillID,@FDSrcFID,@FDSrcEntryID,@FDSrcBNo,
	@FDSrcSEQ,@FDSrcAction,@FDCptType,@FDCostPrj,@FDCptAdj,@FDCptOcc,@FDCptUse,@FDCptFzn,@FDActDate,@FNote
)

--生成FEntryID和FDSEQ
declare @MaxSEQ int
select @MaxSEQ=MAX(FDSEQ) from ora_BDG_CapitalMDDtl where FID=@FID
select @MaxSEQ=isnull(@MaxSEQ, 0)
declare @FEntryID int --表体FEntryID
set @FEntryID = ident_current('Z_ora_BDG_CapitalMDDtl')+1

update #CapitalFlow_temp
set #CapitalFlow_temp.FDEntryID=t.rowid+@FEntryID, 
	#CapitalFlow_temp.FDSEQ=t.rowid+@MaxSEQ
from (
	select ROW_NUMBER() over(order by FID) as rowid, FID from #CapitalFlow_temp
) t
inner join #CapitalFlow_temp cf on t.FID=cf.FID


select @FEntryID=MAX(FDEntryID) from #CapitalFlow_temp
--select * from #CapitalFlow_temp

--临时流水插入正式表
begin tran --开始事务
begin try
	insert into ora_BDG_CapitalMDDtl(
		FID,FDEntryID,FDSEQ,FDEID,FDSrcType,FDSrcBillID,FDSrcFID,FDSrcEntryID,FDSrcBNo,
		FDSrcSEQ,FDSrcAction,FDCptType,FDCostPrj,FDCptAdj,FDCptOcc,FDCptUse,FDCptFzn,FDActDate,FNote
	)
	select 
		FID,FDEntryID,FDSEQ,FDEID,FDSrcType,FDSrcBillID,FDSrcFID,FDSrcEntryID,FDSrcBNo,
		FDSrcSEQ,FDSrcAction,FDCptType,FDCostPrj,FDCptAdj,FDCptOcc,FDCptUse,@FDCptFzn,FDActDate,FNote
	from #CapitalFlow_temp
	DBCC CHECKIDENT (Z_ora_BDG_CapitalMDDtl,RESEED,@FEntryID)

	--由临时流水表更新表头
	select @FDCptAdj=SUM(FDCptAdj),@FDCptOcc=SUM(FDCptOcc),@FDCptUse=SUM(FDCptUse),@FDCptFzn=SUM(FDCptFzn) from #CapitalFlow_temp
	update ora_BDG_CapitalMD 
	set FAdjCpt=FAdjCpt+@FDCptAdj,FOccMon=FOccMon+@FDCptOcc,FUseMon=FUseMon+@FDCptUse,FCptFzn=FCptFzn+@FDCptFzn
	where FID=@FID
	update ora_BDG_CapitalMD 
	set FUseBal=FBegCpt+FAdjCpt-FUseMon --资金实际余额=上月结转余额+本月调拨-本月使用
	where FID=@FID
	update ora_BDG_CapitalMD 
	set FOccBal=FUseBal-FOccMon-FCptFzn --资金占用余额=资金实际余额-已占用资金-冻结资金
	where FID=@FID

	--更新表体
	select FDCptType, SUM(FDCptAdj) FDCptAdj, SUM(FDCptOcc) FDCptOcc, SUM(FDCptUse) FDCptUse, SUM(FDCptFzn) FDCptFzn
	into #CapitalFlow_temp2
	from #CapitalFlow_temp group by FDCptType

	update ora_BDG_CapitalMDEntry
	set FEAdjCpt=FEAdjCpt+t.FDCptAdj, --本月调拨
		FEOccMon=FEOccMon+t.FDCptOcc, --本月占用
		FEUseMon=FEUseMon+t.FDCptUse, --本月使用
		FECptFzn=FECptFzn+t.FDCptFzn  --资金冻结
	from ora_BDG_CapitalMDEntry ce
	inner join #CapitalFlow_temp2 t on ce.FECptType=t.FDCptType

	update ora_BDG_CapitalMDEntry 
	set FEUseBal=FEBegCpt+FEAdjCpt-FEUseMon --资金实际余额 
	where FID=@FID
	update ora_BDG_CapitalMDEntry 
	set FEOccBal=FEUseBal-FEOccMon-FECptFzn --预算占用余额
	where FID=@FID

commit tran
end try
begin catch
	if(@@trancount>0) rollback tran --回滚
end catch

END

/*
exec proc_czly_InsertCapitalFlowS
	@FBraOffice=249319,
	@FDSrcType='付款',
	@FDSrcAction='审核',
	@FDSrcBillID='ora_BDG_BudgetMD',
	@FDSrcFID='1',
	@FDSrcBNo='No123',
	@FDSrcEntryID='0',
	@FDSrcSEQ='0',
	@FDCptType='1',
	@FDCostPrj='201926',
	@FPreCost='3000',
	@FReCost='2900'
*/
