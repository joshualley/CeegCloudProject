# 售后服务，下推选择单据类型

IsReturnData = False
def DataChanged(e):
	key = e.Field.Key
	if key == "FChangeType": #退换货维修
		FSHFWType = e.NewValue
		if FSHFWType:
			this.View.Model.SetValue("FOfferType", 0)
	elif key == "FOfferType": #维修报价
		FXSKPType = e.NewValue
		if FXSKPType:
			this.View.Model.SetValue("FChangeType", 0)


def ButtonClick(e):
	if e.Key == 'FCONFIRM':
		global IsReturnData
		IsReturnData = True
		FChangeType = this.View.Model.GetValue("FChangeType")
		FOfferType = this.View.Model.GetValue("FOfferType")
		PushFormId = ""
		if FChangeType:
			PushFormId = "ora_CRM_MBL_SHFW" #退换货维修
		elif FOfferType:
			PushFormId = "ora_CRM_MBL_MaintainOffer" #维修报价

		this.View.ReturnToParentWindow(PushFormId)
		this.View.Close()

def BeforeClose(e):
	global IsReturnData
	if IsReturnData == False:
		this.View.ReturnToParentWindow(None)



