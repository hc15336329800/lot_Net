using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RuoYi.Data.Dtos.IOT
{
    /// <summary>
    /// 物联网—设备表
    /// </summary>
    public class IotDeviceDto : BaseDto
    {
        public long Id { get; set; }
        public string? DeviceName { get; set; }
        public string? DeviceStatus { get; set; }
        public string? DeviceDn { get; set; }
        public string? CommKey { get; set; }
        public string? IotCardNo { get; set; }
        public string? TcpHost { get; set; }
        public int? TcpPort { get; set; }
        public string? AutoRegPacket { get; set; }
        public long? OrgId { get; set; }
        public long? ProductId { get; set; }
        public string? TagCategory { get; set; }
        public DateTime? ActivateTime { get; set; }
        public string? Status { get; set; }
        public string? DelFlag { get; set; }
        public string? Remark { get; set; }
    }
}
