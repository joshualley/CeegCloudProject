using Kingdee.BOS.App.Data;
using Kingdee.BOS.Core.DynamicForm.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Core.Validation;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.Util;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace CZ.CEEG.SrvErp.PayWriteBack
{
    [HotUpdate]
    [Description("付款单反写")]
    public class CZ_CEEG_SrvErp_PayWriteBack : AbstractOperationServicePlugIn
    {
        public override void OnPreparePropertys(PreparePropertysEventArgs e)
        {
            base.OnPreparePropertys(e);
            e.FieldKeys.Add("REALPAYAMOUNTFOR"); //实付金额
            e.FieldKeys.Add("SOURCETYPE"); //源单类型
            e.FieldKeys.Add("SRCBILLNO");  //源单单号
        }

        public override void EndOperationTransaction(EndOperationTransactionArgs e)
        {
            base.EndOperationTransaction(e);
            string opKey = this.FormOperation.Operation.ToUpperInvariant();
            switch (opKey)
            {
                case "SAVE":
                    writeBackSrcAmt(e);
                    break;
                case "SUBMIT":
                    writeBackSrcAmt(e);
                    break;
                case "DELETE":
                    freeSrcAmt(e);
                    break;
            }
        }

        /// <summary>
        /// 添加校验器
        /// </summary>
        /// <param name="e"></param>
        public override void OnAddValidators(AddValidatorsEventArgs e)
        {
            string opKey = this.FormOperation.Operation.ToUpperInvariant();
            switch (opKey)
            {
                case "SAVE":
                    AddValidator(e);
                    break;
                case "SUBMIT":
                    AddValidator(e);
                    break;
            }
            
        }

        private void AddValidator(AddValidatorsEventArgs e)
        {
            var operValidator = new PerValidator();
            operValidator.AlwaysValidate = true;
            operValidator.EntityKey = "FBillHead";
            e.Validators.Add(operValidator);
        }

        #region 校验器
        private class PerValidator : AbstractValidator
        {
            public override void Validate(Kingdee.BOS.Core.ExtendedDataEntity[] dataEntities, ValidateContext validateContext, Kingdee.BOS.Context ctx)
            {

                foreach (var dataEntity in dataEntities)
                {
                    string FID = dataEntity["Id"].ToString();
                    string sql = string.Format(@"SELECT top 1 FREALPAYAMOUNTFOR, FRealMoney, FRealAmt
                    FROM ora_t_Cust100011 f
                    INNER JOIN T_AP_PAYBILLSRCENTRY src ON f.FBILLNO = src.FSRCBILLNO
                    INNER JOIN T_AP_PAYBILL p ON src.FID = p.FID
                    WHERE p.FID = '0'", FID);
                    var objs = DBUtils.ExecuteDynamicObject(this.Context, sql);
                    if(objs.Count > 0)
                    {
                        double FREALPAYAMOUNTFOR = double.Parse(objs[0]["FREALPAYAMOUNTFOR"].ToString()); //实付金额
                        double FRealMoney = double.Parse(objs[0]["FRealMoney"].ToString()); //批准金额
                        double FRealAmt = double.Parse(objs[0]["FRealAmt"].ToString()); //实际报销金额，反写
                        if (FRealAmt + FREALPAYAMOUNTFOR > FRealMoney)
                        {
                            ValidationErrorInfo ValidationErrorInfo = new ValidationErrorInfo(
                            string.Empty,
                            FID,
                            dataEntity.DataEntityIndex,
                            dataEntity.RowIndex,
                            FID,
                            "实付金额不能超出批准金额余额：" + (FRealMoney- FRealAmt).ToString("f2") + "元！",
                            string.Empty);
                            validateContext.AddError(null, ValidationErrorInfo);
                        }
                        
                    }
                    
                        
                    
                }
            }
        }

        #endregion


        #region Actions
        /// <summary>
        /// 反写对公资金源单实际金额
        /// </summary>
        private void writeBackSrcAmt(EndOperationTransactionArgs e)
        {
            foreach(var dataEntity in e.DataEntitys)
            {
                
                string FID = dataEntity["Id"].ToString();
                string sql = string.Format(@"SELECT * FROM T_AP_PAYBILL p 
                    INNER JOIN T_AP_PAYBILLSRCENTRY pe ON p.FID=pe.FID WHERE p.FID='{0}'", FID);
                var objs = DBUtils.ExecuteDynamicObject(this.Context, sql);
                if (objs.Count <= 0)
                {
                    return;
                }
                foreach (var row in objs)
                {
                    string srcType = row["FSOURCETYPE"].ToString();
                    if (srcType == "k191b3057af6c4252bcea813ff644cd3a") //对公资金
                    {
                        sql = string.Format(@"/*dialect*/ UPDATE f SET f.FRealAmt=f.FRealAmt+{0},f.FSelfRealAnt={1}*f.F_ora_Rate
                        FROM ora_t_Cust100011 f WHERE f.FBILLNO='{2}'",
                        row["FREALPAYAMOUNTFOR"].ToString(), row["FREALPAYAMOUNTFOR"].ToString(), row["FSRCBILLNO"].ToString());
                        DBUtils.Execute(this.Context, sql);
                        // 判断是否关闭源单
                        sql = string.Format(@"/*dialect*/ UPDATE f SET FCLOSESTATUS=1 FROM ora_t_Cust100011 f
                        WHERE f.FRealMoney <= f.FRealAmt AND FBILLNO='{0}'", row["FSRCBILLNO"].ToString());
                        DBUtils.Execute(this.Context, sql);
                        // 创建单据关联 --- 存在BUG
                        sql = "select FID from ora_t_Cust100011 where FBILLNO='" + row["FSRCBILLNO"].ToString() + "'";
                        var res = DBUtils.ExecuteDynamicObject(this.Context, sql);
                        createRelation(res[0]["FID"].ToString(), row["FEntryID"].ToString());
                    }
                    
                }
                
            }
        }

        /// <summary>
        /// 解除关联关系，源单实付金额反写为0
        /// </summary>
        /// <param name="e"></param>
        private void freeSrcAmt(EndOperationTransactionArgs e)
        {
            foreach (var dataEntity in e.DataEntitys)
            {
                string FID = dataEntity["Id"].ToString();
                string sql = string.Format(@"SELECT * FROM T_AP_PAYBILL p 
                    INNER JOIN T_AP_PAYBILLSRCENTRY pe ON p.FID=pe.FID WHERE p.FID='{0}'", FID); //单据头 + 源单明细表体
                var objs = DBUtils.ExecuteDynamicObject(this.Context, sql);
                if(objs.Count <= 0)
                {
                    return;
                }
                foreach (var row in objs)
                {
                    string srcType = row["FSOURCETYPE"].ToString();
                    if (srcType == "k191b3057af6c4252bcea813ff644cd3a") //对公资金
                    {
                        sql = string.Format(@"/*dialect*/ UPDATE f 
                        SET f.FRealAmt=f.FRealAmt-{0},f.FSelfRealAnt=(f.FRealAmt-{1})*f.F_ora_Rate,FCloseStatus=0
                        FROM ora_t_Cust100011 f WHERE f.FBILLNO='{2}'",
                        row["FREALPAYAMOUNTFOR"].ToString(), row["FREALPAYAMOUNTFOR"].ToString(), row["FSRCBILLNO"].ToString());
                        DBUtils.Execute(this.Context, sql);
                        deleteRelation(row["FEntryID"].ToString());
                    }
                }
                
            }
        }


        /// <summary>
        /// 创建单据关联关系
        /// </summary>
        /// <param name="FSrcID"></param>
        /// <param name="FTgtID"></param>
        private void createRelation(string FSrcID, string FTgtID)
        {
            string sql = string.Format("SELECT * FROM T_BF_INSTANCEENTRY where FTTABLENAME='T_AP_PAYBILLSRCENTRY' and FTID='{0}'", FTgtID);
            var objs = DBUtils.ExecuteDynamicObject(this.Context, sql);
            if(objs.Count > 0)
            {
                return;
            }
            string lktable = "T_AP_PAYBILLSRCENTRY_LK";                //下游单据关联表
            string targetfid = FTgtID;                                 //下游单据内码
            string targettable = "T_AP_PAYBILLSRCENTRY";               //下游单据表名
            string targetformid = "AP_PAYBILL";                        //下游单据标识
            string sourcefid = FSrcID;                                 //上游单据头内码
            string sourcetable = "ora_t_Cust100011";                       //上游单据头表名
            string sourceformid = "k191b3057af6c4252bcea813ff644cd3a"; //上游单据标识
            string sourcefentryid = "0";                               //上游单据体内码
            string sourcefentrytable = "";                             //上游单据体表名
            sql = String.Format(@"exec proc_czly_CreateBillRelation 
                   @lktable='{0}',@targetfid='{1}',@targettable='{2}',
                   @targetformid='{3}',@sourcefid='{4}',@sourcetable='{5}',
                   @sourceformid='{6}',@sourcefentryid='{7}',@sourcefentrytable='{8}'",
                   lktable, targetfid, targettable, targetformid, sourcefid, sourcetable, sourceformid, sourcefentryid, sourcefentrytable);
            DBUtils.Execute(this.Context, sql);
        }

        /// <summary>
        /// 删除关联关系
        /// </summary>
        /// <param name="FTgtID"></param>
        private void deleteRelation(string FTgtID)
        {
            string sql = string.Format("DELETE FROM T_BF_INSTANCEENTRY where FTTABLENAME='T_AP_PAYBILLSRCENTRY' and FTID='{0}'", FTgtID);
            DBUtils.Execute(this.Context, sql);
        }
        #endregion
    }
}
