using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RuoYi.Data.Dtos.Iot
{
    /// <summary>
    /// 物联网—设备变量历史表
    /// </summary>
    public class IotDeviceVariableHistoryDto : BaseDto
    {
        public long Id { get; set; }
        public long? DeviceId { get; set; }
        public long? VariableId { get; set; }
        public string? VariableKey { get; set; }
        public string? Value { get; set; }
        public DateTime? Timestamp { get; set; }
        public string? Status { get; set; }
        public string? DelFlag { get; set; }
        public string? Remark { get; set; }
    }
}
