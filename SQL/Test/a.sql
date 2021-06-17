

-- update je set F_ora_SplitAmount=164420, F_ora_SplitAmountFor=164420
select FENTRYID, F_ora_SplitAmount, F_ora_SplitAmountFor 
from T_CN_JOURNALENTRY je
inner join T_CN_JOURNAL j on je.FID=j.FID
where FBillNo='SGRJZ2364'

-- update T_CN_JOURNALENTRY
-- set F_ora_SplitAmount=75431.94, F_ora_SplitAmountFor=75431.94
-- where FENTRYID=102539