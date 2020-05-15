# 商机下推选择界面

IsReturnData = False
def DataChanged(e):
	key = e.Field.Key
	if key == "FBJType":
		FBJType = e.OldValue
		if FBJType:
			this.View.Model.SetValue("FBJType", 1)
		else:
			this.View.Model.SetValue("FKJFWType", 0)
	elif key == "FKJFWType":
		FKJFWType = e.OldValue
		if FKJFWType:
			this.View.Model.SetValue("FKJFWType", 1)
		else:
			this.View.Model.SetValue("FBJType", 0)


def ButtonClick(e):
	if e.Key == 'FCONFIRM':
		if this.View.OpenParameter.Status.ToString() != "VIEW":
			global IsReturnData
			IsReturnData = True
			FBJType = this.View.Model.GetValue("FBJType")
			FKJFWType = this.View.Model.GetValue("FKJFWType")
			PushFormId = ""
			if FBJType:
				PushFormId = "ora_CRM_MBL_BJ"
			elif FKJFWType:
				PushFormId = "ora_CRM_MBL_LawEntrust"
			this.View.ReturnToParentWindow(PushFormId)
			this.View.Close()
		else:
			this.View.Close()


def BeforeClose(e):
	global IsReturnData
	if IsReturnData == False:
		this.View.ReturnToParentWindow(None)

