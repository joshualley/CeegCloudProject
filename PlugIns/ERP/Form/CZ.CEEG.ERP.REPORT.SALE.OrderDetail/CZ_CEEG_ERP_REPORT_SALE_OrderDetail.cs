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
                    string isReject = this.View.Model.GetValue("F_ora_h_is_reject") == null ? "" : this.View.Model.GetValue("F_ora_h_is_reject").ToString();
                    string projectName = this.View.Model.GetValue("F_ora_h_project_name") == null ? "" : this.View.Model.GetValue("F_ora_h_project_name").ToString();
                    string priceZone = this.View.Model.GetValue("F_ora_h_price_zone") == null ? "" : this.View.Model.GetValue("F_ora_h_price_zone").ToString();
                    string rejectReason = this.View.Model.GetValue("F_ora_h_reject_reason") == null ? "" : this.View.Model.GetValue("F_ora_h_reject_reason").ToString();
                    string cusCreateTime = this.View.Model.GetValue("F_ora_h_cus_create_time") == null ? "" : this.View.Model.GetValue("F_ora_h_cus_create_time").ToString();
                    string cusCreateTime2 = this.View.Model.GetValue("F_ora_h_cus_create_time2") == null ? "" : this.View.Model.GetValue("F_ora_h_cus_create_time2").ToString();
                    string recConCode = this.View.Model.GetValue("F_ora_h_rec_con_code") == null ? "" : this.View.Model.GetValue("F_ora_h_rec_con_code").ToString();
                    string recConDesc = this.View.Model.GetValue("F_ora_h_rec_con_desc") == null ? "" : this.View.Model.GetValue("F_ora_h_rec_con_desc").ToString();


                    if (!FSDate.Equals("") && !FEDate.Equals(""))
                    {
                        FEDate = FEDate.Replace("00:00:00", "23:59:59");
                    }
                    else if (!FSDate.Equals(""))
                    {
                        FEDate = "2999-01-01 23:59:59";
                    }
                    else if (!FEDate.Equals(""))
                    {
                        FSDate = "1900-01-01 00:00:00";
                    }


                    string salers = "";

                    if (FQSalerIds != null && FQSalerIds.Count > 0)
                    {
                        
                        for (int i = 0; i < FQSalerIds.Count; i++)
                        {
                            salers = salers + "," + (FQSalerIds[i]["FQSalerId"] as DynamicObject)["Number"];
                        }
                        if (!salers.Equals(""))
                        {
                            salers = salers.Substring(1);
                        }

                    }

                    string cus = "";

                    if (FQCustIds != null && FQCustIds.Count > 0)
                    {            
                        for (int i = 0; i < FQCustIds.Count; i++)
                        {
                            cus = cus + "," + (FQCustIds[i]["FQCustId"] as DynamicObject)["Id"];
                        }
                        if (!cus.Equals(""))
                        {
                            cus = cus.Substring(1);
                        }
                    }

                    string orgs = "";

                    if (saleOrgs != null && saleOrgs.Count > 0)
                    {                   
                        for (int i = 0; i < saleOrgs.Count; i++)
                        {
                            orgs = orgs + "," + (saleOrgs[i]["F_ora_h_sale_org"] as DynamicObject)["Number"];
                        }
                        if (!orgs.Equals(""))
                        {
                            orgs = orgs.Substring(1);
                        }
                    }

                    string deps = "";

                    if (FQDeptIds != null && FQDeptIds.Count > 0)
                    {                 
                        for (int i = 0; i < FQDeptIds.Count; i++)
                        {
                            deps = deps + "," + (FQDeptIds[i]["FQDeptId"] as DynamicObject)["Id"];
                        }
                        if (!deps.Equals(""))
                        {
                            deps = deps.Substring(1);
                        }
                    }

                    string facs = "";

                    if (FQFactoryIds != null && FQFactoryIds.Count > 0)
                    {                     
                        for (int i = 0; i < FQFactoryIds.Count; i++)
                        {
                            facs = facs + "," + (FQFactoryIds[i]["FQFactoryId"] as DynamicObject)["Number"];
                        }
                        if (!facs.Equals(""))
                        {
                            facs = facs.Substring(1);
                        }
                    }

                    string sql = string.Format("EXEC proc_czly_OrderDetail_plugin @QSDt='{0}', @QEDt='{1}', " +
                        "@QOrderNo = '{2}', @QOrderType = '{3}', " +
                        "@QSalerNo = '{4}', @QCustNo = '{5}', @QProdType = '{6}', @QVoltageLevel = '{7}', " +
                        "@QSaleOrgNo = '{8}', @QDeptNo = '{9}', @QStockOrgNo = '{10}'," +
                        "@QMaterialNo = '{11}', @QCkOrigin = '{12}', @QIsReject = '{13}'," +
                        "@QRejectReson = '{14}', @QPriceRange = '{15}'", 
                        FSDate, FEDate, FQOrderNo,
                        orderType, salers, cus, proType, proCap, 
                        orgs, deps, facs, matCode, checkFile, isReject, 
                        rejectReason, priceZone);  

                    
                    this.View.ShowMessage(sql);

                    var objs = DBUtils.ExecuteDynamicObject(this.Context, sql);

                    this.View.Model.DeleteEntryData("F_ora_Entity");

                    for (int i = 0; i < objs.Count; i++)
                    {
                        this.View.Model.CreateNewEntryRow("F_ora_Entity");
                        this.View.Model.SetValue("F_ora_Month", ColFormat(objs[i]["FYearMonth"]), i);
                        this.View.Model.SetValue("F_ora_bill_date", ColFormat(objs[i]["FDate"]), i);
                        this.View.Model.SetValue("F_ora_saleno", ColFormat(objs[i]["FBillNo"]), i);
                        //this.View.Model.SetValue("F_ora_purno", ColFormat(objs[i]["采购订单号"]), i);
                        this.View.Model.SetValue("F_ora_check_contract", ColFormat(objs[i]["FCkOrigin"]), i);
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
                        this.View.Model.SetValue("F_ora_rec_con_code", ColFormat(objs[i]["收款条件编码"]), i);
                        this.View.Model.SetValue("F_ora_cus_create_time", ColFormat(objs[i]["客户创建时间"]), i);
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
