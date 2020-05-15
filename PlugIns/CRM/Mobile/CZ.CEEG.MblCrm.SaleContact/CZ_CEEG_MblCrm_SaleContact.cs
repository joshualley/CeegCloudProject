using Kingdee.BOS.App.Data;
using Kingdee.BOS.Core;
using Kingdee.BOS.Core.DynamicForm;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Core.Metadata;
using Kingdee.BOS.JSON;
using Kingdee.BOS.Mobile.Metadata.ControlDataEntity;
using Kingdee.BOS.Mobile.PlugIn;
using Kingdee.BOS.Mobile.PlugIn.ControlModel;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.ServiceHelper;
using Kingdee.BOS.Util;
using Kingdee.BOS.FileServer.Core;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Web;
using Kingdee.BOS.FileServer.ProxyService;
using Kingdee.BOS.FileServer.Core.Object;
using Kingdee.BOS.Core.Const;
using Kingdee.BOS.Core.DynamicForm.PlugIn.ControlModel;
using Kingdee.BOS.Mobile;

namespace CZ.CEEG.MblCrm.SaleContact
{
    [Description("销售合同评审")]
    [HotUpdate]
    public class CZ_CEEG_MblCrm_SaleContact : AbstractMobileBillPlugin
    {
        #region override
        public override void AfterBindData(EventArgs e)
        {
            base.AfterBindData(e);
            EntryEditEnable();
            HideBtn();
            this.View.GetControl("FRowBtn").Visible = false;
        }

        public override void AfterDoOperation(AfterDoOperationEventArgs e)
        {
            base.AfterDoOperation(e);
            CZ_DoFileUpLoad();
            string op = e.Operation.Operation.ToUpperInvariant();
            switch (op)
            {
                case "SUBMIT":
                    //如果是下推生成的单据，则建立起它们的关联关系
                    CreateBillRelation();
                    Jump2Audit();
                    CheckRptChanged();
                    //CustomerToOfficial();
                    break;
                case "SAVE":
                    //如果是下推生成的单据，则建立起它们的关联关系
                    CreateBillRelation();
                    break;
            }
        }

        public override void DataChanged(DataChangedEventArgs e)
        {
            base.DataChanged(e);
            string key = e.Field.Key.ToUpperInvariant();
            switch (key)
            {
                case "FRPTPRICEMB": //报价
                    if (!IsMainBodyRptPrice(e.Row))
                    {
                        this.View.BillModel.SetValue("FBDownPoints", 0, e.Row);
                    }
                    Act_CalRowDownPoints(e.Row);
                    Act_CalDownPoints1();
                    break;
                
            }
        }

        /// <summary>
        /// 报价明细当前选中行
        /// </summary>
        private int currSelectedRow = 0;

        public override void EntityRowDoubleClick(EntityRowClickEventArgs e)
        {
            base.EntityRowDoubleClick(e);
            currSelectedRow = e.Row - 1;
            OpenDetailedPage();
            //this.View.GetControl("FBJRow").SetCustomPropertyValue("forecolor", "#ffff99");
        }

        public override void ButtonClick(ButtonClickEventArgs e)
        {
            base.ButtonClick(e);
            string key = e.Key.ToUpperInvariant();
            switch (key)
            {
                case "FDETAILED":
                    //OpenDetailedPage();
                    break;
            }
        }

        public override void BeforeF7Select(BeforeF7SelectEventArgs e)
        {
            base.BeforeF7Select(e);
            string key = e.FieldKey.ToUpperInvariant();
            switch (key)
            {
                case "FCUSTNAME": //客户
                    //string filter = FilterCustomerBySeller();
                    //e.ListFilterParameter.Filter = filter;
                    break;
            }
        }

        #endregion

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
            var _BillNo = this.View.BillModel.DataObject["BillNo"];
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
        /// 获取变量值 一般值
        /// </summary>
        /// <param name="_prm">普通控件名称</param>
        /// <returns></returns>
        public string CZ_GetValue(string _prm)
        {
            return this.View.BillModel.GetValue(_prm) == null ? "" : this.View.BillModel.GetValue(_prm).ToString();
        }
        /// <summary>
        /// 获取变量值 基础资料关联
        /// </summary>
        /// <param name="_obj">基础资料控件名称</param>
        /// <param name="_prm">值序列名称</param>
        /// <returns></returns>
        public string CZ_GetValue(string _obj, string _prm)
        {
            return (this.View.BillModel.GetValue(_obj) as DynamicObject) == null ? "" : (this.View.BillModel.GetValue(_obj) as DynamicObject)[_prm].ToString();
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

        #region 通用业务

        /// <summary>
        /// 根据流程显示隐藏按钮
        /// </summary>
        private void HideBtn()
        {
            string _FDocumentStatus = this.CZ_GetValue("FDocumentStatus");
            if (_FDocumentStatus == "Z" || _FDocumentStatus == "A" || _FDocumentStatus == "D")
            {
                var submitBtn = this.View.GetControl("FSubmitBtn");
                var saveBtn = this.View.GetControl("FSaveBtn");
                saveBtn.Visible = false;
                submitBtn.SetCustomPropertyValue("width", 310);
            }
            else if (_FDocumentStatus == "B")
            {
                var submitBtn = this.View.GetControl("FSubmitBtn");
                var saveBtn = this.View.GetControl("FSaveBtn");
                submitBtn.Visible = false;
                saveBtn.SetCustomPropertyValue("width", 310);
            }
            else if (_FDocumentStatus == "C")
            {
                var submitBtn = this.View.GetControl("FSubmitBtn");
                var saveBtn = this.View.GetControl("FSaveBtn");
                saveBtn.Visible = false;
                submitBtn.Visible = false;
            }
        }

        #region 废弃代码
        /// <summary>
        /// 客户转正式
        /// </summary>
        private void CustomerToOfficial()
        {
            //string _FCustID = this.View.BillModel.GetValue("FCustName") == null ? "0" : (this.View.BillModel.GetValue("FCustName") as DynamicObject)["Id"].ToString();
            //string _sql = "SELECT * FROM T_BD_CUSTOMER WHERE FCUSTID='" + _FCustID + "'";
            //var objs = CZDB_GetData(_sql);
            //if(objs.Count > 0)
            //{
            //    string _FIsTrade = objs[0]["FIsTrade"].ToString();
            //    if(_FIsTrade == "0")
            //    {
            //        _sql = "UPDATE T_BD_CUSTOMER SET FISTRADE=1 WHERE FCUSTID='" + _FCustID +"'";
            //        CZDB_GetData(_sql);
            //        this.View.BillModel.SetValue("FIsCustToOfficial", 1);
            //    }
            //}
        }

        /// <summary>
        /// 通过销售员过滤客户，销售员仅可选择自己创建的客户
        /// </summary>
        /// <returns></returns>
        private string FilterCustomerBySeller()
        {
            string filter = " FCustID in (";
            string userId = this.Context.UserId.ToString();
            //string orgId = this.View.BillModel.GetValue("FOrgID") == null ? "0" : (this.View.BillModel.GetValue("FOrgID") as DynamicObject)["Id"].ToString();
            string sql = string.Format("EXEC proc_czly_GetSalesmanIdByUserId @FUserId='{0}'", userId);
            var objs = CZDB_GetData(sql);
            if (objs.Count > 0)
            {
                string FSalesmanIds = "";
                for (int i = 0; i < objs.Count; i++)
                {
                    if (i == objs.Count - 1)
                    {
                        FSalesmanIds += string.Format("'{0}'", objs[i]["FSalesmanId"].ToString());
                    }
                    else
                    {
                        FSalesmanIds += string.Format("'{0}',", objs[i]["FSalesmanId"].ToString());
                    }
                }
                if (objs.Count <= 0)
                {
                    FSalesmanIds += "'0'";
                }

                sql = "select FCustID from T_BD_CUSTOMER where FSELLER in (" + FSalesmanIds + ")";
                objs = CZDB_GetData(sql);

                for (int i = 0; i < objs.Count; i++)
                {
                    filter += "'" + objs[i]["FCustID"].ToString() + "'";
                    if (i < objs.Count - 1) filter += ",";
                }
                if (objs.Count <= 0)
                {
                    filter += "'0'";
                }
            }
            else
            {
                filter += "'0'";
            }

            filter += ')';

            return filter;
        }
        #endregion

        /// <summary>
        /// 检查报价变更
        /// </summary>
        private void CheckRptChanged()
        {
            string _FRptChange = "报价明细表体中报价变更如下：\n";
            string _BJBillNo = CZ_GetValue("FNicheID");
            string _sql = string.Format("SELECT * FROM ora_CRM_SaleOfferBPR WHERE FID=(SELECT FID FROM ora_CRM_SaleOffer WHERE FBILLNO='{0}')", _BJBillNo);
            var objs = CZDB_GetData(_sql);
            if (objs.Count <= 0)
            {
                return;
            }
            var _FEntityBPR = this.View.BillModel.DataObject["FEntityBPR"] as DynamicObjectCollection;
            foreach (var CtcRow in _FEntityBPR)
            {
                int rowEid = int.Parse(CtcRow["FBprEID"].ToString());
                foreach (var SoRow in objs)
                {
                    int eid = int.Parse(SoRow["FBEntryID"].ToString());
                    if (rowEid == eid)
                    {
                        string ctcRpt = double.Parse(CtcRow["FBRptPrice"].ToString()).ToString("0.00");
                        string soRpt = double.Parse(SoRow["FBRptPrice"].ToString()).ToString("0.00");
                        if (ctcRpt != soRpt)
                        {
                            string rowNum = CtcRow["SEQ"].ToString();
                            _FRptChange += string.Format("序号为{0}的行，报价从{1}元变更为{2}元。\n", rowNum, soRpt, ctcRpt);
                        }
                    }
                }
            }
            this.View.BillModel.SetValue("FRptChange", _FRptChange);
            //保存
            IOperationResult saveResult = BusinessDataServiceHelper.Save(
                this.Context,
                this.View.BillView.BillBusinessInfo,
                this.View.BillModel.DataObject
            );
        }

        /// <summary>
        /// 打开报价详情页面
        /// </summary>
        private void OpenDetailedPage()
        {
            var entity = this.View.BillModel.DataObject["FEntityBPR"] as DynamicObjectCollection;

            string FMtlGroup = (entity[currSelectedRow]["FBMtlGroup"] as DynamicObject)["Id"].ToString();
            string FMtlItem = (entity[currSelectedRow]["FBMtlItem"] as DynamicObject)["Id"].ToString();
            string FQty = entity[currSelectedRow]["FBQty"].ToString();
            string FPAmt = entity[currSelectedRow]["FBPAmt"].ToString();

            string FBGUID = entity[currSelectedRow]["FBGUID"].ToString();
            int mbRow = GetMainBodyRowInGroup(FBGUID);
            string FPAmtGroup = entity[mbRow]["FBPAmtGroup"].ToString(); //获取组基价汇总，位于本体行上，单独附件位于组内第一行
            string FBRangeAmtGP = entity[mbRow]["FBRangeAmtGP"].ToString(); //组扣款
            double FRptPriceGroup = -double.Parse(entity[currSelectedRow]["FBRptPrice"].ToString()); //累加组内减去本行的报价
            foreach (var row in entity)
            {
                if (row["FBGUID"].ToString() == FBGUID)
                {
                    FRptPriceGroup += double.Parse(row["FBRptPrice"].ToString());
                }
            }

            //string FPAmtGroup = entity[currSelectedRow]["FBPAmtGroup"].ToString();
            string FDescribe = entity[currSelectedRow]["FBDescribe"].ToString();
            string FModel = entity[currSelectedRow]["FBModel"].ToString();
            string FRptPrice = entity[currSelectedRow]["FBRptPrice"].ToString();
            string FDownPoints = entity[currSelectedRow]["FBDownPoints"].ToString();

            var para = new MobileShowParameter();
            para.FormId = "ora_CRM_MBL_BJD"; //报价详情的标识
            para.OpenStyle.ShowType = ShowType.Modal; //打开方式
            para.Width = 240;
            para.Height = 257;
            para.ParentPageId = this.View.PageId;
            para.CustomParams.Add("FMtlGroup", FMtlGroup);
            para.CustomParams.Add("FMtlItem", FMtlItem);
            para.CustomParams.Add("FQty", FQty);
            para.CustomParams.Add("FPAmt", FPAmt);
            para.CustomParams.Add("FPAmtGroup", FPAmtGroup);
            para.CustomParams.Add("FRangeAmtGP", FBRangeAmtGP);
            para.CustomParams.Add("FDescribe", FDescribe);
            para.CustomParams.Add("FModel", FModel);
            para.CustomParams.Add("FRptPrice", FRptPrice);
            para.CustomParams.Add("FRptPriceGroup", FRptPriceGroup.ToString());
            para.CustomParams.Add("FDownPoints", FDownPoints);

            if (CZ_GetValue("FDocumentStatus") == "A" || CZ_GetValue("FDocumentStatus") == "Z")
            {
                para.Status = OperationStatus.EDIT;
            }
            else
            {
                para.Status = OperationStatus.VIEW;
            }

            this.View.ShowForm(para, formResult =>
            {
                if (formResult.ReturnData == null)
                {
                    return;
                }
                FRptPrice = formResult.ReturnData.ToString();
                this.View.BillModel.SetValue("FBRptPrice", FRptPrice, currSelectedRow);
                this.View.InvokeFieldUpdateService("FBRptPrice", currSelectedRow);
            });
        }
        /// <summary>
        /// 是否主体报价
        /// </summary>
        private bool IsMainBodyRptPrice(int i)
        {
            string _FBMtlItem = this.View.BillModel.GetValue("FBMtlItem", i) == null ? "" : (this.View.BillModel.GetValue("FBMtlItem", i) as DynamicObject)["Id"].ToString();
            if (_FBMtlItem == "123328")
                return true;

            return false;
        }

        /// <summary>
        /// 计算表头总下浮点数（平均），行最大下浮点数
        /// </summary>
        private void Act_CalDownPoints()
        {
            int count = this.View.BillModel.GetEntryRowCount("FEntityBPR");
            string _FBMtlItem;
            float _FBDownPoints;
            float max = 0;
            float avg = 0;
            int cnt = 0;
            for (int i = 0; i < count; i++)
            {
                //获取材料组成
                _FBMtlItem = this.View.BillModel.GetValue("FBMtlItem", i) == null ? "" : (this.View.BillModel.GetValue("FBMtlItem", i) as DynamicObject)["Id"].ToString();
                if (_FBMtlItem == "123328")
                {
                    _FBDownPoints = float.Parse(this.View.BillModel.GetValue("FBDownPoints", i).ToString());
                    if (cnt == 0) max = _FBDownPoints;
                    cnt++;
                    avg += _FBDownPoints;
                    if (_FBDownPoints > max)
                    {
                        max = _FBDownPoints;
                    }
                }
            }
            avg = avg / cnt;
            this.View.BillModel.SetValue("FMaxDnPts", max);
            this.View.BillModel.SetValue("FAvgDnPts", avg);
        }

        private void Act_CalDownPoints1()
        {
            int count = this.View.BillModel.GetEntryRowCount("FEntity");
            float _FBDownPoints;
            float max = float.MinValue;
            float avg = 0;
            int cnt = 0;
            for (int i = 0; i < count; i++)
            {
                string guid = this.View.BillModel.GetValue("FGUID", i).ToString();
                int row = GetMainBodyRowInGroup(guid);
                if (row != -1)
                {
                    _FBDownPoints = float.Parse(this.View.BillModel.GetValue("FBDownPoints", row).ToString());
                    if (cnt == 0) max = _FBDownPoints;
                    cnt++;
                    avg += _FBDownPoints;

                    if (_FBDownPoints > max)
                    {
                        max = _FBDownPoints;
                    }
                }
            }
            avg = avg / cnt;
            this.View.BillModel.SetValue("FMaxDnPts", max);
            this.View.BillModel.SetValue("FAvgDnPts", avg);
        }

        /// <summary>
        /// 计算组报价汇总后的行下浮点数
        /// </summary>
        private void Act_CalRowDownPoints(int Row)
        {
            int count = this.View.BillModel.GetEntryRowCount("FEntityBPR");
            //string _FBMtlItem;
            double _FBRptPrice = 0;
            string _FBGUID = this.View.BillModel.GetValue("FBGUID", Row).ToString();
            string guid = "";

            for (int i = 0; i < count; i++)
            {
                guid = this.View.BillModel.GetValue("FBGUID", i).ToString();
                if (_FBGUID == guid)
                {
                    _FBRptPrice += double.Parse(this.View.BillModel.GetValue("FBRptPrice", i).ToString());
                }
            }

            int mbRow = GetMainBodyRowInGroup(_FBGUID);
            double _FBPAmtGroup = double.Parse(this.View.BillModel.GetValue("FBPAmtGroup", mbRow).ToString());   //组基价
            double _FBRangeAmtGP = double.Parse(this.View.BillModel.GetValue("FBRangeAmtGP", mbRow).ToString()); //组扣款
            double _FBDownPoints = _FBRptPrice == 0 ? 0 : (_FBPAmtGroup - _FBRptPrice + _FBRangeAmtGP) * 100 / _FBPAmtGroup;
            this.View.BillModel.SetValue("FBDownPoints", _FBDownPoints, mbRow);

            //for (int i = 0; i < count; i++)
            //{
            //    //_FBMtlItem = this.View.BillModel.GetValue("FBMtlItem", i) == null ? "0" : (this.View.BillModel.GetValue("FBMtlItem", i) as DynamicObject)["Id"].ToString();
            //    guid = this.View.BillModel.GetValue("FBGUID", i).ToString();
            //    if (IsMainBodyRptPrice(i) && guid == _FBGUID) //本体
            //    {

            //    }
            //}
        }

        /// <summary>
        /// 组内是否存在本体，存在返回本体行号，否则返回组内第一行行号，表体数据不存在返回-1
        /// </summary>
        /// <param name="guid">组ID标识</param>
        /// <returns></returns>
        private int GetMainBodyRowInGroup(string guid)
        {
            var entity = this.View.BillModel.DataObject["FEntityBPR"] as DynamicObjectCollection;
            if (entity.Count <= 0)
            {
                return -1;
            }
            string _FBMtlItem;
            string _FBGUID;
            List<int> rowNums = new List<int>();
            for (int i = 0; i < entity.Count; i++)
            {
                _FBGUID = entity[i]["FBGUID"] == null ? "" : entity[i]["FBGUID"].ToString();
                if (_FBGUID == guid)
                {
                    _FBMtlItem = entity[i]["FBMtlItem"] == null ? "0" : (entity[i]["FBMtlItem"] as DynamicObject)["Id"].ToString();
                    if (_FBMtlItem == "123328") //存在本体
                    {
                        return i;
                    }
                    rowNums.Add(i);
                }
            }
            return rowNums.Min();
        }

        /// <summary>
        /// 提醒跳转，附件上传后执行
        /// </summary>
        private void Jump2Audit()
        {
            this.View.ShowMessage("提交成功，是否退出页面？",
                MessageBoxOptions.YesNo,
                new Action<MessageBoxResult>((result) =>
                {
                    if (result == MessageBoxResult.Yes)
                    {
                        //关闭页面
                        this.View.Close();
                        //JSONObject arg = new JSONObject();
                        //arg.Put("pageId", this.View.PageId);
                        //this.View.AddAction("closeWebViewWithXT", arg);
                        //JSONArray paras = new JSONArray();
                        //JSONObject obj = new JSONObject();
                        //obj["url"] = "http://erp.ceegpower.com/K3Cloud/mobile/k3cloud.html?entryrole=XT&appid=10037&formId=MOB_WFTodoList&formType=mobilelist&acctid=5d3ea17f85b053";
                        //obj["title"] = "业务审批";
                        //paras.Add(obj);
                        //this.View.AddAction("openUrlWindow", paras);
                    }
                })
            );
        }


        /// <summary>
        /// 接收下推并保存时创建单据关联，需要在保存完成后执行
        /// </summary>
        private void CreateBillRelation()
        {
            /*
                @lktable varchar(30),--下游单据关联表
                @targetfid int,--下游单据头内码
                @targettable varchar(30),--下游单据头表名
                @targetformid varchar(36),--下游单据标识
                @sourcefid int,--上游单据头内码
                @sourcetable varchar(30),--上游单据头表名
                @sourceformid varchar(36),--上游单据标识
                @sourcefentryid int = 0, --上游单据体内码
                @sourcefentrytable varchar(30) = '' -- 上游单据体表名
             */
            string FTgtID = this.View.BillModel.DataObject["Id"].ToString();
            string sql = string.Format("SELECT * from T_BF_INSTANCEENTRY where FTTABLENAME = 'ora_CRM_Contract' and FTID = '{0}'", FTgtID);
            var objs = CZDB_GetData(sql);
            if (objs.Count > 0)
            {
                return;
            }
            //string _FID = this.View.OpenParameter.GetCustomParameter("FID").ToString();
            string _FBillNo = this.View.BillModel.GetValue("FNicheID") == null ? "" : this.View.BillModel.GetValue("FNicheID").ToString();
            sql = "select FID,FNicheID from ora_CRM_SaleOffer where FBillNo='" + _FBillNo + "'";
            var data = CZDB_GetData(sql);
            if (data.Count <= 0)
                return;
            
            string lktable = "ora_CRM_Contract_LK";
            string targetfid = FTgtID;
            string targettable = "ora_CRM_Contract";
            string targetformid = "ora_CRM_Contract";
            string sourcefid = data[0]["FID"].ToString();
            string sourcetable = "ora_CRM_SaleOffer";
            string sourceformid = "ora_CRM_SaleOffer";
            string sourcefentryid = "0";
            string sourcefentrytable = "";
            sql = String.Format(@"exec proc_czly_CreateBillRelation 
                   @lktable='{0}',@targetfid='{1}',@targettable='{2}',
                   @targetformid='{3}',@sourcefid='{4}',@sourcetable='{5}',
                   @sourceformid='{6}',@sourcefentryid='{7}',@sourcefentrytable='{8}'",
                   lktable, targetfid, targettable, targetformid, sourcefid, sourcetable, sourceformid, sourcefentryid, sourcefentrytable);
            var obj = CZDB_GetData(sql);

            //修改商机销售状态为签单阶段
            sql = "update ora_CRM_Niche set FSaleStatus='6' where FBillNo='" + data[0]["FNicheID"].ToString() + "'";
            CZDB_GetData(sql);
            sql = "update ora_CRM_SaleOffer set FBillStatus1=1 where FID='" + data[0]["FID"].ToString() + "'";
            CZDB_GetData(sql);

        }

        #endregion

        #region 表体可编辑
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

        #endregion

        #region 数据库查询方法
        /// <summary>
        /// 基本方法 数据库查询
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

        #region 弃用下推
        /// <summary>
        /// 获取销售报价数据
        /// </summary>
        private void FromSaleOffer()
        {
            string Flag = this.View.OpenParameter.GetCustomParameter("Flag") == null ? "" : this.View.OpenParameter.GetCustomParameter("Flag").ToString();
            if (Flag != "ADD")
            {
                return;
            }
            string _FID = this.View.OpenParameter.GetCustomParameter("FID").ToString();
            //查询表头数据
            string sql = string.Format("select * from ora_CRM_SaleOffer where FID='{0}'", _FID);
            var objs = CZDB_GetData(sql);
            if (objs.Count <= 0) return;
            //客户及项目
            this.View.BillModel.SetValue("FCustName", objs[0]["FCustName"].ToString());
            this.View.BillModel.SetValue("FPrjName", objs[0]["FPrjName"].ToString());
            //this.View.BillModel.SetValue("FPrjAddress", objs[0]["FPrjAddress"].ToString());
            this.View.BillModel.SetValue("FRemarks", objs[0]["FRemarks"].ToString());
            //源单类型、编号
            //this.View.BillModel.SetValue("FSrcBillType", "ora_CRM_SaleOffer");
            this.View.BillModel.SetValue("FNicheID", objs[0]["FNicheID"].ToString());
            //持有信息
            this.View.BillModel.SetValue("FCrmHdOrgID", objs[0]["FCrmHdOrgID"].ToString());
            this.View.BillModel.SetValue("FCrmHdDept", objs[0]["FCrmHdDept"].ToString());
            this.View.BillModel.SetValue("FCrmHolder", objs[0]["FCrmHolder"].ToString());
            this.View.BillModel.SetValue("FCrmSN", objs[0]["FCrmSN"].ToString());
            //报价情况
            this.View.BillModel.SetValue("FIsExport", objs[0]["FIsExport"].ToString());    //是否出口
            this.View.BillModel.SetValue("FIsBid", objs[0]["FIsBid"].ToString());          //招投标
            this.View.BillModel.SetValue("FAmount", objs[0]["FAmount"].ToString());        //总基价
            this.View.BillModel.SetValue("FCurrencyID", objs[0]["FCurrencyID"].ToString());//币别
            this.View.BillModel.SetValue("FRangeAmt", objs[0]["FRangeAmt"].ToString());    //扣款金额
            this.View.BillModel.SetValue("FRangeRs", objs[0]["FRangeRs"].ToString());      //扣款原因
            this.View.BillModel.SetValue("FAmountRpt", objs[0]["FAmountRpt"].ToString());  //总报价
            this.View.BillModel.SetValue("FCurrencyCN", objs[0]["FCurrencyCN"].ToString());//本位币
            this.View.BillModel.SetValue("FRateType", objs[0]["FRateType"].ToString());    //扣款原因
            this.View.BillModel.SetValue("FRateType", objs[0]["FRateType"].ToString());    //汇率类型
            this.View.BillModel.SetValue("FRate", objs[0]["FRate"].ToString());            //汇率
            this.View.BillModel.SetValue("FAmtCN", objs[0]["FAmtCN"].ToString());          //本币总基价
            this.View.BillModel.SetValue("FAmtRptCN", objs[0]["FAmtRptCN"].ToString());    //付款条件
            this.View.BillModel.SetValue("FAvgDnPts", objs[0]["FAvgDnPts"].ToString());    //总下浮%
            this.View.BillModel.SetValue("FMaxDnPts", objs[0]["FMaxDnPts"].ToString());    //行最大下浮%

            //创建结转信息
            this.View.BillModel.SetValue("FHHolder", objs[0]["FCrmHolder"].ToString(), 0);
            this.View.BillModel.SetValue("FHHdOrgID", objs[0]["FCrmHdOrgID"].ToString(), 0);
            this.View.BillModel.SetValue("FHHdDept", objs[0]["FCrmHdDept"].ToString(), 0);
            this.View.BillModel.SetValue("FHSN", objs[0]["FCrmSN"].ToString(), 0);
            this.View.BillModel.SetValue("FHBegDate", DateTime.Now.ToString("yyyy-MM-dd"), 0);
            this.View.BillModel.SetValue("FHEndDate", "9999-12-31", 0);


            //产品大类列表
            sql = string.Format("select * from ora_CRM_SaleOfferEntry where FID='{0}'", _FID);
            objs = CZDB_GetData(sql);
            this.View.BillModel.BatchCreateNewEntryRow("FEntity", objs.Count);
            for (int i = 0; i < objs.Count; i++)
            {
                this.View.BillModel.SetValue("FMtlGroup", objs[i]["FMtlGroup"].ToString(), i); //产品大类
                this.View.BillModel.SetValue("FDescribe", objs[i]["FDescribe"].ToString(), i); //描述
                this.View.BillModel.SetValue("FQty", objs[i]["FQty"].ToString(), i); //数量
                this.View.BillModel.SetValue("FModel", objs[i]["FModel"].ToString(), i); //型号
                //this.View.ShowMessage(objs[i]["FIsStandard"].ToString());
                this.View.BillModel.SetValue("FIsStandard", objs[i]["FIsStandard"].ToString(), i); //标准
                this.View.BillModel.SetValue("FBPRndID", objs[i]["FBPRndID"].ToString(), i); //基价计算单ID
                this.View.BillModel.SetValue("FGUID", objs[i]["FGUID"].ToString(), i); //GU码
                this.View.BillModel.SetValue("FIS2W", objs[i]["FIS2W"].ToString(), i); //是否隐藏
            }
        }

        /// <summary>
        /// 从报价获取并设置次要表体
        /// </summary>
        private void SetSencodaryEntry()
        {
            string Flag = this.View.OpenParameter.GetCustomParameter("Flag") == null ? "" : this.View.OpenParameter.GetCustomParameter("Flag").ToString();
            if (Flag != "ADD")
                return;
            string _FID = this.View.OpenParameter.GetCustomParameter("FID").ToString();



            //材料明细
            string sql = string.Format("select * from ora_CRM_SaleOfferMtl where FID='{0}'", _FID);
            var objs = CZDB_GetData(sql);
            this.View.BillModel.BatchCreateNewEntryRow("FEntityM", objs.Count);
            for (int i = 0; i < objs.Count; i++)
            {

                this.View.BillModel.SetValue("FMGUID", objs[i]["FMGUID"].ToString(), i); //E表GUID
                this.View.BillModel.SetValue("FMMtlGroup", objs[i]["FMMtlGroup"].ToString(), i); //产品大类
                this.View.BillModel.SetValue("FMMtlItem", objs[i]["FMMtlItem"].ToString(), i); //产品组成
                this.View.BillModel.SetValue("FMClass", objs[i]["FMClass"].ToString(), i); //材料类别
                this.View.BillModel.SetValue("FMMtl", objs[i]["FMMtl"].ToString(), i); //材料
                this.View.BillModel.SetValue("FMQty", objs[i]["FMQty"].ToString(), i); //数量
                this.View.BillModel.SetValue("FMModel", objs[i]["FMModel"].ToString(), i); //单位
                this.View.BillModel.SetValue("FMPrice", objs[i]["FMPrice"].ToString(), i); //单价
                this.View.BillModel.SetValue("FMAmt", objs[i]["FMAmt"].ToString(), i); //行金额
                this.View.BillModel.SetValue("FMGpAmtB", objs[i]["FMGpAmtB"].ToString(), i); //材料总金额
                this.View.BillModel.SetValue("FMCostRate", objs[i]["FMCostRate"].ToString(), i); //费用率%
                this.View.BillModel.SetValue("FMCost", objs[i]["FMCost"].ToString(), i); //费用
                this.View.BillModel.SetValue("FMGPRate", objs[i]["FMGPRate"].ToString(), i); //毛利率%
                this.View.BillModel.SetValue("FMGP", objs[i]["FMGP"].ToString(), i); //毛利
                this.View.BillModel.SetValue("FMGpAmt", objs[i]["FMGpAmt"].ToString(), i); //合计基价金额
                this.View.BillModel.SetValue("FMGpAmtLc", objs[i]["FMGpAmtLc"].ToString(), i); //合计基价金额Lc
                this.View.BillModel.SetValue("FMIS2W", objs[i]["FMIS2W"].ToString(), i); //显示隐藏
            }



            //报价明细表体
            sql = string.Format("select * from ora_CRM_SaleOfferBPR where FID='{0}'", _FID);
            objs = CZDB_GetData(sql);
            this.View.BillModel.BatchCreateNewEntryRow("FEntityBPR", objs.Count);
            for (int i = 0; i < objs.Count; i++)
            {
                this.View.BillModel.SetValue("FBGUID", objs[i]["FBGUID"].ToString(), i); //E表GUID
                this.View.BillModel.SetValue("FBPRndSEQ", objs[i]["FBPRndSEQ"].ToString(), i); //计算单SEQ
                this.View.BillModel.SetValue("FBMtlGroup", objs[i]["FBMtlGroup"].ToString(), i); //产品大类
                this.View.BillModel.SetValue("FBMtlItem", objs[i]["FBMtlItem"].ToString(), i); //产品组成
                this.View.BillModel.SetValue("FBDescribe", objs[i]["FBDescribe"].ToString(), i); //描述
                this.View.BillModel.SetValue("FBQty", objs[i]["FBQty"].ToString(), i); //数量
                this.View.BillModel.SetValue("FBModel", objs[i]["FBModel"].ToString(), i); //型号
                this.View.BillModel.SetValue("FBIsStandard", objs[i]["FBIsStandard"].ToString(), i); //标准
                this.View.BillModel.SetValue("FBasePrice", objs[i]["FBasePrice"].ToString(), i); //基本单价
                this.View.BillModel.SetValue("FBPAmt", objs[i]["FBPAmt"].ToString(), i); //行基价金额
                this.View.BillModel.SetValue("FBPAmtGroup", objs[i]["FBPAmtGroup"].ToString(), i); //产品基价合计
                this.View.BillModel.SetValue("FBRptPrice", objs[i]["FBRptPrice"].ToString(), i); //报价
                this.View.BillModel.SetValue("FBAbaComm", objs[i]["FBAbaComm"].ToString(), i); //放弃提成%
                this.View.BillModel.SetValue("FBDownPoints", objs[i]["FBDownPoints"].ToString(), i); //下浮点数%
                this.View.BillModel.SetValue("FBWorkDay", objs[i]["FBWorkDay"].ToString(), i); //生产天数
                this.View.BillModel.SetValue("FBCostAdj", objs[i]["FBCostAdj"].ToString(), i); //成本调整
                this.View.BillModel.SetValue("FBCAReason", objs[i]["FBCAReason"].ToString(), i); //调整原因
                if (objs[i]["FBDelivery"].ToString() != "0001-01-01 00:00:00" || objs[i]["FBDelivery"] != null)
                {
                    this.View.BillModel.SetValue("FBDelivery", objs[i]["FBDelivery"].ToString(), i); //交期
                }
                this.View.BillModel.SetValue("FBPAmtLc", objs[i]["FBPAmtLc"].ToString(), i); //合计基价Lc
                this.View.BillModel.SetValue("FBRptPrcLc", objs[i]["FBRptPrcLc"].ToString(), i); //报价Lc
                this.View.BillModel.SetValue("FBIS2W", objs[i]["FBIS2W"].ToString(), i); //显示隐藏
            }

        }
        #endregion
    }
}
