--资金结转
CREATE proc [dbo].[proc_czly_CapitalCarryForward](
	@FID int,
	@FCreatorID int,
	@FCreateOrgId int
)
AS
BEGIN

/*
declare @FID int
declare @FCreatorID int
declare @FCreateOrgId int --创建组织
*/

declare @FYear int --年度
declare @FMonth int --月份
declare @FBraOffice int --分公司

declare @Date datetime=GETDATE()
declare @FCurrency int

select @FYear=FYEAR,@FMonth=FMONTH+1,@FBraOffice=FBRAOFFICE,@FCurrency=FCURRENCYCN
from ora_BDG_CapitalMD where FID=@FID

--生成FID
declare @FID_CapitalMD int = ident_current('Z_ora_BDG_CapitalMD') + 1
--生成单据编号
declare @prefix varchar(50) --前缀
declare @serial_num int --流水号
declare @FBILLNO_CapitalMD varchar(30) --单据编号
select @prefix=CONVERT(VARCHAR(10),YEAR(@Date)) + dbo.fun_BosRnd_addSpace(MONTH(@Date),'','',2) + dbo.fun_BosRnd_addSpace(DAY(@Date),'','',2)
select @serial_num=MAX(CONVERT(INT,RIGHT(FBILLNO, 4)))+1 from ora_BDG_CapitalMD where LEFT(FBILLNO, 4) = LEFT(@prefix, 4)
select @serial_num=isnull(@serial_num, 1)
select @FBILLNO_CapitalMD=dbo.fun_BosRnd_addSpace(@serial_num, @prefix, '', 4)

begin tran -- 开启事务
begin try

INSERT INTO ora_BDG_CapitalMD(FID,FBILLNO,FDOCUMENTSTATUS,FCREATORID,FCREATEDATE,FCREATEORGID,FCURRENCYCN,FYEAR,FMONTH,FBRAOFFICE,
	FBEGCPT, --上月结转余额
	FCPTOCC, --上月结转占用
	FOCCMON, --已占用资金
	FCPTFZN, --已冻结资金
	FOCCBAL, --资金占用余额
	FUSEBAL  --资金实际余额
)
SELECT @FID_CapitalMD,@FBILLNO_CapitalMD,'C',@FCreatorID,@Date,@FCreateOrgId,@FCurrency,@FYear,@FMonth,@FBraOffice,
	cmd.FUseBal,
	cmd.FOccMon,
	cmd.FOccMon,
	cmd.FCptFzn,
	cmd.FOccBal,
	cmd.FUseBal
FROM ora_BDG_CapitalMD cmd WHERE FID=@FID

DBCC CHECKIDENT (Z_ora_BDG_CapitalMD,RESEED,@FID_CapitalMD)


SELECT @FID_CapitalMD FID, 0 FEntryID, FSEQ, FEYEAR, FEMONTH+1 FEMONTH, FEBRAOFFICE, FECPTTYPE,
	   cmde.FEUSEMON FEBEGCPT,  --上月结转余额
	   cmde.FEOCCMON FEBEGOCC,  --上月结转占用
	   FEOCCMON,  --已占用资金
	   FECPTFZN,  --已冻结资金
	   FEOCCBAL,  --资金占用余额
	   FEUSEMON   --资金实际余额
INTO #CapitalMDEntry
FROM ora_BDG_CapitalMDEntry cmde where cmde.FID=@FID

declare @FEntryID int --表体FEntryID
set @FEntryID = ident_current('Z_ora_BDG_CapitalMDEntry') + 1
update #CapitalMDEntry set FEntryID=@FEntryID+FSEQ
select @FEntryID=isnull(MAX(FEntryID),@FEntryID) from #CapitalMDEntry

INSERT INTO ora_BDG_CapitalMDEntry(
	FID,FEntryID,FSEQ,FEYEAR,FEMONTH,FEBRAOFFICE,FECPTTYPE,
	FEBEGCPT,FEBEGOCC,FEOCCMON,FECPTFZN,FEOCCBAL,FEUSEMON
)
SELECT
	FID,FEntryID,FSEQ,FEYEAR,FEMONTH,FEBRAOFFICE,FECPTTYPE,
	FEBEGCPT,FEBEGOCC,FEOCCMON,FECPTFZN,FEOCCBAL,FEUSEMON
FROM #CapitalMDEntry

DBCC CHECKIDENT (Z_ora_BDG_CapitalMDEntry,RESEED,@FEntryID)

--更新月预算明细复选框，已结算
update ora_BDG_CapitalMD set FIsTurn=1 where FID=@FID
end try
begin catch
	if(@@trancount>0) rollback tran --回滚
end catch
if (@@trancount>0) commit tran

END

--exec proc_czly_CapitalCarryForward @FID='100051',@FCreatorID='100621',@FCreateOrgId='249319'
