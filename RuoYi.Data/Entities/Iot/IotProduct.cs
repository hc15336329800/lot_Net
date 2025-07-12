using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SqlSugar;

namespace RuoYi.Data.Entities.Iot
{
    /// <summary>
    /// 物联网—产品表
    /// </summary>
    [SugarTable("iot_product","物联网—产品表")]
    public class IotProduct : UserBaseEntity
    {
        [SugarColumn(ColumnName = "id",IsPrimaryKey = true)]
        public long Id { get; set; }

        [SugarColumn(ColumnName = "product_name",ColumnDescription = "产品名称")]
        public string ProductName { get; set; } = string.Empty;

        [SugarColumn(ColumnName = "org_id",ColumnDescription = "所属组织ID")]
        public long OrgId { get; set; }

        [SugarColumn(ColumnName = "product_model",ColumnDescription = "产品型号")]
        public string ProductModel { get; set; } = string.Empty;

        [SugarColumn(ColumnName = "product_code",ColumnDescription = "产品编码")]
        public string ProductCode { get; set; } = string.Empty;

        [SugarColumn(ColumnName = "brand_name",ColumnDescription = "品牌名称")]
        public string BrandName { get; set; } = string.Empty;

        [SugarColumn(ColumnName = "network_protocol",ColumnDescription = "联网方式（，1 表示蜂窝数据2/3/4/5G，2 表示以太网，3 表示 WiFi，4 表示 NB-IoT，5 表示串口。）")]
        public string NetworkProtocol { get; set; } = string.Empty;

        [SugarColumn(ColumnName = "access_protocol",ColumnDescription = "接入协议（1 表示 TCP，2 表示 MQTT，3 表示 HTTPS，4 表示 LwM2M，5 表示 LoRaWan，6 表示通过网关。）")]
        public string AccessProtocol { get; set; } = string.Empty;

        [SugarColumn(ColumnName = "data_protocol",ColumnDescription = "数据协议（1 表示 Modbus RTU，2 表示 Modbus TCP，3 表示 DL/T645-1997，4 表示 DL/T645-2007，5 表示 DL/T698.45-2017，6 表示 JSON，7 表示数据透传，8 表示自定义 Zigbee 协议，9 表示 CJ/T188-2004。）")]
        public string DataProtocol { get; set; } = string.Empty;

        [SugarColumn(ColumnName = "is_shared",ColumnDescription = "是否共享")]
        public int IsShared { get; set; }

        [SugarColumn(ColumnName = "description",ColumnDescription = "产品描述")]
        public string? Description { get; set; }

        [SugarColumn(ColumnName = "status",ColumnDescription = "状态")]
        public string Status { get; set; } = "0";

        [SugarColumn(ColumnName = "del_flag",ColumnDescription = "删除标志")]
        public string DelFlag { get; set; } = "0";

        [SugarColumn(ColumnName = "remark",ColumnDescription = "备注")]
        public string? Remark { get; set; }
    }
}
