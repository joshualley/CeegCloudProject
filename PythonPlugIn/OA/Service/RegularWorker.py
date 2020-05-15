# 转正反写员工信息

import clr
clr.AddReference('Kingdee.BOS')
clr.AddReference('Kingdee.BOS.Core')
clr.AddReference('Kingdee.BOS.App')

from Kingdee.BOS import *
from Kingdee.BOS.Core import *
from Kingdee.BOS.Core.Bill import *
from Kingdee.BOS.Core.DynamicForm.PlugIn import *
from Kingdee.BOS.Core.DynamicForm.PlugIn.ControlModel import *
from Kingdee.BOS.App.Data import *


def OnPreparePropertys(e):
	e.FieldKeys.Add("FApplyID") #申请人
	e.FieldKeys.Add("FAfterDeptID") #转正后部门
	e.FieldKeys.Add("FAfterPost") #转正后岗位
	e.FieldKeys.Add("FAfterLevel") #职级
	e.FieldKeys.Add("FProbation") #试用期
	e.FieldKeys.Add("F_ora_toDate") #转正日期
	


def BeginOperationTransaction(e):
	opKey = this.FormOperation.Operation.upper()
	if opKey == "AUDIT":
		for entity in e.DataEntitys:
			FApplyID = str(entity["FApplyID"]["Id"])
			FAfterDeptID = str(entity["FAfterDeptID"]["Id"])
			FAfterPost = str(entity["FAfterPost"]["Id"])
			FAfterLevel = str(entity["FAfterLevel"]["Id"])
			FProbation = str(entity["FProbation"])
			F_ora_toDate = str(entity["F_ora_toDate"])

			sql = """UPDATE T_HR_EMPINFO
			SET F_HR_RANK='{}', F_ORA_POST='{}', F_ORA_DEPTID='{}'
			WHERE FID='{}';
			UPDATE T_BD_PERSON 
			SET FProbation='{}', F_ora_toDate='{}'
			WHERE FID='{}';
			""".format(FAfterLevel, FAfterPost, FAfterDeptID, FApplyID,
				FProbation, F_ora_toDate, FApplyID)
			CZ_GetData(sql)


def CZ_GetData(sql):
	try:
		objs = DBUtils.ExecuteDynamicObject(this.Context, sql)
	except Exception as e:
		raise e

	return objs
