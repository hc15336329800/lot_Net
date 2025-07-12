using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RuoYi.Data.Enums
{
    /// <summary>
    /// 接入协议
    /// </summary>
    public enum EnumsAccessProtocol
    {
        [Description("TCP")]
        TCP = 1,

        [Description("MQTT")]
        MQTT = 2,

        [Description("HTTPS")]
        HTTPS = 3,

        [Description("LwM2M")]
        LwM2M = 4,

        [Description("LoRaWan")]
        LoRaWan = 5,

        [Description("通过网关")]
        ViaGateway = 6
    }
}
