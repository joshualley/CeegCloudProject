"""
业务费结算服务插件
功能：
保存时将对应的拆分单标记为已结算
删除时则将对应的拆分单的结算标记清楚掉
"""
import clr
clr.AddReference('Kingdee.BOS.App')
from Kingdee.BOS.App.Data import *

def BeginOperationTransaction(e):
	opKey = this.FormOperation.Operation.upper()
	for d in e.DataEntitys:
		FID = str(d["Id"])
		if opKey == "DELETE":
			untag_split(FID)

def EndOperationTransaction(e):
	opKey = this.FormOperation.Operation.upper()
	for d in e.DataEntitys:
		FID = str(d["Id"])
		if opKey == "SAVE":
			tag_split(FID)


def tag_split(FID):
	sql = """/*dialect*/UPDATE s SET s.FIS_JS=1 FROM T_CZ_ReceiptSplit s
INNER JOIN ora_OptExp_SplitEntry pes ON s.FID=pes.FSplitInterID
WHERE pes.FID='{}' """.format(FID)
	DBUtils.Execute(this.Context, sql)
	

def untag_split(FID):
	sql = """/*dialect*/UPDATE s SET s.FIS_JS=0 FROM T_CZ_ReceiptSplit s
INNER JOIN ora_OptExp_SplitEntry pes ON s.FID=pes.FSplitInterID
WHERE pes.FID='{}' """.format(FID)
	DBUtils.Execute(this.Context, sql)
