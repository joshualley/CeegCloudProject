# 销售订单，替换销售员及其组织部门，替换客户

import clr
clr.AddReference('Kingdee.BOS.App')
from Kingdee.BOS.App.Data import *

def AfterBindData(e):
	FDocumentStatus = str(this.View.Model.GetValue("FDocumentStatus"))
	if FDocumentStatus == "Z":
		AlterCustAndSalerByOrg()
		

def AlterCustAndSalerByOrg():
	FSaleOrgId = 0 if this.View.Model.GetValue("FSaleOrgId") == None else str(this.View.Model.GetValue("FSaleOrgId")["Id"])
	FSalerId = 0 if this.View.Model.GetValue("FSalerId") == None else str(this.View.Model.GetValue("FSalerId")["Id"])
	FCustId = 0 if this.View.Model.GetValue("FCustId") == None else str(this.View.Model.GetValue("FCustId")["Id"])
	if FSalerId == 0:
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


