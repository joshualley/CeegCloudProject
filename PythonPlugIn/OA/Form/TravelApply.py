# 出差申请PlugIn

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

def DataChanged(e):
	if str(this.Context.ClientType) != 'Mobile':
		if e.Key == 'FAddressType':
			ttype = JudgeTravelType()
			this.View.Model.SetValue('FCountry', ttype)


def BeforeDoOperation(e):
	if str(this.Context.ClientType) != 'Mobile':
		opKey = e.Operation.FormOperation.Operation
		if opKey in ('Save', 'Submit'):
			ttype = JudgeTravelType()
			this.View.Model.SetValue('FCountry', ttype)


def JudgeTravelType():
	'''根据表体出差类型，判断国内外
	'''
	rowNum = this.View.Model.GetEntryRowCount('FEntity')
	for i in range(0, rowNum):
		type = this.View.Model.GetValue('FAddressType', i)
		if type in ('2', '3', '4'):
			return '国内'
		elif type in ('5', '6', '7', '8'):
			return '国外'
