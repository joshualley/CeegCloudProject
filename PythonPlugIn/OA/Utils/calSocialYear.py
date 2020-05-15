#员工表单

'''
动态计算入职本公司前，社会工作年
'''
def calSocietialYear(e):
	if e.Key in ('FJoinDate', 'F_HR_BobDate'):
		FJoinDate = this.View.Model.GetValue('FJoinDate')
		F_HR_BobDate = this.View.Model.GetValue('F_HR_BobDate')
		if FJoinDate != None and F_HR_BobDate != None:
			sql = "select dbo.fn_GetWorkYear('{0}', '{1}') SYear".format(FJoinDate, F_HR_BobDate)
			obj = DBUtils.ExecuteDynamicObject(this.Context, sql)[0]
			this.View.Model.SetValue('FSocietyYear', str(obj['SYear']))