# _*_ code: utf-8 _*_
# 采购合同评审插件汇总

'''
设置税率，默认13%
'''
def DataChanged(e):
	if str(this.Context.ClientType) != 'Mobile':
		if e.Key == 'FIncludeTax':
			isInTax = this.View.Model.GetValue('FIncludeTax')
			entryCount = this.View.Model.GetEntryRowCount('FEntity')
			value = 13 if isInTax else 0
			
			for row in range(entryCount):
				this.View.Model.SetValue('FTaxRate', value, row)
				this.View.InvokeFieldUpdateService('FTaxRate', 0)

def AfterCreateNewEntryRow(e):
	if str(this.Context.ClientType) != 'Mobile':
		isInTax = this.View.Model.GetValue('FIncludeTax')
		value = 13 if isInTax else 0
		this.View.Model.SetValue('FTaxRate', value, e.Row)
		this.View.InvokeFieldUpdateService('FTaxRate', 0)



