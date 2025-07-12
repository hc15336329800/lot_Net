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

        [SugarColumn(ColumnName = "network_protocol",ColumnDescription = "联网方式")]
        public string NetworkProtocol { get; set; } = string.Empty;

        [SugarColumn(ColumnName = "access_protocol",ColumnDescription = "接入协议")]
        public string AccessProtocol { get; set; } = string.Empty;

        [SugarColumn(ColumnName = "data_protocol",ColumnDescription = "数据协议")]
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
