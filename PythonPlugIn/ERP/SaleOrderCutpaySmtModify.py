# -*- coding: utf-8 -*-
"""
销售订单上扣款报销备注修改
"""

import clr
clr.AddReference('Kingdee.BOS.App')
from Kingdee.BOS.App.Data import *

IsReturnData = False

def AfterBindData(e):
	row = this.View.OpenParameter.GetCustomParameter('row')
	this.Model.SetValue('FOrderRow', row)
	this.View.UpdateView('FOrderRow')

def BarItemClick(e):
	global IsReturnData
	key = e.BarItemKey.upper()
	if key == 'ORA_TBRTDATA': #ora_tbRtData
		rmk = '' if this.Model.DataObject['F_ora_CutpaySmtRmk'] is None else this.Model.DataObject['F_ora_CutpaySmtRmk']
		eid = this.View.OpenParameter.GetCustomParameter('eid')
		eid = '0' if eid is None else str(eid)
		sql = "/*dialect*/UPDATE T_SAL_ORDERENTRY SET F_ora_CutpaySmtRmk='{}' WHERE FEntryID='{}'".format(rmk, eid)
		#this.View.ShowMessage(sql)
		DBUtils.Execute(this.Context, sql)
		this.View.Close()

def BeforeClose(e):
	global IsReturnData
	if IsReturnData == False:
		this.View.ReturnToParentWindow(None)