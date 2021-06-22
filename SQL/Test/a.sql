

-- update je set F_ora_SplitAmount=164420, F_ora_SplitAmountFor=164420
select FENTRYID, F_ora_SplitAmount, F_ora_SplitAmountFor 
from T_CN_JOURNALENTRY je
inner join T_CN_JOURNAL j on je.FID=j.FID
where FBillNo='SGRJZ2364'

-- update T_CN_JOURNALENTRY
-- set F_ora_SplitAmount=75431.94, F_ora_SplitAmountFor=75431.94
-- where FENTRYID=102539


select be.*, e.FID from ora_Exp_BalanceEntry be
inner join V_BD_SALESMAN sm on sm.FID=be.FSELLERID
inner join T_HR_EMPINFO e on e.FNumber=sm.FNUMBER
where be.FEmpId=0

