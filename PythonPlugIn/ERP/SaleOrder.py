# 销售订单，替换销售员及其组织部门，替换客户

import clr
clr.AddReference('Kingdee.BOS.App')
from Kingdee.BOS.App.Data import *

def AfterBindData(e):
	FDocumentStatus = str(this.View.Model.GetValue("FDocumentStatus"))
	if FDocumentStatus == "Z":
		AlterCustAndSalerByOrg()
		

def DataChanged(e):
	if e.Key == "FSalerId":
		FSalerId = str(e.NewValue)
		sql = "EXEC proc_czly_GetOrgDeptBySalemanId @SmId='{}'".format(FSalerId)
		results = DBUtils.ExecuteDataSet(this.Context, sql).Tables[0].Rows
		if results.Count > 0:
			this.View.Model.SetValue("FSaleDeptId", str(results[0]["FBizDeptID"]))
			this.View.UpdateView("FSaleDeptId")



def AlterCustAndSalerByOrg():
	FSaleOrgId = 0 if this.View.Model.GetValue("FSaleOrgId") == None else str(this.View.Model.GetValue("FSaleOrgId")["Id"])
	if FSaleOrgId == 0:
		return
	FSrcBillNo = "" if this.View.Model.GetValue("FSrcBillNo", 0) == None else str(this.View.Model.GetValue("FSrcBillNo", 0))
	FSalerId, FCustId = 0, 0
	#查询是否为销售合同
	sql = "SELECT FSaler,FCustName FROM ora_CRM_Contract WHERE FBILLNO='{}'".format(FSrcBillNo)
	srcBill = DBUtils.ExecuteDataSet(this.Context, sql).Tables[0].Rows
	if srcBill.Count > 0:
		FSalerId = srcBill[0]["FSaler"]
		FCustId = srcBill[0]["FCustName"]
	#内部合同
	sql = "SELECT FSaler,FCustName FROM ora_CRM_InnerContract WHERE FBILLNO='{}'".format(FSrcBillNo)
	srcBill = DBUtils.ExecuteDataSet(this.Context, sql).Tables[0].Rows
	if srcBill.Count > 0:
		FSalerId = srcBill[0]["FSaler"]
		FCustId = srcBill[0]["FCustName"]
	#维修合同
	sql = "SELECT FSalerID,FCustID FROM ora_CRM_MtnCont WHERE FBILLNO='{}'".format(FSrcBillNo)
	srcBill = DBUtils.ExecuteDataSet(this.Context, sql).Tables[0].Rows
	if srcBill.Count > 0:
		FSalerId = srcBill[0]["FSalerID"]
		FCustId = srcBill[0]["FCustID"]

	if FSalerId == 0 and FCustId == 0:
		return
		
	# 获取客户内码
	sql = """select FCUSTID from T_BD_CUSTOMER where FUSEORGID='{}' 
		and FNUMBER=(select FNUMBER from T_BD_CUSTOMER where FCUSTID='{}')""".format(FSaleOrgId, FCustId)
	results = DBUtils.ExecuteDataSet(this.Context, sql).Tables[0].Rows
	if results.Count > 0:
		this.View.Model.SetValue("FCustId", str(results[0]["FCUSTID"]))
	# 获取销售员内码
	sql = """SELECT FID FROM V_BD_SALESMAN WHERE FBIZORGID='{}' AND 
		FNUMBER=(SELECT FNUMBER FROM V_BD_SALESMAN WHERE FID='{}')""".format(FSaleOrgId, FSalerId)
	results = DBUtils.ExecuteDataSet(this.Context, sql).Tables[0].Rows
	if results.Count > 0:
		this.View.Model.SetValue("FSalerId", str(results[0]["FID"]))
		# 获取办事处
		sql = "EXEC proc_czly_GetOrgDeptBySalemanId @SmId='{}'".format(results[0]["FID"])
		results = DBUtils.ExecuteDataSet(this.Context, sql).Tables[0].Rows
		if results.Count > 0:
			this.View.Model.SetValue("FSaleDeptId", str(results[0]["FBizDeptID"]))


