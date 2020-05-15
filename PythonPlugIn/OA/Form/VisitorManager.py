'''
初始化时隐藏字段
'''
VisitorFirst = True
def AfterBindData(e):
	if str(this.Context.ClientType) != 'Mobile':
		global VisitorFirst
		if VisitorFirst:
			this.View.GetControl('FSubtitle').Visible = False

			this.View.GetControl('FMeetStartTime').Visible = False
			this.View.GetControl('FMeetEndTime').Visible = False
			this.View.GetControl('FMeetPersonNum').Visible = False

			this.View.GetControl('FLunchTime').Visible = False
			this.View.GetControl('FLunch').Visible = False
			this.View.GetControl('FSupper').Visible = False

			this.View.GetControl('FUseCarTime').Visible = False
			this.View.GetControl('FUseCarAddr').Visible = False


'''
按需求显示隐藏相关字段
'''
def DataChanged(e):
	if str(this.Context.ClientType) != 'Mobile':
		if e.Key in ('FMeeting', 'FEat', 'FPrint', 'FVehicle', 'FStay', 'FVisit'):
			_FMeeting = this.View.Model.GetValue('FMeeting')
			_FEat = this.View.Model.GetValue('FEat')
			_FPrint = this.View.Model.GetValue('FPrint')
			_FVehicle = this.View.Model.GetValue('FVehicle')
			_FStay = this.View.Model.GetValue('FStay')
			_FVisit = this.View.Model.GetValue('FVisit')
			#this.View.ShowMessage(_FVisit)
			# 欢迎字幕
			if _FPrint:
				this.View.GetControl('FSubtitle').Visible = True
			else:
				this.View.GetControl('FSubtitle').Visible = False
			# 预定会议室
			if _FMeeting:
				this.View.GetControl('FMeetStartTime').Visible = True
				this.View.GetControl('FMeetEndTime').Visible = True
				this.View.GetControl('FMeetPersonNum').Visible = True
			else:
				this.View.GetControl('FMeetStartTime').Visible = False
				this.View.GetControl('FMeetEndTime').Visible = False
				this.View.GetControl('FMeetPersonNum').Visible = False
			# 内部就餐
			if _FEat:
				this.View.GetControl('FLunchTime').Visible = True
				this.View.GetControl('FLunch').Visible = True
				this.View.GetControl('FSupper').Visible = True
			else:
				this.View.GetControl('FLunchTime').Visible = False
				this.View.GetControl('FLunch').Visible = False
				this.View.GetControl('FSupper').Visible = False
			# 用车
			if _FVehicle:
				this.View.GetControl('FUseCarTime').Visible = True
				this.View.GetControl('FUseCarAddr').Visible = True
			else:
				this.View.GetControl('FUseCarTime').Visible = False
				this.View.GetControl('FUseCarAddr').Visible = False