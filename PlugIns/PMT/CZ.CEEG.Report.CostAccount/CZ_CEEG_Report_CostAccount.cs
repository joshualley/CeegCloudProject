using Kingdee.BOS;
using Kingdee.BOS.App.Data;
using Kingdee.BOS.Core.DynamicForm;
using Kingdee.BOS.Core.DynamicForm.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Core.DynamicForm.PlugIn.ControlModel;
using Kingdee.BOS.Core.Metadata;
using Kingdee.BOS.Core.Metadata.ControlElement;
using Kingdee.BOS.Core.Metadata.EntityElement;
using Kingdee.BOS.Core.Metadata.FieldElement;
using Kingdee.BOS.Core.Metadata.GroupElement;
using Kingdee.BOS.Util;
using System;
using System.ComponentModel;
using System.Data;

namespace CZ.CEEG.Report.CostAccount
{
    [Description("费用台账报表构建")]
    [HotUpdate]
    public class CZ_CEEG_Report_CostAccount : AbstractDynamicFormPlugIn
    {
        private BusinessInfo _currBusinessInfo;
        private LayoutInfo _currLayoutInfo;
        private DataTable entityData;

        public override void OnSetBusinessInfo(SetBusinessInfoArgs e)
        {
            base.OnSetBusinessInfo(e);

            FormMetadata currmetadata = (FormMetadata)ObjectUtils.CreateCopy(this.View.OpenParameter.FormMetaData);
            _currBusinessInfo = currmetadata.BusinessInfo;
            _currLayoutInfo = currmetadata.GetLayoutInfo();
            // 获取单据体表格的元数据及布局
            string entityKey = "FEntity";
            Entity entity = _currBusinessInfo.GetEntity(entityKey);
            //EntityAppearance entityApp = _currLayoutInfo.GetEntityAppearance(entityKey);

            string FSDate = this.View.OpenParameter.GetCustomParameter("FSDate") == null ? "" : 
                this.View.OpenParameter.GetCustomParameter("FSDate").ToString();
            string FEDate = this.View.OpenParameter.GetCustomParameter("FEDate") == null ? "" : 
                this.View.OpenParameter.GetCustomParameter("FEDate").ToString();
            string FOrgId = this.View.OpenParameter.GetCustomParameter("FOrgId") == null ? "0" :
                this.View.OpenParameter.GetCustomParameter("FOrgId").ToString();
            string FDeptID = this.View.OpenParameter.GetCustomParameter("FDeptID") == null ? "0" :
                this.View.OpenParameter.GetCustomParameter("FDeptID").ToString();
            string FAccountId = this.View.OpenParameter.GetCustomParameter("FAccountId") == null ? "0" :
                this.View.OpenParameter.GetCustomParameter("FAccountId").ToString();

            string sql = string.Format(@"EXEC proc_czly_AccountDept @SDt='{0}', @EDt='{1}', 
@FOrgId='{2}', @FDeptId='{3}', @FAccountId='{4}'",
                    FSDate, FEDate, FOrgId, FDeptID, FAccountId);


            Field textField = _currBusinessInfo.GetField("FField");
            Field decimalField = _currBusinessInfo.GetField("FDecimal");
            //var textApp = _currLayoutInfo.GetEntityAppearance("FField");

            entityData = DBUtils.ExecuteDataSet(this.Context, sql).Tables[0];
            for (int i = 0; i < entityData.Columns.Count; i++)
            {
                string name = "FField" + (i + 1).ToString();
                //Field field = new Field();
                Field field;
                if (i == 0)
                {
                    field = (Field)ObjectUtils.CreateCopy(textField);
                }
                else
                {
                    field = (Field)ObjectUtils.CreateCopy(decimalField);
                    // 增加合计列
                    GroupSumColumn sumColumn = new GroupSumColumn();
                    sumColumn.FieldKey = name;
                    sumColumn.Precision = -1;
                    sumColumn.SumType = 1;
                    entity.GroupColumnInfo.AddGroupSumColumn(sumColumn);
                }
                 
                field.DynamicProperty = null;
                //field.ElementType = ElementType.BarItemElementType_TextField;
                field.Entity = entity;
                field.EntityKey = entityKey;
                field.Key = name;
                field.FieldName = name;
                field.PropertyName = name;
                field.Name = new LocaleValue(name);
                _currBusinessInfo.Add(field);
            }

            _currBusinessInfo.Remove(textField);
            _currBusinessInfo.Remove(decimalField);
            // 强制要求重新构建单据的ORM模型
            _currBusinessInfo.GetDynamicObjectType(true);

            // 输出动态调整后的单据逻辑元数据模型(BusinessInfo)
            e.BusinessInfo = _currBusinessInfo;
            e.BillBusinessInfo = _currBusinessInfo;
        }

        public override void OnSetLayoutInfo(SetLayoutInfoArgs e)
        {
            base.OnSetLayoutInfo(e);
            // 获取单据体表格的元数据及布局
            string entityKey = "FEntity";
            //Entity entity = _currBusinessInfo.GetEntity(entityKey);
            EntityAppearance entityApp = _currLayoutInfo.GetEntityAppearance(entityKey);
            Entity entity = entityApp.Entity;
            var textApp = _currLayoutInfo.GetFieldAppearance("FField");
            var decimalApp = _currLayoutInfo.GetFieldAppearance("FDecimal");

            for (int i = 0; i < entityData.Columns.Count; i++)
            {
                string name = "FField" + (i + 1).ToString();
                //FieldAppearance field = new FieldAppearance();
                FieldAppearance field;
                if (i == 0)
                {
                    field = (FieldAppearance)ObjectUtils.CreateCopy(textApp);
                }
                else
                {
                    field = (FieldAppearance)ObjectUtils.CreateCopy(decimalApp);
                    //添加合计列
                    GroupSumColumn sumColumn = new GroupSumColumn();
                    sumColumn.FieldKey = name;
                    sumColumn.Precision = -1;
                    sumColumn.SumType = 1;
                    entity.GroupColumnInfo.AddGroupSumColumn(sumColumn);
                }
                field.Key = name;
                field.Caption = new LocaleValue(entityData.Columns[i].ColumnName);
                field.Field = _currBusinessInfo.GetField(name);
                field.Tabindex = i + 1;
                _currLayoutInfo.Add(field);

                
            }

            _currLayoutInfo.Remove(textApp);
            _currLayoutInfo.Remove(decimalApp);

            entityApp.Layoutinfo.Sort();
            e.LayoutInfo = _currLayoutInfo;

            EntryGrid grid = this.View.GetControl<EntryGrid>("FEntity");
            grid.SetCustomPropertyValue("AllowLayoutSetting", false);
            grid.CreateDyanmicList(_currLayoutInfo.GetEntityAppearance("FEntity"));
            this.View.SendDynamicFormAction(this.View);
        }

        public override void AfterBindData(EventArgs e)
        {
            base.AfterBindData(e);
            this.View.Model.BatchCreateNewEntryRow("FEntity", entityData.Rows.Count);
            for (int i = 0; i < entityData.Rows.Count; i++)
            {
                
                for (int j = 0; j < entityData.Columns.Count; j++)
                {
                    string name = "FField" + (j + 1).ToString();
                    this.View.Model.SetValue(name, entityData.Rows[i][entityData.Columns[j].ColumnName], i);
                }
            }
            this.View.UpdateView("FEntity");
        }

        public override void EntityRowDoubleClick(EntityRowClickEventArgs e)
        {
            base.EntityRowDoubleClick(e);

            string FDeptName = this.View.Model.GetValue("FField1", e.Row) == null ? "" :
                this.View.Model.GetValue("FField1", e.Row).ToString();

            if (FDeptName == "")
            {
                return;
            }

            string FSDate = this.View.OpenParameter.GetCustomParameter("FSDate") == null ? "" :
                this.View.OpenParameter.GetCustomParameter("FSDate").ToString();
            string FEDate = this.View.OpenParameter.GetCustomParameter("FEDate") == null ? "" :
                this.View.OpenParameter.GetCustomParameter("FEDate").ToString();
            string FOrgId = this.View.OpenParameter.GetCustomParameter("FOrgId") == null ? "0" :
                this.View.OpenParameter.GetCustomParameter("FOrgId").ToString();
            string FAccountId = this.View.OpenParameter.GetCustomParameter("FAccountId") == null ? "0" :
                this.View.OpenParameter.GetCustomParameter("FAccountId").ToString();

            DynamicFormShowParameter param = new DynamicFormShowParameter();
            param.ParentPageId = this.View.PageId;
            param.FormId = "ora_VounterDetail";
            param.OpenStyle.ShowType = ShowType.Modal;

            param.CustomParams.Add("FSDate", FSDate);
            param.CustomParams.Add("FEDate", FEDate);
            param.CustomParams.Add("FOrgId", FOrgId);
            param.CustomParams.Add("FAccountId", FAccountId);
            param.CustomParams.Add("FDeptName", FDeptName);

            this.View.ShowForm(param);
        }
    }
}
