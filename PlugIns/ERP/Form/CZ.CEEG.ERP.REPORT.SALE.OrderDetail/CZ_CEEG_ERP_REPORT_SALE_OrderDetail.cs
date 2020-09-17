using Kingdee.BOS.App.Data;
using Kingdee.BOS.Core.DynamicForm.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.Util;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace CZ.CEEG.ERP.REPORT.SALE.OrderDetail
{
    [HotUpdate]
    [Description("订单明细")]
    public class CZ_CEEG_ERP_REPORT_SALE_OrderDetail : AbstractDynamicFormPlugIn
    {
        public override void AfterButtonClick(AfterButtonClickEventArgs e)
        {
            base.AfterButtonClick(e);
            string key = e.Key.ToUpperInvariant();
            switch (key)
            {
                case "FQUERYBTN":

                    string saleOrg = this.View.Model.GetValue("F_ora_h_sale_org") == null ? "" : this.View.Model.GetValue("F_ora_h_sale_org").ToString();
                    string FQOrderNo = this.View.Model.GetValue("FQOrderNo") == null ? "" : this.View.Model.GetValue("FQOrderNo").ToString().Trim();
                    string orderType = this.View.Model.GetValue("F_ora_h_order_type") == null ? "" : this.View.Model.GetValue("F_ora_h_order_type").ToString();
                    string purNo = this.View.Model.GetValue("FQPurNo") == null ? "" : this.View.Model.GetValue("FQPurNo").ToString();
                    string FQFactoryId = this.View.Model.GetValue("FQFactoryId") == null ? "" : (this.View.Model.GetValue("FQFactoryId") as DynamicObject)["Id"].ToString();
                    string contractMoney = this.View.Model.GetValue("F_ora_h_money") == null ? "" : this.View.Model.GetValue("F_ora_h_money").ToString();
                    string FSDate = this.View.Model.GetValue("FSDate") == null ? "" : this.View.Model.GetValue("FSDate").ToString();
                    string FEDate = this.View.Model.GetValue("FEDate") == null ? "" : this.View.Model.GetValue("FEDate").ToString();
                    string FQDeptId = this.View.Model.GetValue("FQDeptId") == null ? "" : (this.View.Model.GetValue("FQDeptId") as DynamicObject)["Id"].ToString();
                    string FQSalerId = this.View.Model.GetValue("FQSalerId") == null ? "" : (this.View.Model.GetValue("FQSalerId") as DynamicObject)["Id"].ToString();
                    string FQCustId = this.View.Model.GetValue("FQCustId") == null ? "" : (this.View.Model.GetValue("FQCustId") as DynamicObject)["Id"].ToString();
                    string matCode = this.View.Model.GetValue("F_ora_h_mat_code") == null ? "" : this.View.Model.GetValue("F_ora_h_mat_code").ToString();
                    string matName = this.View.Model.GetValue("F_ora_h_mat_name") == null ? "" : this.View.Model.GetValue("F_ora_h_mat_name").ToString();
                    string proType = this.View.Model.GetValue("F_ora_h_pro_type") == null ? "" : this.View.Model.GetValue("F_ora_h_pro_type").ToString();
                    string proCap = this.View.Model.GetValue("F_ora_h_pro_cap") == null ? "" : this.View.Model.GetValue("F_ora_h_pro_cap").ToString();


                    string saleOrgCondition = "";

                    if (!saleOrg.Equals("")) {
                        saleOrgCondition = " and t3.forgid = " + saleOrg;
                    }

                    string orderNoCondition = "";

                    if (!FQOrderNo.Equals(""))
                    {
                        saleOrgCondition = " and t1.fbillno like '%" + FQOrderNo+'%';
                    }

                    string orderTypeCondition = "";

                    if (!orderType.Equals(""))
                    {
                        orderTypeCondition = " and t1.f_cz_billtype = '" + orderType+"'";
                    }

                    string purNoCondition = "";

                    if (!purNo.Equals(""))
                    {
                        purNoCondition = " and t1.F_ORA_POORDERNO like '%" + purNo+'%';
                    }

                    string facCondition = "";

                    if (!FQFactoryId.Equals(""))
                    {
                        facCondition = " and a6.FNUMBER = " + FQFactoryId;
                    }

                    string moneyCondition = "";

                    if (!contractMoney.Equals("")&& float.Parse(contractMoney)>0)
                    {
                        moneyCondition = " and ? = " + contractMoney;
                    }
                    
                    string dateCondition = "";

                    if (!FSDate.Equals("") && !FEDate.Equals(""))
                    {
                        dateCondition = "and t1.FDATE between " + FSDate + " and " + FEDate;
                    }
                    else if (!FSDate.Equals("")){
                        dateCondition = "and t1.FDATE >= " + FSDate;
                    }
                    else if (!FEDate.Equals("")){
                        dateCondition = "and t1.FDATE <= " + FEDate;
                    }

                    string depCondition = "";

                    if (!FQDeptId.Equals(""))
                    {
                        depCondition = " and t1.FSALEDEPTID = " + FQDeptId;
                    }

                    string salerCondition = "";

                    if (!FQSalerId.Equals(""))
                    {
                        salerCondition = " and t1.FSALERID = " + FQSalerId;
                    }

                    string cusCondition = "";

                    if (!FQCustId.Equals(""))
                    {
                        cusCondition = " and t4.FNUMBER = " + FQCustId;
                    }

                    string matCodeCondition = "";

                    if (!matCode.Equals(""))
                    {
                        matCodeCondition = " and t6.FNUMBER like '%" + matCode+"%";
                    }

                    string matNameCondition = "";

                    if (!matName.Equals(""))
                    {
                        matNameCondition = " and r4.FNAME like '%" + matName+"%";
                    }
                    
                    

                    string sql = "select SUBSTRING(CONVERT(varchar(100), t1.FDATE, 111),1,7) 月份,t1.FDATE 日期,t1.FBILLNO 销售订单号,F_ORA_POORDERNO  采购订单号,a7.FDELIVERYDATE 要货日期," +
                        "t3.FNAME 销售组织,t8.FCAPTION 订单类型,t9.FNAME 办事处,case when a1.FNAME is null then F_CZ_OldSaler else a1.FNAME end 销售员," +
                        "t4.FNUMBER 客户编码,r1.FNAME 客户名称,a2.FDATAVALUE 客户行业, t1.F_CZ_PrjName 项目名称,case when r2.FNAME is null then F_CZ_Prepay else r2.FNAME end     收款条件," +
                        "t6.FNUMBER 物料编码,r4.FNAME 物料名称,t2.F_CZ_CustItemName 客户物料名称,t2.FQTY 订单数量, r5.FNAME 单位," +
                        "a3.FTAXPRICE 含税单价,t2.FQTY*a3.FTAXPRICE 订单小计金额,a3.FTAXAMOUNT 价税合计,t2.F_CZ_FBPAmt 行基价金额,round(t2.F_CZ_FBPAmt / t2.FQTY, 6) 单台基价," +
                        " t2.F_CZ_FBPAmt 总基价,t2.F_CZ_BRangeAmtGP 单台扣款,t2.F_CZ_FBRangeAmtGP 汇总扣款,a4.FDELIQTY 发货数量,round(a4.FDELIQTY * FTAXPRICE, 2) 发货金额,t2.FBDownPoints 下浮比例," +
                        "case when convert(decimal(10), t2.FBDownPoints) = 0 then '基价订单'when convert(decimal(10),t2.FBDownPoints)> 0 then '超价订单'else '特价订单' end 价格区间," +
                        "case when t1.F_ora_SCYJ = 1 then '是' else '否' end 是否上传原件,a6.FNUMBER 工厂代码,a5.FNAME 工厂 from T_SAL_ORDER t1 inner join T_SAL_ORDERENTRY t2 " +
                        "on t1.FID = t2.FID inner join T_ORG_ORGANIZATIONS b1 " +
                        "on t1.FSALEORGID = b1.FORGID inner join T_ORG_ORGANIZATIONS_L t3 " +
                        "on t1.FSALEORGID = t3.FORGID and t3.FLOCALEID = 2052 " + 
                        " inner join T_BD_CUSTOMER t4 on t1.FCUSTID = t4.FCUSTID inner join T_BD_CUSTOMER_L r1 " +
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
                        "inner join T_SAL_ORDERENTRY_D a7 on t2.FENTRYID = a7.FENTRYID where 1=1 " + 
                        saleOrgCondition + depCondition + salerCondition + cusCondition + matCodeCondition + 
                        orderNoCondition + matNameCondition + dateCondition + 
                        orderTypeCondition + purNoCondition + facCondition + moneyCondition;


                    this.View.ShowMessage(sql);

                    var objs = DBUtils.ExecuteDynamicObject(this.Context, sql);

                    this.View.Model.DeleteEntryData("F_ora_Entity");
                    for (int i = 0; i < objs.Count; i++)
                    {
                        this.View.Model.CreateNewEntryRow("F_ora_Entity");
                        this.View.Model.SetValue("F_ora_Month", objs[i]["月份"].ToString(), i);
                        this.View.Model.SetValue("F_ora_bill_date", objs[i]["日期"].ToString(), i);
                        this.View.Model.SetValue("F_ora_saleno", objs[i]["销售订单号"].ToString(), i);
                        this.View.Model.SetValue("F_ora_purno", objs[i]["采购订单号"].ToString(), i);
                        this.View.Model.SetValue("F_ora_check_contract", objs[i]["是否上传原件"].ToString(), i);
                        this.View.Model.SetValue("F_ora_order_type", objs[i]["订单类型"].ToString(), i);
                        this.View.Model.SetValue("F_ora_req_del_date", objs[i]["要货日期"].ToString(), i);
                        this.View.Model.SetValue("F_ora_sale_org", objs[i]["销售组织"].ToString(), i);
                        this.View.Model.SetValue("F_ora_sale_chan", "xxxxx", i);
                        this.View.Model.SetValue("F_ora_office", objs[i]["办事处"].ToString(), i);
                        this.View.Model.SetValue("F_ora_region", "xxxxx", i);
                        this.View.Model.SetValue("F_ora_region_name", "xxxxx", i);
                        this.View.Model.SetValue("F_ora_cus", objs[i]["客户编码"].ToString(), i);
                        this.View.Model.SetValue("F_ora_cus_name", objs[i]["客户名称"].ToString(), i);
                        this.View.Model.SetValue("F_ora_cus_type_name", "xxxxxxxx", i);
                        this.View.Model.SetValue("F_ora_cus_ind_name", objs[i]["客户行业"].ToString(), i);
                        this.View.Model.SetValue("F_ora_pay_cond", objs[i]["收款条件"].ToString(), i);
                        this.View.Model.SetValue("F_ora_project_name", objs[i]["项目名称"].ToString(), i);
                        this.View.Model.SetValue("F_ora_cus_link", "xxxxx", i);
                        this.View.Model.SetValue("F_ora_cus_tel", "xxxxx", i);
                        this.View.Model.SetValue("F_ora_mat", objs[i]["物料编码"].ToString(), i);
                        this.View.Model.SetValue("F_ora_mat_des", objs[i]["物料名称"].ToString(), i);
                        this.View.Model.SetValue("F_ora_mat_name", objs[i]["客户物料名称"].ToString(), i);
                        this.View.Model.SetValue("F_ora_cap", "xxxxxxx", i);
                        this.View.Model.SetValue("F_ora_num", objs[i]["订单数量"].ToString(), i);
                        this.View.Model.SetValue("F_ora_unit", objs[i]["单位"].ToString(), i);
                        this.View.Model.SetValue("F_ora_price", objs[i]["含税单价"].ToString(), i);
                        this.View.Model.SetValue("F_ora_price_sum", objs[i]["订单小计金额"].ToString(), i);
                        this.View.Model.SetValue("F_ora_baseprice", objs[i]["单台基价"].ToString(), i);
                        this.View.Model.SetValue("F_ora_baseprice_sum", objs[i]["总基价"].ToString(), i);
                        this.View.Model.SetValue("F_ora_money", objs[i]["发货金额"].ToString(), i);
                        this.View.Model.SetValue("F_ora_cut", objs[i]["单台扣款"].ToString(), i);
                        this.View.Model.SetValue("F_ora_send_num", objs[i]["发货数量"].ToString(), i);
                        this.View.Model.SetValue("F_ora_send_unit", "xxxxxxx", i);
                        this.View.Model.SetValue("F_ora_send_cap", objs[i]["单台扣款"].ToString(), i);
                        this.View.Model.SetValue("F_ora_send_sum", objs[i]["发货金额"].ToString(), i);
                        this.View.Model.SetValue("F_ora_dec", objs[i]["下浮比例"].ToString(), i);
                        this.View.Model.SetValue("F_ora_price_region", objs[i]["价格区间"].ToString(), i);
                        this.View.Model.SetValue("F_ora_fac", objs[i]["工厂代码"].ToString(), i);
                        this.View.Model.SetValue("F_ora_fac_des", objs[i]["工厂"].ToString(), i);
                        this.View.Model.SetValue("F_ora_pro1_desc", "xxxxxxx", i);
                        this.View.Model.SetValue("F_ora_pro2_desc", "xxxxxxxx", i);
                        this.View.Model.SetValue("F_ora_saler", "xxxxxx", i);
                        this.View.Model.SetValue("F_ora_saler_name", objs[i]["销售员"].ToString(), i);
                        this.View.Model.SetValue("F_ora_order_rej", "xxxxxxx", i);
                        this.View.Model.SetValue("F_ora_order_rej_rea", "xxxxxxxxxx", i);
                        this.View.Model.SetValue("F_ora_creator", "xxxxxxxx", i);
                        this.View.Model.SetValue("F_ora_creator_name", "xxxxxxxxx", i);
                        
                    }

                    this.View.UpdateView("F_ora_Entity");

                    break;
            }
        }
    }
}
