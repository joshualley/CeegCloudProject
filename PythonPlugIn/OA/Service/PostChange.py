# 调职流程审核时反写

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
	e.FieldKeys.Add("F_ora_InPost") #调入岗位
	e.FieldKeys.Add("FInLevel") #调入职级
	e.FieldKeys.Add("F_ora_InDept") #调入部门
	e.FieldKeys.Add("F_ora_AfterAddr") #调职后工作地点
	e.FieldKeys.Add("F_ora_Type") #员工合同类型
	e.FieldKeys.Add("FApplyID") #申请人


def BeginOperationTransaction(e):
	opKey = this.FormOperation.Operation.upper()
	if opKey == "AUDIT":
		for entity in e.DataEntitys:
			F_ora_InPost = str(entity["F_ora_InPost"]["Id"])
			FInLevel = str(entity["FInLevel"]["Id"])
			F_ora_InDept = str(entity["F_ora_InDept"]["Id"])
			F_ora_AfterAddr = str(entity["F_ora_AfterAddr"])
			F_ora_Type = str(entity["F_ora_Type"]["Id"])
			FApplyID = str(entity["FApplyID"]["Id"])
			sql = """UPDATE T_HR_EMPINFO
			SET F_HR_RANK='{}',F_ORA_POST='{}',F_ORA_DEPTID='{}'
			WHERE FID='{}';
			UPDATE T_BD_PERSON 
			SET FWORKADDRESS='{}', FCONTRACTTYPE='{}'
			WHERE FID='{}';
			""".format(FInLevel, F_ora_InPost, F_ora_InDept, FApplyID,
				F_ora_AfterAddr, F_ora_Type, FApplyID)
			CZ_GetData(sql)

def EndOperationTransaction(e):
	pass


def CZ_GetData(sql):
	try:
		objs = DBUtils.ExecuteDynamicObject(this.Context, sql)
	except Exception as e:
		raise e

	return objs