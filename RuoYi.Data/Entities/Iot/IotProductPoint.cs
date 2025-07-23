using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SqlSugar;

namespace RuoYi.Data.Entities.Iot
{
    /// <summary>
    /// 物联网—产品点位模板表
    /// </summary>
    [SugarTable("iot_product_point","物联网—产品点位模板表")]
    public class IotProductPoint : UserBaseEntity
    {
        [SugarColumn(ColumnName = "id",IsPrimaryKey = true)]
        public long Id { get; set; }

        [SugarColumn(ColumnName = "product_id",ColumnDescription = "所属产品ID")]
        public long ProductId { get; set; }

        [SugarColumn(ColumnName = "point_name",ColumnDescription = "点位名称")]
        public string PointName { get; set; } = string.Empty;

        [SugarColumn(ColumnName = "point_key",ColumnDescription = "点位标识")]
        public string PointKey { get; set; } = string.Empty;

        [SugarColumn(ColumnName = "variable_type",ColumnDescription = "变量类型")]
        public string VariableType { get; set; } = string.Empty;

        [SugarColumn(ColumnName = "data_type",ColumnDescription = "数据类型")]
        public string DataType { get; set; } = string.Empty;

        [SugarColumn(ColumnName = "unit",ColumnDescription = "单位")]
        public string? Unit { get; set; }

        [SugarColumn(ColumnName = "default_value",ColumnDescription = "默认值")]
        public string? DefaultValue { get; set; }

        [SugarColumn(ColumnName = "decimal_digits",ColumnDescription = "小数位数")]
        public int? DecimalDigits { get; set; }

        [SugarColumn(ColumnName = "max_value",ColumnDescription = "最大值")]
        public string? MaxValue { get; set; }

        [SugarColumn(ColumnName = "min_value",ColumnDescription = "最小值")]
        public string? MinValue { get; set; }

        [SugarColumn(ColumnName = "slave_address",ColumnDescription = "从机地址")]
        public int? SlaveAddress { get; set; }

        [SugarColumn(ColumnName = "function_code",ColumnDescription = "功能码")]
        public int? FunctionCode { get; set; }

        [SugarColumn(ColumnName = "data_length",ColumnDescription = "数据位数")]
        public int? DataLength { get; set; }

        [SugarColumn(ColumnName = "register_address",ColumnDescription = "寄存器地址")]
        public int? RegisterAddress { get; set; }

        [SugarColumn(ColumnName = "byte_order",ColumnDescription = "字节顺序")]
        public string? ByteOrder { get; set; }

        [SugarColumn(ColumnName = "signed",ColumnDescription = "有无符号")]
        public bool? Signed { get; set; }

        [SugarColumn(ColumnName = "read_type",ColumnDescription = "读写类型")]
        public int? ReadType { get; set; }

        [SugarColumn(ColumnName = "storage_mode",ColumnDescription = "存储方式")]
        public int? StorageMode { get; set; }

        [SugarColumn(ColumnName = "display_on_dashboard",ColumnDescription = "看板展示")]
        public bool? DisplayOnDashboard { get; set; }

        [SugarColumn(ColumnName = "collect_formula",ColumnDescription = "采集公式")]
        public string? CollectFormula { get; set; }

        [SugarColumn(ColumnName = "control_formula",ColumnDescription = "控制公式")]
        public string? ControlFormula { get; set; }

        [SugarColumn(ColumnName = "status",ColumnDescription = "状态")]
        public string Status { get; set; } = "0";

        [SugarColumn(ColumnName = "del_flag",ColumnDescription = "删除标志")]
        public string DelFlag { get; set; } = "0";

        [SugarColumn(ColumnName = "remark",ColumnDescription = "备注")]
        public string? Remark { get; set; }

        [SugarColumn(ColumnName = "cloud_access_info",ColumnDescription = "上云接入信息")]
        public string? CloudAccessInfo { get; set; }
    }
}
