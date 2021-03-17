# 员工列表，更新销售员绑定的任岗信息

import clr
clr.AddReference('Kingdee.BOS')
clr.AddReference('Kingdee.BOS.App')

from Kingdee.BOS.App.Data import *

def AfterBarItemClick(e):
	key = e.BarItemKey.upper()
	if key == 'ORA_TBUPDATESELLERBINDING': # ora_tbUpdateSellerBinding 更新销售员绑定
		sql = """/*dialect*/
update oe set oe.FSTAFFID=st.FSTAFFID
from T_BD_OPERATORENTRY oe
inner join T_HR_EMPINFO e on e.FNUMBER=oe.FNUMBER
inner join T_BD_STAFFTEMP st on st.FID=e.FID and st.FISFIRSTPOST='1'
where st.FSTAFFID<>oe.FSTAFFID"""
		num = DBUtils.Execute(this.Context, sql)
		this.View.ShowMessage("更新了{}条记录。".format(num))