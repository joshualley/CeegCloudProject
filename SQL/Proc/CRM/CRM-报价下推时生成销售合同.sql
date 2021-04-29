--报价下推时生成销售合同
CREATE PROC [dbo].[proc_czly_CRMGeneContact](
	@FID INT,
	@FUserId INT
)
AS
BEGIN

--select * from ora_CRM_SaleOffer

--declare @FID INT = 110261
--declare @FUserId INT=100560

--获取编码规则
DECLARE @FBillNo VARCHAR(50)
DECLARE @prefix VARCHAR(10) --编码规则前缀
DECLARE @FRULEID VARCHAR(50)
SELECT @FRULEID=FRULEID FROM T_BAS_BILLCODERULE where FBILLFORMID='ora_CRM_Contract'
SELECT @prefix=FPROJECTVALUE FROM T_BAS_BILLCODERULEENTRY WHERE FRULEID=@FRULEID AND FPROJECTTYPE=1
DECLARE @date VARCHAR(10) = LEFT(CONVERT(varchar(100), GETDATE(), 112), 6) --日期yyyyMM
SET @prefix += @date
--SELECT FFORMAT FROM T_BAS_BILLCODERULEENTRY WHERE FRULEID='5e0301cb366088' AND FPROJECTTYPE=2
--DECLARE @serial_num INT
--SELECT FLENGTH FROM T_BAS_BILLCODERULEENTRY WHERE FRULEID='5e0301cb366088' AND FPROJECTTYPE=16

--获取本月本单中最大的编码
DECLARE @MaxNum INT
SELECT  @MaxNum=MAX(RIGHT(FBILLNO, 3))+1 FROM ora_CRM_Contract
WHERE LEFT(RIGHT(FBILLNO, 9),4)=YEAR(GETDATE()) AND LEFT(RIGHT(FBILLNO, 5),2)=MONTH(GETDATE())
SET @MaxNum=ISNULL(@MaxNum, 1)


--SELECT @serial_num=FNUMMAX+1 FROM T_BAS_BILLCODES WHERE FRULEID=@FRULEID
--SELECT @serial_num=ISNULL(@serial_num, 1)
UPDATE T_BAS_BILLCODES SET FNUMMAX=@MaxNum WHERE FRULEID=@FRULEID
SET @FBillNo=dbo.fun_BosRnd_addSpace(@MaxNum, @prefix, '', 3) --生成编码
--生成主键
DECLARE @CFID INT = IDENT_CURRENT('Z_ora_CRM_Contract') + 1
DBCC CHECKIDENT(Z_ora_CRM_Contract, RESEED, @CFID)
--通过用户获取销售员
DECLARE @FSaler INT
--DECLARE @FBizOrg INT
--DECLARE @FDept INT
SELECT @FSaler=s.FID--,@FBizOrg=s.FBIZORGID,@FDept=s.FDEPTID
FROM (SELECT FUserId,FName,FLinkObject FROM T_SEC_USER WHERE FUSERID=@FUserId) u
INNER JOIN V_bd_ContactObject c ON u.FLINKOBJECT=c.FID
INNER JOIN T_HR_EMPINFO e ON c.FNumber=e.FNumber
INNER JOIN V_BD_SALESMAN s ON e.FSTAFFID=s.FSTAFFID

SET @FSaler = ISNULL(@FSaler, 0)

--产品大类表体
DECLARE @EID INT = IDENT_CURRENT('Z_ora_CRM_ContractEntry')+1
SELECT
	@CFID FID,@EID FEntryID,FSEQ,FMTLGROUP,FDESCRIBE,FQTY,FMODEL,FISSTANDARD,FBPRNDID,FBRNDNO,FGUID,FIS2W
INTO #DL
FROM ora_CRM_SaleOfferEntry WHERE FID=@FID
UPDATE #DL SET FEntryID=FEntryID+FSEQ
SELECT @EID=MAX(FEntryID)+1 FROM #DL
DBCC CHECKIDENT(Z_ora_CRM_ContractEntry, RESEED, @EID)
--报价明细表体
DECLARE @EID_BPR INT = IDENT_CURRENT('Z_ora_CRM_ContractBPR')+1
SELECT
	@CFID FID,@EID_BPR FBEntryID,FBSEQ,FBGUID,FBSRCEID,FBSRCSEQ,FBPRNDSEQ,
	FBMTLGROUP,FBMTLITEM,FBDESCRIBE,FBQTY,FBMODEL,FBISSTANDARD,FBASEPRICE,FBPAMT,FBPAMTGROUP,FBRPTPRICE,FBABACOMM,FBDOWNPOINTS,
	FBWORKDAY,FBCOSTADJ,FBCAREASON,FBDELIVERY,FBPAMTLC,FBRPTPRCLC,FBIS2W, FBEntryID FBPREID,
	FBRangeAmtOne,FBRangeAmtGP,FBRangeAmtReason --扣款
INTO #BPR
FROM ora_CRM_SaleOfferBPR WHERE FID=@FID
UPDATE #BPR SET FBEntryID=FBEntryID+FBSEQ
SELECT @EID_BPR=MAX(FBEntryID)+1 FROM #BPR
DBCC CHECKIDENT(Z_ora_CRM_ContractBPR, RESEED, @EID_BPR)
--材料组成
DECLARE @EID_M INT = IDENT_CURRENT('Z_ora_CRM_ContractMtl')+1
SELECT 
	@CFID FID,@EID_M FEntryIDM,FMSEQ,FMGUID,FMSRCEID,FMSRCSEQ,FMMTLGROUP,FMMTLITEM,FMCLASS,FMMTL,
	FMMODEL,FMQTY,FMUNIT,FMPRICE,FMAMT,FMGPAMTB,FMCOSTRATE,FMCOST,FMGPRATE,FMGP,FMGPAMT,FMGPAMTLC,FMIS2W
INTO #MTL
FROM ora_CRM_SaleOfferMtl WHERE FID=@FID
UPDATE #MTL SET FEntryIDM=FEntryIDM+FMSEQ
SELECT @EID_M=MAX(FEntryIDM)+1 FROM #MTL
DBCC CHECKIDENT(Z_ora_CRM_ContractMtl, RESEED, @EID_M)

/*
SELECT * FROM #DL
SELECT * FROM #BPR
SELECT * FROM #MTL
DROP TABLE #DL
DROP TABLE #BPR
DROP TABLE #MTL
*/
/* * * * * * * * * * * 
 *     插入正式表    *
 * * * * * * * * * * */

BEGIN TRAN --开启事务
BEGIN TRY
	--插入表头数据
	INSERT INTO ora_CRM_Contract(
		FID,FBILLNO,FDOCUMENTSTATUS,FCREATORID,FCREATEDATE,FORGID,FSRCBILLTYPE,FNICHEID,
		FCUSTNAME,FPRJNAME,FISEXPORT,FREMARKS,FCURRENCYID,FCURRENCYCN,FRATE,FRATETYPE,FAMOUNT,FAMOUNTRPT,FRANGEAMT,
		FRANGERS,FAMTCN,FAMTRPTCN,FCRMSN,FAMTP2,FAMTRPTP2,FLOCALFILTER,FCRMHDORGID,FCRMHDDEPT,FCRMHOLDER,
		FISBID,FAMTRNDRPT,FPAYCOND,FAVGDNPTS,FMAXDNPTS,
		FSALER,FSETTLEORGID,FDEPT,FNicheBillNo,FPRJADRESS
	)
	SELECT 
		@CFID,@FBillNo,'A',@FUserId,GETDATE(),FORGID,'ora_CRM_SaleOffer',FBILLNO,
		FCUSTNAME,FPRJNAME,FISEXPORT,FREMARKS,FCURRENCYID,FCURRENCYCN,FRATE,FRATETYPE,FAMOUNT,FAMOUNTRPT,FRANGEAMT,
		FRANGERS,FAMTCN,FAMTRPTCN,FCRMSN,FAMTP2,FAMTRPTP2,FLOCALFILTER,FCRMHDORGID,FCRMHDDEPT,FCRMHOLDER,
		FISBID,FAMTRNDRPT,FPAYCOND,FAVGDNPTS,FMAXDNPTS,
		@FSaler,0,FCRMHDDEPT,
		--@FBizOrg,@FDept,
		FNicheID,FPRJADRESS
	FROM ora_CRM_SaleOffer WHERE FID=@FID
	--产品大类表体
	INSERT INTO ora_CRM_ContractEntry(
		FID,FEntryID,FSEQ,FMTLGROUP,FDESCRIBE,FQTY,FMODEL,FISSTANDARD,FBPRNDID,FBRNDNO,FGUID,FIS2W
	)
	SELECT
		FID,FEntryID,FSEQ,FMTLGROUP,FDESCRIBE,FQTY,FMODEL,FISSTANDARD,FBPRNDID,FBRNDNO,FGUID,FIS2W
	FROM #DL
	--报价明细表体
	INSERT INTO ora_CRM_ContractBPR(
		FID,FBEntryID,FBSEQ,FBGUID,FBSRCEID,FBSRCSEQ,FBPRNDSEQ,
		FBMTLGROUP,FBMTLITEM,FBDESCRIBE,FBQTY,FBMODEL,FBISSTANDARD,FBASEPRICE,FBPAMT,FBPAMTGROUP,FBRPTPRICE,FBABACOMM,FBDOWNPOINTS,
		FBWORKDAY,FBCOSTADJ,FBCAREASON,FBDELIVERY,FBPAMTLC,FBRPTPRCLC,FBIS2W,FBPREID,
		--,FMATERIALID,FBUNITID,FBTAXRATEID,FBBOMVSN
		FBRangeAmtOne,FBRangeAmtGP,FBRangeAmtReason, --扣款
		FBTAXRATE,FBTAXPRICE,FBNTPRICE,
		FBTAXAMT,FBNTAMT,--FBLKQTY,
		FBCLOSEST
	)
	SELECT
		FID,FBEntryID,FBSEQ,FBGUID,FBSRCEID,FBSRCSEQ,FBPRNDSEQ,
		FBMTLGROUP,FBMTLITEM,FBDESCRIBE,FBQTY,FBMODEL,FBISSTANDARD,FBASEPRICE,FBPAMT,FBPAMTGROUP,FBRPTPRICE,FBABACOMM,FBDOWNPOINTS,
		FBWORKDAY,FBCOSTADJ,FBCAREASON,FBDELIVERY,FBPAMTLC,FBRPTPRCLC,FBIS2W,FBPREID,
		FBRangeAmtOne,FBRangeAmtGP,FBRangeAmtReason, --扣款
		13,FBRPTPRICE/FBQTY,FBRPTPRICE/FBQTY/1.13, --税率，含税单价，单价
		FBRPTPRICE-(FBRPTPRICE/1.13),FBRPTPRICE/1.13,
		0
	FROM #BPR --13为税率
	--材料组成
	INSERT INTO ora_CRM_ContractMtl(
		FID,FEntryIDM,FMSEQ,FMGUID,FMSRCEID,FMSRCSEQ,FMMTLGROUP,FMMTLITEM,FMCLASS,FMMTL,
		FMMODEL,FMQTY,FMUNIT,FMPRICE,FMAMT,FMGPAMTB,FMCOSTRATE,FMCOST,FMGPRATE,FMGP,FMGPAMT,FMGPAMTLC,FMIS2W
	)
	SELECT 
		FID,FEntryIDM,FMSEQ,FMGUID,FMSRCEID,FMSRCSEQ,FMMTLGROUP,FMMTLITEM,FMCLASS,FMMTL,
		FMMODEL,FMQTY,FMUNIT,FMPRICE,FMAMT,FMGPAMTB,FMCOSTRATE,FMCOST,FMGPRATE,FMGP,FMGPAMT,FMGPAMTLC,FMIS2W
	FROM #MTL
	--持有记录
	DECLARE @EID_H INT = IDENT_CURRENT('Z_ora_CRM_ContractHode')+1
	DBCC CHECKIDENT(Z_ora_CRM_ContractHode, RESEED, @EID_H)
	INSERT INTO ora_CRM_ContractHode(
		FID,FHEntryID,FHSEQ,FHHDORGID,FHHDDEPT,FHHOLDER,FHSN,FHBEGDATE,FHENDDATE
	) 
	SELECT @CFID,@EID_H,1,FCrmHdOrgID,FCrmHdDept,FCrmHolder,FCrmSN,GETDATE(),'9999-12-31'
	FROM ora_CRM_SaleOffer WHERE FID=@FID
END TRY
BEGIN CATCH
	IF(@@trancount>0) ROLLBACK TRAN --回滚
END CATCH
IF (@@trancount>0) COMMIT TRAN

--返回销售合同FID
SELECT @CFID FID
END


--EXEC proc_czly_CRMGeneContact @FUserId='100560', @FID='110434'
