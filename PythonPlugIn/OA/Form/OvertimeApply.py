# _*_ code: utf-8 _*_
# 加班申请插件汇总

'''
计算加班时长
'''
import clr
from System import DateTime
#clr.AddReference('Kingdee.BOS.App')
#from Kingdee.BOS.App.Data import *

# 加班类型选择后控制
def AfterBindData(e):
	if str(this.Context.ClientType) != 'Mobile':
		this.View.StyleManager.SetEnabled("FPayType", "", False)



def DataChanged(e):
	if str(this.Context.ClientType) != 'Mobile':
		if e.Key == 'FType':
			# 1=>工作日加班;2=>节假日加班;3=>双休日加班
			# 支付方式：1付费；2调休
			ftype = this.View.Model.GetValue('FType')
			if ftype in ('1', '3'):
				this.View.Model.SetValue('FPayType', 2)
				this.View.StyleManager.SetEnabled("FPayType", "", False)
			elif ftype == '2':
				this.View.StyleManager.SetEnabled("FPayType", "", True)

		# 加班时间选择时提醒
		if e.Key == 'F_SDatetime' or e.Key == 'F_EDatetime':
			_FStart = this.View.Model.GetValue('F_SDatetime', e.Row)
			_FEnd = this.View.Model.GetValue('F_EDatetime', e.Row)
			if _FStart != None and _FEnd != None:

				#sql = 'select * from T_ENG_WorkCalData where FDay='.format()
				#DBUtils.ExecuteDataSet(this.Context, sql).Tables[0]

				## 如果开始在17:30之前,结束在18：00后
				#if _FStart.CompareTo(DateTime.Parse('17:30:00')) in (-1, 0) and _FEnd.CompareTo(DateTime.Parse('18:00:00')) in (0, 1):
				#	_FStart = DateTime.Parse('18:00:00')

				hours = float(_FEnd.ToUniversalTime().Ticks - _FStart.ToUniversalTime().Ticks)/10000000/3600
				if hours > 0:
					floor = lambda x: int(str(x).split('.')[0]) + 0.5 if float('0.' + str(x).split('.')[1]) >= 0.5 else int(str(x).split('.')[0])
					last_hours = floor(hours - NoonCrt(_FStart, _FEnd))
					
					this.View.Model.SetValue('F_ora_PreTime', last_hours, e.Row)
					this.View.Model.SetValue('F_ora_RealTime', last_hours, e.Row)
				else:
					this.View.ShowMessage('加班结束日期不能小于开始日期')
					#this.View.BillModel.SetValue('F_ora_EndTime', _FStart, e.Row)
					this.View.Model.SetValue('F_ora_PreTime', 0, e.Row)
					this.View.Model.SetValue('F_ora_RealTime', 0, e.Row)


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
