# 销售订单，下推选择单据类型

IsReturnData = False
def DataChanged(e):
	key = e.Field.Key
	if key == "FSHFWType": #售后服务
		FSHFWType = e.NewValue
		if FSHFWType:
			this.View.Model.SetValue("FXSKPType", 0)
			this.View.Model.SetValue("FTSDDType", 0)
	elif key == "FXSKPType": #销售开票
		FXSKPType = e.NewValue
		if FXSKPType:
			this.View.Model.SetValue("FSHFWType", 0)
			this.View.Model.SetValue("FTSDDType", 0)
	elif key == "FTSDDType": #特殊订单
		FTSDDType = e.NewValue
		if FTSDDType:
			this.View.Model.SetValue("FSHFWType", 0)
			this.View.Model.SetValue("FXSKPType", 0)


def ButtonClick(e):
	if e.Key == 'FCONFIRM':
		global IsReturnData
		IsReturnData = True
		FSHFWType = this.View.Model.GetValue("FSHFWType")
		FXSKPType = this.View.Model.GetValue("FXSKPType")
		FTSDDType = this.View.Model.GetValue("FTSDDType")
		PushFormId = ""
		if FSHFWType:
			PushFormId = "ora_CRM_MBL_CCRP"
		elif FXSKPType:
			PushFormId = "ora_CRM_MBL_XSKP"
		elif FTSDDType:
			PushFormId = "ora_CRM_MBL_TSDDJS"
		this.View.ReturnToParentWindow(PushFormId)
		this.View.Close()

def BeforeClose(e):
	global IsReturnData
	if IsReturnData == False:
		this.View.ReturnToParentWindow(None)

