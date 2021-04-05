# 销售开票

import clr
clr.AddReference('Kingdee.BOS.App')
from Kingdee.BOS.App.Data import *


def AfterBindData(e):
	if str(this.View.ClientType) == "Mobile":
		return
	FDocumentStatus = this.View.Model.GetValue("FDocumentStatus")
	if str(FDocumentStatus) != "Z":
		return
	
	# 携带用户信息
	sql = "exec proc_czty_GetLoginUser2Emp @FUserID='{}'".format(this.Context.UserId)
	objs = DBUtils.ExecuteDataSet(this.Context, sql).Tables[0].Rows
	if objs.Count > 0:
		this.View.Model.SetValue("FOrgID", objs[0]["FOrgID"])
		this.View.Model.SetValue("FDeptID", objs[0]["FDeptID"])
		this.View.Model.SetValue("FPhone", objs[0]["FMobile"])
	
	setAmtInfo()
	

def DataChanged(e):
	if str(this.View.ClientType) == "Mobile":
		return
	if e.Key == "FSaleOrderID":
		setAmtInfo()


def setAmtInfo():
	FSaleOrderNo = this.View.Model.GetValue("FSaleOrderID")
	FSaleOrderNo = '' if FSaleOrderNo == None else str(FSaleOrderNo)
	# 计算已开票金额
	sql = """SELECT ISNULL(SUM(FInvCurAmt), 0) FInvAmt FROM ora_CRM_SaleInvoice 
	WHERE FSaleOrderID='{}' AND FSaleOrderID<>'' """.format(FSaleOrderNo)
	objs = DBUtils.ExecuteDataSet(this.Context, sql).Tables[0].Rows
	FInvAmt = 0
	if objs.Count > 0:
		FInvAmt = float(objs[0]["FInvAmt"])
		this.View.Model.SetValue("FInvAmt", FInvAmt)

	# 携带销售订单信息
	sql = "SELECT FID FROM T_SAL_ORDER WHERE FBILLNO='{}'".format(FSaleOrderNo)
	objs = DBUtils.ExecuteDataSet(this.Context, sql).Tables[0].Rows
	if objs.Count <= 0:
		return
	sql = "EXEC proc_czly_GetSaleOrderSrcInfo @FID='{}'".format(objs[0]["FID"])
	objs = DBUtils.ExecuteDataSet(this.Context, sql).Tables[0].Rows
	if objs.Count > 0:
		this.View.Model.SetValue("FCustOrgId", objs[0]["FCustOrgID"])
		this.View.Model.SetValue("FCustID", objs[0]["FCustID"])
		this.View.Model.SetValue("FOrderAmt", objs[0]["FOrderAmt"])
		this.View.Model.SetValue("FSendPdtAmt", objs[0]["FSendPdtAmt"])
		this.View.Model.SetValue("FRecAmt", objs[0]["FRecAmt"])
		# 设置剩余开票金额
		FInvNoAmt = float(objs[0]["FOrderAmt"]) - FInvAmt
		this.View.Model.SetValue("FInvNoAmt", FInvNoAmt)

	# 携带客户信息
	sql = """SELECT FADDRESS,FTEL,FTAXREGISTERCODE,ISNULL(FBANKCODE,'')FBANKCODE,ISNULL(FACCOUNTNAME,'')FACCOUNTNAME 
FROM T_BD_CUSTOMER c 
LEFT JOIN T_BD_CUSTBANK cb ON c.FCUSTID=cb.FCUSTID 
WHERE c.FCUSTID='{}'""".format(objs[0]["FCustID"])
	objs = DBUtils.ExecuteDataSet(this.Context, sql).Tables[0].Rows
	if objs.Count > 0:
		this.View.Model.SetValue("FCustAddress", objs[0]["FADDRESS"])
		this.View.Model.SetValue("FCustCPhone", objs[0]["FTEL"])
		this.View.Model.SetValue("FCustBank", objs[0]["FACCOUNTNAME"])
		this.View.Model.SetValue("FCustBankNo", objs[0]["FBANKCODE"])
		this.View.Model.SetValue("FTaxNum", objs[0]["FTAXREGISTERCODE"])