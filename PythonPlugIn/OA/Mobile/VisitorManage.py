# _*_ code: utf-8 _*_
# 访客管理插件汇总

import clr
clr.AddReference('Kingdee.BOS')
clr.AddReference('Kingdee.BOS.Core')
clr.AddReference('Kingdee.BOS.App')

from Kingdee.BOS import *
from Kingdee.BOS.Core import *
from Kingdee.BOS.Core.Bill import *
from Kingdee.BOS.Core.DynamicForm.PlugIn import *
from Kingdee.BOS.Core.DynamicForm.PlugIn.ControlModel import *
from System import *
from Kingdee.BOS.App.Data import *

'''
初始化时隐藏字段
'''
VisitorFirst = True
def AfterBindData(e):
	global VisitorFirst
	if VisitorFirst:
		this.View.GetControl('FSubtitleFlow').Visible = False

		this.View.GetControl('FMettingStartFlow').Visible = False
		this.View.GetControl('FMettingEndFlow').Visible = False
		this.View.GetControl('FMettingNumFlow').Visible = False

		this.View.GetControl('FEatFlow').Visible = False

		this.View.GetControl('FCarTimeFlow').Visible = False
		this.View.GetControl('FCarAddrFlow').Visible = False


'''
按需求显示隐藏相关字段
'''
def DataChanged(e):
	if e.Key in ('FSubtitle', 'FMeetingRoom', 'FInnerEat', 'FInnerStay', 'FUseCar', 'FVisit'):
		_FMeeting = this.View.BillModel.GetValue('FMeeting')
		_FEat = this.View.BillModel.GetValue('FEat')
		_FPrint = this.View.BillModel.GetValue('FPrint')
		_FVehicle = this.View.BillModel.GetValue('FVehicle')
		_FStay = this.View.BillModel.GetValue('FStay')
		_FVisit = this.View.BillModel.GetValue('FVisit')
		#this.View.ShowMessage(_FVisit)
		# 欢迎字幕
		if _FPrint:
			this.View.GetControl('FSubtitleFlow').Visible = True
		else:
			this.View.GetControl('FSubtitleFlow').Visible = False
		# 预定会议室
		if _FMeeting:
			this.View.GetControl('FMettingStartFlow').Visible = True
			this.View.GetControl('FMettingEndFlow').Visible = True
			this.View.GetControl('FMettingNumFlow').Visible = True
		else:
			this.View.GetControl('FMettingStartFlow').Visible = False
			this.View.GetControl('FMettingEndFlow').Visible = False
			this.View.GetControl('FMettingNumFlow').Visible = False
		# 内部就餐
		if _FEat:
			this.View.GetControl('FEatFlow').Visible = True
		else:
			this.View.GetControl('FEatFlow').Visible = False
		# 用车
		if _FVehicle:
			this.View.GetControl('FCarTimeFlow').Visible = True
			this.View.GetControl('FCarAddrFlow').Visible = True
		else:
			this.View.GetControl('FCarTimeFlow').Visible = False
			this.View.GetControl('FCarAddrFlow').Visible = False


'''
动态验证字段必录
'''
def BeforeDoOperation(e):
	def Check():
		_FMeeting = this.View.BillModel.GetValue('FMeeting')
		_FEat = this.View.BillModel.GetValue('FEat')
		_FPrint = this.View.BillModel.GetValue('FPrint')
		_FVehicle = this.View.BillModel.GetValue('FVehicle')
		_FStay = this.View.BillModel.GetValue('FStay')
		_FVisit = this.View.BillModel.GetValue('FVisit')
		# 会议
		if _FMeeting:
			_FMeetStartTime = this.View.BillModel.GetValue('FMeetStartTime')
			_FMeetEndTime = this.View.BillModel.GetValue('FMeetEndTime')
			_FMeetPersonNum = this.View.BillModel.GetValue('FMeetPersonNum')
			if _FMeetStartTime is None:
				this.View.ShowErrMessage('‘会议开始’字段是必录项')
				return True
			if _FMeetEndTime is None:
				this.View.ShowErrMessage('‘会议结束’字段是必录项')
				return True
			if _FMeetPersonNum is None:
				this.View.ShowErrMessage('‘参会人数’字段是必录项')
				return True
		# 内部就餐
		if _FEat:
			pass
		# 用车
		if _FVehicle:
			_FUseCarTime = this.View.BillModel.GetValue('FUseCarTime')
			_FUseCarAddr = this.View.BillModel.GetValue('FUseCarAddr')
			if _FUseCarTime is None:
				this.View.ShowErrMessage('‘用车时间’字段是必录项')
				return True
			if _FUseCarAddr is None:
				this.View.ShowErrMessage('‘用车地点’字段是必录项')
				return True

		# 字幕
		if _FPrint:
			_FSubtitle = this.View.BillModel.GetValue('FSubtitle')
			if _FSubtitle is None:
				this.View.ShowErrMessage('‘字幕内容’字段是必录项')
				return True

		return False

	_opKey = e.Operation.FormOperation.Operation
	if _opKey == 'Submit':
		if Check():
			e.Cancel = True
