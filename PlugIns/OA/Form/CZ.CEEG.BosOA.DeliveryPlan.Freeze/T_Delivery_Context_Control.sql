/*
 Navicat Premium Data Transfer

 Source Server         : CEEG_SQLServer_dev
 Source Server Type    : SQL Server
 Source Server Version : 12005000
 Source Host           : 10.4.200.191:1433
 Source Catalog        : NTB
 Source Schema         : dbo

 Target Server Type    : SQL Server
 Target Server Version : 12005000
 File Encoding         : 65001

 Date: 13/05/2022 15:07:07
*/


-- ----------------------------
-- Table structure for T_Delivery_Context_Control
-- ----------------------------
IF EXISTS (SELECT * FROM sys.all_objects WHERE object_id = OBJECT_ID(N'[dbo].[T_Delivery_Context_Control]') AND type IN ('U'))
	DROP TABLE [dbo].[T_Delivery_Context_Control]
GO

CREATE TABLE [dbo].[T_Delivery_Context_Control] (
  [FID] int  NOT NULL,
  [FUserId] int  NULL,
  [FContext] varchar(255) COLLATE Chinese_PRC_CI_AS  NULL,
  [FDesc] varchar(255) COLLATE Chinese_PRC_CI_AS  NULL
)
GO

ALTER TABLE [dbo].[T_Delivery_Context_Control] SET (LOCK_ESCALATION = TABLE)
GO

EXEC sp_addextendedproperty
'MS_Description', N'主键',
'SCHEMA', N'dbo',
'TABLE', N'T_Delivery_Context_Control',
'COLUMN', N'FID'
GO

EXEC sp_addextendedproperty
'MS_Description', N'用户ID',
'SCHEMA', N'dbo',
'TABLE', N'T_Delivery_Context_Control',
'COLUMN', N'FUserId'
GO

EXEC sp_addextendedproperty
'MS_Description', N'用户能写操作的标识',
'SCHEMA', N'dbo',
'TABLE', N'T_Delivery_Context_Control',
'COLUMN', N'FContext'
GO

EXEC sp_addextendedproperty
'MS_Description', N'描述',
'SCHEMA', N'dbo',
'TABLE', N'T_Delivery_Context_Control',
'COLUMN', N'FDesc'
GO


-- ----------------------------
-- Records of T_Delivery_Context_Control
-- ----------------------------
INSERT INTO [dbo].[T_Delivery_Context_Control] ([FID], [FUserId], [FContext], [FDesc]) VALUES (N'1', N'0', N'FOrderId', N'销售订单号')
GO

INSERT INTO [dbo].[T_Delivery_Context_Control] ([FID], [FUserId], [FContext], [FDesc]) VALUES (N'2', N'0', N'FCustId', N'客户名称')
GO

INSERT INTO [dbo].[T_Delivery_Context_Control] ([FID], [FUserId], [FContext], [FDesc]) VALUES (N'3', N'0', N'FSALERID', N'销售员')
GO

INSERT INTO [dbo].[T_Delivery_Context_Control] ([FID], [FUserId], [FContext], [FDesc]) VALUES (N'4', N'0', N'FProductModel', N'产品型号')
GO

INSERT INTO [dbo].[T_Delivery_Context_Control] ([FID], [FUserId], [FContext], [FDesc]) VALUES (N'5', N'0', N'FOrderNum', N'订单数量')
GO

INSERT INTO [dbo].[T_Delivery_Context_Control] ([FID], [FUserId], [FContext], [FDesc]) VALUES (N'6', N'0', N'FPlanDeliveryDate', N'合同交货日期')
GO

INSERT INTO [dbo].[T_Delivery_Context_Control] ([FID], [FUserId], [FContext], [FDesc]) VALUES (N'7', N'0', N'FPlannedDeliveryDate', N'计划交货日期')
GO

INSERT INTO [dbo].[T_Delivery_Context_Control] ([FID], [FUserId], [FContext], [FDesc]) VALUES (N'8', N'0', N'FLateDelivery', N'迟交/早完成')
GO

INSERT INTO [dbo].[T_Delivery_Context_Control] ([FID], [FUserId], [FContext], [FDesc]) VALUES (N'9', N'0', N'FDeliverySchedule', N'交货进度')
GO

INSERT INTO [dbo].[T_Delivery_Context_Control] ([FID], [FUserId], [FContext], [FDesc]) VALUES (N'10', N'0', N'FSCMManager', N'供应链经理')
GO

INSERT INTO [dbo].[T_Delivery_Context_Control] ([FID], [FUserId], [FContext], [FDesc]) VALUES (N'11', N'0', N'FSCMPro1', N'供应专业一号（曹）')
GO

INSERT INTO [dbo].[T_Delivery_Context_Control] ([FID], [FUserId], [FContext], [FDesc]) VALUES (N'12', N'0', N'FSCMPro2', N'供应专业二号（丁）')
GO

INSERT INTO [dbo].[T_Delivery_Context_Control] ([FID], [FUserId], [FContext], [FDesc]) VALUES (N'13', N'0', N'FPurchase1', N'魏敏
')
GO

INSERT INTO [dbo].[T_Delivery_Context_Control] ([FID], [FUserId], [FContext], [FDesc]) VALUES (N'14', N'0', N'FPurchase2', N'徐晓清
')
GO

INSERT INTO [dbo].[T_Delivery_Context_Control] ([FID], [FUserId], [FContext], [FDesc]) VALUES (N'15', N'0', N'FPurchase3', N'高文峰
')
GO

INSERT INTO [dbo].[T_Delivery_Context_Control] ([FID], [FUserId], [FContext], [FDesc]) VALUES (N'16', N'0', N'FAuxiliaryMaterial1', N'辅材1
')
GO

INSERT INTO [dbo].[T_Delivery_Context_Control] ([FID], [FUserId], [FContext], [FDesc]) VALUES (N'17', N'0', N'FProductionFeedback', N'生产反馈
')
GO

INSERT INTO [dbo].[T_Delivery_Context_Control] ([FID], [FUserId], [FContext], [FDesc]) VALUES (N'18', N'100458', NULL, NULL)
GO

INSERT INTO [dbo].[T_Delivery_Context_Control] ([FID], [FUserId], [FContext], [FDesc]) VALUES (N'19', N'192614', NULL, NULL)
GO


-- ----------------------------
-- Primary Key structure for table T_Delivery_Context_Control
-- ----------------------------
ALTER TABLE [dbo].[T_Delivery_Context_Control] ADD CONSTRAINT [PK__T_Delive__C1BEA5A2457953D2] PRIMARY KEY CLUSTERED ([FID])
WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON)  
ON [PRIMARY]
GO

