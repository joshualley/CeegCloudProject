"""
单据插件
注册于子系统：业务费结算
单据：业务费结算
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
	now = DateTime.Now;
	year = now.Year - 1 if now.Month == 1 else now.Year
	month = 12 if now.Month == 1 else now.Month - 1
	sDt = "{}-{}-01".format(year, month)
	this.Model.SetValue("FQSDate", sDt)
	this.Model.SetValue("FQEDate", str(now))


def AfterEntryBarItemClick(e):
	key = e.BarItemKey.upper()
	if key == 'ORA_DRAW':
		param = DynamicFormShowParameter()
		param.FormId = "ora_OptExp_OrderDraw"
		param.OpenStyle.ShowType = ShowType.Modal
		param.ParentPageId = this.View.PageId
		this.View.ShowForm(param, return_callback)


def return_callback(FormResult):
	"""选单返回数据时回调"""
	if FormResult.ReturnData is None:
		return
	fids = str(FormResult.ReturnData)
	sql = '''SELECT 
	o.FID, FBillNo FOrderNo, FSalerId, FDate, FCustId, 
	SUM(FAllAmount_LC) FOrderAmt, SUM(F_CZ_FBPAmt) FBaseAmt
FROM T_SAL_ORDER o
INNER JOIN T_SAL_ORDERENTRY oe ON o.FID=oe.FID
INNER JOIN T_SAL_ORDERENTRY_F oef ON oef.FEntryID=oe.FEntryID
WHERE ISNULL(F_ora_Jjyy, '')='' AND o.FID IN ({})
GROUP BY o.FID, FBillNo, FSalerId, FDate, FCustId'''.format(fids)

	objs = DBUtils.ExecuteDataSet(this.Context, sql).Tables[0].Rows
	if objs.Count <= 0:
		return
	entity = this.Model.DataObject['FEntityOrd']
	billNos = [str(row["FOSourceBillNo"]) for row in entity]
	i = entity.Count
	for row in objs:
		if row["FOrderNo"] in billNos:
			continue
		this.Model.CreateNewEntryRow("FEntityOrd")
		this.Model.SetValue("FOSourceBillNo", row["FOrderNo"], i)
		this.Model.SetValue("FOSellerId", row["FSalerId"], i)
		this.Model.SetValue("FODate", row["FDate"], i)
		this.Model.SetValue("FOCustId", row["FCustId"], i)
		this.Model.SetValue("FOOrderAmt", row["FOrderAmt"], i)
		this.Model.SetValue("FOBaseAmt", row["FBaseAmt"], i)
		this.Model.SetValue("FOrderId", row["FID"], i)
		i += 1 

	this.View.UpdateView("FEntityOrd")



def AfterButtonClick(e):
	if e.Key == "FSETTLEBTN":
		FDocumentStatus = this.Model.GetValue("FDocumentStatus")
		if str(FDocumentStatus) != "Z":
			this.View.ShowMessage("单据已经完成结算，不允许再次结算！")
			return

		FQOrderNo = this.Model.GetValue("FQOrderNo")
		FQOrderNo = str(FQOrderNo) if FQOrderNo is not None else ""
		FQSellerId = this.Model.GetValue("FQSellerId")
		FQSellerNumber = str(FQSellerId["Number"]) if FQSellerId is not None else ""
		FQSDate = this.Model.GetValue("FQSDate")
		FQSDate = str(FQSDate) if FQSDate is not None else ""
		FQEDate = this.Model.GetValue("FQEDate")
		FQEDate = str(FQEDate) if FQEDate is not None else ""
		if FQSDate == "" or FQEDate == "":
			this.View.ShowMessage("开始和结束日期必录！")
			return
		search_split_entry(FQOrderNo, FQSellerNumber, FQSDate, FQEDate)
		create_settle_entry(FQOrderNo, FQSellerNumber, FQSDate, FQEDate)


def search_split_entry(FQOrderNo, FQSellerNumber, FQSDate, FQEDate):
	sql = """SELECT s.FBillNo FSplitNo, F_ora_OrgId FRcvOrgId,
    sp.FCustID, FOrderNo, s.FCreateDate FRcvDate, FSplitAmount, s.FID, sp.FEntryIDOP
FROM T_CZ_ReceiptSplitOrderPlan sp
INNER JOIN T_CZ_ReceiptSplit s ON sp.FID=s.FID
INNER JOIN T_SAL_ORDER o ON sp.FOrderInterID=o.FID
INNER JOIN V_BD_SALESMAN sm ON sm.FID=o.FSALERID
WHERE s.FCreateDate BETWEEN '{}' AND '{}' 
	""".format(FQSDate, FQEDate)
	if FQOrderNo != "":
		sql += " AND sp.FOrderNo='{}'  ".format(FQOrderNo)
	if FQSellerNumber != "":
		sql += " AND sm.FNUMBER LIKE '%{}%' ".format(FQSellerNumber)
	
	objs = DBUtils.ExecuteDataSet(this.Context, sql).Tables[0].Rows
	this.Model.DeleteEntryData("FEntitySp")
	if objs.Count <= 0:
		return
	
	this.Model.BatchCreateNewEntryRow("FEntitySp", objs.Count)
	for i in range(objs.Count):
		this.Model.SetValue("FSplitNo", objs[i]["FSplitNo"], i)
		this.Model.SetValue("FSOrgId", objs[i]["FRcvOrgId"], i)
		this.Model.SetValue("FSCustId", objs[i]["FCustID"], i)
		this.Model.SetValue("FSOrderNo", objs[i]["FOrderNo"], i)
		this.Model.SetValue("FSRcvDate", objs[i]["FRcvDate"], i)
		this.Model.SetValue("FSRcvAmt", objs[i]["FSplitAmount"], i)
		this.Model.SetValue("FSplitInterID", objs[i]["FID"], i)
		this.Model.SetValue("FSplitEntryId", objs[i]["FEntryIDOP"], i)
	this.View.UpdateView("FEntitySp")


def create_settle_entry(FQOrderNo, FQSellerNumber, FQSDate, FQEDate):
	entity = this.Model.DataObject['FEntityOrd']
	this.Model.DeleteEntryData("FEntity")
	
	if entity.Count > 0:
		orderNos = ','.join([str(row['FOSourceBillNo']) for row in entity])
		sql = "EXEC CZ_Prc_EmpContractJS_TS '{}'".format(orderNos)
		objs = DBUtils.ExecuteDataSet(this.Context, sql).Tables[0].Rows
	else:
		sql = "EXEC CZ_Prc_EmpContractJS  '{}','{}','{}','{}'".format(FQSDate, FQEDate, FQOrderNo, FQSellerNumber)
		objs = DBUtils.ExecuteDataSet(this.Context, sql).Tables[0].Rows
	
	if objs.Count <= 0:
		return
	this.Model.BatchCreateNewEntryRow("FEntity", objs.Count)
	for i in range(objs.Count):
		this.Model.SetValue("F_ora_seller_text", objs[i]["sellername"], i)
		this.Model.SetValue("F_ora_sellerno_text", objs[i]["sellerno"], i)
		this.Model.SetValue("FSellerId", objs[i]["FSALERID"], i)
		this.Model.SetValue("FOrgId", objs[i]["FSALEORGID"], i)
		this.Model.SetValue("FDeptId", objs[i]["FSALEDEPTID"], i)
		this.Model.SetValue("FOrderNo", objs[i]["FBillno"], i)
		this.Model.SetValue("FCustId", objs[i]["FCustID"], i)
		this.Model.SetValue("FOrderDate", objs[i]["FOrderDate"], i)
		this.Model.SetValue("FMaterialId", objs[i]["FMaterialId"], i)
		this.Model.SetValue("FProdSeries", objs[i]["F_ORA_ASSISTANT"], i)
		this.Model.SetValue("FOrderRowAmt", objs[i]["FALLAMOUNT_LC"], i)
		this.Model.SetValue("FOrderAmt", objs[i]["FBILLALLAMOUNT_LC"], i)
		this.Model.SetValue("FHTAmountRate", objs[i]["FHTAmountRate"], i)
		this.Model.SetValue("FDownRate", objs[i]["FBDOWNPOINTS"], i)
		this.Model.SetValue("FRcvAmt", objs[i]["FSPLITAMOUNTFOR"], i)
		this.Model.SetValue("FSettleRate", objs[i]["FRATE"], i)
		this.Model.SetValue("FSettleAmt", objs[i]["FJSAmount"], i)
		this.Model.SetValue("FGreaterAmtRate", objs[i]["FGreaterAmountRate"], i)
		this.Model.SetValue("FGreaterAmt", objs[i]["FGreaterAmount"], i)
		this.Model.SetValue("FAllDKRate", objs[i]["FALLDKAmountRate"], i)
		this.Model.SetValue("FAllDKAmt", objs[i]["FALLDKAmount"], i)
		this.Model.SetValue("FLastSettleAmt", objs[i]["FoldALLDKAmount"], i)
		this.Model.SetValue("FCurrSettleAmt", objs[i]["FAllJSAmount"], i)
		this.Model.SetValue("FRealSettleAmt", objs[i]["FRealAmount"], i)
		this.Model.SetValue("FOrderInterID", objs[i]["FORDERINTERID"], i)
		this.Model.SetValue("FOrderEntryID", objs[i]["FEntryID"], i)
		this.Model.SetValue("F_ora_ProdGroupAmt", objs[i]["F_ORA_PRODGROUPAMT"], i)
		this.Model.SetValue("FLastDate", objs[i]["FLastDate"], i)
		this.Model.SetValue("FRecDate", objs[i]["FRecDate"], i)
		this.Model.SetValue("F_CZ_FBRangeAmtGp", objs[i]["F_CZ_FBRANGEAMTGP"], i)
		this.Model.SetValue("FCWRateAmount", objs[i]["FCWRateAmount"], i)
		
	this.View.UpdateView("FEntity")

