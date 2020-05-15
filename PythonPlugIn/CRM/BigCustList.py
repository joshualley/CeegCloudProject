# 大客户列表过滤，仅能客户负责人看到

def PrepareFilterParameter(e):
	userId = this.Context.UserId
	StrFilter = "FCreatorId=" + str(userId)
	e.AppendQueryFilter(StrFilter)


