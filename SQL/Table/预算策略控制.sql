-- 预算策略控制
CREATE TABLE [dbo].[CZ_BDG_COSTITEMCTRL](
	[FID] [int] PRIMARY KEY IDENTITY(1,1) NOT NULL,
	[FBraOffice] [int] NULL,
	[FCtrl4Prj] [int] NOT NULL DEFAULT(1)
)

