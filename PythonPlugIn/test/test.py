

# 测试Code
#-------------------------------------------------------------
ekey = "FEntity"
grd = this.View.GetControl(ekey)
he = HiddenEntity()
he.H = True
he.M = ""
grd.SetCellHidden("FText1", he, 0)

from Kingdee.BOS.Core.DynamicForm.PlugIn import *
from Kingdee.BOS.Core.DynamicForm.PlugIn.ControlModel import *
def DataChanged(e):
	grid = this.View.GetControl("FEntity")
	hidden = HiddenEntity()
	hidden.H = True
	hidden.M = ""
	grid.SetCellHidden("FNumber", hidden, 0)


	# 用餐类别
	DiningType = 'FDiningType'
	if e.Key == DiningType:
		value = this.View.BillModel.GetValue('FDiningType')
		
		if value == '2':
			ekey = "FEntity"
#-------------------------------------------------------------