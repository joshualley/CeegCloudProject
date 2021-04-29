-- 获取同步到HR的数据
CREATE proc [dbo].[proc_czly_GetSynData](
	@FType varchar(44)
)
AS
BEGIN

--declare @FType varchar(44)='录用'

if @FType='录用'
begin
	--查询未同步的OA录用单
	select op.FID, op.FCreateDate,
	dbo.GetName('EMP',FApplyID,'') FApplyName, dbo.GetSHrID('EMP',FApplyID) FHrApplyID,
	dbo.GetName('ORG',FORGID,'') FOrgName, dbo.GetSHrID('ORG',FORGID) FHrOrgID,
	dbo.GetName('DEPT',FJoinDept,'') FDeptName, dbo.GetSHrID('DEPT',FJoinDept) FHrDeptID,
	dbo.GetName('POST',FJoinPost,'') FPostName, dbo.GetSHrID('POST',FJoinPost) FHrPostID,
	op.FName, FGender, FIdentityNum, FPassportNum, FPhone,FBirthday,
	FEntryDate, FProbation, FRegularDate,
	FSocialDate, dbo.GetName('ENUM',FEmpType,'雇佣类型') FEmpType, FSalary+FPosArrange FRemarks
	from ora_t_OfferProcess op
	where op.FDocumentStatus='C' and op.FIsSynHR=0
end
else if @FType='转正'
begin
	select w.FID,FCreateDate,
	dbo.GetName('EMP',FApplyID,'') FApplyName, dbo.GetSHrID('EMP',FApplyID) FHrApplyID,
	dbo.GetName('ORG',FORGID,'') FOrgName, dbo.GetSHrID('ORG',FORGID) FHrOrgID,
	dbo.GetName('EMP',FSTAFF,'') FStaffName, dbo.GetSHrID('EMP',FSTAFF) FHrEmpID,
	dbo.GetName('DEPT',FBeforeDeptID,'') FBeforeDeptName, dbo.GetSHrID('DEPT',FBeforeDeptID) FHrBeforeDeptID,
	dbo.GetName('POST',FBeforePost,'') FBeforePostName, dbo.GetSHrID('POST',FBeforePost) FHrBeforePostID,
	dbo.GetName('DEPT',FAfterDeptID,'') FAfterDeptName, dbo.GetSHrID('DEPT',FAfterDeptID) FHrAfterDeptID,
	dbo.GetName('POST',FAfterPost,'') FAfterPostName, dbo.GetSHrID('POST',FAfterPost) FHrAfterPostID,
	F_ora_toDate FRegularDate,F_ora_FExplain FRemarks
	from ora_t_Work w
	where FDocumentStatus='C' and FIsSynHR=0
end
else if @FType='调职'
begin
	select FID,FCreateDate,
	dbo.GetName('EMP',FApplyID,'') FApplyName, dbo.GetSHrID('EMP',FApplyID) FHrApplyID,
	dbo.GetName('ORG',FORGID,'') FOrgName, dbo.GetSHrID('ORG',FORGID) FHrOrgID,
	dbo.GetName('DEPT',F_ora_OutDept,'') FOutDeptName, dbo.GetSHrID('DEPT',F_ora_OutDept) FHrOutDeptID,
	dbo.GetName('POST',F_ora_OutPost,'') FOutPostName, dbo.GetSHrID('POST',F_ora_OutPost) FHrOutPostID,
	dbo.GetName('DEPT',F_ora_InDept,'') FInDeptName, dbo.GetSHrID('DEPT',F_ora_InDept) FHrInDeptID,
	dbo.GetName('POST',F_ora_InPost,'') FInPostName, dbo.GetSHrID('POST',F_ora_InPost) FHrInPostID,
	F_ora_OutDate FOutDate, F_ora_InDate FInDate, 
	F_ora_BeforeAddr FBeforeAddr, F_ora_AfterAddr FAfterAddr,
	F_ora_Cause+F_ora_Job FRemarks
	from ora_t_Transfer
	where FDocumentStatus='C' and FIsSynHR=0
end
else if @FType='离职'
begin
	select FID, FCreateDate,
	dbo.GetName('EMP',FApplyID,'') FApplyName, dbo.GetSHrID('EMP',FApplyID) FHrApplyID,
	dbo.GetName('ORG',FORGID,'') FOrgName, dbo.GetSHrID('ORG',FORGID) FHrOrgID,
	dbo.GetName('DEPT',FDeptID,'') FDeptName, dbo.GetSHrID('DEPT',FDeptID) FHrDeptID,
	dbo.GetName('POST',FPost,'') FPostName, dbo.GetSHrID('POST',FPost) FHrPostID,
	FQuitDate, FReason+FJob FRemarks
	from ora_t_Dimission
	where FDocumentStatus='C' and FIsSynHR=0
end

END

--exec proc_czly_GetSynData @FType='录用'
--exec proc_czly_GetSynData @FType='转正'
--exec proc_czly_GetSynData @FType='调职'
--exec proc_czly_GetSynData @FType='离职'