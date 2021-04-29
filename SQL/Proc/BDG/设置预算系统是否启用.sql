--设置预算系统是否启用
CREATE PROC [dbo].[proc_cz_ly_UsingBdgSysSetting](@FSwitch INT)
AS
BEGIN
	UPDATE CZ_BDG_SETTING SET FSwitch=@FSwitch WHERE FID=1
END

