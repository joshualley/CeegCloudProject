# 调职，设置调入部门单位总经理

import clr
clr.AddReference('Kingdee.BOS.App')
from Kingdee.BOS.App.Data import *


def DataChanged(e):
	if e.Field.Key in ("", ""):
		orgId = 0 if this.View.Model.GetValue("") == None else str(this.View.Model.GetValue("")["Id"])
		deptId = 0 if this.View.Model.GetValue("") == None else str(this.View.Model.GetValue("")["Id"])

		sql = "exec proc_czly_GetGManager @FOrgId='{}', @FDeptId='{}'".format(orgId, deptId)
		results = DBUtils.ExecuteDataSet(this.Context, sql).Tables[0].Rows
		if results.Count > 0:
			gManager = results[0]["FID"]
			this.View.Model.SetValue("FManager1", gManager)





