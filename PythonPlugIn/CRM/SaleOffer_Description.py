'''报价详情
'''

IsReturnData = False

def AfterCreateNewData(e):
	FMtlGroup = this.View.OpenParameter.GetCustomParameter('FMtlGroup')
	FMtlItem = this.View.OpenParameter.GetCustomParameter('FMtlItem')
	FQty = this.View.OpenParameter.GetCustomParameter('FQty')
	FPAmt = this.View.OpenParameter.GetCustomParameter('FPAmt') #行基价
	FPAmtGroup = this.View.OpenParameter.GetCustomParameter('FPAmtGroup') #组基价汇总
	FRangeAmtGP = this.View.OpenParameter.GetCustomParameter('FRangeAmtGP') #组扣款汇总
	FDescribe = this.View.OpenParameter.GetCustomParameter('FDescribe') 
	FModel = this.View.OpenParameter.GetCustomParameter('FModel')
	FRptPrice = this.View.OpenParameter.GetCustomParameter('FRptPrice') #报价
	FDownPoints = this.View.OpenParameter.GetCustomParameter('FDownPoints') #下浮
	this.View.Model.SetValue('FMtlGroup', FMtlGroup)
	this.View.Model.SetValue('FMtlItem', FMtlItem)
	this.View.Model.SetValue('FQty', FQty)
	this.View.Model.SetValue('FPAmt', FPAmt)
	this.View.Model.SetValue('FPAmtGroup', FPAmtGroup)
	this.View.Model.SetValue('FRangeAmtGP', FRangeAmtGP)
	this.View.Model.SetValue('FDescribe', FDescribe)
	this.View.Model.SetValue('FModel', FModel)
	this.View.Model.SetValue('FRptPrice', FRptPrice)
	this.View.Model.SetValue('FDownPoints', FDownPoints)
	

def DataChanged(e):
	if e.Key == 'FRptPrice':
		FPAmtGroup = float(this.View.Model.GetValue('FPAmtGroup')) #组基价汇总
		FRangeAmtGP = float(this.View.Model.GetValue('FRangeAmtGP')) 
		FRptPriceGroup = float(this.View.OpenParameter.GetCustomParameter('FRptPriceGroup')) 
		FRptPrice = float(this.View.Model.GetValue('FRptPrice')) + FRptPriceGroup #组报价汇总
		downPoints = (FPAmtGroup-FRptPrice+FRangeAmtGP)*100/FPAmtGroup
		this.View.Model.SetValue('FDownPoints', downPoints)

def ButtonClick(e):
	if e.Key == 'FCONFIRM':
		if this.View.OpenParameter.Status.ToString() != "VIEW":
			global IsReturnData
			IsReturnData = True
			FRptPrice = this.View.Model.GetValue('FRptPrice')
			this.View.ReturnToParentWindow(str(FRptPrice))
			this.View.Close()
		else:
			this.View.Close()


def BeforeClose(e):
	global IsReturnData
	if IsReturnData == False:
		this.View.ReturnToParentWindow(None)