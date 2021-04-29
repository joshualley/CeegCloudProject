/*---------------------------- PRD_MO 自动计算序列号 ----------------------------*/
CREATE proc [dbo].[proc_czty_PRDMO_RndSERIAL] 
@FID	int
as
begin
set nocount on
------------ 获取 MO 行 订单数量、序列号最大SEQ，序列号数量
select row_number() over(order by me.FEntryID)FRndIdx,m.FPRDORGID FMOOrgID,
m.FID FMOID,m.FCREATORID,me.FEntryID FMOEID,me.FMaterialID FMtlID,
mtl.FMasterID FMtlMID,me.FQty,ms.FMSCnt,ms.FMSMaxSEQ,--mtls.FISSNMANAGE,mtls.FSNUNIT,
convert(int,0)FSERIALID,convert(varchar(80),'')FSERIALNO,convert(int,0)FSL_FPKID,
convert(int,0)FSOrg_FEntryID,convert(int,0)FSOT_FEntryID,convert(int,0)FMs_FDetailID,
'' FMCCode 
into #t
--from (select * from t_prd_MO where FID=100193)m 
from (select * from t_prd_MO where FID=@FID)m 
inner join t_Prd_MOEntry me on m.FID=me.FID and me.FProductType=1 
inner join t_BD_Material mtl on me.FMaterialID=mtl.FMaterialID
inner join t_bd_department d on me.FWorkshopID=d.FDeptID
inner join t_BD_MaterialStock mtls on me.FMaterialID=mtls.FMaterialID and mtls.FISSNMANAGE=1
left join (
	select me.FID,me.FEntryID,count(ms.FDetailID)FMSCnt,isnull(max(ms.FSEQ),0)FMSMaxSEQ
	--from (select * from t_Prd_MOEntry where FID=100193)me 
	from (select * from t_Prd_MOEntry where FID=@FID)me 
	left join T_PRD_MOSNDETAIL ms on me.FEntryID=ms.FEntryID 
	group by me.FID,me.FEntryID
)ms on me.FEntryID=ms.FEntryID 
where me.FQty>ms.FMSCnt 

--select t.FMtlID,m.F_ora_Assistant,b.FNumber 
update t set t.FMCCode=b.FNumber 
from #t t 
inner join T_BD_MATERIAL m on t.FMtlID=m.FMATERIALID
inner join(
	select a.FID,ae.FENTRYID,ae.FSEQ,ae.FNUMBER,ael.FDESCRIPTION,ael.FDATAVALUE 
	from (select FID from T_BAS_ASSISTANTDATA_L where FName='产品大类' and FLOCALEID=2052)a
	inner join T_BAS_ASSISTANTDATAENTRY ae on a.FID=ae.FID
	inner join T_BAS_ASSISTANTDATAENTRY_L ael on ae.FENTRYID=ael.FENTRYID and ael.FLOCALEID=2052
)b on m.F_ora_Assistant=b.FENTRYID

--drop table #t
--select * from #t

--update #t set 
--	FSERIALID=ident_current('Z_BD_SERIALMASTER'),		--FSERIALID | FMASTERID 
--	FSL_FPKID=ident_current('Z_BD_SERIALMASTER_L'),		--FPKID 
--	FSOrg_FEntryID=ident_current('Z_BD_SERIALMASTERORG'),	--FEntryiD 
--	FSOT_FEntryID=ident_current('Z_BD_SERIALMASTEROTHER'),	--FEntryID 
--	FMs_FDetailID=ident_current('Z_PRD_MOSNDETAIL')		--FDetailID 
	
------------ 创建 算序列号表
create table #tp_SERIAL
(
FID	int	 primary key identity(1,1),	--ID
FRndIdx	int,						--#t:FRndIdx
FDHCode varchar(255),				--#t:FHelpCode
FIdx		int,					--#分FRndIdx计算序号
FSEQ		int,					--#t:FSEQ+FIdx
FMCCode		varchar(10),			--产品大类 FNUMBER
FSerialNo	varchar(80)				--#算号
)

--select newid()

------------ 写入 算序列号表
declare @FRndIdxMin int,@FRndIdxMax int
declare @FSEQ int,@FQty int,@FIdx int
select @FRndIdxMin=min(FRndIdx),@FRndIdxMax=max(FRndIdx) from #t
while(@FRndIdxMin<=@FRndIdxMax)
begin
	select @FIdx=1,@FSEQ=FMsMaxSEQ+1,@FQty=FQty from #t where FRndIdx=@FRndIdxMin
	while(@FSEQ<=@FQty)
	begin
		insert into #tp_SERIAL(FRndIdx,FIdx,FSEQ,FMCCode,FSerialNo)
		select FRndIdx,@FIdx,@FSEQ,FMCCode,--left(convert(varchar,getdate(),21),2)+right(newid(),7)
		FMCCode+left(convert(varchar,getdate(),21),2)+right(newid(),5)
		from #t where FRndIdx=@FRndIdxMin 
		select @FSEQ=@FSEQ+1,@FIdx=@FIdx+1
	end
	--print @FRndIdxMin
	set @FRndIdxMin=@FRndIdxMin+1
end

-- select * from #tp_SERIAL

------------ 算序列号表 防重复 #tp_SERIAL:T_BD_SERIALMASTER | #tp_SERIAL:#tp_SERIAL
declare @FRepeat int 
select @FRepeat=count(FID) from #tp_SERIAL tp inner join T_BD_SERIALMASTER s on tp.FSerialNo=s.FNumber 
select @FRepeat=@FRepeat+FRepeat 
	from(select FSerialNo,count(FID)FRepeat from #tp_SERIAL group by FSerialNo having count(FID)>1)t
--print @FRepeat 
while(@FRepeat>0)
begin
	----select *  
	--update tp set tp.FSerialNo='C'+t.FHelpCode+right(newid(),12)
	--from #tp_SERIAL tp 
	--inner join #t t on tp.FRndIdx=t.FRndIdx
	--inner join T_BD_SERIALMASTER s on tp.FSerialNo=s.FNumber 
	
	----select tp.*
	--update tp set tp.FSerialNo='C'+t.FHelpCode+right(newid(),12)
	--from #tp_SERIAL tp 
	--inner join #t t on tp.FRndIdx=t.FRndIdx
	--inner join(select FSerialNo,count(FID)FRepeat from #tp_SERIAL 
	--	group by FSerialNo having count(FID)>1)r on tp.FSerialNo=r.FSerialNo 
		
	update #tp_SERIAL set --FSerialNo=left(convert(varchar,getdate(),21),2)+right(newid(),7)
		FSerialNo=FMCCode+left(convert(varchar,getdate(),21),2)+right(newid(),5)
	where FID in(
		select distinct FID	--,FSerialNo
		from(
			select tp.FID,tp.FSerialNo 
			from #tp_SERIAL tp 
			inner join T_BD_SERIALMASTER s on tp.FSerialNo=s.FNumber 
			union
			select tp.FID,tp.FSerialNo 
			from #tp_SERIAL tp 
			inner join(select FSerialNo,count(FID)FRepeat from #tp_SERIAL 
				group by FSerialNo having count(FID)>1)r on tp.FSerialNo=r.FSerialNo 
		)r
	)
	
	set @FRepeat=0 
	select @FRepeat=count(FID) from #tp_SERIAL tp inner join T_BD_SERIALMASTER s on tp.FSerialNo=s.FNumber 
	select @FRepeat=@FRepeat+FRepeat 
		from(select FSerialNo,count(FID)FRepeat from #tp_SERIAL group by FSerialNo having count(FID)>1)t
end

------------ 写入K3 物理表 
declare @FSERIALID int,@FSL_FPKID int,@FSOrg_FEntryID int,@FSOT_FEntryID int,@FMs_FDetailID int
/*	select 
	ident_current('Z_BD_SERIALMASTER'),		--FSERIALID|FMASTERID	#t:FSERIALID
	ident_current('Z_BD_SERIALMASTER_L'),	--FPKID			#t:FSL_FPKID
	ident_current('Z_BD_SERIALMASTERORG'),	--FEntryiD		#t:FSOrg_FEntryID
	ident_current('Z_BD_SERIALMASTEROTHER'),--FEntryID		#t:FSOT_FEntryID
	ident_current('Z_PRD_MOSNDETAIL')		--FDetailID		#t:FMs_FDetailID
*/
------------ 写入K3 物理表 T_BD_SERIALMASTER
update #t set FSERIALID=ident_current('Z_BD_SERIALMASTER')
insert into T_BD_SERIALMASTER(
	FSERIALID,FMASTERID,FNUMBER,FFORBIDSTATUS,FDOCUMENTSTATUS,FMATERIALID,
	FTYPE,FFORBIDER,FFORBIDDATE,FISPREGENED,FRetailLocked,FRetailDetailUUID,
	FRetailLockTime,FSN,FIMEI1,FIMEI2,FIMEI3	
)
select --写入物料内码为FMasterID
	t.FSERIALID+s.FID FSERIALID,t.FSERIALID+s.FID FMASTERID,s.FSerialNo,'A','A',t.FMtlMID,
	'Y' FTYPE,0 FFORBIDER,NULL,0,0,'' FRetailDetailUUID,
	NULL,'','','',''
from #tp_SERIAL s inner join #t t on s.FRndIdx=t.FRndIdx 

select @FSERIALID=max(FSERIALID) from T_BD_SERIALMASTER
DBCC CHECKIDENT (Z_BD_SERIALMASTER,RESEED,@FSERIALID)	--改
--delete from T_BD_SERIALMASTER where FSERIALID>100016

------------ 写入K3 物理表 T_BD_SERIALMASTER_L
update #t set FSL_FPKID=ident_current('Z_BD_SERIALMASTER_L')
insert into T_BD_SERIALMASTER_L(FPKID,FSERIALID,FLOCALEID,FNAME,FDESCRIPTION)
select t.FSL_FPKID+s.FID FPKID,ss.FSERIALID,2052,s.FSerialNo,''
from #tp_SERIAL s inner join #t t on s.FRndIdx=t.FRndIdx 
inner join T_BD_SERIALMASTER ss on t.FSERIALID+s.FID =ss.FSERIALID 

select @FSL_FPKID=max(FPKID) from T_BD_SERIALMASTER_L
DBCC CHECKIDENT (Z_BD_SERIALMASTER_L,RESEED,@FSL_FPKID)	--改
--delete from T_BD_SERIALMASTER_L where FPKID>100016

------------ 写入K3 物理表 T_BD_SERIALMASTERORG
update #t set FSOrg_FEntryID=ident_current('Z_BD_SERIALMASTERORG')
insert into T_BD_SERIALMASTERORG(FENTRYID,FSERIALID,FORGID,FSTOCKSTATUS)
select t.FSOrg_FEntryID+s.FID FPKID,ss.FSERIALID,t.FMOOrgID,''
from #tp_SERIAL s inner join #t t on s.FRndIdx=t.FRndIdx 
inner join T_BD_SERIALMASTER ss on t.FSERIALID+s.FID =ss.FSERIALID 

select @FSOrg_FEntryID=max(FEntryID) from T_BD_SERIALMASTERORG
DBCC CHECKIDENT (Z_BD_SERIALMASTERORG,RESEED,@FSOrg_FEntryID)	--改
--delete from T_BD_SERIALMASTERORG where FEntryID>100016
--select * from T_BD_SERIALMASTERORG where FEntryID>100016

------------ 写入K3 物理表 T_BD_SERIALMASTEROTHER
update #t set FSOT_FEntryID=ident_current('Z_BD_SERIALMASTEROTHER')
insert into T_BD_SERIALMASTEROTHER(
	FENTRYID,FSERIALID,FSUPPLYID,FSUPPLYSERIAL,FPRODUCEDEPTID,FSTOCKID,FSTOCKLOCID,FAUXPROPID,FCUSTID,
	FLOT,FLOT_TEXT,FCREATORID,FCREATEDATE,FMODIFIERID,FMODIFYDATE,FSTARTDATE,FENDDATE,FPRODUCEDATE,FINPUTDATE
)
select 
	t.FSOT_FEntryID+s.FID FPKID,ss.FSERIALID,0 FSUPPLYID,'',0,0,0,0,0,
	0 FLOT,'',t.FCREATORID,getdate(),t.FCREATORID,getdate(),NULL,NULL,NULL,NULL
from #tp_SERIAL s inner join #t t on s.FRndIdx=t.FRndIdx 
inner join T_BD_SERIALMASTER ss on t.FSERIALID+s.FID =ss.FSERIALID 

select @FSOT_FEntryID=max(FEntryID) from T_BD_SERIALMASTEROTHER
DBCC CHECKIDENT (Z_BD_SERIALMASTEROTHER,RESEED,@FSOT_FEntryID)	--改

------------ 写入K3 物理表 T_PRD_MOSNDETAIL
update #t set FMs_FDetailID=ident_current('Z_PRD_MOSNDETAIL')
insert into T_PRD_MOSNDETAIL(
	FDETAILID,FENTRYID,FSEQ,FSERIALID,FSERIALNO,FSERIALNOTE,FSNQTY,
	FSNSTOCKINSELQTY,FSNRPTSELQTY,FBASESNQTY,FBASESNRPTSELQTY,FBASESNSTOCKINSELQTY)
select 
	t.FMs_FDetailID+s.FID,t.FMOEID,s.FSEQ,ss.FSERIALID,s.FSerialNo,'' FSERIALNOTE,1 FSNQTY,
	0 FSNSTOCKINSELQTY,0 FSNRPTSELQTY,1 FBASESNQTY,0 FBASESNRPTSELQTY,0 FBASESNSTOCKINSELQTY
from #tp_SERIAL s inner join #t t on s.FRndIdx=t.FRndIdx 
inner join T_BD_SERIALMASTER ss on t.FSERIALID+s.FID =ss.FSERIALID 

select @FMs_FDetailID=max(FDETAILID) from T_PRD_MOSNDETAIL
DBCC CHECKIDENT (Z_PRD_MOSNDETAIL,RESEED,@FMs_FDetailID)	--改

------------ 删除临时表 返回输出 
declare @backM varchar(10),@backMsg varchar(255),@rndRows int
select 'OK' backM,'共生成序列号 '+convert(varchar,count(FID))+'行' backMsg from #tp_SERIAL

drop table #t
drop table #tp_SERIAL
end
-------------------
-- exec proc_czty_PRDMO_RndSERIAL @FID=100003
