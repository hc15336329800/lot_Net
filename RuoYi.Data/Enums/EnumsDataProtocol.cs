using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RuoYi.Data.Enums
{
    /// <summary>
    /// 数据协议
    /// </summary>
    public enum EnumsDataProtocol
    {
        [Description("Modbus RTU")]
        ModbusRTU = 1,

        [Description("Modbus TCP")]
        ModbusTCP = 2,

        [Description("DL/T645-1997")]
        DLT645_1997 = 3,

        [Description("DL/T645-2007")]
        DLT645_2007 = 4,

        [Description("DL/T698.45-2017")]
        DLT69845_2017 = 5,

        [Description("JSON")]
        JSON = 6,

        [Description("数据透传")]
        Transparent = 7,

        [Description("自定义Zigbee协议")]
        CustomZigbee = 8,

        [Description("CJ/T188-2004")]
        CTT188_2004 = 9
    }
}
