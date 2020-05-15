# 付款单接收对公资金下推数据

import clr
clr.AddReference('Kingdee.BOS.App')
from Kingdee.BOS.App.Data import *

def AfterBindData(e):
	FDocumentStatus = str(this.View.Model.GetValue("FDOCUMENTSTATUS"))
	if FDocumentStatus == "Z":
		setEntryBySrcEntry()
		refreshContactUnit()

def DataChanged(e):
	if str(e.Key) == "FSRCBILLNO": #源单编号
		setEntryBySrcEntry()
	if str(e.Key) == "FPAYORGID":  #付款组织
		refreshContactUnit()

def BeforeDoOperation(e):
	_opKey = e.Operation.FormOperation.Operation
	if _opKey in ('Submit', 'Save'):
		refreshContactUnit()


def setEntryBySrcEntry():
	''' 由源单明细表体写入明细表体
	'''
	srcEntry = this.View.Model.DataObject["PAYBILLSRCENTRY"] #源单明细
	rowNum = this.View.Model.GetEntryRowCount("FPAYBILLENTRY") #明细表体
	if rowNum > 0:
		if (str(this.View.Model.GetValue("FPAYTOTALAMOUNTFOR", rowNum-1)) == "0" and 
			str(this.View.Model.GetValue("FPAYAMOUNTFOR_E", rowNum-1)) == "0" and 
			str(this.View.Model.GetValue("FSETTLEPAYAMOUNTFOR", rowNum-1)) == "0"):
			this.View.Model.DeleteEntryRow("FPAYBILLENTRY", rowNum-1)

	for row in srcEntry:
		srcBillType = str(row["SOURCETYPE"])
		if srcBillType != "k191b3057af6c4252bcea813ff644cd3a": #对公资金申请
			continue
		costId = 0 if row["SRCCOSTID"] == None else row["SRCCOSTID"]["Id"]
		settleTypeId = 0 if row["SRCSETTLETYPEID"] == None else row["SRCSETTLETYPEID"]["Id"]
		amount = row["AFTTAXTOTALAMOUNT"]
		comment = row["SRCREMARK"]

		this.View.Model.CreateNewEntryRow("FPAYBILLENTRY")
		i = this.View.Model.GetEntryRowCount("FPAYBILLENTRY") - 1

		this.View.Model.SetValue("FSETTLETYPEID", settleTypeId, i)
		this.View.Model.SetValue("FCOSTID", costId, i)
		this.View.Model.SetValue("FCOMMENT", comment, i)
		this.View.Model.SetValue("FPAYTOTALAMOUNTFOR", amount, i)
		this.View.Model.SetValue("FPAYAMOUNTFOR_E", amount, i)
		this.View.Model.SetValue("FSETTLEPAYAMOUNTFOR", amount, i)
	this.View.UpdateView("FPAYBILLENTRY")

def refreshContactUnit():
	'''根据付款组织刷新往来单位
	'''
	payOrgId = 0 if this.View.Model.GetValue("FPAYORGID") == None else str(this.View.Model.GetValue("FPAYORGID")["Id"])
	contactUnitType = "" if this.View.Model.GetValue("FCONTACTUNITTYPE") == None else str(this.View.Model.GetValue("FCONTACTUNITTYPE"))
	contactUnitNumber = 0 if this.View.Model.GetValue("FCONTACTUNIT") == None else str(this.View.Model.GetValue("FCONTACTUNIT")["Number"])
	if contactUnitType == "BD_Supplier":
		sql = "SELECT FSUPPLIERID FROM T_BD_Supplier WHERE FNUMBER='{}' AND FUSEORGID='{}'".format(contactUnitNumber, payOrgId)
		objs = DBUtils.ExecuteDataSet(this.Context, sql).Tables[0].Rows
		if objs.Count > 0:
			this.View.Model.SetValue("FCONTACTUNIT", str(objs[0]["FSUPPLIERID"]))
	elif contactUnitType == "BD_Customer":
		sql = "SELECT FCUSTID FROM T_BD_CUSTOMER WHERE FNUMBER='{}' AND FUSEORGID='{}'".format(contactUnitNumber, payOrgId)
		objs = DBUtils.ExecuteDataSet(this.Context, sql).Tables[0].Rows
		if objs.Count > 0:
			this.View.Model.SetValue("FCONTACTUNIT", str(objs[0]["FCUSTID"]))


