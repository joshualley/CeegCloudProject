"""
手工日记账
"""

import clr
clr.AddReference('Kingdee.BOS.App')
from Kingdee.BOS.App.Data import *

def BarItemClick(e):
	key = e.BarItemKey.upper()
	if key == "ORA_TBUNAUDIT": #作废前反审核
		fid = str(this.Model.DataObject['Id'])
		sql = "/*dialect*/update T_CN_JOURNAL set FDocumentStatus='D' where fid='{}'".format(fid)
		DBUtils.Execute(this.Context, sql)
		this.View.Refresh()
