using Kingdee.BOS.Core.Bill.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.Util;
using System.ComponentModel;

namespace CEEG.BOS.emp.collect
{
    [Description("员工汇总"), HotUpdate]

    #region var
    //string 
    #endregion


    public class Class1 : AbstractBillPlugIn
    {
        public override void DataChanged(DataChangedEventArgs e)
        {
            if (e.Field.Key == "F_ora_MulBase")
                //if (e.Key.Equals("F_ora_MulBase"))
                EmpListChange();

        }

        public override void AfterDeleteRow(AfterDeleteRowEventArgs e)
        {
            EmpListChange();
        }

        /// <summary>
        /// 汇总员工信息
        /// </summary>
        public void EmpListChange()
        {
            DynamicObjectCollection FEmps = null;
            int len = this.View.Model.GetEntryRowCount("FEntity");
            for (int i = 0; i < len; i++)
            {
                DynamicObjectCollection emps = this.View.Model.GetValue("F_ora_MulBase", i) as DynamicObjectCollection;
                for (int j = 0; j < emps.Count; j++)
                {
                    if (FEmps == null)
                    {
                        FEmps = new DynamicObjectCollection(emps[j].DynamicObjectType);
                    }
                    int k;
                    for (k = 0; k < FEmps.Count; k++)
                    {
                        if (FEmps[k]["F_ora_MulBase_Id"].Equals(emps[j]["F_ora_MulBase_Id"]))
                        {
                            break;
                        }
                    }
                    if (k >= FEmps.Count)
                    {
                        FEmps.Add(emps[j]);
                    }
                }
            }
            this.View.Model.SetValue("FEmps", FEmps);
        }
    }
}
