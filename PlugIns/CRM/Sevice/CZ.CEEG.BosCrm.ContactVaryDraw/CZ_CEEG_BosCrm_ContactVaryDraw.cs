using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Data;
using System.Collections;

using Kingdee.BOS.Contracts;
using Kingdee.BOS.Core.Bill;
using Kingdee.BOS.Core.Bill.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Core.Metadata;
using Kingdee.BOS.Core.SqlBuilder;
using Kingdee.BOS.Orm.DataEntity;

using Kingdee.BOS.Core.DynamicForm.PlugIn.ControlModel;
using Kingdee.BOS.App.Data;
using Kingdee.BOS.Core.Metadata.ConvertElement.PlugIn;
using Kingdee.BOS.Core.Metadata.ConvertElement.PlugIn.Args;
using Kingdee.BOS.Core.Metadata.EntityElement;
using Kingdee.BOS.App;

namespace CZ.CEEG.BosCrm.ContactVaryDraw
{
    [Description("销售合同变更-选单服务")]
    [Kingdee.BOS.Util.HotUpdate]
    public class CZ_CEEG_BosCrm_ContactVaryDraw : AbstractConvertPlugIn
    {
        #region 数据查询
        /// <summary>
        /// 基本方法 
        /// </summary>
        /// <param name="_sql"></param>
        /// <returns></returns>
        public DynamicObjectCollection CZDB_GetData(string _sql)
        {
            try
            {
                var obj = DBUtils.ExecuteDynamicObject(this.Context, _sql);
                return obj;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        #endregion

        public override void OnParseFilter(ParseFilterEventArgs e)
        {
            base.OnParseFilter(e);
            
        }

        public override void AfterConvert(AfterConvertEventArgs e)
        {
            base.AfterConvert(e);
        }

        public override void OnAfterCreateLink(CreateLinkEventArgs e)
        {
            base.OnAfterCreateLink(e);
            //源单
            Entity srcFEntity = e.SourceBusinessInfo.GetEntity("FEntity");
            Entity srcFEntityBPR = e.SourceBusinessInfo.GetEntity("FEntityBPR");
            Entity srcFEntityM = e.SourceBusinessInfo.GetEntity("FEntityM");
            //Entity srcFEntityHD = e.SourceBusinessInfo.GetEntity("FEntityHD");
            //目标单
            Entity tgtFEntity = e.TargetBusinessInfo.GetEntity("FEntity");
            Entity tgtFEntityBPR = e.TargetBusinessInfo.GetEntity("FEntityBPR");
            Entity tgtFEntityM = e.TargetBusinessInfo.GetEntity("FEntityM");
            //Entity tgtFEntityHD = e.TargetBusinessInfo.GetEntity("FEntityHD");

            var billDataEntitys = e.TargetExtendedDataEntities.FindByEntityKey("FBillHead");
            foreach(var item in billDataEntitys)
            {
                DynamicObject dataObject = item.DataEntity;
                string sql = string.Format("select * from ora_CRM_Contract where FBillNo='{0}'", dataObject["FNicheID"].ToString());
                var pkIDs = CZDB_GetData(sql);
                if (pkIDs.Count <= 0)
                    continue;
                string FID = pkIDs[0]["FID"].ToString();
                //加载源单数据
                IViewService viewService = ServiceHelper.GetService<IViewService>();
                
                sql = string.Format("select FEntryID from ora_CRM_ContractEntry where FID='{0}'", FID);
                var srcFEntityEIDs = CZDB_GetData(sql);
                if (srcFEntityEIDs.Count > 0)
                {
                    object[] objs = new object[srcFEntityEIDs.Count];
                    for (int i = 0; i < srcFEntityEIDs.Count; i++)
                    {
                        objs[i] = srcFEntityEIDs[i]["FEntryID"].ToString();
                    }
                    var srcFEntityBillObjs = viewService.Load(this.Context, objs, srcFEntity.DynamicObjectType);
                    // 开始把源单单据体数据，填写到目标单上
                    DynamicObjectCollection tgtFEntityRows = tgtFEntity.DynamicProperty.GetValue(dataObject) as DynamicObjectCollection;
                    tgtFEntityRows.Clear();
                    foreach (var srcRow in srcFEntityBillObjs)
                    {
                        DynamicObject newRow = new DynamicObject(tgtFEntity.DynamicObjectType);
                        tgtFEntityRows.Add(newRow);
                        newRow["FMtlGroup"] = srcRow["FMtlGroup"];
                        newRow["FDescribe"] = srcRow["FDescribe"];
                        newRow["FQty"] = srcRow["FQty"];
                        newRow["FModel"] = srcRow["FModel"];
                        newRow["FIsStandard"] = srcRow["FIsStandard"];
                        newRow["FBPRndID"] = srcRow["FBPRndID"];
                        newRow["FBRndNo"] = srcRow["FBRndNo"];
                        newRow["FGUID"] = srcRow["FGUID"];
                        newRow["FIS2W"] = srcRow["FIS2W"];
                    }
                    
                }
                /*
                sql = string.Format("select FBEntryID from ora_CRM_ContractBPR where FID='{0}'", FID);
                var srcFEntityBPREIDs = CZDB_GetData(sql);
                if (srcFEntityBPREIDs.Count > 0)
                {
                    object[] objs = new object[srcFEntityBPREIDs.Count];
                    for (int i = 0; i < srcFEntityBPREIDs.Count; i++)
                    {
                        objs[i] = srcFEntityBPREIDs[i]["FBEntryID"].ToString();
                    }
                    var srcFEntityBPRBillObjs = viewService.Load(this.Context, objs, srcFEntityBPR.DynamicObjectType);
                    DynamicObjectCollection tgtFEntityBPRRows = tgtFEntityBPR.DynamicProperty.GetValue(dataObject) as DynamicObjectCollection;
                    tgtFEntityBPRRows.Clear();
                    foreach (var srcRow in srcFEntityBPRBillObjs)
                    {
                        DynamicObject newRow = new DynamicObject(tgtFEntityBPR.DynamicObjectType);
                        tgtFEntityBPRRows.Add(newRow);
                        //newRow["BSEQ"] = srcRow["BSEQ"];
                        newRow["FBGUID"] = srcRow["FBGUID"];
                        newRow["FBSrcEID"] = srcRow["FBSrcEID"];
                        newRow["FBSrcSEQ"] = srcRow["FBSrcSEQ"];
                        newRow["FBPRndSEQ"] = srcRow["FBPRndSEQ"];
                        newRow["FBMtlGroup"] = srcRow["FBMtlGroup"];
                        newRow["FBMtlItem"] = srcRow["FBMtlItem"];
                        newRow["FMaterialID"] = srcRow["FMaterialID"];
                        newRow["FBDescribe"] = srcRow["FBDescribe"];
                        newRow["FBQty"] = srcRow["FBQty"];
                        newRow["FBModel"] = srcRow["FBModel"];
                        newRow["FBIsStandard"] = srcRow["FBIsStandard"];
                        newRow["FBasePrice"] = srcRow["FBasePrice"];
                        newRow["FBPAmt"] = srcRow["FBPAmt"];
                        newRow["FBPAmtGroup"] = srcRow["FBPAmtGroup"];
                        newRow["FBRptPrice"] = srcRow["FBRptPrice"];
                        newRow["FBAbaComm"] = srcRow["FBAbaComm"];
                        newRow["FBDownPoints"] = srcRow["FBDownPoints"];
                        newRow["FBWorkDay"] = srcRow["FBWorkDay"];
                        newRow["FBCostAdj"] = srcRow["FBCostAdj"];
                        newRow["FBCAReason"] = srcRow["FBCAReason"];
                        newRow["FBDelivery"] = srcRow["FBDelivery"];
                        newRow["FBPAmtLc"] = srcRow["FBPAmtLc"];
                        newRow["FBRptPrcLc"] = srcRow["FBRptPrcLc"];
                        newRow["FBIS2W"] = srcRow["FBIS2W"];
                        newRow["FBUnitID"] = srcRow["FBUnitID"];
                        newRow["FBTaxRateID"] = srcRow["FBTaxRateID"];
                        newRow["FBTaxRate"] = srcRow["FBTaxRate"];
                        newRow["FBBomVsn"] = srcRow["FBBomVsn"];
                        newRow["FBTaxPrice"] = srcRow["FBTaxPrice"];
                        newRow["FBNTPrice"] = srcRow["FBNTPrice"];
                        newRow["FBTaxAmt"] = srcRow["FBTaxAmt"];
                        newRow["FBNTAmt"] = srcRow["FBNTAmt"];

                        newRow["FBRangeAmtOne"] = srcRow["FBRangeAmtOne"];
                        newRow["FBRangeAmtGP"] = srcRow["FBRangeAmtGP"];
                        newRow["FBRangeAmtReason"] = srcRow["FBRangeAmtReason"];
                        newRow["FProdFactory"] = srcRow["FProdFactory"];
                        //newRow["FBLkQty"] = srcRow["FBLkQty"];
                    }
                }
                */
                sql = string.Format("select FEntryIDM from ora_CRM_ContractMtl where FID='{0}'", FID);
                var srcFEntityMEIDs = CZDB_GetData(sql);
                if (srcFEntityMEIDs.Count > 0)
                {
                    object[] objs = new object[srcFEntityMEIDs.Count];
                    for (int i = 0; i < srcFEntityMEIDs.Count; i++)
                    {
                        objs[i] = srcFEntityMEIDs[i]["FEntryIDM"].ToString();
                    }
                    var srcFEntityMBillObjs = viewService.Load(this.Context, objs, srcFEntityM.DynamicObjectType);
                    DynamicObjectCollection tgtFEntityMRows = tgtFEntityM.DynamicProperty.GetValue(dataObject) as DynamicObjectCollection;
                    tgtFEntityMRows.Clear();
                    foreach (var srcRow in srcFEntityMBillObjs)
                    {
                        DynamicObject newRow = new DynamicObject(tgtFEntityM.DynamicObjectType);
                        tgtFEntityMRows.Add(newRow);
                        newRow["FMGUID"] = srcRow["FMGUID"];
                        newRow["FMSrcEID"] = srcRow["FMSrcEID"];
                        newRow["FMSrcSEQ"] = srcRow["FMSrcSEQ"];
                        newRow["FMMtlGroup"] = srcRow["FMMtlGroup"];
                        newRow["FMMtlItem"] = srcRow["FMMtlItem"];
                        newRow["FMClass"] = srcRow["FMClass"];
                        newRow["FMMtl"] = srcRow["FMMtl"];
                        newRow["FMModel"] = srcRow["FMModel"];
                        newRow["FMQty"] = srcRow["FMQty"];
                        newRow["FMUnit"] = srcRow["FMUnit"];
                        newRow["FMPrice"] = srcRow["FMPrice"];
                        newRow["FMAmt"] = srcRow["FMAmt"];
                        newRow["FMGpAmtB"] = srcRow["FMGpAmtB"];
                        newRow["FMCostRate"] = srcRow["FMCostRate"];
                        newRow["FMCost"] = srcRow["FMCost"];
                        newRow["FMGPRate"] = srcRow["FMGPRate"];
                        newRow["FMGP"] = srcRow["FMGP"];
                        newRow["FMGpAmt"] = srcRow["FMGpAmt"];
                        newRow["FMGpAmtLc"] = srcRow["FMGpAmtLc"];
                        newRow["FMIS2W"] = srcRow["FMIS2W"];
                    }
                    
                }
                
                



            }
                

           
        }

    }
}
