# 销售出库，带出销售员电话信息

import clr
clr.AddReference('Kingdee.BOS.App')
from Kingdee.BOS.App.Data import *


def AfterBindData(e):
	if str(this.View.Model.GetValue("FDocumentStatus")) == "Z":
		SetSalemanPhone()

def DataChanged(e):
	if e.Key == "FSalesManID":
		SetSalemanPhone()


def SetSalemanPhone():
	smId = 0 if this.View.Model.GetValue("FSalesManID") == None else str(this.View.Model.GetValue("FSalesManID")["Id"])
	orgId = -1 if this.View.Model.GetValue("FStockOrgId") == None else str(this.View.Model.GetValue("FStockOrgId")["Id"])
	sql = "EXEC proc_czly_GetOrgDeptBySalemanId @SmId='{}',@OrgId='{}'".format(smId, orgId)
	objs = DBUtils.ExecuteDataSet(this.Context, sql).Tables[0].Rows
	if objs.Count > 0:
		this.View.Model.SetValue("FSalesManID", str(objs[0]["FSalesmanId"]))
		this.View.Model.SetValue("F_Salemolie", str(objs[0]["FMobile"]))
		this.View.Model.SetValue("FSmOrgId1", str(objs[0]["FOrgID"]))
		this.View.Model.SetValue("FSmDeptID1", str(objs[0]["FDeptID"]))
		
