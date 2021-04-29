-- CRM-销售报价保存后
CREATE proc [dbo].[proc_cztyCrm_SaleOffer_AfterSave]
@FID	int
as
begin
set nocount on
--select sb.FBSRCEID,se.FEntryID,sb.FBSRCSEQ,se.FSEQ
update d set d.FBSRCEID=t.FEntryID,d.FBSRCSEQ=t.FSEQ,d.FBSEQ=t.FNewSEQ
from(select sb.FBEntryID,ROW_NUMBER() over(order by se.FSEQ,sb.FBSEQ) FNewSEQ,se.FEntryID,se.FSEQ
	from(select * from ora_CRM_SaleOfferEntry where FID=@FID)se
	inner join ora_CRM_SaleOfferBPR sb on se.FGUID=sb.FBGUID
)t
inner join ora_CRM_SaleOfferBPR d on t.FBEntryID=d.FBEntryID

--select sm.FMSRCEID,se.FEntryID,sm.FMSRCSEQ,se.FSEQ
update d set d.FMSRCEID=t.FEntryID,d.FMSRCSEQ=t.FSEQ,d.FMSEQ=FNewSEQ
from(select sm.FEntryIDM,ROW_NUMBER() over(order by se.FSEQ,sm.FMSEQ) FNewSEQ,se.FEntryID,se.FSEQ
	from(select * from ora_CRM_SaleOfferEntry where FID=@FID)se
	inner join ora_CRM_SaleOfferMtl sm on se.FGUID=sm.FMGUID
)t
inner join ora_CRM_SaleOfferMtl d on t.FEntryIDM=d.FEntryIDM

--(基价-报价)*100/报价 ===>（总基价-总报价+表头扣款金额）/总基价
--select b.FID,FAvgDnPts,case FAmountRpt when 0 then 0 else convert(decimal(18,2),(FAmount-FAmountRpt)*100/FAmountRpt) end,FMaxDnPts,FRowMaxPts  
update b set 
--b.FAvgDnPts=case FAmountRpt when 0 then 0 else convert(decimal(18,2),(FAmount-FAmountRpt)*100/FAmountRpt)end,
b.FAvgDnPts=case when (FAmountRpt=0 or FAmount=0) then 0 else convert(decimal(18,2),(FAmount-FAmountRpt+FRangeAmt)*100/FAmount) end,
b.FMaxDnPts=isnull(bb.FRowMaxPts,0)
from ora_CRM_SaleOffer b
left join (select FID,convert(decimal(18,2),Max(FBDownPoints))FRowMaxPts from ora_CRM_SaleOfferBPR where FID=@FID group by FID) bb on b.FID=bb.FID 
where b.FID=@FID 

select 'OK' backM,'' bakcMsg

end
----------------
-- exec proc_cztyCrm_SaleOffer_AfterSave @FID='110170'