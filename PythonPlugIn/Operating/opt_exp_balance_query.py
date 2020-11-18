"""
业务费余额查询
"""
import clr
clr.AddReference('Kingdee.BOS')
clr.AddReference('Kingdee.BOS.Core')
clr.AddReference('Kingdee.BOS.App')

from Kingdee.BOS.App.Data import *
from Kingdee.BOS import *
from Kingdee.BOS.Core import *
from Kingdee.BOS.Core.DynamicForm import *
from Kingdee.BOS.Core.DynamicForm.PlugIn import *
from Kingdee.BOS.Core.DynamicForm.PlugIn.ControlModel import *
from System import DateTime

def AfterBindData(e):
	now = DateTime.Now
	this.Model.SetValue("FYear", now.Year)
	this.Model.SetValue("FMonth", now.Month-1)
	this.View.UpdateView('FYear')
	this.View.UpdateView('FMonth')

# 订单明细当前点击行
currEntryRow = 0
def EntityRowClick(e):
	if e.Key == "FEntity".upper():
		global currEntryRow
		currEntryRow = e.Row


def AfterEntryBarItemClick(e):
	key = e.BarItemKey.upper()
	if key == 'ORA_TBSETTLE':
		OpenForm("ora_OptExp_QSettle")
	elif key == "ORA_TBACCOUNT":
		OpenForm("ora_OptExp_QAccount")
		

def AfterButtonClick(e):
	key = e.Key.upper()
	if key == "FQUERYBTN":
		QueryBalance()


def OpenForm(formid):
	param = DynamicFormShowParameter()
	param.FormId = formid
	param.OpenStyle.ShowType = ShowType.Modal
	param.ParentPageId = this.View.PageId

	global currEntryRow
	FSellerNumber = this.Model.GetValue("FSellerId", currEntryRow)
	FSellerNumber = "" if FSellerNumber is None else str(FSellerNumber["Number"])
	year = str(this.Model.GetValue("FYear"))
	month = str(this.Model.GetValue("FMonth"))

	param.CustomParams.Add("FSellerNumber", FSellerNumber)
	param.CustomParams.Add("year", year)
	param.CustomParams.Add("month", month)
	this.View.ShowForm(param)


def QueryBalance():
	year = this.Model.GetValue("FYear")
	month = this.Model.GetValue("FMonth")
	FSellerNo = this.Model.GetValue("FQSellerId")
	FSellerNo = "" if FSellerNo is None else str(FSellerNo["Number"])
	sql = "exec proc_czly_QueryExpBalance @FYear={}, @FMonth={}, @FSellerNo='{}'".format(year, month, FSellerNo)
	objs = DBUtils.ExecuteDataSet(this.Context, sql).Tables[0].Rows
	this.Model.DeleteEntryData("FEntity")
	if objs.Count <= 0:
		this.View.ShowMessage('未查询到数据！')
		return
	this.Model.BatchCreateNewEntryRow("FEntity", objs.Count)
	for i in range(objs.Count):
		this.Model.SetValue("FSellerId", objs[i]["FSellerId"], i)
		this.Model.SetValue("FInitBalance", objs[i]["FInitBalance"], i)
		this.Model.SetValue("FOldSysAmt", objs[i]["FOldSysAmt"], i)
		this.Model.SetValue("FNewSysAmt", objs[i]["FNewSysAmt"], i)
		this.Model.SetValue("FRecordedAmt", objs[i]["FRecordedAmt"], i)
		this.Model.SetValue("FCurrPeriodBalance", objs[i]["FCurrPeriodBalance"], i)
	this.View.UpdateView("FEntity")
	