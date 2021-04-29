-- 创建单据关联
create PROCEDURE [dbo].[proc_czly_CreateBillRelation]
@lktable varchar(30),--下游单据关联表
@targetfid int,--下游单据头内码
@targettable varchar(30),--下游单据头表名
@targetformid varchar(36),--下游单据标识
@sourcefid int,--上游单据头内码
@sourcetable varchar(30),--上游单据头表名
@sourceformid varchar(36),--上游单据标识
@sourcefentryid int = 0, --上游单据体内码
@sourcefentrytable varchar(30) = '' -- 上游单据体表名
AS
--获取源单编号
DECLARE @billNo NVARCHAR(255)
DECLARE @getSourceBillSql NVARCHAR(255)
SELECT @getSourceBillSql = N'SELECT @a = FBILLNO  FROM  '+REPLACE(@sourcetable,'1','')+' WHERE  FID = '+ CONVERT(NVARCHAR(255),@sourcefid)
EXEC sp_executesql @getSourceBillSql,N'@a nvarchar(255) output',@billNo OUTPUT
DECLARE @sourceT VARCHAR(30)
DECLARE @sourceID INT
--决定关联的是上游单据体还是单据头
SELECT @sourceT = (CASE WHEN @sourcefentrytable= '' THEN @sourcetable ELSE @sourcefentrytable END)
SELECT @sourceID = (CASE WHEN @sourcefentryid = 0 THEN @sourcefid ELSE @sourcefentryid END)
--插入lk表
--判断lk表中有无该记录
DECLARE @lkCount INT
DECLARE @judgeSql NVARCHAR(255)
SELECT @lkCount = 0,@judgeSql = N'SELECT @c=COUNT(*) FROM '+@lktable+' WHERE FSTableName = '''+@sourcetable+''' AND FSBillId = '+CONVERT(NVARCHAR(255),@sourcefid)+' AND FSId = '+CONVERT(NVARCHAR(255),@sourceID)+''
EXEC sp_executesql @judgeSql,N'@c int output',@lkCount OUTPUT
IF(@lkCount = 0 )
BEGIN
      --获取主键
      DECLARE @flinkid INT
      DECLARE @getZIDSql NVARCHAR(255)
      EXEC('INSERT INTO Z_'+@lktable+' (Column1)VALUES(1)')
      SELECT @getZIDSql = N'SELECT @b=Id FROM Z_'+@lktable
      EXEC sp_executesql @getZIDSql,N'@b int output',@flinkid OUTPUT
      EXEC('delete Z_'+@lktable)
      DECLARE @ruleid VARCHAR(36)
      --获取转换规则
      SELECT TOP 1 @ruleid = FID FROM T_META_CONVERTRULE WHERE FSOURCEFORMID= @sourceformid AND FTARGETFORMID= @targetformid
      --lk表,下游单据内码,主键,'',0,转换规则,0,上游表名,上游单据头内码,上游单据体内码[如果源单主关联实体是单据头，则此属性也填写源单单据内码]
      EXEC('INSERT '+@lktable+' ( FID ,FLinkId ,FFlowId,FFlowLineId ,FRuleId ,FSTableId ,FSTableName ,FSBillId ,FSId) VALUES  ('+@targetfid+' , '+@flinkid+' ,'''' , 0 , '''+@ruleid+''' , 0 ,'''+@sourceT+''' , '''+@sourcefid+''' , '''+@sourceID+''' )')
END
--插入t_bf_instance
--流程实例编码
DECLARE @finstanceid varchar(36)
--如果两个单据之间已经有关联关系（即已下推），那么需要绑定到同一个流程之中
IF EXISTS(SELECT * FROM T_BF_INSTANCEENTRY WHERE FTTABLENAME= @sourceT AND FTID = @sourceID)
BEGIN
      SELECT TOP 1 @finstanceid = FINSTANCEID FROM T_BF_INSTANCEENTRY
      WHERE FTTABLENAME = @sourceT AND FTID = @sourceID ORDER BY FCREATETIME ASC
END
ELSE
BEGIN
    SELECT  @finstanceid = REPLACE(NEWID(), '-', '')
    INSERT  INTO T_BF_INSTANCE
            ( FINSTANCEID ,
              FFLOWID,
              FSOURCEID,
              FMASTERID,
              FSTATUS,
              FFIRSTFORMID,
              FFIRSTBILLID,
              FFIRSTBILLNO,
              FSTARTTIME
            )
    VALUES  ( @finstanceid , -- FINSTANCEID - varchar(36)
              '', -- FFLOWID -varchar(36)
              '', -- FSOURCEID -varchar(36)
              @finstanceid, -- FMASTERID -varchar(36)
              'A', -- FSTATUS -char(1)
              @sourceformid, -- FFIRSTFORMID -varchar(36) 上游单据标识
              @sourcefid, -- FFIRSTBILLID - int上游单据内码
              @billNo, -- FFIRSTBILLNO -nvarchar(160) 上游单据编号
              GETDATE()  -- FSTARTTIME - datetime
            )
END
--流程一般分为一个头、一个尾，现在判断有没有该流程实例的头
IF NOT EXISTS(SELECT * FROM T_BF_INSTANCEENTRY WHERE FTTABLENAME = @sourceT AND FINSTANCEID = @finstanceid AND FTID = @sourceID)
BEGIN
      --插入t_bf_instanceentry 上游单据
    INSERT  INTO T_BF_INSTANCEENTRY        
            ( FROUTEID ,
              FINSTANCEID,
              FLINEID,
              FSTABLENAME,
              FSID,
              FTTABLENAME,
              FTID,
              FFIRSTNODE,
              FCREATETIME
            )
    VALUES  ( REPLACE(NEWID(), '-', '') , -- FROUTEID - varchar(36)
              @finstanceid, -- FINSTANCEID -varchar(36)
              0 ,-- FLINEID - int
              '', -- FSTABLENAME -varchar(30)
              0 ,-- FSID - int
              @sourceT, -- FTTABLENAME -varchar(30) 上游单据体表名
              @sourceID, -- FTID - int 上游单据体内码
              '1', -- FFIRSTNODE -char(1)
              GETDATE()  -- FCREATETIME - datetime
            )
END
--现在是判断是否存在流程实例的尾
IF NOT EXISTS(SELECT * FROM dbo.T_BF_INSTANCEENTRY WHERE FINSTANCEID = @finstanceid AND FSTABLENAME = @sourceT AND
FSID = @sourceID AND FTTABLENAME = @targettable AND FTID = @targetfid)
BEGIN
      --插入t_bf_instanceentry 下游单据
      INSERT INTO T_BF_INSTANCEENTRY
                 ( FROUTEID ,
                   FINSTANCEID ,
                   FLINEID ,
                   FSTABLENAME ,
                   FSID ,
                   FTTABLENAME ,
                   FTID ,
                   FFIRSTNODE ,
                   FCREATETIME
                 )
      VALUES  ( REPLACE(NEWID(), '-', '') , -- FROUTEID - varchar(36)
                   @finstanceid , -- FINSTANCEID -varchar(36)
                   0 , -- FLINEID - int
                   @sourceT , -- FSTABLENAME -varchar(30)上游单据体表名
                   @sourceID , -- FSID - int 上游单据体内码
                   @targettable , -- FTTABLENAME -varchar(30) 下游单据表名
                   @targetfid , -- FTID - int 下游单据内码
                   '0' , -- FFIRSTNODE - char(1)
                   GETDATE()  -- FCREATETIME - datetime
                 )
END
