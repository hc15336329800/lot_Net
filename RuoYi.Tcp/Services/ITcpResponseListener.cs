using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RuoYi.Tcp.Services
{
    /// <summary>
    /// Callback for raw data received from a device. 用于接收设备原始数据的回调方法
    /// </summary>
    public interface ITcpResponseListener
    {
        void OnTcpDataReceived(long deviceId,byte[] data);
    }
}
