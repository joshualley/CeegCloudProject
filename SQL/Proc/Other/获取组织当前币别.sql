-- 获取组织当前币别
create proc [dbo].[proc_czty_GetOrgCurrency]
@FOrgID int
as
begin
---------CLOUD 会计核算体系--会计政策（主币别）
select 
a.FAcctSystemID,ae.FENTRYID,ae.FMainOrgID,ae.FDefAcctPolicy,p.FCurrencyID,cl.FNAME
--会计核算体系
from (select FACCTSYSTEMID from T_ORG_AccountSystem where FDOCUMENTSTATUS='C' and FForbidStatus='A')a
inner join T_ORG_ACCTSYSENTRY ae on a.FACCTSYSTEMID=ae.FACCTSYSTEMID
--会计政策
inner join T_FA_ACCTPOLICY p on ae.FDefAcctPolicy=p.FACCTPOLICYID 
--币别
inner join T_BD_CURRENCY c on p.FCurrencyID=c.FCurrencyID
inner join T_BD_CURRENCY_L cl on c.FCurrencyID=cl.FCurrencyID and cl.FLOCALEID=2052
where ae.FMainOrgID=@FOrgID 
end
-------------
-- exec proc_czty_GetOrgCurrency @FOrgID='1'
