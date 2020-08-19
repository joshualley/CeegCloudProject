# 费用台账报表


def ButtonClick(e):
	if e.Key.upper() == "FQUERY":
		webobj = {}
		webobj["source"] = "http://buidu.com"
		webobj["height"] = 540
		webobj["width"] = 810
		webobj["isweb"] = False
		webobj["title"] = "费用台账详情"
		this.View.AddAction("ShowKDWebbrowseForm", webobj)
		this.View.SendDynamicFormAction(this.View)

