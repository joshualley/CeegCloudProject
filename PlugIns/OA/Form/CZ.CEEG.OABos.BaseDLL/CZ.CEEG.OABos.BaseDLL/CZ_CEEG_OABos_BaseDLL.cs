using Kingdee.BOS.Core;
using Kingdee.BOS.Core.Metadata;
using Kingdee.BOS.FileServer.Core;
using Kingdee.BOS.FileServer.Core.Object;
using Kingdee.BOS.FileServer.ProxyService;
using Kingdee.BOS.KDThread;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.ServiceHelper;
using Kingdee.BOS.Util;
using Kingdee.BOS.App.Data;
using Kingdee.BOS.Core.Bill;
using Kingdee.BOS.Core.Bill.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Core.DynamicForm.PlugIn.ControlModel;
using Kingdee.BOS.JSON;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Web;
using System.Data;


namespace CZ.CEEG.OABos.BaseDLL
{
    [HotUpdate]
    [Description("Bos通用")]
    public class CZ_CEEG_OABos_BaseDLL : AbstractBillPlugIn
    {
        private int submitType = 0; //附件上传类型
        private JSONObject jSONObject = null; //用于获取文件上传附件中的文件名称
        private bool isFirst = true;

        #region 通用业务功能
        /// <summary>
        /// 根据表单选择的组织过滤员工
        /// </summary>
        /// <param name="e"></param>
        private void FilterStaffByOrganization(BeforeF7SelectEventArgs e)
        {
            string orgId = this.View.Model.DataObject[GetApplySign()["FOrgId"]] == null ? "0" :
                (this.View.Model.DataObject[GetApplySign()["FOrgId"]] as DynamicObject)["Id"].ToString();
            if(orgId == "0") return;
            string orgName = this.Context.CurrentOrganizationInfo.Name;
            string sql = "";
            if (orgName.Equals("开曼集团") || orgName.Equals("中电电气江苏光伏有限公司") || orgName.Equals("华思"))
            {
                sql = string.Format(@"
SELECT FID FROM T_BD_STAFFTEMP st
INNER JOIN T_BD_STAFF s on st.FSTAFFID=s.FSTAFFID and s.FDOCUMENTSTATUS='C' and s.FFORBIDSTATUS='A'
WHERE st.FISFIRSTPOST='1' AND st.FWORKORGID='{0}'", orgId);
            } 
            else
            {
                sql = string.Format(@"/*dialect*/
SELECT DISTINCT FID FROM T_BD_STAFFTEMP st
INNER JOIN T_BD_STAFF s on st.FSTAFFID=s.FSTAFFID and s.FDOCUMENTSTATUS='C' and s.FFORBIDSTATUS='A'
INNER JOIN T_BD_Department d on d.FDeptId=[dbo].[fun_czty_GetWorkDeptID](st.FDeptId)
WHERE FISFIRSTPOST='1' AND d.FUseOrgId='{0}'", orgId);
            }
            
            var objs = CZDB_GetData(sql);
            string filter = objs.Count <= 0 ? "0" : string.Join(",", objs.Select(i => i["FID"].ToString()).ToArray());
            filter = $"FID in ({filter})";
            e.ListFilterParameter.Filter = filter;
        }

        /// <summary>
        /// 用车申请，过滤司机
        /// </summary>
        /// <param name="e"></param>
        private void FilterDriver(BeforeF7SelectEventArgs e)
        {
            string sql = @"SELECT se.FID FROM T_BD_STAFFTEMP se
            INNER JOIN T_ORG_POST_L p ON se.FPOSTID = p.FPOSTID
            WHERE p.FNAME = '驾驶员'";
            var objs = CZDB_GetData(sql);
            if(objs.Count > 0)
            {
                string filter = "FID in (";
                for(int i = 0; i < objs.Count; i++)
                {
                    filter += "'" + objs[i]["FID"].ToString() + "'";
                    if (i < objs.Count - 1) filter += ",";
                }
                if (objs.Count <= 0)
                {
                    filter += "'0'";
                }
                filter += ")";
                e.ListFilterParameter.Filter = filter;
            }
        }

        /// <summary>
        /// 默认申请人为制单人用户对应的员工
        /// </summary>
        private void SetDefaultApply()
        {
            if (this.View.Model.GetValue("FDocumentStatus").ToString() == "Z")
            {
                string userId = this.Context.UserId.ToString();
                //userId = "100229";
                //DynamicObjectCollection obj = CZDB_GetLoginUser2Emp(userId);
                //string orgId = this.View.Model.GetValue(GetApplySign()["FOrgId"]) == null ? "0" : (this.View.Model.GetValue(GetApplySign()["FOrgId"]) as DynamicObject)["Id"].ToString();
                string sql = string.Format("exec proc_czty_GetLoginUser2Emp @FUserID='{0}'", userId);
                var obj = DBUtils.ExecuteDynamicObject(this.Context, sql);
                if(obj.Count > 0)
                {
                    string _FEmpID = obj[0]["FEmpID"].ToString();
                    var _Names = GetApplySign();
                    this.View.Model.SetValue(_Names["FEmpID"], _FEmpID, -1);
                }
            }

        }

        /// <summary>
        /// 判断是否是HR单据
        /// </summary>
        /// <returns></returns>
        private bool IsHRForm()
        {
            string formId = this.View.GetFormId();
            switch (formId)
            {
                case "ora_PC_LZLC": //离职
                    return true;
                case "ora_PC_LYLC": //录用？？
                    return false;
                case "ora_PC_RYXQ": //人员需求？？
                    return false;
                case "ora_DZ": //调职
                    return true; 
                case "ora_Renewal": //续签
                    return true;
                case "ora_Work":  //转正
                    return true;
            }
            return false;
        }

        /// <summary>
        /// 值更新时带出主任岗信息
        /// </summary>
        private void SetApplyValueUpdate()
        {
            var _Names = GetApplySign();
            //string orgId = this.View.Model.GetValue(GetApplySign()["FOrgId"]) == null ? "0" : (this.View.Model.GetValue(GetApplySign()["FOrgId"]) as DynamicObject)["Id"].ToString();
            string FApplyID = this.View.Model.DataObject[_Names["FEmpID"]] == null ? "" : (this.View.Model.DataObject[_Names["FEmpID"]] as DynamicObject)["Id"].ToString();
            string sql = String.Format(@"exec proc_czty_GetLoginUser2Emp @FEmpID='{0}'", FApplyID);
            var obj = CZDB_GetData(sql);
            //string _FEmpID = "";
            string _FOrgID = "";
            string _FDeptID = "";
            string _FPostID = "";
            string _FRankID = "";
            string _FMobile = "";
            string _FSuperiorPost = "";
            string _FGManager = "";
            if (obj.Count > 0)
            {
                //_FEmpID = obj[0]["FEmpID"].ToString();
                _FOrgID = obj[0]["FOrgID"].ToString();
                _FDeptID = obj[0]["FDeptID"].ToString();
                _FPostID = obj[0]["FPostID"].ToString();
                _FRankID = obj[0]["FRankID"].ToString();
                _FMobile = obj[0]["FMobile"].ToString();
                _FSuperiorPost = obj[0]["FSuperiorPost"].ToString();
                _FGManager = obj[0]["FGManager"].ToString();
                if (_Names.Count > 0)
                {
                    //this.View.Model.SetItemValueByID(_Names["FEmpID"], _FEmpID, -1);
                    this.View.Model.SetValue(_Names["FOrgId"], _FOrgID);
                    this.View.Model.SetValue(_Names["FPostID"], _FPostID);
                    this.View.Model.SetValue(_Names["FDeptID"], _FDeptID);
                    this.View.Model.SetValue(_Names["FRankID"], _FRankID);
                    this.View.Model.SetValue(_Names["FMobile"], _FMobile);
                    this.View.Model.SetValue("FPostNameSup", _FSuperiorPost);
                    SetHrFields(obj[0]);
                    //变压器下的组织，设置单位总经理
                    this.View.Model.SetValue("FManager", _FGManager);
                    //string TransformerOrgCode = "175325.281530.281529.175322.175323.175324.156139.156140.156141.156143";
                    ////如果是变压器下的组织，设置单位总经理
                    //if (TransformerOrgCode.Contains(orgId))
                    //{
                        
                    //}
                }

            }
        }

        private void SetHrFields(DynamicObject obj)
        {
            string formId = this.View.GetFormId();
            string sql = "";
            DynamicObjectCollection objs;
            switch (formId)
            {
                case "ora_DZ": //调职
                    this.View.Model.SetItemValueByID("FOutLevel", obj["FRankID"].ToString(), -1);
                    this.View.Model.SetItemValueByID("F_ora_OutDept", obj["FDeptID"].ToString(), -1);
                    this.View.Model.SetItemValueByID("F_ora_OutPost", obj["FPostID"].ToString(), -1);
                    this.View.Model.SetItemValueByID("F_ora_OutOrg", obj["FOrgID"].ToString(), -1);
                    sql = string.Format(@"SELECT * FROM ( SELECT * FROM T_HR_EMPINFO WHERE FID='{0}') e 
                                        INNER JOIN T_BD_PERSON p on e.FID=p.FID",
                                        obj["FEmpID"].ToString());
                    objs = CZDB_GetData(sql);
                    if (objs.Count <= 0)
                    {
                        return;
                    }
                    if (DateTime.Parse(objs[0]["F_HR_BobDate"].ToString()).Year > 1900)
                    {
                        this.View.Model.SetValue("FEntryDate", objs[0]["F_HR_BobDate"].ToString());//入职日期
                    }
                    this.View.Model.SetValue("F_ora_BeforeAddr", obj["FWorkAddress"].ToString());//调职前工作地点
                    this.View.Model.SetValue("F_ora_Type", obj["FContractType"].ToString());//员工合同类型
                    break;
                case "ora_PC_LZLC": //离职
                    this.View.Model.SetItemValueByID("FQuitPost", obj["FPostID"].ToString(), -1);
                    this.View.Model.SetItemValueByID("FContractType", obj["FContractType"].ToString(), -1);
                    this.View.Model.SetValue("FAddress", obj["FWorkAddress"].ToString());
                    break;
                case "ora_Renewal": //续签
                    sql = string.Format(@"SELECT * FROM ( SELECT * FROM T_HR_EMPINFO WHERE FID='{0}') e 
                                        INNER JOIN T_BD_PERSON p on e.FID=p.FID", 
                                        obj["FEmpID"].ToString());
                    objs = CZDB_GetData(sql);
                    if(objs.Count <= 0)
                    {
                        return;
                    }
                    this.View.Model.SetValue("F_ora_Sex", objs[0]["F_HR_SEX"].ToString());//性别
                    this.View.Model.SetValue("FEdubg", objs[0]["FHighestEduId"].ToString());//学历
                    if (DateTime.Parse(objs[0]["F_HR_BornDate"].ToString()).Year > 1900)
                    {
                        this.View.Model.SetValue("F_ora_Birthday", objs[0]["F_HR_BornDate"].ToString());//生日
                    }
                    this.View.Model.SetValue("F_ora_GraduateSchool", objs[0]["FGraduateSchool"].ToString());//毕业院校
                    this.View.Model.SetValue("F_ora_Master", objs[0]["FMajor"].ToString());//专业
                    if (DateTime.Parse(objs[0]["F_HR_BobDate"].ToString()).Year > 1900)
                    {
                        this.View.Model.SetValue("F_ora_InDate", objs[0]["F_HR_BobDate"].ToString());//入职日期
                    }
                    if (DateTime.Parse(objs[0]["FJoinDate"].ToString()).Year > 1900)
                    {
                        this.View.Model.SetValue("F_ora_SocialWorkDay", objs[0]["FJoinDate"].ToString());//参加工作日期
                    }
                    if(DateTime.Parse(objs[0]["FHTDateStart"].ToString()).Year > 1900)
                    {
                        this.View.Model.SetValue("FStartDate", objs[0]["FHTDateStart"].ToString());//合同开始
                    }
                    if (DateTime.Parse(objs[0]["FHTDateEnd"].ToString()).Year > 1900)
                    {
                        this.View.Model.SetValue("FEndDate", objs[0]["FHTDateEnd"].ToString());//合同结束
                    }
                    this.View.Model.SetValue("F_ora_Workplace", obj["FWorkAddress"].ToString());//工作地点
                    this.View.Model.SetValue("F_ora_Level", obj["FRankID"].ToString());//职级
                    this.View.Model.SetValue("F_ora_ContractType", obj["FContractType"].ToString());//合同类型
                    sql = string.Format(@"select FID from T_BD_STAFFTEMP es
inner join T_BD_STAFF s on es.FSTAFFID=s.FSTAFFID and s.FDOCUMENTSTATUS='C' and s.FFORBIDSTATUS='A'
where es.FPostID='{0}' and FIsFirstPost='1' ", obj["FSuperiorPost"].ToString());
                    objs = CZDB_GetData(sql);
                    if(objs.Count > 0)
                    {
                        this.View.Model.SetValue("F_ora_Leader", objs[0]["FID"].ToString());//直接领导
                    }
                    
                    break;
                case "ora_Work": //转正
                    sql = string.Format(@"SELECT * FROM ( SELECT * FROM T_HR_EMPINFO WHERE FID='{0}') e 
                                        INNER JOIN T_BD_PERSON p on e.FID=p.FID",
                                        obj["FEmpID"].ToString());
                    objs = CZDB_GetData(sql);
                    if (objs.Count <= 0)
                    {
                        return;
                    }
                    this.View.Model.SetValue("FStaff", obj["FEmpID"].ToString());//转正员工
                    this.View.Model.SetValue("FBeforeDeptID", obj["FDeptID"].ToString());//转正前部门
                    this.View.Model.SetValue("FBeforePost", obj["FPostID"].ToString());//转正前岗位
                    this.View.Model.SetValue("FBeforeLevel", obj["FRankID"].ToString());//转正前职级
                    if (DateTime.Parse(objs[0]["F_HR_BobDate"].ToString()).Year > 1900)
                    {
                        this.View.Model.SetValue("F_ora_fromDate", objs[0]["F_HR_BobDate"].ToString());//入职日期
                    }
                    this.View.Model.SetValue("FProbation", objs[0]["FProbation"].ToString());//试用期
                    if (DateTime.Parse(objs[0]["F_ora_toDate"].ToString()).Year > 1900)
                    {
                        this.View.Model.SetValue("F_ora_toDate", objs[0]["F_ora_toDate"].ToString());//转正日期
                    }
                    this.View.Model.SetValue("F_ora_School", objs[0]["FGraduateSchool"].ToString());//毕业院校
                    this.View.Model.SetValue("F_ora_Subject", objs[0]["FMajor"].ToString());//所学专业
                    this.View.Model.SetValue("F_ora_Number", objs[0]["FStaffNumber"].ToString());//工号
                    break;
            }
        }

        /// <summary>
        /// 根据单据的FormID，获取单据的申请人信息标识名
        /// </summary>
        /// <param name="x"></param>
        /// <returns></returns>
        private Dictionary<string, string> GetApplySign()
        {
            Dictionary<string, string> dict = new Dictionary<string, string>();

            string formId = this.View.GetFormId();
            switch (formId)
            {
                case "k0c30c431418e4cf4a60d241a18cb241c": //出差申请
                    dict.Add("FEmpID", "FApply");
                    dict.Add("FOrgId", "FOrgId");
                    dict.Add("FDeptID", "FDeptID");
                    dict.Add("FPostID", "FPost");
                    dict.Add("FRankID", "FLevel");
                    dict.Add("FMobile", "FTel");
                    break;
                case "k56f7d65e79df456eb9156bfdc3339985": //出门证
                    dict.Add("FEmpID", "FApplyID");
                    dict.Add("FOrgId", "FOrgId");
                    dict.Add("FDeptID", "FDeptID");
                    dict.Add("FPostID", "FPost");
                    dict.Add("FRankID", "FLevel");
                    dict.Add("FMobile", "FTel");
                    break;
                case "kaa55d0cac0c5447bbc6700cfbdf0b11e": //对公费用立项
                    dict.Add("FEmpID", "FApply");
                    dict.Add("FOrgId", "FOrgId");
                    dict.Add("FDeptID", "FDeptID");
                    dict.Add("FPostID", "FPost");
                    dict.Add("FRankID", "FLevel");
                    dict.Add("FMobile", "FTel");
                    break;
                case "k191b3057af6c4252bcea813ff644cd3a": //对公资金申请
                    dict.Add("FEmpID", "FApplyID");
                    dict.Add("FOrgId", "FOrgId");
                    dict.Add("FDeptID", "FDeptID");
                    dict.Add("FPostID", "F_ora_Post");
                    dict.Add("FRankID", "F_ora_Level");
                    dict.Add("FMobile", "F_ora_Mobile");
                    break;
                case "ke6d80dfd260e4ef88d75f69f4c7ef0a1": //个人费用立项
                    dict.Add("FEmpID", "FApplyID");
                    dict.Add("FOrgId", "FOrgId");
                    dict.Add("FDeptID", "F_DeptID");
                    dict.Add("FPostID", "FPost");
                    dict.Add("FRankID", "F_ora_AppLevel");
                    dict.Add("FMobile", "FTel");
                    break;
                case "k0c6b452fa8154c4f8e8e5f55f96bcfac": //个人资金借支
                    dict.Add("FEmpID", "FApply");
                    dict.Add("FOrgId", "FOrgId");
                    dict.Add("FDeptID", "FDeptID");
                    dict.Add("FPostID", "FPost");
                    dict.Add("FRankID", "F_ora_Level");
                    dict.Add("FMobile", "FContract");
                    break;
                case "k9c79fe3a14f54d8ba0922480b91c1e05": //加班申请
                    dict.Add("FEmpID", "F_ora_Applicant");
                    dict.Add("FOrgId", "F_ora_OrgId");
                    dict.Add("FDeptID", "F_ora_AppDep");
                    dict.Add("FPostID", "F_ora_Station");
                    dict.Add("FRankID", "F_ora_Level");
                    dict.Add("FMobile", "F_ora_Contract");
                    break;
                case "kbea624189d8e4d829b68340507eda196": //请假申请
                    dict.Add("FEmpID", "FApplyID");
                    dict.Add("FOrgId", "FOrgId");
                    dict.Add("FDeptID", "FDeptID");
                    dict.Add("FPostID", "F_ora_Post");
                    dict.Add("FRankID", "F_ora_Level");
                    dict.Add("FMobile", "F_ora_Contract");
                    break;
                case "ke33602c4467b41ae8ba8c1d4011c20c4": //印章
                    dict.Add("FEmpID", "FApply");
                    dict.Add("FOrgId", "FOrgId");
                    dict.Add("FDeptID", "FDeptID");
                    dict.Add("FPostID", "FPost");
                    dict.Add("FRankID", "FLevel");
                    dict.Add("FMobile", "FTel");
                    break;
                case "k1ae2591790044d95b9966ad0dff1d987": //招待费用申请
                    dict.Add("FEmpID", "F_ora_Applicant");
                    dict.Add("FOrgId", "F_ora_OrgId");
                    dict.Add("FDeptID", "FDeptID");
                    dict.Add("FPostID", "FPost");
                    dict.Add("FRankID", "F_ora_Level");
                    dict.Add("FMobile", "F_ora_Contact");
                    break;
                case "kdcdde6ac18cb4d419a6924b49a593460": //招待费用报销
                    dict.Add("FEmpID", "F_ora_Applicant");
                    dict.Add("FOrgId", "F_ora_OrgId");
                    dict.Add("FDeptID", "FDeptID");
                    dict.Add("FPostID", "FPost");
                    dict.Add("FRankID", "FLevel");
                    dict.Add("FMobile", "F_ora_Contact");
                    break;
                case "k6575db4ed77c449f88dd20cceef75a73": //出差报销
                    dict.Add("FEmpID", "FApply");
                    dict.Add("FOrgId", "FOrgId");
                    dict.Add("FDeptID", "FDeptID");
                    dict.Add("FPostID", "FPost");
                    dict.Add("FRankID", "FLevel");
                    dict.Add("FMobile", "FTel");
                    break;
                case "k5c88e2dc1ac14349935d452e74e152c8": //对公费用报销
                    dict.Add("FEmpID", "FApply");
                    dict.Add("FOrgId", "FOrgId");
                    dict.Add("FDeptID", "FDeptID");
                    dict.Add("FPostID", "F_ora_Post");
                    dict.Add("FRankID", "F_ora_Level");
                    dict.Add("FMobile", "FMobile");
                    break;
                case "k767a317ad28e40f1b25e95b92e218fea": //个人费用报销
                    dict.Add("FEmpID", "FApplyID");
                    dict.Add("FOrgId", "F_ora_OrgId");
                    dict.Add("FDeptID", "FDeptID");
                    dict.Add("FPostID", "FPost");
                    dict.Add("FRankID", "FLevel");
                    dict.Add("FMobile", "FTel");
                    break;
                case "k3972241808034802b04c3d18d4107afd": //采购合同
                    dict.Add("FEmpID", "FApply");
                    dict.Add("FOrgId", "FOrgId");
                    dict.Add("FDeptID", "FDeptID");
                    dict.Add("FPostID", "FPost");
                    dict.Add("FRankID", "FLevel");
                    dict.Add("FMobile", "FTel");
                    break;
                case "kdb6ae742543a4f6da09dfed7ba4e02dd": //销售合同
                    dict.Add("FEmpID", "FApplyID");
                    dict.Add("FOrgId", "FOrgId");
                    dict.Add("FDeptID", "FDeptID");
                    dict.Add("FPostID", "F_ora_Post");
                    dict.Add("FRankID", "F_ora_Level");
                    dict.Add("FMobile", "FContractWay");
                    break;
                case "kbb14985fbec4445c846533837b2eea65": //对公合同
                    dict.Add("FEmpID", "FApplyID");
                    dict.Add("FOrgId", "FOrgId");
                    dict.Add("FDeptID", "FDeptID");
                    dict.Add("FPostID", "FPost");
                    dict.Add("FRankID", "FLevel");
                    dict.Add("FMobile", "FTel");
                    break;
                default:
                    dict.Add("FEmpID", "FApplyID");
                    dict.Add("FOrgId", "FOrgId");
                    dict.Add("FDeptID", "FDeptID");
                    dict.Add("FPostID", "FPost");
                    dict.Add("FRankID", "FLevel");
                    dict.Add("FMobile", "FTel");
                    break;
            }

            return dict;
        }

        /// <summary>
        /// 输入用户ID 取得绑定职员ID proc_czty_GetLoginUser2Emp 
        /// 如需取得FEmpID外字段的控制或输出，另建方法
        /// </summary>
        /// <param name="_userID"></param>
        /// <returns></returns>
        public DynamicObjectCollection CZDB_GetLoginUser2Emp(string _userID)
        {
            //FUserID	FEmpID	FEmpName	FDeptID	    FDeptName
            //173439	113015	张报	    112498	    财务融资部
            string sql = "exec proc_czty_GetLoginUser2Emp @FUserID=" + _userID;
            var obj = DBUtils.ExecuteDynamicObject(this.Context, sql);
            return obj;
        }

        /// <summary>
        /// 上传附件方法
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="dataBuff"></param>
        /// <returns></returns>
        private FileUploadResult UploadAttachment(string fileName, byte[] dataBuff)
        {
            // 初始化上传下载服务，这个Service会根据Cloud配置自动上传到对应的文件服务器
            var service = new UpDownloadService();
            int len = 0, less = 0;
            string fileId = null;
            byte[] buff = null;
            while (len < dataBuff.Length)
            {
                // 文件服务器采用分段上传，每次上传4096字节, 最后一次如果不够则上传剩余长度
                less = (dataBuff.Length - len) >= 4096000 ? 4096000 : (dataBuff.Length - len);
                buff = new byte[less];
                Array.Copy(dataBuff, len, buff, 0, less);
                len += less;

                using (System.IO.MemoryStream ms = new System.IO.MemoryStream(buff))
                {
                    TFileInfo tFile = new TFileInfo()
                    {
                        FileId = fileId,
                        FileName = fileName,
                        CTX = this.Context,
                        Last = len >= dataBuff.Length,//标记是否为文件的最后一个片段 
                        Stream = ms
                    };

                    var result = service.UploadAttachment(tFile);
                    // 注意点：上传时fileId传入null为新文件开始,会返回一个文件的fileId，后续采用这个fileId标识均为同一文件的不同片段。
                    fileId = result.FileId;
                    if (!result.Success)
                    {
                        return result;
                    }
                }
            }

            return new FileUploadResult()
            {
                Success = true,
                FileId = fileId
            };
        }
        
        /// <summary>
        /// 绑定单据后，上传附件
        /// </summary>
        private string BindForm_UploadAttach()
        {
            if (jSONObject != null)
            {
                JSONArray jSONArray = new JSONArray(jSONObject["NewValue"].ToString());
                if (jSONArray.Count > 0)
                {
                    List<DynamicObject> dynList = new List<DynamicObject>();
                    StringBuilder sb = new StringBuilder();
                    for (int i = 0; i < jSONArray.Count; i++)
                    {
                        //获取上传的文件名
                        string serverFileName = (jSONArray[i] as Dictionary<string, object>)["ServerFileName"].ToString();
                        string fileName = (jSONArray[i] as Dictionary<string, object>)["FileName"].ToString();
                        //文件上传到服务端的临时目录
                        string directory = "FileUpLoadServices\\UploadFiles";
                        //文件的完整路径
                        string fileFullPath = PathUtils.GetPhysicalPath(directory, serverFileName);

                        var formMetadata = (FormMetadata)MetaDataServiceHelper.Load(this.Context, FormIdConst.BOS_Attachment);
                        var dataBuff = System.IO.File.ReadAllBytes(fileFullPath);
                        var dyn = new DynamicObject(formMetadata.BusinessInfo.GetDynamicObjectType());

                        if (submitType != 0)
                        {
                            // 将数据上传至文件服务器，并返回上传结果
                            var result = this.UploadAttachment(fileName, dataBuff);
                            if (!result.Success)
                            {
                                // 上传失败，收集失败信息
                                sb.AppendLine(string.Format("附件：{0}，上传失败：{1}", fileName, result.Message));
                                continue;
                            }

                            // 通过这个FileId就可以从文件服务器下载到对应的附件
                            dyn["FileId"] = result.FileId;
                        }
                        else
                        {
                            // 数据库的方式是直接保存附件数据
                            dyn["Attachment"] = dataBuff;
                        }
                        string _BillType = this.View.BusinessInfo.GetForm().Id;
                        string _BillNo = this.View.Model.DataObject["BillNo"].ToString();   //原始BOS单据【绑定实体属性】
                        string _InterID = this.View.Model.DataObject["Id"].ToString();

                        dyn["BillType"] = _BillType;
                        dyn["BillNo"] = _BillNo;
                        dyn["InterID"] = _InterID;

                        // 上传文件服务器成功后才加入列表
                        dyn["AttachmentName"] = fileName;
                        dyn["AttachmentSize"] = Math.Round(dataBuff.Length / 1024.0, 2);
                        dyn["EntryInterID"] = -1;// 参照属性解读

                        dyn["CreateMen_Id"] = Convert.ToInt32(this.Context.UserId);
                        dyn["CreateMen"] = GetUser(this.Context.UserId.ToString());
                        dyn["ModifyTime"] = dyn["CreateTime"] = TimeServiceHelper.GetSystemDateTime(this.Context);
                        dyn["ExtName"] = System.IO.Path.GetExtension(fileName);
                        dyn["FileStorage"] = submitType.ToString();
                        dyn["EntryKey"] = " ";

                        dynList.Add(dyn);
                    }
                    if (dynList.Count > 0)
                    {
                        // 所有数据加载完成后再一次性保存全部
                        BusinessDataServiceHelper.Save(this.Context, dynList.ToArray());

                        sb.AppendLine();
                        sb.AppendLine(string.Join(",", dynList.Select(dyn => dyn["AttachmentName"].ToString()).ToArray()) + ",上传成功");
                    }

                    //写入上传日志 增加判定 FLog不存在不写入
                    if (this.Model.BillBusinessInfo.GetElement("FLog") != null)
                    {
                        this.Model.SetValue("FLog", sb.ToString());
                    }
                    return sb.ToString();
                }
            }
            return "";
        }

        /// <summary>
        /// 设置附件上传类型
        /// </summary>
        private void SetSubmitType()
        {
            // 采用文件服务器方式进行附件存取
            submitType = 1;
            // 检查是否启用了文件服务器
            if (!Kingdee.BOS.ServiceHelper.FileServer.FileServerHelper.UsedFileServer(this.Context))
            {
                this.View.ShowMessage("未启用文件服务器，无法实现上传。");
                return;
            }
            // 取Cloud服务器配置的存储方式进行附件存储
            submitType = Kingdee.BOS.ServiceHelper.FileServer.FileServerHelper.GetFileStorgaeType(this.Context);
        }
        #endregion

        #region override函数
        /// <summary>
        /// 数据绑定后执行，用于操作界面样式
        /// </summary>
        /// <param name="e"></param>
        public override void AfterBindData(EventArgs e)
        {
            base.AfterBindData(e);
            if (this.Context.ClientType.ToString() != "Mobile")
            {
                if(this.View.Model.GetValue("FDocumentStatus").ToString() == "Z")
                {
                    try
                    {
                        //初始加载隐藏上传按钮
                        this.View.GetControl("FUpload").Visible = false;
                    }
                    catch { }
                }
                
                
                if (!IsHRForm() && isFirst)
                {
                    SetDefaultApply();
                    isFirst = false;
                }
                
            }
        }
        
        /// <summary>
        /// 值更新
        /// </summary>
        /// <param name="e"></param>
        public override void DataChanged(DataChangedEventArgs e)
        {
            if (this.Context.ClientType.ToString() != "Mobile")
            {
                base.DataChanged(e);
                var _Names = GetApplySign();
                if (e.Field.Key.ToString().ToUpper() == _Names["FEmpID"].ToUpper())
                {
                    SetApplyValueUpdate();
                }
            }
        }

        private bool isUpload = false; //控制文件不要进行多次上传
        /// <summary>
        /// 提交成功后进行文件上传
        /// </summary>
        /// <param name="e"></param>
        public override void AfterDoOperation(AfterDoOperationEventArgs e)
        {
            base.AfterDoOperation(e);
            if (this.Context.ClientType.ToString() != "Mobile")
            {
                var opKey = e.Operation.Operation.ToString();
                if (opKey == "Save" && !isUpload) //点击保存，且附件未上传过
                {
                    //如果生成了单据编号，进行附件上传
                    var _BillNo = this.View.Model.DataObject["BillNo"];
                    if (_BillNo != null)
                    {
                        SetSubmitType();
                        BindForm_UploadAttach();
                        isUpload = true;
                    }
                }
                else if (opKey == "Submit" && !isUpload)//点击提交，且附件未上传过
                {
                    //如果生成了单据编号，进行附件上传
                    var _BillNo = this.View.Model.DataObject["BillNo"];
                    if (_BillNo != null)
                    {
                        SetSubmitType();
                        BindForm_UploadAttach();
                        isUpload = true;
                    }
                }
            }
            
        }

        /// <summary>
        /// 选择前过滤数据
        /// </summary>
        /// <param name="e"></param>
        public override void BeforeF7Select(BeforeF7SelectEventArgs e)
        {
            //base.BeforeF7Select(e);
            switch(e.FieldKey)
            {
                case "FApply":
                    FilterStaffByOrganization(e);
                    break;
                case "FApplyID":
                    FilterStaffByOrganization(e);
                    break;
                case "F_ora_Applicant":
                    FilterStaffByOrganization(e);
                    break;
                case "F_ora_Handler": //调职，工作交接人
                    FilterStaffByOrganization(e);
                    break;
                case "FHandler": //离职，工作交接人
                    FilterStaffByOrganization(e);
                    break;
                case "F_ora_Leader": //续签，直接领导
                    FilterStaffByOrganization(e);
                    break;
                case "F_ora_rcvDiver": //用车，司机
                    FilterDriver(e);
                    break;
            }

        }

        

        /// <summary>
        /// 自定义事件,获取文件上传控件中文件的名称，以便提交后进行文件上传
        /// </summary>
        /// <param name="e"></param>
        public override void CustomEvents(CustomEventsArgs e)
        {
            if (this.Context.ClientType.ToString() != "Mobile")
            {
                if (e.Key.ToUpper() == "FFileUpdate".ToUpper())
                {
                    //触发事件是上传文件有变化
                    if (e.EventName.ToUpper() == "FILECHANGED")
                    {
                        jSONObject = KDObjectConverter.DeserializeObject<JSONObject>(e.EventArgs);
                    }
                }
                base.CustomEvents(e);
            }
        }

        public override void AfterButtonClick(AfterButtonClickEventArgs e)
        {
            base.AfterButtonClick(e);
            string key = e.Key.ToUpperInvariant();
            switch (key)
            {
                case "FUPLOAD": //FUpload
                    string FDocumentStatus = this.View.Model.GetValue("FDocumentStatus").ToString();
                    if (FDocumentStatus == "Z")
                    {
                        this.View.ShowWarnningMessage("请保存后再上传附件！");
                    }
                    else
                    {
                        if (jSONObject == null)
                        {
                            this.View.ShowWarnningMessage("请先选择附件！");
                        }
                        else
                        {
                            string reault = BindForm_UploadAttach();
                            if (reault != "")
                            {
                                isUpload = true;
                                this.View.ShowMessage(reault);

                            }
                        }
                    }
                    break;
            }
        }
        #endregion

        #region 通用函数
        /// <summary>
        /// 获取用户
        /// </summary>
        /// <param name="userID"></param>
        /// <returns></returns>
        DynamicObject GetUser(string userID)
        {
            OQLFilter filter = OQLFilter.CreateHeadEntityFilter(string.Format("FUSERID={0}", userID));
            return BusinessDataServiceHelper.Load(this.View.Context, FormIdConst.SEC_User, null, filter).FirstOrDefault();
        }

        /// <summary>
        /// 基本方法 
        /// </summary>
        /// <param name="_sql"></param>
        /// <returns></returns>
        public DynamicObjectCollection CZDB_GetData(string _sql)
        {
            try
            {
                var obj = DBUtils.ExecuteDynamicObject(this.Context, _sql);
                return obj;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        #endregion
    }
}
