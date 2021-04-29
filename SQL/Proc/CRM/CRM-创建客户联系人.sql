--创建客户联系人
CREATE PROC [dbo].[proc_czly_CreateCustContactor](
	@FName VARCHAR(50),   --联系人姓名
	@FMobile VARCHAR(50), --联系电话
	@FUserID INT,         --用户ID
	@FCustID INT          --客户ID
)
AS
BEGIN

	--DECLARE @FName VARCHAR(50)   --联系人姓名
	--DECLARE @FMobile VARCHAR(50) --联系电话
	--DECLARE @FUserID INT         --用户ID
	--DECLARE @FCustID INT         --客户ID

	DECLARE @FContactId INT = IDENT_CURRENT('Z_BD_COMMONCONTACT')+1 --获取ID
	DECLARE @FNumber VARCHAR(50)
	DECLARE @Date DATETIME=GETDATE()

	DECLARE @prefix VARCHAR(10) = 'CXR'
	DECLARE @serial_num INT
	SELECT @serial_num=MAX(CONVERT(INT,RIGHT(FNumber, 6)))+1 FROM T_BD_COMMONCONTACT
	SELECT @serial_num=ISNULL(@serial_num, 1)
	--更新公共联系人的编码规则表中的最大编码
	UPDATE T_BAS_BILLCODES SET FNUMMAX=@serial_num WHERE FCODEID=10152 --公共联系人编码规则表
	SET @FNumber=dbo.fun_BosRnd_addSpace(@serial_num, @prefix, '', 6) --生成编码

	INSERT INTO T_BD_COMMONCONTACT(
		FCONTACTID,FMASTERID,FNUMBER,FCOMPANYTYPE,FMOBILE,
		FDOCUMENTSTATUS,FFORBIDSTATUS,FCREATORID,FCREATEDATE,FMODIFIERID,FMODIFYDATE,FCUSTID,FCOMPANY,FISDEFAULTCONTACT
	) VALUES (
		@FContactId,@FContactId,@FNumber,'BD_Customer',@FMobile,
		'C','A',@FUserID,@Date,@FUserID,@Date,@FCustID,@FCustID,1
	)
	DBCC CHECKIDENT (Z_BD_COMMONCONTACT, RESEED, @FContactId)
	--插入L表
	DECLARE @FPKID INT = IDENT_CURRENT('Z_BD_COMMONCONTACT_L')+1
	INSERT INTO T_BD_COMMONCONTACT_L(FPKID,FCONTACTID,FLOCALEID,FNAME) VALUES(@FPKID,@FContactId,2052,@FName)
	DBCC CHECKIDENT (Z_BD_COMMONCONTACT_L, RESEED, @FPKID)

	select @FContactId as FContactId
END


--select * from T_BD_COMMONCONTACT_L
--EXEC proc_czly_CreateCustContactor @FName='倪可爱',@FMobile='17856387939',@FUserID='100560',@FCustID='291310'

/*
SELECT * FROM T_BAS_BILLCODES c
INNER JOIN T_BAS_BILLCODERULE_L crl on c.FRULEID=crl.FRULEID
WHERE FNAME LIKE '%联系人%'
*/
