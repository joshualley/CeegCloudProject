import clr
clr.AddReference('Kingdee.BOS')
clr.AddReference('Kingdee.BOS.Core')
clr.AddReference('Kingdee.BOS.App')

from Kingdee.BOS import *
from Kingdee.BOS.Core import *
from Kingdee.BOS.Core.Bill import *
from Kingdee.BOS.Core.DynamicForm.PlugIn import *
from Kingdee.BOS.Core.DynamicForm.PlugIn.ControlModel import *
from Kingdee.BOS.Core.DynamicForm.PlugIn.Args import *
from Kingdee.BOS.App.Data import *
from Kingdee.BOS.Util import *


def OnPreparePropertys(e):
	e.FieldKeys.Add('FApplyID') #申请人
	e.FieldKeys.Add('FInLevel') #调入职级
	e.FieldKeys.Add('F_ora_InPost') #调入岗位
	e.FieldKeys.Add('F_ora_InDept') #调入部门

def EndOperationTransaction(e):
	for dataEntity in e.DataEntitys:
		applyId = dataEntity['FApplyID']
		level = dataEntity['FInLevel']
		post = dataEntity['F_ora_InPost']
		dept = dataEntity['F_ora_InDept']

		sql = "update T_HT_EMPINFO set F_HR_Rank='{}',F_ORA_Post='{}',F_ora_DeptID='{}' where FPersonID='{}'".format(level,post,dept,applyId)
		#DBUtils.ExecuteDynamicObject(this.Context, sql)
		


