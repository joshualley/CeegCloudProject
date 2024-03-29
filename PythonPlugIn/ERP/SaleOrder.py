# 销售订单，替换销售员及其组织部门，替换客户
import clr
clr.AddReference('Kingdee.BOS.App')
from Kingdee.BOS.App.Data import *

def AfterBindData(e):
	if str(this.Context.ClientType) == 'Mobile':
		return
	FDocumentStatus = str(this.View.Model.GetValue("FDocumentStatus"))
	if FDocumentStatus == "Z":
		AlterCustAndSalerByOrg()
	check_permission()

def BarItemClick(e):
	key = str(e.BarItemKey).upper()
	FDocumentStatus = str(this.View.Model.GetValue("FDocumentStatus"))
	if FDocumentStatus == "Z":
		this.View.ShowMessage('单据未保存!')
		return
	fid = str(this.Model.DataObject['Id'])
	if key == "ORA_CLOSE":
		sql = "/*dialect*/update T_SAL_ORDER set FCloseStatus='B' where fid='{}'".format(fid)
		DBUtils.Execute(this.Context, sql)
		this.View.Refresh()
	elif key == "ORA_UNCLOSE":
		sql = "/*dialect*/update T_SAL_ORDER set FCloseStatus='A' where fid='{}'".format(fid)
		DBUtils.Execute(this.Context, sql)
		this.View.Refresh()
	elif key == "ORA_AUDIT":
		sql = "/*dialect*/update T_SAL_ORDER set FDocumentStatus='C' where fid='{}'".format(fid)
		DBUtils.Execute(this.Context, sql)
		this.View.Refresh()
	elif key == "ORA_UNAUDIT":
		sql = "/*dialect*/update T_SAL_ORDER set FDocumentStatus='D' where fid='{}'".format(fid)
		DBUtils.Execute(this.Context, sql)
		this.View.Refresh()

def check_permission():
	uid = str(this.Context.UserId)
	sql = """select FNumber from t_SEC_role r 
		inner join T_SEC_ROLEUSER ru on ru.FRoleId=r.FRoleId
		where r.FNumber='999' and ru.FUserId={}
		""".format(uid)
	results = DBUtils.ExecuteDataSet(this.Context, sql).Tables[0].Rows
	if results.Count > 0:
		return
	this.View.GetMainBarItem("ora_tbSplitButton").Visible = False

def DataChanged(e):
	if e.Key == "FSalerId":
		FSalerId = str(e.NewValue)
		sql = "EXEC proc_czly_GetOrgDeptBySalemanId @SmId='{}'".format(FSalerId)
		results = DBUtils.ExecuteDataSet(this.Context, sql).Tables[0].Rows
		if results.Count > 0:
			this.Model.SetValue("FSaleDeptId", str(results[0]["FBizDeptID"]))
			this.Model.SetValue("FSaleDeptId", str(results[0]["FBizDeptID"]), -1)

def BeforeDoOperation(e):
	_opKey = e.Operation.FormOperation.Operation
	if _opKey == 'Save':
		SumAmtAndFilterRejectedRow()

# 订单明细当前点击行
currEntryRow = 0
def EntityRowClick(e):
	if e.Key == "FSaleOrderEntry".upper():
		global currEntryRow
		currEntryRow = e.Row


def AfterEntryBarItemClick(e):
	key = e.BarItemKey.upper()
	FDocumentStatus = str(this.View.Model.GetValue("FDocumentStatus"))
	if FDocumentStatus != 'Z':
		if key == 'ORA_TBSETFREE': # 设为赠品 ora_tbSetFree
			entityRow = this.Model.DataObject['SaleOrderEntry'][currEntryRow]
			if entityRow is None:
				this.View.ShowMessage('请选择订单行！')
				return
			eid = str(entityRow['Id'])
			sql = "update T_SAL_ORDERENTRY_F set FIsFree=1 where FEntryId={}".format(eid)
			DBUtils.Execute(this.Context, sql)
			this.View.Refresh()
		elif key == 'ORA_TBCANCELFREE': # 取消赠品 ora_tbCancelFree
			entityRow = this.Model.DataObject['SaleOrderEntry'][currEntryRow]
			if entityRow is None:
				this.View.ShowMessage('请选择订单行！')
				return
			eid = str(entityRow['Id'])
			sql = "update T_SAL_ORDERENTRY_F set FIsFree=0 where FEntryId={}".format(eid)
			DBUtils.Execute(this.Context, sql)
			this.View.Refresh()
	if key == 'ORA_TBCUTPAYSMT': #ora_tbCutpaySmt
		global currEntryRow
		param = DynamicFormShowParameter()
		param.FormId = "ora_Sal_CutpaySmtRgst"
		param.OpenStyle.ShowType = ShowType.Modal
		param.ParentPageId = this.View.PageId

		entityRow = this.Model.DataObject['SaleOrderEntry'][currEntryRow]
		#entityRow = clr.Reference[System.Single]()
		#row = 0
		#this.Model.TryGetEntryCurrentRow('SaleOrderEntry', entityRow, row)
		if entityRow is None:
			this.View.ShowMessage('请选择订单行！')
			return
		eid = str(entityRow['Id'])
		row = str(currEntryRow+1)
		param.CustomParams.Add('eid', eid)
		param.CustomParams.Add('row', row)

		this.View.ShowForm(param, CutpaySmtModifyCallback)

def CutpaySmtModifyCallback(FormResult):
	"""扣款报销修改后，刷新表单"""
	this.View.Refresh()


def SumAmtAndFilterRejectedRow():
	"""计算销售订单，并过滤掉拒绝的行"""
	entity = this.Model.DataObject['SaleOrderEntry']
	amts = [[
			float(entity[i]['AllAmount']), float(entity[i]['TaxAmount']), 
			float(entity[i]['AllAmount_LC']), float(entity[i]['TaxAmount_LC'])]
		for i in range(entity.Count) 
		if entity[i]['F_ora_Jjyy'] is None or str(entity[i]['F_ora_Jjyy']).strip()==''
	]
	#t = [entity[i]['F_ora_Jjyy'] for i in range(entity.Count)]
	#this.View.ShowMessage(str(t))
	allAmt = sum([i[0] for i in amts])
	taxAmt = sum([i[1] for i in amts])
	amt = allAmt - taxAmt
	allAmtLc = sum([i[2] for i in amts])
	taxAmtLc = sum([i[3] for i in amts])
	amtLc = allAmtLc - taxAmtLc
	this.Model.SetValue("FBillAllAmount", allAmt)
	this.Model.SetValue("FBillTaxAmount", taxAmt)
	this.Model.SetValue("FBillAmount", amt)
	this.Model.SetValue("FBillAllAmount_LC", allAmtLc)
	this.Model.SetValue("FBillTaxAmount_LC", taxAmtLc)
	this.Model.SetValue("FBillAmount_LC", amtLc)


def AlterCustAndSalerByOrg():
	"""替换客户，销售员"""
	FSaleOrgId = 0 if this.Model.GetValue("FSaleOrgId") == None else str(this.Model.GetValue("FSaleOrgId")["Id"])
	if FSaleOrgId == 0:
		return
	FSrcType = "" if this.Model.GetValue("FSrcType", 0) == None else str(this.Model.GetValue("FSrcType", 0))
	FSrcBillNo = "" if this.Model.GetValue("FSrcBillNo", 0) == None else str(this.Model.GetValue("FSrcBillNo", 0))
	FSalerId, FCustId = 0, 0
	#查询是否为销售合同
	if FSrcType == "ora_CRM_Contract":
		sql = "SELECT FSaler,FCustName FROM ora_CRM_Contract WHERE FBILLNO='{}'".format(FSrcBillNo)
		srcBill = DBUtils.ExecuteDataSet(this.Context, sql).Tables[0].Rows
		if srcBill.Count > 0:
			FSalerId = srcBill[0]["FSaler"]
			FCustId = srcBill[0]["FCustName"]
	#内部合同
	elif FSrcType == "ora_CRM_InnerContract":
		sql = "SELECT FSaler,FCustName FROM ora_CRM_InnerContract WHERE FBILLNO='{}'".format(FSrcBillNo)
		srcBill = DBUtils.ExecuteDataSet(this.Context, sql).Tables[0].Rows
		if srcBill.Count > 0:
			FSalerId = srcBill[0]["FSaler"]
			FCustId = srcBill[0]["FCustName"]
	#维修合同
	elif FSrcType == "ora_CRM_MantainContract":
		sql = "SELECT FSalerID,FCustID FROM ora_CRM_MtnCont WHERE FBILLNO='{}'".format(FSrcBillNo)
		srcBill = DBUtils.ExecuteDataSet(this.Context, sql).Tables[0].Rows
		if srcBill.Count > 0:
			FSalerId = srcBill[0]["FSalerID"]
			FCustId = srcBill[0]["FCustID"]
	elif FSrcType == "SAL_SaleOrder":
		sql = "SELECT FSalerId,FCustId FROM T_SAL_ORDER WHERE FBILLNO='{}'".format(FSrcBillNo)
		srcBill = DBUtils.ExecuteDataSet(this.Context, sql).Tables[0].Rows
		if srcBill.Count > 0:
			FSalerId = srcBill[0]["FSalerID"]
			FCustId = 102450
	else:
		return

	if FSalerId == 0 or FCustId == 0:
		this.Model.SetValue("FSalerId", FSalerId)
		this.Model.SetValue("FCustId", FCustId)
		this.View.ShowErrMessage("销售员或客户不能为空值！")
		return

	# 获取客户内码
	sql = """select FCUSTID from T_BD_CUSTOMER where FUSEORGID='{}' 
		and FNUMBER=(select FNUMBER from T_BD_CUSTOMER where FCUSTID='{}')""".format(FSaleOrgId, FCustId)
	results = DBUtils.ExecuteDataSet(this.Context, sql).Tables[0].Rows
	if results.Count > 0:
		this.Model.SetValue("FCustId", str(results[0]["FCUSTID"]))
	# 获取销售员内码
	sql = """SELECT FID FROM V_BD_SALESMAN WHERE FBIZORGID='{}' AND 
		FNUMBER=(SELECT FNUMBER FROM V_BD_SALESMAN WHERE FID='{}')""".format(FSaleOrgId, FSalerId)
	results = DBUtils.ExecuteDataSet(this.Context, sql).Tables[0].Rows
	if results.Count > 0:
		this.Model.SetValue("FSalerId", str(results[0]["FID"]))
		# 获取办事处
		sql = "EXEC proc_czly_GetOrgDeptBySalemanId @SmId='{}'".format(results[0]["FID"])
		results = DBUtils.ExecuteDataSet(this.Context, sql).Tables[0].Rows
		if results.Count > 0:
			this.Model.SetValue("FSaleDeptId", str(results[0]["FBizDeptID"]))

