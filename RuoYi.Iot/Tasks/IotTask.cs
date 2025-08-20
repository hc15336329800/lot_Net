using RuoYi.Common.Utils;
using RuoYi.Framework.Attributes;
using RuoYi.Iot.Services;
using RuoYi.Quartz.Dtos;
using RuoYi.Quartz.Services;
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
    /// 发送读取指令到指定设备，若同时存在设备ID和产品ID，优先使用设备ID
    /// </summary>
    public async Task sendReadCommand( )
    {
        var logger = App.GetService<ILogger<IotTask>>();
        var deviceSvc = App.GetService<IotDeviceService>();
        var pointSvc = App.GetService<IotProductPointService>();
        var tcpSender = App.GetService<ITcpSender>();
        var jobExtSvc = App.GetService<SysJobIotService>();

        Console.WriteLine($"[DEBUG] {DateTime.Now:yyyy-MM-dd HH:mm:ss} -> sendReadCommand() 方法开始执行");

        // 获取当前任务及其扩展信息
        var job = JobContext.CurrentJob;
        if(job == null)
        {
            logger.LogWarning("未获取到任务上下文");
            Console.WriteLine("[WARN] 未获取到任务上下文");
            return;
        }
        Console.WriteLine($"[DEBUG] 当前任务ID: {job.JobId}");

        var ext = await jobExtSvc.FirstOrDefaultAsync(e => e.JobId == job.JobId);
        if(ext == null)
        {
            logger.LogWarning($"任务 {job.JobId} 未找到扩展信息");
            Console.WriteLine($"[WARN] 任务 {job.JobId} 未找到扩展信息");
            return;
        }

        long? deviceId = ext.DeviceId;
        long? productId = ext.productId;
        Console.WriteLine($"[DEBUG] 从扩展信息读取 -> deviceId={deviceId}, productId={productId}");

        // 若同时存在设备ID和产品ID，以设备ID为准
        if(deviceId == null && productId != null)
        {
            Console.WriteLine($"[DEBUG] 未指定设备ID，根据产品ID {productId} 查询设备");
            var devEntity = await deviceSvc.FirstOrDefaultAsync(d => d.ProductId == productId);
            if(devEntity == null)
            {
                logger.LogWarning($"产品 {productId} 未找到设备");
                Console.WriteLine($"[WARN] 产品 {productId} 未找到设备");
                return;
            }
            if(!string.Equals(devEntity.DeviceStatus,"online1",StringComparison.OrdinalIgnoreCase))
            {
                logger.LogWarning($"设备 {devEntity.Id} 不在线");
                Console.WriteLine($"[WARN] 设备 {devEntity.Id} 不在线");
                return;
            }
            deviceId = devEntity.Id;
            productId = devEntity.ProductId;
        }
        else if(deviceId != null)
        {
            Console.WriteLine($"[DEBUG] 直接使用设备ID {deviceId} 查询设备");
            var devEntity = await deviceSvc.FirstOrDefaultAsync(d => d.Id == deviceId);
            if(devEntity == null)
            {
                logger.LogWarning($"设备 {deviceId} 不存在");
                Console.WriteLine($"[WARN] 设备 {deviceId} 不存在");
                return;
            }
            productId = devEntity.ProductId;
            if(!string.Equals(devEntity.DeviceStatus,"online1",StringComparison.OrdinalIgnoreCase))
            {
                logger.LogDebug($"设备 {deviceId} 不在线，跳过执行 sendReadCommand");
                Console.WriteLine($"[DEBUG] 设备 {deviceId} 不在线，跳过执行 sendReadCommand");
                return;
            }
        }
        else
        {
            logger.LogWarning($"任务 {job.JobId} 未配置设备或产品");
            Console.WriteLine($"[WARN] 任务 {job.JobId} 未配置设备或产品");
            return;
        }

        Console.WriteLine($"[INFO] 开始执行 sendReadCommand，deviceId={deviceId}, productId={productId}");

        // 获取点位并发送指令
        var points = await pointSvc.GetCachedListAsync(productId!.Value);
        Console.WriteLine($"[DEBUG] 产品 {productId} 获取到 {points.Count} 个点位");

        var targets = points
            .Where(p => p.RegisterAddress.HasValue && p.SlaveAddress.HasValue)
            .ToList();

        Console.WriteLine($"[DEBUG] 有效点位数量: {targets.Count}");
        if(targets.Count == 0)
        {
            logger.LogWarning($"设备 {deviceId} 未找到可用的点位");
            Console.WriteLine($"[WARN] 设备 {deviceId} 未找到可用的点位");
            return;
        }

        foreach(var p in targets)
        {
            byte slave = (byte)p.SlaveAddress!.Value;
            byte func = (byte)(p.FunctionCode ?? 3);
            ushort start = (ushort)p.RegisterAddress!.Value;
            ushort qty = (ushort)Math.Max(1,(p.DataLength ?? 16) / 16);

            var frame = ModbusUtils.BuildReadFrame(slave,func,start,qty);
            Console.WriteLine($"[INFO] 发送读取指令 -> Device={deviceId}, Slave={slave}, Func={func}, Start={start}, Qty={qty}, Frame={BitConverter.ToString(frame)}");
            await tcpSender.SendAsync(deviceId.Value,frame);
        }

        Console.WriteLine($"[DEBUG] {DateTime.Now:yyyy-MM-dd HH:mm:ss} -> sendReadCommand() 方法执行结束");
    }


}
