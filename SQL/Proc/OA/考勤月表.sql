-- 考勤月表
CREATE proc [dbo].[proc_czty_hrInsEmpWkDtMon] 
@FID bigint=100029
as
begin
set nocount on 

------汇总日数据生成月表行
select @FID FID,0 FMEntryID,ROW_NUMBER() over(order by min(FSEQ))FMSEQ,
FDeptID,FEmpID,FWorkOrgID,FPostID,FPostClass,FUserNA,convert(varchar(7),MIN(FWkDate),20)FWkDate,
sum(case FDateStyle when 1 then 1 else 0 end )FMWkDays,SUM(case FIsTA when 1 then 1 else 0 end)FMTADays,
SUM(FOverHour)FOverHour,SUM(case FISLD when 1 then 1 else 0 end)FMLDDays,
SUM(FLd01ChanJian)FLd01ChanJian,SUM(FLd02ShiJia)FLd02ShiJia,SUM(FLd03TanQin)FLd03TanQin,SUM(FLd04BinJia)FLd04BinJia,	
SUM(FLd05PeiChan)FLd05PeiChan,SUM(FLd06NianXiu)FLd06NianXiu,SUM(FLd07SangJia)FLd07SangJia,SUM(FLd08HunJia)FLd08HunJia,	
SUM(FLd09TiaoXiu)FLd09TiaoXiu,SUM(FLd10GongShang)FLd10GongShang,SUM(FLd12ChanJia)FLd12ChanJia,SUM(FLdxxOther)FLdxxOther,	
SUM(case FIsLateIn when 0 then 0 else 1 end)FMLateInDays,SUM(case FIsEarlyOut when 0 then 0 else 1 end)FMEarlyOutDays,
SUM(case FIsAbs when 0 then 0 else 1 end)FMAbsDays,'' FMNote,
SUM(case 
	when FOverHour>0 then 1						--日加班小时>0（有加班申请）
	/*when FISTA=1 then 1*/						--是否出差=是
	when FDATESTYLE <>1 and FOVERHOUR=0 then 0	--日期类型<>工作日	非加班
	when(FMINDTIN is not null or FMaxDtIn is not null or FMinDtOut is not null or FMaxDtOut is not null) then 1  --有打卡
	else 0 end
	) as FMRealWkDs,
--case when FOverHour>0 then 1 when FISTA=1 then 1 when(ISNULL(FMINDTIN,'')<>'' or ISNULL(FMaxDtIn,'')<>'' or ISNULL(FMinDtOut,'')<>'' or ISNULL(FMaxDtOut,'')<>'')then 1 else 0 end as FMRealWkDs 
CONVERT(decimal(18,2),0)FMLDDays2,CONVERT(decimal(18,2),0)FAllLdHour,CONVERT(decimal(18,2),0)FMAch 
into #t 
from ora_hr_EmpWkDtEntry 
where FID=@FID 
group by FDeptID,FEmpID,FWorkOrgID,FPostID,FPostClass,FUserNA 

------统计总请假小时数，换算请假天数 4小时折半天向上取整，再换算天数
update #t set FAllLdHour=
	FLd01ChanJian+FLd02ShiJia+FLd03TanQin+FLd04BinJia
	+FLd05PeiChan+FLd06NianXiu+FLd07SangJia+FLd08HunJia
	+FLd09TiaoXiu+FLd10GongShang+FLd12ChanJia+FLdxxOther 
	
update #t set FMLDDays2=ceiling(FAllLdHour/4)/2.0 

------取 员工——用户——当月绩效
--select t.FEMPID,t.FUSERNA,u.FName,t.FMAch,isnull(pr.FScore,0) 
update t set t.FMAch=isnull(pr.FScore,0)
from #t t 
inner join T_HR_EMPINFO e on t.FEmpID=e.FID and e.FDOCUMENTSTATUS='C'
left join V_bd_ContactObject c on e.FNUMBER=c.FNumber 
left join T_SEC_USER u on c.FID=u.FLinkObject  
left join ora_Task_PersonalReport pr on u.FUSERID=pr.FCreatorID and t.FWkDate=CONVERT(varchar(7),pr.FCreateDate,20)

------删除原行，更新行ID，序号
delete from ora_hr_EmpWkDtMon where FID=@FID 
declare @curEID bigint=100032,@maxEID bigint 
select @curEID=isnull(ident_current('Z_ora_hr_EmpWkDtMon'),100032) 

update #t set FMEntryID=@curEID+FMSEQ 

------ 写入表 select * from ora_hr_EmpWkDtMon  select * from #t
insert into ora_hr_EmpWkDtMon(
	FID,FMEntryID,FMSEQ,
	FMDeptID,FMEmpID,FMWorkOrgID,FMPostID,FMPostClass,FMUserNA,FMWkMon,
	FMWkDays,FMTADays,FMOverHour,FMLDDays,
	FMLd01ChanJian,FMLd02ShiJia,FMLd03TanQin,FMLd04BinJia,
	FMLd05PeiChan,FMLd06NianXiu,FMLd07SangJia,FMLd08HunJia,
	FMLd09TiaoXiu,FMLd10GongShang,FMLd12ChanJia,FMLdxxOther,
	FMLateInDays,FMEarlyOutDays,FMAbsDays,FMNote,FMRealWkDs,
	FMLDDays2,FMAch
)
select 
	FID,FMEntryID,FMSEQ,
	FDeptID,FEmpID,FWorkOrgID,FPostID,FPostClass,FUserNA,FWkDate,
	FMWkDays,FMTADays,FOverHour,FMLDDays,
	FLd01ChanJian,FLd02ShiJia,FLd03TanQin,FLd04BinJia,
	FLd05PeiChan,FLd06NianXiu,FLd07SangJia,FLd08HunJia,
	FLd09TiaoXiu,FLd10GongShang,FLd12ChanJia,FLdxxOther,
	FMLateInDays,FMEarlyOutDays,FMAbsDays,FMNote,FMRealWkDs,
	FMLDDays2,FMAch
from #t 

select @maxEID=isnull(MAX(FMEntryID),@curEID) from ora_hr_EmpWkDtMon
if(@maxEID>@curEID)
begin
	DBCC CHECKIDENT (Z_ora_hr_EmpWkDtMon,RESEED,@maxEID)
end

select 'OK' FBackM,'生成月统计 已执行' FBackMsg

drop table #t
end
------------- 
-- exec proc_czty_hrInsEmpWkDtMon @FID=100029 