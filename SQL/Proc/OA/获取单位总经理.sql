--获取单位总经理
CREATE PROC [dbo].[proc_czly_GetGManager](
	@FOrgId INT,
	@FDeptId INT
)
AS
BEGIN

DECLARE @FGManager INT=0
DECLARE @FDeptNumber VARCHAR(100)=(select FNUMBER from T_BD_DEPARTMENT where FDEPTID=@FDeptId)
DECLARE @FLevelCode VARCHAR(100)=(select FLEVELCODE from T_BD_DEPARTMENT where FNUMBER=@FDeptNumber and  FUSEORGID=1)

set @FGManager=case @FOrgId
	when 156139 then  --中电电气（江苏）变压器制造有限公司
		case dbo.Fun_GetValueAt(@FLevelCode, '.', 4)
		when '295112' then ( --总经办
			select top 1 FGMANAGERID from ora_OA_GManager gm
			--inner join T_BD_DEPARTMENT d on gm.FDEPTID=d.FDEPTID
			where FORGID=@FOrgId and gm.FDEPTID=dbo.Fun_GetValueAt(@FLevelCode, '.', 5)
		)
		else (
			select top 1 FGMANAGERID from ora_OA_GManager gm
			--inner join T_BD_DEPARTMENT d on gm.FDEPTID=d.FDEPTID
			where FORGID=@FOrgId and gm.FDEPTID=dbo.Fun_GetValueAt(@FLevelCode, '.', 4)
		)
		end
	else 
		(select top 1 FGMANAGERID from ora_OA_GManager where FORGID=@FOrgId)
	end


--set @FGManager = case (select ol.FNAME from T_ORG_ORGANIZATIONS_L ol where ol.FORGID=@FOrgId and ol.FLOCALEID=2052)
--when '华思信息技术' then (select FID from T_HR_EMPINFO_L where FNAME='刘跃' and FLOCALEID=2052) --华思
--when '营销中心' then (select FID from T_HR_EMPINFO_L where FNAME='张新荣' and FLOCALEID=2052) --营销中心
--when '杜邦技术产品中心' then (select FID from T_HR_EMPINFO_L where FNAME='陈正明' and FLOCALEID=2052) --杜邦技术
--when '战略客户中心' then (select FID from T_HR_EMPINFO_L where FNAME='何春红' and FLOCALEID=2052) --战略客户
--when '干变事业部' then (select FID from T_HR_EMPINFO_L where FNAME='陈晓冬' and FLOCALEID=2052) --干变事业部
--when '中高压事业部' then (select FID from T_HR_EMPINFO_L where FNAME='郭明军' and FLOCALEID=2052) --中高压事业部
--when '成套事业部' then (select FID from T_HR_EMPINFO_L where FNAME='孙红梅' and FLOCALEID=2052) --成套事业部
----中电电气（江苏）变压器制造有限公司
--when '中电电气（江苏）变压器制造有限公司' then 
--	case (SELECT FNAME FROM T_BD_DEPARTMENT_L DL WHERE DL.FDEPTID=@FDeptId AND FLOCALEID=2052)
--	when '财务中心' then (select FID from T_HR_EMPINFO_L where FNAME='陆素霞' and FLOCALEID=2052) --财务中心
--	when '科技中心' then (select FID from T_HR_EMPINFO_L where FNAME='白忠东' and FLOCALEID=2052) --科技中心
--	when '采购中心' then (select FID from T_HR_EMPINFO_L where FNAME='周传兵' and FLOCALEID=2052) --采购中心
--	when '行政部' then (select FID from T_HR_EMPINFO_L where FNAME='祝瑞章' and FLOCALEID=2052) 
--	when '风控部' then (select FID from T_HR_EMPINFO_L where FNAME='孙敏' and FLOCALEID=2052)
--	when '人力资源部' then (select FID from T_HR_EMPINFO_L where FNAME='倪青峰' and FLOCALEID=2052) --总经办风控部
--	when '总经办' then (select FID from T_HR_EMPINFO_L where FNAME='陆瀚' and FLOCALEID=2052) --总经办其他
--	end
--end

SELECT FID, FNAME FROM T_HR_EMPINFO_L WHERE FID=@FGManager

END

-- EXEC proc_czly_GetGManager @FOrgId='', @FDeptId=''
-- EXEC proc_czly_GetGManager @FOrgId='0', @FDeptId='0'
-- EXEC proc_czly_GetGManager @FOrgId='175324', @FDeptId='295149'
