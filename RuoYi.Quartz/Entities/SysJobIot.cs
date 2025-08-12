using RuoYi.Data.Entities;
using SqlSugar;

namespace RuoYi.Quartz.Entities
{
    /// <summary>
    ///  定时任务IOT扩展表 对象 sys_job_iot
    /// </summary>
    [SugarTable("sys_job_iot","定时任务IOT扩展表")]
    public class SysJobIot : UserBaseEntity
    {
        /// <summary>
        /// 任务ID
        /// </summary>
        [SugarColumn(ColumnName = "job_id",ColumnDescription = "任务ID",IsPrimaryKey = true)]
        public long JobId { get; set; }

        /// <summary>
        /// 目标类型
        /// </summary>
        [SugarColumn(ColumnName = "target_type",ColumnDescription = "目标类型")]
        public string? TargetType { get; set; }

        /// <summary>
        /// 任务类型
        /// </summary>
        [SugarColumn(ColumnName = "task_type",ColumnDescription = "任务类型")]
        public string? TaskType { get; set; }

        /// <summary>
        /// 设备ID
        /// </summary>
        [SugarColumn(ColumnName = "device_id",ColumnDescription = "设备ID")]
        public long? DeviceId { get; set; }

        /// <summary>
        /// 选择点位
        /// </summary>
        [SugarColumn(ColumnName = "select_points",ColumnDescription = "选择点位")]
        public string? SelectPoints { get; set; }

        /// <summary>
        /// 触发源
        /// </summary>
        [SugarColumn(ColumnName = "trigger_source",ColumnDescription = "触发源")]
        public string? TriggerSource { get; set; }

        /// <summary>
        /// 状态
        /// </summary>
        [SugarColumn(ColumnName = "status",ColumnDescription = "状态")]
        public string? Status { get; set; }

        /// <summary>
        /// 备注
        /// </summary>
        [SugarColumn(ColumnName = "remark",ColumnDescription = "备注")]
        public string? Remark { get; set; }
    }
}