# _*_ code: utf-8 _*_
# 出差报销插件汇总


def DataChanged(e):
	'''合计补贴金额
	'''
	if e.Key == 'FCarAmount':
		row = this.View.Model.GetEntryRowCount('FEntity')
		subTAmount = 0
		for row_i in range(row):
			ftype = this.View.Model.GetValue('FType', row_i)
			if ftype == '2':
				subAmount = this.View.Model.GetValue('FCarAmount', row_i)
				subTAmount += float(subAmount)

		this.View.Model.SetValue('FSubsidyAmount', subTAmount)



