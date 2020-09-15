using Kingdee.BOS.App.Data;
using Kingdee.BOS.Core.DynamicForm.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Orm.DataEntity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CZ.CEEG.ERP.REPORT.SALE.OrderDetail
{
    public class CZ_CEEG_ERP_REPORT_SALE_OrderDetail : AbstractDynamicFormPlugIn
    {
        public override void AfterButtonClick(AfterButtonClickEventArgs e)
        {
            base.AfterButtonClick(e);
            string key = e.Key.ToUpperInvariant();
            switch (key)
            {
                case "FQUERYBTN":
                    string FSDate = this.View.Model.GetValue("FSDate") == null ? "" : this.View.Model.GetValue("FSDate").ToString();
                    string FEDate = this.View.Model.GetValue("FEDate") == null ? "" : this.View.Model.GetValue("FEDate").ToString();
                    string FQDeptId = this.View.Model.GetValue("FQDeptId") == null ? "0" : (this.View.Model.GetValue("FQDeptId") as DynamicObject)["Id"].ToString();
                    string FQSalerId = this.View.Model.GetValue("FQSalerId") == null ? "0" : (this.View.Model.GetValue("FQSalerId") as DynamicObject)["Id"].ToString();
                    string FQCustId = this.View.Model.GetValue("FQCustId") == null ? "0" : (this.View.Model.GetValue("FQCustId") as DynamicObject)["Id"].ToString();
                    string FQFactoryId = this.View.Model.GetValue("FQFactoryId") == null ? "0" : (this.View.Model.GetValue("FQFactoryId") as DynamicObject)["Id"].ToString();
                    string FQOrderNo = this.View.Model.GetValue("FQOrderNo") == null ? "" : this.View.Model.GetValue("FQOrderNo").ToString().Trim();

                    string sql = "select t1.FDATE 日期,t1.FBILLNO 销售订单号,F_ORA_POORDERNO  采购订单号,a7.FDELIVERYDATE 要货日期," +
                        "t3.FNAME 销售组织,t8.FCAPTION 订单类型,t9.FNAME 办事处,case when a1.FNAME is null then F_CZ_OldSaler else a1.FNAME end 销售员," +
                        "t4.FNUMBER 客户编码,r1.FNAME 客户名称,a2.FDATAVALUE 客户行业, t1.F_CZ_PrjName 项目名称,case when r2.FNAME is null then F_CZ_Prepay else r2.FNAME end     收款条件," +
                        "t6.FNUMBER 物料编码,r4.FNAME 物料名称,t2.F_CZ_CustItemName 客户物料名称,t2.FQTY 订单数量, r5.FNAME 单位," +
                        "a3.FTAXPRICE 含税单价,a3.FTAXAMOUNT 价税合计,t2.F_CZ_FBPAmt 行基价金额,round(t2.F_CZ_FBPAmt / t2.FQTY, 6) 单台基价," +
                        "t2.F_CZ_BRangeAmtGP 单台扣款,t2.F_CZ_FBRangeAmtGP 汇总扣款,a4.FDELIQTY 发货数量,round(a4.FDELIQTY * FTAXPRICE, 2) 发货金额,t2.FBDownPoints 下浮比例," +
                        "case when convert(decimal(10), t2.FBDownPoints) = 0 then '基价订单'when convert(decimal(10),t2.FBDownPoints)> 0 then '超价订单'else '特价订单' end 价格区间," +
                        "case when t1.F_ora_SCYJ = 1 then '是' else '否' end 是否上传原件,a6.FNUMBER 工厂代码,a5.FNAME 工厂 from T_SAL_ORDER t1 inner join T_SAL_ORDERENTRY t2 " +
                        "on t1.FID = t2.FID--and t2.FMRPCLOSESTATUS = 'A' and FMRPTERMINATESTATUS = 'A' inner join T_ORG_ORGANIZATIONS b1 " +
                        "on t1.FSALEORGID = b1.FORGID inner join T_ORG_ORGANIZATIONS_L t3 " +
                        "on t1.FSALEORGID = t3.FORGID and t3.FLOCALEID = 2052 and t1.FDATE between '2020-04-01' and '2020-04-15' " +
                        "inner join T_BD_CUSTOMER t4 on t1.FCUSTID = t4.FCUSTID inner join T_BD_CUSTOMER_L r1 " +
                        "on t4.FCUSTID = r1.FCUSTID inner join T_SAL_ORDERFIN r3 on r3.FID = t1.FID inner join T_BD_UNIT_L r5 on t2.FUNITID = r5.FUNITID " +
                        "left join T_BD_RECCONDITION t5 on r3.FRECCONDITIONID = t5.FID left join T_BD_RECCONDITION_L r2 on r2.FID = t5.FID " +
                        "inner join T_BD_MATERIAL t6 on t2.FMATERIALID = t6.FMATERIALID inner join T_BD_MATERIAL_L r4 on t6.FMATERIALID = r4.FMATERIALID " +
                        "left join T_META_FORMENUMITEM t7 on t1.F_CZ_BILLTYPE = t7.FVALUE " +
                        "left join T_META_FORMENUMITEM_L t8 on t7.FENUMID = t8.FENUMID " +
                        "left join T_BD_DEPARTMENT_L t9 on t1.FSALEDEPTID = t9.FDEPTID " +
                        "left join V_BD_SALESMAN_L a1 on a1.fid = t1.FSALERID " +
                        "left join T_BAS_ASSISTANTDATAENTRY_L a2 on t4.F_ora_Assistant = a2.FENTRYID " +
                        "inner join T_SAL_ORDERENTRY_F a3 on t2.FENTRYID = a3.FENTRYID " +
                        "inner join T_SAL_ORDERENTRY_R a4 on a3.FENTRYID = a4.FENTRYID " +
                        "inner join T_ORG_ORGANIZATIONS_L a5 on t2.FOWNERID = a5.FORGID and a5.FLOCALEID = 2052 " +
                        "inner join T_ORG_ORGANIZATIONS a6 on t2.FOWNERID = a6.FORGID " +
                        "inner join T_SAL_ORDERENTRY_D a7 on t2.FENTRYID = a7.FENTRYID";

                    var objs = DBUtils.ExecuteDynamicObject(this.Context, sql);

                    this.View.Model.DeleteEntryData("F_ora_Entity");
                    for (int i = 0; i < objs.Count; i++)
                    {
                        this.View.Model.CreateNewEntryRow("F_ora_Entity");
                        this.View.Model.SetValue("F_ora_saleno", objs[i]["销售订单号"].ToString(), i);
                    }

                    break;
            }
        }
    }
}
