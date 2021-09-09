import clr
clr.AddReference('Kingdee.BOS')
clr.AddReference('Kingdee.BOS.Core')
clr.AddReference('Kingdee.BOS.App')
clr.AddReference('System.Data')

from Kingdee.BOS import *
from Kingdee.BOS.Core import *
from Kingdee.BOS.Core.Bill import *
from Kingdee.BOS.Core.Metadata import *
from Kingdee.BOS.Core.DynamicForm.PlugIn import *
from Kingdee.BOS.Core.DynamicForm.PlugIn.ControlModel import *
from System import *
from Kingdee.BOS.App.Data import *

 
'''
数据与View绑定后触发
'''
def AfterBindData(e):
	############################### FUNCTION SECTION ######################################
	'''由用户ID获取员工信息'''
	def CZDB_GetLoginUser2Emp(_userId):
		sql = 'exec proc_czty_GetLoginUser2Emp @FUserID=' + str(_userId)
		try:
			dt = DBUtils.ExecuteDataSet(this.Context, sql).Tables[0]
			if dt.Rows.Count > 0:
				return dt
			else:
				return '-1'
		except Exception as e:
			return '-1'
	############################### FUNCTION SECTION ######################################
	
	mobileEntry = this.View.GetControl("F_ora_MobileProxyEntryEntity") #移动代理分录
	
	if this.View.OpenParameter.Status != "VIEW":
		# 表体可直接编辑
		if mobileEntry != None:
			mobileEntry.SetCustomPropertyValue("listEditable", True)

		# 默认申请人与制单人一致，并带出申请人相关信息
		_userId = this.Context.UserId
		#dt = CZDB_GetLoginUser2Emp('100229') #王再亮测试
		dt = CZDB_GetLoginUser2Emp(_userId)
		if dt != '-1':
			_FEmpID = dt.Rows[0]['FEmpID']
			_FDeptID = dt.Rows[0]['FDeptID']
			_FPostID = dt.Rows[0]['FPostID']
			_FRankID = dt.Rows[0]['FRankID']
			_FMobile = dt.Rows[0]['FMobile']
			# 下述标识须根据各自表单进行判定
			this.View.BillModel.SetItemValueByID('FApplyID', _FEmpID, -1)
			this.View.BillModel.SetItemValueByID('FDeptID', _FDeptID, -1)
			this.View.BillModel.SetItemValueByID('FPost', _FPostID, -1)
			this.View.BillModel.SetItemValueByID('FLevel', _FRankID, -1)
			this.View.BillModel.SetValue('FTel', _FMobile)
	else :
		if mobileEntry != None:
			mobileEntry.SetCustomPropertyValue("listEditable", False)


'''
执行提交、保存、审核等操作前执行，主要进行各种验证
'''
def BeforeDoOperation(e):
	############################### FUNCTION SECTION ######################################
	def Check():
		# 确定PC表单中单据体标识是否为 FEntity， 表体必录验证
		if this.View.GetControl("F_ora_MobileProxyEntryEntity") != None and this.View.BillModel.GetEntryRowCount('FEntity') == 0:
			this.View.ShowErrMessage('请在明细信息中输入内容')
			return True
		return CustomeVerification()

	def CustomeVerification():
		# 这里写自定义验证条件
		''' 
		# *印章*
		# 根据条件验证字段 示例
		if this.View.BillModel.GetValue('FApplyType') == '2':
			# 申请类别 为 外带
			planOffTime = this.View.BillModel.GetValue('FPlanOffTime')
			planBackTime = this.View.BillModel.GetValue('FPlanBackTime')
			if planOffTime == '' or planOffTime == None:
				this.View.ShowErrMessage('字段“计划带离时间”是必填项')
				return True
			elif planBackTime == '' or planBackTime == None:
				this.View.ShowErrMessage('字段“计划归还时间”是必填项')
				return True
		'''

		return False
	############################### FUNCTION SECTION ######################################

	_opKey = e.Operation.FormOperation.Operation
	if _opKey == 'Submit':
		if Check():
			e.Cancel = True


'''
执行提交、保存、审核等操作后执行
'''
def AfterDoOperation(e):
	pass

'''
数据发生变化时触发
'''
def DataChanged(e):
	############################### FUNCTION SECTION ######################################
	'''表体中预估费用更改时汇总到表头'''
	def SummaryAmount(amount_key, total_amount_key):
		count = this.View.BillModel.GetEntryRowCount('FEntity')
		amount = sum([float(this.View.BillModel.GetValue(amount_key, row)) for row in range(count)])
		this.View.BillModel.SetValue(total_amount_key, amount)
	
	'''金额转为大写'''
	def ConvertNumToChinese(totalPrice):
		dictChinese = [u'零',u'壹',u'贰',u'叁',u'肆',u'伍',u'陆',u'柒',u'捌',u'玖']
		unitChinese = [u'',u'拾',u'佰',u'仟','',u'拾',u'佰',u'仟']
		#将整数部分和小数部分区分开
		partA = int(totalPrice)
		partB = round(totalPrice-partA, 2)
		strPartA = str(partA)
		strPartB = ''
		if partB != 0:
			strPartB = str(partB)[2:]

		singleNum = []
		if len(strPartA) != 0:
			i = 0
			while i < len(strPartA):
				singleNum.append(strPartA[i])
				i = i+1
		#将整数部分先压再出，因为可以从后向前处理，好判断位数 
		tnumChinesePartA = []
		numChinesePartA = []
		j = 0
		bef = '0';
		if len(strPartA) != 0:
			while j < len(strPartA) :
				curr = singleNum.pop()
				if curr == '0' and bef !='0':
					tnumChinesePartA.append(dictChinese[0])
					bef = curr
				if curr != '0':
					tnumChinesePartA.append(unitChinese[j])
					tnumChinesePartA.append(dictChinese[int(curr)])
					bef = curr
				f j == 3:
					tnumChinesePartA.append(u'萬')
					bef = '0'
				j = j+1

			for i in range(len(tnumChinesePartA)):
				numChinesePartA.append(tnumChinesePartA.pop())
		A = ''      
		for i in numChinesePartA:
			A = A+i
		#小数部分很简单，只要判断下角是否为零
		B = ''
		if len(strPartB) == 1:
			B = dictChinese[int(strPartB[0])] + u'角'
		if len(strPartB) == 2 and strPartB[0] != '0':
			B = dictChinese[int(strPartB[0])] + u'角' + dictChinese[int(strPartB[1])] + u'分'
		if len(strPartB) == 2 and strPartB[0] == '0':
			B = dictChinese[int(strPartB[0])] + dictChinese[int(strPartB[1])] + u'分'

		if len(strPartB) == 0:
			S = A + u'元'
		if len(strPartB)!= 0:
			S = A + u'元' + B
		return S
	
	'''请假天数计算，调用存储过程实现'''
	def CZDB_GetLeaveWorkDaysAP(_FOrgID, _FBegDt, _FBegDtAP, _FEndDt, _FEndDtAP):
		_LeaveWDDays = "0"
		sql = "exec proc_czty_LeaveWorkDaysAP @FOrgID='" + str(_FOrgID) + "',@FBD='" + str(_FBegDt) + "',@FBD_AP='" + str(_FBegDtAP) + "',@FED='" + str(_FEndDt) + "',@FED_AP='" + str(_FEndDtAP) + "'";
		dt = DBUtils.ExecuteDataSet(this.Context, sql).Tables[0]
		if dt.Rows.Count > 0:
			_LeaveWDDays = dt.Rows[0]["lwds"]

		return _LeaveWDDays
	############################### FUNCTION SECTION ######################################
	# *印章*
	# 根据条件隐藏相关字段
	if e.Key == 'F_ora_MobileProxyField6':
		applyType = this.View.BillModel.GetValue("FApplyType")
		if applyType == '1':
			# 申请类别为 现用
			this.View.GetControl('F_ora_FlowLayout_PlanOffTime').Visible = False
			this.View.GetControl('F_ora_FlowLayout_PlanBackTime').Visible = False
			this.View.GetControl('F_ora_FlowLayout_RealOffTime').Visible = False
			this.View.GetControl('F_ora_FlowLayout_RealBackTime').Visible = False

		elif applyType == '2':
			# 申请类别为 外借
			this.View.GetControl('F_ora_FlowLayout_PlanOffTime').Visible = True
			this.View.GetControl('F_ora_FlowLayout_PlanBackTime').Visible = True
			this.View.GetControl('F_ora_FlowLayout_RealOffTime').Visible = True
			this.View.GetControl('F_ora_FlowLayout_RealBackTime').Visible = True
	
	if e.Key == 'FAmount': # FAmount 为预计金额代理字段标识
		# 预计费用
		PreMoney = float(this.View.BillModel.GetValue('F_ora_Amount'))
		PreMoney = convertNumToChinese(PreMoney)
		this.View.BillModel.SetValue('F_ora_AmountDisplay', PreMoney)

	# *请假申请*
	# 计算请假天数
	if e.Key == 'FStartTime' or e.Key == 'FStartFrame' or e.Key == 'FEndTime' or e.Key == 'FEndFrame':
		orgId = this.View.BillModel.GetValue('FOrgID')
		st = this.View.BillModel.GetValue('FStartDate', e.Row)
		sf = this.View.BillModel.GetValue('FStartTimeFrame', e.Row)
		et = this.View.BillModel.GetValue('FEndDate', e.Row)
		ef = this.View.BillModel.GetValue('FEndTimeFrame', e.Row)

		day = CZDB_GetLeaveWorkDaysAP(orgId, st, sf, et, ef)
		this.View.BillModel.SetValue('FDayNum', day, e.Row)


'''
按钮事件
'''
def ButtonClick(e):
	if e.Key == 'FNEWROW':
		# *具有分录的页面*
		# 新增明细不弹出页面
		this.View.BillModel.BeginIniti()
		this.View.BillModel.BatchCreateNewEntryRow('FEntity', 1)
		this.View.BillModel.EndIniti()
		this.View.UpdateView()
