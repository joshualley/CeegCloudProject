"""
合同扣款调整反写销售订单
"""

import clr
clr.AddReference('Kingdee.BOS.App')
from Kingdee.BOS.App.Data import *


def EndOperationTransaction(e):
	opKey = this.FormOperation.Operation.upper()
	for d in e.DataEntitys:
		FID = str(d["Id"])
		if opKey == "AUDIT":
			backWriteOrder(FID)

def backWriteOrder(FID):
	sql = '''/*dialect*/UPDATE oe SET 
		oe.F_CZ_FBRangeAmtGP=we.FWithholdAmt,
		oe.F_CZ_BRangeAmtGP=we.FWithholdAmt/we.FQty,
		oe.F_CZ_FBrangeAmtReason=we.FWithholdReason,
		oe.FBDownPoints=we.FDownPoint
	FROM T_SAL_ORDERENTRY oe 
	INNER JOIN ora_OptExp_WithholdEntry we ON oe.FEntryID=we.FSrcEntryId
	WHERE we.FID={}
	'''.format(FID)
	DBUtils.Execute(this.Context, sql)