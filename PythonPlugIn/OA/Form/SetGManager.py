# 设置单位总经理

import clr
clr.AddReference('Kingdee.BOS.App')
from Kingdee.BOS.App.Data import *

def AfterBindData(e):
	sql = "select * from ora_OA_GManager order by FID"
	objs = DBUtils.ExecuteDataSet(this.Context, sql).Tables[0].Rows
	if objs.Count > 0:
		for i, row in enumerate(objs):
			this.View.Model.CreateNewEntryRow("FEntity")
			this.View.Model.SetValue("FOrgID", str(row["FOrgID"]), i)
			this.View.Model.SetValue("FDeptID", str(row["FDeptID"]), i)
			this.View.Model.SetValue("FGManagerID", str(row["FGManagerID"]), i)
			this.View.Model.SetValue("FID", str(row["FID"]), i)


def AfterBarItemClick(e):
	key = e.BarItemKey.upper()
	this.View.ShowMessage(key)
	if key == "TBSAVE":
		entity = this.View.Model.DataObject["FEntity"]
		sql = "DELETE FROM ora_OA_GManager"
		DBUtils.ExecuteDataSet(this.Context, sql)
		for row in entity:
			FOrgID = "0" if row["FOrgID"] == None else str(row["FOrgID"]["Id"])
			FDeptID = "0" if row["FDeptID"] == None else str(row["FDeptID"]["Id"])
			FGManagerID = "0" if row["FGManagerID"] == None else str(row["FGManagerID"]["Id"])
			sql = """INSERT INTO ora_OA_GManager(FOrgID,FDeptID,FGManagerID) VALUES('{}','{}','{}')
			""".format(FOrgID, FDeptID, FGManagerID)
			DBUtils.ExecuteDataSet(this.Context, sql)
		this.View.ShowMessage("保存成功！")

