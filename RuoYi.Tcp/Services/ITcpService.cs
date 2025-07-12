using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RuoYi.Tcp.Services
{
    /// <summary>
    /// TCP 服务接口，提供传感器位置数据和事件访问。
    /// </summary>
    public interface ITcpService
    {
        /// <summary>
        /// 传感器对应的轨道号字典。
        /// </summary>
        System.Collections.Concurrent.ConcurrentDictionary<string,int> SensorRails { get; }

        /// <summary>
        /// 传感器对应的位置点字典。
        /// </summary>
        System.Collections.Concurrent.ConcurrentDictionary<string,int> SensorPositions { get; }

        /// <summary>
        /// 当接收到新的定位数据时触发。参数依次为传感器ID、轨道号、定位点。
        /// </summary>
        event System.Action<string,int,int> OnDataReceived;
    }
}
