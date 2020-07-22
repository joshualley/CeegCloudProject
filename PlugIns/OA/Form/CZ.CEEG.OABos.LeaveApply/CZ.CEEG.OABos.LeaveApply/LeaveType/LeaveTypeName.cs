using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace CZ.CEEG.OABos.LeaveApply.LeaveType
{
    public enum LeaveTypeName
    {
        /// <summary>
        /// 产检假
        /// </summary>
        [Description("产检假")]
        AntenatalCareLeave = 1,
        /// <summary>
        /// 事假
        /// </summary>
        [Description("事假")]
        PersonalAffairsLeave = 2,
        /// <summary>
        /// 探亲假
        /// </summary>
        [Description("探亲假")]
        HomeLeave = 3,
        /// <summary>
        /// 病假
        /// </summary>
        [Description("病假")]
        SickLeave = 4,
        /// <summary>
        /// 陪护假
        /// </summary>
        [Description("陪护假")]
        NursingLeave = 5,
        /// <summary>
        /// 年休假
        /// </summary>
        [Description("年休假")]
        AnnualLeave = 6,
        /// <summary>
        /// 丧假
        /// </summary>
        [Description("丧假")]
        FuneralLeave = 7,
        /// <summary>
        /// 婚假
        /// </summary>
        [Description("婚假")]
        MarriageLeave = 8,
        /// <summary>
        /// 调休假
        /// </summary>
        [Description("调休假")]
        LieuLeave = 9,
        /// <summary>
        /// 工伤
        /// </summary>
        [Description("工伤")]
        OccupationalInjury = 10,
        /// <summary>
        /// 哺乳假
        /// </summary>
        [Description("哺乳假")]
        BreastfeedingLeave = 11,
        /// <summary>
        /// 顺产假
        /// </summary>
        [Description("顺产假")]
        EutocousLeave = 12,
        /// <summary>
        /// 剖腹产假
        /// </summary>
        [Description("剖腹产假")]
        CesareanLeave = 13,
        /// <summary>
        /// 流产假(90)
        /// </summary>
        [Description("流产假(90天)")]
        AbortionLeave_90 = 14,
        /// <summary>
        /// 流产假(210)
        /// </summary>
        [Description("流产假(210天)")]
        AbortionLeave_210 = 15,
        /// <summary>
        /// 流产假(210以上)
        /// </summary>
        [Description("流产假(210天以上)")]
        AbortionLeave_210_UP = 16,
        /// <summary>
        /// 参军
        /// </summary>
        [Description("参军")]
        JoinArmy = 17,
        /// <summary>
        /// 拆迁
        /// </summary>
        [Description("拆迁")]
        Relocation = 18,
        /// <summary>
        /// 献血
        /// </summary>
        [Description("献血")]
        DonateBlood = 19,
        /// <summary>
        /// 销售员探亲假
        /// </summary>
        [Description("销售员探亲假")]
        SalemanHomeLeave = 20
    }
}
