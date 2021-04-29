-- 跳过休息日_计算间隔时长
/*-------------- proc_czty_LeaveWorkDays 工作日 计算.sql --------------*/  
CREATE proc [dbo].[proc_czty_LeaveWorkDaysAP]   
@FOrgID  int,  
@FBD  datetime,  --='2019-07-27 12:00:00',  
@FBD_AP  int,   --1:AM 2:PM  
@FED  datetime,  --='2019-07-31 17:00:00'  
@FED_AP  int    --1:AM 2:PM  
as  
begin  
set nocount on  
set @FOrgID=1  
select @FBD=convert(varchar(10),@FBD,20),@FED=convert(varchar(10),@FED,20)  
declare @lwds decimal(18,1)=0,@wds int=0 --一请假工作日｜起始区间中间工作日  


select top 1 --w.FID,w.FNUMBER,w.FUSEORGID,w.FDOCUMENTSTATUS,wd.FENTRYID,wd.FDAY,wd.FDATESTYLE,--ds.FCAPTION,  
@FBD=case wd.FDAY when @FBD then @FBD else wd.FDAY  end  
from(select * from T_ENG_WORKCAL where FID=309442)w  
inner join T_ENG_WorkCalData wd on w.FID=wd.FID  
where wd.FDAY>=@FBD and wd.FDATESTYLE=1   
order by wd.FDAY asc  
  
select top 1 --w.FID,w.FNUMBER,w.FUSEORGID,w.FDOCUMENTSTATUS,wd.FENTRYID,wd.FDAY,wd.FDATESTYLE,--ds.FCAPTION,  
@FED=case wd.FDAY when @FED then @FED else wd.FDAY end  
from(select * from T_ENG_WORKCAL where FID=309442)w  
inner join T_ENG_WorkCalData wd on w.FID=wd.FID  
where wd.FDAY<=@FED and wd.FDATESTYLE=1   
order by wd.FDAY desc  
  
select --w.FID,w.FNUMBER,w.FUSEORGID,w.FDOCUMENTSTATUS,wd.FENTRYID,wd.FDAY,wd.FDATESTYLE, --ds.FCAPTION  
@wds=count(FENTRYID)  
from(select * from T_ENG_WORKCAL where FID=309442)w  
inner join T_ENG_WorkCalData wd on w.FID=wd.FID  
where wd.fday between @FBD and @FED and wd.FDATESTYLE=1   


--set @wds=DATEDIFF(DAY,@FBD,@FED)+1

if(@FBD>@FED)  
begin  
 set @lwds=0   
end   
else if(@FBD=@FED) --同一天   
begin  
 --2:B=E 上：上=0.5  下：下=0.5  上：下=1 下：上=0  
 select @lwds=@wds-1    
 select @lwds=case when @FBD_AP=@FED_AP then 0.5 when (@FBD_AP=1 and @FED_AP=2) then 1 else 0 end  
end   
else begin  
 --3：B<E 基准值：N（WD）-2 上：上=1.5 下：下=1.5 上：下=2 下：上=1 请假工作日=N-2+P  
 select @lwds=@wds-1  
 select @lwds=@lwds+case when @FBD_AP=@FED_AP then 0.5 when (@FBD_AP=1 and @FED_AP=2) then 1 else 0 end  
end  
  
select @FBD FBD,@FED FED,@wds wds,@lwds lwds  
  
end  
----------  
-- exec proc_czty_LeaveWorkDaysAP @FOrgID='1',@FBD='2019-07-27',@FBD_AP='1',@FED='2019-07-31',@FED_AP='1' --5 |4.5  
-- exec proc_czty_LeaveWorkDaysAP @FOrgID='1',@FBD='2019-07-25',@FBD_AP='1',@FED='2019-07-31',@FED_AP='1' --7 |6.5  
-- exec proc_czty_LeaveWorkDaysAP @FOrgID='1',@FBD='2019-07-25',@FBD_AP='1',@FED='2019-07-25',@FED_AP='1' --1 |0.5  