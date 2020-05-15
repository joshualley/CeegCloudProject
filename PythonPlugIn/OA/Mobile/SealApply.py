# _*_ code: utf-8 _*_
# 印章使用申请插件汇总



import clr
from System import DateTime


first = True
def AfterBindData(e):
	global first
	if first:
		first = False
		this.View.GetControl('F_ora_FlowLayout_Datetime').Visible = False
		this.View.GetControl('F_ora_FlowLayout_RealOffTime').Visible = False
		this.View.GetControl('F_ora_FlowLayout_RealBackTime').Visible = False


'''
根据申请类别隐藏or显示相关字段 && 控制时间先后
*********时间设置有问题***********
'''
def  DataChanged(e):
	if e.Key == 'FApplyType':
		applyType = this.View.BillModel.GetValue("FApplyType")
		if applyType == '1':
			# 申请类别为 现用
			this.View.GetControl('F_ora_FlowLayout_PlanOffTime').Visible = False
			this.View.GetControl('F_ora_FlowLayout_PlanBackTime').Visible = False
			this.View.GetControl('F_ora_FlowLayout_Datetime').Visible = True

		elif applyType == '2':
			# 申请类别为 外借
			this.View.GetControl('F_ora_FlowLayout_PlanOffTime').Visible = True
			this.View.GetControl('F_ora_FlowLayout_PlanBackTime').Visible = True
			this.View.GetControl('F_ora_FlowLayout_Datetime').Visible = False
			

	FCreateDate1 = this.View.BillModel.GetValue('FCreateDate1')
	Now = DateTime.Parse(str(FCreateDate1))
	# 控制时间显示
	FDatetime = this.View.BillModel.GetValue('FDatetime')
	FPlanOffTime = this.View.BillModel.GetValue('FPlanOffTime')
	FPlanBackTime = this.View.BillModel.GetValue('FPlanBackTime')
	FRealOffTime = this.View.BillModel.GetValue('FRealOffTime')
	FRealBackTime = this.View.BillModel.GetValue('FRealBackTime')
	# 印章使用时间
	if e.Key == 'FDatetime':
		if DateTime.Parse(str(FDatetime)).CompareTo(Now) == -1:
			this.View.BillModel.SetValue('FDatetime', Now.ToString())
			this.View.ShowMessage('印章使用时间不能早于当前时间！')
	# 计划带离时间
	if e.Key == 'FPlanOffTime':
		if DateTime.Parse(str(FPlanOffTime)).CompareTo(Now) == -1:
			this.View.BillModel.SetValue('FPlanOffTime', Now.ToString())
			this.View.ShowMessage('计划带离时间不能小于当前时间！')
		elif FPlanBackTime != None and DateTime.Parse(str(FPlanOffTime)).CompareTo(DateTime.Parse(str(FPlanBackTime))) == 1:
			this.View.BillModel.SetValue('FPlanOffTime', str(FPlanBackTime))
			this.View.ShowMessage('计划带离时间不能大于计划归还时间！')
	# 计划归还时间
	if e.Key == 'FPlanBackTime':
		if DateTime.Parse(str(FPlanBackTime)).CompareTo(Now) == -1:
			this.View.BillModel.SetValue('FPlanBackTime', str(Now))
			this.View.ShowMessage('计划归还时间不能早于当前时间！')
		elif FPlanOffTime != None and DateTime.Parse(str(FPlanOffTime)).CompareTo(DateTime.Parse(str(FPlanBackTime))) == 1:
			this.View.BillModel.SetValue('FPlanBackTime', str(FPlanOffTime))
			this.View.ShowMessage('计划归还时间不能小于计划带离时间！')
	# 实际带离时间
	if e.Key == 'FRealOffTime':
		if DateTime.Parse(str(FRealOffTime)).CompareTo(Now) == -1:
			this.View.BillModel.SetValue('FRealOffTime', str(Now))
			this.View.ShowMessage('实际带离时间不能小于申请时间！')
		elif FRealBackTime != None and DateTime.Parse(str(FRealOffTime)).CompareTo(DateTime.Parse(str(FRealBackTime))) == 1:
			this.View.BillModel.SetValue('FRealOffTime', str(FRealBackTime))
			this.View.ShowMessage('实际带离时间不能大于实际归还时间！')
	# 实际归还时间
	if e.Key == 'FRealBackTime':
		if DateTime.Parse(str(FRealBackTime)).CompareTo(Now) == -1:
			this.View.BillModel.SetValue('FRealBackTime', str(Now))
			this.View.ShowMessage('实际带离时间不能小于申请时间！')
		elif FRealOffTime != None and DateTime.Parse(str(FRealOffTime)).CompareTo(DateTime.Parse(str(FRealBackTime))) == 1:
			this.View.BillModel.SetValue('FRealBackTime', str(FRealOffTime))
			this.View.ShowMessage('实际带离时间不能大于实际归还时间！')


def BeforeDoOperation(e):
	'''
	印章
	申请类别 为 外带时
	计划带离时间及计划归还时间必录
	'''
	def Check():
		# 动态验证
		if this.View.BillModel.GetValue('FApplyType') == '2':
			# 申请类别 为 外带
			planOffTime = this.View.BillModel.GetValue('FPlanOffTime')
			planBackTime = this.View.BillModel.GetValue('FPlanBackTime')
			if planOffTime == '' or planOffTime == None:
				this.View.ShowErrMessage('计划带离时间是必录字段!')
				return True
			elif planBackTime == '' or planBackTime == None:
				this.View.ShowErrMessage('计划归还时间是必录字段!')
				return True
		elif this.View.BillModel.GetValue('FApplyType') == '1':
			FDatetime = this.View.BillModel.GetValue('FDatetime')
			if FDatetime == None:
				this.View.ShowErrMessage('用章时间是必录字段!')
				return True
		return False

	_opKey = e.Operation.FormOperation.Operation
	if _opKey == 'Submit':
		if Check():
			e.Cancel = True