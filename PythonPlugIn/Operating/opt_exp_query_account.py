"""
查询销售员入账明细
"""
import clr
clr.AddReference('Kingdee.BOS')
clr.AddReference('Kingdee.BOS.Core')
clr.AddReference('Kingdee.BOS.App')

from Kingdee.BOS.App.Data import *
from Kingdee.BOS import *
from Kingdee.BOS.Core import *
from Kingdee.BOS.Core.Bill import *
from Kingdee.BOS.Core.Metadata import *
from Kingdee.BOS.Core.DynamicForm import *
from Kingdee.BOS.Core.DynamicForm.PlugIn import *
from Kingdee.BOS.Core.DynamicForm.PlugIn.ControlModel import *


def AfterBindData(e):
	QueryAccount()


def EntityRowDoubleClick(e):
	fid = this.Model.GetValue("FIntegerID", e.Row)
	param = BillShowParameter()
	param.FormId = "ora_CZ_Account"
	param.OpenStyle.ShowType = ShowType.Modal
	param.ParentPageId = this.View.PageId
	param.PKey = str(fid)
	param.Status = OperationStatus.VIEW

	this.View.ShowForm(param)


def QueryAccount():
	"""查询销售员的费用台账明细
	"""
	year = this.View.OpenParameter.GetCustomParameter('year')
	month = this.View.OpenParameter.GetCustomParameter('month')
	FSellerNumber = this.View.OpenParameter.GetCustomParameter('FSellerNumber')
	if year is None or month is None or FSellerNumber is None:
		return
	sql = '''
	SELECT a.FBILLNO, ae.* 
FROM ora_t_AccountEntry ae
INNER JOIN ora_t_Account a ON ae.FID=a.FID AND a.FDOCUMENTSTATUS='C'
INNER JOIN T_HR_EMPINFO e ON e.FID=ae.FSellerEmpID
INNER JOIN V_BD_SALESMAN sm ON e.FSTAFFID=sm.FSTAFFID
WHERE YEAR(a.FDate)='{}' AND MONTH(a.FDate)='{}'
AND sm.FNUMBER='{}' AND FAccountActual=828919
	'''.format(year, month, FSellerNumber)
	objs = DBUtils.ExecuteDataSet(this.Context, sql).Tables[0].Rows
	this.Model.DeleteEntryData("FEntity")
	if objs.Count <= 0:
		return
	this.Model.BatchCreateNewEntryRow("FEntity", objs.Count)
	for i in range(objs.Count):
		this.Model.SetValue("FAccNo", objs[i]["FBILLNO"], i)
		this.Model.SetValue("FIntegerID", objs[i]["FID"], i)
		this.Model.SetValue("FAccountID", objs[i]["FAccountID"], i)
		this.Model.SetValue("FAccountDesp", objs[i]["FAccountDesp"], i)
		this.Model.SetValue("FAmountBase", objs[i]["FAmountBase"], i)
		this.Model.SetValue("FAmountOriginal", objs[i]["FAmountOriginal"], i)
		this.Model.SetValue("FAccountActual", objs[i]["FAccountActual"], i)
		this.Model.SetValue("FSellerEmpID", objs[i]["FSellerEmpID"], i)
		
	this.View.UpdateView("FEntity")