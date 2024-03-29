--ͨ获取销售员信息
CREATE PROC [dbo].[proc_czly_GetOrgDeptBySalemanId]
	@SmId INT,
	@OrgId INT=-1
AS
BEGIN
IF @OrgId <> -1 
BEGIN
    SELECT @SmId=FID FROM V_BD_SALESMAN WHERE FBIZORGID=@OrgId and FNUMBER=(SELECT FNUMBER FROM V_BD_SALESMAN WHERE FID=@SmId)
END
ELSE
BEGIN
    SELECT s.FID FSalesmanId, d.FDeptID, d.FUSEORGID FOrgID, e.FMobile, bizd.FDEPTID FBizDeptID, s.FBIZORGID
    FROM (SELECT * FROM V_BD_SALESMAN WHERE FID=@SmId) s
    INNER JOIN T_HR_EMPINFO e ON s.FNUMBER=e.FNUMBER
    INNER JOIN T_BD_STAFFTEMP se ON e.FID=se.FID AND se.FIsFirstPost='1'
    INNER JOIN T_BD_DEPARTMENT d ON d.FDEPTID=dbo.fun_czty_GetWorkDeptID(se.FDEPTID)
    INNER JOIN T_BD_DEPARTMENT bizd ON d.FNUMBER=bizd.FNUMBER AND bizd.FUSEORGID=s.FBIZORGID
END

END

-- EXEC proc_czly_GetOrgDeptBySalemanId @SmId='543498', @OrgId=156139
