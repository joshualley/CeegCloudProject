"""
查询销售员结算明细
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
	QuerySettle()


def EntityRowDoubleClick(e):
	fid = this.Model.GetValue("FIntegerID", e.Row)
	param = BillShowParameter()
	param.FormId = "ora_OptExp_Settle2"
	param.OpenStyle.ShowType = ShowType.Modal
	param.ParentPageId = this.View.PageId
	param.PKey = str(fid)
	param.Status = OperationStatus.VIEW

	this.View.ShowForm(param)


def QuerySettle():
	"""查询销售员的明细
	"""
	year = this.View.OpenParameter.GetCustomParameter('year')
	month = this.View.OpenParameter.GetCustomParameter('month')
	FSellerNumber = this.View.OpenParameter.GetCustomParameter('FSellerNumber')
	if year is None or month is None or FSellerNumber is None:
		return
	sql = '''
	SELECT s.FBillNo, se.*
FROM ora_OptExp_SettleEntry se
INNER JOIN ora_OptExp_Settle s ON s.FID=se.FID AND s.FDOCUMENTSTATUS='C'
INNER JOIN V_BD_SALESMAN sm ON se.FSellerID=sm.FID 
WHERE YEAR(FDate)='{}' AND MONTH(FDATE)='{}' 
AND sm.FNumber='{}'
	'''.format(year, month, FSellerNumber)
	objs = DBUtils.ExecuteDataSet(this.Context, sql).Tables[0].Rows
	this.Model.DeleteEntryData("FEntity")
	if objs.Count <= 0:
		return
	this.Model.BatchCreateNewEntryRow("FEntity", objs.Count)
	for i in range(objs.Count):
		this.Model.SetValue("FSettleNo", objs[i]["FBILLNO"], i)
		this.Model.SetValue("FIntegerID", objs[i]["FID"], i)
		this.Model.SetValue("FSellerId", objs[i]["FSellerId"], i)
		this.Model.SetValue("FOrderNo", objs[i]["FOrderNo"], i)
		this.Model.SetValue("FCustId", objs[i]["FCustId"], i)
		this.Model.SetValue("FProdSeries", objs[i]["FProdSeries"], i)
		this.Model.SetValue("FRealSettleAmt", objs[i]["FRealSettleAmt"], i)
		
	this.View.UpdateView("FEntity")