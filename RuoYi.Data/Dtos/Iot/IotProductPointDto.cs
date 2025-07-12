using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RuoYi.Data.Dtos.IOT
{
    /// <summary>
    /// 物联网—产品点位模板表
    /// </summary>
    public class IotProductPointDto : BaseDto
    {
        public long Id { get; set; }
        public long? ProductId { get; set; }
        public string? PointName { get; set; }
        public string? PointKey { get; set; }
        public string? VariableType { get; set; }
        public string? DataType { get; set; }
        public string? Unit { get; set; }
        public string? DefaultValue { get; set; }
        public int? DecimalDigits { get; set; }
        public string? MaxValue { get; set; }
        public string? MinValue { get; set; }
        public int? SlaveAddress { get; set; }
        public int? FunctionCode { get; set; }
        public int? DataLength { get; set; }
        public int? RegisterAddress { get; set; }
        public string? ByteOrder { get; set; }
        public bool? Signed { get; set; }
        public string? ReadType { get; set; }
        public string? StorageMode { get; set; }
        public bool? DisplayOnDashboard { get; set; }
        public string? CollectFormula { get; set; }
        public string? ControlFormula { get; set; }
        public string? Status { get; set; }
        public string? DelFlag { get; set; }
        public string? Remark { get; set; }
        public string? CloudAccessInfo { get; set; }
    }
}
