# _*_ code: utf-8 _*_
# 加班申请插件汇总


'''
计算加班时长
'''
import clr
from System import DateTime

def DataChanged(e):
	if e.Key == 'F_SDatetime' or e.Key == 'F_EDatetime':
		_FStart = this.View.BillModel.GetValue('F_SDatetime', e.Row)
		_FEnd = this.View.BillModel.GetValue('F_EDatetime', e.Row)
		if _FStart != None and _FEnd != None:
			hours = float(_FEnd.ToUniversalTime().Ticks - _FStart.ToUniversalTime().Ticks)/10000000/3600
			if hours > 0:
				floor = lambda x: int(str(x).split('.')[0]) + 0.5 if float('0.' + str(x).split('.')[1]) >= 0.5 else int(str(x).split('.')[0])
				last_hours = floor(hours - NoonCrt(_FStart, _FEnd))
				
				this.View.BillModel.SetValue('F_ora_PreTime', last_hours, e.Row)
				this.View.BillModel.SetValue('F_ora_RealTime', last_hours, e.Row)
			else:
				this.View.ShowErrMessage('加班结束时间不能小于开始时间')
				this.View.BillModel.SetValue('F_ora_PreTime', 0, e.Row)
				this.View.BillModel.SetValue('F_ora_RealTime', 0, e.Row)

def NoonCrt(start, end):
	'''中午时间校正
	'''
	crtHour = 0
	if start.CompareTo(DateTime.Parse('12:00:00')) == -1:
		if end.CompareTo(DateTime.Parse('13:00:00')) in (-1, 0):
			crtHour = float(end.ToUniversalTime().Ticks - DateTime.Parse('13:00:00').ToUniversalTime().Ticks)/10000000/3600
		else:
			crtHour = 1
	elif start.CompareTo(DateTime.Parse('12:00:00')) in (0, 1) and start.CompareTo(DateTime.Parse('13:00:00')) == -1:
		crtHour = float(DateTime.Parse('13:00:00').ToUniversalTime().Ticks - start.ToUniversalTime().Ticks)/10000000/3600

	return crtHour

# 加班类型选择后控制
def DataChanged(e):
	if e.Key == 'FType':
		# 1=>工作日加班;2=>节假日加班;3=>双休日加班
		# 支付方式：1付费；2调休
		ftype = this.View.BillModel.GetValue('FType')
		if ftype in ('1', '3'):
			this.View.BillModel.SetValue('FPayType', 2)
			this.View.GetControl("FPayType").SetCustomPropertyValue("disabled", True)
		elif ftype == '2':
			this.View.GetControl("FPayType").SetCustomPropertyValue("disabled", False)

