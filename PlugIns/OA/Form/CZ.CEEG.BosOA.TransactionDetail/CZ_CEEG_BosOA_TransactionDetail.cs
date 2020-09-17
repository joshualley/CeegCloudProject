using Kingdee.BOS.App.Data;
using Kingdee.BOS.Core.Bill.PlugIn;
using Kingdee.BOS.Core.DynamicForm;
using Kingdee.BOS.Core.DynamicForm.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.Util;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Text;

namespace CZ.CEEG.BosOA.TransactionDetail
{
    [HotUpdate]
    [Description("获取往来明细")]
    public class CZ_CEEG_BosOA_TransactionDetail :  AbstractDynamicFormPlugIn
    {

        public override void AfterButtonClick(AfterButtonClickEventArgs e)
        {
            base.AfterButtonClick(e);
            if (e.Key.Equals("F_ORA_DETAIL_VIEW_PUBLIC"))
            {

                if (this.View.Model.GetValue("FContractParty") == null)
                {
                    this.View.ShowErrMessage("请先选择往来单位");
                }
                else
                {
                    DynamicFormShowParameter param = new DynamicFormShowParameter();
                    param.ParentPageId = this.View.PageId;
                    param.FormId = "ora_trasaction_detail_public_form";
                    param.OpenStyle.ShowType = ShowType.Modal;
                    param.CustomParams.Add("Type", this.View.Model.GetValue("FContractPartyType").ToString());
                    param.CustomParams.Add("ObjId", (this.View.Model.GetValue("FContractParty") as DynamicObject)["Id"].ToString());
                    this.View.ShowForm(param);
                }
            }
            else if (e.Key.Equals("F_ORA_DETAIL_VIEW"))
            {
                if (this.View.Model.GetValue("FApply") == null)
                {
                    this.View.ShowErrMessage("请先选择申请人");
                }
                else
                {
                    DynamicFormShowParameter param = new DynamicFormShowParameter();
                    param.ParentPageId = this.View.PageId;
                    param.FormId = "ora_transaction_detail_form";
                    param.OpenStyle.ShowType = ShowType.Modal;
                    param.CustomParams.Add("Type", "emp");
                    param.CustomParams.Add("ObjId", (this.View.Model.GetValue("FApply") as DynamicObject)["Id"].ToString());
                    this.View.ShowForm(param);
                }
            }
        }

        public override void AfterBindData(EventArgs e)
        {
            base.AfterBindData(e);

            object type = this.View.OpenParameter.GetCustomParameter("Type");

            if (type != null) {
                object objId = this.View.OpenParameter.GetCustomParameter("ObjId");

                if (objId == null)
                {
                    this.View.ShowErrMessage("无效的对象ID");
                }
                else {

                    if (type.ToString().Equals("emp"))
                    {                  
                        CZ_FB_GetObjBalDetail(CZ_FB_EnFBObject.BD_Empinfo, objId.ToString());
                    }else if (type.ToString().Equals("BD_Supplier"))
                    {
                        CZ_FB_GetObjBalDetail(CZ_FB_EnFBObject.BD_Supplier, objId.ToString());
                    }
                    else if(type.ToString().Equals("BD_Customer"))
                    {
                        CZ_FB_GetObjBalDetail(CZ_FB_EnFBObject.BD_Customer, objId.ToString());
                    }
                }           
            }
        }


        private string GetObjValue(DynamicObject obj, string sign)
        {
            return obj[sign] == null ? "" : obj[sign].ToString();
        }

        private string CZ_FB_GetObjBalDetail(CZ_FB_EnFBObject _fbo, string _FObjID)
        {
            if (_FObjID == "" || _FObjID == "0")
            {
                return "warning:未传入有效的查询对象ID";
            }

           

            string _FValueSource = "";  //t_bd_FlexItemProperty：FValueSource
            string _ObjMstIDSch = "";   //查询对象表主键FMasterID的语句 
            string _ObjAcctWhile = "";  //对象查询科目定义 如果为空 不拼入查询语句 [每个公司需求不同]
            string _FFlexNumber = "";   //t_bd_FlexItemProperty：FFLexNumber，应用于t_bd_FlexItemDetailV的列名
            CZ_FB_GetParam(_fbo, ref _FValueSource, ref _ObjMstIDSch, ref _ObjAcctWhile);
            _ObjMstIDSch = _ObjMstIDSch.Replace("#objID#", _FObjID);

            #region step 01:获取 t_bd_FlexItemProperty-FFLexNumber
            string _sqlGetFFlex = "select FValueSource,FFLexNumber from t_bd_FlexItemProperty where FValueSource='" + _FValueSource + "'";
            DataTable _dtFFlex = new DataTable();
            try
            {
                _dtFFlex = DBUtils.ExecuteDataSet(this.Context, _sqlGetFFlex).Tables[0];
            }
            catch (Exception _ex)
            {
                return "Err:获取FFlexNumber时发生错误 ErrMsg:" + _ex.Message;
            }
            if (_dtFFlex.Rows.Count == 0)
            {
                return "warning:t_bd_FlexItemProperty 未设置核算对象";

            }
            _FFlexNumber = _dtFFlex.Rows[0]["FFLexNumber"].ToString();
            #endregion

  
            #region step 02:获取往来余额
            StringBuilder _sb = new StringBuilder();

            switch (_fbo.ToString())
            {
                case "BD_Empinfo": //员工

                    _sb.Append("/*dialect*/ ");
                    _sb.Append("select i.FORGID,ab.FBookID,sy.FValue FCBYear,sp.FValue FCBPeriod, ");
                    _sb.Append("convert(datetime,CONVERT(varchar,sy.FValue)+'-'+CONVERT(varchar,sp.FValue)+'-01')FBegDate ");
                    _sb.Append("into #aco from T_ORG_Organizations i inner join t_bd_AccountBook ab on i.FOrgID=ab.FAccountOrgID ");
                    _sb.Append("inner join T_BAS_SystemProFile sy on ab.FBOOKID=sy.FAccountBookID and sy.FCategory='GL' and sy.FKEY='CurrentYear' ");
                    _sb.Append("inner join T_BAS_SystemProFile sp on ab.FBOOKID=sp.FAccountBookID and sp.FCategory='GL' and sp.FKEY='CurrentPeriod';");
                    _sb.Append("select b.FBeginBalance FGOBAmt,aco.FOrgID FOrgID,SUBSTRING(CONVERT(varchar(100), a.FMODIFYDATE, 120  ),1,10) Date,'' fexplanation,'B' mType from(" + _ObjMstIDSch + ")o1 ");
                    _sb.Append("inner join t_bd_FlexItemDetailV f on o1.FObjMID=f." + _FFlexNumber + " inner join #aco aco on 1=1 ");
                    _sb.Append("inner join T_GL_BALANCE b on f.FID=b.FDetailID and aco.FBOOKID=b.FACCOUNTBOOKID and aco.FCBYear=b.FYEAR and aco.FCBPeriod=b.FPeriod and b.FCURRENCYID=0 ");
                    _sb.Append("inner join T_BD_ACCOUNT a on b.FACCOUNTID=a.FACCTID ");
                    _sb.Append("where aco.FOrgID like('%') " + _ObjAcctWhile + " union all ");
                    _sb.Append("select ve.FDC*ve.FAmount FGOBAmt,aco.FOrgID FOrgID,SUBSTRING(CONVERT(varchar(100), v.FDATE, 120  ),1,10) Date,ve.fexplanation,'R' mType from(" + _ObjMstIDSch + ")o1 ");
                    _sb.Append("inner join t_bd_FlexItemDetailV f on o1.FObjMID=f." + _FFlexNumber + " inner join #aco aco on 1=1 ");
                    _sb.Append("inner join T_GL_VOUCHER v on aco.FBookID=v.FAccountBookID and FPOSTDATE is not null and v.FInvalid=0 ");
                    _sb.Append("inner join T_GL_VOUCHERENTRY ve on v.FVoucherID=ve.FVoucherID and f.FID=ve.FDetailID ");
                    _sb.Append("inner join T_BD_ACCOUNT a on ve.FAccountID=a.FACCTID ");
                    _sb.Append("where aco.FOrgID like('%') " + _ObjAcctWhile + " ");

                    break;
                default:    //供应商或者客户

                    _sb.Append("/*dialect*/ ");
                    _sb.Append("select i.FORGID,ab.FBookID,sy.FValue FCBYear,sp.FValue FCBPeriod, ");
                    _sb.Append("convert(datetime,CONVERT(varchar,sy.FValue)+'-'+CONVERT(varchar,sp.FValue)+'-01')FBegDate ");
                    _sb.Append("into #aco from T_ORG_Organizations i inner join t_bd_AccountBook ab on i.FOrgID=ab.FAccountOrgID ");
                    _sb.Append("inner join T_BAS_SystemProFile sy on ab.FBOOKID=sy.FAccountBookID and sy.FCategory='GL' and sy.FKEY='CurrentYear' ");
                    _sb.Append("inner join T_BAS_SystemProFile sp on ab.FBOOKID=sp.FAccountBookID and sp.FCategory='GL' and sp.FKEY='CurrentPeriod';");
                    _sb.Append("select b.FBeginBalance FGOBAmt,aco.FOrgID FOrgID from(" + _ObjMstIDSch + ")o1 ");
                    _sb.Append("inner join t_bd_FlexItemDetailV f on o1.FObjMID=f." + _FFlexNumber + " inner join #aco aco on 1=1 ");
                    _sb.Append("inner join T_GL_BALANCE b on f.FID=b.FDetailID and aco.FBOOKID=b.FACCOUNTBOOKID and aco.FCBYear=b.FYEAR and aco.FCBPeriod=b.FPeriod and b.FCURRENCYID=0 ");
                    _sb.Append("inner join T_BD_ACCOUNT a on b.FACCOUNTID=a.FACCTID ");
                    _sb.Append("where aco.FOrgID like('%') " + _ObjAcctWhile + " union all ");
                    _sb.Append("select ve.FDC*ve.FAmount FGOBAmt,aco.FOrgID FOrgID from(" + _ObjMstIDSch + ")o1 ");
                    _sb.Append("inner join t_bd_FlexItemDetailV f on o1.FObjMID=f." + _FFlexNumber + " inner join #aco aco on 1=1 ");
                    _sb.Append("inner join T_GL_VOUCHER v on aco.FBookID=v.FAccountBookID and aco.FBegDate<=v.FBusDate and v.FInvalid=0 ");
                    _sb.Append("inner join T_GL_VOUCHERENTRY ve on v.FVoucherID=ve.FVoucherID and f.FID=ve.FDetailID ");
                    _sb.Append("inner join T_BD_ACCOUNT a on ve.FAccountID=a.FACCTID ");
                    _sb.Append("where aco.FOrgID like('%') " + _ObjAcctWhile + " ");

                    break;
            }

            
            string _sqlGetFOBAmt = _sb.ToString();
            try
            {
                var objs = DBUtils.ExecuteDynamicObject(this.Context, _sqlGetFOBAmt);

                //按照工廠拆分
                List<DynamicObject> dList2100 = objs.Where(d => d["FOrgID"].ToString().Equals("156140")).ToList();
                List<DynamicObject> dList2200 = objs.Where(d => d["FOrgID"].ToString().Equals("156141")).ToList();
                List<DynamicObject> dList2300 = objs.Where(d => d["FOrgID"].ToString().Equals("156142")).ToList();
                List<DynamicObject> dList2600 = objs.Where(d => d["FOrgID"].ToString().Equals("293065")).ToList();
                List<DynamicObject> dList2700 = objs.Where(d => d["FOrgID"].ToString().Equals("293066")).ToList();
                List<DynamicObject> dList2800 = objs.Where(d => d["FOrgID"].ToString().Equals("156139")).ToList();
                List<DynamicObject> dList2900 = objs.Where(d => d["FOrgID"].ToString().Equals("293067")).ToList();
                List<DynamicObject> dList3000 = objs.Where(d => d["FOrgID"].ToString().Equals("293070")).ToList();


                switch (_fbo.ToString())
                {
                    case "BD_Empinfo": //员工

                        setRowValue(dList2100,"2100");
                        setRowValue(dList2200, "2200");
                        setRowValue(dList2300, "2300");
                        setRowValue(dList2600, "2600");
                        setRowValue(dList2700, "2700");
                        setRowValue(dList2800, "2800");
                        setRowValue(dList2900, "2900");
                        setRowValue(dList3000, "3000");

                        break;            
                    default:    //供应商或者客户

                        setCompValue(dList2100, "2100");
                        setCompValue(dList2200, "2200");
                        setCompValue(dList2300, "2300");
                        setCompValue(dList2600, "2600");
                        setCompValue(dList2700, "2700");
                        setCompValue(dList2800, "2800");
                        setCompValue(dList2900, "2900");
                        setCompValue(dList3000, "3000");

                        break;

                }
            }
            catch (Exception _ex)
            {
                this.View.ShowErrMessage(_ex.Message, "获取往来余额时发生错误");
                return "Err:获取往来余额时发生错误 ErrMsg:" + _ex.Message;          
            }

            return "success";
            #endregion
        }

        private void setRowValue(List<DynamicObject> dList,string comp) {
            this.View.Model.BatchCreateNewEntryRow("F_ora_d"+ comp, dList.Count());
            for (int i = 0; i < dList.Count(); i++)
            {

                string money = GetObjValue(dList[i], "FGOBAmt");

                if (i == 0)
                {
                    //第一行的金额为余额，减去所有记账，得到初始金额
                    money = (new Decimal(Convert.ToDouble(GetObjValue(dList[i], "FGOBAmt"))) - new Decimal(dList.Where(d=>d["mType"].ToString().Equals("R")).Sum(d => Convert.ToDouble(d["FGOBAmt"])))).ToString();
                }

                this.View.Model.SetValue("F_ora_exp_" + comp, GetObjValue(dList[i], "fexplanation"), i);
                this.View.Model.SetValue("F_ora_tMoney_"+ comp, money, i);             
                this.View.Model.SetValue("F_ora_tDate_"+ comp, GetObjValue(dList[i], "Date"), i);
            }
            this.View.UpdateView("F_ora_d"+ comp);
        }

        private void setCompValue(List<DynamicObject> dList, string comp) {
            this.View.Model.SetValue("F_ora_"+comp, dList.Sum(d => Convert.ToDouble(d["FGOBAmt"])));
            this.View.UpdateView("F_ora_"+comp);
        }

        private void CZ_FB_GetParam(CZ_FB_EnFBObject _fbo, ref string _FValueSource, ref string _ObjMstIDSch, ref string _ObjAcctWhile)
        {
            switch (_fbo.ToString())
            {
                case "BD_Customer":
                    _FValueSource = "BD_Customer";
                    _ObjAcctWhile = " and a.FNumber in('1221.03') ";  //客户  取科目1221.03，借+贷-
                    _ObjMstIDSch = "select FMasterID FObjMID from t_BD_Customer where FCUSTID=#objID# ";
                    break;
                case "BD_Department":
                    _FValueSource = "BD_Department";
                    _ObjAcctWhile = "";
                    _ObjMstIDSch = "select FMasterID FObjMID from t_BD_Department where FDeptID=#objID# ";
                    break;
                case "BD_Empinfo":
                    _FValueSource = "BD_Empinfo";
                    _ObjAcctWhile = " and a.FNumber in('1221.01') ";   //员工 取科目1221.01，借+贷-
                    _ObjMstIDSch = "select FMasterID FObjMID from t_Hr_Empinfo where FID=#objID# ";
                    break;
                case "BD_Supplier":
                    _FValueSource = "BD_Supplier";
                    _ObjAcctWhile = " and a.FNumber in('2202.01','2241.01') ";     //供应商 取科目2202.01及2241.01，按余额进行合并后借+贷-
                    _ObjMstIDSch = "select FMasterID FObjMID from t_BD_Supplier where FSupplierID=#objID# ";
                    break;
                case "ORG_Organizations":
                    _FValueSource = "ORG_Organizations";
                    _ObjAcctWhile = "";
                    _ObjMstIDSch = "select FOrgID FObjMID from t_ORG_Organizations where FOrgID=#objID# ";
                    break;
                case "CN_BANKACNT":
                    _FValueSource = "CN_BANKACNT";
                    _ObjAcctWhile = "";
                    _ObjMstIDSch = "select FMasterID FObjMID from t_CN_BANKACNT where FBankAcntID=#objID# ";
                    break;
                default:
                    break;

            }
        }

        private enum CZ_FB_EnFBObject
        {
            /// <summary>
            /// 客户          中电变压器：取科目1221.03，借+贷-
            /// </summary>
            BD_Customer,           //FFLEX6
            /// <summary>
            /// 部门
            /// </summary>
            BD_Department,         //FFLEX5
            /// <summary>
            /// 职员          中电变压器：取科目1221.01，借+贷-
            /// </summary>
            BD_Empinfo,            //FFLEX7
            /// <summary>
            /// 供应商        中电变压器：取科目2202.01及2241.01，按余额进行合并后借+贷-
            /// </summary>
            BD_Supplier,           //FFLEX4
            /// <summary>
            /// 组织
            /// </summary>
            ORG_Organizations,     //FFLEX11
            /// <summary>
            /// 银行
            /// </summary>
            CN_BANKACNT            //FF100002
        }
    }
}