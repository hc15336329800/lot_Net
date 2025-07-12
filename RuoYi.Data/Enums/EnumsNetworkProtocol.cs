using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RuoYi.Data.Enums
{
    /// <summary>
    /// 联网方式
    /// </summary>
    public enum EnumsNetworkProtocol
    {
        [Description("蜂窝数据2/3/4/5G")]
        Cellular2345G = 1,

        [Description("以太网")]
        Ethernet = 2,

        [Description("WiFi")]
        WiFi = 3,

        [Description("NB-IoT")]
        NBIoT = 4,

        [Description("串口")]
        SerialPort = 5
    }
}
