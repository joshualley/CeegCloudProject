import clr
clr.AddReference('Kingdee.BOS.App')
from Kingdee.BOS.App.Data import *

def AfterBindData(e):
        _FDocumentStatus =  this.View.BillModel.GetValue("FDocumentStatus")
        saveBtn = this.View.GetControl("FSaveBtn")
        submitBtn = this.View.GetControl("FSubmitBtn")
        if  _FDocumentStatus == "Z":
        	saveBtn.Visible = True
        	submitBtn.Visible = True
        	saveBtn.SetCustomPropertyValue("width", 155)
        	submitBtn.SetCustomPropertyValue("width", 155)