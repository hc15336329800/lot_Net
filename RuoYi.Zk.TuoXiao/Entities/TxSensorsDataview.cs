using SqlSugar;
using System;
using System.Collections.Generic;
using RuoYi.Data.Entities;

namespace RuoYi.Zk.TuoXiao.Entities
{
    /// <summary>
    ///  工厂设备_传感器大屏数据 对象 tx_sensors_dataview
    ///  author ruoyi.net
    ///  date   2024-10-26 16:11:03
    /// </summary>
    [SugarTable("tx_sensors_dataview", "工厂设备_传感器大屏数据")]
    public class TxSensorsDataview : BaseEntity
    {
        /// <summary>
        /// 主键ID (id)
        /// </summary>
        [SugarColumn(ColumnName = "id", ColumnDescription = "主键ID", IsPrimaryKey = true, IsIdentity = true)]
        public long Id { get; set; }

        /// <summary>
        /// 公司唯一编码 (code)
        /// </summary>
        [SugarColumn(ColumnName = "code", ColumnDescription = "公司唯一编码")]
        public string? Code { get; set; }

        /// <summary>
        /// 状态 (0:正常, 1:禁用) (status)
        /// </summary>
        [SugarColumn(ColumnName = "status", ColumnDescription = "状态 (0:正常, 1:禁用)")]
        public string? Status { get; set; }

        /// <summary>
        /// 租户ID (tenant_id)
        /// </summary>
        [SugarColumn(ColumnName = "tenant_id", ColumnDescription = "租户ID")]
        public string? TenantId { get; set; }

        /// <summary>
        /// 软删除标记 (0:未删除, 1:已删除) (is_deleted)
        /// </summary>
        [SugarColumn(ColumnName = "is_deleted", ColumnDescription = "软删除标记 (0:未删除, 1:已删除)")]
        public int? IsDeleted { get; set; }

        /// <summary>
        /// 料仓温度1 (silo_temp1)
        /// </summary>
        [SugarColumn(ColumnName = "silo_temp1", ColumnDescription = "料仓温度1")]
        public string? SiloTemp1 { get; set; }

        /// <summary>
        /// 料仓温度2 (silo_temp2)
        /// </summary>
        [SugarColumn(ColumnName = "silo_temp2", ColumnDescription = "料仓温度2")]
        public string? SiloTemp2 { get; set; }

        /// <summary>
        /// 风机温度 (fan_temp)
        /// </summary>
        [SugarColumn(ColumnName = "fan_temp", ColumnDescription = "风机温度")]
        public string? FanTemp { get; set; }

        /// <summary>
        /// 氧含量 (oxygen_content)
        /// </summary>
        [SugarColumn(ColumnName = "oxygen_content", ColumnDescription = "氧含量")]
        public string? OxygenContent { get; set; }

        /// <summary>
        /// 锅炉负荷 (boiler_load)
        /// </summary>
        [SugarColumn(ColumnName = "boiler_load", ColumnDescription = "锅炉负荷")]
        public string? BoilerLoad { get; set; }

        /// <summary>
        /// NOx 含量 (nox)
        /// </summary>
        [SugarColumn(ColumnName = "nox", ColumnDescription = "NOx 含量")]
        public string? Nox { get; set; }

        /// <summary>
        /// 进口压力 (inlet_pressure)
        /// </summary>
        [SugarColumn(ColumnName = "inlet_pressure", ColumnDescription = "进口压力")]
        public string? InletPressure { get; set; }

        /// <summary>
        /// 出口压力 (outlet_pressure)
        /// </summary>
        [SugarColumn(ColumnName = "outlet_pressure", ColumnDescription = "出口压力")]
        public string? OutletPressure { get; set; }

        /// <summary>
        /// 称重 (weight)
        /// </summary>
        [SugarColumn(ColumnName = "weight", ColumnDescription = "称重")]
        public string? Weight { get; set; }

        /// <summary>
        /// 电加热温度 (electric_heating_temp)
        /// </summary>
        [SugarColumn(ColumnName = "electric_heating_temp", ColumnDescription = "电加热温度")]
        public string? ElectricHeatingTemp { get; set; }

        /// <summary>
        /// 电加热启停 (heater_start)
        /// </summary>
        [SugarColumn(ColumnName = "heater_start", ColumnDescription = "电加热启停")]
        public string? HeaterStart { get; set; }

        /// <summary>
        /// 电加热手自动 (heater_mode)
        /// </summary>
        [SugarColumn(ColumnName = "heater_mode", ColumnDescription = "电加热手自动")]
        public string? HeaterMode { get; set; }

        /// <summary>
        /// 电加热温度上限 (heater_temp_max)
        /// </summary>
        [SugarColumn(ColumnName = "heater_temp_max", ColumnDescription = "电加热温度上限")]
        public string? HeaterTempMax { get; set; }

        /// <summary>
        /// 电加热温度下限 (heater_temp_min)
        /// </summary>
        [SugarColumn(ColumnName = "heater_temp_min", ColumnDescription = "电加热温度下限")]
        public string? HeaterTempMin { get; set; }

        /// <summary>
        /// 振打电机启停 (vibrator_start)
        /// </summary>
        [SugarColumn(ColumnName = "vibrator_start", ColumnDescription = "振打电机启停")]
        public string? VibratorStart { get; set; }

        /// <summary>
        /// 振打电机手自动 (vibrator_mode)
        /// </summary>
        [SugarColumn(ColumnName = "vibrator_mode", ColumnDescription = "振打电机手自动")]
        public string? VibratorMode { get; set; }

        /// <summary>
        /// 振打电机震动时间 (vibrator_duration)
        /// </summary>
        [SugarColumn(ColumnName = "vibrator_duration", ColumnDescription = "振打电机震动时间")]
        public string? VibratorDuration { get; set; }

        /// <summary>
        /// 振打电机震动间隔 (vibrator_interval)
        /// </summary>
        [SugarColumn(ColumnName = "vibrator_interval", ColumnDescription = "振打电机震动间隔")]
        public string? VibratorInterval { get; set; }

        /// <summary>
        /// 下料器启停 (feeder_start)
        /// </summary>
        [SugarColumn(ColumnName = "feeder_start", ColumnDescription = "下料器启停")]
        public string? FeederStart { get; set; }

        /// <summary>
        /// 下料器手自动 (feeder_mode)
        /// </summary>
        [SugarColumn(ColumnName = "feeder_mode", ColumnDescription = "下料器手自动")]
        public string? FeederMode { get; set; }

        /// <summary>
        /// 下料器设定HZ (feeder_set_hz)
        /// </summary>
        [SugarColumn(ColumnName = "feeder_set_hz", ColumnDescription = "下料器设定HZ")]
        public string? FeederSetHz { get; set; }

        /// <summary>
        /// 下料器实际HZ (feeder_actual_hz)
        /// </summary>
        [SugarColumn(ColumnName = "feeder_actual_hz", ColumnDescription = "下料器实际HZ")]
        public string? FeederActualHz { get; set; }

        /// <summary>
        /// 风机启停 (fan_start)
        /// </summary>
        [SugarColumn(ColumnName = "fan_start", ColumnDescription = "风机启停")]
        public string? FanStart { get; set; }

        /// <summary>
        /// 风机手自动 (fan_mode)
        /// </summary>
        [SugarColumn(ColumnName = "fan_mode", ColumnDescription = "风机手自动")]
        public string? FanMode { get; set; }

        /// <summary>
        /// 风机设定HZ (fan_set_hz)
        /// </summary>
        [SugarColumn(ColumnName = "fan_set_hz", ColumnDescription = "风机设定HZ")]
        public string? FanSetHz { get; set; }

        /// <summary>
        /// 风机实际HZ (fan_actual_hz)
        /// </summary>
        [SugarColumn(ColumnName = "fan_actual_hz", ColumnDescription = "风机实际HZ")]
        public string? FanActualHz { get; set; }

    }
}
