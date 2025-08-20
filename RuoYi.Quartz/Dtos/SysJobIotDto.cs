using System.ComponentModel.DataAnnotations;

namespace RuoYi.Quartz.Dtos
{
    /// <summary>
    /// 定时任务IOT扩展表 DTO
    /// </summary>
    public class SysJobIotDto : SysJobDto
    {
        /// <summary>
        /// 目标类型
        /// </summary>
        [Required(ErrorMessage = "目标类型不能为空"), MaxLength(64,ErrorMessage = "目标类型不能超过64个字符")]
        public string? TargetType { get; set; }

        /// <summary>
        /// 任务类型
        /// </summary>
        [MaxLength(64,ErrorMessage = "任务类型不能超过64个字符")]
        public string? TaskType { get; set; }

        /// <summary>
        /// 设备ID
        /// </summary>
        public long? DeviceId { get; set; }

        /// <summary>
        /// 产品ID
        /// </summary>
        public long? ProductId { get; set; }

        /// <summary>
        ///  选择点位 （ iot_product_point表的 point_key 字段）
        /// </summary>
        [MaxLength(500,ErrorMessage = "选择点位不能超过500个字符")]
        public string? SelectPoints { get; set; }

        /// <summary>
        /// 触发源
        /// </summary>
        [MaxLength(64,ErrorMessage = "触发源不能超过64个字符")]
        public string? TriggerSource { get; set; }

        /// <summary>
        /// 任务启停标记（1启动 0停止）
        /// </summary>
        public int? Star { get; set; }
    }
}