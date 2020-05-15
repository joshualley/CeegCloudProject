# _*_ code: utf-8 _*_
# 请假申请PC插件汇总

'''
控制生育日期字段显示隐藏及显示时必录验证
'''
first = True
def AfterBindData(e):
	if str(this.Context.ClientType) != 'Mobile':
		global first
		if first:
			first = False
			this.View.GetControl('FDeliveryDate').Visible = False


def DataChanged(e):
	if str(this.Context.ClientType) != 'Mobile':
		if e.Key == 'FLeaveType':
			rowCount = this.View.Model.GetEntryRowCount('FEntity')
			isDisplay = False
			for i in range(rowCount):
				_FLeaveType = this.View.Model.GetValue('FLeaveType', i)
				if int(_FLeaveType) in (11,12,13,14,15,16):
					isDisplay = True
			if isDisplay:
				this.View.GetControl('FDeliveryDate').Visible = True
			else:
				this.View.GetControl('FDeliveryDate').Visible = False

def BeforeDoOperation(e):
	def Check():
		# 动态验证
		if this.View.GetControl('FDeliveryDate').Visible and this.View.Model.GetValue('FDeliveryDate') == None:
			this.View.ShowMessage('生育日期是必录字段！')
			return True
		return False
	if str(this.Context.ClientType) != 'Mobile':
		_opKey = e.Operation.FormOperation.Operation
		if _opKey == 'Submit':
			if Check():
				e.Cancel = True