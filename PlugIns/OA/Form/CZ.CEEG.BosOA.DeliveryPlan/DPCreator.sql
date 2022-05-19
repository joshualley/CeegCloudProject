/*
 Navicat Premium Data Transfer

 Source Server         : CEEG_SQLServer_prod
 Source Server Type    : SQL Server
 Source Server Version : 12005000
 Source Host           : 10.4.200.187:1433
 Source Catalog        : AIS202104NTB
 Source Schema         : dbo

 Target Server Type    : SQL Server
 Target Server Version : 12005000
 File Encoding         : 65001

 Date: 19/05/2022 10:48:01
*/


-- ----------------------------
-- Table structure for DPCreator
-- ----------------------------
IF EXISTS (SELECT * FROM sys.all_objects WHERE object_id = OBJECT_ID(N'[dbo].[DPCreator]') AND type IN ('U'))
	DROP TABLE [dbo].[DPCreator]
GO

CREATE TABLE [dbo].[DPCreator] (
  [FUserId] nchar(10) COLLATE Chinese_PRC_CI_AS  NULL
)
GO

ALTER TABLE [dbo].[DPCreator] SET (LOCK_ESCALATION = TABLE)
GO

