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

 Date: 19/05/2022 10:49:19
*/


-- ----------------------------
-- Table structure for T_Delivery_Context_Control_copy1
-- ----------------------------
IF EXISTS (SELECT * FROM sys.all_objects WHERE object_id = OBJECT_ID(N'[dbo].[T_Delivery_Context_Control_copy1]') AND type IN ('U'))
	DROP TABLE [dbo].[T_Delivery_Context_Control_copy1]
GO

CREATE TABLE [dbo].[T_Delivery_Context_Control_copy1] (
  [FID] int  NOT NULL,
  [FUserId] int  NULL,
  [FContext] varchar(255) COLLATE Chinese_PRC_CI_AS  NULL,
  [FDescZD] varchar(255) COLLATE Chinese_PRC_CI_AS  NULL,
  [FDescNTB] varchar(255) COLLATE Chinese_PRC_CI_AS  NULL
)
GO

ALTER TABLE [dbo].[T_Delivery_Context_Control_copy1] SET (LOCK_ESCALATION = TABLE)
GO

EXEC sp_addextendedproperty
'MS_Description', N'主键',
'SCHEMA', N'dbo',
'TABLE', N'T_Delivery_Context_Control_copy1',
'COLUMN', N'FID'
GO

EXEC sp_addextendedproperty
'MS_Description', N'用户ID',
'SCHEMA', N'dbo',
'TABLE', N'T_Delivery_Context_Control_copy1',
'COLUMN', N'FUserId'
GO

EXEC sp_addextendedproperty
'MS_Description', N'用户能写操作的标识',
'SCHEMA', N'dbo',
'TABLE', N'T_Delivery_Context_Control_copy1',
'COLUMN', N'FContext'
GO

EXEC sp_addextendedproperty
'MS_Description', N'中电描述',
'SCHEMA', N'dbo',
'TABLE', N'T_Delivery_Context_Control_copy1',
'COLUMN', N'FDescZD'
GO

EXEC sp_addextendedproperty
'MS_Description', N'特变描述',
'SCHEMA', N'dbo',
'TABLE', N'T_Delivery_Context_Control_copy1',
'COLUMN', N'FDescNTB'
GO


-- ----------------------------
-- Records of T_Delivery_Context_Control_copy1
-- ----------------------------
INSERT INTO [dbo].[T_Delivery_Context_Control_copy1] ([FID], [FUserId], [FContext], [FDescZD], [FDescNTB]) VALUES (N'1', NULL, N'FOrderId', N'销售订单号', N'销售订单号')
GO

INSERT INTO [dbo].[T_Delivery_Context_Control_copy1] ([FID], [FUserId], [FContext], [FDescZD], [FDescNTB]) VALUES (N'2', NULL, N'FCustId', N'客户名称', N'客户名称')
GO

INSERT INTO [dbo].[T_Delivery_Context_Control_copy1] ([FID], [FUserId], [FContext], [FDescZD], [FDescNTB]) VALUES (N'3', NULL, N'FSALERID', N'销售员', N'销售员')
GO

INSERT INTO [dbo].[T_Delivery_Context_Control_copy1] ([FID], [FUserId], [FContext], [FDescZD], [FDescNTB]) VALUES (N'4', NULL, N'FProductModel', N'产品型号', N'产品型号')
GO

INSERT INTO [dbo].[T_Delivery_Context_Control_copy1] ([FID], [FUserId], [FContext], [FDescZD], [FDescNTB]) VALUES (N'5', NULL, N'FOrderNum', N'订单数量', N'订单数量')
GO

INSERT INTO [dbo].[T_Delivery_Context_Control_copy1] ([FID], [FUserId], [FContext], [FDescZD], [FDescNTB]) VALUES (N'6', NULL, N'FPlanDeliveryDate', N'合同交货日期', N'合同交货日期')
GO

INSERT INTO [dbo].[T_Delivery_Context_Control_copy1] ([FID], [FUserId], [FContext], [FDescZD], [FDescNTB]) VALUES (N'7', NULL, N'FPlannedDeliveryDate', N'计划交货日期', N'计划交货日期')
GO

INSERT INTO [dbo].[T_Delivery_Context_Control_copy1] ([FID], [FUserId], [FContext], [FDescZD], [FDescNTB]) VALUES (N'8', NULL, N'FLateDelivery', N'迟交/早完成', N'迟交/早完成')
GO

INSERT INTO [dbo].[T_Delivery_Context_Control_copy1] ([FID], [FUserId], [FContext], [FDescZD], [FDescNTB]) VALUES (N'9', NULL, N'FDeliverySchedule', N'交货进度', N'交货进度')
GO

INSERT INTO [dbo].[T_Delivery_Context_Control_copy1] ([FID], [FUserId], [FContext], [FDescZD], [FDescNTB]) VALUES (N'10', NULL, N'FSCMManager', N'供应链经理', N'供应链经理')
GO

INSERT INTO [dbo].[T_Delivery_Context_Control_copy1] ([FID], [FUserId], [FContext], [FDescZD], [FDescNTB]) VALUES (N'11', NULL, N'FSCMPro1', N'供应专业一号（曹）', N'供应专员陈')
GO

INSERT INTO [dbo].[T_Delivery_Context_Control_copy1] ([FID], [FUserId], [FContext], [FDescZD], [FDescNTB]) VALUES (N'12', NULL, N'FSCMPro2', N'供应专业二号（丁）', N'尹延富')
GO

INSERT INTO [dbo].[T_Delivery_Context_Control_copy1] ([FID], [FUserId], [FContext], [FDescZD], [FDescNTB]) VALUES (N'13', NULL, N'FPurchase1', N'魏敏
', N'
王梦君')
GO

INSERT INTO [dbo].[T_Delivery_Context_Control_copy1] ([FID], [FUserId], [FContext], [FDescZD], [FDescNTB]) VALUES (N'14', NULL, N'FPurchase2', N'徐晓清
', N'吴文祥
')
GO

INSERT INTO [dbo].[T_Delivery_Context_Control_copy1] ([FID], [FUserId], [FContext], [FDescZD], [FDescNTB]) VALUES (N'15', NULL, N'FPurchase3', N'高文峰
', N'郭成林
')
GO

INSERT INTO [dbo].[T_Delivery_Context_Control_copy1] ([FID], [FUserId], [FContext], [FDescZD], [FDescNTB]) VALUES (N'16', NULL, N'FAuxiliaryMaterial1', N'辅材1
', N'施伟
')
GO

INSERT INTO [dbo].[T_Delivery_Context_Control_copy1] ([FID], [FUserId], [FContext], [FDescZD], [FDescNTB]) VALUES (N'17', NULL, N'FProductionFeedback', N'生产反馈
', N'徐颖')
GO

INSERT INTO [dbo].[T_Delivery_Context_Control_copy1] ([FID], [FUserId], [FContext], [FDescZD], [FDescNTB]) VALUES (N'18', NULL, NULL, NULL, NULL)
GO


-- ----------------------------
-- Primary Key structure for table T_Delivery_Context_Control_copy1
-- ----------------------------
ALTER TABLE [dbo].[T_Delivery_Context_Control_copy1] ADD CONSTRAINT [PK__T_Delive__C1BEA5A2457953D2_copy1] PRIMARY KEY CLUSTERED ([FID])
WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON)  
ON [PRIMARY]
GO

