using CZ.CEEG.OABos.LeaveApply.LeaveType.Imp;
using Kingdee.BOS;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace CZ.CEEG.OABos.LeaveApply.LeaveType
{
    public class LeaveFactory
    {
        private Dictionary<string, ILeaveType> leaveTypes = new Dictionary<string, ILeaveType>();

        /// <summary>
        /// 哺乳假请假次数
        /// </summary>
        private int count = 0;

        /// <summary>
        /// 创建一个请假对象
        /// </summary>
        /// <param name="leaveTypeValue">请假类型的枚举值</param>
        /// <param name="leaver">请假员工FID</param>
        /// <param name="days">请假天数</param>
        /// <returns>返回请假对象接口</returns>
        public ILeaveType MakeLeave(Context context, int leaveTypeValue, long leaver, double days)
        {
            LeaveTypeName leaveType = (LeaveTypeName)leaveTypeValue;

            switch (leaveType)
            {
                case LeaveTypeName.AntenatalCareLeave:
                    return new AntenatalCare(context, leaver, days);
                case LeaveTypeName.PersonalAffairsLeave:
                    return new PersonalAffairs(context, leaver, days);
                case LeaveTypeName.HomeLeave:
                    return new Home(context, leaver, days);
                case LeaveTypeName.SickLeave:
                    return new Sick(context, leaver, days);
                case LeaveTypeName.NursingLeave:
                    return new Nursing(context, leaver, days);
                case LeaveTypeName.AnnualLeave:
                    return new Annual(context, leaver, days);
                case LeaveTypeName.FuneralLeave:
                    return new Funeral(context, leaver, days);
                case LeaveTypeName.MarriageLeave:
                    return new Marriage(context, leaver, days);
                case LeaveTypeName.LieuLeave:
                    return new Lieu(context, leaver, days);
                case LeaveTypeName.OccupationalInjury:
                    return new OccupationalInjury(context, leaver, days);
                case LeaveTypeName.BreastfeedingLeave:
                    return new Breastfeeding(context, leaver, days);
                case LeaveTypeName.EutocousLeave:
                    return new Maternity(context, LeaveTypeName.EutocousLeave, leaver, days);
                case LeaveTypeName.CesareanLeave:
                    return new Maternity(context, LeaveTypeName.CesareanLeave, leaver, days);
                case LeaveTypeName.AbortionLeave_90:
                    return new Maternity(context, LeaveTypeName.AbortionLeave_90, leaver, days);
                case LeaveTypeName.AbortionLeave_210:
                    return new Maternity(context, LeaveTypeName.AbortionLeave_210, leaver, days);
                case LeaveTypeName.AbortionLeave_210_UP:
                    return new Maternity(context, LeaveTypeName.AbortionLeave_210_UP, leaver, days);
                case LeaveTypeName.JoinArmy:
                    return new JoinArmy(context, leaver, days);
                case LeaveTypeName.Relocation:
                    return new Relocation(context, leaver, days);
                case LeaveTypeName.DonateBlood:
                    return new DonateBlood(context, leaver, days);
                case LeaveTypeName.SalemanHomeLeave:
                    return new SalemanHome(context, leaver, days);
                case LeaveTypeName.SpecialHomeLeave:
                    return new SpecialHome(context, leaver, days);
                default:
                    return new Other(context, leaver, days);
            }
        }
        /// <summary>
        /// 添加一个请假对象到字典中，如果该请假不存在，则创建一个，如何存在，则合并请假天数。
        /// </summary>
        /// <param name="leaveTypeValue">请假类型的枚举值</param>
        /// <param name="leaver"></param>
        /// <param name="days"></param>
        /// <returns></returns>
        public ILeaveType AppendLeave(Context context, int leaveTypeValue, long leaver, double days)
        {
            ILeaveType leave = null;
            
            string type = "L" + leaveTypeValue + "_" + leaver;
            // 哺乳假不进行合并
            if((LeaveTypeName)leaveTypeValue == LeaveTypeName.BreastfeedingLeave)
            {
                type += "_" + count.ToString();
                count++;
            }
            if (leaveTypes.ContainsKey(type))
            {
                leave = leaveTypes[type];
                leave.MergeLeaveDays(days);
            }
            else
            {
                leave = MakeLeave(context, leaveTypeValue, leaver, days);
                leaveTypes.Add(type, leave);
            }
            
            return leave;
        }
        /// <summary>
        /// 验证请假是否通过
        /// </summary>
        /// <param name="msg">未通过的提示消息</param>
        /// <returns></returns>
        public bool VadidateLeave(ref string msg)
        {
            bool isPass = true;
            foreach(var leave in leaveTypes)
            {
                if(!leave.Value.ValidateLeave(ref msg))
                {
                    isPass = false;
                }
            }
            return isPass;
        }
    }
}
