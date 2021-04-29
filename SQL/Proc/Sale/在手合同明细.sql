/*
在手合同明细
Created: 2020-11-03
Author: 刘跃
*/
CREATE PROC [dbo].[proc_czly_HoldContractDetail](
    @QSDt DATETIME='',
    @QEDt DATETIME='',
    @QOrderNo VARCHAR(100)='',
    @QOrderType VARCHAR(100)='',
    @QSalerNo VARCHAR(100)='',
    @QCustNo VARCHAR(100)='',
    @QProdType VARCHAR(100)='',
    @QVoltageLevel VARCHAR(100)='',
    @QSaleOrgNo VARCHAR(100)='',
    @QDeptNo VARCHAR(100)='',
    @QStockOrgNo VARCHAR(100)='',
    @QMaterialNo VARCHAR(100)='',
    @QCkOrigin VARCHAR(100)=''
)
AS
BEGIN

SET NOCOUNT ON

SELECT feil.FCAPTION, fei.FVALUE 
INTO #order_type
FROM T_META_FORMENUM_L fel
INNER JOIN T_META_FORMENUMITEM fei ON fel.FID=fei.FID
INNER JOIN T_META_FORMENUMITEM_L feil ON feil.FENUMID=fei.FENUMID
WHERE FNAME='销售订单类型'


SELECT ae.FENTRYID, ael.FDATAVALUE 
INTO #cust_type
FROM T_BAS_ASSISTANTDATA_L al
INNER JOIN T_BAS_ASSISTANTDATAENTRY ae ON ae.FID=al.FID
INNER JOIN T_BAS_ASSISTANTDATAENTRY_L ael ON ael.FENTRYID=ae.FENTRYID
WHERE al.FNAME='客户类别'

SELECT ae.FENTRYID, ael.FDATAVALUE 
INTO #cust_business
FROM T_BAS_ASSISTANTDATA_L al
INNER JOIN T_BAS_ASSISTANTDATAENTRY ae ON ae.FID=al.FID
INNER JOIN T_BAS_ASSISTANTDATAENTRY_L ael ON ael.FENTRYID=ae.FENTRYID
WHERE al.FNAME='行业'

SELECT ae.FENTRYID, ael.FDATAVALUE 
INTO #product_type
FROM T_BAS_ASSISTANTDATA_L al
INNER JOIN T_BAS_ASSISTANTDATAENTRY ae ON ae.FID=al.FID
INNER JOIN T_BAS_ASSISTANTDATAENTRY_L ael ON ael.FENTRYID=ae.FENTRYID
WHERE al.FNAME='产品大类'

SELECT ae.FENTRYID, ael.FDATAVALUE 
INTO #capacity
FROM T_BAS_ASSISTANTDATA_L al
INNER JOIN T_BAS_ASSISTANTDATAENTRY ae ON ae.FID=al.FID
INNER JOIN T_BAS_ASSISTANTDATAENTRY_L ael ON ael.FENTRYID=ae.FENTRYID
WHERE al.FNAME='容量'

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


SELECT o.FID, o.FBILLNO FOrderNo,  o.F_ora_poorderno FPoorderNo, 
    o.FSaleOrgID, org.FNUMBER FOrgNum, orgl.FName FOrgName, o.FSaleDeptId, d.FNUMBER FDeptNum, dl.FName FDeptName,
    sm.FNUMBER FSaleNum, sml.FName FSalerName, oe.FPLANDELIVERYDATE FPlanDelvDate, oep.FDeliveryDate FDelvDate, 
    o.FCustId, c.FNUMBER FCustNum, cl.FName FCustName, ca.FDATAVALUE FCustType, cb.FDATAVALUE FCustBussiness,
    ot.FCAPTION FBillType, ofi.FRecConditionId, rc.FNUMBER FRecConditionNum,  rcl.FNAME FRecConditionName,
    CASE WHEN o.F_ora_SCYJ = 1 THEN '是' ELSE '否' END FCkOrigin,
    mpt.FDATAVALUE FProdType, mct.FDATAVALUE FVoltageLevel,
    oe.FMaterialID, m.FNUMBER FMaterialNum, ml.FName FMaterialName, ml.FDescription FMaterialDesc, oe.F_CZ_CustItemName FCustMaterialName,
    oe.FStockOrgId, fact.FNUMBER FStockOrgNum, factl.FNAME FStockOrgName,
    oe.FExpPeriod, ofi.FSettleCurrId, cyl.FNAME FCurrencyName, 
    oe.FQty, oer.FStockOutQty FDelvQty, oer.FStockOutQty*oef.FTaxPrice FDelvAmt, 
    oe.FQty-oer.FStockOutQty FNotDelvQty, (oe.FQty-oer.FStockOutQty)*oef.FTaxPrice FNotDelvAmt,
    oef.FTaxPrice, ofi.FBILLALLAMOUNT FOrderAmt, oe.F_CZ_FBRangeAmtGP FWithholdAmt,
    -- 到款时间，预付款，收款，[收款比例，特殊付款，合同情况]
    rcv.FRcvDate, rcv.FPreRcvAmt, rcv.FRcvAmt, 
    CASE WHEN ofi.FBILLALLAMOUNT=0 THEN 1 ELSE rcv.FRcvAmt/ofi.FBILLALLAMOUNT*100 END FRcvRate, 
    o.FCreatorID, u.FNAME FUserName
-- 订单
FROM T_SAL_ORDER o
INNER JOIN T_SAL_ORDERFIN ofi ON ofi.FID=o.FID
INNER JOIN T_SAL_ORDERENTRY oe ON oe.FID=o.FID
INNER JOIN T_SAL_ORDERENTRYDELIPLAN oep ON oep.FENTRYID=oe.FENTRYID
INNER JOIN T_SAL_ORDERENTRY_R oer ON oer.FEntryID=oe.FENTRYID
INNER JOIN T_SAL_ORDERENTRY_F oef ON oef.FEntryID=oe.FENTRYID
INNER JOIN #order_type ot ON o.F_CZ_BillType=ot.FVALUE
-- 销售员
LEFT JOIN V_BD_SALESMAN sm ON sm.FID=o.FSalerID
LEFT JOIN V_BD_SALESMAN_L sml ON sml.FID=sm.FID
-- 收款
INNER JOIN #rcv_info rcv ON rcv.FID=o.FID
-- 收款条件
INNER JOIN T_BD_RecCondition rc ON rc.FID=ofi.FRecConditionId
INNER JOIN T_BD_RecCondition_L rcl ON rcl.FID=rc.FID
-- 客户
INNER JOIN T_BD_CUSTOMER c ON c.FCUSTID=o.FCustId
INNER JOIN T_BD_CUSTOMER_L cl ON cl.FCUSTID=c.FCustId
LEFT JOIN #cust_type ca ON c.FCustTypeId=ca.FENTRYID
LEFT JOIN #cust_business cb ON c.F_ora_Assistant=cb.FENTRYID
-- 物料
INNER JOIN T_BD_Material m ON oe.FMATERIALID=m.FMATERIALID
INNER JOIN T_BD_Material_L ml ON ml.FMATERIALID=m.FMATERIALID
LEFT JOIN #product_type mpt ON m.F_ora_Assistant=mpt.FENTRYID
LEFT JOIN #capacity mct ON m.F_ora_Assistant1=mct.FENTRYID
-- 销售组织
INNER JOIN T_ORG_ORGANIZATIONS org ON org.FORGID=o.FSALEORGID
INNER JOIN T_ORG_ORGANIZATIONS_L orgl ON orgl.FORGID=org.FORGID AND orgl.FLocalEID=2052
-- 工厂(库存组织)
INNER JOIN T_ORG_ORGANIZATIONS fact ON fact.FORGID=o.FSALEORGID
INNER JOIN T_ORG_ORGANIZATIONS_L factl ON factl.FORGID=fact.FORGID AND factl.FLocalEID=2052
-- 办事处
LEFT JOIN T_BD_DEPARTMENT d ON o.FSaleDeptId=d.FDEPTID
LEFT JOIN T_BD_DEPARTMENT_L dl ON d.FDEPTID=dl.FDEPTID
-- 币别
INNER JOIN T_BD_CURRENCY_L cyl ON cyl.FCURRENCYID=ofi.FSettleCurrId
-- 用户
INNER JOIN T_SEC_USER u ON u.FUserID=o.FCREATORID
WHERE ((@QSDt='' OR @QEDt='') OR FDate BETWEEN @QSDt AND @QEDt)
AND o.FBillNo LIKE '%'+ @QOrderNo +'%'
AND ISNULL(sm.FNUMBER,'') LIKE '%'+ @QSalerNo +'%'
AND c.FNUMBER LIKE '%'+ @QCustNo +'%'
AND org.FNUMBER LIKE '%'+ @QSaleOrgNo +'%'
AND ISNULL(d.FNUMBER,'') LIKE '%'+ @QDeptNo +'%'
AND fact.FNUMBER LIKE '%'+ @QStockOrgNo +'%'
AND m.FNUMBER LIKE '%'+ @QMaterialNo +'%'
AND ISNULL(mpt.FDATAVALUE,'') LIKE '%'+ @QProdType +'%'
AND ISNULL(mct.FDATAVALUE,'') LIKE '%'+ @QVoltageLevel +'%'
AND o.F_ora_SCYJ LIKE '%'+ @QCkOrigin +'%'
AND o.F_CZ_BillType LIKE '%'+ @QOrderType +'%'



DROP TABLE #order_type
DROP TABLE #cust_type
DROP TABLE #cust_business
DROP TABLE #product_type
DROP TABLE #capacity
DROP TABLE #rcv_info

END

/*
EXEC proc_czly_HoldContractDetail


EXEC proc_czly_HoldContractDetail @QSDt='#FBeginDate#', @QEDt='#FEndDate#', 
    @QOrderNo='#FOrderNO#', @QOrderType='#FOrderType#', 
    @QSalerNo='#FSellerNo#', @QCustNo='@FCustNO@', 
    @QProdType='#FProdType#', @QVoltageLevel='#FVoltageLevel#', 
    @QSaleOrgNo='#FSaleOrgNo#', @QDeptNo='#FDeptNo#', @QStockOrgNo='#FStockOrgNo#',
    @QMaterialNo='#FMaterialNo#', @QCkOrigin='#FCkOrigin#'

*/

-- CREATE INDEX Idx_V_BD_SALESMAN_FNumber ON V_BD_SALESMAN(FNumber)