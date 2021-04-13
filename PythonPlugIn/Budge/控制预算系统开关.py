import clr
clr.AddReference('Kingdee.BOS.App')

from Kingdee.BOS.App.Data import *

def AfterBindData(e):
	sql = 'EXEC proc_cz_ly_IsUsingBdgSys'
	values = DBUtils.ExecuteDataSet(this.Context, sql).Tables[0].Rows
	if values.Count <= 0:
		return
	value = str(values[0]['FSwitch']) == '1'
	this.Model.SetValue('FStartBdgCtr',  value)
	this.View.UpdateView('FStartBdgCtr')
	if value:
		sql = 'EXEC proc_czly_GetPrjCtrl'
		datas = DBUtils.ExecuteDataSet(this.Context, sql).Tables[0].Rows
		this.Model.BatchCreateNewEntryRow('FEntity', datas.Count)
		for i, row in enumerate(datas):
			this.Model.SetValue('FBraOffice',row['FBraOffice'], i)
			this.Model.SetValue('FCtrl4Prj', row['FCtrl4Prj'], i)
		this.View.UpdateView('FEntity')

def DataChanged(e):
	key = str(e.Field.Key)
	if key == 'FStartBdgCtr':
		value = this.View.Model.GetValue('FStartBdgCtr')
		value = 1 if value else 0
		sql = "EXEC proc_cz_ly_UsingBdgSysSetting @FSwitch='{}'".format(value)
		DBUtils.Execute(this.Context, sql)
	elif key == 'FCtrl4Prj':
		BraOffice = this.View.Model.GetValue('FBraOffice', e.Row)['Id']
		value = this.View.Model.GetValue('FCtrl4Prj', e.Row)
		value = 1 if value else 0
		sql = "EXEC proc_czly_SetPrjCtrl @FBraOffice='{}', @FCtrl4Prj='{}' ".format(BraOffice, value)
		DBUtils.Execute(this.Context, sql)