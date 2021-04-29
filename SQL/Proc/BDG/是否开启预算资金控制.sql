--是否开启预算资金控制
CREATE PROC [dbo].[proc_cz_ly_IsUsingBdgSys]
AS
BEGIN
	SELECT FSwitch FROM CZ_BDG_SETTING WHERE FID=1
END

