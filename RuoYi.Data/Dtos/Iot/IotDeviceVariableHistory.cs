using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RuoYi.Data.Entities;
using SqlSugar;

namespace RuoYi.Data.Dtos.Iot
{
    /// <summary>
    /// 物联网—设备变量历史表
    /// </summary>
    [SugarTable("iot_device_variable_history","物联网—设备变量历史表")]
    public class IotDeviceVariableHistory : UserBaseEntity
    {
        [SugarColumn(ColumnName = "id",IsPrimaryKey = true)]
        public long Id { get; set; }

        [SugarColumn(ColumnName = "device_id",ColumnDescription = "设备ID")]
        public long DeviceId { get; set; }

        [SugarColumn(ColumnName = "variable_id",ColumnDescription = "变量ID")]
        public long VariableId { get; set; }

        [SugarColumn(ColumnName = "variable_key",ColumnDescription = "变量标识")]
        public string VariableKey { get; set; } = string.Empty;

        [SugarColumn(ColumnName = "value",ColumnDescription = "采集值")]
        public string? Value { get; set; }

        [SugarColumn(ColumnName = "timestamp",ColumnDescription = "采集时间")]
        public DateTime Timestamp { get; set; }

        [SugarColumn(ColumnName = "status",ColumnDescription = "状态")]
        public string Status { get; set; } = "0";

        [SugarColumn(ColumnName = "del_flag",ColumnDescription = "删除标志")]
        public string DelFlag { get; set; } = "0";

        [SugarColumn(ColumnName = "remark",ColumnDescription = "备注")]
        public string? Remark { get; set; }
    }
}
