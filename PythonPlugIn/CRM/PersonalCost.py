
'''个人费用报销
表体费用项目名称写入表头
'''
def DataChanged(e):
	if e.Key == 'FCostItem':
		setCostItem()

def BeforeDoOperation(e):
	_opKey = e.Operation.FormOperation.Operation
	if _opKey in ('Save', 'Submit'):
		setCostItem()

def setCostItem():
	if str(this.Context.ClientType) == 'Mobile':
		FCostItem = '' if this.View.BillModel.GetValue('FCostItem', 0) is None else str(this.View.BillModel.GetValue('FCostItem', 0)['Name'])
		this.View.BillModel.SetValue('FCostItemName', FCostItem)
	else:
		FCostItem = '' if this.View.Model.GetValue('FCostItem', 0) is None else str(this.View.Model.GetValue('FCostItem', 0)['Name'])
		this.View.Model.SetValue('FCostItemName', FCostItem)

