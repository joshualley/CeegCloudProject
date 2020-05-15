# 续签反写员工信息

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
from System import DataTime


def OnPreparePropertys(e):
	e.FieldKeys.Add("FEndDate") #合同结束日期
	e.FieldKeys.Add("F_ora_SignYear") #续签年限
	e.FieldKeys.Add("F_ora_Level") #职级
	e.FieldKeys.Add("F_ora_Workplace") #工作地点
	e.FieldKeys.Add("F_ora_ContractType") #员工合同类型
	e.FieldKeys.Add("FApplyID") #申请人


def BeginOperationTransaction(e):
	opKey = this.FormOperation.Operation.upper()
	if opKey == "AUDIT":
		for entity in e.DataEntitys:
			FEndDate = str(entity["FEndDate"])
			F_ora_SignYear = int(entity["F_ora_SignYear"])
			FEndDate = str(DataTime.Parse(FEndDate).AddYears(F_ora_SignYear))

			F_ora_Level = str(entity["F_ora_Level"]["Id"])
			F_ora_Workplace = str(entity["F_ora_Workplace"])
			F_ora_ContractType = str(entity["F_ora_ContractType"]["Id"])
			FApplyID = str(entity["FApplyID"]["Id"])
			sql = """UPDATE T_HR_EMPINFO
			SET F_HR_RANK='{}'
			WHERE FID='{}';
			UPDATE T_BD_PERSON 
			SET FWORKADDRESS='{}', FCONTRACTTYPE='{}', FHTDateEnd='{}'
			WHERE FID='{}';
			""".format(F_ora_Level, FApplyID,
				F_ora_Workplace, F_ora_ContractType, FEndDate, FApplyID)
			CZ_GetData(sql)


def CZ_GetData(sql):
	try:
		objs = DBUtils.ExecuteDynamicObject(this.Context, sql)
	except Exception as e:
		raise e

	return objs