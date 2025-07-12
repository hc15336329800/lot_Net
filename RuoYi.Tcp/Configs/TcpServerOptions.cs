using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RuoYi.Tcp.Configs
{
    /// <summary>
    /// Options for Tcp server listener.
    /// </summary>
    public class TcpServerOptions
    {
        /// <summary>
        /// Port to listen on.
        /// </summary>
        public int Port { get; set; } = 5003;
    }
}
