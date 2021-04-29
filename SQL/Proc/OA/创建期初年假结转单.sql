--创建期初年假结转单
CREATE proc [dbo].[proc_czly_CreateInitLeave]
AS
BEGIN

declare @Date datetime=GETDATE()
declare @Year int=YEAR(@Date)-1
/*
create table #LeaveStation(
	序号 int,
	姓名 varchar(55),
	员工内码 int,
	请假类别 varchar(55),
	类别值 int,
	本年可请 decimal(18,2),
	目前可请 decimal(18,2),
	上年结转 decimal(18,4),
	已请天数 decimal(18,4),
	剩余可请 decimal(18,4),
)
*/
--insert into ##LeaveStation exec proc_czly_LeaveQuery @FNameId='',@FLeaveType='6',@FYear=@Year
exec proc_czly_LeaveQuery @FNameId='',@FLeaveType='6',@FYear=@Year

select * from ##LeaveStation
--drop table #LeaveStation
--select * from ora_t_Leave

declare @FID int=ident_current('Z_ora_t_LeaveHead')+1
declare @max_num int=(select MAX(RIGHT(FBILLNO,5)) from ora_t_LeaveHead)
declare @FBILLNO varchar(30)='122'+RIGHT(@Year,2)+dbo.fun_BosRnd_addSpace(@max_num,'','',5)
declare @FCREATORID int=16394 --管理员
declare @FAPPLYID int=113060  --总经办
declare @FORGID int=100221    --江苏光伏
declare @FDEPTID int
declare @F_ORA_POST int
declare @FISORIGIN int=1      --期初单标记
declare @F_ORA_REMARKS varchar(100)='上年结转'

select @FAPPLYID=e.FID,@FORGID=e.FUSEORGID,@FDEPTID=e.F_ORA_DEPTID,@F_ORA_POST=e.F_ORA_POST
from T_HR_EMPINFO e 
inner join T_HR_EMPINFO_L el on e.FID=el.FID
where el.FNAME='总经办'

--select * from T_HR_EMPINFO_L

declare @FEntryID int=ident_current('Z_ora_t_Leave')+1
select
 ROW_NUMBER() over(order by t.员工内码) FSEQ,0 as FEntryID,
 t.员工内码 as FName,
 e.F_ORA_DEPTID FDept, e.F_ORA_POST FPost,
 6 FLeaveType,
 case when t.本年可请>=t.剩余可请 then -t.剩余可请 else -t.本年可请 end as FDayNum
into #temp
from ##LeaveStation t
inner join T_HR_EMPINFO e on t.员工内码=e.FID
update #temp set FEntryID=FSEQ+@FEntryID
select @FEntryID=Max(FEntryID) from #temp

select @FID,@FBILLNO,'C',@Date,@FCREATORID,@FORGID,@FAPPLYID,@FDEPTID,@F_ORA_POST,@FISORIGIN,@F_ORA_REMARKS
select * from #temp
--drop table #temp

begin tran
begin try
--生成表头
INSERT INTO ora_t_LeaveHead(
	FID,FBILLNO,FDOCUMENTSTATUS,FCREATEDATE,FCREATORID,FORGID,FAPPLYID,FDEPTID,F_ORA_POST,FISORIGIN,F_ORA_REMARKS
)
VALUES(
	@FID,@FBILLNO,'C',@Date,@FCREATORID,@FORGID,@FAPPLYID,@FDEPTID,@F_ORA_POST,@FISORIGIN,@F_ORA_REMARKS
)
DBCC CHECKIDENT (Z_ora_t_LeaveHead,RESEED,@FID)
--生成表体
INSERT INTO ora_t_Leave(
	FID,FEntryID,FSEQ,FLEAVETYPE,FSTARTDATE,FSTARTTIMEFRAME,FENDDATE,FENDTIMEFRAME,FDAYNUM,FNAME,FPOST,FDEPT,FSTIME,FETIME
)
SELECT 
	@FID,FEntryID,FSEQ,FLeaveType,@Date,1,@Date,1,FDayNum,FName,FPost,FDept,@Date,@Date
FROM #temp
DBCC CHECKIDENT (Z_ora_t_Leave,RESEED,@FEntryID)

end try
begin catch
	if(@@trancount>0) rollback tran --回滚
end catch
if(@@trancount>0) commit tran

drop table #temp

--select * from ora_t_Leave where FID=@FID

END

--exec proc_czly_CreateInitLeave