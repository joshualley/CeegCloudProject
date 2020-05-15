# _*_ code: utf-8 _*_
# 销售合同评审插件汇总

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
from System import *

def DataChanged(e):
	if str(this.Context.ClientType) != 'Mobile':
		setTaxOnFieldChange(e)
		

def AfterCreateNewEntryRow(e):
	'''新增行时设置税率，默认13%
	'''
	if str(this.Context.ClientType) != 'Mobile':
		isInTax = this.View.Model.GetValue('FIsIncludedTax')
		value = 13 if isInTax else 0
		this.View.Model.SetValue('FEntryTaxRate', value, e.Row)
		this.View.InvokeFieldUpdateService('FEntryTaxRate', 0)


def setTaxOnFieldChange(e):
	'''值更新时设置税率（是否含税），默认13%
	'''
	if e.Key == 'FIsIncludedTax':
		isInTax = this.View.Model.GetValue('FIsIncludedTax')
		entryCount = this.View.Model.GetEntryRowCount('FEntity')
		value = 13 if isInTax else 0
		
		for row in range(entryCount):
			this.View.Model.SetValue('FEntryTaxRate', value, row)
			this.View.InvokeFieldUpdateService('FEntryTaxRate', 0)



