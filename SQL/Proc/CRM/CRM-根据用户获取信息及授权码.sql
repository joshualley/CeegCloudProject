-- CRM-根据用户获取信息及授权码
/*---------- CRM 跟据传入用户获取 部门、层级码、岗位信息 ----------*/
create proc [dbo].[proc_cztyCrm_GetCrmSN4U]
@FUserID		int
as
begin
set nocount on
--用户-职员 授权组织.部门 层码
select u.FUserID,u.FNAME FUserName,e.FID FEmpID,ec.FCrmOrgID,ec.FCrmDeptID,ec.FCrmSN,ec.FCrmSch,ec.fCrmCtrl 
--into #ec
from(select * from T_SEC_USER where FUSERID=@FUserID )u 
inner join V_bd_ContactObject c on u.FLinkObject=c.FID 
inner join T_HR_EMPINFO e on c.FNumber=e.FNumber 
inner join T_HR_EMPINFOCrmCR ec on e.FID=ec.FID 
where ec.FCrmSch=1 and len(ec.FCrmSN)>0
end
--------------
-- exec proc_cztyCrm_GetCrmSN4U @FUserID='157198'
