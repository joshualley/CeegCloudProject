"""
业务费结算之销售订单选单列表
"""

import clr
clr.AddReference('Kingdee.BOS.App')
from Kingdee.BOS.App.Data import *
from System import DateTime

IsReturnData = False

def AfterBindData(e):
	now = DateTime.Now;
	year = now.Year - 1 if now.Month == 1 else now.Year
	month = 12 if now.Month == 1 else now.Month - 1
	sDt = "{}-{}-01".format(year, month)
	this.Model.SetValue("FQSDate", sDt)
	this.Model.SetValue("FQEDate", str(now))
	this.View.UpdateView("FQSDate")
	this.View.UpdateView("FQEDate")
	query()
	

def query():
	FQOrderNo = '' if this.Model.GetValue('FQOrderNo') is None else str(this.Model.GetValue('FQOrderNo'))
	FQSellerNumber = '' if this.Model.GetValue('FQSellerId') is None else str(this.Model.GetValue('FQSellerId')['Number'])
	FQCustNumber = '' if this.Model.GetValue('FQCustId') is None else str(this.Model.GetValue('FQCustId')['Number'])
	FQSDate = '' if this.Model.GetValue('FQSDate') is None else str(this.Model.GetValue('FQSDate'))
	FQEDate = '' if this.Model.GetValue('FQEDate') is None else str(this.Model.GetValue('FQEDate'))
	if FQSDate == '' or FQEDate == '':
		this.View.ShowWarnningMessage('请输入查询时间！')
		return

	sql = '''SELECT 
    o.FID, FBillNo FOrderNo, o.FSalerId, o.FDate, o.FCustId, 
    SUM(FAllAmount_LC) FOrderAmt, SUM(F_CZ_FBPAmt) FBaseAmt
FROM T_SAL_ORDER o
INNER JOIN T_SAL_ORDERENTRY oe ON o.FID=oe.FID
INNER JOIN T_SAL_ORDERENTRY_F oef ON o.FID=oef.FID
INNER JOIN V_BD_SALESMAN sm ON sm.FID=o.FSalerId
INNER JOIN T_BD_CUSTOMER c ON c.FCustID=o.FCustId
WHERE ISNULL(F_ora_Jjyy, '')=''
AND o.FBillNo LIKE '{}%'
AND o.FDate BETWEEN '{}' AND '{}' 
	'''.format(FQOrderNo, FQSDate, FQEDate)
	if FQSellerNumber != '':
		sql += "AND sm.FNumber='{}' ".format(FQSellerNumber)
	if FQCustNumber != '':
		sql += "AND c.FNumber='{}' ".format(FQCustNumber)
	sql += " GROUP BY o.FID, o.FBillNo, o.FSalerId, o.FDate, o.FCustId"
	
	this.Model.DeleteEntryData("FEntity")
	objs = DBUtils.ExecuteDataSet(this.Context, sql).Tables[0].Rows
	if objs.Count <= 0:
		return
	this.Model.BatchCreateNewEntryRow("FEntity", objs.Count)
	for i in range(objs.Count):
		this.Model.SetValue("FOrderId", objs[i]["FID"], i)
		this.Model.SetValue("FOrderNo", objs[i]["FOrderNo"], i)
		this.Model.SetValue("FSalerId", objs[i]["FSalerId"], i)
		this.Model.SetValue("FDate", objs[i]["FDate"], i)
		this.Model.SetValue("FCustId", objs[i]["FCustId"], i)
		this.Model.SetValue("FOrderAmt", objs[i]["FOrderAmt"], i)
		this.Model.SetValue("FBaseAmt", objs[i]["FBaseAmt"], i)

	this.View.UpdateView("FEntity")

def AfterButtonClick(e):
	key = e.Key.upper()
	if key == "FQUERYBTN":
		query()

def BarItemClick(e):
	global IsReturnData
	key = e.BarItemKey.upper()
	if key == 'ORA_RTDATA':
		entity = this.Model.DataObject['FEntity']
		dataStr = ','.join([str(row['FOrderId']) for row in entity if str(row['FIsChoosed'])=='True'])
		this.View.ShowMessage(dataStr)
		this.View.ReturnToParentWindow(dataStr)
		IsReturnData = True
		this.View.Close()


def BeforeClose(e):
	global IsReturnData
	if IsReturnData == False:
		this.View.ReturnToParentWindow(None)
