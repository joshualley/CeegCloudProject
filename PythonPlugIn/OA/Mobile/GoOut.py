# _*_ code: utf-8 _*_
# 出门证插件汇总


def AfterBindData(e):
	# 初始化时隐藏快递单号
	# 快递单号
	expressNum = 'FExpressNum'
	red_star = 'F_ora_Lable3238'
	expressNumLabel = 'F_ora_Lable148'
	this.View.GetControl(expressNum).Visible = False
	this.View.GetControl(expressNumLabel).Visible = False
	this.View.GetControl(red_star).Visible = False
	
def DataChanged(e):
	'''
	根据运输方式，控制字段的显示和隐藏情况
	'''
	# 运输方式标识
	shipType = 'FShipType'
	if e.Key == shipType:
		# 车牌，驾驶员
		plate_driverLayout = 'F_ora_FlowLayout391'
		# 快递单号
		expressNum = 'FExpressNum'
		red_star = 'F_ora_Lable3238'
		expressNumLabel = 'F_ora_Lable148'
		value = this.View.BillModel.GetValue('FShipType')
		
		#this.View.ShowMessage(value)
		if value == '1':
			# 运输，隐藏快递单号
			this.View.GetControl(plate_driverLayout).Visible = True
			this.View.GetControl(expressNum).Visible = False
			this.View.GetControl(expressNumLabel).Visible = False
			this.View.GetControl(red_star).Visible = False
		elif value == '2':
			# 快递，隐藏车牌，驾驶员
			this.View.GetControl(plate_driverLayout).Visible = False
			this.View.GetControl(expressNum).Visible = True
			this.View.GetControl(expressNumLabel).Visible = True
			this.View.GetControl(red_star).Visible = True
		elif value == '3':
			# 个人携带，吟唱单号，车牌，驾驶员
			this.View.GetControl(plate_driverLayout).Visible = False
			this.View.GetControl(expressNum).Visible = False
			this.View.GetControl(expressNumLabel).Visible = False
			this.View.GetControl(red_star).Visible = False


'''
验证字段是否必录(New)
'''
def BeforeDoOperation(e):
	_opKey = e.Operation.FormOperation.Operation
	#this.View.ShowMessage(_opKey)
	if _opKey == 'Submit':
		if Check():
			e.Cancel = True

def Check():
	# 动态验证
	if this.View.BillModel.GetValue('FShipType') == '2':
		# 运输方式 为 快递
		expNumValue = this.View.BillModel.GetValue('FExpNumber')
		# 单号为空
		if expNumValue == '' or expNumValue == None:
			this.View.ShowErrMessage('单号是必录字段！')
			return True
	elif this.View.BillModel.GetValue('FShipType') == '1':
		# 运输方式 为 运输
		driver = this.View.BillModel.GetValue('FDriver')
		plateNum = this.View.BillModel.GetValue('FPlateNum')
		if driver == '' or driver == None:
			this.View.ShowErrMessage('驾驶员是必录字段！')
			return True
		elif plateNum == '' or plateNum == None:
			this.View.ShowErrMessage('车牌号是必录字段！')
			return True
	return False
