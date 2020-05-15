using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Kingdee.BOS.Core.DynamicForm.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.Core.Validation;
using Kingdee.BOS.App.Data;
using Kingdee.BOS.Util;
using Kingdee.BOS.Contracts;
using Kingdee.BOS.Core.Const;
using Kingdee.BOS.Core.Metadata;
using Kingdee.BOS.Core;
using Kingdee.BOS;

namespace CEEG.CZ.OABos.HrSyncDelPermision
{
    [HotUpdate]
    [Description("HR单据同步后删除权限")]
    public class CEEG_CZ_OABos_HrSyncDelPermision : AbstractOperationServicePlugIn
    {
        public override void OnPreparePropertys(PreparePropertysEventArgs e)
        {
            base.OnPreparePropertys(e);
            e.FieldKeys.Add("DocumentStatus");
            e.FieldKeys.Add("FIsSynHR");
        }

        public override void BeginOperationTransaction(BeginOperationTransactionArgs e)
        {
            base.BeginOperationTransaction(e);
        }

        /// <summary>
        /// 添加校验器
        /// </summary>
        /// <param name="e"></param>
        public override void OnAddValidators(AddValidatorsEventArgs e)
        {
            var operValidator = new PerValidator();
            operValidator.AlwaysValidate = true;
            operValidator.EntityKey = "FBillHead";
            e.Validators.Add(operValidator);
        }

        private class PerValidator : AbstractValidator
        {
            public override void Validate(ExtendedDataEntity[] dataEntities, ValidateContext validateContext, Context ctx)
            {
                foreach (var dataEntity in dataEntities)
                {
                    string _FIsSynHR = dataEntity["FIsSynHR"].ToString();
                    if (_FIsSynHR.Equals("True"))
                    {
                        ValidationErrorInfo ValidationErrorInfo = new ValidationErrorInfo(
                            string.Empty,
                            dataEntity["Id"].ToString(),
                            dataEntity.DataEntityIndex,
                            dataEntity.RowIndex,
                            dataEntity["Id"].ToString(),
                            "单据已同步至HR，不允许反审核！",
                            string.Empty
                        );
                        validateContext.AddError(null, ValidationErrorInfo);
                    }
                }
                
            }
        }
    }
}
