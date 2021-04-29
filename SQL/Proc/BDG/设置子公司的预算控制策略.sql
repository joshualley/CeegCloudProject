--设置子公司的预算控制策略
CREATE PROC [dbo].[proc_czly_SetPrjCtrl](
	@FBraOffice INT,
	@FCtrl4Prj INT
)
AS
BEGIN 
	UPDATE CZ_BDG_COSTITEMCTRL SET FCtrl4Prj=@FCtrl4Prj WHERE FBraOffice=@FBraOffice
END
