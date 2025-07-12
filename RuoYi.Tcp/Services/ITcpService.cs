using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using RuoYi.Data.Dtos.IOT;

namespace RuoYi.Tcp.Services
{
    /// <summary>
    /// TCP 协议处理接口
    /// </summary>
    public interface ITcpService
    {


        /// <summary>
        /// TCP 服务接口，当收到信息时候动态解析存库
        /// 处理来自设备的连接
        /// </summary>
        Task HandleClientAsync(TcpClient client,IotDeviceDto device,CancellationToken token);

 

    }
}
