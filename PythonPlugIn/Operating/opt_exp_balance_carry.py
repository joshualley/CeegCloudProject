"""
业务费余额结转
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

# 订单明细当前点击行
currEntryRow = 0
def EntityRowClick(e):
	if e.Key == "FEntry".upper():
		global currEntryRow
		currEntryRow = e.Row


def AfterBindData(e):
	FDocumentStatus = this.Model.GetValue("FDocumentStatus")
	if str(FDocumentStatus) == "Z":
		now = DateTime.Now
		this.Model.SetValue("FYear", now.Year)
		this.Model.SetValue("FMonth", now.Month-1)
		this.View.UpdateView('FYear')
		this.View.UpdateView('FMonth')


def BeforeDoOperation(e):
	def Pass():
		year = this.Model.GetValue("FYear")
		month = this.Model.GetValue("FMonth")
		sql = "SELECT FID FROM ora_Exp_Balance WHERE FYear={} AND FMonth={}".format(year, month)
		objs = DBUtils.ExecuteDataSet(this.Context, sql).Tables[0].Rows
		if objs.Count > 0:
			this.View.ShowMessage("本期余额表已经存在，不允许重复创建！")
			return False
		return True
	_opKey = e.Operation.FormOperation.Operation
	if _opKey == 'Save':
		if not Pass():
			e.Cancel = True
	elif _opKey == 'Submit':
		if not Pass():
			e.Cancel = True


def AfterEntryBarItemClick(e):
	key = e.BarItemKey.upper()
	if key == 'ORA_TBSETTLE':
		OpenForm("ora_OptExp_QSettle")
	elif key == "ORA_TBACCOUNT":
		OpenForm("ora_OptExp_QAccount")


def AfterButtonClick(e):
	key = e.Key.upper()
	if key == "FQUERYBTN":
		Query()


def Query():
	FDocumentStatus = this.Model.GetValue("FDocumentStatus")
	if str(FDocumentStatus) != "Z":
		this.View.ShowMessage("单据已保存，不允许重复结转！")
		return
	year = this.Model.GetValue("FYear")
	month = this.Model.GetValue("FMonth")
	sql = "exec proc_czly_QueryExpBalance @FYear={}, @FMonth={}".format(year, month)
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