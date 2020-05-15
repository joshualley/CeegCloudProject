# 收款单，源单明细写入明细表体

from System import Guid

def AfterBindData(e):
	FDocumentStatus = str(this.View.Model.GetValue("FDOCUMENTSTATUS"))
	if FDocumentStatus == "Z":
		setEntryBySrcEntry()


def DataChanged(e):
	if str(e.Key) == "FSRCBILLNO": #源单编号
		setEntryBySrcEntry()


def setEntryBySrcEntry():
	''' 由源单明细表体写入明细表体
	'''
	srcEntry = this.View.Model.DataObject["RECEIVEBILLSRCENTRY"]

	entry = this.View.Model.DataObject["RECEIVEBILLENTRY"]
	rowNum = this.View.Model.GetEntryRowCount("FRECEIVEBILLENTRY")
	if rowNum > 0:
		if (str(entry[rowNum-1]["RECTOTALAMOUNTFOR"]) == "0" and 
			str(entry[rowNum-1]["RECAMOUNTFOR_E"]) == "0" and
			str(entry[rowNum-1]["SETTLERECAMOUNTFOR"]) == "0"):
			this.View.Model.DeleteEntryRow("FRECEIVEBILLENTRY", rowNum-1)
	for idx, row in enumerate(srcEntry):
		srcBillType = str(row["SRCBILLTYPEID"])
		if srcBillType == "ora_CZ_ReceiptSplit":
			guid = Guid.NewGuid()
			amount = row["REALRECAMOUNT"] #本次收款金额
			this.View.Model.CreateNewEntryRow("FRECEIVEBILLENTRY")
			i = this.View.Model.GetEntryRowCount("FRECEIVEBILLENTRY") - 1
			this.View.Model.SetValue("FRECTOTALAMOUNTFOR", amount, i) #应收金额
			this.View.Model.SetValue("FRECAMOUNTFOR_E", amount, i) #收款金额
			this.View.Model.SetValue("FSETTLERECAMOUNTFOR", amount, i) #折后金额
			this.View.Model.SetValue("F_ora_ReGuid", guid, i) #明细表体
			this.View.Model.SetValue("F_ora_RseGuid", guid, idx) #源单明细表体

	this.View.UpdateView("FRECEIVEBILLENTRY")
