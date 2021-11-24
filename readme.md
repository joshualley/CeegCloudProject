# 中电电气插件文档

### OA及通用插件

------

| #    | 插件名称                                                     | 插件具体功能描述                                             |
| ---- | :----------------------------------------------------------- | :----------------------------------------------------------- |
| 1    | [CZ_CEEG_OABos_BaseDLL](https://github.com/joshualley/CeegCloudProject/blob/master/PlugIns/OA/Form/CZ.CEEG.OABos.BaseDLL/CZ.CEEG.OABos.BaseDLL/CZ_CEEG_OABos_BaseDLL.cs)<br>包含存储过程：<br>1. `proc_czty_GetLoginUser2Emp` | **`OA-PC`端通用插件**（包含HR），实现功能:<br/>1. 员工任岗信息的带出<br/>2. HR表单信息的带出<br/>3. 部分字段的过滤<br/>4. PC端自定义的附件上传 |
| 2    | [CZ_CEEG_OAMBL_BaseDLL](https://github.com/joshualley/CeegCloudProject/blob/master/PlugIns/OA/Mobile/CZ.CEEG.OAMBL.BaseDLL/CZ.CEEG.OAMBL.BaseDLL/CZ_CEEG_OAMBL_BaseDLL.cs)<br/>包含存储过程：<br/>1.`proc_czty_GetLoginUser2Emp` | **`OA-Mobile`端通用插件**，实现功能：<br/>1. 员工任岗信息的带出<br/>2. 组织的过滤<br/>3. 移动端自定义附件上传<br/>4. 移动端表体可编辑<br/>5. 新增表体行 |
| 3    | [CZ_CEEG_OABos_LeaveApplyNew](https://github.com/joshualley/CeegCloudProject/blob/master/PlugIns/OA/Form/CZ.CEEG.OABos.LeaveApply/CZ.CEEG.OABos.LeaveApply/CZ_CEEG_OABos_LeaveApplyNew.cs)<br/>包含存储过程：<br/>1.`proc_czly_GetHolidayShiftSituation`<br/>2.`proc_czty_GetLoginUser2Emp`<br/>3.`proc_czty_LeaveWorkDaysAP` | **请假控制插件**，功能：<br/>1. 默认携带表体请假人、部门、岗位信息<br/>2. 计算请假天数<br/>3. 设置最大请假天数<br/>4. 提示剩余请假天数<br/>5. 提交时验证请假是否合法 |
| 4    | [CZ_CEEG_OABos_LeaveQueryNew](https://github.com/joshualley/CeegCloudProject/blob/master/PlugIns/OA/Form/CZ.CEEG.OABos.LeaveApply/CZ.CEEG.OABos.LeaveApply/CZ_CEEG_OABos_LeaveQueryNew.cs)<br/>包含存储过程：<br/>1.`proc_czly_CreateInitLeave`<br/>2.`proc_czly_LeaveQuery` | **请假查询动态表单插件**(仅用于江苏光伏)<br/>功能：<br/>1. 生成年休假查询报表<br/>2. 提供年休假结转功能 |
| 5    | [CZ_CEEG_BosOa_GetIntercourse](https://github.com/joshualley/CeegCloudProject/blob/master/PlugIns/OA/Form/CZ.CEEG.BosOa.GetIntercourse/CZ_CEEG_BosOa_GetIntercourse.cs) | **获取往来余额插件**<br/>应用表单：`个人资金申请`和`对公资金申请`，功能：<br/>1. 设置单据上的往来金额<br/>2. 设置提醒信息，贷方还是借方，剩余金额多少 |
| 6    | [CZ_CEEG_OABos_CalExchangeRate](https://github.com/joshualley/CeegCloudProject/blob/master/PlugIns/OA/Form/CZ.CEEG.OABos.CalExchangeRate/CZ.CEEG.OABos.CalExchangeRate/CZ_CEEG_OABos_CalExchangeRate.cs) | 汇率计算插件<br/>表单：`OA`的`采购合同`、`销售合同`及`对公资金`<br/>1. 计算汇率 |
| 7    | [CZ_CEEG_OAMbl_FieldVisibleCtrl](https://github.com/joshualley/CeegCloudProject/blob/master/PlugIns/OA/Mobile/CZ.CEEG.OAMbl.FieldVisibleCtrl/CZ_CEEG_OAMbl_FieldVisibleCtrl.cs) | **`OA`移动端按钮的保存、提交的显示隐藏控制**<br/>1. 保存、提交按钮的显示隐藏控制 |
| 8    | [CZ_CEEG_SysBos_UploadAttachment](https://github.com/joshualley/CeegCloudProject/blob/master/PlugIns/OA/Form/CZ.CEEG.SysBos.UploadAttachment/CZ.CEEG.SysBos.UploadAttachment/CZ_CEEG_SysBos_UploadAttachment.cs) | **`BOS`附件上传独立插件**，主要用于`CRM`单据                 |
| 9    | [CZ_CEEG_BosOA_WorkContract](https://github.com/joshualley/CeegCloudProject/blob/master/PlugIns/OA/Form/CZ.CEEG.BosOA.WorkContract/CZ_CEEG_BosOA_WorkContract.cs)<br/>包含存储过程：<br/>1. `proc_czly_GetGManager` | **工作联系单插件**，功能：<br/>1. 设置被联系组织单位总经理   |
| 10   | [CZ_CEEG_BosOA_Hired](https://github.com/joshualley/CeegCloudProject/blob/master/PlugIns/OA/Form/CZ.CEEG.BosOA.Hired/CZ_CEEG_BosOA_Hired.cs)<br/>包含存储过程：<br/>1. `proc_czly_GetGManager` | **录用申请插件**，功能：<br/>1. 设置单位总经理               |
| 11   | [CZ_CEEG_BosOA_ForPubFund](https://github.com/joshualley/CeegCloudProject/blob/master/PlugIns/OA/Form/CZ.CEEG.BosOA.ForPubFund/CZ_CEEG_BosOA_ForPubFund.cs) | **对公资金申请插件**，功能：<br/>1. 携带供应商或客户的银行信息 |
| 12   | [CEEG_CZ_OABos_HrSyncDelPermision](https://github.com/joshualley/CeegCloudProject/blob/master/PlugIns/OA/Service/CEEG.CZ.OABos.HrSyncDelPermision/CEEG.CZ.OABos.HrSyncDelPermision/CEEG_CZ_OABos_HrSyncDelPermision.cs) | **HR单据反审核控制服务插件**，功能：<br/>1. HR单据同步后禁止反审核 |
| 13   | [CZ_CEEG_HRWF_Transfer](https://github.com/joshualley/CeegCloudProject/blob/master/PlugIns/OA/Service/CZ.CEEG.HRWF.Transfer/CZ.CEEG.HRWF.Transfer/CZ_CEEG_HRWF_Transfer.cs) | **调职审核服务插件**, (江苏光伏使用)，功能：<br/>1. 调职审核后反写员工岗位信息 |
| 14   | [CZ_CEEG_SrvOA_PostChange](https://github.com/joshualley/CeegCloudProject/blob/master/PlugIns/OA/Service/HR/CZ.CEEG.SrvOA.PostChange/CZ_CEEG_SrvOA_PostChange.cs) | **调职审核服务插件**, (变压器使用)，功能：<br/>1. 调职审核后反写员工岗位信息 |
| 15   | [CZ_CEEG_SrvOA_RegularWork](https://github.com/joshualley/CeegCloudProject/blob/master/PlugIns/OA/Service/HR/CZ.CEEG.SrvOA.RegularWork/CZ_CEEG_SrvOA_RegularWork.cs) | **转正审核服务插件**, (变压器使用)，功能：<br/>1. 转正审核后反写员工信息 |
| 16   | [CZ_CEEG_SrvOA_Renewal](https://github.com/joshualley/CeegCloudProject/blob/master/PlugIns/OA/Service/HR/CZ.CEEG.SrvOA.Renewal/CZ_CEEG_SrvOA_Renewal.cs) | **续签审核服务插件**, (变压器使用)，功能：<br/>1. 续签审核后反写员工信息 |
| 17   | [CZ_CEEG_OAWF_AdminService](https://github.com/joshualley/CeegCloudProject/blob/master/PlugIns/OA/Service/Validate/CZ.CEEG.OAWF.AdminService/CZ.CEEG.OAWF.AdminService/CZ_CEEG_OAWF_AdminService.cs) | **行政服务**流程控制服务插件，功能：<br/>1. 分配执行人时，执行人必录<br/>2. 执行人填写结果时，执行结果必录 |
| 18   | [CZ_CEEG_OAWF_InfoService](https://github.com/joshualley/CeegCloudProject/blob/master/PlugIns/OA/Service/Validate/CZ.CEEG.OAWF.InfoService/CZ.CEEG.OAWF.InfoService/CZ_CEEG_OAWF_InfoService.cs) | **信息服务**流程控制服务插件，功能：<br/>1. 分配执行人时，执行人必录<br/>2. 申请人评价时，评价结果必录 |
| 19   | [CZ_CEEG_OAWF_InnerAccommodation](https://github.com/joshualley/CeegCloudProject/blob/master/PlugIns/OA/Service/Validate/CZ.CEEG.OAWF.InnerAccommodation/CZ.CEEG.OAWF.InnerAccommodation/CZ_CEEG_OAWF_InnerAccommodation.cs) | **内部住宿**流程控制服务插件，功能：<br/>1. 分配房间号时，房间号必录 |
| 20   | [CZ_CEEG_OAWF_InnerEating](https://github.com/joshualley/CeegCloudProject/blob/master/PlugIns/OA/Service/Validate/CZ.CEEG.OAWF.InnerEating/CZ.CEEG.OAWF.InnerEating/CZ_CEEG_OAWF_InnerEating.cs) | **内部就餐**流程控制服务插件，功能：<br/>1. 分配包厢时，包厢号必录<br/>2. 确认金额节点时，实际金额大于0 |
| 21   | [CZ_CEEG_OAWF_SealApply](https://github.com/joshualley/CeegCloudProject/blob/master/PlugIns/OA/Service/Validate/CZ.CEEG.OAWF.SealApply/CZ.CEEG.OAWF.SealApply/CZ_CEEG_OAWF_SealApply.cs) | **印章使用申请**流程控制服务插件，功能：<br/>1. 股份盖章节点时，实际带离及实际归还时间必录 |
| 22   | [CZ_CEEG_OAWF_TechService](https://github.com/joshualley/CeegCloudProject/blob/master/PlugIns/OA/Service/Validate/CZ.CEEG.OAWF.TechService/CZ.CEEG.OAWF.TechService/CZ_CEEG_OAWF_TechService.cs) | **技术服务**流程控制服务插件，功能：<br/>1. 分配执行人时，执行人必录<br/>2. 申请人评价时，评价结果必录 |
| 23   | [CZ_CEEG_OAWF_UseCar](https://github.com/joshualley/CeegCloudProject/blob/master/PlugIns/OA/Service/Validate/CZ.CEEG.OAWF.UseCar/CZ.CEEG.OAWF.UseCar/CZ_CEEG_OAWF_UseCar.cs) | **用车申请**流程控制服务插件，功能：<br/>1. 行政分配时，司机、车牌号、出发地点、出发时间必录<br/>2. 行政分配2节点时，实际公里数、实际费用(自动计算)及 补贴必录 |
| 24   | [CZ_CEEG_OAWF_WorkContract](https://github.com/joshualley/CeegCloudProject/blob/master/PlugIns/OA/Service/Validate/CZ.CEEG.OAWF.WorkContract/CZ.CEEG.OAWF.WorkContract/CZ_CEEG_OAWF_WorkContract.cs) | **工作联系单**流程控制服务插件，功能：<br/>1.单位总经理节点，选择联系组织及部门<br/>2. 被联系单位总经理，分配执行人<br/>3. 申请人评价时，评价结果必录 |
| 25   | [CZ_CEEG_BosSys_WorkflowChart](https://github.com/joshualley/CeegCloudProject/blob/master/PlugIns/ERP/Form/CZ.CEEG.BosSys.WorkflowChart/CZ_CEEG_BosSys_WorkflowChart.cs) | **流程图插件**，功能：<br/>1. BOS首页审批信息中，查看流程图时，可以直接打开单据 |
| 26   | [CZ_CEEG_BosPM_PersonalReport](https://github.com/joshualley/CeegCloudProject/blob/master/PlugIns/WorkTask/CZ.CEEG.BosPM.PersonalReport/CZ.CEEG.BosPM.PersonalReport/CZ_CEEG_BosPM_PersonalReport.cs)<br/>包含存储过程：<br/>1. `proc_czty_GetLoginUser2Emp`<br/>2. `GetLastMonthDailyTask`<br/>3.`GetAssignTask` | **工作计划插件**，功能：<br>1. 新增时获取上月数据(日常及交办)<br/>2. 单据日期变动时，根据单据日期重新加载上月数据<br/>3. 设置创建组织及部门，直接领导和单位总经理<br/>4. 验证表体权重是否为100<br/>5. 验证上月任务结果是否填写 |
| 27   | [CZ_CEEG_WFTask_PersonalReport](https://github.com/joshualley/CeegCloudProject/blob/master/PlugIns/WorkTask/CZ.CEEG.WFTask.PersonalReport/CZ.CEEG.WFTask.PersonalReport/CZ_CEEG_WFTask_PersonalReport.cs) | **工作计划审核服务插件**，功能：<br/>1. 直接领导评分必录<br/>2. 审核后反写上月得分 |
| 28   | [CZ_CEEG_SheduleTask_WorkPlanNotify](https://github.com/joshualley/CeegCloudProject/blob/master/PlugIns/ScheduleTask/CZ.CEEG.SheduleTask.WorkPlanNotify/CZ_CEEG_SheduleTask_WorkPlanNotify.cs) | **工作计划定时提醒任务**，功能：<br/>1. 每月3、5、7号提醒提交工作计划 |
| 29   | [CZ_CEEG_BosTask_PfmReview](https://github.com/joshualley/CeegCloudProject/blob/master/PlugIns/WorkTask/CZ.CEEG.BosTask.PfmReview/CZ_CEEG_BosTask_PfmReview.cs)<br/>包含存储过程：<br/>1.`proc_czly_GetGManager`<br>2.`proc_czly_GetPerformanceInfo` | **绩效复核插件**，功能：<br/>1. 根据组织、部门获取绩效信息<br/>2. 根据组织、部门获取单位总经理 |
| 30   | [CZ_CEEG_SheduleTask_GetClockInData](https://github.com/joshualley/CeegCloudProject/blob/master/PlugIns/ScheduleTask/CZ.CEEG.SheduleTask.GetClockInData/CZ_CEEG_SheduleTask_GetClockInData.cs) | 考勤同步定时任务插件，功能：<br/>1. 每日`1:00`将云之家打卡数据同步到数据库表`ora_HR_SignInData` |
| 31   | [CZ_CEEG_OABos_LeaveApplyList](https://github.com/joshualley/CeegCloudProject/blob/master/PlugIns/OA/Form/CZ.CEEG.OABos.LeaveApply/CZ.CEEG.OABos.LeaveApply/CZ_CEEG_OABos_LeaveApplyList.cs) | **请假列表**，集体请假                                       |
| 32   | [CZ_CEEG_OABos_AllLeaveSetting](https://github.com/joshualley/CeegCloudProject/blob/master/PlugIns/OA/Form/CZ.CEEG.OABos.LeaveApply/CZ.CEEG.OABos.LeaveApply/CZ_CEEG_OABos_AllLeaveSetting.cs)<br>包含存储过程：<br>`proc_czly_AllLeave` | **集体请假动态表单插件**                                     |
| 33   | [CZ_CEEG_ERP_ConsignmentNotify](https://github.com/joshualley/CeegCloudProject/blob/master/PlugIns/ERP/Form/CZ.CEEG.ERP.ConsignmentNotify/CZ_CEEG_ERP_ConsignmentNotify.cs) | **发货通知**，携带用户组织、部门                             |
| 34   | [CZ_CEEG_SheduleTask_EmpWkDt](https://github.com/joshualley/CeegCloudProject/blob/master/PlugIns/ScheduleTask/CZ.CEEG.SheduleTask.GetClockInData/CZ_CEEG_SheduleTask_EmpWkDt.cs) | **考勤表插件**，云之家签到手动同步按钮，可打开同步日期选择表单 |
| 35   | [CZ_CEEG_SheduleTask_SignInSync](https://github.com/joshualley/CeegCloudProject/blob/master/PlugIns/ScheduleTask/CZ.CEEG.SheduleTask.GetClockInData/CZ_CEEG_SheduleTask_SignInSync.cs) | **考勤同步动态表单**，根据日期获取云之家签到数据             |

### CRM相关插件

------

|  #   | 插件名称                                                     | 插件具体功能描述                                             |
| :--: | ------------------------------------------------------------ | ------------------------------------------------------------ |
|  1   | [CZ_CEEG_MblCrmLst_CustFliter](https://github.com/joshualley/CeegCloudProject/blob/master/PlugIns/CRM/CRMLists/CZ.CEEG.MblCrmLst.CustFliter/CZ_CEEG_MblCrmLst_CustFliter.cs) | **移动端客户管理列表插件**，功能：<br/>1. 根据销售员过滤客户 |
|  2   | [CZ_CEEG_MblCrm_SaleOrderLst](https://github.com/joshualley/CeegCloudProject/blob/master/PlugIns/CRM/CRMLists/CZ.CEEG.MblCrm.SaleOrderLst/CZ_CEEG_MblCrm_SaleOrderLst.cs) | **移动销售订单列表插件**，功能：<br/>1. 取消组织隔离，显示所有组织数据 |
|  3   | [CZ_CEEG_CRMListFilte](https://github.com/joshualley/CeegCloudProject/blob/master/PlugIns/CRM/CRMLists/CZ.CEEG.CRMListFilter/CZ.CEEG.CRMListFilter/CZ.CEEG.CRMListFilter/CZ_CEEG_CRMListFilter.cs) | 移动线索、商机、报价、合同的列表插件，功能：<br/>1. 实现权限菜单功能：<br/>全部单据、我创建的、我持有的、我管理的 |
|  4   | [CZ_CEEG_BosCrmLst_AllBill](https://github.com/joshualley/CeegCloudProject/blob/master/PlugIns/CRM/CRMLists/CZ.CEEG.BosCrmLst.AllBill/CZ.CEEG.BosCrmLst.AllBill/CZ.CEEG.BosCrmLst.AllBill/CZ.CEEG.BosCrmLst.AllBill/CZ_CEEG_BosCrmLst_AllBill.cs) | PC上商机、报价、合同的列表插件，功能：<br/>1. 菜单：全部单据、我创建的、我持有的、我管理的 |
|  5   | [CZ_CEEG_BosCrmLst_Clue](https://github.com/joshualley/CeegCloudProject/blob/master/PlugIns/CRM/CRMLists/CZ.CEEG.BosCrmLst.Clue/CZ.CEEG.BosCrmLst.Clue/CZ.CEEG.BosCrmLst.Clue/CZ.CEEG.BosCrmLst.Clue/CZ_CEEG_BosCrmLst_Clue.cs) | PC上线索的列表插件，功能：<br/>1. 菜单：全部单据、我创建的、我持有的、我管理的 |
|  6   | [CZ_CEEG_BosCrm_CustToTrade](https://github.com/joshualley/CeegCloudProject/blob/master/PlugIns/CRM/Form/CZ.CEEG.BosCrm.CustToTrade/CZ_CEEG_BosCrm_CustToTrade.cs) | 非交易客户列表插件，功能：<br/>1. 提供打开按钮，使得打开潜在客户的单据后可以进行修改，用于实现非交易客户转交易客户功能 |
|  7   | [CZ_CEEG_MblCrm_CustManager](https://github.com/joshualley/CeegCloudProject/blob/master/PlugIns/CRM/Mobile/CZ.CEEG.MblCrm.CustManager/CZ_CEEG_MblCrm_CustManager.cs) | 移动端客户管理插件，功能：<br/>1. 客户名称唯一性检验<br/>2. 带出用户绑定的销售员<br/>3. 插表生成客户绑定的联系人<br/>4. 实现客户地址的多段选择，并拼接为全地址<br/>5. 基础资料的附件上传 |
|  8   | [CZ_CEEG_BosCrm_Clue](https://github.com/joshualley/CeegCloudProject/blob/master/PlugIns/CRM/Form/CZ.CEEG.BosCrm.Clue/CZ.CEEG.BosCrm.Clue/CZ_CEEG_BosCrm_Clue.cs) | PC端线索插件，功能：<br/>1. 计算单据的CRM标识码<br/>2. 线索分配、转化、关闭 |
|  9   | [CZ_CEEG_MblCrm_Clue](https://github.com/joshualley/CeegCloudProject/blob/master/PlugIns/CRM/Mobile/CZ.CEEG.MblCrm.Clue/CZ.CEEG.MblCrm.Clue/CZ_CEEG_MblCrm_Clue.cs) | 移动端线索插件，功能：<br/>1. 计算单据的CRM标识码<br/>2. 线索分配、转化、关闭 |
|  10  | [CZ_CEEG_BosCrm_Niche](https://github.com/joshualley/CeegCloudProject/blob/master/PlugIns/CRM/Form/CZ.CEEG.BosCrm.Niche/CZ.CEEG.BosCrm.Niche/CZ_CEEG_BosCrm_Niche.cs) | PC端商机插件，功能：<br/>1. 计算单据的CRM标识码<br/>2. 生成持有记录 |
|  11  | [CZ_CEEG_MblCrm_Niche](https://github.com/joshualley/CeegCloudProject/blob/master/PlugIns/CRM/Mobile/CZ.CEEG.MblCrm.Niche/CZ.CEEG.MblCrm.Niche/CZ_CEEG_MblCrm_Niche.cs) | 移动端商机插件，功能：<br/>1. 计算单据的CRM标识码<br/>2. 生成持有记录<br/>3. 商机复制<br/>4. 商机下推<br/>5. 按钮的显示隐藏控制 |
|  12  | [CZ_CEEG_BosCrm_SaleOffer](https://github.com/joshualley/CeegCloudProject/blob/master/PlugIns/CRM/Form/CZ.CEEG.BosCrm.SaleOffer/CZ.CEEG.BosCrm.SaleOffer/CZ_CEEG_BosCrm_SaleOffer.cs) | PC端报价插件，功能：<br/>1. 根据报价员进行行隐藏<br/>2. 打开基价计算单，并回写基价及材料组成数据<br/>3. 提供拆分报价功能<br/>4. 填写报价时计算下浮比例等数据<br/>5. 删除产品大类行时，同时删除相关的明细行及材料组成行 |
|  13  | [CZ_CEEG_MblCrm_ToSaler](https://github.com/joshualley/CeegCloudProject/blob/master/PlugIns/CRM/Mobile/CZ.CEEG.MblCrm.ToSaler/CZ.CEEG.MblCrm.ToSaler/CZ_CEEG_MblCrm_ToSaler.cs) | 移动端报价插件，功能：<br/>1. 填写报价时计算下浮比例等数据<br/>2. 报价下推<br/>3. 按钮显示隐藏控制 |
|  14  | [CZ_CEEG_SrvCrm_SaleOfferSbmt](https://github.com/joshualley/CeegCloudProject/blob/master/PlugIns/CRM/Sevice/CZ.CEEG.SrvCrm.SaleOfferSbmt/CZ.CEEG.SrvCrm.SaleOfferSbmt/CZ_CEEG_SrvCrm_SaleOfferSbmt.cs) | 销售报价保存服务插件，功能：<br/>1. 保存时验证产品大类是否被授权<br/>2. 根据产品大类获取需要参与审核的报价员 |
|  15  | [CZ_CEEG_SrvCrm_SaleOffer](https://github.com/joshualley/CeegCloudProject/blob/master/PlugIns/CRM/Sevice/CZ.CEEG.SrvCrm.SaleOffer/CZ_CEEG_SrvCrm_SaleOffer.cs) | 销售报价流程控制服务插件，功能：<br/>1. 最终报价时验证报价的填写<br/>2. 标书分配时，验证标书制作员必录<br/>3. 报价员时，验证是否报价完成 |
|  16  | [CZ_CEEG_BosCrm_Contract](https://github.com/joshualley/CeegCloudProject/blob/master/PlugIns/CRM/Form/CZ.CEEG.BosCrm.Contract/CZ.CEEG.BosCrm.Contract/CZ_CEEG_BosCrm_Contract.cs) | PC端销售合同插件，功能基本同报价，额外：<br>1. 多表体下推时数据携带<br/>2. Bom员打开时，锁定其无需填写的物料 |
|  17  | [CZ_CEEG_MblCrm_SaleContact](https://github.com/joshualley/CeegCloudProject/blob/master/PlugIns/CRM/Mobile/CZ.CEEG.MblCrm.SaleContact/CZ_CEEG_MblCrm_SaleContact.cs) | 移动端销售合同插件，功能同报价                               |
|  18  | [CZ_CEEG_SrvCrm_SaleOfferSbmt](https://github.com/joshualley/CeegCloudProject/blob/master/PlugIns/CRM/Sevice/CZ.CEEG.SrvCrm.SaleOfferSbmt/CZ.CEEG.SrvCrm.SaleOfferSbmt/CZ_CEEG_SrvCrm_SaleOfferSbmt.cs) | 销售保存合同服务插件，功能：<br/>1. 保存时验证产品大类是否被授权<br/>2. 根据产品大类获取需要参与审核的报价员<br/>3. 根据产品大类寻找bom员 |
|  19  | [CZ_CEEG_SrvCrm_SaleContract](https://github.com/joshualley/CeegCloudProject/blob/master/PlugIns/CRM/Sevice/CZ.CEEG.SrvCrm.SaleContract/CZ_CEEG_SrvCrm_SaleContract.cs) | 销售报价流程控制服务插件，功能：<br/>1. 验证合同评审员信息填写<br/>2. 验证Bom员物料填写<br/>3. 验证客户管理员，转交易客户 |
|  20  | [CZ_CEEG_BosCrm_DelUpFormCorrelation](https://github.com/joshualley/CeegCloudProject/blob/master/PlugIns/CRM/Sevice/CZ.CEEG.BosCrm.DelUpFormCorrelation/CZ_CEEG_BosCrm_DelUpFormCorrelation.cs) | 删除单据关联插件，功能：<br/>1. 解决移动端单据关联关系建立后，单据反审核删除时，无法清除关联关系表的情况。 |
|  21  | [CZ_CEEG_SrvCrm_InContractSbmt](https://github.com/joshualley/CeegCloudProject/blob/master/PlugIns/CRM/Sevice/CZ.CEEG.SrvCrm.InContractSbmt/CZ.CEEG.SrvCrm.InContractSbmt/CZ_CEEG_SrvCrm_InContractSbmt.cs) | 内部合同保存服务插件，功能：<br/>1. 根据产品大类获取需要参与审核的报价员 |
|  22  | [CZ_CEEG_BosCrm_ContactVaryDraw](https://github.com/joshualley/CeegCloudProject/blob/master/PlugIns/CRM/Sevice/CZ.CEEG.BosCrm.ContactVaryDraw/CZ_CEEG_BosCrm_ContactVaryDraw.cs) | 销售合同-内部合同单据转换插件，功能：<br/>1. 携带产品大类及材料组成单据体 |
|  23  | [CZ_CEEG_SrvCrm_AfterSaleService](https://github.com/joshualley/CeegCloudProject/blob/master/PlugIns/CRM/Sevice/CZ.CEEG.SrvCrm.AfterSaleService/CZ_CEEG_SrvCrm_AfterSaleService.cs) | 售后服务流程控制服务插件，功能：<br/>1. 分配执行人时，执行人必录<br/>2. 执行人填写结果时，执行结果必录<br/>3. 申请人评价时，评价结果必录 |
|  24  | [CZ_CEEG_BosCrm_OtherContract](https://github.com/joshualley/CeegCloudProject/blob/master/PlugIns/CRM/Form/CZ.CEEG.BosCrm.OtherContract/CZ_CEEG_BosCrm_OtherContract.cs) | 内部合同插件，功能同销售合同                                 |
|  25  | [CZ_CEEG_BosCrm_BPRnd](https://github.com/joshualley/CeegCloudProject/blob/master/PlugIns/CRM/Form/CZ.CEEG.BosCrm.BPRnd/CZ.CEEG.BosCrm.BPRnd/CZ_CEEG_BosCrm_BPRnd.cs) | 基价计算单插件，功能：<br/>1. 基价的计算，并反写到销售报价或销售合同上<br/>2. 保存为基价方案<br/>3. 从基价方案中选择 |
|  26  | [CZ_CEEG_BosCrmBD_BPScheme](https://github.com/joshualley/CeegCloudProject/blob/master/PlugIns/CRM/Form/CZ.CEEG.BosCrmBD.BPScheme/CZ.CEEG.BosCrmBD.BPScheme/CZ_CEEG_BosCrmBD_BPScheme.cs) | 基价方案插件，功能：<br/>1. 提供基价计算                     |
|  27  | [CZ_CEEG_BosCrm_RepairContract](https://github.com/joshualley/CeegCloudProject/blob/master/PlugIns/CRM/Form/CZ.CEEG.BosCrm.RepairContract/CZ_CEEG_BosCrm_RepairContract.cs) | 维修合同插件，功能：<br/>1. 计算报价、含税单价、不含税单价、税额 |
|  28  | [CZ_CEEG_BosCrm_Common](https://github.com/joshualley/CeegCloudProject/blob/master/PlugIns/CRM/Form/CZ.CEEG.BosCrm.Common/CZ_CEEG_BosCrm_Common.cs) | CRM预留通用插件，功能：<br/>1. 目前仅提供携带申请人信息、直接领导，单位总经理、销售员 |
|  29  | [CZ_CEEG_MblCrm_Common](https://github.com/joshualley/CeegCloudProject/blob/master/PlugIns/CRM/Mobile/CZ.CEEG.MblCrm.Common/CZ_CEEG_MblCrm_Common.cs) | CRM移动端通用插件，功能：<br/>1. 设置组织及部门<br/>2. 附件上传 |
|  30  | [CZ_CEEG_CrmMbl_LawEntrust](https://github.com/joshualley/CeegCloudProject/blob/master/PlugIns/CRM/Mobile/CZ.CEEG.CrmMbl.LawEntrust/CZ_CEEG_CrmMbl_LawEntrust.cs) | 开具法委移动插件，功能<br/>1. 接收下推数据，建立关联关系<br/>2. 收件人相关字段显示隐藏 |
|  31  | [CZ_CEEG_MblCrm_AfterSaleService](https://github.com/joshualley/CeegCloudProject/blob/master/PlugIns/CRM/Mobile/CZ.CEEG.MblCrm.AfterSaleService/CZ_CEEG_MblCrm_AfterSaleService.cs) | 售后服务移动端插件，功能：<br/>1. 下推退换货，接收销售订单返回数据<br/>2. 按钮及标签页显示隐藏 |
|  32  | [CZ_CEEG_MblCrm_ChangeRefund](https://github.com/joshualley/CeegCloudProject/blob/master/PlugIns/CRM/Mobile/CZ.CEEG.MblCrm.ChangeRefund/CZ_CEEG_MblCrm_ChangeRefund.cs) | 退换货维修移动端插件，功能：<br/>1. 接收下推数据，建立关联关系 |
|  33  | [CZ_CEEG_MblCrm_MaintainOffer](https://github.com/joshualley/CeegCloudProject/blob/master/PlugIns/CRM/Mobile/CZ.CEEG.MblCrm.MaintainOffer/CZ_CEEG_MblCrm_MaintainOffer.cs) | 维修报价移动端插件，功能：<br/>1. 接收下推数据，建立关联关系<br>2. 下推维修合同<br/>3. 保存、提交、下推按钮显示隐藏 |
|  34  | [CZ_CEEG_MblCrm_MaintainContract](https://github.com/joshualley/CeegCloudProject/blob/master/PlugIns/CRM/Mobile/CZ.CEEG.MblCrm.MaintainOffer/CZ_CEEG_MblCrm_MaintainContract.cs) | 维修合同移动端插件，功能：<br/>1. 接收下推数据，建立关联关系<br/>2. 上查<br/>3. 保存、提交、上查按钮显示隐藏 |
|  35  | [CZ_CEEG_MblCrm_SaleInvoice](https://github.com/joshualley/CeegCloudProject/blob/master/PlugIns/CRM/Mobile/CZ.CEEG.MblCrm.SaleInvoice/CZ_CEEG_MblCrm_SaleInvoice.cs) | 销售开票移动端插件，功能：<br>1. 接收下推数据，建立关联关系<br/>2. 显示隐藏收件人相关字段，并动态校验<br/>3. 带出订单、发货、收款、开票金额 |
|  36  | [CZ_CEEG_MblCrm_SpclOrderSettle](https://github.com/joshualley/CeegCloudProject/blob/master/PlugIns/CRM/Mobile/CZ.CEEG.MblCrm.SpclOrderSettle/CZ_CEEG_MblCrm_SpclOrderSettle.cs) | 特殊订单结算移动端插件，功能：<br/>1. 接收下推数据，建立关联关系 |
|  37  | [CZ_CEEG_BosCW_ReceiptSplit](https://github.com/joshualley/CeegCloudProject/blob/master/PlugIns/Expense/CZ.CEEG.BosCW.ReceiptSplit/CZ.CEEG.BosCW.ReceiptSplit/CZ_CEEG_BosCW_ReceiptSplit.cs) | **到款拆分单插件**                                           |
|  38  | [CZ_CEEG_BosCW_GetSOP4RS](https://github.com/joshualley/CeegCloudProject/blob/master/PlugIns/Expense/CZ.CEEG.BosCW.GetSOP4RS/CZ.CEEG.BosCW.GetSOP4RS/CZ_CEEG_BosCW_GetSOP4RS.cs) | **到款拆分选择收款计划单插件**                               |

### 报表类插件

------

| #    | 插件名称                                                     | 具体功能描述                                                 |
| ---- | ------------------------------------------------------------ | ------------------------------------------------------------ |
| 1    | [CZ_CEEG_Report_AccountQueryCond](https://github.com/joshualley/CeegCloudProject/blob/master/PlugIns/PMT/CZ.CEEG.Report.CostAccount/CZ_CEEG_Report_AccountQueryCond.cs) | 费用台账报表查询条件之动态表单插件：<br>1. 提供查询条件，点击查询时弹出报表 |
| 2    | [CZ_CEEG_Report_CostAccount](https://github.com/joshualley/CeegCloudProject/blob/master/PlugIns/PMT/CZ.CEEG.Report.CostAccount/CZ_CEEG_Report_CostAccount.cs)<br/>存储过程：<br/>1. `proc_czly_AccountDept` | 费用台账报表之动态表单插件：<br/>1. 动态添加表体列，构建二维查询报表<br/>2. 动态添加表体合计列<br/>3. 点击单据体行，显示对应部门的凭证行明细 |
| 3    | [CZ_CEEG_Report_CostAccountBuilder](https://github.com/joshualley/CeegCloudProject/blob/master/PlugIns/PMT/CZ.CEEG.Report.CostAccount/CZ_CEEG_Report_CostAccountBuilder.cs)<br/>存储过程：<br/>1.`proc_czly_AccountDept` | 费用台账报表之表单构建插件：<br/>1. 显示表体行汇总列         |
| 4    | [CZ_CEEG_Report_VounterDetail](https://github.com/joshualley/CeegCloudProject/blob/master/PlugIns/PMT/CZ.CEEG.Report.CostAccount/CZ_CEEG_Report_VounterDetail.cs)<br>存储过程：<br>1.`proc_czly_AccountVocunter` | 凭证明细报表之动态表单插件：<br>1. 根据查询条件显示凭证行明细 |
| 5    | [CZ_CEEG_BosPmt_PmtSummary](https://github.com/joshualley/CeegCloudProject/blob/master/PlugIns/PMT/CZ.CEEG.BosPmt.PmtSummary/CZ_CEEG_BosPmt_PmtSummary.cs)<br>存储过程：<br>1. `proc_czly_GetPmtSummary`<br>2. `proc_czly_GetPmtDetail` | 货款汇总报表之动态表单插件：<br>1. 查询货款的汇总及明细情况  |
| 6    | [CZ_CEEG_BosPmt_PmtDepartment](https://github.com/joshualley/CeegCloudProject/blob/master/PlugIns/PMT/CZ.CEEG.BosPmt.PmtSummary/CZ_CEEG_BosPmt_PmtDepartment.cs)<br>1. `proc_czly_GetPmt` | 各类别货款插件：<br>1. 根据办事处、客户、销售员、子公司、子公司客户进行货款的统计 |
| 7    | [CZ_CEEG_BosPmt_SalemanItem](https://github.com/joshualley/CeegCloudProject/blob/master/PlugIns/PMT/CZ.CEEG.BosPmt.PmtSummary/CZ_CEEG_BosPmt_SalemanItem.cs)<br>1. `proc_czly_GetPmt` | 销售员货款明细：<br>1. 点击销售员货款行时，弹出销售员相关的销售订单及货款信息 |
| 8    | [CZ_CEEG_BosPmt_FullPmtDelv](https://github.com/joshualley/CeegCloudProject/blob/master/PlugIns/PMT/CZ.CEEG.BosPmt.PmtSummary/CZ_CEEG_BosPmt_FullPmtDelv.cs)<br>1. `proc_czly_PmtFullDelv` | 全款提货报表：<br>1. 查询合同明细及发货明细                  |
| 9    | [CZ_CEEG_BosPmt_OuterPmt](https://github.com/joshualley/CeegCloudProject/blob/master/PlugIns/PMT/CZ.CEEG.BosPmt.PmtSummary/CZ_CEEG_BosPmt_OuterPmt.cs)<br>1. `proc_czly_GetPmtSummary` | 在外货款报表：<br>1. 查询在外货款                            |
| 10   | [CZ_CEEG_BosPmt_PmtAging](https://github.com/joshualley/CeegCloudProject/blob/master/PlugIns/PMT/CZ.CEEG.BosPmt.PmtSummary/CZ_CEEG_BosPmt_PmtAging.cs) | 账龄报表：<br>1. 查询账龄                                    |

### Cloud-HR同步Win服务

------

项目传送门 [`srvoahr`]() ，目录树及功能如下：

```shell
.
│  enconf.json           # 配置文件
│  encrypt_config.go     # 配置文件加密程序
│  go.mod        # go-mod 包管理文件
│  go.sum        # go-mod 包管理文件
│  winsrv_syn2hr.go      # 同步服务主程序
│
├─log                    # 日志文件夹
│  └─202004
│          Log_20200422.log
│          SuccessfulLog_20200422.log
│
├─SynOA2Hr              # build生成的文件
│      enconf.json
│      encrypt_config.exe
│      winsrv_syn2hr.exe
│
├─tasks
│      datasyn.go       # 同步任务，实现了同步的业务逻辑
│
└─utils
        encrypt.go      # 加密工具
        loghelper.go    # 日志工具
        sqlxhelper.go   # sql工具
```

将`conf.json`与`encrypt_config.exe`放置在同一目录下，运行`encrypt_config.exe`后会生成加密后的配置文件`enconf.json`

### 函数

1. 补零函数

   ```mssql
    create function [dbo].[fun_BosRnd_addSpace](
        @itemID varchar(10),	--待处理值
        @strB varchar(50),		--前缀
        @strE varchar(50),		--后缀
        @maxLen int				--补足长度
    )
   ```

2. 计算货款收款到期日

   ```mssql
   CREATE FUNCTION [dbo].[Fun_CalDeadline](
       @paywayId BIGINT,
       @planSeq INT,
       @dvtDt DATETIME
   )
   ```

3. 按比例计算收款金额

   ```mssql
   CREATE FUNCTION [dbo].[Fun_CalRateAmt](
       @paywayId BIGINT,
       @planSeq INT,
       @orderAmt DECIMAL(18,2)
   )
   ```

4. 将员工任岗的工作部门转换为对应使用组织下的工作组织

   ```mssql
   CREATE function [dbo].[fun_czty_GetWorkDeptID](@FDeptID int)
   ```

5. 获取分割字符串中的第几个值

   ```mssql
   CREATE FUNCTION [dbo].[Fun_GetValueAt](
   	@str VARCHAR(MAX),
   	@tag VARCHAR,       --分隔符
   	@index int
   )
   ```

6. 判断是否为全款提货

   ```mssql
   CREATE FUNCTION [dbo].[Fun_IsFullPay](
       @recCondId BIGINT
   )
   ```

7. 判断收款计划的行是否为质保金

   ```mssql
   CREATE FUNCTION [dbo].[Fun_IsWarranty](
       @paywayId BIGINT,
       @planSeq INT
   )
   ```

8. 字符串分割

   ```mssql
   CREATE FUNCTION [dbo].[Fun_Split](
   	@str VARCHAR(MAX),
   	@tag VARCHAR       --分隔符
   )
   ```

### 存储过程

1. 根据凭证及费用台账，动态生成以部门为行，费用项目为列的二维报表

   ```mssql
   CREATE PROC [dbo].[proc_czly_AccountDept](
       @SDt DATETIME,       --开始日期
       @EDt DATETIME,       --截止日期
       @FOrgId BIGINT=0,    --冗余字段，提供根据子公司查询
       @FDeptId BIGINT=0,   --部门
       @FAccountId BIGINT=0 --科目，为0时包含销售费用和管理费用
   )
   ```

2. 查询凭证行详情

   ```mssql
   CREATE PROC [dbo].[proc_czly_AccountVocunter](
       @SDt DATETIME,              --开始日期
       @EDt DATETIME,              --截止日期
       @FOrgId BIGINT=0,           --冗余字段，提供根据子公司查询
       @FAccountId BIGINT=0,       --科目
       @FDeptName VARCHAR(100)=''  --部门名称
   )
   ```

3. `CRM`移动端建立关联关系

   ```mssql
   CREATE PROC [dbo].[proc_czly_CreateBillRelation](
       @lktable varchar(30),				--下游单据关联表
       @targetfid int,						--下游单据头内码
       @targettable varchar(30),			--下游单据头表名
       @targetformid varchar(36),			--下游单据标识
       @sourcefid int,						--上游单据头内码
       @sourcetable varchar(30),			--上游单据头表名
       @sourceformid varchar(36),			--上游单据标识
       @sourcefentryid int = 0, 			--上游单据体内码
       @sourcefentrytable varchar(30) = '' --上游单据体表名
   )
   ```

4. 移动端客户管理创建客户联系人

   ```mssql
   CREATE PROC [dbo].[proc_czly_CreateCustContactor](
   	@FName VARCHAR(50),   --联系人姓名
   	@FMobile VARCHAR(50), --联系电话
   	@FUserID INT,         --用户ID
   	@FCustID INT          --客户ID
   )
   ```

5. 创建年假结转单（仅江苏光伏）

   ```mssql
   CREATE PROC [dbo].[proc_czly_CreateInitLeave]
   ```

6. 移动端商机下推生成报价

   ```mssql
   CREATE PROC [dbo].[proc_czly_GeneSaleOffer](
   	@NFID INT			--商机主键
   )
   ```

7. 移动端销售报价下推时生成销售合同

   ```mssql
   CREATE PROC [dbo].[proc_czly_CRMGeneContact](
   	@FID INT,           --报价单主键
   	@FUserId INT        --创建用户
   )
   ```

8. 计算账龄

   ```mssql
   CREATE PROC [dbo].[proc_czly_GetAging](
       @Type VARCHAR(100)	--类型
   )
   --@Type  有效参数：
   'Dept'		--办事处
   'Factory'	--子公司
   ```

9. `CRM`合同评审中根据产品大类获取审核人`BOM`员

   ```mssql
   CREATE PROC [dbo].[proc_czly_GetCrmBomerByMtlGroup](
       @CFID INT			--合同评审主键
   )
   ```

10. 获取单位总经理（行政类表单有使用）

    ```mssql
    CREATE PROC [dbo].[proc_czly_GetGManager](
    	@FOrgId INT,		--组织
    	@FDeptId INT		--部门
    )
    ```

11. 根据加班类型计算可视为调休的天数（江苏光伏与变压器的逻辑不同）

    ```mssql
    CREATE proc [dbo].[proc_czly_GetHolidayShiftSituation](
    	@EmpID int			--员工ID
    )
    ```

12. 获取工作计划需要被通知的用户(用于每月发送提醒消息的定时任务插件)

    ```mssql
    CREATE PROC [dbo].[proc_czly_GetNeedNotifiedUsers]
    ```

13. 获取销售员信息

    ```mssql
    CREATE PROC [dbo].[proc_czly_GetOrgDeptBySalemanId](
    	@SmId INT,		--销售员内码
    	@OrgId INT=-1	--业务组织
    )
    ```

14. 通过主任岗的部门的使用组织查询员工绩效

    ```mssql
    CREATE PROC [dbo].[proc_czly_GetPerformanceInfo](
    	@FOrgId int,		--组织
    	@FDeptId int,		--部门
    	@Date datetime		--查询年月
    )
    ```

16. 货款汇总

    ```mssql
    CREATE PROC [dbo].[proc_czly_GetPmtDetail2](
        @SDt DATETIME='',				--开始日期
        @EDt DATETIME='',				--截止日期
        @FQDeptId BIGINT=0,				--办事处，用于查询货款
        @FQSalerId BIGINT=0,			--销售员，用于查询货款
        @FQCustId BIGINT=0,				--客户，，用于查询货款
        @FQFactoryId BIGINT=0,			--子公司，用于查询货款
        @FQOrderNo VARCHAR(55)=''		--销售订单编号，用于查询货款
    )
    ```

17. 各类别货款，依赖于货款汇总存储过程

    ```mssql
    CREATE PROC [dbo].[proc_czly_GetPmt](
        @FormId VARCHAR(55),			--单据唯一标识
        @FSellerID BIGINT=0,			--销售员ID，查询销售员货款明细时使用
        @sDt DATETIME='',				--开始日期
        @eDt DATETIME='',				--截止日期
        @FQDeptId BIGINT=0,				--办事处，用于查询货款
        @FQSalerId BIGINT=0,			--销售员，用于查询货款
        @FQCustId BIGINT=0,				--客户，，用于查询货款
        @FQFactoryId BIGINT=0,			--子公司，用于查询货款
        @FQOrderNo VARCHAR(55)=''		--销售订单编号，用于查询货款
    )
    --@FormId 有效参数：
    'ora_PMT_OfficePmt' 		--办事处货款
    'ora_PMT_CustomerPmt'		--客户货款
    'ora_PMT_SalesmanPmt'		--销售员货款
    'ora_PMT_SalesmanItemPmt'	--销售员货款明细
    'ora_PMT_FactoryPmt'		--子公司货款
    'ora_PMT_FactoryCustPmt'	--子公司客户货款
    ```

18. 汇总全款提货的销售订单，依赖货款汇总存储过程

    ```mssql
    CREATE PROC [dbo].[proc_czly_PmtFullDelv](
        @Type VARCHAR(55),			--类别
        @sDt DATETIME,				--开始日期
        @eDt DATETIME,				--截止日期
        @FQDeptId BIGINT=0,			--办事处，用于查询货款
        @FQSalerId BIGINT=0,		--销售员，用于查询货款
        @FQCustId BIGINT=0,			--客户，，用于查询货款
        @FQOrderNo VARCHAR(55)=''	--销售订单编号，用于查询货款
    )
    --@Type 有效参数
    'Deliver'		--发货明细
    'Contract'		--合同明细
    ```

19. 维修合同货款报表

    ```mssql
    CREATE PROC [dbo].[proc_czly_GetPmtWxDetail](
        @SDt DATETIME,			--开始日期
        @EDt DATETIME			--截止日期
    )
    ```

19. 预付款查询

    ```mssql
    CREATE PROC [dbo].[proc_czly_PrePayAmtQuery](
        @QSDt DATETIME='',
        @QEDt DATETIME='',
        @QOrderNo VARCHAR(100)='',
        @QSalerNo VARCHAR(100)='',
        @QCustNo VARCHAR(100)='',
        @QSaleOrgNo VARCHAR(100)='',
        @QDeptNo VARCHAR(100)='',
        @QRecConditionNo VARCHAR(100)=''
    )
    ```

    

20. 获取销售订单及其源单的相关信息，为下游单据提供信息(如：销售开票)

    ```mssql
    CREATE PROC [dbo].[proc_czly_GetSaleOrderSrcInfo](
        @FID INT	--销售订单内码
    )
    ```

21. 通过用户获取销售员信息

    ```mssql
    CREATE PROC [dbo].[proc_czly_GetSalesmanIdByUserId](
    	@FUserId int,				--用户ID
    	@FOrgId int=-1				--业务组织，不传入组织时，带出该用户所有组织下的销售员
    )
    ```

22. 获取Cloud需要同步的HR单据数据(同步服务使用)

    ```mssql
    CREATE proc [dbo].[proc_czly_GetSynData](
    	@FType varchar(44)			--单据类型
    )
    --@FType   有效参数
    '录用'
    '转正'
    '调职'
    '离职'
    ```

23. 请假查询(仅江苏光伏)

    ```mssql
    CREATE PROC [dbo].[proc_czly_LeaveQuery](
    	@FNameId int,				--用户ID
    	@FLeaveType int,			--请假类别
    	@FYear int,					--年份
    	@FUseOrg int=100221			--使用组织
    )
    ```

24. 销售费用二维表，对于凭证每行的费用类别汇总

    ```mssql
    CREATE PROC [dbo].[proc_czly_SaleCostReport](
        @SDt DATETIME,			--开始日期
        @EDt DATETIME			--截止日期
    )
    ```

25. 设置由HR系统获取的员工数据

    ```mssql
    CREATE PROC [dbo].[proc_czly_SynEmpInfo](
    	@FHrPID varchar(44),		--HR系统员工内码
    	@FHrDeptID varchar(44),		--HR系统部门内码
    	@FHrPostID varchar(44),		--HR系统职位内码
    	@FWorkDate datetime, 		--参加工作日期
    	@FJoinDate datetime, 		--入职日期
    	@FGender int,				--性别
    	@FBirthday datetime			--生日
    )
    ```

26. 由用户ID或者员工ID获取绑定的员工信息

    ```mssql
    CREATE PROC [dbo].[proc_czty_GetLoginUser2Emp](
    	@FUserID  int=-1,					--用户ID
        @FEmpID int=-1, 					--员工ID
        @FIsFirstPost varchar(10)='1',		--是否主任岗
        @FOrgID int=-1						--无用字段，不清楚插件中是否存在调用，故未删除
    )
    ```

27. 获取组织的币别

    ```mssql
    CREATE PROC [dbo].[proc_czty_GetOrgCurrency](
    	@FOrgID int		--组织
    )
    ```

28. 计算请假时长，根据工厂日历跳过双休日、节假日

    ```mssql
    CREATE proc [dbo].[proc_czty_LeaveWorkDaysAP](
    	@FOrgID  int,  		--日历所属组织，由于固定了组织，此字段已经无效
        @FBD  datetime,  	--开始日期
        @FBD_AP  int,   	--开始时段  1代表AM 2代表PM  
        @FED  datetime,  	--结束日期
        @FED_AP  int    	--结束时段  1代表AM 2代表PM  
    )
    ```

29. 获取汇率

    ```mssql
    CREATE proc [dbo].[proc_cztyBD_GetRate]
    @FRateTypeID	int=1,					--取汇率类型
    @FGetDate		datetime,				--即取日期
    @FCyForID		int,					--原币ID
    @FFCyToID		int						--目标币ID
    ```

30. `CRM`单据标识码获取，同时获取用户的组织、部门、岗位信息

    ```mssql
    CREATE proc [dbo].[proc_cztyCrm_GetCrmSN]
    @FUserID		int, 
    @FIsFirstPost	varchar(10)='1'
    ```

31. 集体请假

    ```mssql
    -- 集体请假
    CREATE PROC [dbo].[proc_czly_AllLeave](
    	@FCreatorID BIGINT,   -- 创建用户
    	@FleaveType INT,      -- 请假类型
    	@FBeginDt DATETIME,
    	@FEndDt DATETIME,
    	@FBeginFrame INT,
    	@FEndFrame INT,
    	@FBeginTime DATETIME,
    	@FEndTime DATETIME,
    	@FRemarks VARCHAR(255),
    	@FDays DECIMAL(18, 2)
    )
    ```

32. 业务费报表

    ```mssql
    --业务费结算报表
    CREATE PROC [dbo].[proc_czly_OptExpSettleRpt](
        @FSDate DATETIME,
        @FEDate DATETIME,
        @FOrderNo VARCHAR(55)='',
        @FSellerNo VARCHAR(55)='',
        @FCustNo VARCHAR(55)=''
    )
    
    -- 业务费余额查询
    CREATE PROC [dbo].[proc_czly_QueryExpBalance](
        @FYear INT,
        @FMonth INT,
        @FSellerNo VARCHAR(100)=''
    )
    ```

33. 销售报表

    ```mssql
    --子公司订单销售汇总
    CREATE PROC [dbo].[proc_czly_CompanyOrderSale](
        @QDate DATETIME=''
    )
    
    --日订单销售汇总
    CREATE PROC [dbo].[proc_czly_DailyOrderSale](
        @QDate DATETIME='',
        @QProdType VARCHAR(100)='',
        @QVoltageLevel VARCHAR(100)=''
    )
    
    --办事处业绩
    CREATE PROC [dbo].[proc_czly_DeptPerform] (
           @QDeptNo VARCHAR(100)='',
           @QDate DATETIME=''
    )
    
    --在手合同分析
    CREATE PROC [dbo].[proc_czly_HoldContractAnaly](
        @QDate DATETIME='',
        @QProdType VARCHAR(100)='',
        @QVoltageLevel VARCHAR(100)=''
    )
    
    --在手合同明细
    CREATE PROC [dbo].[proc_czly_HoldContractDetail](
        @QSDt DATETIME='',
        @QEDt DATETIME='',
        @QOrderNo VARCHAR(100)='',
        @QOrderType VARCHAR(100)='',
        @QSalerNo VARCHAR(100)='',
        @QCustNo VARCHAR(100)='',
        @QProdType VARCHAR(100)='',
        @QVoltageLevel VARCHAR(100)='',
        @QSaleOrgNo VARCHAR(100)='',
        @QDeptNo VARCHAR(100)='',
        @QStockOrgNo VARCHAR(100)='',
        @QMaterialNo VARCHAR(100)='',
        @QCkOrigin VARCHAR(100)=''
    )
    
    -- 订单明细
    CREATE PROC [dbo].[proc_czly_OrderDetail](
        @QSDt DATETIME='',
        @QEDt DATETIME='',
        @QOrderNo VARCHAR(100)='',
        @QOrderType VARCHAR(100)='',
        @QSalerNo VARCHAR(100)='',
        @QCustNo VARCHAR(100)='',
        @QProdType VARCHAR(100)='',
        @QVoltageLevel VARCHAR(100)='',
        @QSaleOrgNo VARCHAR(100)='',
        @QDeptNo VARCHAR(100)='',
        @QStockOrgNo VARCHAR(100)='',
        @QMaterialNo VARCHAR(100)='',
        @QCkOrigin VARCHAR(100)='',
        @QIsReject VARCHAR(100)='',
        @QRejectReson VARCHAR(100)='',
        @QPriceRange VARCHAR(100)=''
    )
    
    -- 营销单位全款提货
    CREATE PROC [dbo].[proc_czly_OrgFullPay](
        @QOrgNo VARCHAR(100)='',
        @QDate DATETIME=''
    )
    
    -- 营销单位全款分价格分析
    CREATE PROC [dbo].[proc_czly_OrgFullPayPrice](
        @QOrgNo VARCHAR(100)='',
        @QDate DATETIME=''
    )
    
    -- 营销单位订单分价格段分析
    CREATE PROC [dbo].[proc_czly_OrgOrderPrice](
        @QOrgNo VARCHAR(100)='',
        @QDate DATETIME=''
    )
    
    -- 营销单位订单销售汇总
    CREATE PROC [dbo].[proc_czly_OrgOrderSaleSumm](
        @QDate DATETIME='',
        @QOrgNo VARCHAR(55)=''
    )
    
    -- 营销单位单笔订单分布
    CREATE PROC [dbo].[proc_czly_OrgSingleOrder](
        @QOrgNo VARCHAR(100)='',
        @QDate DATETIME=''
    )
    
    -- 各产品订单价格分析
    CREATE PROC [dbo].[proc_czly_ProdOrderPrice](
        @QDate DATETIME=''
        ,@QOrgNo VARCHAR(100)=''
        ,@QProdType VARCHAR(100)=''
        ,@QVoltageLevel VARCHAR(100)=''
    )
    
    -- 销售明细
    CREATE PROC [dbo].[proc_czly_SaleDetail](
        @QSDt DATETIME='',
        @QEDt DATETIME='',
        @QOrderNo VARCHAR(100)='',
        @QOrderType VARCHAR(100)='',
        @QStockOutNo VARCHAR(100)='',
        @QSalerNo VARCHAR(100)='',
        @QCustNo VARCHAR(100)='',
        @QProdType VARCHAR(100)='',
        @QVoltageLevel VARCHAR(100)='',
        @QSaleOrgNo VARCHAR(100)='',
        @QDeptNo VARCHAR(100)='',
        @QStockOrgNo VARCHAR(100)='',
        @QMaterialNo VARCHAR(100)='',
        @QCkOrigin VARCHAR(100)='',
        @QDeliverType VARCHAR(100)=''
    )
    
    -- 销售员业绩统计
    CREATE PROC [dbo].[proc_czly_SellerPerform](
        @QDate DATETIME='',
        @QSaleNo VARCHAR(55)=''
    )
    
    -- 销售员各产品统计
    CREATE PROC [dbo].[proc_czly_SellerProd](
        @QDate DATETIME='', 
        @QSaleNo VARCHAR(55)='', 
        @QProdType VARCHAR(55)=''
    )
    
    -- 销售员签订产品价格分析
    CREATE PROC [dbo].[proc_czly_SellerProdSign](
        @QDate DATETIME='', 
        @QSaleNo VARCHAR(100)=''
    )
    ```

34. 其他存储过程

    ```mssql
    -- 会计核算体系--会计政策（主币别）
    create proc [dbo].[proc_czty_GetOrgCurrency]
    @FOrgID int
    
    -- 考勤
    CREATE proc [dbo].[proc_czty_hrInsEmpWkDtEntry] 
    @FID bigint=100029
    
    -- 考勤
    CREATE proc [dbo].[proc_czty_hrInsEmpWkDtMon] 
    @FID bigint=100029
    
    -- 未知
    CREATE proc [dbo].[proc_czty_PrdInStock2Sbmt]
    @FID	bigint
    
    -- PRD_MO 自动计算序列号(生产订单)
    CREATE proc [dbo].[proc_czty_PRDMO_RndSERIAL] 
    @FID	int
    
    -- 收款单 审核（弃用）
    CREATE proc [dbo].[proc_czty_ReceiveBillAudit]
    @FID int 
    
    -- 收款单 反审核（弃用）
    create proc [dbo].[proc_czty_ReceiveBillUnAudit]
    @FID int 
    
    -- 内部合同
    create proc [dbo].[proc_czty_RndInContractName] 
    @FID int 
    
    -- 获取汇率
    create proc [dbo].[proc_cztyBD_GetRate]
    @FRateTypeID	int=1,					--取汇率类型
    @FGetDate		datetime,				--即取日期
    @FCyForID		int,	--原币			--原币ID
    @FFCyToID		int		--目标币		--目标币ID
    
    --获取报价员授权
    CREATE proc [dbo].[proc_cztyCrm_Contract]
    @FCrmSOID	int	--ora_CRM_Contract[FID]
    
    -- CRM合同评审 审批流程中 保存后处理
    CREATE proc [dbo].[proc_cztyCrm_Contract_AfterSave]
    @FID	int
    
    -- 可能是生成基价计算单的单号
    create proc [dbo].[proc_cztyCrm_GetBPRndNo]
    @FTag varchar(50)
    
    -- CRM报价 取用户授权CRM物料分类
    CREATE proc [dbo].[proc_cztyCrm_OfferGetMtlGroup]
    @FUserID	int	--登录用户ID
    
    -- Crm合同评审 跟据表体Crm产品分类 选择报价员
    CREATE proc [dbo].[proc_cztyCrm_RndContract]
    @FCrmSOID	int
    
    -- Crm报价单 跟据表体Crm产品分类 选择报价员
    CREATE proc [dbo].[proc_cztyCrm_RndOffer]
    @FCrmSOID	int	--ora_CRM_SaleOffer[FID]
    
    -- 销售报价保存后
    CREATE proc [dbo].[proc_cztyCrm_SaleOffer_AfterSave]
    @FID	int
    
    -- Crm报价单 验证表体物料分类 已有报价员授权
    CREATE proc [dbo].[proc_cztyCrm_VadOffer]
    @FCrmSOID	int	--ora_CRM_SaleOffer[FID]
    
    
    
    ```

    






​    

​    






























