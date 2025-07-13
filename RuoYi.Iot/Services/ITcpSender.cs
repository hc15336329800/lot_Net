using System.Collections.Concurrent;

/// <summary>
/// 发送数据到TCP连接设备的抽象接口。
/// 用于定义设备通讯的统一发送入口（比如用于控制、主动下发指令等）。
/// </summary>
public interface ITcpSender
{
    /// <summary>
    /// 异步发送数据到指定设备，通常用于Modbus或自定义协议的主动通讯。
    /// </summary>
    /// <param name="deviceId">目标设备的唯一ID</param>
    /// <param name="data">要发送的数据帧（字节数组）</param>
    /// <param name="token">可选：取消任务的标记（用于超时或强制中断）</param>
    /// <returns>
    /// 返回设备响应的数据帧（字节数组）；如果没有响应或发送失败则返回null。
    /// </returns>
    Task<byte[]?> SendAsync(long deviceId,byte[] data,CancellationToken token = default);

 
}