using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RuoYi.Tcp.Configs
{
    /// <summary>
    /// Options for Tcp server listener. 监听端口
    /// </summary>
    public class TcpServerOptions
    {
        /// <summary>
        /// Port to listen on.
        /// </summary>
        public int Port { get; set; } = 5003;


        /// <summary>
        /// Interval in seconds between polling cycles.  采集时间
        /// 后期需要根据表字段进行动态配置
        /// </summary>
        public int PollIntervalSeconds { get; set; } = 3;
    }
}
