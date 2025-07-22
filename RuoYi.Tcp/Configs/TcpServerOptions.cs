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
        /// Interval in seconds between polling cycles.   
        ///  轮询周期之间的间隔（以秒为单位）。
        /// </summary>
        public int PollIntervalSeconds { get; set; } = 1;
    }
}
