using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SqlSugar;

namespace RuoYi.Data.Entities.Iot
{
    /// <summary>
    /// 物联网—设备变量明细表
    /// </summary>
    [SugarTable("iot_device_variable","物联网—设备变量明细表")]
    public class IotDeviceVariable : UserBaseEntity
    {
        [SugarColumn(ColumnName = "id",IsPrimaryKey = true)]
        public long Id { get; set; }

        [SugarColumn(ColumnName = "device_id",ColumnDescription = "设备ID")]
        public long DeviceId { get; set; }

        [SugarColumn(ColumnName = "variable_id",ColumnDescription = "变量ID")]
        public long VariableId { get; set; }

        //[SugarColumn(ColumnName = "variable_name",ColumnDescription = "变量名称")]
        //public string VariableName { get; set; } = string.Empty;

        //[SugarColumn(ColumnName = "variable_key",ColumnDescription = "变量标识")]
        //public string VariableKey { get; set; } = string.Empty;

        //[SugarColumn(ColumnName = "variable_type",ColumnDescription = "变量类型")]
        //public string VariableType { get; set; } = string.Empty;

        [SugarColumn(ColumnName = "current_value",ColumnDescription = "当前值")]
        public string? CurrentValue { get; set; }

        [SugarColumn(ColumnName = "last_update_time",ColumnDescription = "更新时间")]
        public DateTime? LastUpdateTime { get; set; }

        [SugarColumn(ColumnName = "status",ColumnDescription = "状态")]
        public string Status { get; set; } = "0";

        [SugarColumn(ColumnName = "del_flag",ColumnDescription = "删除标志")]
        public string DelFlag { get; set; } = "0";

        [SugarColumn(ColumnName = "remark",ColumnDescription = "备注")]
        public string? Remark { get; set; }
    }
}
