
# 凭证
import clr
clr.AddReference('Kingdee.BOS.App')
from Kingdee.BOS.App.Data import *

def AfterBarItemClick(e):
	key = str(e.BarItemKey)
	if key == "ora_tbUpdate":
		update_voucher()


def update_voucher():
	if str(this.Model.GetValue("FDocumentStatus")) != "C":
		this.View.ShowMessage("审核后才能使用！")
		return
	entryCount = this.Model.GetEntryRowCount("FEntity")
	if entryCount == 0:
		return
	sql = ""
	for i in range(entryCount):
		F_ora_CostType = this.Model.GetValue("F_ora_CostType", i)
		F_ora_CostTypeRemarks = this.Model.GetValue("F_ora_CostTypeRemarks", i)
		if F_ora_CostType == None and F_ora_CostTypeRemarks == None:
			continue
		costType = "" if F_ora_CostType == None else F_ora_CostType["Id"]
		remark = "" if F_ora_CostTypeRemarks == None else F_ora_CostTypeRemarks
		FEntryID = this.Model.GetEntryPKValue("FEntity", i)
		sql += "UPDATE T_GL_VOUCHERENTRY SET F_ora_CostType='{}', F_ora_CostTypeRemarks='{}' WHERE FEntryID='{}';\n".format(
			costType, remark, FEntryID)
	
	count = DBUtils.Execute(this.Context, sql)
	if count > 0:
		this.View.Refresh()
		this.View.ShowMessage("更新完成！")
