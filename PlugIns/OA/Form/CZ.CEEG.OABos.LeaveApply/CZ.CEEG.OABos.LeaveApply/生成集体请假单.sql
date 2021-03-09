
-- 集体请假
ALTER PROC proc_czly_AllLeave(
	@FCreatorID BIGINT,   -- 创建用户
	@FleaveType INT,      -- 请假类型
	@FBeginDt DATETIME,
	@FEndDt DATETIME,
	@FBeginFrame INT,
	@FEndFrame INT,
	@FBeginTime DATETIME,
	@FEndTime DATETIME,
	@FRemarks VARCHAR(255),
	@FDays DECIMAL(18, 2)
)
AS
BEGIN

SET NOCOUNT ON

--declare
--@FCreatorID BIGINT='100560'
--,@FleaveType INT='6'
--,@FBeginDt DATETIME='2020-12-28 00:00:00'
--,@FEndDt DATETIME='2020-12-28 00:00:00'
--,@FBeginFrame INT='1'
--,@FEndFrame INT='2'
--,@FBeginTime DATETIME='2020-12-28 08:30:00'
--,@FEndTime DATETIME='2020-12-28 12:00:00'
--,@FRemarks VARCHAR(255)='测试'
--,@FDays DECIMAL(18, 2)='1.0'


declare @Date datetime=GETDATE()
declare @Year int=YEAR(@Date)

declare @FID int=ident_current('Z_ora_t_LeaveHead')+1
declare @max_num int=(select MAX(RIGHT(FBILLNO,5))+1 from ora_t_LeaveHead)
declare @FBILLNO varchar(30)='122'+RIGHT(@Year,2)+dbo.fun_BosRnd_addSpace(@max_num,'','',5)
declare @FAPPLYID int=0
declare @FORGID int=0
declare @FDEPTID int=0
declare @F_ORA_POST int=0
declare @FISORIGIN int=0      --期初单标记

select @FAPPLYID=e.FID,@FORGID=es.FWORKORGID,@FDEPTID=es.FDeptID,@F_ORA_POST=es.FPostID
from (select * from T_SEC_USER where FUSERID=@FCreatorID) u
inner join V_bd_ContactObject c on u.FLinkObject=c.FID
inner join T_HR_EMPINFO e on c.FNumber=e.FNumber
inner join T_BD_STAFFTEMP es on e.FID=es.FID and es.FIsFirstPost='1'  
inner join T_BD_STAFF s on es.FSTAFFID=s.FSTAFFID and s.FDOCUMENTSTATUS='C' and s.FFORBIDSTATUS='A'

declare @FEntryID int=ident_current('Z_ora_t_Leave')+1

-- 获取员工列表，生成临时表
select 
	ROW_NUMBER() over(order by e.FID) FSEQ, 0 as FEntryID,
	e.FID FName, es.FDeptID FDept, es.FPostID FPost
into #temp
from T_HR_EMPINFO e 
inner join T_BD_STAFFTEMP es on e.FID=es.FID and es.FIsFirstPost='1'  
inner join T_BD_STAFF s on es.FSTAFFID=s.FSTAFFID and s.FDOCUMENTSTATUS='C' and s.FFORBIDSTATUS='A'

update #temp set FEntryID=FSEQ+@FEntryID


begin tran
begin try
--生成表头
INSERT INTO ora_t_LeaveHead(
	FID,FBILLNO,FDOCUMENTSTATUS,FCREATEDATE,FCREATORID,FORGID,FAPPLYID,FDEPTID,F_ORA_POST,FISORIGIN,F_ORA_REMARKS
)
VALUES(
	@FID,@FBILLNO,'C',@Date,@FCreatorID,@FORGID,@FAPPLYID,@FDEPTID,@F_ORA_POST,@FISORIGIN,@FRemarks
)
DBCC CHECKIDENT (Z_ora_t_LeaveHead,RESEED,@FID)
--生成表体
INSERT INTO ora_t_Leave(
	FID,FEntryID,FSEQ,FLEAVETYPE,FSTARTDATE,FSTARTTIMEFRAME,FENDDATE,FENDTIMEFRAME,FDAYNUM,FNAME,FPOST,FDEPT,FSTIME,FETIME
)
SELECT 
	@FID,FEntryID,FSEQ,@FLeaveType,@FBeginDt,@FBeginFrame,@FEndDt,@FEndFrame,@FDays,FName,FPost,FDept,@FBeginTime,@FEndTime
FROM #temp
DBCC CHECKIDENT (Z_ora_t_Leave,RESEED,@FEntryID)
-- 刷新单据规则最大码
update bc set bc.FNUMMAX=@max_num
from T_BAS_BILLCODES bc
inner join T_BAS_BILLCODERULE bcr on bcr.FRULEID=bc.FRULEID
where bcr.FBILLFORMID='kbea624189d8e4d829b68340507eda196'
and FBYVALUE='20{{{{{0}}}'

end try
begin catch
	if(@@trancount>0) rollback tran --回滚
end catch
if(@@trancount>0) commit tran

drop table #temp

select @FID AS FID

END

/*
exec proc_czly_AllLeave
	@FCreatorID='{}',
	@FleaveType='{}',
	@FBeginDt='{}',
	@FEndDt='{}',
	@FBeginFrame='{}',
	@FEndFrame='{}',
	@FBeginTime='{}',
	@FEndTime='{}',
	@FRemarks='{}',
	@FDays='{}'
*/