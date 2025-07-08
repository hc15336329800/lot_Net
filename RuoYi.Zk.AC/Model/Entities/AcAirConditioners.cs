using RuoYi.Data.Entities;
using SqlSugar;
using System;

namespace RuoYi.Zk.AC.Model.Entities
{
    /// <summary>
    /// 空调设备表
    /// </summary>
    [SugarTable("ac_air_conditioners", "空调设备表")]
    public class AcAirConditioners : UserBaseEntity
    {
        /** 空调ID */
        [SugarColumn(ColumnName = "id", ColumnDescription = "空调ID", IsPrimaryKey = true, IsIdentity = true)]
        public long Id { get; set; }

        /** 空调唯一编码 */
        [SugarColumn(ColumnName = "code", ColumnDescription = "空调唯一编码")]
        public string Code { get; set; }

        /** 品牌 */
        [SugarColumn(ColumnName = "brand", ColumnDescription = "品牌")]
        public string Brand { get; set; }


        /** 空调型号 */
        [SugarColumn(ColumnName = "model", ColumnDescription = "型号")]
        public string? Model { get; set; }

        /** 安装日期 */
        [SugarColumn(ColumnName = "installation_date", ColumnDescription = "安装日期")]
        public DateTime? InstallationDate { get; set; }

        /** 空调状态 */
        [SugarColumn(ColumnName = "status", ColumnDescription = "空调状态")]
        public string? Status { get; set; }

        /** 出风口温度 */
        [SugarColumn(ColumnName = "temperature", ColumnDescription = "出风口温度")]
        public decimal? Temperature { get; set; }

        /** 管道压力 */
        [SugarColumn(ColumnName = "pipeline_pressure", ColumnDescription = "管道压力")]
        public decimal? PipelinePressure { get; set; }

        /** 电度 */
        [SugarColumn(ColumnName = "electricity_usage", ColumnDescription = "电度")]
        public decimal? ElectricityUsage { get; set; }

        /** 省份 */
        [SugarColumn(ColumnName = "province", ColumnDescription = "省份")]
        public string? Province { get; set; }

        /** 城市 */
        [SugarColumn(ColumnName = "city", ColumnDescription = "城市")]
        public string? City { get; set; }

        /** 安装地点详细地址 */
        [SugarColumn(ColumnName = "address", ColumnDescription = "安装地点详细地址")]
        public string? Address { get; set; }

        /** 纬度 */
        [SugarColumn(ColumnName = "latitude", ColumnDescription = "纬度")]
        public decimal? Latitude { get; set; }

        /** 经度 */
        [SugarColumn(ColumnName = "longitude", ColumnDescription = "经度")]
        public decimal? Longitude { get; set; }

 


        /** 软删除标记 */
        [SugarColumn(ColumnName = "is_deleted", ColumnDescription = "软删除标记")]
        public string IsDeleted { get; set; }


        /** 空调ID */
        [SugarColumn(ColumnName = "tenant_id",ColumnDescription = "tid")]
        public long TenantId { get; set; }
    }
}
