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

def AfterBindData(e):
	sql = 'EXEC proc_cz_ly_IsUsingBdgSys'
	value = DBUtils.ExecuteDataSet(this.Context, sql).Tables[0].Rows[0]['FSwitch']
	value = True if str(value) == '1' else False
	this.View.Model.SetValue('FStartBdgCtr',  value)
	this.View.UpdateView('FStartBdgCtr')
	if value:
		sql = 'EXEC proc_czly_GetPrjCtrl'
		data = DBUtils.ExecuteDataSet(this.Context, sql).Tables[0]
		for i, row in enumerate(data.Rows):
			this.View.Model.CreateNewEntryRow('FEntity')
			this.View.Model.SetItemValueByID('FBraOffice',row['FBraOffice'], i)
			this.View.Model.SetValue('FCtrl4Prj', row['FCtrl4Prj'], i)
		this.View.Model.DeleteEntryRow('FEntity', len(data.Rows))
		this.View.UpdateView('FEntity')

def DataChanged(e):
	if e.Key == 'FStartBdgCtr':
		value = this.View.Model.GetValue('FStartBdgCtr')
		value = 1 if value else 0
		sql = "EXEC proc_cz_ly_UsingBdgSysSetting @FSwitch='{}'".format(value)
		DBUtils.ExecuteDataSet(this.Context, sql)
	elif e.Key == 'FCtrl4Prj':
		BraOffice = this.View.Model.GetValue('FBraOffice', e.Row)['Id']
		value = this.View.Model.GetValue('FCtrl4Prj', e.Row)
		value = 1 if value else 0
		sql = "EXEC proc_czly_SetPrjCtrl @FBraOffice='{}', @FCtrl4Prj='{}' ".format(BraOffice, value)
		DBUtils.ExecuteDataSet(this.Context, sql)