using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RuoYi.Zk.TuoXiao.Dtos
{
    /// <summary>
    /// 数据采集_数据传输对象(String)
    /// </summary>
    public class TxSensorsDataviewDto
    {
        public string? SiloTemp1 { get; set; }
        public string? SiloTemp2 { get; set; }
        public string? FanTemp { get; set; }
        public string? OxygenContent { get; set; }
        public string? BoilerLoad { get; set; }
        public string? Nox { get; set; }
        public string? InletPressure { get; set; }
        public string? OutletPressure { get; set; }
        public string? Weight { get; set; }
        public string? ElectricHeatingTemp { get; set; }
        public string? HeaterStart { get; set; }
        public string? HeaterMode { get; set; }
        public string? HeaterTempMax { get; set; }
        public string? HeaterTempMin { get; set; }
        public string? VibratorStart { get; set; }
        public string? VibratorMode { get; set; }
        public string? VibratorDuration { get; set; }
        public string? VibratorInterval { get; set; }
        public string? FeederStart { get; set; }
        public string? FeederMode { get; set; }
        public string? FeederSetHz { get; set; }
        public string? FeederActualHz { get; set; }
        public string? FanStart { get; set; }
        public string? FanMode { get; set; }
        public string? FanSetHz { get; set; }
        public string? FanActualHz { get; set; }
    }
}
