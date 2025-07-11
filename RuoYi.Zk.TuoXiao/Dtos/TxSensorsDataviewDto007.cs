using System.Collections.Generic;
using RuoYi.Data.Dtos;

namespace RuoYi.Zk.TuoXiao.Dtos
{
    /// <summary>
    ///  工厂设备_传感器大屏数据 对象 tx_sensors_dataview
    ///  author ruoyi.net
    ///  date   2024-10-26 16:11:03
    /// </summary>
    public class TxSensorsDataviewDto007 : BaseDto
    {
        /// <summary>
        /// 主键ID
        /// </summary>
        public long Id { get; set; }

        /// <summary>
        /// 公司唯一编码
        /// </summary>
        public string? Code { get; set; }

        /// <summary>
        /// 状态 (0:正常, 1:禁用)
        /// </summary>
        public string? Status { get; set; }

        /// <summary>
        /// 租户ID
        /// </summary>
        public string? TenantId { get; set; }

        /// <summary>
        /// 软删除标记 (0:未删除, 1:已删除)
        /// </summary>
        public int? IsDeleted { get; set; }

        /// <summary>
        /// 料仓温度1
        /// </summary>
        public string? SiloTemp1 { get; set; }

        /// <summary>
        /// 料仓温度2
        /// </summary>
        public string? SiloTemp2 { get; set; }

        /// <summary>
        /// 风机温度
        /// </summary>
        public string? FanTemp { get; set; }

        /// <summary>
        /// 氧含量
        /// </summary>
        public string? OxygenContent { get; set; }

        /// <summary>
        /// 锅炉负荷
        /// </summary>
        public string? BoilerLoad { get; set; }

        /// <summary>
        /// NOx 含量
        /// </summary>
        public string? Nox { get; set; }

        /// <summary>
        /// 进口压力
        /// </summary>
        public string? InletPressure { get; set; }

        /// <summary>
        /// 出口压力
        /// </summary>
        public string? OutletPressure { get; set; }

        /// <summary>
        /// 称重
        /// </summary>
        public string? Weight { get; set; }

        /// <summary>
        /// 电加热温度
        /// </summary>
        public string? ElectricHeatingTemp { get; set; }

        /// <summary>
        /// 电加热启停  2停止  1启动
        /// </summary>
        public string? HeaterStart { get; set; }

        /// <summary>
        /// 电加热手自动 2手动  1自动
        /// </summary>
        public string? HeaterMode { get; set; }

        /// <summary>
        /// 电加热温度上限
        /// </summary>
        public string? HeaterTempMax { get; set; }

        /// <summary>
        /// 电加热温度下限
        /// </summary>
        public string? HeaterTempMin { get; set; }

        /// <summary>
        /// 振打电机启停
        /// </summary>
        public string? VibratorStart { get; set; }

        /// <summary>
        /// 振打电机手自动
        /// </summary>
        public string? VibratorMode { get; set; }

        /// <summary>
        /// 振打电机震动时间
        /// </summary>
        public string? VibratorDuration { get; set; }

        /// <summary>
        /// 振打电机震动间隔
        /// </summary>
        public string? VibratorInterval { get; set; }

        /// <summary>
        /// 下料器启停
        /// </summary>
        public string? FeederStart { get; set; }

        /// <summary>
        /// 下料器手自动
        /// </summary>
        public string? FeederMode { get; set; }

        /// <summary>
        /// 下料器设定HZ
        /// </summary>
        public string? FeederSetHz { get; set; }

        /// <summary>
        /// 下料器实际HZ
        /// </summary>
        public string? FeederActualHz { get; set; }

        /// <summary>
        /// 风机启停
        /// </summary>
        public string? FanStart { get; set; }

        /// <summary>
        /// 风机手自动
        /// </summary>
        public string? FanMode { get; set; }

        /// <summary>
        /// 风机设定HZ
        /// </summary>
        public string? FanSetHz { get; set; }

        /// <summary>
        /// 风机实际HZ
        /// </summary>
        public string? FanActualHz { get; set; }

    }
}


