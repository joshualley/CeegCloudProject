using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.ComponentModel;
using System.Data;
using Kingdee.BOS.Core.DynamicForm.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.Core.Validation;
using Kingdee.BOS.App.Data;

namespace CZ.CEEG.SrvCrm.ContractSbmt
{
    /// <summary>
    /// ora_CRM_Contract 服务端 Submit
    /// </summary>
    [Description("ora_CRM_Contract 服务端 Submit")]
    [Kingdee.BOS.Util.HotUpdate]
    public class CZ_CEEG_SrvCrm_ContractSbmt : AbstractOperationServicePlugIn
    {
        #region override
        public override void OnPreparePropertys(PreparePropertysEventArgs e)
        {
            //将需要应用的字段Key加入
            //e.FieldKeys.Add("FFORMID");
        }

        /// <summary>
        /// 添加校验器
        /// </summary>
        /// <param name="e"></param>
        public override void OnAddValidators(AddValidatorsEventArgs e)
        {
            var operValidator = new OperValidator();
            operValidator.AlwaysValidate = true;
            operValidator.EntityKey = "FBillHead";
            e.Validators.Add(operValidator);
        }

        /// <summary>
        /// 操作开始前功能处理
        /// </summary>
        /// <param name="e"></param>
        public override void BeginOperationTransaction(BeginOperationTransactionArgs e)
        {
            foreach (DynamicObject o in e.DataEntitys)
            {
                
            }
        }

        /// <summary>
        /// 操作结束后功能处理
        /// 列表批量提交时e.DataEntitys为复数个
        /// </summary>
        /// <param name="e"></param>
        public override void EndOperationTransaction(EndOperationTransactionArgs e)
        {
            //string _FID = "";
            //string _FTestTxt = "";
            /*  by:田杨
             * string _FBillInfo = this.Context.UserTransactionId;
             * _FBillInfo = {"PageId":"e2b11588-1673-48ec-9fc0-81b4c48cf775","FormId":"BOS_ListBatchTips"}
             * 其中FormId在偶然情况下值为k5f819797c98b4d7a8e28e183b99e731a 出现情况不明
             * 目前发现两种取FormId的方法  CZ_GetFormType
            */
            //

            foreach (DynamicObject o in e.DataEntitys)
            {
                //_FID = o["Id"].ToString();
                Act_DoAfterSubmit(o);
            }
            //string _prm = "";
        }

        /// <summary>
        /// 当前操作的校验器
        /// </summary>
        private class OperValidator : AbstractValidator
        {
            public override void Validate(Kingdee.BOS.Core.ExtendedDataEntity[] dataEntities, ValidateContext validateContext, Kingdee.BOS.Context ctx)
            {
                DataTable _dt;
                string _sql = "exec proc_cztyCrm_Contract @FCrmSOID='#FID#'";
                string _backM = "";
                string _backMsg = "";

                foreach (var dataEntity in dataEntities)
                {
                    string _FID = dataEntity["Id"].ToString();
                    string _vadSql = _sql.Replace("#FID#", _FID);
                    try
                    {
                        _dt = DBUtils.ExecuteDataSet(this.Context, _vadSql).Tables[0];
                        if (_dt.Rows.Count > 0)
                        {
                            _backM = _dt.Rows[0]["FBackM"].ToString();
                            _backMsg = _dt.Rows[0]["FBackMsg"].ToString();
                        }
                    }
                    catch (Exception _ex)
                    {
                        _backM = "ERR";
                        _backMsg = _ex.Message;
                    }

                    //判断到数据有错误
                    if (_backM == "ERR")
                    {
                        ValidationErrorInfo ValidationErrorInfo = new ValidationErrorInfo(
                            string.Empty,
                            dataEntity["Id"].ToString(),
                            dataEntity.DataEntityIndex,
                            dataEntity.RowIndex,
                            dataEntity["Id"].ToString(),
                            _backMsg,
                             string.Empty);
                        validateContext.AddError(null, ValidationErrorInfo);
                        continue;
                    }
                }
            }
        }
        #endregion

        #region Action
        /// <summary>
        /// 处理方法
        /// </summary>
        /// <param name="o">数据对象</param>
        private void Act_DoAfterSubmit(DynamicObject o)
        {
            /*  by:田杨
             * Id是必然携带字段，
             * 在列表执行断点测试中，BillNo｜DocumentStatus｜FCreateDate不显式声明时，随列表是否显示不传递。
             * 如需使用其他字段要在OnPreparePropertys中加入
             * 不建议使用Id以外的字段
             *  by：田杨
             *  列表测试中 FTESTTXT2字段值会直接显示 (在服务插件中改写的数据）
             *  单据测试中 FTESTTXT2字段值写入数据库，在不刷新的情况下届面值未显示更新(在服务插件中改写的数据）
            */
            string _FID = o["Id"].ToString();
            string _sql = "exec proc_cztyCrm_RndContract @FCrmSOID='" + _FID + "'";
            DBUtils.Execute(this.Context, _sql);
            //设置CRM-Bom员
            _sql = string.Format("EXEC proc_czly_GetCrmBomerByMtlGroup @CFID='{0}'", _FID);
            DBUtils.Execute(this.Context, _sql);
            // 设置产品大类组合
            _sql = @"SELECT DISTINCT FNAME FROM ora_CrmBD_MtlGroup_L ml
            INNER JOIN ora_CRM_ContractEntry cb ON ml.FID = cb.FMtlGroup WHERE cb.FID='" + _FID + "'";
            var _FMtlGroupNames = DBUtils.ExecuteDynamicObject(this.Context, _sql);
            string names = "";
            foreach(var name in _FMtlGroupNames)
            {
                names += name["FNAME"].ToString() + ",";
            }
            _sql = "UPDATE ora_CRM_Contract SET FMtlGroupNames='" + names + "' WHERE FID='" + _FID + "'";
            DBUtils.Execute(this.Context, _sql);
        }

        /// <summary>
        /// 示例代码
        /// </summary>
        /// <param name="o"></param>
        private void Act_DoAfterOperationExp(DynamicObject o)
        {
            /*  by:田杨
                 * Id是必然携带字段，
                 * 在列表执行断点测试中，BillNo｜DocumentStatus｜FCreateDate不显式声明时，随列表是否显示不传递。
                 * 如需使用其他字段要在OnPreparePropertys中加入
                 * 不建议使用Id以外的字段
                 *  by：田杨
                 *  列表测试中 FTESTTXT2字段值会直接显示
                 *  单据测试中 FTESTTXT2字段值写入数据库，在不刷新的情况下届面值未显示更新
                */
            //string _FID = o["Id"].ToString();
            //string _FFormID = o["FFormID"].ToString();
            //string _Action = this.FormOperation.Operation;
            //string _FTestTxt = o["FTestTxt"].ToString();

            //string _BI_DTONS = this.BusinessInfo.DTONS;	//"Kingdee.BOS.ServiceInterface.Temp.ora_test_Table002"
            //string[] _BI_DTONS_C = this.BusinessInfo.DTONS.Split('.');
            //string _FFormID_BI = _BI_DTONS_C[_BI_DTONS_C.Length - 1].ToString();

            //string _sql = "update " + _FFormID_BI + " set FTESTTXT2='" + _Action + " " + _FTestTxt + "' where FID='" + _FID + "'";
            //DBUtils.Execute(this.Context, _sql);
        }
        #endregion

        #region CZTY Action Base
        /// <summary>
        /// 获取单据标识 FormType | FormID
        /// 使用带参数CZ_GetFormType(o,_Key)方法，需要在BOS设计-表单中显式定义(表单属性-单据类型字段：FFormID)
        /// CLOUD表单 不同表单有两种定义 FFormID｜FObjectTypeID
        /// </summary>
        /// <param name="o">数据对象</param>
        /// <param name="_Key">Key DF_Val:FFormID</param>
        /// <returns></returns>
        private string CZ_GetFormType(DynamicObject o, string _Key)
        {
            return o[_Key].ToString();
        }

        /// <summary>
        /// 获取单据标识 FormType | FormID
        /// </summary>
        /// <returns></returns>
        private string CZ_GetFormType()
        {
            //string _BI_DTONS = this.BusinessInfo.DTONS;     //"Kingdee.BOS.ServiceInterface.Temp.ora_test_Table002"
            string[] _BI_DTONS_C = this.BusinessInfo.DTONS.Split('.');
            return _BI_DTONS_C[_BI_DTONS_C.Length - 1].ToString();
        }

        /// <summary>
        /// 获取操作
        /// </summary>
        /// <returns></returns>
        private string CZ_GetOperation()
        {
            return this.FormOperation.Operation;
        }

        /// <summary>
        /// 获取当前操作人
        /// </summary>
        /// <returns></returns>
        private string CZ_GetLcUser()
        {
            return this.Context.UserId.ToString();
        }

        /// <summary>
        /// search 基本方法
        /// </summary>
        /// <param name="_sql"></param>
        /// <returns></returns>
        private DataTable CZDB_SearchBase(string _sql)
        {
            DataTable dt;
            try
            {
                dt = DBUtils.ExecuteDataSet(this.Context, _sql).Tables[0];
                return dt;
            }
            catch (Exception _ex)
            {
                return null;
                throw _ex;
            }
        }
        #endregion
    }
}
