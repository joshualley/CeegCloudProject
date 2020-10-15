using Kingdee.BOS.App.Data;
using Kingdee.BOS.Business.Bill.Operation;
using Kingdee.BOS.Core.Bill;
using Kingdee.BOS.Core.DynamicForm;
using Kingdee.BOS.Core.DynamicForm.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Core.Metadata;
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

        public override void EntityRowDoubleClick(EntityRowClickEventArgs e)
        {
            base.EntityRowDoubleClick(e);
            string key = e.ColKey.ToUpperInvariant();
            switch (key)
            {
                case "F_ORA_SALENO":
                    var para = new BillShowParameter();
                    para.FormId = "SAL_SaleOrder";
                    para.OpenStyle.ShowType = ShowType.Modal;
                    para.ParentPageId = this.View.PageId;
                    para.Status = OperationStatus.VIEW;
                    para.PKey = this.Model.GetValue("F_ora_inner_code", e.Row).ToString();
                    this.View.ShowForm(para);
                    break;
            }
        }

        public override void AfterButtonClick(AfterButtonClickEventArgs e)
        {
            base.AfterButtonClick(e);
            string key = e.Key.ToUpperInvariant();
            switch (key)
            {
                case "FQUERYBTN":

                    DynamicObjectCollection saleOrgs = (this.View.Model.GetValue("F_ora_h_sale_org") as DynamicObjectCollection);
                    string FQOrderNo = this.View.Model.GetValue("FQOrderNo") == null ? "" : this.View.Model.GetValue("FQOrderNo").ToString().Trim();
                    string orderType = this.View.Model.GetValue("F_ora_h_order_type") == null ? "" : this.View.Model.GetValue("F_ora_h_order_type").ToString();
                    string purNo = this.View.Model.GetValue("FQPurNo") == null ? "" : this.View.Model.GetValue("FQPurNo").ToString();
                    DynamicObjectCollection FQFactoryIds = (this.View.Model.GetValue("FQFactoryId") as DynamicObjectCollection);
                    string contractMoney = this.View.Model.GetValue("F_ora_h_money") == null ? "" : this.View.Model.GetValue("F_ora_h_money").ToString();
                    string FSDate = this.View.Model.GetValue("FSDate") == null ? "" : this.View.Model.GetValue("FSDate").ToString();
                    string FEDate = this.View.Model.GetValue("FEDate") == null ? "" : this.View.Model.GetValue("FEDate").ToString();
                    DynamicObjectCollection FQDeptIds = (this.View.Model.GetValue("FQDeptId") as DynamicObjectCollection);
                    DynamicObjectCollection FQSalerIds = (this.View.Model.GetValue("FQSalerId") as DynamicObjectCollection);
                    DynamicObjectCollection FQCustIds = (this.View.Model.GetValue("FQCustId") as DynamicObjectCollection);
                    string matCode = this.View.Model.GetValue("F_ora_h_mat_code") == null ? "" : this.View.Model.GetValue("F_ora_h_mat_code").ToString();
                    string matName = this.View.Model.GetValue("F_ora_h_mat_name") == null ? "" : this.View.Model.GetValue("F_ora_h_mat_name").ToString();
                    string proType = this.View.Model.GetValue("F_ora_h_pro_type") == null ? "" : this.View.Model.GetValue("F_ora_h_pro_type").ToString();
                    string proCap = this.View.Model.GetValue("F_ora_h_pro_cap") == null ? "" : this.View.Model.GetValue("F_ora_h_pro_cap").ToString();
                    string checkFile = this.View.Model.GetValue("F_ora_h_check_file") == null ? "" : this.View.Model.GetValue("F_ora_h_check_file").ToString();
                    string projectName = this.View.Model.GetValue("F_ora_h_project_name") == null ? "" : this.View.Model.GetValue("F_ora_h_project_name").ToString();
                    string priceZone = this.View.Model.GetValue("F_ora_h_price_zone") == null ? "" : this.View.Model.GetValue("F_ora_h_price_zone").ToString();
                    string rejectReason = this.View.Model.GetValue("F_ora_h_reject_reason") == null ? "" : this.View.Model.GetValue("F_ora_h_reject_reason").ToString();


                    string saleOrgCondition = "";

                    if (saleOrgs != null && saleOrgs.Count > 0)
                    {
                        string orgs = "";
                        for (int i = 0; i < saleOrgs.Count; i++)
                        {
                            orgs = orgs + ",'" + (saleOrgs[i]["F_ora_h_sale_org"] as DynamicObject)["Name"] + "'";
                        }
                        saleOrgCondition = " and t3.FNAME  in (" + orgs.Substring(1) + ")";
                    }


                    string orderNoCondition = "";

                    if (!FQOrderNo.Equals(""))
                    {
                        saleOrgCondition = " and t1.fbillno like '%" + FQOrderNo + "%'";
                    }

                    string orderTypeCondition = "";

                    if (!orderType.Equals(""))
                    {
                        orderTypeCondition = " and t1.f_cz_billtype = '" + orderType + "'";
                    }

                    string purNoCondition = "";

                    if (!purNo.Equals(""))
                    {
                        purNoCondition = " and t1.F_ORA_POORDERNO like '%" + purNo + "%'";
                    }

                    string facCondition = "";

                    if (FQFactoryIds != null && FQFactoryIds.Count > 0)
                    {
                        string facs = "";
                        for (int i = 0; i < FQFactoryIds.Count; i++)
                        {
                            facs = facs + ",'" + (FQFactoryIds[i]["FQFactoryId"] as DynamicObject)["Number"] + "'";
                        }
                        facCondition = " and a6.FNUMBER  in (" + facs.Substring(1) + ")";
                    }

                    string moneyCondition = "";

                    if (!contractMoney.Equals("") && float.Parse(contractMoney) > 0)
                    {
                        moneyCondition = " and r3.FBILLALLAMOUNT = " + contractMoney;
                    }

                    string dateCondition = "";

                    if (!FSDate.Equals("") && !FEDate.Equals(""))
                    {
                        dateCondition = " and t1.FDATE between '" + FSDate + "' and '" + FEDate+"'";
                    }
                    else if (!FSDate.Equals("")) {
                        dateCondition = " and t1.FDATE >= '" + FSDate + "'";
                    }
                    else if (!FEDate.Equals("")) {
                        dateCondition = " and t1.FDATE <= '" + FEDate + "'";
                    }

                    string depCondition = "";

                    if (FQDeptIds != null && FQDeptIds.Count > 0)
                    {
                        string deps = "";
                        for (int i = 0; i < FQDeptIds.Count; i++)
                        {
                            deps = deps + ",'" + (FQDeptIds[i]["FQDeptId"] as DynamicObject)["Id"] + "'";
                        }
                        depCondition = " and t1.FSALEDEPTID  in (" + deps.Substring(1) + ")";
                    }


                    string salerCondition = "";

                    if (FQSalerIds != null && FQSalerIds.Count > 0)
                    {
                        string salers = "";
                        for (int i = 0; i < FQSalerIds.Count; i++)
                        {
                            salers = salers + ",'" + (FQSalerIds[i]["FQSalerId"] as DynamicObject)["Number"] + "'";
                        }
                        salerCondition = " and a11.FNUMBER  in (" + salers.Substring(1) + ")";
                    }


                    string cusCondition = "";

                    if (FQCustIds != null && FQCustIds.Count > 0)
                    {
                        string cus = "";
                        for (int i = 0; i < FQCustIds.Count; i++)
                        {
                            cus = cus + ",'" + (FQCustIds[i]["FQCustId"] as DynamicObject)["Id"] + "'";
                        }
                        cusCondition = " and t1.FCUSTID  in (" + cus.Substring(1) + ")";
                    }

                    string matCodeCondition = "";

                    if (!matCode.Equals(""))
                    {
                        matCodeCondition = " and t6.FNUMBER like '%" + matCode + "%'";
                    }

                    string matNameCondition = "";

                    if (!matName.Equals(""))
                    {
                        matNameCondition = " and t2.F_CZ_CustItemName like '%" + matName + "%'";
                    }

                    string proTypeCondition = "";

                    if (!proType.Equals(""))
                    {
                        proTypeCondition = " and ta.FDATAVALUE like '%" + proType + "%'";
                    }

                    string proCapCondition = "";

                    if (!proCap.Equals(""))
                    {
                        proCapCondition = " and ta1.FDATAVALUE like '%" + proCap + "%'";
                    }

                    string checkFileCondition = "";

                    if (!checkFile.Equals(""))
                    {
                        checkFileCondition = " and t1.F_ora_SCYJ = '" + checkFile + "'";
                    }

                    string projectNameCondition = "";

                    if (!projectName.Equals(""))
                    {
                        projectNameCondition = " and t1.F_CZ_PrjName = '" + projectName + "'";
                    }

                    string priceZoneCondition = "";

                    if (!priceZone.Equals(""))
                    {
                        switch (priceZone) {
                            case "1": priceZoneCondition = " and  convert(decimal(10),t2.FBDownPoints)<=-11 ";break;
                            case "2": priceZoneCondition = " and  convert(decimal(10),t2.FBDownPoints)<0 and  convert(decimal(10),t2.FBDownPoints)>-11 "; break;
                            case "3": priceZoneCondition = " and  convert(decimal(10),t2.FBDownPoints)=0 "; break;
                            case "4": priceZoneCondition = " and  convert(decimal(10),t2.FBDownPoints)<=5 and convert(decimal(10),t2.FBDownPoints)>0 "; break;
                            case "5": priceZoneCondition = " and  convert(decimal(10),t2.FBDownPoints)<=10 and convert(decimal(10),t2.FBDownPoints)>5 "; break;
                            case "6": priceZoneCondition = " and  convert(decimal(10),t2.FBDownPoints)>10 "; break;
                            default: priceZoneCondition = "";break;
                        }                   
                    }

                    string rejectReasonCondition = "";

                    if (!rejectReason.Equals(""))
                    {
                        rejectReasonCondition = " and t2.F_ORA_JJYY = '" + rejectReason + "'";
                    }


                    string sql = "/*dialect*/ select SUBSTRING(CONVERT(varchar(100), t1.FDATE, 111),1,7) 月份,t1.FDATE 日期,t1.Fid 内码,t1.FBILLNO 销售订单号,F_ORA_POORDERNO  采购订单号,a7.FDELIVERYDATE 要货日期," +
                        "t3.FNAME 销售组织,t8.FCAPTION 订单类型,t9.FNAME 办事处," +
                        "a11.FNUMBER 销售员编号," +
                        "case when a1.FNAME is null then F_CZ_OldSaler else a1.FNAME end 销售员," +
                        "t4.FNUMBER 客户编码,r1.FNAME 客户名称,a2.FDATAVALUE 客户行业, " +
                        "ac.FDATAVALUE  客户分类, " +
                        "t1.F_CZ_PrjName 项目名称,case when r2.FNAME is null then F_CZ_Prepay else r2.FNAME end     收款条件," +
                        "t6.FNUMBER 物料编码,r4.FNAME 物料描述,t2.F_CZ_CustItemName 物料名称,t2.FQTY 订单数量, r5.FNAME 单位," +
                        "a3.FTAXPRICE 含税单价,t2.FQTY*a3.FTAXPRICE 订单小计金额,a3.FTAXAMOUNT 价税合计,t2.F_CZ_FBPAmt 行基价金额,round(t2.F_CZ_FBPAmt / t2.FQTY, 6) 单台基价," +
                        " t2.F_CZ_FBPAmt 总基价,t2.F_CZ_BRangeAmtGP 单台扣款,t2.F_CZ_FBRangeAmtGP 汇总扣款,a4.FDELIQTY 发货数量,round(a4.FDELIQTY * FTAXPRICE, 2) 发货金额,t2.FBDownPoints 下浮比例," +
                        "case when convert(decimal(10),t2.FBDownPoints)<= -11 then '基价110%以上' " +
                        "when convert(decimal(10),t2.FBDownPoints)< 0 then '基价100%-110%' " +
                        "when convert(decimal(10),t2.FBDownPoints)= 0 then '基价100%' " +
                        "when convert(decimal(10),t2.FBDownPoints)<= 5 then '基价95%-100%' " +
                        "when convert(decimal(10),t2.FBDownPoints)<= 10 then '基价90%-95%' " +
                        "else '基价90%以下'  end 价格区间," +
                        "case when t2.F_ORA_JJYY = 1 then '内部原因' " +
                        "when t2.F_ORA_JJYY = 2 then '客户原因' " +
                        "when t2.F_ORA_JJYY = 3 then '质量原因' " +
                        "else '' end 拒绝原因, " +
                        "t2.F_CZ_BRANGEAMTGP 单台扣款," +
                        "t2.F_CZ_FBRANGEAMTGP 汇总扣款," +
                        "case when t1.F_ora_SCYJ = 1 then '是' else '否' end 是否上传原件,a6.FNUMBER 工厂代码,a5.FNAME 工厂," +
                        "o.FNUMBER 创建者,u.fname 创建者名称, " +
                        "t4.FTEL 客户联系电话, " +
                        "t4.FCONTRACTORNAME 客户联系人, " +
                        "ta.FDATAVALUE 产品大类, " +
                        "ta1.FDATAVALUE 产品容量, " +
                        "r3.FBILLALLAMOUNT 订单总金额 " +                               
                        "from T_SAL_ORDER t1 inner join T_SAL_ORDERENTRY t2 " +
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
                        "left join  V_BD_SALESMAN a11 on a11.fid = a1.fid " +
                        "left join T_BAS_ASSISTANTDATAENTRY_L a2 on t4.F_ora_Assistant = a2.FENTRYID " +
                        "inner join T_SAL_ORDERENTRY_F a3 on t2.FENTRYID = a3.FENTRYID " +
                        "inner join T_SAL_ORDERENTRY_R a4 on a3.FENTRYID = a4.FENTRYID " +
                        "inner join T_ORG_ORGANIZATIONS_L a5 on t2.FOWNERID = a5.FORGID and a5.FLOCALEID = 2052 " +
                        "inner join T_ORG_ORGANIZATIONS a6 on t2.FOWNERID = a6.FORGID " +
                        "inner join T_SAL_ORDERENTRY_D a7 on t2.FENTRYID = a7.FENTRYID " +
                        "left join T_SEC_USER u on u.fuserid = t1.FCREATORID " +
                        "left join V_bd_ContactObject o on u.FLinkObject = o.FID  " +
                        "left join T_BAS_ASSISTANTDATAENTRY_L ac on t4.FCUSTTYPEID=ac.FENTRYID " +
                        "left join T_BAS_ASSISTANTDATAENTRY_L ta on t6.F_ora_Assistant=ta.FENTRYID " +
                        "left join T_BAS_ASSISTANTDATAENTRY_L ta1 on t6.F_ora_Assistant1 = ta1.FENTRYID " +
                        "where 1=1 " +
                        saleOrgCondition + depCondition + salerCondition + cusCondition + matCodeCondition + checkFileCondition + rejectReasonCondition +
                        orderNoCondition + matNameCondition + dateCondition + proTypeCondition + proCapCondition + priceZoneCondition + 
                        orderTypeCondition + purNoCondition + facCondition + moneyCondition + projectNameCondition;

                    var objs = DBUtils.ExecuteDynamicObject(this.Context, sql);

                    this.View.Model.DeleteEntryData("F_ora_Entity");

                    for (int i = 0; i < objs.Count; i++)
                    {
                        this.View.Model.CreateNewEntryRow("F_ora_Entity");
                        this.View.Model.SetValue("F_ora_Month", ColFormat(objs[i]["月份"]), i);
                        this.View.Model.SetValue("F_ora_bill_date", ColFormat(objs[i]["日期"]), i);
                        this.View.Model.SetValue("F_ora_saleno", ColFormat(objs[i]["销售订单号"]), i);
                        this.View.Model.SetValue("F_ora_purno", ColFormat(objs[i]["采购订单号"]), i);
                        this.View.Model.SetValue("F_ora_check_contract", ColFormat(objs[i]["是否上传原件"]), i);
                        this.View.Model.SetValue("F_ora_order_type", ColFormat(objs[i]["订单类型"]), i);
                        this.View.Model.SetValue("F_ora_req_del_date", ColFormat(objs[i]["要货日期"]), i);
                        this.View.Model.SetValue("F_ora_sale_org", ColFormat(objs[i]["销售组织"]), i);
                        this.View.Model.SetValue("F_ora_office", ColFormat(objs[i]["办事处"]), i);
                        this.View.Model.SetValue("F_ora_cus", ColFormat(objs[i]["客户编码"]), i);
                        this.View.Model.SetValue("F_ora_cus_name", ColFormat(objs[i]["客户名称"]), i);
                        this.View.Model.SetValue("F_ora_cus_type_name", ColFormat(objs[i]["客户分类"]), i);
                        this.View.Model.SetValue("F_ora_cus_ind_name", ColFormat(objs[i]["客户行业"]), i);
                        this.View.Model.SetValue("F_ora_pay_cond", ColFormat(objs[i]["收款条件"]), i);
                        this.View.Model.SetValue("F_ora_project_name", ColFormat(objs[i]["项目名称"]), i);
                        this.View.Model.SetValue("F_ora_cus_link", ColFormat(objs[i]["客户联系人"]), i);
                        this.View.Model.SetValue("F_ora_cus_tel", ColFormat(objs[i]["客户联系电话"]), i);
                        this.View.Model.SetValue("F_ora_mat", ColFormat(objs[i]["物料编码"]), i);
                        this.View.Model.SetValue("F_ora_mat_des", ColFormat(objs[i]["物料描述"]), i);
                        this.View.Model.SetValue("F_ora_mat_name", ColFormat(objs[i]["物料名称"]), i);                    
                        this.View.Model.SetValue("F_ora_num", ColFormat(objs[i]["订单数量"]), i);
                        this.View.Model.SetValue("F_ora_unit", ColFormat(objs[i]["单位"]), i);
                        this.View.Model.SetValue("F_ora_price", ColFormat(objs[i]["含税单价"]), i);
                        this.View.Model.SetValue("F_ora_price_sum", ColFormat(objs[i]["订单小计金额"]), i);
                        this.View.Model.SetValue("F_ora_baseprice", ColFormat(objs[i]["单台基价"]), i);
                        this.View.Model.SetValue("F_ora_baseprice_sum", ColFormat(objs[i]["总基价"]), i);
                        this.View.Model.SetValue("F_ora_money", ColFormat(objs[i]["订单总金额"]), i);
                        this.View.Model.SetValue("F_ora_cut_single", ColFormat(objs[i]["单台扣款"]), i);
                        this.View.Model.SetValue("F_ora_cut", ColFormat(objs[i]["汇总扣款"]), i);                    
                        this.View.Model.SetValue("F_ora_send_unit", ColFormat(objs[i]["单位"]), i);
                        this.View.Model.SetValue("F_ora_send_num", ColFormat(objs[i]["发货数量"]), i);
                        this.View.Model.SetValue("F_ora_pro_type", ColFormat(objs[i]["产品大类"]), i);
                        this.View.Model.SetValue("F_ora_cap", ColFormat(objs[i]["产品容量"]), i);
                        this.View.Model.SetValue("F_ora_send_sum", ColFormat(objs[i]["发货金额"]), i);
                        this.View.Model.SetValue("F_ora_dec", ColFormat(objs[i]["下浮比例"]), i);
                        this.View.Model.SetValue("F_ora_price_region", ColFormat(objs[i]["价格区间"]), i);
                        this.View.Model.SetValue("F_ora_fac", ColFormat(objs[i]["工厂代码"]), i);
                        this.View.Model.SetValue("F_ora_fac_des", ColFormat(objs[i]["工厂"]), i);
                        this.View.Model.SetValue("F_ora_saler", ColFormat(objs[i]["销售员编号"]), i);
                        this.View.Model.SetValue("F_ora_saler_name", ColFormat(objs[i]["销售员"]), i);
                        this.View.Model.SetValue("F_ora_order_rej_rea", ColFormat(objs[i]["拒绝原因"]), i);
                        this.View.Model.SetValue("F_ora_creator", ColFormat(objs[i]["创建者"]), i);
                        this.View.Model.SetValue("F_ora_creator_name", ColFormat(objs[i]["创建者名称"]), i);
                        this.View.Model.SetValue("F_ora_inner_code", ColFormat(objs[i]["内码"]), i);
                    }


            this.View.UpdateView("F_ora_Entity");

                    break;

            }
        }

        private string ColFormat(object o)
        {
            if (o == null)
            {
                return "";
            }
            return o.ToString();
        }
    }
}
