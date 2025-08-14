using RuoYi.Common.Utils;
using RuoYi.Framework.Attributes;
using RuoYi.Iot.Services;
using RuoYi.Quartz.Dtos;
using RuoYi.Quartz.Utils;

namespace RuoYi.Iot.Tasks;

/// <summary>
/// 测试用：定时读取并写入设备点位
/// </summary>
[Task("iotTask")]
public class IotTask
{
    /// <summary>
    /// 示例：读取点位当前值并加 1 写回（直接传参）
    /// </summary>
    public async Task readAndWriteWithParams(long deviceId,string pointKey)
    {
        var logger = App.GetService<ILogger<IotTask>>();
        var deviceSvc = App.GetService<IotDeviceService>();
        var varSvc = App.GetService<IotDeviceVariableService>();

        var device = await deviceSvc.GetDtoAsync(deviceId);
        if(device == null)
        {
            logger.LogWarning($"Device {deviceId} not found");
            return;
        }

        var map = await varSvc.GetVariableMapAsync(deviceId);
        if(!map.TryGetValue(pointKey,out var variable) || !variable.VariableId.HasValue)
        {
            logger.LogWarning($"Point {pointKey} not found on device {deviceId}");
            return;
        }

        logger.LogInformation($"Current {pointKey} value: {variable.CurrentValue}");

        var newVal = (int.Parse(variable.CurrentValue ?? "0") + 1).ToString();
        await varSvc.SaveValueAsync(deviceId,variable.VariableId.Value,pointKey,newVal);

        logger.LogInformation($"Updated {pointKey} to {newVal}");
    }

    /// <summary>
    /// 示例：测试是否执行
    /// </summary>
    public async Task readAndWrite( )
    {

        Console.WriteLine("测试 ：定时任务readAndWrite执行");
        return;
 
    }


    /// <summary>
    /// 发送读取指令到设备，利用已有 TCP 连接获取点位数据
    /// </summary>
    /// <param name="deviceId">设备 ID</param>
    /// <param name="productId">产品 ID</param>
    public async Task sendReadCommand(long deviceId,long productId)
    {
        // 获取日志组件和所需的服务实例
        var logger = App.GetService<ILogger<IotTask>>();
        var deviceSvc = App.GetService<IotDeviceService>();
        var pointSvc = App.GetService<IotProductPointService>();
        var tcpSender = App.GetService<ITcpSender>();

        Console.WriteLine($"[INFO] 开始执行 sendReadCommand，deviceId={deviceId}, productId={productId}");


        // 根据设备 ID 查询设备信息
        var device = await deviceSvc.GetDtoAsync(deviceId);
        if(device == null)
        {
            Console.WriteLine($"[WARN] 设备 {deviceId} 不存在");

            logger.LogWarning($"设备 {deviceId} 不存在");
            return;
        }

        Console.WriteLine($"[INFO] 已获取设备信息：{device.DeviceName}，状态={device.DeviceStatus}");


        // 判断设备是否在线（online1 表示在线）
        if(!string.Equals(device.DeviceStatus,"online1",StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine($"[WARN] 设备 {deviceId} 不在线");

            logger.LogWarning($"设备 {deviceId} 不在线");
            return;
        }

        // 根据产品 ID 获取点位列表
        var points = await pointSvc.GetCachedListAsync(productId);
        Console.WriteLine($"[INFO] 获取到 {points.Count} 个点位");

        var targets = points
            .Where(p => p.RegisterAddress.HasValue && p.SlaveAddress.HasValue)
            .ToList();

        Console.WriteLine($"[INFO] 有效点位数量：{targets.Count}");


        if(targets.Count == 0)
        {
            Console.WriteLine("[WARN] 未找到可用的点位");
            logger.LogWarning("未找到可用的点位");
            return;
        }

        foreach(var p in targets)
        {
            // 组装 Modbus RTU 读取指令
            byte slave = (byte)p.SlaveAddress!.Value;
            byte func = (byte)(p.FunctionCode ?? 3);
            ushort start = (ushort)p.RegisterAddress!.Value;
            ushort qty = (ushort)Math.Max(1,(p.DataLength ?? 16) / 16);

            var frame = ModbusUtils.BuildReadFrame(slave,func,start,qty);
            Console.WriteLine($"[INFO] 发送读取指令 -> Slave={slave}, Func={func}, Start={start}, Qty={qty}, Frame={BitConverter.ToString(frame)}");

            // 通过当前 TCP 连接发送读取指令
            await tcpSender.SendAsync(deviceId,frame);
            Console.WriteLine($"[INFO] sendReadCommand 执行完成，已向设备 {deviceId} 发送所有读取指令");

        }
    }
 


}
 