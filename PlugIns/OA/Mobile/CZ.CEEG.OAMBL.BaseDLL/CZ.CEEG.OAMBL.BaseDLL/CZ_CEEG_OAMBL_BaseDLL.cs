using Kingdee.BOS.Core;
using Kingdee.BOS.Core.Metadata;
using Kingdee.BOS.FileServer.Core;
using Kingdee.BOS.FileServer.Core.Object;
using Kingdee.BOS.FileServer.ProxyService;
using Kingdee.BOS.KDThread;
using Kingdee.BOS.Mobile.Metadata.ControlDataEntity;
using Kingdee.BOS.Mobile.PlugIn;
using Kingdee.BOS.Mobile.PlugIn.ControlModel;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.ServiceHelper;
using Kingdee.BOS.Util;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
//using System.Threading.Tasks;
using System.ComponentModel;
using System.Web;
using System.Data;
using Kingdee.BOS.App.Data;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Core.DynamicForm.PlugIn.ControlModel;
using Kingdee.BOS.JSON;
using Kingdee.BOS.Core.DynamicForm;
using System.Threading;
using Kingdee.BOS.App;
using Kingdee.BOS.Core.DynamicForm.Operation;
using Kingdee.BOS.Core.Metadata.ConvertElement.ServiceArgs;
using Kingdee.BOS.Core.List;
using Kingdee.BOS.Orm;
using Kingdee.BOS;
using Kingdee.BOS.Mobile;
//using Kingdee.BOS;
//using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;

namespace CZ.CEEG.OAMBL.BaseDLL
{
    /// <summary>
    /// Mobile端 通用DLL
    /// </summary>
    /// <remarks>
    /// 附件上传涉及以下步骤：
    /// 1. 附件控件将选取文件拷贝到临时目录
    /// 2. 触发AfterMobileUpload函数并传入所需参数
    /// 3. 从临时目录将文件保存至数据库或文件服务器
    /// </remarks>
    [Description("Mobile端 通用DLL")]
    [Kingdee.BOS.Util.HotUpdate]
    public class CZ_CEEG_OAMBL_BaseDLL : AbstractMobileBillPlugin
    {
        /// <summary>
        /// 上传类型, 0为数据库，1为文件服务，2为亚马逊云，3为金蝶云
        /// </summary>
        private int submitType = 0;

        #region 通用业务功能
        /// <summary>
        /// 根据表单选择的组织过滤员工
        /// </summary>
        /// <param name="e"></param>
        private void FilterStaffByOrganization(BeforeF7SelectEventArgs e)
        {
            string filter = "FID in (";
            string orgId = this.View.BillModel.DataObject[GetApplySign()["FOrgId"]] == null ? "0" :
                (this.View.BillModel.DataObject[GetApplySign()["FOrgId"]] as DynamicObject)["Id"].ToString();
            if(orgId == "0")
            {
                return;
            }
            string sql = string.Format("SELECT es.FID FROM T_BD_STAFFTEMP es INNER JOIN T_BD_DEPARTMENT d on es.FDEPTID=d.FDEPTID WHERE es.FISFIRSTPOST='1' AND d.FUSEORGID='{0}'", orgId);
            var objs = DBUtils.ExecuteDynamicObject(this.Context, sql);

            for (int i = 0; i < objs.Count; i++)
            {
                filter += "'" + objs[i]["FID"].ToString() + "'";
                if (i < objs.Count - 1) filter += ",";
            }
            if (objs.Count <= 0)
            {
                filter += "'0'";
            }
            filter += ')';
            e.ListFilterParameter.Filter = filter;
        }

        /// <summary>
        /// 表体可编辑
        /// </summary>
        private void EntryEditEnable()
        {
            Control entryCtl = null;
            try
            {
                entryCtl = this.View.GetControl("F_ora_MobileProxyEntryEntity");
            }
            catch (Exception) { }

            if (entryCtl != null)
            {
                if (this.View.OpenParameter.Status.ToString() != "VIEW")
                {
                    entryCtl.SetCustomPropertyValue("listEditable", true);
                }
                else
                {
                    entryCtl.SetCustomPropertyValue("listEditable", false);
                }
            }
        }

        /// <summary>
        /// 新增表体行
        /// </summary>
        private void AddNewEntryRow()
        {
            this.View.BillModel.BeginIniti();
            this.View.BillModel.CreateNewEntryRow("FEntity");
            this.View.BillModel.EndIniti();
            this.View.UpdateView("F_ora_MobileProxyEntryEntity");
        }

        /// <summary>
        /// 默认申请人为制单人用户对应的员工
        /// </summary>
        private void SetDefaultApply()
        {

            if (this.View.ClientType.ToString() == "Mobile" && this.View.BillModel.GetValue("FDocumentStatus").ToString() == "Z")
            {
                string userId = this.Context.UserId.ToString();
                //userId = "100229";
                //DynamicObjectCollection obj = CZDB_GetLoginUser2Emp(userId);
                //string orgId = this.View.BillModel.GetValue(GetApplySign()["FOrgId"]) == null ? "0" : (this.View.BillModel.GetValue(GetApplySign()["FOrgId"]) as DynamicObject)["Id"].ToString();
                string sql = string.Format("exec proc_czty_GetLoginUser2Emp @FUserID='{0}'", userId);
                var obj = DBUtils.ExecuteDynamicObject(this.Context, sql);

                if (obj.Count > 0)
                {
                    var _Names = GetApplySign();
                    string _FEmpID = obj[0]["FEmpID"].ToString();
                    this.View.BillModel.SetValue(_Names["FEmpID"], _FEmpID);
                }
            }
            
        }

        /// <summary>
        /// 值更新时带出主任岗信息
        /// </summary>
        private void SetApplyValueUpdate()
        {

            var _Names = GetApplySign();
            string FApplyID = this.View.BillModel.DataObject[_Names["FEmpID"]] == null ? "0" : (this.View.BillModel.DataObject[_Names["FEmpID"]] as DynamicObject)["Id"].ToString();
            string sql = String.Format(@"exec proc_czty_GetLoginUser2Emp @FEmpID='{0}'", FApplyID);
            var obj = DBUtils.ExecuteDynamicObject(this.Context, sql);
            //string _FEmpID = "";
            string _FOrgID = "";
            string _FDeptID = "";
            string _FPostID = "";
            string _FRankID = "";
            string _FMobile = "";
            string _FSuperiorPost = "";
            string _FGManager = "";

            //this.View.ShowMessage(obj.Count+"");

            if (obj.Count > 0)
            {
                //_FEmpID = obj[0]["FEmpID"].ToString();
                _FOrgID = obj[0]["FORGID"].ToString();
                _FDeptID = obj[0]["FDeptID"].ToString();
                _FPostID = obj[0]["FPostID"].ToString();
                _FMobile = obj[0]["FMobile"].ToString();
                _FRankID = obj[0]["FRankID"].ToString();


                //this.View.ShowMessage(_FPostID);

                _FSuperiorPost = obj[0]["FSuperiorPost"].ToString();
                _FGManager = obj[0]["FGManager"].ToString();
                if (_Names.Count > 0)
                {
                    //this.View.BillModel.SetItemValueByID(_Names["FEmpID"], _FEmpID, -1);
                    this.View.BillModel.SetValue(_Names["FOrgId"], _FOrgID);
                    this.View.BillModel.SetValue(_Names["FDeptID"], _FDeptID);
                    this.View.BillModel.SetValue(_Names["FPostID"], _FPostID);
                    this.View.BillModel.SetValue(_Names["FRankID"], _FRankID);
                    this.View.BillModel.SetValue(_Names["FMobile"], _FMobile);
                    this.View.BillModel.SetValue("FPostNameSup", _FSuperiorPost);
                }
                this.View.BillModel.SetValue("FManager", _FGManager);
            }
        }

        /// <summary>
        /// 根据单据的FormID，获取单据的申请人信息标识名
        /// </summary>
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
                case "k0c6b452fa8154c4f8e8e5f55f96bcfac": //个人资金借支
                    dict.Add("FEmpID", "FApply");
                    dict.Add("FOrgId", "FOrgId");
                    dict.Add("FDeptID", "FDeptID");
                    dict.Add("FPostID", "FPost");
                    dict.Add("FRankID", "F_ora_Level");
                    dict.Add("FMobile", "FContract");
                    break;
                case "ke6d80dfd260e4ef88d75f69f4c7ef0a1": //个人费用立项
                    dict.Add("FEmpID", "FApplyID");
                    dict.Add("FOrgId", "FOrgId");
                    dict.Add("FDeptID", "F_DeptID");
                    dict.Add("FPostID", "FPost");
                    dict.Add("FRankID", "F_ora_AppLevel");
                    dict.Add("FMobile", "FTel");
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
        /// do file upload，附件上传
        /// </summary>
        private void CZ_DoFileUpLoad()
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
            // 先将客户端附件上传至临时目录
            var _BillNo = this.View.BillModel.DataObject["BillNo"];
            if (_BillNo != null)
            {
                FileUploadControl _FFileUpdateCtl = null;
                try
                {
                    _FFileUpdateCtl = this.View.GetControl<FileUploadControl>("FFileUpdate");
                }
                catch (Exception) { }

                if (_FFileUpdateCtl != null)
                {
                    this.View.ShowMessage("正在上传附件，请不要关闭页面！", MessageBoxOptions.OK);
                    _FFileUpdateCtl.UploadFieldBatch();
                }
                else
                {
                    Jump2Audit();
                }
            }
        }
        #endregion

        #region 提交后提醒跳转到业务审批
        /// <summary>
        /// 提醒跳转到业务审批，附件上传后执行
        /// </summary>
        private void Jump2Audit()
        {
            this.View.ShowMessage("提交成功，是否退出页面？", 
                MessageBoxOptions.YesNo,
                new Action<MessageBoxResult>((result) =>
                {
                    if(result == MessageBoxResult.Yes)
                    {
                        //关闭页面
                        JSONObject arg = new JSONObject();
                        arg.Put("pageId", this.View.PageId);
                        this.View.AddAction("closeWebViewWithXT", arg);
                    }
                })
            );
            
        }
        #endregion

        #region override方法
        /// <summary>
        /// 数据绑定后执行，用于操作界面样式
        /// </summary>
        /// <param name="e"></param>
        public override void AfterBindData(EventArgs e)
        {
            base.AfterBindData(e);
            EntryEditEnable();
            SetDefaultApply();
        }

        /// <summary>
        /// 值更新
        /// </summary>
        /// <param name="e"></param>
        public override void DataChanged(DataChangedEventArgs e)
        {
            base.DataChanged(e);

            //this.View.ShowMessage(e.Field.Key.ToString().ToUpperInvariant());

            switch (e.Field.Key.ToString().ToUpperInvariant())
            {
                case "FAPPLYID":
                case "FAPPLY":
                case "F_ORA_APPLICANT": 
                    SetApplyValueUpdate();
                    break;
            }
        }


        /// <summary>
        /// 点击事件
        /// </summary>
        /// <param name="e"></param>
        public override void AfterButtonClick(AfterButtonClickEventArgs e)
        {
            base.AfterButtonClick(e);
            string key = e.Key.ToString().ToUpperInvariant();
            switch (key)
            {
                case "FNEWROW": //新增行
                    AddNewEntryRow();
                    break;
                case "FPUSHBTN": //下推
                    string formId = this.View.BillView.GetFormId();
                    if(formId.Equals("ora_FYLX")){
                        //费用立项下推选择
                        //获取发起部门
                        string dept = (this.View.BillModel.GetValue("FDeptID") as DynamicObject)["Number"].ToString();
                        string workstation = (this.View.BillModel.GetValue("FPost") as DynamicObject)["Number"].ToString();
                        string costType = (this.View.BillModel.GetValue("FCostType1") as DynamicObject)["Number"].ToString();
                        string billNo = this.View.BillModel.GetValue("FBillNo").ToString();
                        Dictionary<string, string> options = new Dictionary<string, string>
                        {
                            { "dept", dept },
                            { "workstation", workstation },
                            { "costType", costType },
                            {"billNo",billNo }
                        };
                        this.Act_Push(options);
                    }else{
                        PushFormByFormId(null,null);
                    }
                    break;
            }
        }

        private void Act_Push(Dictionary<string, string> options)
        {
            //打开选择界面
            var para1 = new MobileShowParameter();
            para1.FormId = "ora_fl_push_choose";
            para1.OpenStyle.ShowType = ShowType.Modal;
            para1.Height = 143;
            para1.Width = 240;
            para1.ParentPageId = this.View.PageId;
            para1.Status = OperationStatus.EDIT;
            string pushFormId = "";
            this.View.ShowForm(para1, (formResult) =>
            {
                if (formResult.ReturnData == null)
                {
                    return;
                }
                pushFormId = formResult.ReturnData.ToString();
                if (pushFormId == "1")
                {
                    //下推个人资金        
                    string sql = "select isnull(max(fid),0) as targetId from ora_t_PersonMoneyEntry where " +
                    "fentryid in (SELECT  FEntryID FROM ora_t_PersonMoneyEntry_LK WHERE(FSBillId = " +
                    "(select fid from ora_t_Cust100050 where fbillno = '"+ options["billNo"] + "')))";

                    var obj = DBUtils.ExecuteDynamicObject(this.Context, sql);

                    options.Add("targetId", obj[0]["targetId"].ToString());

                    this.PushFormByFormId("k0c6b452fa8154c4f8e8e5f55f96bcfac",options);
                }
                else if (pushFormId == "2")
                {
                    //下推对公资金
                    string sql = "select isnull(max(fid),0) as targetId from ora_t_PublicMoneyEntry where " +
                    "fentryid in (SELECT  FEntryID FROM ora_t_PublicMoneyEntry_LK WHERE(FSBillId = " +
                    "(select fid from ora_t_Cust100050 where fbillno = '" + options["billNo"] + "')))";

                    var obj = DBUtils.ExecuteDynamicObject(this.Context, sql);

                    options.Add("targetId", obj[0]["targetId"].ToString());

                    this.PushFormByFormId("k191b3057af6c4252bcea813ff644cd3a", options);
                }
            });
        }

        /// <summary>
        /// 根据单据唯一标识下推单据
        /// </summary>
        private void PushFormByFormId(string targetFormIdParam, Dictionary<string, string> options)
        {
            string targetId = null;

            if (options!=null) {

                targetId = options["targetId"];

                if (targetId.Equals("0")) {
                    string status = this.View.BillModel.GetValue("FDocumentStatus").ToString();
                    if (status == "Z") return;
                    string formId = this.View.BillView.GetFormId();
                    string targetFormId = "k0c6b452fa8154c4f8e8e5f55f96bcfac"; // 个人资金

                    if (targetFormIdParam != null)
                    {
                        targetFormId = targetFormIdParam;
                    }

                    var rules = ConvertServiceHelper.GetConvertRules(this.View.Context, formId, targetFormId);

                    //this.View.ShowMessage(options["costType"]);

                    var rule = rules.FirstOrDefault(t => t.IsDefault);

                    List<string> fyxmList = new List<string>
                        {
                            "FYXM0052",
                            "FYXM005201",
                            "FYXM005202",
                            "FYXM005203",
                            "FYXM005206",
                            "FYXM0098",
                            "FYXM0093"
                        };

                    if (targetFormId.Equals("k0c6b452fa8154c4f8e8e5f55f96bcfac"))
                    {
                        if (options["dept"].Equals("009") && fyxmList.Contains(options["costType"]))
                        {
                            rule = rules.Find(r => r.Id.Equals("2cae0c23-4cf6-4576-a68e-9b4878ac76df"));
                        }
                        else
                        {
                            rule = rules.Find(r => r.Id.Equals("0bafc37e-2d0e-4046-a02b-f977e4a27834"));
                        }
                    }
                    else if (targetFormId.Equals("k191b3057af6c4252bcea813ff644cd3a"))
                    {

                        if (options["dept"].Equals("009") || options["workstation"].Equals("0040004"))
                        {
                            rule = rules.Find(r => r.Id.Equals("ce020301-7b4f-4d9f-beab-f98af5a15e87"));
                        }
                        else
                        {
                            rule = rules.Find(r => r.Id.Equals("e44fcb45-9a69-46b5-83a5-d977279a1b3b"));
                        }
                    }

                    string fid = this.View.BillModel.GetPKValue().ToString();

                    ListSelectedRow[] selectedRows;
                    if (formId == "k0c30c431418e4cf4a60d241a18cb241c") // 出差申请
                    {
                        int count = this.View.BillModel.GetEntryRowCount("FEntity");
                        selectedRows = new ListSelectedRow[count];
                        for (int i = 0; i < count; i++)
                        {
                            string entryId = this.View.BillModel.GetEntryPKValue("FEntryID", i).ToString();
                            selectedRows[i] = new ListSelectedRow(fid, entryId, i, formId);
                        }
                    }
                    else
                    {
                        ListSelectedRow row = new ListSelectedRow(fid, string.Empty, 0, formId);
                        selectedRows = new ListSelectedRow[] { row };
                    }

                    // 调用下推服务，生成下游单据数据包
                    ConvertOperationResult operationResult = null;
                    PushArgs pushArgs = new PushArgs(rule, selectedRows)
                    {
                        TargetBillTypeId = "",
                        TargetOrgId = 0,
                    };
                    try
                    {
                        //执行下推操作，并获取下推结果
                        operationResult = ConvertServiceHelper.Push(this.View.Context, pushArgs, OperateOption.Create());
                    }
                    catch (KDExceptionValidate ex)
                    {
                        this.View.ShowErrMessage(ex.Message, ex.ValidateString);
                        return;
                    }
                    catch (Exception ex)
                    {
                        this.View.ShowErrMessage(ex.Message);
                        return;
                    }

                    // 获取生成的目标单据数据包
                    DynamicObject[] objs = operationResult.TargetDataEntities.Select(p => p.DataEntity).ToArray();
                    // 读取目标单据元数据
                    var targetBillMeta = MetaDataServiceHelper.Load(this.View.Context, targetFormId) as FormMetadata;
                    OperateOption option = OperateOption.Create();
                    // 忽略全部需要交互性质的提示
                    option.SetIgnoreWarning(true);
                    // 暂存数据
                    var saveResult = BusinessDataServiceHelper.Draft(this.View.Context, targetBillMeta.BusinessInfo, objs, option);
                    targetId = saveResult.SuccessDataEnity.Select(item => item["Id"].ToString()).Distinct().FirstOrDefault();
                }
   
            }

 
            // 打开目标单据
            if(targetId != null)
            {
                MobileShowParameter param = new MobileShowParameter();

                if (targetFormIdParam.Equals("k0c6b452fa8154c4f8e8e5f55f96bcfac"))
                {
                    param.Caption = "个人资金申请";
                    param.FormId = "ora_GRZJJZ";
                }
                else if (targetFormIdParam.Equals("k191b3057af6c4252bcea813ff644cd3a"))
                {
                    param.Caption = "对公资金申请";
                    param.FormId = "ora_DGZJSQ";
                }
     
                param.PKey = targetId;
                param.ParentPageId = this.View.PageId;
                param.Status = OperationStatus.EDIT;
                param.OpenStyle.ShowType = ShowType.Default;

                this.View.ShowForm(param);
            }
        }

        /// <summary>
        /// 提交后执行
        /// </summary>
        /// <param name="e"></param>
        public override void AfterDoOperation(AfterDoOperationEventArgs e)
        {
            base.AfterDoOperation(e);
            
            string opKey = e.Operation.Operation.ToUpperInvariant();
            switch (opKey)
            {
                case "SAVE":
                    CZ_DoFileUpLoad();
                    break;
                case "SUBMIT":

                    bool success = e.OperationResult.IsSuccess;

                    if (success) {
                        CZ_DoFileUpLoad();
                    }

                    break;
            }
        }

        /// <summary>
        /// 附件上传至本地临时目录后的回调函数
        /// PlugIn.Args.MobileUploadEventArgs
        /// Kingdee.BOS.Mobile.PlugIn.Args.MobileUploadEventArgs
        /// </summary>
        /// <param name="e">PlugIn.Args.MobileUploadEventArgs e</param>
        public override void AfterMobileUpload(Kingdee.BOS.Mobile.PlugIn.Args.MobileUploadEventArgs e)
        {
            // 获取服务器临时目录    HttpContext.Current.Request.PhysicalApplicationPath + KeyConst.TEMPFILEPATH;
            string tempDirPath = HttpContext.Current.Request.PhysicalApplicationPath + KeyConst.TEMPFILEPATH;
            // 获取附件表的元数据类
            var formMetadata = (FormMetadata)MetaDataServiceHelper.Load(this.Context, FormIdConst.BOS_Attachment);

            //TY:以下是取值 by 田杨
            //string _BillType = this.View.BillBusinessInfo.GetForm().Id;
            string _BillType = this.View.BillView.BusinessInfo.GetForm().Id;
            string _BillNo = this.View.BillModel.DataObject["BillNo"].ToString();   //原始BOS单据【绑定实体属性】
            string _InterID = this.View.BillModel.DataObject["Id"].ToString();

            /*  示例代码 从5.0
            //_BillType = this.View.BillView.BusinessInfo.GetForm().Id;
            //_BillNo = this.View.BillModel.DataObject["BillNo"];
            //dyn["BillStatus"] = this.View.BillModel.DataObject["DocumentStatus"];
            //_InterID = this.View.BillModel.DataObject["Id"];
            */

            List<DynamicObject> dynList = new List<DynamicObject>();
            StringBuilder sb = new StringBuilder();

            foreach (FiledUploadEntity file in e.FileNameArray)
            {
                //在版本PT139774 [7.3.1351.3]及以上，增加了FileId，并且新上传的附件临时ID是一个小数，可以以此来判断是否新上传附件
                if (file.FileId.IndexOf("0.") < 0)
                {
                    continue;
                }

                // 检查文件是否成功上传到临时目录
                if (!file.IsSuccess)
                {
                    continue;
                }

                // 检查文件是否存在于临时目录
                var fileName = System.IO.Path.Combine(tempDirPath, file.FileName);
                if (!System.IO.File.Exists(fileName))
                {
                    continue;
                }

                /**
                 * 此处几个关键属性解读：
                 * 1. BillType  关联的模型的FormId
                 * 2. BillNo    关联的单据编号，用于确定此附件是属于哪张单据
                 * 3. InterID   关联的单据/基础资料ID，附件列表就是根据这个ID进行加载
                 * 4. EntryInterID  关联的单据体ID，这里我们只演示单据头，所以固定设置为-1
                 * 5. AttachmentSize    系统统一按照KB单位进行显示，所以需要除以1024
                 * 6. FileStorage   文件存储类型，0为数据库，1为文件服务，2为亚马逊云，3为金蝶云
                 */
                var dataBuff = System.IO.File.ReadAllBytes(fileName);
                var dyn = new DynamicObject(formMetadata.BusinessInfo.GetDynamicObjectType());

                if (submitType != 0)
                {
                    // 将数据上传至文件服务器，并返回上传结果
                    var result = this.UploadAttachment(file.FileName, dataBuff);
                    if (!result.Success)
                    {
                        // 上传失败，收集失败信息
                        sb.AppendLine(string.Format("附件：{0}，上传失败：{1}", file.FileName, result.Message));
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

                /*  示例代码 从6.1
                //// 此处我们不绑定到特定的单据，为了简化示例，只实现单纯的文件上传与下载
                //dyn["BillType"] = "Test_MOB_Accessory"; // 虚拟FormId
                //dyn["BillNo"] = "A00001"; // 虚拟的单据编号
                //dyn["InterID"] = "D00001"; // 虚拟的InterId，这个ID将作为我们下载的识别标识

                *  示例代码 从5.0
                // 而实际插件开发可以从移动单据中获取到这些数据
                //dyn["BillType"] = this.View.BillView.BusinessInfo.GetForm().Id;
                //dyn["BillNo"] = this.View.BillModel.DataObject["BillNo"];
                ////dyn["BillStatus"] = this.View.BillModel.DataObject["DocumentStatus"];
                //dyn["InterID"] = this.View.BillModel.DataObject["Id"];
                */

                dyn["BillType"] = _BillType;
                dyn["BillNo"] = _BillNo;
                dyn["InterID"] = _InterID;

                // 上传文件服务器成功后才加入列表
                dyn["AttachmentName"] = file.OldName;
                dyn["AttachmentSize"] = Math.Round(dataBuff.Length / 1024.0, 2);
                dyn["EntryInterID"] = -1;// 参照属性解读

                dyn["CreateMen_Id"] = Convert.ToInt32(this.Context.UserId);
                dyn["CreateMen"] = GetUser(this.Context.UserId.ToString());
                dyn["ModifyTime"] = dyn["CreateTime"] = TimeServiceHelper.GetSystemDateTime(this.Context);
                dyn["ExtName"] = System.IO.Path.GetExtension(file.OldName);
                dyn["FileStorage"] = submitType.ToString();
                dyn["EntryKey"] = " ";
                dyn["IsAllowDownLoad"] = 0;//参考PC端，历史原因 0 允许下载，1 不允许下载

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
            base.AfterMobileUpload(e);

            this.View.ShowMessage("提交成功，是否退出页面？",
                MessageBoxOptions.YesNo,
                new Action<MessageBoxResult>((result) =>
                {
                    if (result == MessageBoxResult.Yes)
                    {
                        //关闭页面
                        JSONObject arg = new JSONObject();
                        arg.Put("pageId", this.View.PageId);
                        this.View.AddAction("closeWebViewWithXT", arg);
                    }
                })
            );
            
            //Jump2Audit();
        }

        /// <summary>
        /// 选择前过滤数据
        /// </summary>
        /// <param name="e"></param>
        //public override void BeforeF7Select(BeforeF7SelectEventArgs e)
        //{
        //    base.BeforeF7Select(e);
        //    switch (e.FieldKey)
        //    {
        //        case "FApply":
        //            FilterStaffByOrganization(e);
        //            break;
        //        case "FApplyID":
        //            FilterStaffByOrganization(e);
        //            break;
        //        case "F_ora_Applicant":
        //            FilterStaffByOrganization(e);
        //            break;
        //    }
        //}
        #endregion


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
    }
}
