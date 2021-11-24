-- 货款明细
ALTER PROC [dbo].[proc_czly_GetPmtDetail2](
    @SDt DATETIME='',
    @EDt DATETIME='',
    @FQDeptId BIGINT=0,
    @FQSalerId BIGINT=0,
    @FQCustId BIGINT=0,
    @FQFactoryId BIGINT=0,
    @FQOrderNo VARCHAR(55)=''
)
AS 
BEGIN
SET NOCOUNT ON

IF @SDt='' SET @SDt = (SELECT TOP 1 FDate FROM T_SAL_ORDER ORDER BY FDate ASC)
IF @EDt='' SET @EDt = GETDATE()

-- 货款明细 select * from T_SAL_OUTSTOCK
SELECT ISNULL(MIN(FEarlyDelvGoodsDt), '1900-01-01') FEarlyDelvGoodsDt,
    ISNULL(MAX(FLaterDelvGoodsDt), '') FLaterDelvGoodsDt, 0 FDirectors, 
    o.FBILLNO FOrderNo, 
    o.FID, o.F_ora_poorderno FSerialNum,
    o.FSALERID FSellerID, d.FDeptID, d.FUSEORGID FOrgID,
    o.FCustID, 
    o.FSaleOrgID, o.F_ora_SignOrgId FSignOrgID,
    o.FNote FRemark,
    ofi.FRecConditionId FPayWay,
    MAX(ofi.FBILLALLAMOUNT) FTOrderAmt, 
    ISNULL(si.FInvoiceAmt, 0) FInvoiceAmt,
    SUM(ISNULL(ost.FDeliverAmt, 0)) FDeliverAmt,
    case ISNULL(dlv.FID, 0) when 0 then '已移交' else '未移交' end FDeliverNote,
    ISNULL(dlv.FDeliverAmt, 0) FDelvPmt -- 移交货款
into #order_entry_temp
FROM T_SAL_ORDER o
INNER JOIN T_SAL_ORDERFIN ofi ON o.FID=ofi.FID
INNER JOIN T_SAL_ORDERENTRY oe ON o.FID=oe.FID
INNER JOIN T_SAL_ORDERENTRY_F oef ON oe.FENTRYID=oef.FENTRYID --财务信息拆分表
LEFT JOIN T_SAL_ORDERENTRY_R oer ON oe.FENTRYID=oer.FENTRYID
LEFT JOIN (
    SELECT osr.FSOENTRYID, SUM(ose.FRealQty*oef.FTaxPrice) FDeliverAmt, 
    MIN(os.FDATE) FEarlyDelvGoodsDt, MAX(os.FDATE) FLaterDelvGoodsDt
    FROM T_SAL_OUTSTOCKENTRY_R osr
    INNER JOIN T_SAL_OUTSTOCKENTRY_F osef ON osr.FEntryId=osef.FEntryId
    INNER JOIN T_SAL_OUTSTOCKENTRY ose ON osr.FEntryId=ose.FEntryId
    INNER JOIN T_SAL_OUTSTOCK os ON os.FID=osr.FID
    INNER JOIN T_SAL_ORDERENTRY_F oef ON oef.FEntryID=osr.FSOENTRYID
	where  osr.FSRCTYPE IN ('SAL_DELIVERYNOTICE', 'SAL_SaleOrder')   --去除发货不匹配调整单
	-- where convert(varchar(10),os.fdate,20) <='2020-07-28'  ---临时增加
    GROUP BY osr.FSOENTRYID
) ost ON ost.FSOENTRYID=oe.FENTRYID
LEFT JOIN (
    SELECT o.FID, SUM(FInvCurAmt) FInvoiceAmt FROM ora_CRM_SaleInvoice s
    INNER JOIN T_SAL_ORDER o ON o.FBILLNO=s.FSaleOrderID
	-- where convert(varchar(10),o.fdate,20) <='2020-07-28'  ---临时增加
    GROUP BY o.FID
) si ON si.FID=o.FID
LEFT JOIN V_BD_SALESMAN sm ON o.FSALERID=sm.FID
LEFT JOIN V_BD_SALESMAN smt ON smt.FNUMBER=sm.FNUMBER AND smt.FBIZORGID=1
LEFT JOIN T_BD_CUSTOMER c ON c.FCUSTID=o.FCUSTID
LEFT JOIN T_BD_DEPARTMENT d ON dbo.fun_czty_GetWorkDeptID(sm.FDEPTID)=d.FDEPTID
LEFT JOIN ora_PMT_Deliver dlv on o.FBILLNO=dlv.FORDERNO AND dlv.FDOCUMENTSTATUS='C' -- 货款移交单
WHERE o.FDocumentStatus='C' 
    -- and   convert(varchar(10),o.fdate,20) <='2020-07-28'  ---临时增加 
AND o.FCustID NOT IN ( --过滤掉内单
	SELECT cl.FCUSTID FROM T_BD_CUSTOMER_L cl
	INNER JOIN T_ORG_ORGANIZATIONS_L ol ON cl.FNAME=ol.FNAME
)
-- AND (SELECT COUNT(1) NUM FROM ora_PMT_Deliver dlv WHERE o.FBILLNO=dlv.FORDERNO AND dlv.FDOCUMENTSTATUS='C')=0 --过滤掉转交单
AND o.F_CZ_BillType<>'ZRD2' --过滤维修合同
AND (o.FBILLTYPEID<>'a300e2620037435492aed9842875b451' AND ofi.FBILLALLAMOUNT<>0) --过滤退货订单
AND ISNULL(oe.F_ora_Jjyy, '')='' --过滤掉存在拒绝原因的行
--AND FBILLNo LIKE 'XSDD20100086'
AND o.FDate BETWEEN @SDt AND @EDt
AND (@FQDeptId=0 OR d.FMASTERID=@FQDeptId)
AND (@FQSalerId=0 OR smt.fid=@FQSalerId)
AND (@FQCustId=0 OR c.FMASTERID=@FQCustId)
AND (@FQFactoryId=0 OR oe.FSTOCKORGID=@FQFactoryId)
AND (@FQOrderNo='' OR o.FBILLNO LIKE '%'+@FQOrderNo+'%')
GROUP BY o.FID,o.FBILLNO,o.F_ora_poorderno,o.FSALERID, d.FDeptID, d.FUSEORGID,
o.FCustID,o.FSaleOrgID,o.F_ora_SignOrgId,ofi.FRecConditionId,o.FNote, si.FInvoiceAmt,
o.FDate, dlv.FID, dlv.FDeliverAmt
ORDER BY o.FDATE DESC

-- 累计期初的发货金额
UPDATE o SET o.FDeliverAmt=ISNULL(p.FDeliverAmt,0)+ISNULL(o.FDeliverAmt,0)
FROM  #order_entry_temp o
INNER JOIN ora_Pmt_InitialPayment p ON o.FID=p.FOrderID
-- 比较期初最后发货日期
UPDATE o SET o.FLaterDelvGoodsDt=CONVERT(VARCHAR(10),p.FLaterDelvGoodsDt,20)
-- SELECT CONVERT(VARCHAR(10),ISNULL(o.FLaterDelvGoodsDt,'1900-01-01'),20),CONVERT(VARCHAR(10),p.FLaterDelvGoodsDt,20)
FROM  #order_entry_temp o
INNER JOIN ora_Pmt_InitialPayment p ON o.FID=p.FOrderID
WHERE CONVERT(VARCHAR(10),ISNULL(o.FLaterDelvGoodsDt,'1900-01-01'),20)<CONVERT(VARCHAR(10),p.FLaterDelvGoodsDt,20)

-- DELETE FROM #order_entry_temp WHERE ISNULL(FDeliverAmt, 0)=0s

-- SELECT * from #order_entry_temp

-- 收款计划关联收款单
SELECT ROW_NUMBER() OVER(ORDER BY ot.FID) FSQ, ot.FID, 
    dbo.Fun_IsWarranty(ot.FPayWay, op.FSEQ) FIsWrt,
    FLaterDelvGoodsDt,
    DATEADD(DAY, 7, ISNULL(dbo.Fun_CalDeadline(ot.FPayWay, op.FSEQ, ot.FLaterDelvGoodsDt), ot.FLaterDelvGoodsDt)) FDeadline,
    dbo.Fun_CalRateAmt(ot.FPayWay, op.FSEQ, ot.FDeliverAmt) FDeliverAmt
INTO #plan_temp
FROM #order_entry_temp ot
INNER JOIN T_SAL_ORDERPLAN op ON op.FID=ot.FID

-- select * from #plan_temp


SELECT t.FID, 
    SUM(ISNULL(FSplitAmount,0)) FTRcvAmt
INTO #rcv_total
FROM (
	SELECT o.FID, FSplitAmount
	FROM #order_entry_temp o
	LEFT JOIN T_CZ_ReceiptSplitOrderPlan rse ON rse.FOrderInterID=o.FID
	LEFT JOIN T_CZ_ReceiptSplit rs ON rs.FID=rse.FID AND rs.FDocumentStatus='C'
	-- where convert(varchar(10),rs.FCREATEDATE,20) <='2020-07-28'  ---临时增加
	UNION ALL 
	SELECT FOrderID FID, FReceiverAmt FSplitAmount FROM ora_Pmt_InitialPayment
) t
GROUP BY t.FID

-- select * from #rcv_total


-- 关联到款拆分单, 汇总订单的收款
SELECT --ROW_NUMBER() OVER(ORDER BY o.FID) FSQ, 
    p.FSQ,
    o.FID, 
    SUM(ISNULL(FSplitAmount,0)) FRcvAmt, SUM(ISNULL(FSplitAmount,0)) as FRemainAmt,
    p.FIsWrt, p.FLaterDelvGoodsDt, p.FDeadline, p.FDeliverAmt
INTO #rcv
FROM #order_entry_temp o
INNER JOIN #plan_temp p ON p.FID=o.FID
LEFT JOIN T_CZ_ReceiptSplitOrderPlan rse ON rse.FOrderInterID=o.FID
LEFT JOIN T_CZ_ReceiptSplit rs ON rs.FID=rse.FID AND rs.FDocumentStatus='C'
-- where convert(varchar(10),rs.FCREATEDATE,20) <='2020-07-28'  ---临时增加
GROUP BY o.FID, p.FSQ, p.FIsWrt, p.FLaterDelvGoodsDt, p.FDeadline, p.FDeliverAmt


-- 累加期初收款
UPDATE t SET 
    t.FRcvAmt=ISNULL(p.FReceiverAmt,0)+ISNULL(t.FRcvAmt,0),
    t.FRemainAmt=ISNULL(p.FReceiverAmt,0)+ISNULL(t.FRemainAmt,0)
FROM #rcv t
INNER JOIN ora_Pmt_InitialPayment p ON t.FID=p.FOrderID

-- SELECT * FROM #order_entry_temp
-- 拆分总到款金额到收款计划的某一行
SELECT * INTO #t FROM #rcv
DECLARE @sq INT=0
DECLARE @fid INT=0
DECLARE @amt DECIMAL(18,2)=0
WHILE EXISTS(SELECT FID FROM #t)
BEGIN 
    SELECT TOP 1 @sq=FSQ,@fid=FID FROM #t
    SET @amt=0
    --取出拆分的金额
    SELECT @amt=CASE WHEN FRemainAmt>=FDeliverAmt THEN FDeliverAmt ELSE FRemainAmt END FROM #rcv WHERE FSQ=@sq
    UPDATE r SET FRcvAmt=@amt FROM #rcv r WHERE FSQ=@sq
    UPDATE #rcv SET FRemainAmt=CASE WHEN FRemainAmt-@amt<0 THEN 0 ELSE FRemainAmt-@amt END WHERE FID=@fid
    DELETE FROM #t WHERE FSQ=@sq
END

-- drop table #rcv
-- SELECT *,DATEDIFF(DAY, DATEADD(MONTH,2, FDeadline), GETDATE()),DATEADD(MONTH,2, FDeadline) FROM #rcv

SELECT FID, SUM(FNormOverAmt) FNormOverAmt, --正常逾期
    SUM(FOverAmt) FOverAmt, --逾期
    SUM(FNormOverAmt)+SUM(FOverAmt)+SUM(FOverWrtAmt) FTOverAmt, --总逾期
    SUM(FExceedeAmt) FExceedeAmt, --超期
    SUM(FUnOverAmt) FUnOverAmt, --正常未逾期
    SUM(FOverWrtAmt) FOverWrtAmt, --逾期质保金
    SUM(FUnOverWrtAmt) FUnOverWrtAmt, --未逾期质保金
    SUM(FUnOverWrtAmt)+SUM(FUnOverAmt) FTUnOverAmt, --总未逾期
    SUM(FOverWrtAmt)+SUM(FUnOverWrtAmt) FWrtAmt, --质保金
    MAX(FOverMonth) FOverMonth --逾期月份
INTO #payment_now
FROM (
-- 正常逾期
SELECT FID, FDeliverAmt-FRcvAmt FNormOverAmt,0 FOverAmt,0 FExceedeAmt,0 FUnOverAmt,0 FOverWrtAmt,0 FUnOverWrtAmt,                
DATEDIFF(MONTH, DATEADD(MONTH,2, FDeadline), GETDATE()) FOverMonth
FROM #rcv WHERE DATEDIFF(DAY, DATEADD(MONTH,2, FDeadline), GETDATE())<=0 AND FDeliverAmt-FRcvAmt>0 AND FIsWrt=0
AND GETDATE()>FDeadline
UNION ALL
-- 逾期
SELECT FID, 0 FNormOverAmt, FDeliverAmt-FRcvAmt FOverAmt, 0 FExceedeAmt,0 FUnOverAmt,0 FOverWrtAmt,0 FUnOverWrtAmt,
DATEDIFF(MONTH, DATEADD(MONTH, 2, FDeadline), GETDATE()) FOverMonth
FROM #rcv WHERE DATEDIFF(DAY, DATEADD(MONTH, 2, FDeadline), GETDATE())>0 AND FDeliverAmt-FRcvAmt>0  AND FIsWrt=0
UNION ALL
-- 超期
SELECT FID, 0 FNormOverAmt, 0 FOverAmt, FDeliverAmt-FRcvAmt FExceedeAmt, 0 FUnOverAmt,0 FOverWrtAmt,0 FUnOverWrtAmt,
DATEDIFF(MONTH, DATEADD(MONTH, 8, FDeadline), GETDATE()) FOverMonth
FROM #rcv WHERE DATEDIFF(DAY, DATEADD(MONTH, 8, FDeadline), GETDATE())>0 AND FDeliverAmt-FRcvAmt>0  AND FIsWrt=0
UNION ALL
-- 正常未逾期
SELECT FID, 0 FNormOverAmt, 0 FOverAmt, 0 FExceedeAmt, FDeliverAmt-FRcvAmt FUnOverAmt, 0 FOverWrtAmt,0 FUnOverWrtAmt,
DATEDIFF(MONTH, FDeadline, GETDATE()) FOverDay
FROM #rcv WHERE DATEDIFF(DAY, FDeadline, GETDATE())<=0 AND FDeliverAmt-FRcvAmt>0  AND FIsWrt=0
AND GETDATE()<=FDeadline
UNION ALL
-- 逾期质保金
SELECT FID, 0 FNormOverAmt, 0 FOverAmt, 0 FExceedeAmt,0 FUnOverAmt, FDeliverAmt-FRcvAmt FOverWrtAmt, 0 FUnOverWrtAmt,
DATEDIFF(MONTH, DATEADD(MONTH, 2, FDeadline), GETDATE()) FOverMonth
FROM #rcv WHERE DATEDIFF(DAY, DATEADD(MONTH, 2, FDeadline), GETDATE())>0 AND FDeliverAmt-FRcvAmt>0  AND FIsWrt=1
UNION ALL
-- 未逾期质保金
SELECT FID, 0 FNormOverAmt, 0 FOverAmt, 0 FExceedeAmt,0 FUnOverAmt, 0 FOverWrtAmt, FDeliverAmt-FRcvAmt FUnOverWrtAmt, 
DATEDIFF(MONTH, DATEADD(MONTH, 2, FDeadline), GETDATE()) FOverMonth
FROM #rcv WHERE DATEDIFF(DAY, DATEADD(MONTH, 2, FDeadline), GETDATE())<=0 AND FDeliverAmt-FRcvAmt>0  AND FIsWrt=1
AND GETDATE()<=FDeadline
)t
GROUP BY FID

SELECT FID, 
    SUM(FNormOverAmt) FNormOverAmt, --正常逾期
    SUM(FOverAmt) FOverAmt, --逾期
    SUM(FTOverAmt) FTOverAmt, --总逾期
    SUM(FExceedeAmt) FExceedeAmt, --超期
    SUM(FUnOverAmt) FUnOverAmt, --正常未逾期
    SUM(FOverWrtAmt) FOverWrtAmt, --逾期质保金
    SUM(FUnOverWrtAmt) FUnOverWrtAmt, --未逾期质保金
    SUM(FTUnOverAmt) FTUnOverAmt, --总未逾期
    SUM(FWrtAmt) FWrtAmt, --质保金
    MAX(FOverMonth) FOverMonth --逾期月份
INTO #payment FROM (
    SELECT FID, FNormOverAmt, FOverAmt, FTOverAmt, FExceedeAmt, 
    FUnOverAmt, FOverWrtAmt, FUnOverWrtAmt, FTUnOverAmt, 
    FWrtAmt, FOverMonth
    FROM #payment_now
    --UNION ALL 
    --SELECT FORDERID FID, FNOrmOverduePmt FNormOverAmt, 0 FOverAmt, FTOverduePmt FTOverAmt, FExceedePmt FExceedeAmt,
    --FNormUnOverduePmt FUnOverAmt, FOverdueWarranty FOverWrtAmt, FUnOverdueWarranty FUnOverWrtAmt, FTUnOverduePmt FTUnOverAmt,
    --FTWarranty FWrtAmt, 0 FOverMonth
    --FROM ora_Pmt_InitialPayment
) t
GROUP BY t.FID


SELECT o.FID, 0 FEntryID, 0 FBeforeRcvAmt, o.FOrderNo, 0 FOrderSeq, 0 FFactoryID, o.FDirectors, o.FSerialNum, o.FSaleOrgID, o.FSignOrgID,
    o.FSellerID, o.FDeptID, o.FOrgID, o.FCustID,
    o.FEarlyDelvGoodsDt, o.FLaterDelvGoodsDt, 0 FIntervalDay, p.FOverMonth FIntervalMonth, o.FPayWay, o.FRemark, 0 FOrderAmt,
    o.FTOrderAmt, o.FDeliverAmt, rt.FTRcvAmt FReceiverAmt, o.FInvoiceAmt, 
    CASE WHEN ISNULL(FDeliverAmt, 0)-ISNULL(FTRcvAmt,0)>0 THEN ISNULL(FDeliverAmt, 0)-ISNULL(FTRcvAmt,0) ELSE 0 END FOuterPmt,
    p.FNormOverAmt FNormOverduePmt, p.FUnOverAmt FNormUnoverduePmt, 
    p.FOverAmt FOverduePmt, p.FTOverAmt FTOverduePmt, 
    p.FTUnOverAmt FTUnoverduePmt, p.FExceedeAmt FTExceedePmt, 
    p.FWrtAmt FTWarranty, p.FOverWrtAmt FOverdueWarranty, p.FUnOverWrtAmt FUnoverdueWarranty,
    o.FDeliverNote, o.FDelvPmt
FROM #order_entry_temp o 
LEFT JOIN #rcv_total rt ON rt.FID=o.FID
LEFT JOIN #payment p ON p.FID=o.FID
WHERE o.FDeliverAmt<>0 OR rt.FTRcvAmt<>0  -- 除去未发货且未收款的订单
-- WHERE FORDERNO='20020822'

drop table #order_entry_temp
drop table #plan_temp
drop table #rcv
drop table #rcv_total
drop table #t
drop table #payment
-- drop table #dept_directors


END

-- exec proc_czly_GetPmtDetail2