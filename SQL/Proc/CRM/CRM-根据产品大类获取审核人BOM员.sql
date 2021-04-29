--根据产品大类获取审核人BOM员
CREATE PROC [dbo].[proc_czly_GetCrmBomerByMtlGroup]
    @CFID INT
AS
BEGIN
    --根据产品大类表体选择BOM员
    SELECT DISTINCT e.FID --, el.FNAME, mgl.FID, mgl.FNAME 
    INTO #TempBomer
    FROM ora_CRM_ContractEntry ce
    INNER JOIN ora_CrmBD_MtlGroup_L mgl ON ce.FMTLGROUP=mgl.FID
    LEFT JOIN T_HR_EMPINFOCrmMG emg ON ce.FMTLGROUP=emg.F_ORA_CRMMTLGP
    INNER JOIN T_HR_EMPINFO e ON emg.FID=e.FID
    INNER JOIN T_HR_EMPINFO_L el ON e.FID=el.FID
    WHERE ce.FID=@CFID AND e.F_ORA_ISCRMBOM=1
    --设置BOM员
    DECLARE @pk INT = (SELECT MAX(FPKID) FROM ora_CRM_ContractBomer)
    SELECT @pk = ISNULL(@pk, 100000)
    DECLARE @bomer INT
    DELETE FROM ora_CRM_ContractBomer WHERE FID=@CFID
    WHILE EXISTS(SELECT FID FROM #TempBomer)
    BEGIN
        SET @pk += 1
        SELECT TOP 1 @bomer=FID from #TempBomer
        INSERT INTO ora_CRM_ContractBomer(FPKID, FID, FBomAuditor) VALUES(@pk, @CFID, @bomer)
        DELETE FROM #TempBomer WHERE FID=@bomer
    END
    --DBCC CHECKIDENT(Z_ora_CRM_ContractBomer, RESEED, @pk)
END

-- EXEC proc_czly_GetCrmBomerByMtlGroup @CFID='100068'

--SELECT * FROM ora_CRM_Contract
--DELETE from ora_CRM_ContractBomer
