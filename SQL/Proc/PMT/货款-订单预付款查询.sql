/*
订单预付款查询
Created: 2020-11-06
Author: 刘跃
*/
CREATE PROC [dbo].[proc_czly_PrePayAmtQuery](
    @QSDt DATETIME='',
    @QEDt DATETIME='',
    @QOrderNo VARCHAR(100)='',
    @QSalerNo VARCHAR(100)='',
    @QCustNo VARCHAR(100)='',
    @QSaleOrgNo VARCHAR(100)='',
    @QDeptNo VARCHAR(100)='',
    @QRecConditionNo VARCHAR(100)=''
)
AS
BEGIN

SET NOCOUNT ON

-- 收款情况
SELECT FID, SUM(FPreRcvAmt) FPreRcvAmt, SUM(FRcvAmt) FRcvAmt, MAX(FRcvDate) FRcvDate
INTO #rcv_info
FROM (
    SELECT o.FID, op.FRecAdvanceAmount FPreRcvAmt, 0 FRcvAmt, '' FRcvDate
    FROM T_SAL_ORDER o
    INNER JOIN T_SAL_ORDERPLAN op ON o.FID=op.FID
    WHERE op.FNeedRecAdvance=1
    UNION ALL
    SELECT o.FID, 0 FPreRcvAmt, ISNULL(FSplitAmount,0) FRcvAmt, ISNULL(rs.FCreateDate, '') FRcvDate
    FROM T_SAL_ORDER o
    LEFT JOIN T_CZ_ReceiptSplitOrderPlan rse ON rse.FOrderInterID=o.FID
    LEFT JOIN T_CZ_ReceiptSplit rs ON rs.FID=rse.FID AND rs.FDocumentStatus='C'
    UNION ALL
    SELECT FOrderID FID, 0 FPreRcvAmt, FReceiverAmt FRcvAmt, '' FRcvDate
    FROM ora_Pmt_InitialPayment WHERE FOrderID<>0
)t GROUP BY FID


SELECT o.FID,
    o.FDate, o.FBillNo, o.F_ora_poorderno FPoorderNo,
    o.FSalerID, sm.FNUMBER FSalerNum, sml.FName FSalerName,
    o.FSaleDeptId, d.FNUMBER FDeptNum, dl.FName FDeptName,
    o.FSaleOrgID, org.FNUMBER FSaleOrgNum, orgl.FName FSaleOrgName,
    o.FCustId, c.FNUMBER FCustNum, cl.FName FCustName,
    ofi.FBillAllAmount FOrderAmt,
    ofi.FRecConditionId, rc.FNUMBER FRecConditionNum, rcl.FNAME FRecConditionName,
    os.FDelvAmt, rcv.FPreRcvAmt, rcv.FRcvAmt
-- 订单
FROM T_SAL_ORDER o
-- 收款
INNER JOIN #rcv_info rcv ON rcv.FID=o.FID 
-- 出库
LEFT JOIN (
    SELECT oef.FID, SUM(ose.FRealQty*oef.FTaxPrice) FDelvAmt
    FROM T_SAL_ORDERENTRY_F oef
    LEFT JOIN T_SAL_OUTSTOCKENTRY_R oser ON oef.FEntryId=oser.FSOENTRYID
    LEFT JOIN T_SAL_OUTSTOCKENTRY ose ON oser.FEntryId=ose.FEntryId
    GROUP BY oef.FID
) os ON os.FID=o.FID
-- 销售员
LEFT JOIN V_BD_SALESMAN sm ON sm.FID=o.FSalerID
LEFT JOIN V_BD_SALESMAN_L sml ON sml.FID=sm.FID
-- 办事处
LEFT JOIN T_BD_DEPARTMENT d ON o.FSaleDeptId=d.FDEPTID
LEFT JOIN T_BD_DEPARTMENT_L dl ON d.FDEPTID=dl.FDEPTID
-- 客户
INNER JOIN T_BD_CUSTOMER c ON c.FCUSTID=o.FCustId
INNER JOIN T_BD_CUSTOMER_L cl ON cl.FCUSTID=c.FCustId
-- 销售组织
INNER JOIN T_ORG_ORGANIZATIONS org ON org.FORGID=o.FSALEORGID
INNER JOIN T_ORG_ORGANIZATIONS_L orgl ON orgl.FORGID=org.FORGID AND orgl.FLocalEID=2052
-- 收款条件
INNER JOIN T_SAL_ORDERFIN ofi ON ofi.FID=o.FID
INNER JOIN T_BD_RecCondition rc ON rc.FID=ofi.FRecConditionId
INNER JOIN T_BD_RecCondition_L rcl ON rcl.FID=rc.FID
WHERE ISNULL(os.FDelvAmt, 0)=0 AND rcv.FRcvAmt>0
AND ((@QSDt='' OR @QEDt='') OR o.FDate BETWEEN @QSDt AND @QEDt)
AND o.FBillNo LIKE '%'+ @QOrderNo +'%'
AND ISNULL(sm.FNUMBER,'') LIKE '%'+ @QSalerNo +'%'
AND c.FNUMBER LIKE '%'+ @QCustNo +'%'
AND org.FNUMBER LIKE '%'+ @QSaleOrgNo +'%'
AND ISNULL(d.FNUMBER,'') LIKE '%'+ @QDeptNo +'%'
AND rc.FNUMBER LIKE '%'+ @QRecConditionNo +'%'
ORDER BY o.FDate DESC


DROP TABLE #rcv_info

END

/*
EXEC proc_czly_PrePayAmtQuery @QOrderNo='0020016821'

EXEC proc_czly_PrePayAmtQuery @QSDt='#FBeginDate#', @QEDt='#FEndDate#',
    @QOrderNo='#FOrderNO#', @QSalerNo='#FSellerNo#', @QCustNo='@FCustNO@',
    @QSaleOrgNo='#FSaleOrgNo#', @QDeptNo='#FDeptNo#',
    @QRecConditionNo='#FRecConditionNo#'
*/
