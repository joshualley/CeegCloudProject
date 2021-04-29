-- 订单明细
/*
Created: 2020-11-03
Author: 刘跃
*/
CREATE PROC [dbo].[proc_czly_OrderDetail](
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
    @QCkOrigin VARCHAR(100)='',
    @QIsReject VARCHAR(100)='',
    @QRejectReson VARCHAR(100)='',
    @QPriceRange VARCHAR(100)=''
)
AS
BEGIN

SET NOCOUNT ON

-- 枚举类型
SELECT feil.FCAPTION, fei.FVALUE
INTO #reject 
FROM T_META_FORMENUM_L fel
INNER JOIN T_META_FORMENUMITEM fei ON fel.FID=fei.FID
INNER JOIN T_META_FORMENUMITEM_L feil ON feil.FENUMID=fei.FENUMID
WHERE FNAME='拒绝原因'

SELECT feil.FCAPTION, fei.FVALUE 
INTO #order_type
FROM T_META_FORMENUM_L fel
INNER JOIN T_META_FORMENUMITEM fei ON fel.FID=fei.FID
INNER JOIN T_META_FORMENUMITEM_L feil ON feil.FENUMID=fei.FENUMID
WHERE FNAME='销售订单类型'

-- 辅助资料
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

SELECT o.FID,
    SUBSTRING(CONVERT(VARCHAR(100), o.FDATE, 111),1,7) FYearMonth,
    o.FDate, o.FBillNo, o.F_ora_poorderno FPoorderNo, CASE WHEN o.F_ora_SCYJ = 1 THEN '是' ELSE '否' END FCkOrigin, ot.FCAPTION FBillType,
    oed.FDeliveryDate, o.FSaleOrgID, org.FNUMBER FOrgNum, orgl.FName FOrgName,
    o.FSaleDeptId, d.FNUMBER FDeptNum, dl.FName FDeptName,
    o.FCustId, c.FNUMBER FCustNum, cl.FName FCustName, ca.FDATAVALUE FCustType, cb.FDATAVALUE FCustBussiness, c.FCreateDate, 
    ofi.FRecConditionId, rc.FNUMBER FRecConditionNum, rcl.FNAME FRecConditionName,
    CASE WHEN dbo.Fun_IsFullPay(ofi.FRecConditionId)=1 THEN '是' ELSE '否' END FIsAllMoney,
    o.F_CZ_PrjName FPrjName, ccl.FName FCustContactName, cc.FMobile FCustContactMobile,
    oe.FMaterialID, m.FNUMBER FMaterialNum, ml.FName FMaterialName, ml.FDescription FMaterialDesc, oe.F_CZ_CustItemName FCustMaterialName,
    -- ISNULL(ivt.FBaseQty,0) FProdCapacity, --产品容量
    oe.FQty, oe.FUnitID, ul.FName FUnitName,
    oef.FTaxPrice, oef.FAllAmount FOrderRowAmt, oe.F_CZ_FBPAmt/oe.FQty FSingleBasePrice, oe.F_CZ_FBPAmt FBasePrice,
    ofi.FBillAllAmount FOrderAmt, oe.F_CZ_FBRangeAmtGP FWithholdAmt,
    oer.FStockOutQty, oef.FTaxPrice*oer.FStockOutQty FStockOutPrice, CONVERT(DECIMAL(18,6),oe.FBDownPoints) FBDownPoints,
    CASE WHEN CONVERT(DECIMAL(18,6),oe.FBDownPoints)>10 THEN '基价90%以下'
         WHEN CONVERT(DECIMAL(18,6),oe.FBDownPoints)>5 AND CONVERT(DECIMAL(18,6),oe.FBDownPoints)<=10 THEN '基价90%-95%'
         WHEN CONVERT(DECIMAL(18,6),oe.FBDownPoints)>0 AND CONVERT(DECIMAL(18,6),oe.FBDownPoints)<=5 THEN '基价95%-100%'
         WHEN CONVERT(DECIMAL(18,6),oe.FBDownPoints)=0 THEN '基价100%'
         WHEN CONVERT(DECIMAL(18,6),oe.FBDownPoints)>-10 AND CONVERT(DECIMAL(18,6),oe.FBDownPoints)<0 THEN '基价100%-110%'
         WHEN CONVERT(DECIMAL(18,6),oe.FBDownPoints)<=-10 THEN '基价110%以上'
    END FPriceRange,
    oe.FStockOrgId, fact.FNUMBER FStockOrgNum, factl.FNAME FStockOrgName,
    mpt.FDATAVALUE FProdType, mct.FDATAVALUE FVoltageLevel, --产品大类，电压等级
    o.FSalerID, sm.FNUMBER FSalerNum, sml.FName FSalerName,
    CASE WHEN oe.F_ORA_JJYY<>'' THEN '是' ELSE '否' END FIsReject, rj.FCAPTION FRejectReson, 
    o.FCreatorID, u.FNAME FUserName
-- 订单
FROM T_SAL_ORDER o
INNER JOIN T_SAL_ORDERFIN ofi ON ofi.FID=o.FID
INNER JOIN T_SAL_ORDERENTRY oe ON o.FID=oe.FID
INNER JOIN T_SAL_ORDERENTRY_F oef ON oef.FENTRYID=oe.FENTRYID
INNER JOIN T_SAL_ORDERENTRY_R oer ON oer.FENTRYID=oe.FENTRYID
INNER JOIN T_SAL_ORDERENTRYDELIPLAN oed ON oe.FENTRYID=oed.FENTRYID
INNER JOIN #order_type ot ON o.F_CZ_BillType=ot.FVALUE
LEFT JOIN #reject rj ON oe.F_ORA_JJYY=rj.FVALUE
-- 收款条件
INNER JOIN T_BD_RecCondition rc ON rc.FID=ofi.FRecConditionId
INNER JOIN T_BD_RecCondition_L rcl ON rcl.FID=rc.FID
-- 用户
INNER JOIN T_SEC_USER u ON u.FUserID=o.FCREATORID
-- 物料
INNER JOIN T_BD_Material m ON oe.FMATERIALID=m.FMATERIALID
INNER JOIN T_BD_Material_L ml ON ml.FMATERIALID=m.FMATERIALID
LEFT JOIN #product_type mpt ON m.F_ora_Assistant=mpt.FENTRYID
LEFT JOIN #capacity mct ON m.F_ora_Assistant1=mct.FENTRYID
-- 单位
INNER JOIN T_BD_UNIT_L ul ON ul.FUNITID=oe.FUnitID
-- 销售员
LEFT JOIN V_BD_SALESMAN sm ON sm.FID=o.FSalerID
LEFT JOIN V_BD_SALESMAN_L sml ON sml.FID=sm.FID
-- 客户
INNER JOIN T_BD_CUSTOMER c ON c.FCUSTID=o.FCustId
INNER JOIN T_BD_CUSTOMER_L cl ON cl.FCUSTID=c.FCustId
LEFT JOIN T_BD_COMMONCONTACT cc ON cc.FCustId=c.FCustId AND cc.FISDEFAULTCONTACT=1
LEFT JOIN T_BD_COMMONCONTACT_L ccl ON ccl.FCONTACTID=cc.FCONTACTID
LEFT JOIN #cust_type ca ON c.FCustTypeId=ca.FENTRYID
LEFT JOIN #cust_business cb ON c.F_ora_Assistant=cb.FENTRYID
-- 销售组织
INNER JOIN T_ORG_ORGANIZATIONS org ON org.FORGID=o.FSALEORGID
INNER JOIN T_ORG_ORGANIZATIONS_L orgl ON orgl.FORGID=org.FORGID AND orgl.FLocalEID=2052
-- 库存组织，工厂
INNER JOIN T_ORG_ORGANIZATIONS fact ON fact.FORGID=o.FSALEORGID
INNER JOIN T_ORG_ORGANIZATIONS_L factl ON factl.FORGID=fact.FORGID AND factl.FLocalEID=2052
-- 办事处
LEFT JOIN T_BD_DEPARTMENT d ON o.FSaleDeptId=d.FDEPTID
LEFT JOIN T_BD_DEPARTMENT_L dl ON d.FDEPTID=dl.FDEPTID
-- LEFT JOIN T_STK_INVENTORY ivt ON ivt.FMATERIALID=m.FMATERIALID --即时库存
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
AND CASE WHEN oe.F_ORA_JJYY<>'' THEN '1' ELSE '2' END LIKE '%'+ @QIsReject +'%'
AND oe.F_ORA_JJYY LIKE '%'+ @QRejectReson +'%'
AND CASE WHEN CONVERT(DECIMAL(18,6),oe.FBDownPoints)>10 THEN '6'
         WHEN CONVERT(DECIMAL(18,6),oe.FBDownPoints)>5 AND CONVERT(DECIMAL(18,6),oe.FBDownPoints)<=10 THEN '5'
         WHEN CONVERT(DECIMAL(18,6),oe.FBDownPoints)>0 AND CONVERT(DECIMAL(18,6),oe.FBDownPoints)<=5 THEN '4'
         WHEN CONVERT(DECIMAL(18,6),oe.FBDownPoints)=0 THEN '3'
         WHEN CONVERT(DECIMAL(18,6),oe.FBDownPoints)>-10 AND CONVERT(DECIMAL(18,6),oe.FBDownPoints)<0 THEN '2'
         WHEN CONVERT(DECIMAL(18,6),oe.FBDownPoints)<=-10 THEN '1'
    END LIKE '%'+ @QPriceRange +'%'

DROP TABLE #reject
DROP TABLE #order_type
DROP TABLE #cust_type
DROP TABLE #cust_business
DROP TABLE #product_type
DROP TABLE #capacity

END

/*
EXEC proc_czly_OrderDetail

EXEC proc_czly_OrderDetail @QSDt='#FBeginDate#', @QEDt='#FEndDate#', 
    @QOrderNo='#FOrderNO#', @QOrderType='#FOrderType#', 
    @QSalerNo='#FSellerNo#', @QCustNo='@FCustNO@', 
    @QProdType='#FProdType#', @QVoltageLevel='#FVoltageLevel#', 
    @QSaleOrgNo='#FSaleOrgNo#', @QDeptNo='#FDeptNo#', @QStockOrgNo='#FStockOrgNo#',
    @QMaterialNo='#FMaterialNo#', @QCkOrigin='#FCkOrigin#', @QIsReject='#FIsReject#',
    @QRejectReson='#FRejectReson#', @QPriceRange='#FPriceRange#'
*/