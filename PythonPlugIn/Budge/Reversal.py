import clr
clr.AddReference('Kingdee.BOS')
clr.AddReference('Kingdee.BOS.Core')
clr.AddReference('Kingdee.BOS.App')

from Kingdee.BOS import *
from Kingdee.BOS.Core import *
from Kingdee.BOS.Core.Bill import *
from Kingdee.BOS.Core.DynamicForm.PlugIn import *
from Kingdee.BOS.Core.DynamicForm.PlugIn.ControlModel import *
from System import *
from Kingdee.BOS.App.Data import *

# 测试，预算反结转

def BarItemClick(e):
	if e.BarItemKey == 'FREVERSAL':
		FYear = this.View.Model.GetValue('FYear')
		FMonth = this.View.Model.GetValue('FMonth') + 1
		FBraOffice = this.View.Model.GetValue('FBraOffice')['Id']
		this.View.Model.SetValue('FIsTurn', 0)
		#objs = DBUtils.ExecuteDynamicObject(this.Context, sql)