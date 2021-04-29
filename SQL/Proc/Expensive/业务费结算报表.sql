--业务费结算报表
CREATE PROC [dbo].[proc_czly_OptExpSettleRpt](
    @FSDate DATETIME,
    @FEDate DATETIME,
    @FOrderNo VARCHAR(55)='',
    @FSellerNo VARCHAR(55)='',
    @FCustNo VARCHAR(55)=''
)
AS
BEGIN

-- DECLARE @FSDate DATETIME='2020-01-01'
-- DECLARE @FEDate DATETIME='2020-10-12'
SET NOCOUNT ON

SELECT 
    ROW_NUMBER() OVER(ORDER BY FDate desc) 序号,
    s.FBillNo 结算单号, 
    s.FDate 结算日期, 
    se.FOrderNo 销售订单, 
    se.FOrderDate 订单日期,
    sm.FNumber FSellerNumber, sml.FName 销售员,
    se.FOrgId, ol.FName 组织,
    d.FDeptId, dl.FName 部门,
    se.FCustId, cl.FName 客户,
    se.FProdSeries, ps.FDATAVALUE 产品大类,
    SUM(se.FOrderAmt) 订单金额, sum(F_ORA_PRODGROUPAMT) as 基价, sum(F_CZ_FBRANGEAMTGP) as 合同扣款, SUM(se.FRcvAmt) 到款总额, 
    SUM(se.FLastSettleAmt) 上次结算金额, SUM(se.FRealSettleAmt) 本次结算金额,
    SUM(ad.FAdjustAmt) 结算调整金额,
    SUM(se.FRealSettleAmt)-SUM(se.FLastSettleAmt)+SUM(ad.FAdjustAmt) 实际结算金额
FROM ora_OptExp_Settle s
INNER JOIN ora_OptExp_SettleEntry se ON s.FID=se.FID
INNER JOIN T_ORG_ORGANIZATIONS_L ol ON se.FOrgID=ol.FORGID AND ol.FLOCALEID=2052
INNER JOIN T_BD_CUSTOMER_L cl ON cl.FCUSTID=se.FCUSTID AND cl.FLOCALEID=2052
INNER JOIN T_BD_CUSTOMER c ON cl.FCUSTID=c.FCUSTID
LEFT JOIN (
    SELECT ael.FENTRYID,ael.FDATAVALUE,ael.FLOCALEID FROM T_BAS_ASSISTANTDATAENTRY ae
	INNER JOIN T_BAS_ASSISTANTDATAENTRY_L ael ON ae.FENTRYID=ael.FENTRYID
    WHERE ae.FID='5f57738d818080' --产品大类
) ps ON se.FProdSeries=ps.FENTRYID
LEFT JOIN V_BD_SALESMAN sm ON sm.fid=se.FSellerId
LEFT JOIN V_BD_SALESMAN_L sml ON sm.fid=sml.FID AND sml.FLOCALEID=2052
LEFT JOIN T_BD_DEPARTMENT d ON dbo.fun_czty_GetWorkDeptID(sm.FDEPTID)=d.FDEPTID
LEFT JOIN T_BD_DEPARTMENT_L dl ON d.FDEPTID=dl.FDEPTID AND dl.FLOCALEID=2052
LEFT JOIN (
    SELECT 
        FSEORDERNO FOrderNo, FItemType FProdSeries, sm1.FNUMBER FSellerNumber,
        SUM(FAmount) FAdjustAmt
    FROM T_CZ_JSTZD t
    LEFT JOIN T_CZ_JSTZDEntry te ON t.FID=te.FID
    LEFT JOIN V_BD_SALESMAN sm1 ON sm1.fid=te.FEMPID
    WHERE F_ora_Date BETWEEN @FSDate AND @FEDate
    GROUP BY sm1.FNUMBER,FItemType,FEMPID,FSEORDERNO
) ad ON ad.FOrderNo=se.FOrderNo AND ad.FProdSeries=se.FProdSeries AND ad.FSellerNumber=sm.FNumber
WHERE se.FOrderDate BETWEEN @FSDate AND @FEDate
AND se.FOrderNo LIKE '%' + @FOrderNo + '%'
AND sm.FNumber LIKE '%' + @FSellerNo + '%'
AND c.FNumber LIKE '%' + @FCustNo + '%'
GROUP BY s.FBillNo, s.FDate, se.FOrderNo, se.FOrderDate,
    sm.FNumber, sml.FNAME, 
    se.FOrgId, ol.FNAME,
    d.FDeptId, dl.FNAME,
    se.FCustId, cl.FNAME,
    se.FProdSeries, ps.FDATAVALUE
END

-- EXEC proc_czly_OptExpSettleRpt '#FBeginDate#', '#FEndDate#'
-- EXEC proc_czly_OptExpSettleRpt '2020-01-01', '2020-10-12'
