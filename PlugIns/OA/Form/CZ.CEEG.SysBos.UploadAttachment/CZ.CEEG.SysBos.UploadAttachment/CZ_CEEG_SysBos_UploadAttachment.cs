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
using Kingdee.BOS.Core.Const;

namespace CZ.CEEG.SysBos.UploadAttachment
{
    [HotUpdate]
    [Description("Sys附件上传")]
    public class CZ_CEEG_SysBos_UploadAttachment : AbstractBillPlugIn
    {
        private int submitType = 0; //附件上传类型
        private JSONObject jSONObject = null; //用于获取文件上传附件中的文件名称

        #region 业务函数
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

        #region override
        public override void AfterButtonClick(AfterButtonClickEventArgs e)
        {
            base.AfterButtonClick(e);
            string key = e.Key.ToUpperInvariant();
            switch (key)
            {
                case "FUPLOADBTN": //FUploadBtn
                    string FDocumentStatus = this.View.Model.GetValue("FDocumentStatus").ToString();
                    if(FDocumentStatus == "Z")
                    {
                        this.View.ShowWarnningMessage("请保存后再上传附件！");
                    }
                    else if(FDocumentStatus == "C")
                    {
                        this.View.ShowWarnningMessage("单据已审核！");
                    }
                    else
                    {
                        if(jSONObject == null)
                        {
                            this.View.ShowWarnningMessage("请先选择附件！");
                        }
                        else
                        {
                            string reault = BindForm_UploadAttach();
                            if(reault != "")
                            {
                                isUpload = true;
                                this.View.ShowMessage(reault);
                                
                            }
                        }
                    }
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
