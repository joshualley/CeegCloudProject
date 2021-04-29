--同步HR员工信息
CREATE proc [dbo].[proc_czly_SynEmpInfo](
	@FHrPID varchar(44),
	@FHrDeptID varchar(44),
	@FHrPostID varchar(44),
	@FWorkDate datetime, --参加工作
	@FJoinDate datetime, --入职
	@FGender int,
	@FBirthday datetime
)
AS
BEGIN

update e
set e.FJoinDate=@FWorkDate,e.F_HR_BOBDATE=@FJoinDate,
	e.FSOCIETYYEAR=dbo.fn_GetWorkYear(@FWorkDate, GETDATE()),
	--F_HR_SEX=@FGender,F_HR_BORNDATE=@FBirthday,
	e.F_ORA_DEPTID=(SELECT TOP 1 FDEPTID FROM T_BAS_HRCLOUDMAPPING4D WHERE FSHRID=@FHrDeptID AND FSTATUS=1),
	e.F_ORA_POST=(SELECT TOP 1 FPOSTID FROM T_BAS_HRCLOUDMAPPING4H WHERE FSHRID=@FHrPostID AND FSTATUS=1)
from T_HR_EMPINFO e
inner join T_BAS_HRCLOUDMAPPING4P hrp on e.FNUMBER=hrp.FNUMBER AND FSTATUS=1 AND hrp.FSHRID=@FHrPID
--where dbo.GetSHrID('EMP',FID)=@FHrPID

END

/*
exec proc_czly_SynEmpInfo 
	@FHrPID='BC0hplT8SWeLtgPIbqU17IDvfe0=',@FHrDeptID='市场营销部',@FHrPostID='海外销售代表',
	@FWorkDate='2003-07-01',@FJoinDate='2003-07-01',
	@FGender='0',@FBirthday='1991-01-16'

select FID, dbo.GetSHrID('EMP',FID) FHrPID, 
dbo.GetName('EMP',FID,'') FName, F_HR_BORNDATE from T_HR_EMPINFO 
*/