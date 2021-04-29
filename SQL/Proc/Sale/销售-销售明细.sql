/*
销售明细
Created: 2020-11-03
Author: 刘跃
*/
CREATE PROC [dbo].[proc_czly_SaleDetail](
    @QSDt DATETIME='',
    @QEDt DATETIME='',
    @QOrderNo VARCHAR(100)='',
    @QOrderType VARCHAR(100)='',
    @QStockOutNo VARCHAR(100)='',
    @QSalerNo VARCHAR(100)='',
    @QCustNo VARCHAR(100)='',
    @QProdType VARCHAR(100)='',
    @QVoltageLevel VARCHAR(100)='',
    @QSaleOrgNo VARCHAR(100)='',
    @QDeptNo VARCHAR(100)='',
    @QStockOrgNo VARCHAR(100)='',
    @QMaterialNo VARCHAR(100)='',
    @QCkOrigin VARCHAR(100)='',
    @QDeliverType VARCHAR(100)=''
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

SELECT feil.FCAPTION, fei.FVALUE
INTO #deliver_type
FROM T_META_FORMENUM_L fel
INNER JOIN T_META_FORMENUMITEM fei ON fel.FID=fei.FID
INNER JOIN T_META_FORMENUMITEM_L feil ON feil.FENUMID=fei.FENUMID
WHERE FNAME='发货类型'


-- 辅助资料
SELECT ae.FENTRYID, ael.FDATAVALUE 
INTO #cust_type
FROM T_BAS_ASSISTANTDATA_L al
INNER JOIN T_BAS_ASSISTANTDATAENTRY ae ON ae.FID=al.FID
INNER JOIN T_BAS_ASSISTANTDATAENTRY_L ael ON ael.FENTRYID=ae.FENTRYID
WHERE al.FNAME='客户类别'


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

SELECT os.FID, os.FBillNo,
    o.FBillNo FOrderNo, o.F_ora_poorderno FPoorderNo, o.F_ORA_TEXT FSoNum, CASE WHEN o.F_ora_SCYJ = 1 THEN '是' ELSE '否' END FCkOrigin, ot.FCAPTION FBillType,
    o.FDate, dt.FCAPTION FDeliverType, o.FSaleOrgID, org.FNUMBER FOrgNum, orgl.FName FOrgName,
    o.FSaleDeptId, d.FNUMBER FDeptNum, dl.FName FDeptName,
    o.FCustId, c.FNUMBER FCustNum, cl.FName FCustName, ca.FDATAVALUE FCustType, 
    o.F_CZ_PrjName FPrjName, ccl.FName FCustContactName, cc.FMobile FCustContactMobile,
    ofi.FRecConditionId, rc.FNUMBER FRecConditionNum, rcl.FNAME FRecConditionName,
    oe.FMaterialID, m.FNUMBER FMaterialNum, ml.FName FMaterialName, ml.FDescription FMaterialDesc, oe.F_CZ_CustItemName FCustMaterialName,
    dn.F_ora_Date FPlanDelvDate, os.FDate FDelvDate, ose.FRealQty FDelvQty, osef.FSalUnitID, ul.FName FUnitName,
    oef.FTaxPrice, ose.FRealQty*oef.FTaxPrice FDelvAmt, oef.FAllAmount FOrderRowAmt, 
    oe.FStockOrgId, fact.FNUMBER FStockOrgNum, factl.FNAME FStockOrgName,
    mpt.FDATAVALUE FProdType, mct.FDATAVALUE FVoltageLevel,
    o.FSalerID, sm.FNUMBER FSalerNum, sml.FName FSalerName,
    os.FCreatorID, u.FNAME FUserName
-- 出库 
FROM T_SAL_OUTSTOCK os
INNER JOIN T_SAL_OUTSTOCKENTRY_R osr ON os.FID=osr.FID
INNER JOIN T_SAL_OUTSTOCKENTRY ose ON osr.FEntryId=ose.FEntryId
INNER JOIN T_SAL_OUTSTOCKENTRY_F osef ON ose.FEntryId=osef.FEntryId
-- 订单
INNER JOIN T_SAL_ORDERENTRY_F oef ON oef.FEntryID=osr.FSOENTRYID
INNER JOIN T_SAL_ORDERENTRY oe ON oe.FENTRYID=oef.FENTRYID
INNER JOIN T_SAL_ORDER o ON o.FID=oe.FID
INNER JOIN #order_type ot ON o.F_CZ_BillType=ot.FVALUE
-- 销售组织
INNER JOIN T_ORG_ORGANIZATIONS org ON org.FORGID=o.FSALEORGID
INNER JOIN T_ORG_ORGANIZATIONS_L orgl ON orgl.FORGID=org.FORGID AND orgl.FLocalEID=2052
-- 工厂(库存组织)
INNER JOIN T_ORG_ORGANIZATIONS fact ON fact.FORGID=o.FSALEORGID
INNER JOIN T_ORG_ORGANIZATIONS_L factl ON factl.FORGID=fact.FORGID AND factl.FLocalEID=2052
-- 办事处
LEFT JOIN T_BD_DEPARTMENT d ON o.FSaleDeptId=d.FDEPTID
LEFT JOIN T_BD_DEPARTMENT_L dl ON d.FDEPTID=dl.FDEPTID
-- 客户
INNER JOIN T_BD_CUSTOMER c ON c.FCUSTID=o.FCustId
INNER JOIN T_BD_CUSTOMER_L cl ON cl.FCUSTID=c.FCustId
LEFT JOIN T_BD_COMMONCONTACT cc ON cc.FCustId=c.FCustId AND cc.FISDEFAULTCONTACT=1
LEFT JOIN T_BD_COMMONCONTACT_L ccl ON ccl.FCONTACTID=cc.FCONTACTID
LEFT JOIN #cust_type ca ON c.FCustTypeId=ca.FENTRYID
-- 发货
-- LEFT JOIN T_SAL_DELIVERYNOTICEENTRY_LK dnlk ON dnlk.FSID=oe.FENTRYID
LEFT JOIN T_SAL_OUTSTOCKENTRY_LK oslk ON oslk.FENTRYID=ose.FENTRYID AND oslk.FSTABLENAME='T_SAL_DELIVERYNOTICEENTRY'
LEFT JOIN T_SAL_DELIVERYNOTICEENTRY dne ON dne.FENTRYID=oslk.FSID  --dnlk.FENTRYID
LEFT JOIN T_SAL_DELIVERYNOTICE dn ON dn.FID=dne.FID
LEFT JOIN #deliver_type dt ON dt.FVALUE=dn.F_CZ_DeliverType

-- 收款条件
INNER JOIN T_SAL_ORDERFIN ofi ON ofi.FID=o.FID
INNER JOIN T_BD_RecCondition rc ON rc.FID=ofi.FRecConditionId
INNER JOIN T_BD_RecCondition_L rcl ON rcl.FID=rc.FID
-- 物料
INNER JOIN T_BD_Material m ON oe.FMATERIALID=m.FMATERIALID
INNER JOIN T_BD_Material_L ml ON ml.FMATERIALID=m.FMATERIALID
LEFT JOIN #product_type mpt ON m.F_ora_Assistant=mpt.FENTRYID
LEFT JOIN #capacity mct ON m.F_ora_Assistant1=mct.FENTRYID
-- 单位
INNER JOIN T_BD_UNIT_L ul ON ul.FUNITID=osef.FSalUnitID
-- 销售员
LEFT JOIN V_BD_SALESMAN sm ON sm.FID=o.FSalerID
LEFT JOIN V_BD_SALESMAN_L sml ON sml.FID=sm.FID
-- 用户
INNER JOIN T_SEC_USER u ON u.FUserID=os.FCREATORID
WHERE ((@QSDt='' OR @QEDt='') OR os.FDate BETWEEN @QSDt AND @QEDt)
AND os.FBillNo LIKE '%'+ @QStockOutNo +'%'
AND o.FBillNo LIKE '%'+ @QOrderNo +'%'
AND ISNULL(sm.FNUMBER,'') LIKE '%'+ @QSalerNo +'%'
AND c.FNUMBER LIKE '%'+ @QCustNo +'%'
AND org.FNUMBER LIKE '%'+ @QSaleOrgNo +'%'
AND ISNULL(d.FNUMBER,'') LIKE '%'+ @QDeptNo +'%'
AND fact.FNUMBER LIKE '%'+ @QStockOrgNo +'%'
AND m.FNUMBER LIKE '%'+ @QMaterialNo +'%'
AND ISNULL(mpt.FDATAVALUE,'') LIKE '%'+ @QProdType +'%'
AND ISNULL(mct.FDATAVALUE,'') LIKE '%'+ @QVoltageLevel +'%'
AND ISNULL(dn.F_CZ_DeliverType,'') LIKE '%'+ @QDeliverType +'%'
AND o.F_ora_SCYJ LIKE '%'+ @QCkOrigin +'%'
AND o.F_CZ_BillType LIKE '%'+ @QOrderType +'%'



DROP TABLE #order_type
DROP TABLE #deliver_type
DROP TABLE #cust_type
DROP TABLE #product_type
DROP TABLE #capacity

END

/*
EXEC proc_czly_SaleDetail @QSDt='', @QEDt='', 
    @QStockOutNo='', @QOrderNo='', @QOrderType='', 
    @QSalerNo='', @QCustNo='', @QProdType='', @QVoltageLevel='', 
    @QSaleOrgNo='', @QDeptNo='', @QStockOrgNo='',
    @QMaterialNo='', @QCkOrigin='', @QDeliverType=''
    
EXEC proc_czly_SaleDetail @QSDt='#FBeginDate#', @QEDt='#FEndDate#', 
    @QStockOutNo='#FStockOutNo#', @QOrderNo='#FOrderNO#', @QOrderType='#FOrderType#', 
    @QSalerNo='#FSellerNo#', @QCustNo='@FCustNO@', @QProdType='#FProdType#', @QVoltageLevel='#FVoltageLevel#', 
    @QSaleOrgNo='#FSaleOrgNo#', @QDeptNo='#FDeptNo#', @QStockOrgNo='#FStockOrgNo#',
    @QMaterialNo='#FMaterialNo#', @QCkOrigin='#FCkOrigin#', @QDeliverType='#FDeliverType#'
*/