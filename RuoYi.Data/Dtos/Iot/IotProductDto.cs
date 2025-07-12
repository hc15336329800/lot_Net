using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RuoYi.Data.Dtos.IOT
{
    /// <summary>
    /// 物联网—产品表
    /// </summary>
    public class IotProductDto : BaseDto
    {
        public long Id { get; set; }
        public string? ProductName { get; set; }
        public long? OrgId { get; set; }
        public string? ProductModel { get; set; }
        public string? ProductCode { get; set; }
        public string? BrandName { get; set; }
        public string? NetworkProtocol { get; set; }
        public string? AccessProtocol { get; set; }
        public string? DataProtocol { get; set; }
        public int? IsShared { get; set; }
        public string? Description { get; set; }
        public string? Status { get; set; }
        public string? DelFlag { get; set; }
    }
}
