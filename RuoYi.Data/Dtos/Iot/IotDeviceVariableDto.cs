using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RuoYi.Data.Dtos.IOT
{
    /// <summary>
    /// 物联网—设备变量明细表
    /// </summary>
    public class IotDeviceVariableDto : BaseDto
    {
        public long Id { get; set; }
        public long? DeviceId { get; set; }
        public long? VariableId { get; set; }
        public string? VariableName { get; set; }
        public string? VariableKey { get; set; }
        public string? VariableType { get; set; }
        public string? CurrentValue { get; set; }
        public DateTime? LastUpdateTime { get; set; }
        public string? Status { get; set; }
        public string? DelFlag { get; set; }
    }
}
