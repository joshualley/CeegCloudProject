# 费用台账报表


def ButtonClick(e):
	if e.Key.upper() == "FQUERY":
		webobj = {}
		webobj["source"] = "10.8.8.8:9000/index"
		webobj["height"] = 540
		webobj["width"] = 810
		webobj["isweb"] = False
		webobj["title"] = "费用台账详情"
		this.View.AddAction("ShowKDWebbrowseForm", webobj)
		this.View.SendDynamicFormAction(this.View)

