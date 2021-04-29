-- 考勤
CREATE proc [dbo].[proc_czty_hrInsEmpWkDtEntry] 
@FID bigint=100029
as
begin

set nocount on
--declare @FID	bigint=100029
----declare @FDeptID bigint=295177	--常规-江苏办
declare @FBegDt datetime='2020-05-01' 
declare @FEndDt datetime='2020-05-31 23:59:59'
declare @FBackM varchar(10)='',@FBackMsg varchar(200)=''
declare @ttCount int=0

-------- step 00.00 获取条件变量
select @FBegDt=convert(varchar(7),FANZDATE,20)+'-01' from ora_hr_EmpWkDt where FID=@FID
select @FEndDt=DATEADD(SECOND,-1, DATEADD(MONTH,1,@FBegDt))

select d.FID,dl.FDeptID,dl.FNAME FDeptNA 
into #dept 
from(select FID,FDDEPTID from ora_hr_EmpWkDtDept where FID=@FID)d
inner join T_BD_DEPARTMENT_L dl on d.FDDEPTID=dl.FDEPTID and dl.FLOCALEID=2052 

select @ttCount=COUNT(*) from #dept 
if(@ttCount=0)
begin
	select @FBackM='Err',@FBackMsg='生成日明细已执行。错误：没有选择部门'
	goto OutPut
end


-------- step 01.01 查询部门--员工--用户
select d.FDeptID,FDeptMID,dl.FNAME FDeptNA,d.FDeptOrgID,dol.FNAME FDeptOrg,st.FID,st.FWorkOrgID,sol.FNAME FWorkOrg,
e.FID FEmpID,el.FNAME FEmpNA,eol.FNAME FEmpOrg,st.FPostID,isnull(sa.FPOSTCLASS,'DF')FPostClass,pl.FNAME FPostNA,
u.FUserID,u.FNAME FUserNA,xm.FOpenID   
into #eu
from (select FDEPTID,FMasterID FDeptMID,FUSEORGID FDeptOrgID from T_BD_Department )d
inner join T_BD_DEPARTMENT_L dl on d.FDEPTID=dl.FDEPTID and dl.FLOCALEID=2052 
inner join #dept dd on dl.FNAME=dd.FDeptNA
inner join T_ORG_ORGANIZATIONS_L dol on d.FDeptOrgID=dol.FORGID and dol.FLOCALEID=2052 
inner join T_BD_STAFFTEMP st on d.FDEPTID=st.FDeptID and F_Ora_ISCWAPost=1 --and FIsFirstPost=1
inner join T_BD_STAFF s on st.FSTAFFID=s.FSTAFFID and s.FDOCUMENTSTATUS='C' and s.FFORBIDSTATUS='A'   
left join T_ORG_POST_L pl on st.FPostID=pl.FPOSTID and pl.FLOCALEID=2052 
inner join T_ORG_ORGANIZATIONS_L sol on st.FWorkOrgID=sol.FORGID and sol.FLOCALEID=2052 
inner join T_HR_EMPINFO e on st.FID=e.FID and e.FDOCUMENTSTATUS='C'
inner join T_HR_EMPINFO_L el on e.FID=el.FID and el.FLOCALEID=2052 
inner join T_ORG_ORGANIZATIONS_L eol on e.FUSEORGID=eol.FORGID and eol.FLOCALEID=2052 
left join V_bd_ContactObject c on e.FNUMBER=c.FNumber 
left join T_SEC_USER u on c.FID=u.FLinkObject 
left join T_SEC_XTUSERMAP xm on u.FUSERID=xm.FUSERID 
left join(
	select pe.FPostID,p.FPostClass 
	from(select FID,FPostClass from ora_BD_PostType where FPostClass='SA' and FDocumentStatus='C')p 
	inner join ora_BD_PostTypeEntry pe on p.FID=pe.FID)sa on st.FPOSTID=sa.FPostID
--where d.FDEPTID=@FDeptID --and dl.FName='常规-江苏办'
where pl.FNAME not in('操作工')
order by FDeptNA,FEmpNA 

select @ttCount=COUNT(*) from #eu
if(@ttCount=0)
begin
	select @FBackM='Err',@FBackMsg='生成日明细已执行。错误：没有职员'
	goto OutPut
end

--		select * from #eu
 
-------- step 01.02 eu + 工厂日历 中电电气（江苏）股份　的日历
--select w.FID,w.FMasterID,w.FUseOrgID,w.FApproveDate from t_eng_workcal w 
select w.FID,w.FUseOrgID,wd.FDay FDate,YEAR(FDay)FYear,Month(FDay)FMonth,DAY(FDay)FDay,wd.FDateStyle,ds.FCAPTION FDS,wd.FIsWorkTime 
into #day
from (select * from t_eng_workcal where FID=309442) w 
inner join T_ENG_WORKCAL_L wl on w.FID=wl.FID and wl.FLOCALEID=2052
inner join T_ENG_WorkCalData wd on w.FID=wd.FID 
inner join(
	select fl.FID,fl.FName,fi.FValue,fi.FSEQ,fil.FCaption 
	from (select FID,FName from T_META_FORMENUM_L where FNAME='MFG_日期类型' and FLOCALEID=2052) fl 
	inner join T_META_FORMENUMItem fi on fl.FID=fi.FID 
	inner join T_META_FORMENUMITEM_L fil on fi.FENUMID=fil.FENUMID and fil.FLOCALEID=2052
)ds on wd.FDateStyle=ds.FVALUE 
where FDAY between @FBegDt and @FEndDt 

--		select * from #day

-------- step 01.03 取班次（上下班时间）
--select s.FID,sd.FEntryID,convert(varchar(5),sd.FStartTime,108)FStartTime,convert(varchar(5),sd.FEndTime,108)FEndTime 
--from (select FID from T_ENG_SHIFT where FNumber='BZ01_SYS')s
--inner join t_ENG_ShiftData sd on s.FID=sd.FID

select * into #h
from(
	select 'DF' FPC,
	min(convert(varchar(5),sd.FStartTime,108))FAMST,min(convert(varchar(5),sd.FEndTime,108))FAMET,
	max(convert(varchar(5),sd.FStartTime,108))FPMST,max(convert(varchar(5),sd.FEndTime,108))FPMET 
	from (select FID from T_ENG_SHIFT where FNumber='BZ01_SYS')s
	inner join t_ENG_ShiftData sd on s.FID=sd.FID
	union
	select 'SA' FPC,'09:00',null,null,'17:00'	--销售员
)t

-------- step 01.04 员工+日历 考勤数据底板
select eu.FDeptID,eu.FDeptMID,eu.FDeptNA,eu.FEmpID,eu.FEmpNA,eu.FEmpOrg,eu.FWorkOrgID,eu.FPostID,eu.FPostClass,eu.FUserID,eu.FUserNA,eu.FOpenID,
d.FDate,d.FYear,d.FMonth,d.FDay,d.FDateStyle,d.FDS 
into #mod 
from #eu eu
left join #day d on 1=1 
order by eu.FUserNA,d.FDay 

--		select * from #mod 

-------- step 01.05 关联打卡 区分InOut
select eu.FOPENID,sd.FDate,FInOut,min(FFullDate)FMinDate,MAX(FFullDate)FMaxDate 
into #sd
from #eu eu 
inner join Ora_HR_SignInData sd on eu.FOPENID=sd.FOpenID and sd.FDate between @FBegDt and @FEndDt 
group by eu.FOPENID,sd.FDate,FInOut 

--		select * from #sd

-------- step 01.06 考勤数据底板mod + 班时h（分） +实际打卡
select ROW_NUMBER() over(order by m.FDEPTID,m.FEmpID,m.FDate)FRndIdx ,m.*,h.FAMST,h.FAMET,h.FPMST,h.FPMET,
--sdi.FMinDate FMinDtIn,sdi.FMaxDate FMaxDtIn,sdo.FMinDate FMinDtOut,sdo.FMaxDate FMaxDtOut
convert(varchar(5),sdi.FMinDate,108)FMinDtIn,convert(varchar(5),sdi.FMaxDate,108)FMaxDtIn,convert(varchar(5),sdo.FMinDate,108)FMinDtOut,convert(varchar(5),sdo.FMaxDate,108)FMaxDtOut,
CONVERT(int,0)FIsTA,convert(decimal(18,2),0)FOverHour,CONVERT(int,0)FIsLD,
convert(decimal(18,2),0)FLd01ChanJian,convert(decimal(18,2),0)FLd02ShiJia,convert(decimal(18,2),0)FLd03TanQin,convert(decimal(18,2),0)FLd04BinJia,
convert(decimal(18,2),0)FLd05PeiChan,convert(decimal(18,2),0)FLd06NianXiu,convert(decimal(18,2),0)FLd07SangJia,convert(decimal(18,2),0)FLd08HunJia,
convert(decimal(18,2),0)FLd09TiaoXiu,convert(decimal(18,2),0)FLd10GongShang,convert(decimal(18,2),0)FLd12ChanJia,convert(decimal(18,2),0)FLdxxOther,
0 FIsLateIn,0 FIsEarlyOut,0 FIsAbs
into #md 
from #mod m 
left join #h h on m.FPostClass=h.FPC 
left join #sd sdi on m.FOpenID=sdi.FOpenID and m.FDate=sdi.FDate and sdi.FInOut='Normal' 
left join #sd sdo on m.FOpenID=sdo.FOpenID and m.FDate=sdo.FDate and sdo.FInOut='Outside' 
order by FDeptNA,FEmpNA,FDate --FDEPTID,FEmpID,FDate 

--		select * from #md

-------- step 02.01 加载考勤数据 请假 select * from ora_t_LeaveHead		select * from ora_t_Leave
-- select FNAME FEmpID,FPOST,FDEPT,FLeaveType,FStartDate,FSTime,FEndDate,FETime from ora_t_Leave --where 
select --md.*,
md.FRndIdx,md.FEmpID,md.FDEPTID,md.FPOSTID,md.FDate,md.FPostClass,md.FDateStyle,md.FDS,md.FAMST,md.FAMET,md.FPMST,md.FPMET,
ld.FEntryID,ld.FLeaveType,ld.FStartDate,convert(varchar(5),ld.FSTime,108)FSTime,ld.FEndDate,convert(varchar(5),ld.FETime,108)FETime,
case when ld.FStartDate=ld.FEndDate then 'C' when md.FDate=ld.FStartDate then 'B' when md.FDate=ld.FEndDate then 'E' else 'In' end as 'FDayLimit',
CONVERT(decimal(18,2),0.00)FLeaveHour,0 FIsAnz 
into #ld
from #md md 
inner join ora_t_Leave ld on md.FEmpID=ld.FNAME and md.FDate between ld.FSTARTDATE and ld.FENDDATE 
inner join ora_t_LeaveHead l on ld.FID=l.FID and l.FDocumentStatus='C' 

--		select * from #ld order by FUserNA,FDate 
----请假中间日期，判定当天小时数为8(全值)
update #ld set FLeaveHour=8,FIsAnz=1 where FDayLimit='In' and FIsAnz=0
------不分岗位,FDayLimit=B,当天请假结束时间=PM下班时间
--update #ld set FETime=FPMET where FDayLimit='B' and FIsAnz=0
------不分岗位,FDayLimit=E,当天请假开始时间=AM上班时间
--update #ld set FSTime=FAMST where FDayLimit='E' and FIsAnz=0
----DF岗位,请假开始时间 FDayLimit=E =AM上班时间,小于AM上班时间=AM上班时间,(AM下班：PM上班)=PM上班时间,其他不变
----DF岗位,请假结束时间 FDayLimit=B =PM下班时间,大于PM下班时间=PM下班时间,(AM下班：PM上班)=AM下班时间,其他不变
--select FPostClass,FSTime,FAMST,FAMET,FPMST,case when FSTime<FAMST then FAMST when FSTime between FAMET and FPMST then FPMST else FSTime end from #ld 
update #ld set --FIsAnz=1,
	FSTime=case when FDayLimit='E' then FAMST when FSTime<FAMST then FAMST when FSTime between FAMET and FPMST then FPMST else FSTime end,
	FETime=case when FDayLimit='B' then FPMET when FETime>FPMET then FPMET when FETime between FAMET and FPMST then FAMET else FETime end
where FPostClass='DF' and FIsAnz=0
----SA岗位,请假开始时间 FDayLimit=E =AM上班时间,小于AM上班时间=AM上班时间,其他不变
----SA岗位,请假结束时间 FDayLimit=B =PM下班时间,大于PM下班时间=PM下班时间,其他不变
update #ld set --FIsAnz=1,
	FSTime=case when FDayLimit='E' then FAMST when FSTime<FAMST then FAMST else FSTime end,
	FETime=case when FDayLimit='B' then FPMET when FETime>FPMET then FPMET else FETime end
where FPostClass='SA' and FIsAnz=0
----FDayLimit in(B,E,C)
update #ld set FIsAnz=1,FLeaveHour=DATEDIFF(minute,FSTime,FETime)/60 where FIsAnz=0
----修正值
update #ld set FLeaveHour=0 where FLeaveHour<0
update #ld set FLeaveHour=8 where FLeaveHour>8

-------- step 02.02 加载考勤数据 加班 ora_t_Overtime		ora_t_Overtime_Entry 
--select o.FID,o.FBILLNO,o.FDocumentStatus,oe.F_ora_OMan,oe.F_ORA_DEPT,oe.F_ORA_Post,F_SDatetime,F_EDatetime,F_ora_PreTime,F_ora_RealTime
--from ora_t_Overtime o
--inner join ora_t_Overtime_Entry oe on o.FID=oe.FID 
--where DATEPART(day,F_SDatetime)<>DATEPART(day,F_EDatetime)
select --md.*,
md.FRndIdx,md.FEmpID,md.FDEPTID,md.FPOSTID,md.FDate,md.FPostClass,md.FDateStyle,md.FDS,md.FAMST,md.FAMET,md.FPMST,md.FPMET,
convert(varchar(10),oe.F_SDatetime,20)FStartDate,convert(varchar(5),oe.F_SDatetime,108)FSTime,
convert(varchar(10),oe.F_EDatetime,20)FEndDate,convert(varchar(5),oe.F_EDatetime,108)FETime,
CONVERT(varchar(5),'')FDayLimit,CONVERT(decimal(18,2),0.00)FOverHour,0 FIsAnz 
into #ot
from #md md 
inner join ora_t_Overtime_Entry oe on md.FEmpID=oe.F_ora_OMan and md.FDate between convert(varchar(10),oe.F_SDatetime,20) and oe.F_EDatetime 
inner join ora_t_Overtime o on oe.FID=o.FID and o.FDOCUMENTSTATUS='C' 

--		select * from #ot

--拆分连续加班
update #ot set FDayLimit=case when FStartDate=FEndDate then 'C' when FDate=FStartDate then 'B' when FDate=FEndDate then 'E' else 'In' end 
----FDayLimit=In,工作日,休息日=8,节假日=12	
----FDayLimit=C,结束时间-开始时间		FDayLimit='B' 24-(0点-开始时间) 		FDayLimit='E' 0点到结束时间
update #ot set FOverHour=case FDATESTYLE when 3 then 12 else 8 end where FDayLimit='In'
update #ot set FOverHour=case FDayLimit when 'C' then DATEDIFF(MINUTE,FSTime,FETime)/60 when 'B' then 24-DATEDIFF(MINUTE,'00:00',FSTime)/60 when 'E' then DATEDIFF(MINUTE,'00:00',FETime)/60 end where FDayLimit<>'In'

-------- step 02.03 加载考勤数据 出差 ora_t_TravelApply	ora_t_TravelApplyEntry 
--select t.FID,t.FApply,t.FPost,t.FDocumentStatus,te.FEntryID,te.FDateStart,te.FDateEnd,
--convert(varchar(10),te.FDateStart,20)FStartDate,convert(varchar(5),te.FDateStart,108)FSTime,
--convert(varchar(10),te.FDateEnd,20)FEndDate,convert(varchar(5),te.FDateEnd,108)FETime,
--CONVERT(varchar(5),'')FDayLimit,CONVERT(decimal(18,2),0.00)FOverHour,0 FIsAnz  
--from ora_t_TravelApply t 
--inner join ora_t_TravelApplyEntry te on t.FID=te.FID 

select --md.*, 
md.FRndIdx,md.FEmpID,md.FDEPTID,md.FPOSTID,md.FDate,md.FPostClass,md.FDateStyle,md.FDS,md.FAMST,md.FAMET,md.FPMST,md.FPMET,
convert(varchar(10),te.FDateStart,20)FStartDate,convert(varchar(5),te.FDateStart,108)FSTime,
convert(varchar(10),te.FDateEnd,20)FEndDate,convert(varchar(5),te.FDateEnd,108)FETime,
CONVERT(varchar(5),'')FDayLimit,CONVERT(decimal(18,2),0.00)FOverHour,0 FIsAnz  
into #ta
from #md md
inner join ora_t_TravelApplyEntry te on md.FDate between convert(varchar(10),te.FDateStart,20) and te.FDateEnd 
inner join ora_t_TravelApply t on te.FID=t.FID and md.FEmpID=t.FAPPLY and t.FDocumentStatus='C' 
--拆分连续出差
update #ta set FDayLimit=case when FStartDate=FEndDate then 'C' when FDate=FStartDate then 'B' when FDate=FEndDate then 'E' else 'In' end 

--		select * from #ta

-------- step 03.01 数据整合 03.02 模板md + 出差#ta 先判定是否出差 （班时及打卡数据判定需要先行判定内勤or外勤）
--select m.*,ta.* 
update m set m.FIsTA=ta.FIsTA
from #md m 
inner join (select distinct FRndIdx,1 FIsTA from #ta)ta on m.FRndIdx=ta.FRndIdx 

-------- step 03.02 数据整合 模板md + 加班ot
--select md.FRndIdx,md.FOverHour,oh.FOverHour 
update md set md.FOverHour=oh.FOverHour
from #md md
inner join(select FRndIdx,sum(FOverHour)FOverHour from #ot group by FRndIdx)oh on md.FRndIdx=oh.FRndIdx

-------- step 03.03 数据整合 模板md + 请假ld	select * from #ld
--select ld.FRndIdx,l.FNumber,SUM(FLeaveHour)FLeaveHour from #ld ld 
--inner join ora_BD_LeaveClassEntry le on ld.FLEAVETYPE=le.FOALItem 
--inner join ora_BD_LeaveClass l on le.FID=l.FID 
--group by ld.FRndIdx,l.FNumber
update md set md.FIsLD=r.FIsLD,
md.FLd01ChanJian=r.FLd01ChanJian,md.FLd02ShiJia=r.FLd02ShiJia,md.FLd03TanQin=r.FLd03TanQin,
md.FLd04BinJia=r.FLd04BinJia,md.FLd05PeiChan=r.FLd05PeiChan,md.FLd06NianXiu=r.FLd06NianXiu,
md.FLd07SangJia=r.FLd07SangJia,md.FLd08HunJia=r.FLd08HunJia,md.FLd09TiaoXiu=r.FLd09TiaoXiu,
md.FLd10GongShang=r.FLd10GongShang,md.FLd12ChanJia=r.FLd12ChanJia,md.FLdxxOther=r.FLdxxOther 
from #md md
inner join(
	select ld.FRndIdx,1 FIsLD,
	sum(case l.FNumber when 'FLd01ChanJian' then ld.FLeaveHour else 0 end) as FLd01ChanJian,
	sum(case l.FNumber when 'FLd02ShiJia' then ld.FLeaveHour else 0 end) as FLd02ShiJia,
	sum(case l.FNumber when 'FLd03TanQin' then ld.FLeaveHour else 0 end) as FLd03TanQin,
	sum(case l.FNumber when 'FLd04BinJia' then ld.FLeaveHour else 0 end) as FLd04BinJia,
	sum(case l.FNumber when 'FLd05PeiChan' then ld.FLeaveHour else 0 end) as FLd05PeiChan,
	sum(case l.FNumber when 'FLd06NianXiu' then ld.FLeaveHour else 0 end) as FLd06NianXiu,
	sum(case l.FNumber when 'FLd07SangJia' then ld.FLeaveHour else 0 end) as FLd07SangJia,
	sum(case l.FNumber when 'FLd08HunJia' then ld.FLeaveHour else 0 end) as FLd08HunJia,
	sum(case l.FNumber when 'FLd09TiaoXiu' then ld.FLeaveHour else 0 end) as FLd09TiaoXiu,
	sum(case l.FNumber when 'FLd10GongShang' then ld.FLeaveHour else 0 end) as FLd10GongShang,
	sum(case l.FNumber when 'FLd12ChanJia' then ld.FLeaveHour else 0 end) as FLd12ChanJia,
	sum(case l.FNumber when 'FLdxxOther' then ld.FLeaveHour else 0 end) as FLdxxOther
	from #ld ld 
	inner join ora_BD_LeaveClassEntry le on ld.FLEAVETYPE=le.FOALItem 
	inner join ora_BD_LeaveClass l on le.FID=l.FID 
	group by ld.FRndIdx
)r on md.FRndIdx=r.FRndIdx 

--		select * from #md
-------- step 03.04 数据判定 迟到，早退，缺卡
update md set md.FIsLateIn=t.FIsLateIn,md.FIsEarlyOut=t.FIsEarlyOut 
from( 
	select FRndIdx,FPostClass,FEmpNA,
	FMinDtIn,FAMST,case when FMinDtIn <=FAMST then 0 when FMinDtIn between FAMST and '12:00' then 1 else 2 end as FIsLateIn, 
	FMaxDtIn,FPMET,case when FMaxDtIn >=FPMET then 0 when FMaxDtIn between '12:00' and FPMET then 1 else 2 end as FIsEarlyOut 
	from #md where FIsTA=0 and FIsLD=0 and FDATESTYLE=1	--工作日，非请假，非出差
)t
inner join #md md on t.FRndIdx=md.FRndIdx 

-------- step 04.01 写入 BOS考勤表-考勤日明细 ora_hr_EmpWkDtEntry 
delete from ora_hr_EmpWkDtEntry where FID=@FID 
declare @curEID bigint=100001,@maxEID bigint
select @curEID=isnull(ident_current('Z_ora_hr_EmpWkDtEntry'),100006) 

insert into ora_hr_EmpWkDtEntry
(
	FID,FEntryID,FSEQ,
	FDEPTID,FEMPID,FWORKORGID,FPOSTID,FPOSTCLASS,FUSERNA,FOPENID,FWKDATE,FDATESTYLE,
	FAMST,FAMET,FPMST,FPMET,FMINDTIN,FMAXDTIN,FMINDTOUT,FMAXDTOUT,
	FISTA,FOVERHOUR,FISLD,
	FLD01CHANJIAN,FLD02SHIJIA,FLD03TANQIN,FLD04BINJIA,
	FLD05PEICHAN,FLD06NIANXIU,FLD07SANGJIA,FLD08HUNJIA,
	FLD09TIAOXIU,FLD10GONGSHANG,FLD12CHANJIA,FLDXXOTHER,
	FISLATEIN,FISEARLYOUT,FISABS,FNOTE
)
select @FID FID,@curEID+FRndIdx FEntryID,FRndIdx FSEQ,
FDeptID,FEmpID,FWorkOrgID,FPostID,FPostClass,isnull(FUserNA,''),isnull(FOpenID,''),FDate FWkDate,FDATESTYLE,
FDate+''+FAMST FAMST,FDate+''+FAMET FAMET,FDate+''+FPMST FPMST,FDate+''+FPMET FPMET,	
FDate+''+FMinDtIn FMinDtIn,FDate+''+FMaxDtIn FMaxDtIn,FDate+''+FMinDtOut FMinDtOut,FDate+''+FMaxDtOut FMaxDtOut,
FIsTA,FOverHour,FIsLD,
FLd01ChanJian,FLd02ShiJia,FLd03TanQin,FLd04BinJia,
FLd05PeiChan,FLd06NianXiu,FLd07SangJia,FLd08HunJia,
FLd09TiaoXiu,FLd10GongShang,FLd12ChanJia,FLdxxOther,
FIsLateIn,FIsEarlyOut,FIsAbs,'' FNote	
from #md order by FRndIdx 

select @maxEID=isnull(MAX(FEntryID),@curEID) from ora_hr_EmpWkDtEntry

if(@maxEID>@curEID)
begin
	DBCC CHECKIDENT (Z_ora_hr_EmpWkDtEntry,RESEED,@maxEID)
end 

-------- step 04.02 更新表头部门数据 
declare @FDeptNAs varchar(400)=''
select @FDeptNAs=stuff((  
select ','+[FDeptNA] from #dept as b where b.FID = a.FID for xml path('')),1,1,'') from #dept as a  
group by FID 
update ora_hr_EmpWkDt set FDeptNAs=@FDeptNAs where FID=@FID

select @FBackM='OK',@FBackMsg='生成日明细已执行'

-------- step 05.01 OutPut Massage
OutPut:
select @FBackM FBackM,@FBackMsg FBackMsg

end
--------------------------
/*
exec proc_czty_hrInsEmpWkDtEntry @FID='100029' 
*/
