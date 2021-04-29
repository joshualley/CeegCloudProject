/*
根据加班类型计算可视为调休的天数
加班类型：
1 工作日 x1.5
3 双休日 x2
2 节假日 x3
参数：
@EmpID： 员工ID
*/
CREATE proc [dbo].[proc_czly_GetHolidayShiftSituation]
@EmpID int
AS

if exists(select * from tempdb..sysobjects where id=object_id('tempdb..#temp_overtime'))
drop table #temp_overtime;

-- 单据已审核，且支付方式为调休
select F_ORA_PRETIME FHours,FTYPE FType
into #temp_overtime
from
(select oe.F_ORA_PRETIME, o.FTYPE from ora_t_Overtime_Entry oe
inner join ora_t_Overtime o on oe.FID=o.FID
where oe.F_ORA_OMAN=@EmpID and o.FDOCUMENTSTATUS='C' --and o.FPAYTYPE = '2'
) t;

/*
--工作日加班 算1.5倍
update #temp_overtime
set FHours = FHours*1.5
where FType = 1
--周末加班 2倍
update #temp_overtime
set FHours = FHours*2
where FType = 3
--节假日加班 3倍
update #temp_overtime
set FHours = FHours*3
where FType = 2
*/

--将查询结果赋值给变量
declare @FOverHours float
set @FOverHours = (select sum(FHours) FOverHours from #temp_overtime)

--select sum(FHours) FOverHours from #temp_overtime  

--计算已请调休假的天数
if exists(select * from tempdb..sysobjects where id=object_id('tempdb..#temp_leavetime'))
drop table #temp_leavetime;

--查询请调休的天数，并转为小时，1天对应8小时
select (le.FDAYNUM*8) FRestHours
into #temp_leavetime
from ora_t_Leave le
inner join ora_t_LeaveHead lh on le.FID=lh.FID
where le.FNAME=@EmpID and lh.FDOCUMENTSTATUS='C' and le.FLEAVETYPE=9
--将查询结果赋值给变量
declare @FRestHours float
set @FRestHours=(select sum(FRestHours) from #temp_leavetime)
--如果查询结果不存在，对其置0
if @FOverHours is null
set @FOverHours=0
if @FRestHours is null
set @FRestHours=0

select @FOverHours FOverHours, @FRestHours FRestHours, (@FOverHours-@FRestHours) FLeftHours

--exec proc_czly_GetHolidayShiftSituation @EmpID=299318