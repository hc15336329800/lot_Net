using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SqlSugar;

namespace RuoYi.Data.Entities.Iot
{
    /// <summary>
    /// 物联网—设备表
    /// </summary>
    [SugarTable("iot_device","物联网—设备表")]
    public class IotDevice : UserBaseEntity
    {
        [SugarColumn(ColumnName = "id",IsPrimaryKey = true)]
        public long Id { get; set; }

        [SugarColumn(ColumnName = "device_name",ColumnDescription = "设备名称")]
        public string DeviceName { get; set; } = string.Empty;

        [SugarColumn(ColumnName = "device_status",ColumnDescription = "设备状态")]
        public string DeviceStatus { get; set; } = string.Empty;

        [SugarColumn(ColumnName = "device_dn",ColumnDescription = "设备DN")]
        public string DeviceDn { get; set; } = string.Empty;

        [SugarColumn(ColumnName = "comm_key",ColumnDescription = "通讯密钥")]
        public string CommKey { get; set; } = string.Empty;

        [SugarColumn(ColumnName = "iot_card_no",ColumnDescription = "物联网卡号")]
        public string? IotCardNo { get; set; }

        [SugarColumn(ColumnName = "tcp_host",ColumnDescription = "TCP 主机")]
        public string TcpHost { get; set; } = string.Empty;

        [SugarColumn(ColumnName = "tcp_port",ColumnDescription = "TCP 端口")]
        public int TcpPort { get; set; }

        [SugarColumn(ColumnName = "auto_reg_packet",ColumnDescription = "自动生成注册包")]
        public string AutoRegPacket { get; set; } = string.Empty;

        [SugarColumn(ColumnName = "org_id",ColumnDescription = "所属组织ID")]
        public long OrgId { get; set; }

        [SugarColumn(ColumnName = "product_id",ColumnDescription = "产品ID")]
        public long ProductId { get; set; }

        [SugarColumn(ColumnName = "tag_category",ColumnDescription = "标签分类")]
        public string? TagCategory { get; set; }

        [SugarColumn(ColumnName = "activate_time",ColumnDescription = "激活时间")]
        public DateTime? ActivateTime { get; set; }

        [SugarColumn(ColumnName = "status",ColumnDescription = "状态")]
        public string Status { get; set; } = "0";

        [SugarColumn(ColumnName = "del_flag",ColumnDescription = "删除标志")]
        public string DelFlag { get; set; } = "0";

        [SugarColumn(ColumnName = "remark",ColumnDescription = "备注")]
        public string? Remark { get; set; }
    }
}
