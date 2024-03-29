﻿using Kingdee.BOS;
using Kingdee.BOS.App.Data;
using Kingdee.BOS.Core;
using Kingdee.BOS.Core.DynamicForm;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Core.Metadata;
using Kingdee.BOS.FileServer.Core;
using Kingdee.BOS.FileServer.Core.Object;
using Kingdee.BOS.FileServer.ProxyService;
using Kingdee.BOS.Mobile.Metadata.ControlDataEntity;
using Kingdee.BOS.Mobile.PlugIn;
using Kingdee.BOS.Mobile.PlugIn.ControlModel;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.ServiceHelper;
using Kingdee.BOS.Util;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Web;

namespace CZ.CEEG.MblCrm.CustManager
{
    [Description("Mbl客户管理")]
    [HotUpdate]
    public class CZ_CEEG_MblCrm_CustManager : AbstractMobileBillPlugin
    {
        public override void AfterBindData(EventArgs e)
        {
            base.AfterBindData(e);
            string ds = this.View.BillModel.GetValue("FDocumentStatus").ToString();
            if(ds == "Z" || ds == "A")
            {
                this.View.BillModel.SetValue("FCreateOrgId", 1);
                Act_SetSalerByUser();
                var submitBtn = this.View.GetControl("FSubmitBtn");
                submitBtn.SetCustomPropertyValue("width", 310);
                this.View.GetControl("FSaveBtn").Visible = false;
            }
            if (ds == "B" || ds == "D")
            {
                this.View.GetControl("FSubmitBtn").Visible = false;
                var saveBtn = this.View.GetControl("FSaveBtn");
                saveBtn.SetCustomPropertyValue("width", 310);
            }
            if(ds == "C")
            {
                this.View.GetControl("FSubmitBtn").Visible = false;
                this.View.GetControl("FSaveBtn").Visible = false;
            }
        }


        public override void DataChanged(DataChangedEventArgs e)
        {
            base.DataChanged(e);
            Act_CatAddress();
            
        }

        public override void BeforeDoOperation(BeforeDoOperationEventArgs e)
        {
            base.BeforeDoOperation(e);
            string opKey = e.Operation.FormOperation.Operation.ToUpperInvariant();
            switch (opKey)
            {
                case "SAVE":
                    //if (!Act_OnlyValidatePass())
                    //{
                    //    e.Cancel = true;
                    //}
                    Act_CatAddress();
                    break;
                case "SUBMIT":
                    if (!Act_OnlyValidatePass())
                    {
                        e.Cancel = true;
                    }
                    //this.View.BillModel.SetValue("FNumber", "dfajskldfjas");
                    Act_CatAddress();
                    break;
            }
        }



        public override void AfterDoOperation(AfterDoOperationEventArgs e)
        {

            base.AfterDoOperation(e);

            bool success = e.OperationResult.IsSuccess;

            if (success)
            {
                
                CZ_DoFileUpLoad();
                string opKey = e.Operation.Operation.ToUpperInvariant();
                switch (opKey)
                {
                    case "SUBMIT":
                        string FID = this.View.BillModel.DataObject["Id"].ToString();
                        Act_CreateDefaultContract(FID);
                        this.View.ShowMessage("提交成功！", MessageBoxOptions.OK, (result) =>
                        {
                            this.View.Close();
                        });
                        break;
                }
            }
        }

        /// <summary>
        /// 客户名称唯一性检验
        /// </summary>
        /// <returns></returns>
        private bool Act_OnlyValidatePass()
        {
            string FCustName = this.View.BillModel.GetValue("FName") == null ? "" : (this.View.BillModel.GetValue("FName") as LocaleValue)[this.Context.UserLocale.LCID].ToString();
            string sql = string.Format(@"SELECT c.FCUSTID FROM T_BD_CUSTOMER_L cl
            INNER JOIN T_BD_CUSTOMER c ON cl.FCUSTID=c.FCUSTID 
            WHERE FNAME='{0}' AND FLOCALEID=2052 And c.FDocumentStatus IN ('B','C')", FCustName);
            var custs = DBUtils.ExecuteDynamicObject(this.Context, sql);
            if(custs.Count > 0)
            {
                this.View.ShowMessage("此客户在系统中已经存在，请勿重新创建！");
                return false;
            }
            return true;
        }


        /// <summary>
        /// 带出默认销售员
        /// </summary>
        private void Act_SetSalerByUser()
        {
            string userId = this.Context.UserId.ToString();
            string orgId = this.View.BillModel.GetValue("FCreateOrgId") == null ? "0" : (this.View.BillModel.GetValue("FCreateOrgId") as DynamicObject)["Id"].ToString();
            string sql = "EXEC proc_czly_GetSalesmanIdByUserId @FUserId='" + userId + "',@FOrgId='" + orgId + "'";
            var objs = DBUtils.ExecuteDynamicObject(this.Context, sql);
            if(objs.Count > 0)
            {
                this.View.BillModel.SetValue("FSELLER", objs[0]["FSalesmanId"].ToString());
            }
            
        }

        /// <summary>
        /// 创建客户默认联系人
        /// </summary>
        /// <param name="FCustID"></param>
        private void Act_CreateDefaultContract(string FCustID)
        {
            string _FName = this.View.BillModel.GetValue("FContractorName") == null ? "" : this.View.BillModel.GetValue("FContractorName").ToString();
            string _FMobile = this.View.BillModel.GetValue("FTEL") == null ? "" : this.View.BillModel.GetValue("FTEL").ToString();
            if(_FName == "" || _FMobile == "")
            {
                return;
            }
            string _FUserID = this.Context.UserId.ToString();
            string sql = string.Format("EXEC proc_czly_CreateCustContactor @FName='{0}',@FMobile='{1}',@FUserID='{2}',@FCustID='{3}'",
                                        _FName, _FMobile, _FUserID, FCustID);
            var objs = DBUtils.ExecuteDynamicObject(this.Context, sql);
            if(objs.Count > 0)
            {
                string FContactId = objs[0]["FContactId"].ToString();
                this.View.BillModel.SetValue("FDefaultContact", FContactId);
            }
        }

        /// <summary>
        /// 拼接客户详细地址
        /// </summary>
        private void Act_CatAddress()
        {
            string coutry = this.View.BillModel.GetValue("FCOUNTRY") == null ? "" : (this.View.BillModel.GetValue("FCOUNTRY") as DynamicObject)["FDataValue"].ToString();
            string province = this.View.BillModel.GetValue("FPROVINCE") == null ? "" : (this.View.BillModel.GetValue("FPROVINCE") as DynamicObject)["FDataValue"].ToString();
            string city = this.View.BillModel.GetValue("FCITY") == null ? "" : (this.View.BillModel.GetValue("FCITY") as DynamicObject)["FDataValue"].ToString();
            string detaileAdr = this.View.BillModel.GetValue("FDetailedAdr") == null ? "" : this.View.BillModel.GetValue("FDetailedAdr").ToString();
            if(coutry != "" && province != "" && city != "" && detaileAdr != "")
            {
                string fullAdr = coutry + province + city + detaileAdr;
                this.View.BillModel.SetValue("FADDRESS", fullAdr);
            }
        }


        #region 附件上传
        /// <summary>
        /// 上传类型, 0为数据库，1为文件服务，2为亚马逊云，3为金蝶云
        /// </summary>
        private int submitType = 0;

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
                less = (dataBuff.Length - len) >= 4096 ? 4096 : (dataBuff.Length - len);
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
            var _BillNo = this.View.BillModel.DataObject["Number"];
            if (_BillNo != null)
            {
                FileUploadControl _FFileUpdateCtl = null;
                try
                {
                    _FFileUpdateCtl = this.View.GetControl<FileUploadControl>("FFileUpdate");
                }
                catch (Exception) { }

                if (_FFileUpdateCtl != null) _FFileUpdateCtl.UploadFieldBatch();
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
            string _BillNo = this.View.BillModel.DataObject["Number"].ToString();   //原始BOS单据【绑定实体属性】
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
                dyn["AttachmentName"] = file.FileName;
                dyn["AttachmentSize"] = Math.Round(dataBuff.Length / 1024.0, 2);
                dyn["EntryInterID"] = -1;// 参照属性解读

                dyn["CreateMen_Id"] = Convert.ToInt32(this.Context.UserId);
                dyn["CreateMen"] = GetUser(this.Context.UserId.ToString());
                dyn["ModifyTime"] = dyn["CreateTime"] = TimeServiceHelper.GetSystemDateTime(this.Context);
                dyn["ExtName"] = System.IO.Path.GetExtension(file.FileName);
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

            base.AfterMobileUpload(e);
        }

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

        #endregion
    }
}
